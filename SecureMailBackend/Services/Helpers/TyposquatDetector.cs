namespace SecureMailBackend.Services.Helpers
{
    /// <summary>
    /// Detects typosquatting by comparing sender domains against a whitelist
    /// of high-value targets using Levenshtein Distance (Fastenshtein).
    /// </summary>
    public static class TyposquatDetector
    {
        // High-value targets that attackers commonly impersonate
        private static readonly List<string> TrustedDomains = new()
        {
            // Global Finance & Payments
            "paypal.com", "stripe.com", "wise.com", "chase.com",
            "bankofamerica.com", "wellsfargo.com", "citibank.com",

            // Big Tech
            "google.com", "microsoft.com", "apple.com", "amazon.com",
            "facebook.com", "instagram.com", "twitter.com", "linkedin.com",
            "netflix.com", "dropbox.com", "zoom.us", "slack.com",

            // Email Providers (common impersonation targets)
            "outlook.com", "yahoo.com", "hotmail.com", "icloud.com",
            "protonmail.com",

            // Shipping & Logistics
            "dhl.com", "fedex.com", "ups.com", "aramex.com",

            // Middle East / Saudi Banks & Telecoms
            "stc.com.sa", "alrajhibank.com.sa", "samba.com",
            "alahli.com", "riyadbank.com",
        };

        /// <summary>
        /// Returns the closest trusted domain and its Levenshtein distance,
        /// or null if no near-match is found within threshold.
        /// An exact whitelist match returns null (domain IS the legit one).
        /// </summary>
        /// <param name="senderDomain">The lowercase sender domain to check.</param>
        /// <param name="threshold">Maximum edit distance to consider (default 3).</param>
        public static (string matchedDomain, int distance)? DetectTyposquat(
            string senderDomain, int threshold = 3)
        {
            if (string.IsNullOrWhiteSpace(senderDomain))
                return null;

            senderDomain = senderDomain.ToLower().Trim();

            // If the domain IS an exact whitelist match, it's legitimate
            if (TrustedDomains.Contains(senderDomain))
                return null;

            var lev = new Fastenshtein.Levenshtein(senderDomain);
            (string matchedDomain, int distance)? closest = null;

            foreach (var trusted in TrustedDomains)
            {
                int dist = lev.DistanceFrom(trusted);

                // Only flag if distance is > 0 (not exact) and <= threshold
                if (dist > 0 && dist <= threshold)
                {
                    if (closest == null || dist < closest.Value.distance)
                        closest = (trusted, dist);
                }
            }

            return closest;
        }
    }
}
