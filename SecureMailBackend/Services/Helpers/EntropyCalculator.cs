namespace SecureMailBackend.Services.Helpers
{
    /// <summary>
    /// Calculates Shannon Entropy of strings to detect randomized/obfuscated
    /// URLs and attachment names used to evade pattern-based detection.
    /// Formula: H(X) = -Σ p(xᵢ) × log₂(p(xᵢ))
    /// </summary>
    public static class EntropyCalculator
    {
        /// <summary>
        /// Calculates Shannon entropy of a string (bits per character).
        /// Reference values:
        ///   - Normal English text:   ~3.5 - 4.5 bits
        ///   - Structured domains:    ~3.0 - 4.0 bits
        ///   - Random/obfuscated:     ~5.0 - 6.0+ bits
        ///   - Hex/Base64 strings:    ~5.5 - 6.0 bits
        /// </summary>
        public static double Calculate(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            var freq = new Dictionary<char, int>();
            foreach (var c in input)
            {
                freq.TryGetValue(c, out int count);
                freq[c] = count + 1;
            }

            double entropy = 0;
            int len = input.Length;
            foreach (var count in freq.Values)
            {
                double p = (double)count / len;
                if (p > 0)
                    entropy -= p * Math.Log2(p);
            }

            return entropy;
        }

        /// <summary>
        /// Extracts the host + path from a URL and calculates its entropy.
        /// The path/query is where obfuscation typically hides
        /// (e.g., /a8f2c?q=z9w1p&tok=x7kd).
        /// </summary>
        public static double CalculateUrlEntropy(string url)
        {
            try
            {
                var uri = new Uri(url);
                // Analyze host + path + query (where obfuscation hides)
                string target = uri.Host + uri.PathAndQuery;
                return Calculate(target);
            }
            catch
            {
                return Calculate(url);
            }
        }

        /// <summary>
        /// Checks if a URL's combined host+path exceeds the entropy threshold.
        /// Default threshold 5.0 bits → flags randomized/encoded URLs.
        /// </summary>
        public static bool IsUrlHighEntropy(string url, double threshold = 5.0)
        {
            return CalculateUrlEntropy(url) >= threshold;
        }

        /// <summary>
        /// Checks if a filename (without extension) exceeds the entropy threshold.
        /// Default threshold 4.8 bits → flags obfuscated filenames like x8k2mf9.pdf.
        /// </summary>
        public static bool IsFilenameHighEntropy(string filename, double threshold = 4.8)
        {
            // Strip extension before calculating — extension is structured, not random
            string nameOnly = Path.GetFileNameWithoutExtension(filename);
            if (string.IsNullOrEmpty(nameOnly) || nameOnly.Length < 4)
                return false; // Too short to be meaningful
            return Calculate(nameOnly) >= threshold;
        }
    }
}
