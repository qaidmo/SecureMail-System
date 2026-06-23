using Microsoft.EntityFrameworkCore;
using SecureMailBackend.Data;
using SecureMailBackend.Services;
using SecureMailBackend.Services.Helpers;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using FirebaseAdmin.Messaging;

namespace SecureMailBackend.BackgroundServices
{
    public class GmailPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GmailPollingService> _logger;

        public GmailPollingService(IServiceProvider serviceProvider, ILogger<GmailPollingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Gmail Polling Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollEmailsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while polling emails.");
                }

                // Wait for 30 seconds before polling again (Development Mode)
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }

            _logger.LogInformation("Gmail Polling Service is stopping.");
        }

        private async Task PollEmailsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var analyzerService = scope.ServiceProvider.GetRequiredService<IEmailAnalyzerService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var clientId = configuration["GoogleOAuth:ClientId"];
            var clientSecret = configuration["GoogleOAuth:ClientSecret"];
            var aesKey = configuration["Encryption:AesKey"]!;

            // Get all active Gmail integrations
            var activeIntegrations = await dbContext.EmailIntegrations
                .Where(e => e.Provider == "GMAIL" && e.Status == "active")
                .ToListAsync(stoppingToken);

            foreach (var integration in activeIntegrations)
            {
                bool retryAfterRefresh = false;
                
                do
                {
                    try
                    {
                        // 1. Decrypt AES-256 encrypted access token and initialize Gmail Service
                        string decryptedAccessToken = AesEncryptionHelper.Decrypt(integration.AccessTokenEnc, aesKey);
                        var credential = GoogleCredential.FromAccessToken(decryptedAccessToken);
                        
                        var gmailService = new GmailService(new BaseClientService.Initializer()
                        {
                            HttpClientInitializer = credential,
                            ApplicationName = "SecureMail Background Poller",
                        });

                        // 2. Fetch Unread Messages (Inbox only)
                        var request = gmailService.Users.Messages.List("me");
                        request.LabelIds = new List<string> { "INBOX", "UNREAD" };
                        request.MaxResults = 10; // Process 10 at a time to avoid rate limits

                        var response = await request.ExecuteAsync(stoppingToken);

                        if (response.Messages != null && response.Messages.Any())
                        {
                            foreach (var messageHeader in response.Messages)
                            {
                                // Check if we already scanned this message
                                bool alreadyScanned = await dbContext.EmailMessages
                                    .AnyAsync(m => m.ProviderMessageId == messageHeader.Id && m.IntegrationId == integration.Id);
                                
                                if (alreadyScanned) continue;

                                // 3. Fetch Full Message Details
                                var msgReq = gmailService.Users.Messages.Get("me", messageHeader.Id);
                                msgReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                                var fullMsg = await msgReq.ExecuteAsync(stoppingToken);

                                var headers = fullMsg.Payload.Headers;
                                var fromHeader = headers.FirstOrDefault(h => h.Name.Equals("From", StringComparison.OrdinalIgnoreCase))?.Value;
                                var subjectHeader = headers.FirstOrDefault(h => h.Name.Equals("Subject", StringComparison.OrdinalIgnoreCase))?.Value;
                                var authResultsHeader = headers.FirstOrDefault(h => h.Name.Equals("Authentication-Results", StringComparison.OrdinalIgnoreCase))?.Value;

                                if (string.IsNullOrEmpty(fromHeader)) continue;

                                // Extract plain email from From header (e.g., "John Doe <john@attacker.com>" -> "john@attacker.com")
                                string senderEmail = ExtractEmailAddress(fromHeader);

                                string plainTextBody = "";
                                string htmlBody = "";
                                var attachmentNames = new List<string>();
                                var attachmentPayloads = new List<DTOs.AttachmentPayload>();
                                const long MAX_ATTACHMENT_SIZE_BYTES = 10 * 1024 * 1024; // 10MB OOM Protection

                                async Task ParsePartsAsync(Google.Apis.Gmail.v1.Data.MessagePart part)
                                {
                                    if (part == null) return;
                                    
                                    if (!string.IsNullOrEmpty(part.Filename))
                                    {
                                        attachmentNames.Add(part.Filename);

                                        // Download attachment binary data for YARA scanning (with 10MB OOM guard)
                                        if (!string.IsNullOrEmpty(part.Body?.AttachmentId))
                                        {
                                            long attachmentSize = part.Body?.Size ?? 0;
                                            if (attachmentSize > MAX_ATTACHMENT_SIZE_BYTES)
                                            {
                                                _logger.LogWarning(
                                                    "Skipping YARA download for '{Filename}' ({SizeMB:F1} MB) — exceeds 10 MB limit.",
                                                    part.Filename, attachmentSize / (1024.0 * 1024.0));
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    var attReq = gmailService.Users.Messages.Attachments
                                                        .Get("me", messageHeader.Id, part.Body.AttachmentId);
                                                    var attData = await attReq.ExecuteAsync(stoppingToken);
                                                    if (attData?.Data != null)
                                                    {
                                                        var rawBytes = DecodeBase64UrlToBytes(attData.Data);
                                                        attachmentPayloads.Add(new DTOs.AttachmentPayload
                                                        {
                                                            FileName = part.Filename,
                                                            Data = rawBytes,
                                                            SizeBytes = rawBytes.Length
                                                        });
                                                    }
                                                }
                                                catch (Exception attEx)
                                                {
                                                    _logger.LogWarning(attEx,
                                                        "Failed to download attachment '{Filename}' for YARA scanning. Skipping.",
                                                        part.Filename);
                                                }
                                            }
                                        }
                                    }

                                    if (part.MimeType == "text/plain" && part.Body?.Data != null)
                                    {
                                        plainTextBody += DecodeBase64Url(part.Body.Data);
                                    }
                                    else if (part.MimeType == "text/html" && part.Body?.Data != null)
                                    {
                                        htmlBody += DecodeBase64Url(part.Body.Data);
                                    }

                                    if (part.Parts != null)
                                    {
                                        foreach (var subPart in part.Parts)
                                            await ParsePartsAsync(subPart);
                                    }
                                }

                                await ParsePartsAsync(fullMsg.Payload);

                                // 4. Send to OSINT Engine (8-Layer — Phase 1 Upgrade)
                                var analysisResult = await analyzerService.AnalyzeEmailAsync(
                                    email: senderEmail,
                                    subject: subjectHeader ?? "",
                                    plainTextBody: plainTextBody,
                                    htmlBody: htmlBody,
                                    rawHeaders: authResultsHeader ?? "",
                                    attachmentNames: attachmentNames,
                                    attachmentPayloads: attachmentPayloads
                                );

                                // 5. Save the Message Record
                                var newMessage = new Models.EmailMessage
                                {
                                    IntegrationId = integration.Id,
                                    ProviderMessageId = messageHeader.Id,
                                    FromEmail = senderEmail,
                                    FromName = fromHeader, // Storing raw header for now
                                    Subject = subjectHeader ?? "No Subject",
                                    ReceivedAt = DateTime.UtcNow, // Should parse from Date header ideally
                                };
                                dbContext.EmailMessages.Add(newMessage);
                                await dbContext.SaveChangesAsync(stoppingToken); // Save to generate newMessage.Id

                                // 6. Save the Scan Result
                                var newScan = new Models.Scan
                                {
                                    UserId = integration.UserId,
                                    ScanType = "MESSAGE",
                                    MessageId = newMessage.Id,
                                    RiskScore = analysisResult.TrustScore,
                                    Verdict = analysisResult.RiskLevel.Contains("Safe") ? "SAFE" : (analysisResult.RiskLevel.Contains("High") ? "HIGH" : "RISK"),
                                    Provider = analysisResult.Provider,
                                    DomainCountry = analysisResult.Country,
                                    SpfStatus = analysisResult.HasSpfRecord,
                                    DmarcStatus = analysisResult.HasDmarcRecord,
                                    PlainTextBody = analysisResult.PlainTextBody,
                                    PhishingKeywordsJson = System.Text.Json.JsonSerializer.Serialize(analysisResult.PhishingKeywordsFound),
                                    MaliciousUrlsJson = System.Text.Json.JsonSerializer.Serialize(analysisResult.MaliciousUrls),
                                    ReasonsJson = System.Text.Json.JsonSerializer.Serialize(analysisResult.Reasons),
                                    RecommendationsJson = System.Text.Json.JsonSerializer.Serialize(analysisResult.Recommendations),
                                    CreatedAt = DateTime.UtcNow
                                };
                                dbContext.Scans.Add(newScan);
                                await dbContext.SaveChangesAsync(stoppingToken);

                                // FCM Trigger
                                if (newScan.Verdict == "HIGH" || newScan.Verdict == "RISK")
                                {
                                    var fcmTokens = await dbContext.DeviceTokens
                                        .Where(t => t.UserId == integration.UserId)
                                        .Select(t => t.FcmToken)
                                        .ToListAsync(stoppingToken);

                                    if (fcmTokens.Any())
                                    {
                                        var message = new MulticastMessage()
                                        {
                                            Tokens = fcmTokens,
                                            Notification = new FirebaseAdmin.Messaging.Notification()
                                            {
                                                Title = "⚠️ تحذير أمني خطير",
                                                Body = "تم رصد رسالة تصيد أو روابط خبيثة في بريدك!"
                                            },
                                            Data = new Dictionary<string, string>()
                                            {
                                                { "scanId", newScan.Id.ToString() },
                                                { "riskLevel", newScan.Verdict },
                                                { "sender", senderEmail }
                                            }
                                        };
                                        var fcmResponse = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                                        _logger.LogInformation($"FCM Multicast completed for User {integration.UserId}. Success: {fcmResponse.SuccessCount}, Failures: {fcmResponse.FailureCount}");
                                    }
                                }

                                _logger.LogInformation($"Scanned message {messageHeader.Id} from {senderEmail}. Score: {analysisResult.TrustScore}");
                            }
                        }
                        
                        retryAfterRefresh = false; // Success, end loop for this integration
                    }
                    catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        if (retryAfterRefresh) 
                        {
                            // Already tried refreshing and still failed
                            _logger.LogError($"Token refresh failed or invalid for Integration {integration.Id}. Marking inactive.");
                            integration.Status = "inactive";
                            await dbContext.SaveChangesAsync(stoppingToken);
                            break;
                        }

                        _logger.LogWarning($"Access Token expired for Integration {integration.Id}. Attempting auto-refresh...");
                        try
                        {
                            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                            {
                                ClientSecrets = new ClientSecrets
                                {
                                    ClientId = clientId,
                                    ClientSecret = clientSecret
                                }
                            });
                            
                            // Decrypt the stored refresh token before using it
                            string decryptedRefreshToken = AesEncryptionHelper.Decrypt(integration.RefreshTokenEnc!, aesKey);
                            var tokenResponse = await flow.RefreshTokenAsync(integration.UserId.ToString(), decryptedRefreshToken, stoppingToken);
                            
                            // Re-encrypt the new tokens before storing
                            integration.AccessTokenEnc = AesEncryptionHelper.Encrypt(tokenResponse.AccessToken, aesKey);
                            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                            {
                                integration.RefreshTokenEnc = AesEncryptionHelper.Encrypt(tokenResponse.RefreshToken, aesKey);
                            }
                            integration.UpdatedAt = DateTime.UtcNow;
                            
                            await dbContext.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"Token refreshed successfully for Integration {integration.Id}. Retrying fetch.");
                            retryAfterRefresh = true; // Retry the original code block
                        }
                        catch (Exception refreshEx)
                        {
                            _logger.LogError(refreshEx, $"Failed to refresh token using RefreshToken for Integration {integration.Id}. Scope revoked likely. Marking inactive.");
                            integration.Status = "inactive";
                            await dbContext.SaveChangesAsync(stoppingToken);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to poll for Integration ID: {integration.Id}");
                        break;
                    }
                } while (retryAfterRefresh);
            }
        }

        private string ExtractEmailAddress(string fromHeader)
        {
            int startIndex = fromHeader.IndexOf('<');
            int endIndex = fromHeader.IndexOf('>');
            
            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
            {
                return fromHeader.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
            }
            return fromHeader.Trim();
        }

        private string DecodeBase64Url(string base64Url)
        {
            var base64 = base64Url.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            var bytes = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        private byte[] DecodeBase64UrlToBytes(string base64Url)
        {
            var base64 = base64Url.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}
