using System.Diagnostics;

namespace SecureMailBackend.Services
{
    public interface IYaraScannerService
    {
        /// <summary>
        /// Scans raw file bytes against all YARA rule files in the YaraRules directory.
        /// Returns matched rule names and a severity classification.
        /// </summary>
        Task<YaraScanResult> ScanAsync(byte[] fileData, string fileName);
    }

    public class YaraScanResult
    {
        public bool HasMatches { get; set; }
        public List<string> MatchedRules { get; set; } = new();
        public string Severity { get; set; } = "NONE"; // NONE, CRITICAL
    }

    /// <summary>
    /// YARA rule scanner using CLI-based execution (yara.exe via Process.Start).
    /// This approach is zero-dependency and immune to native wrapper maintenance issues.
    /// Files are written to temp, scanned, then deleted.
    /// </summary>
    public class YaraScannerService : IYaraScannerService
    {
        private readonly ILogger<YaraScannerService> _logger;
        private readonly string _yaraExePath;
        private readonly string _rulesDirectory;

        public YaraScannerService(ILogger<YaraScannerService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _yaraExePath = configuration["Yara:ExePath"] ?? "yara";
            _rulesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "YaraRules");
        }

        public async Task<YaraScanResult> ScanAsync(byte[] fileData, string fileName)
        {
            var result = new YaraScanResult();

            if (fileData == null || fileData.Length == 0)
                return result;

            // Check if YARA rules directory exists
            if (!Directory.Exists(_rulesDirectory))
            {
                _logger.LogWarning("YARA rules directory not found at {Path}. Skipping YARA scan.", _rulesDirectory);
                return result;
            }

            var ruleFiles = Directory.GetFiles(_rulesDirectory, "*.yar");
            if (ruleFiles.Length == 0)
            {
                _logger.LogWarning("No .yar rule files found in {Path}. Skipping YARA scan.", _rulesDirectory);
                return result;
            }

            // Write attachment to a temporary file for CLI scanning
            var tempFile = Path.Combine(Path.GetTempPath(),
                $"securemail_yara_{Guid.NewGuid()}{Path.GetExtension(fileName)}");

            try
            {
                await File.WriteAllBytesAsync(tempFile, fileData);

                foreach (var ruleFile in ruleFiles)
                {
                    var matches = await RunYaraAsync(ruleFile, tempFile);
                    result.MatchedRules.AddRange(matches);
                }

                result.HasMatches = result.MatchedRules.Any();
                result.Severity = result.HasMatches ? "CRITICAL" : "NONE";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YARA scanning failed for attachment {File}", fileName);
            }
            finally
            {
                // Always clean up the temp file
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up temp YARA file {File}", tempFile);
                }
            }

            return result;
        }

        private async Task<List<string>> RunYaraAsync(string ruleFile, string targetFile)
        {
            var matches = new List<string>();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _yaraExePath,
                    Arguments = $"\"{ruleFile}\" \"{targetFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    _logger.LogWarning("Failed to start YARA process. Is yara.exe installed and in PATH?");
                    return matches;
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                string errors = await process.StandardError.ReadToEndAsync();

                // Timeout protection: wait max 30 seconds per rule file
                bool exited = await Task.Run(() => process.WaitForExit(30_000));
                if (!exited)
                {
                    process.Kill();
                    _logger.LogWarning("YARA process timed out for rule {Rule}", Path.GetFileName(ruleFile));
                    return matches;
                }

                if (!string.IsNullOrEmpty(errors))
                {
                    _logger.LogWarning("YARA stderr for {Rule}: {Errors}", Path.GetFileName(ruleFile), errors.Trim());
                }

                // YARA output format: "RuleName targetFile\n"
                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    string ruleName = line.Split(' ')[0].Trim();
                    if (!string.IsNullOrEmpty(ruleName))
                        matches.Add(ruleName);
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // yara.exe not found in PATH — graceful degradation
                _logger.LogWarning("YARA executable not found ({Message}). Skipping YARA scanning. Install yara.exe and ensure it is in PATH.", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YARA scan error for rule {Rule}", Path.GetFileName(ruleFile));
            }
            return matches;
        }
    }
}
