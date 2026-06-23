using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMailBackend.Data;
using SecureMailBackend.Models;
using SecureMailBackend.DTOs;
using SecureMailBackend.Services;
using SecureMailBackend.Services.Helpers;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using System.Text.Json;

namespace SecureMailBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IntegrationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IImapValidatorService _imapValidator;
        
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private readonly string _frontendDashboardUrl;
        private readonly ILogger<IntegrationController> _logger;

        public IntegrationController(AppDbContext context, IConfiguration configuration, ILogger<IntegrationController> logger, IImapValidatorService imapValidator)
        {
            _context = context;
            _configuration = configuration;
            _imapValidator = imapValidator;
            _clientId = _configuration["GoogleOAuth:ClientId"];
            _clientSecret = _configuration["GoogleOAuth:ClientSecret"];
            _redirectUri = _configuration["GoogleOAuth:RedirectUri"];
            _frontendDashboardUrl = _configuration["FrontendUrl"] ?? "http://127.0.0.1:5500/dashboard.html";
            _logger = logger;
        }

        private async Task<int> GetUserIdFromTokenAsync()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer ")) return 0;
            
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.SessionToken == token && s.ExpiresAt > DateTime.UtcNow);
            return session?.UserId ?? 0;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetIntegrationStatus()
        {
            int userId = await GetUserIdFromTokenAsync();
            if (userId == 0) return Unauthorized(new { message = "Unauthorized session" });

            var integrations = await _context.EmailIntegrations
                .Where(e => e.UserId == userId && e.Status == "active")
                .Select(e => new { e.Id, e.Provider, e.ProviderAccountEmail, e.CreatedAt })
                .ToListAsync();

            return Ok(new { integrations, count = integrations.Count });
        }

        [HttpDelete("unlink/{integrationId}")]
        public async Task<IActionResult> UnlinkIntegration(int integrationId)
        {
            int userId = await GetUserIdFromTokenAsync();
            if (userId == 0) return Unauthorized(new { message = "Unauthorized session" });

            var activeIntegration = await _context.EmailIntegrations
                .FirstOrDefaultAsync(e => e.Id == integrationId && e.UserId == userId && e.Status == "active");

            if (activeIntegration == null) return BadRequest(new { message = "لا يوجد حساب متصل للإلغاء" });

            // Soft delete to preserve historical records and avoid foreign key constraint errors
            activeIntegration.AccessTokenEnc = "";
            activeIntegration.RefreshTokenEnc = "";
            activeIntegration.Status = "unlinked";
            activeIntegration.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إلغاء الربط بنجاح" });
        }

        // ============================================================
        //  PHASE 2: IMAP Universal Connector
        // ============================================================

        [HttpPost("imap/connect")]
        public async Task<IActionResult> ConnectImap([FromBody] ImapConnectionDto dto)
        {
            int userId = await GetUserIdFromTokenAsync();
            if (userId == 0) return Unauthorized(new { message = "Unauthorized session" });

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "البريد الإلكتروني وكلمة المرور مطلوبان." });

            var validation = await _imapValidator.ValidateAsync(
                dto.Email, dto.Password, dto.ImapHost, dto.ImapPort);

            if (!validation.IsValid)
            {
                return BadRequest(new { message = validation.ErrorMessage });
            }

            var existing = await _context.EmailIntegrations
                .FirstOrDefaultAsync(e => e.ProviderAccountEmail == dto.Email && e.Provider == "IMAP");

            if (existing != null && existing.UserId != userId)
            {
                return BadRequest(new { message = "هذا البريد الإلكتروني مرتبط بحساب مستخدم آخر." });
            }

            string aesKey = _configuration["Encryption:AesKey"]!;
            var credentialPayload = JsonSerializer.Serialize(new
            {
                host = validation.DetectedHost,
                port = validation.DetectedPort,
                email = dto.Email,
                password = dto.Password
            });
            string encryptedPayload = AesEncryptionHelper.Encrypt(credentialPayload, aesKey);

            if (existing != null && existing.UserId == userId)
            {
                if (existing.Status == "active")
                {
                    return BadRequest(new { message = "هذا البريد الإلكتروني مرتبط ونشط بالفعل." });
                }

                existing.AccessTokenEnc = encryptedPayload;
                existing.RefreshTokenEnc = null;
                existing.Status = "active";
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var integration = new EmailIntegration
                {
                    UserId = userId,
                    Provider = "IMAP",
                    ProviderAccountEmail = dto.Email,
                    AccessTokenEnc = encryptedPayload,
                    RefreshTokenEnc = null,
                    ExpiresAt = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = "active"
                };
                _context.EmailIntegrations.Add(integration);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "IMAP integration created/updated for user {UserId} with email {Email} via {Host}:{Port}",
                userId, dto.Email, validation.DetectedHost, validation.DetectedPort);

            return Ok(new
            {
                message = "تم ربط الحساب بنجاح عبر IMAP!",
                email = dto.Email,
                host = validation.DetectedHost,
                port = validation.DetectedPort
            });
        }

        [HttpGet("google/login")]
        public async Task<IActionResult> GoogleLogin([FromQuery] string sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken)) return Unauthorized("No token provided");
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.ExpiresAt > DateTime.UtcNow);
            if (session == null) return Unauthorized("Invalid or expired session");
            int userId = session.UserId;

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                },
                Scopes = new[] { "https://www.googleapis.com/auth/gmail.readonly", "email" },
                DataStore = new FileDataStore("Store") 
            });

            var uri = flow.CreateAuthorizationCodeRequest(_redirectUri).Build();
            
            var state = $"userId={userId}"; 
            
            var finalUri = $"{uri}&state={state}&prompt=consent%20select_account";

            return Redirect(finalUri);
        }

        [HttpGet("/api/google/callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(code))
                return Redirect($"{_frontendDashboardUrl}?integrationError=no_code");

            int userId = ParseUserIdFromState(state); // Implement a secure state parsing
            if (userId <= 0) return Redirect($"{_frontendDashboardUrl}?integrationError=invalid_state");

            try
            {
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _clientId,
                        ClientSecret = _clientSecret
                    }
                });

                var token = await flow.ExchangeCodeForTokenAsync(userId.ToString(), code, _redirectUri, CancellationToken.None);

                string googleEmail = "";
                try
                {
                    var credential = GoogleCredential.FromAccessToken(token.AccessToken);
                    var gmailService = new GmailService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "SecureMail Integration",
                    });
                    
                    var profile = await gmailService.Users.GetProfile("me").ExecuteAsync();
                    googleEmail = profile.EmailAddress;
                }
                catch (Google.GoogleApiException ex)
                {
                    _logger.LogError(ex, "Google API Error: {Message}. Status: {Status}", ex.Message, ex.HttpStatusCode);
                    return Redirect($"{_frontendDashboardUrl}?integrationError=invalid_token");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error fetching Google profile: {Message}", ex.Message);
                    return Redirect($"{_frontendDashboardUrl}?integrationError=invalid_token");
                }

                if (string.IsNullOrEmpty(googleEmail))
                {
                    return Redirect($"{_frontendDashboardUrl}?integrationError=email_not_found");
                }


                try
                {
                    var existingIntegration = await _context.EmailIntegrations
                        .FirstOrDefaultAsync(e => e.ProviderAccountEmail == googleEmail && e.Provider == "GMAIL");

                    if (existingIntegration != null && existingIntegration.UserId != userId)
                    {
                         return Redirect($"{_frontendDashboardUrl}?integrationError=already_linked");
                    }
                    
                    // Encrypt OAuth tokens with AES-256 before storing
                    string aesKey = _configuration["Encryption:AesKey"]!;
                    string encAccessToken = AesEncryptionHelper.Encrypt(token.AccessToken, aesKey);
                    string? encRefreshToken = !string.IsNullOrEmpty(token.RefreshToken)
                        ? AesEncryptionHelper.Encrypt(token.RefreshToken, aesKey)
                        : null;

                    if (existingIntegration != null && existingIntegration.UserId == userId)
                    {
                        existingIntegration.AccessTokenEnc = encAccessToken;
                        existingIntegration.RefreshTokenEnc = encRefreshToken ?? existingIntegration.RefreshTokenEnc;
                        existingIntegration.UpdatedAt = DateTime.UtcNow;
                        existingIntegration.ExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds ?? 3600);
                    }
                    else
                    {
                        var integration = new EmailIntegration
                        {
                            UserId = userId,
                            Provider = "GMAIL",
                            ProviderAccountEmail = googleEmail,
                            AccessTokenEnc = encAccessToken,
                            RefreshTokenEnc = encRefreshToken,
                            ExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds ?? 3600),
                            CreatedAt = DateTime.UtcNow,
                            Status = "active"
                        };
                        _context.EmailIntegrations.Add(integration);
                    }

                    await _context.SaveChangesAsync();

                    return Redirect($"{_frontendDashboardUrl}?integrationSuccess=true");
                }
                catch (DbUpdateException dbEx)
                {
                     _logger.LogError(dbEx, "Database Save Error details: {Message}. Inner Exception: {InnerMessage}", dbEx.Message, dbEx.InnerException?.Message);
                     return Redirect($"{_frontendDashboardUrl}?integrationError=db_save_failed");
                }
            }
            catch (Exception ex)
            {
                 return Redirect($"{_frontendDashboardUrl}?integrationError=server_error");
            }
        }

        private int ParseUserIdFromState(string state)
        {
             if(state.StartsWith("userId=") && int.TryParse(state.Substring(7), out int id))
                return id;
             return 0;
        }
    }
}
