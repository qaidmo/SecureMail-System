using SecureMailBackend.DTOs;
using SecureMailBackend.Services.Helpers;
using DnsClient;
using System.Text.RegularExpressions;

namespace SecureMailBackend.Services
{
    public interface IEmailAnalyzerService
    {
        Task<EmailAnalysisResultDto> AnalyzeEmailAsync(string email, string subject = "", string plainTextBody = "", string htmlBody = "", string rawHeaders = "", List<string>? attachmentNames = null, List<AttachmentPayload>? attachmentPayloads = null);
    }

    public class EmailAnalyzerService : IEmailAnalyzerService
    {
        private readonly List<string> _disposableDomains = new() { "10minutemail.com", "tempmail.com", "guerrillamail.com", "yopmail.com", "mailinator.com" };
        private readonly List<string> _freeProviders = new() { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "icloud.com", "mail.ru" };
        
        private readonly Dictionary<string, string> _countryCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            { "sa", "المملكة العربية السعودية" }, { "ae", "الإمارات العربية المتحدة" }, { "eg", "مصر" },
            { "uk", "المملكة المتحدة" }, { "us", "الولايات المتحدة الأمريكية" }, { "ru", "روسيا" },
            { "cn", "الصين" }, { "fr", "فرنسا" }, { "de", "ألمانيا" }
        };

        private readonly List<string> _phishingKeywords = new()
        {
            // Arabic
            "حسابك معلق", "تحديث بياناتك", "اضغط هنا فورا", "كلمة المرور", "إشعار أمني عاجل", 
            "التحقق من هويتك", "تم إيقاف حسابك", "عاجل", "سري", "فاتورة مرفقة",
            // English
            "urgent action required", "account suspended", "verify your identity", "reset password", 
            "invoice attached", "unauthorized login", "click here", "validate your account",
            "security alert", "update your billing"
        };

        private readonly List<string> _blacklistedAttachments = new() { ".exe", ".bat", ".cmd", ".scr", ".vbs", ".js", ".ps1", ".msi" };
        private readonly List<string> _suspiciousAttachments = new() { ".docm", ".xlsm", ".zip", ".rar", ".pdf" };

        private readonly ILookupClient _dnsClient;
        private readonly IVirusTotalService _virusTotalService;
        private readonly IYaraScannerService _yaraScannerService;
        private readonly ILogger<EmailAnalyzerService> _logger;

        public EmailAnalyzerService(IVirusTotalService virusTotalService, IYaraScannerService yaraScannerService, ILogger<EmailAnalyzerService> logger)
        {
            _dnsClient = new LookupClient();
            _virusTotalService = virusTotalService;
            _yaraScannerService = yaraScannerService;
            _logger = logger;
        }

        public async Task<EmailAnalysisResultDto> AnalyzeEmailAsync(string email, string subject = "", string plainTextBody = "", string htmlBody = "", string rawHeaders = "", List<string>? attachmentNames = null, List<AttachmentPayload>? attachmentPayloads = null)
        {
            var result = new EmailAnalysisResultDto
            {
                Email = email ?? "",
                TrustScore = 100, 
                RiskLevel = "منخفض (Low)",
                Country = "عالمي (Global)",
                Provider = "مجهول",
                PlainTextBody = plainTextBody ?? "", // Inject body for UI Snippet
                AttachmentNames = attachmentNames ?? new List<string>() // Pass names for UI
            };

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                result.IsValidFormat = false; result.TrustScore = 0; result.RiskLevel = "عالي جداً (Critical)";
                result.Provider = "صيغة غير صحيحة";
                result.Reasons.Add("صيغة البريد الإلكتروني غير صحيحة ولم يتم التعرف عليها.");
                return result;
            }

            result.IsValidFormat = true;
            var domain = email.Split('@')[1].ToLower().Trim();
            result.Domain = domain;

            // General Provider Checks
            if (_freeProviders.Contains(domain)) { result.Provider = "بريد مجاني"; result.IsFreeProvider = true; result.TrustScore -= 20; }
            else if (_disposableDomains.Contains(domain)) { result.Provider = "بريد مؤقت"; result.IsDisposable = true; result.TrustScore -= 70; }
            else { result.Provider = "بريد مؤسسي"; result.TrustScore += 10; }

            var tld = domain.Split('.').Last();
            if (_countryCodes.ContainsKey(tld)) { result.Country = _countryCodes[tld]; }

            // --- Layer 1: Spoofing & Auth Validations ---
            await PerformDnsChecksAsync(domain, result);
            AnalyzeAuthHeaders(rawHeaders, result);

            // --- Layer 1.5: Typosquatting Detection (Levenshtein Distance) ---
            AnalyzeTyposquatting(domain, result);

            // --- Layer 2: URLs & VirusTotal ---
            await AnalyzeUrlsAsync(plainTextBody, htmlBody, result);

            // --- Layer 2.5: Entropy Analysis (Shannon Entropy) ---
            AnalyzeEntropy(result.ExtractedUrls, attachmentNames, result);

            // --- Layer 3: Phishing Keywords (NLP) ---
            AnalyzeHeuristics(subject, plainTextBody, htmlBody, result);

            // --- Layer 4: Attachment Metadata ---
            AnalyzeAttachments(attachmentNames, result);

            // --- Layer 4.5: YARA Attachment Forensics ---
            await AnalyzeYaraAsync(attachmentPayloads, result);

            // --- Layer 5: Trust Score Engine & Final Verdict ---
            FinalizeScore(result);

            return result;
        }

        private async Task PerformDnsChecksAsync(string domain, EmailAnalysisResultDto result)
        {
            try
            {
                var mxResponse = await _dnsClient.QueryAsync(domain, QueryType.MX);
                result.HasMxRecord = mxResponse.Answers.MxRecords().Any();
                if (!result.HasMxRecord) { result.TrustScore -= 50; result.Reasons.Add("لا توجد سجلات MX صالحة للنطاق."); }
                
                var txtResponse = await _dnsClient.QueryAsync(domain, QueryType.TXT);
                var textRecords = txtResponse.Answers.TxtRecords().Select(x => string.Join("", x.Text)).ToList();
                result.HasSpfRecord = textRecords.Any(r => r.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase));
                
                var dmarcResponse = await _dnsClient.QueryAsync($"_dmarc.{domain}", QueryType.TXT);
                var dmarcRecords = dmarcResponse.Answers.TxtRecords().Select(x => string.Join("", x.Text)).ToList();
                result.HasDmarcRecord = dmarcRecords.Any(r => r.StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase));
            }
            catch { result.TrustScore -= 30; result.Reasons.Add("فشل فحص DNS للنطاق."); }
        }

        private void AnalyzeAuthHeaders(string rawHeaders, EmailAnalysisResultDto result)
        {
            if (string.IsNullOrWhiteSpace(rawHeaders)) return;

            // Look for Authentication-Results header
            if (rawHeaders.Contains("dkim=fail", StringComparison.OrdinalIgnoreCase) || 
                rawHeaders.Contains("dkim=neutral", StringComparison.OrdinalIgnoreCase))
            {
                result.TrustScore -= 40;
                result.Reasons.Add("فشل في المصادقة (DKIM Failed) - هوية المرسل غير مشفرة أو مزيفة.");
            }
            if (rawHeaders.Contains("spf=fail", StringComparison.OrdinalIgnoreCase) || 
                rawHeaders.Contains("spf=softfail", StringComparison.OrdinalIgnoreCase))
            {
                result.TrustScore -= 40;
                result.Reasons.Add("فشل في المصادقة (SPF Failed) - الخادم المرسل غير مصرح له بالنشر باسم هذا النطاق.");
            }
        }

        private async Task AnalyzeUrlsAsync(string plainText, string html, EmailAnalysisResultDto result)
        {
            if (_virusTotalService == null) return;

            string content = html + " " + plainText;
            var urlRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)", RegexOptions.IgnoreCase);
            var matches = urlRegex.Matches(content);
            
            var uniqueUrls = matches.Select(m => m.Value).Distinct().ToList();
            
            // Filter basic known safe domains to save quotas
            var safeDomains = new[] { "google.com", "microsoft.com", "apple.com", "fb.com", "linkedin.com", "twitter.com", "instagram.com" };
            var urlsToCheck = uniqueUrls.Where(u => !safeDomains.Any(sd => u.Contains(sd, StringComparison.OrdinalIgnoreCase))).Take(3).ToList(); // Max 3 per email to avoid hitting limit too fast

            result.ExtractedUrls = uniqueUrls;

            try
            {
                foreach (var url in urlsToCheck)
                {
                    bool isMalicious = await _virusTotalService.IsUrlMaliciousAsync(url);
                    if (isMalicious)
                    {
                        result.MaliciousLinksCount++;
                        result.MaliciousUrls.Add(url); // Track specific bad URL for UI highlighting
                        result.TrustScore -= 100; // Immediate fail
                        result.Reasons.Add($"روابط خبيثة - تم اكتشاف رابط مشبوه أو ضار: {url}");
                    }
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                result.VirusTotalSkippedDueToQuota = true;
                result.Reasons.Add("تخطي فحص الروابط (VirusTotal) بسبب استنفاد الحد اليومي للطلبات. ينصح بالحذر.");
            }
            catch (Exception)
            {
                result.Reasons.Add("فشل فحص الروابط أمنياً.");
            }
        }

        private void AnalyzeHeuristics(string subject, string plainText, string html, EmailAnalysisResultDto result)
        {
            string content = (subject + " " + plainText + " " + html).ToLower();
            int hitCount = 0;

            foreach (var keyword in _phishingKeywords)
            {
                if (content.Contains(keyword.ToLower()))
                {
                    hitCount++;
                    result.PhishingKeywordsFound.Add(keyword); // Track specific keyword for NLP highlighting
                }
            }

            result.PhishingKeywordsCount = hitCount;
            if (hitCount > 0)
            {
                int penalty = Math.Min(hitCount * 5, 25);
                result.TrustScore -= penalty;
                result.Reasons.Add($"أسلوب احتيالي محتمل - تم رصد {hitCount} كلمات مفتاحية للتصيد والهندسة الاجتماعية.");
            }
        }

        private void AnalyzeAttachments(List<string>? attachmentNames, EmailAnalysisResultDto result)
        {
            if (attachmentNames == null || !attachmentNames.Any()) return;

            foreach (var name in attachmentNames)
            {
                string ext = Path.GetExtension(name).ToLower();
                if (_blacklistedAttachments.Contains(ext))
                {
                    result.HasExecutableAttachments = true;
                    result.TrustScore -= 60;
                    result.Reasons.Add($"مرفقات خبيثة جداً - يحتوي البريد على ملف تنفيذي خطير ({ext}).");
                }
                else if (_suspiciousAttachments.Contains(ext))
                {
                    result.HasSuspiciousAttachments = true;
                    result.TrustScore -= 20;
                    result.Reasons.Add($"مرفقات مشبوهة - يحتوي البريد على ملف مضغوط أو مستند قد يحتوي على ماكرو ({ext}).");
                }
            }
        }

        private void FinalizeScore(EmailAnalysisResultDto result)
        {
            if (result.TrustScore > 100) result.TrustScore = 100;
            if (result.TrustScore < 0) result.TrustScore = 0;

            if (result.TrustScore >= 80)
            {
                result.RiskLevel = "آمن (Safe)";
                result.Recommendations.Add("يبدو البريد آمناً وموثوقاً.");
            }
            else if (result.TrustScore >= 50)
            {
                result.RiskLevel = "متوسط المشبوهية (Suspicious)";
                result.Recommendations.Add("تصنيف الخطر متوسط. تجنب فتح الروابط أو المرفقات إن لم تكن تتوقعها.");
            }
            else
            {
                result.RiskLevel = "خطير (High Risk)";
                result.Recommendations.Add("خطر أمني! البريد مصنف كعالي الخطورة، لا تتفاعل معه أبداً.");
            }
        }

        // ============================================================
        //  PHASE 1 ADVANCED ALGORITHMS
        // ============================================================

        /// <summary>
        /// Layer 1.5: Typosquatting Detection via Levenshtein Distance.
        /// Compares sender domain against a whitelist of high-value targets.
        /// </summary>
        private void AnalyzeTyposquatting(string domain, EmailAnalysisResultDto result)
        {
            try
            {
                var match = TyposquatDetector.DetectTyposquat(domain);
                if (match != null)
                {
                    result.IsTyposquatSuspect = true;
                    result.TyposquatMatchedDomain = match.Value.matchedDomain;
                    result.TyposquatDistance = match.Value.distance;

                    // Penalty scales with closeness: distance 1 = -60, 2 = -40, 3 = -20
                    int penalty = match.Value.distance switch
                    {
                        1 => 60,
                        2 => 40,
                        3 => 20,
                        _ => 10
                    };
                    result.TrustScore -= penalty;

                    result.Reasons.Add(
                        $"تحذير من انتحال النطاق (Typosquatting) - النطاق '{domain}' مشابه جداً لـ '{match.Value.matchedDomain}' (فرق {match.Value.distance} حرف). خصم -{penalty} نقطة.");
                    result.Recommendations.Add(
                        $"تحقق يدوياً من أن المرسل هو فعلاً '{match.Value.matchedDomain}' وليس نطاقاً مزيفاً.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Typosquatting analysis failed for domain {Domain}", domain);
            }
        }

        /// <summary>
        /// Layer 2.5: Shannon Entropy analysis for URL obfuscation and
        /// randomized attachment filenames.
        /// </summary>
        private void AnalyzeEntropy(List<string> extractedUrls, List<string>? attachmentNames, EmailAnalysisResultDto result)
        {
            try
            {
                // URL entropy check (max 3 penalties)
                if (extractedUrls != null)
                {
                    int urlPenaltyCount = 0;
                    foreach (var url in extractedUrls)
                    {
                        if (urlPenaltyCount >= 3) break;

                        double entropy = EntropyCalculator.CalculateUrlEntropy(url);
                        if (entropy >= 5.0)
                        {
                            urlPenaltyCount++;
                            result.HighEntropyUrls.Add(url);
                            result.TrustScore -= 15;
                            result.Reasons.Add(
                                $"روابط مشبوهة (Entropy عالي) - الرابط '{TruncateUrl(url)}' يحتوي على نص عشوائي/مشفر مشبوه (Entropy: {entropy:F1} bits). خصم -15 نقطة.");
                        }
                    }
                }

                // Attachment filename entropy check
                if (attachmentNames != null)
                {
                    foreach (var name in attachmentNames)
                    {
                        double entropy = EntropyCalculator.Calculate(
                            Path.GetFileNameWithoutExtension(name));
                        if (EntropyCalculator.IsFilenameHighEntropy(name))
                        {
                            result.HighEntropyAttachments.Add(name);
                            result.TrustScore -= 25;
                            result.Reasons.Add(
                                $"اسم مرفق مشبوه (Entropy عالي) - الملف '{name}' يحمل اسم عشوائي يشير إلى محاولة تمويه (Entropy: {entropy:F1} bits). خصم -25 نقطة.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Entropy analysis failed");
            }
        }

        /// <summary>
        /// Layer 4.5: YARA rule scanning on raw attachment binary data.
        /// </summary>
        private async Task AnalyzeYaraAsync(List<AttachmentPayload>? attachmentPayloads, EmailAnalysisResultDto result)
        {
            if (attachmentPayloads == null || !attachmentPayloads.Any())
                return;

            try
            {
                foreach (var payload in attachmentPayloads)
                {
                    if (payload.Data == null || payload.Data.Length == 0)
                        continue;

                    var yaraResult = await _yaraScannerService.ScanAsync(payload.Data, payload.FileName);

                    if (yaraResult.HasMatches)
                    {
                        result.YaraMatched = true;
                        result.YaraMatchedRules.AddRange(yaraResult.MatchedRules);
                        result.TrustScore -= 100; // Immediate critical fail

                        string rulesStr = string.Join(", ", yaraResult.MatchedRules);
                        result.Reasons.Add(
                            $"كشف توقيعات برمجيات خبيثة (YARA) - الملف '{payload.FileName}' يتطابق مع قواعد: {rulesStr}. لا تفتح هذا المرفق أبداً! خصم -100 نقطة.");
                        result.Recommendations.Add(
                            $"المرفق '{payload.FileName}' يحتوي على أنماط برمجيات خبيثة معروفة. احذفه فوراً وأبلغ فريق الأمان.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "YARA analysis failed");
            }
        }

        /// <summary>
        /// Truncates a URL for display in reason strings (max 80 chars).
        /// </summary>
        private static string TruncateUrl(string url, int maxLen = 80)
        {
            return url.Length <= maxLen ? url : url[..maxLen] + "...";
        }
    }
}
