using MailKit;
using MailKit.Net.Imap;
using DnsClient;

namespace SecureMailBackend.Services
{
    public interface IImapValidatorService
    {
        /// <summary>
        /// Performs a dry-run connection to validate IMAP credentials.
        /// Connects, authenticates, opens inbox, then disconnects.
        /// </summary>
        Task<ImapValidationResult> ValidateAsync(string email, string password,
            string? host = null, int? port = null);
    }

    public class ImapValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string DetectedHost { get; set; } = string.Empty;
        public int DetectedPort { get; set; } = 993;
    }

    /// <summary>
    /// MailKit-based IMAP credential validator.
    /// Auto-detects IMAP host from email domain if not provided.
    /// </summary>
    public class ImapValidatorService : IImapValidatorService
    {
        private readonly ILogger<ImapValidatorService> _logger;
        private readonly ILookupClient _dnsClient;

        // Well-known IMAP hosts keyed by email domain
        private static readonly Dictionary<string, (string host, int port)> KnownImapHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            // Google
            { "gmail.com",       ("imap.gmail.com",           993) },
            { "googlemail.com",  ("imap.gmail.com",           993) },
            // Microsoft
            { "outlook.com",     ("imap-mail.outlook.com",    993) },
            { "hotmail.com",     ("imap-mail.outlook.com",    993) },
            { "live.com",        ("imap-mail.outlook.com",    993) },
            { "msn.com",         ("imap-mail.outlook.com",    993) },
            // Yahoo
            { "yahoo.com",       ("imap.mail.yahoo.com",      993) },
            { "ymail.com",       ("imap.mail.yahoo.com",      993) },
            // Apple
            { "icloud.com",      ("imap.mail.me.com",         993) },
            { "me.com",          ("imap.mail.me.com",         993) },
            { "mac.com",         ("imap.mail.me.com",         993) },
            // Zoho (fixes the zohomail.com bug)
            { "zoho.com",        ("imap.zoho.com",            993) },
            { "zohomail.com",    ("imap.zoho.com",            993) },
            // Other major providers
            { "aol.com",         ("imap.aol.com",             993) },
            { "mail.ru",         ("imap.mail.ru",             993) },
            { "yandex.com",      ("imap.yandex.com",          993) },
            { "fastmail.com",    ("imap.fastmail.com",        993) },
            { "gmx.com",         ("imap.gmx.com",             993) },
            { "gmx.net",         ("imap.gmx.net",             993) },
            { "protonmail.com",  ("127.0.0.1",                1143) }, // ProtonMail Bridge only
            { "pm.me",           ("127.0.0.1",                1143) }, // ProtonMail Bridge only
        };

        // MX record patterns → IMAP host mapping (for corporate/custom domains)
        private static readonly List<(string mxPattern, string imapHost, int imapPort)> MxToImapMap = new()
        {
            ( ".google.com",        "imap.gmail.com",          993 ),  // Google Workspace
            ( ".googlemail.com",    "imap.gmail.com",          993 ),
            ( ".outlook.com",       "imap-mail.outlook.com",   993 ),  // Microsoft 365
            ( ".protection.outlook.com", "imap-mail.outlook.com", 993 ),
            ( ".zoho.com",          "imap.zoho.com",           993 ),  // Zoho Workplace
            ( ".yahoodns.net",      "imap.mail.yahoo.com",     993 ),  // Yahoo Business
        };

        public ImapValidatorService(ILogger<ImapValidatorService> logger)
        {
            _logger = logger;
            _dnsClient = new LookupClient();
        }

        public async Task<ImapValidationResult> ValidateAsync(string email, string password,
            string? host = null, int? port = null)
        {
            var result = new ImapValidationResult();

            // --- Auto-detect host/port from email domain ---
            string domain = email.Contains('@') ? email.Split('@')[1].ToLower().Trim() : "";

            if (string.IsNullOrEmpty(host))
            {
                if (KnownImapHosts.TryGetValue(domain, out var known))
                {
                    // Step 1: Direct lookup from known hosts table
                    result.DetectedHost = known.host;
                    result.DetectedPort = known.port;
                }
                else
                {
                    // Step 2: DNS MX record fallback for unknown/corporate domains
                    var mxResolved = await TryResolveViaMxAsync(domain);
                    if (mxResolved != null)
                    {
                        result.DetectedHost = mxResolved.Value.host;
                        result.DetectedPort = mxResolved.Value.port;
                        _logger.LogInformation(
                            "IMAP host resolved via MX record for domain {Domain} → {Host}:{Port}",
                            domain, result.DetectedHost, result.DetectedPort);
                    }
                    else
                    {
                        // Step 3: Last resort fallback
                        result.DetectedHost = $"imap.{domain}";
                        result.DetectedPort = 993;
                        _logger.LogWarning(
                            "No known IMAP host or MX mapping for domain {Domain}. Falling back to {Host}:{Port}. " +
                            "If connection fails, the user should provide manual IMAP settings.",
                            domain, result.DetectedHost, result.DetectedPort);
                    }
                }
            }
            else
            {
                result.DetectedHost = host;
                result.DetectedPort = port ?? 993;
            }

            // --- Dry-run MailKit connection test ---
            try
            {
                using var client = new ImapClient();

                // Connect with SSL/TLS (timeout: 15 seconds)
                client.Timeout = 15_000;
                await client.ConnectAsync(result.DetectedHost, result.DetectedPort, useSsl: true);

                // Authenticate
                await client.AuthenticateAsync(email, password);

                // Verify inbox access
                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly);

                _logger.LogInformation(
                    "IMAP validation succeeded for {Email} via {Host}:{Port} — {Count} messages in inbox.",
                    email, result.DetectedHost, result.DetectedPort, inbox.Count);

                // Clean disconnect
                await client.DisconnectAsync(true);

                result.IsValid = true;
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _logger.LogWarning("IMAP auth failed for {Email}: {Message}", email, ex.Message);
                result.IsValid = false;
                result.ErrorMessage = "فشل المصادقة - تأكد من صحة البريد وكلمة المرور. لحسابات Gmail/Outlook مع المصادقة الثنائية، استخدم 'كلمة مرور التطبيقات'.";
            }
            catch (System.IO.IOException ex)
            {
                _logger.LogWarning("IMAP connection I/O error for {Email} at {Host}: {Message}",
                    email, result.DetectedHost, ex.Message);
                result.IsValid = false;
                result.ErrorMessage = $"تعذر الاتصال بخادم IMAP ({result.DetectedHost}:{result.DetectedPort}). تحقق من عنوان الخادم والمنفذ.";
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _logger.LogWarning("IMAP socket error for {Email} at {Host}: {Message}",
                    email, result.DetectedHost, ex.Message);
                result.IsValid = false;
                result.ErrorMessage = $"تعذر الاتصال بخادم IMAP ({result.DetectedHost}:{result.DetectedPort}). الخادم غير متاح أو المنفذ محظور.";
            }
            catch (TimeoutException)
            {
                result.IsValid = false;
                result.ErrorMessage = $"انتهت مهلة الاتصال بخادم IMAP ({result.DetectedHost}). تحقق من اتصال الإنترنت.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected IMAP validation error for {Email}", email);
                result.IsValid = false;
                result.ErrorMessage = $"خطأ غير متوقع أثناء الاتصال: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Attempts to resolve the IMAP host by querying DNS MX records for the domain
        /// and matching them against known email provider patterns.
        /// Returns null if no match is found.
        /// </summary>
        private async Task<(string host, int port)?> TryResolveViaMxAsync(string domain)
        {
            try
            {
                var mxResponse = await _dnsClient.QueryAsync(domain, QueryType.MX);
                var mxRecords = mxResponse.Answers.MxRecords().ToList();

                if (!mxRecords.Any())
                    return null;

                foreach (var mx in mxRecords.OrderBy(m => m.Preference))
                {
                    string mxHost = mx.Exchange.Value.TrimEnd('.').ToLower();

                    foreach (var mapping in MxToImapMap)
                    {
                        if (mxHost.EndsWith(mapping.mxPattern, StringComparison.OrdinalIgnoreCase))
                        {
                            return (mapping.imapHost, mapping.imapPort);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DNS MX lookup failed for domain {Domain}. Falling back to naive guess.", domain);
            }

            return null;
        }
    }
}
