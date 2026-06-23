namespace SecureMailBackend.DTOs
{
    public class EmailAnalysisResultDto
    {
        public string Email { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public bool IsValidFormat { get; set; }
        public string Provider { get; set; } = string.Empty; 
        public string Country { get; set; } = string.Empty; 
        public bool IsDisposable { get; set; }
        public bool IsFreeProvider { get; set; }
        public int TrustScore { get; set; } 
        public string RiskLevel { get; set; } = string.Empty; 
        
        // DNS Records
        public bool HasMxRecord { get; set; }
        public bool HasSpfRecord { get; set; }
        public bool HasDmarcRecord { get; set; }
        
        // Layer 2: URLs
        public int MaliciousLinksCount { get; set; }
        public List<string> ExtractedUrls { get; set; } = new();

        // Layer 3: Heuristics
        public int PhishingKeywordsCount { get; set; }

        // Layer 4: Attachments
        public bool HasSuspiciousAttachments { get; set; }
        public bool HasExecutableAttachments { get; set; }

        // Breach Data
        public int BreachCount { get; set; }
        
        // Error info (e.g. Rate Limits)
        public bool VirusTotalSkippedDueToQuota { get; set; }

        // --- NEW FRONTEND THREAT HIGHILIGHTING PROPERTIES ---
        public string PlainTextBody { get; set; } = string.Empty;
        public List<string> PhishingKeywordsFound { get; set; } = new();
        public List<string> MaliciousUrls { get; set; } = new();
        public List<string> AttachmentNames { get; set; } = new();

        // --- Layer 1.5: Typosquatting ---
        public bool IsTyposquatSuspect { get; set; }
        public string? TyposquatMatchedDomain { get; set; }
        public int TyposquatDistance { get; set; }

        // --- Layer 2.5: Entropy ---
        public List<string> HighEntropyUrls { get; set; } = new();
        public List<string> HighEntropyAttachments { get; set; } = new();

        // --- Layer 4.5: YARA ---
        public bool YaraMatched { get; set; }
        public List<string> YaraMatchedRules { get; set; } = new();

        // UI Arrays
        public List<string> Reasons { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}
