using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMailBackend.Data;
using SecureMailBackend.Models;

namespace SecureMailBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<int> GetUserIdFromTokenAsync()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer ")) return 0;
            
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.SessionToken == token && s.ExpiresAt > DateTime.UtcNow);
            return session?.UserId ?? 0;
        }

        [HttpGet("scans")]
        public async Task<IActionResult> GetRecentScans([FromQuery] int? integrationId = null)
        {
            int userId = await GetUserIdFromTokenAsync();
            if (userId == 0) return Unauthorized("Invalid or expired session");

            var scans = await _context.Scans
                .Include(s => s.EmailMessage)
                .Include(s => s.AddressCheck)
                .Where(s => s.UserId == userId &&
                    (integrationId == null ||
                     (s.EmailMessage != null && s.EmailMessage.IntegrationId == integrationId)))
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .Select(s => new
                {
                    s.Id,
                    Date = s.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Email = s.EmailMessage != null ? s.EmailMessage.FromEmail : 
                            (s.AddressCheck != null ? s.AddressCheck.EmailAddress : "فحص يدوي (بدون إيميل مسجل)"),
                    Score = s.RiskScore,
                    Verdict = s.Verdict,
                    Type = s.ScanType
                })
                .ToListAsync();

            return Ok(scans);
        }

        [HttpGet("scan/{id}")]
        public async Task<IActionResult> GetScanDetails(int id)
        {
            int userId = await GetUserIdFromTokenAsync();
            if (userId == 0) return Unauthorized("Invalid or expired session");

            var scan = await _context.Scans
                .Include(s => s.EmailMessage)
                    .ThenInclude(e => e.Urls)
                .Include(s => s.EmailMessage)
                    .ThenInclude(e => e.Attachments)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (scan == null) return NotFound();

            var keywordsList = string.IsNullOrEmpty(scan.PhishingKeywordsJson) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(scan.PhishingKeywordsJson) ?? new();
            var urlsList = string.IsNullOrEmpty(scan.MaliciousUrlsJson) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(scan.MaliciousUrlsJson) ?? new();
            var attachments = scan.EmailMessage?.Attachments?.Select(a => a.FileName ?? "").ToList() ?? new List<string>();

            var dto = new SecureMailBackend.DTOs.EmailAnalysisResultDto
            {
                Email = scan.EmailMessage?.FromEmail ?? "فحص أرشيف",
                TrustScore = scan.RiskScore,
                RiskLevel = scan.Verdict,
                Provider = scan.Provider ?? "مجهول",
                Country = scan.DomainCountry ?? "غير محدد",
                HasSpfRecord = scan.SpfStatus ?? false,
                HasDmarcRecord = scan.DmarcStatus ?? false,
                Reasons = string.IsNullOrEmpty(scan.ReasonsJson) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(scan.ReasonsJson) ?? new(),
                Recommendations = string.IsNullOrEmpty(scan.RecommendationsJson) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(scan.RecommendationsJson) ?? new(),
                PlainTextBody = scan.PlainTextBody ?? "",
                PhishingKeywordsFound = keywordsList,
                PhishingKeywordsCount = keywordsList.Count,
                MaliciousUrls = urlsList,
                MaliciousLinksCount = urlsList.Count,
                AttachmentNames = attachments,
                HasExecutableAttachments = attachments.Any(a => a.EndsWith(".exe") || a.EndsWith(".bat") || a.EndsWith(".cmd") || a.EndsWith(".vbs") || a.EndsWith(".scr") || a.EndsWith(".js")),
                HasSuspiciousAttachments = attachments.Any(a => a.EndsWith(".zip") || a.EndsWith(".rar") || a.EndsWith(".pdf") || a.EndsWith(".docm") || a.EndsWith(".xlsm")),
                ExtractedUrls = scan.EmailMessage?.Urls?.Select(u => u.Url ?? "").ToList() ?? new List<string>()
            };

            // --- Populate advanced algorithm properties by parsing reasons ---
            var reasons = dto.Reasons;

            // Typosquatting Detection: parse from reasons containing "Typosquatting"
            var typosquatReason = reasons.FirstOrDefault(r => r.Contains("Typosquatting"));
            if (typosquatReason != null)
            {
                dto.IsTyposquatSuspect = true;
                // Extract matched domain from pattern: "مشابه جداً لـ 'paypal.com'"
                var matchDomain = System.Text.RegularExpressions.Regex.Match(typosquatReason, @"لـ '([^']+)'");
                if (matchDomain.Success) dto.TyposquatMatchedDomain = matchDomain.Groups[1].Value;
                // Extract distance from pattern: "(فرق 1 حرف)"
                var matchDist = System.Text.RegularExpressions.Regex.Match(typosquatReason, @"فرق (\d+) حرف");
                if (matchDist.Success) dto.TyposquatDistance = int.Parse(matchDist.Groups[1].Value);
            }

            // Shannon Entropy: parse URLs and attachments flagged for high entropy
            dto.HighEntropyUrls = reasons
                .Where(r => r.Contains("Entropy") && r.Contains("روابط"))
                .Select(r => {
                    var m = System.Text.RegularExpressions.Regex.Match(r, @"الرابط '([^']+)'");
                    return m.Success ? m.Groups[1].Value : "";
                })
                .Where(u => !string.IsNullOrEmpty(u))
                .ToList();

            dto.HighEntropyAttachments = reasons
                .Where(r => r.Contains("Entropy") && r.Contains("مرفق"))
                .Select(r => {
                    var m = System.Text.RegularExpressions.Regex.Match(r, @"الملف '([^']+)'");
                    return m.Success ? m.Groups[1].Value : "";
                })
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();

            // YARA Forensics: parse matched rules
            var yaraReasons = reasons.Where(r => r.Contains("YARA")).ToList();
            if (yaraReasons.Any())
            {
                dto.YaraMatched = true;
                dto.YaraMatchedRules = yaraReasons
                    .SelectMany(r => {
                        var m = System.Text.RegularExpressions.Regex.Match(r, @"قواعد: ([^.]+)\.");
                        return m.Success ? m.Groups[1].Value.Split(", ").ToList() : new List<string>();
                    })
                    .Distinct()
                    .ToList();
            }

            return Ok(dto);
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            int userId = await GetUserIdFromTokenAsync();
            if (userId == 0) return Unauthorized("Invalid or expired session");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.SentAt)
                .Take(10)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Body,
                    n.Status,
                    Date = n.SentAt
                })
                .ToListAsync();

            return Ok(notifications);
        }
    }
}
