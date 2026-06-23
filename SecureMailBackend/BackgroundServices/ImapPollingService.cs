using Microsoft.EntityFrameworkCore;
using SecureMailBackend.Data;
using SecureMailBackend.Services;
using SecureMailBackend.Services.Helpers;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using FirebaseAdmin.Messaging;
using System.Text.Json;

namespace SecureMailBackend.BackgroundServices
{
    /// <summary>
    /// Background worker that polls IMAP-connected accounts for unseen messages,
    /// feeds them through the EmailAnalyzerService, and stores results.
    /// Mirror architecture of GmailPollingService for feature parity.
    /// </summary>
    public class ImapPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ImapPollingService> _logger;

        // 10 MB OOM protection — matches GmailPollingService limit
        private const long MAX_ATTACHMENT_SIZE_BYTES = 10 * 1024 * 1024;

        public ImapPollingService(IServiceProvider serviceProvider, ILogger<ImapPollingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("IMAP Polling Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollImapAccountsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while polling IMAP accounts.");
                }

                // Wait for 30 seconds before polling again (matches Gmail interval)
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }

            _logger.LogInformation("IMAP Polling Service is stopping.");
        }

        private async Task PollImapAccountsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var analyzerService = scope.ServiceProvider.GetRequiredService<IEmailAnalyzerService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            string aesKey = configuration["Encryption:AesKey"]!;

            // Get all active IMAP integrations
            var activeIntegrations = await dbContext.EmailIntegrations
                .Where(e => e.Provider == "IMAP" && e.Status == "active")
                .ToListAsync(stoppingToken);

            foreach (var integration in activeIntegrations)
            {
                try
                {
                    await PollSingleAccountAsync(integration, dbContext, analyzerService, aesKey, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to poll IMAP Integration ID: {IntegrationId}", integration.Id);
                }
            }
        }

        private async Task PollSingleAccountAsync(
            Models.EmailIntegration integration,
            AppDbContext dbContext,
            IEmailAnalyzerService analyzerService,
            string aesKey,
            CancellationToken stoppingToken)
        {
            // 1. Decrypt the AES-256 credential payload
            ImapCredentialPayload credentials;
            try
            {
                string decryptedJson = AesEncryptionHelper.Decrypt(integration.AccessTokenEnc, aesKey);
                credentials = JsonSerializer.Deserialize<ImapCredentialPayload>(decryptedJson)!;

                if (string.IsNullOrEmpty(credentials.email) || string.IsNullOrEmpty(credentials.password))
                {
                    _logger.LogWarning("Decrypted IMAP credentials are empty for Integration {Id}. Skipping.", integration.Id);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt IMAP credentials for Integration {Id}. Marking inactive.", integration.Id);
                integration.Status = "inactive";
                await dbContext.SaveChangesAsync(stoppingToken);
                return;
            }

            // 2. Connect via MailKit
            using var client = new ImapClient();
            try
            {
                client.Timeout = 30_000; // 30 seconds timeout for background ops
                await client.ConnectAsync(credentials.host, credentials.port, useSsl: true, stoppingToken);
                await client.AuthenticateAsync(credentials.email, credentials.password, stoppingToken);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _logger.LogWarning("IMAP auth failed for Integration {Id} ({Email}): {Message}",
                    integration.Id, credentials.email, ex.Message);
                integration.Status = "inactive";
                await dbContext.SaveChangesAsync(stoppingToken);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IMAP connection failed for Integration {Id} ({Host}:{Port})",
                    integration.Id, credentials.host, credentials.port);
                // Don't mark inactive for transient connection errors
                return;
            }

            try
            {
                // 3. Open Inbox and search for unseen messages
                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly, stoppingToken);

                var unseenUids = await inbox.SearchAsync(SearchQuery.NotSeen, stoppingToken);

                // Process up to 10 messages per cycle to avoid long-running connections
                var uidsToProcess = unseenUids.Take(10).ToList();

                foreach (var uid in uidsToProcess)
                {
                    // Check if we already scanned this message (using UID as ProviderMessageId)
                    string providerMessageId = $"IMAP:{credentials.host}:{uid}";
                    bool alreadyScanned = await dbContext.EmailMessages
                        .AnyAsync(m => m.ProviderMessageId == providerMessageId && m.IntegrationId == integration.Id, stoppingToken);

                    if (alreadyScanned) continue;

                    // 4. Fetch the full message
                    var mimeMessage = await inbox.GetMessageAsync(uid, stoppingToken);
                    if (mimeMessage == null) continue;

                    // 5. Extract email components
                    string fromHeader = mimeMessage.From?.ToString() ?? "";
                    string senderEmail = ExtractEmailAddress(fromHeader);
                    string subject = mimeMessage.Subject ?? "";

                    if (string.IsNullOrEmpty(senderEmail)) continue;

                    // Extract Authentication-Results header
                    string authResultsHeader = "";
                    var authHeader = mimeMessage.Headers.FirstOrDefault(
                        h => h.Field.Equals("Authentication-Results", StringComparison.OrdinalIgnoreCase));
                    if (authHeader != null)
                    {
                        authResultsHeader = authHeader.Value ?? "";
                    }

                    // Extract body content
                    string plainTextBody = mimeMessage.TextBody ?? "";
                    string htmlBody = mimeMessage.HtmlBody ?? "";

                    // Extract attachments
                    var attachmentNames = new List<string>();
                    var attachmentPayloads = new List<DTOs.AttachmentPayload>();

                    foreach (var attachment in mimeMessage.Attachments)
                    {
                        string fileName = attachment.ContentDisposition?.FileName
                            ?? (attachment as MimePart)?.FileName
                            ?? "unknown_attachment";

                        attachmentNames.Add(fileName);

                        // Download attachment binary data for YARA scanning (with 10MB OOM guard)
                        if (attachment is MimePart mimePart)
                        {
                            try
                            {
                                using var memoryStream = new MemoryStream();
                                mimePart.Content.DecodeTo(memoryStream);
                                byte[] rawBytes = memoryStream.ToArray();

                                if (rawBytes.Length > MAX_ATTACHMENT_SIZE_BYTES)
                                {
                                    _logger.LogWarning(
                                        "Skipping YARA download for '{Filename}' ({SizeMB:F1} MB) — exceeds 10 MB limit.",
                                        fileName, rawBytes.Length / (1024.0 * 1024.0));
                                }
                                else
                                {
                                    attachmentPayloads.Add(new DTOs.AttachmentPayload
                                    {
                                        FileName = fileName,
                                        Data = rawBytes,
                                        SizeBytes = rawBytes.Length
                                    });
                                }
                            }
                            catch (Exception attEx)
                            {
                                _logger.LogWarning(attEx,
                                    "Failed to extract attachment '{Filename}' for YARA scanning. Skipping.", fileName);
                            }
                        }
                    }

                    // 6. Feed to OSINT Engine (8-Layer analysis — identical to Gmail pipeline)
                    var analysisResult = await analyzerService.AnalyzeEmailAsync(
                        email: senderEmail,
                        subject: subject,
                        plainTextBody: plainTextBody,
                        htmlBody: htmlBody,
                        rawHeaders: authResultsHeader,
                        attachmentNames: attachmentNames,
                        attachmentPayloads: attachmentPayloads
                    );

                    // 7. Save the Message Record
                    var newMessage = new Models.EmailMessage
                    {
                        IntegrationId = integration.Id,
                        ProviderMessageId = providerMessageId,
                        FromEmail = senderEmail,
                        FromName = fromHeader,
                        Subject = subject.Length > 500 ? subject[..500] : subject,
                        ReceivedAt = mimeMessage.Date.UtcDateTime != default
                            ? mimeMessage.Date.UtcDateTime
                            : DateTime.UtcNow,
                    };
                    dbContext.EmailMessages.Add(newMessage);
                    await dbContext.SaveChangesAsync(stoppingToken); // Save to generate newMessage.Id

                    // 8. Save the Scan Result
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
                        PhishingKeywordsJson = JsonSerializer.Serialize(analysisResult.PhishingKeywordsFound),
                        MaliciousUrlsJson = JsonSerializer.Serialize(analysisResult.MaliciousUrls),
                        ReasonsJson = JsonSerializer.Serialize(analysisResult.Reasons),
                        RecommendationsJson = JsonSerializer.Serialize(analysisResult.Recommendations),
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Scans.Add(newScan);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    // 9. FCM Push Notification trigger (mirrors GmailPollingService logic)
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
                            _logger.LogInformation(
                                "FCM Multicast completed for User {UserId} (IMAP). Success: {SuccessCount}, Failures: {FailureCount}",
                                integration.UserId, fcmResponse.SuccessCount, fcmResponse.FailureCount);
                        }
                    }

                    _logger.LogInformation(
                        "IMAP: Scanned message UID {Uid} from {Sender}. Score: {Score}",
                        uid, senderEmail, analysisResult.TrustScore);
                }
            }
            finally
            {
                // Always disconnect cleanly
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true, stoppingToken);
                }
            }
        }

        /// <summary>
        /// Extracts a clean email address from a From header string.
        /// e.g., "John Doe <john@example.com>" → "john@example.com"
        /// </summary>
        private string ExtractEmailAddress(string fromHeader)
        {
            int startIndex = fromHeader.IndexOf('<');
            int endIndex = fromHeader.IndexOf('>');

            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
            {
                return fromHeader.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
            }

            // Fallback: if no angle brackets, try to extract email-like string
            if (fromHeader.Contains('@'))
            {
                return fromHeader.Trim().Trim('"');
            }

            return fromHeader.Trim();
        }

        /// <summary>
        /// Internal DTO for deserializing the AES-encrypted IMAP credential payload.
        /// Matches the JSON shape created in IntegrationController.ConnectImap().
        /// </summary>
        private class ImapCredentialPayload
        {
            public string host { get; set; } = string.Empty;
            public int port { get; set; } = 993;
            public string email { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
        }
    }
}
