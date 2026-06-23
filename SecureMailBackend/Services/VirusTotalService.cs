using Microsoft.EntityFrameworkCore;
using SecureMailBackend.Data;
using SecureMailBackend.Models;
using System.Text.Json;

namespace SecureMailBackend.Services
{
    public interface IVirusTotalService
    {
        Task<bool> IsUrlMaliciousAsync(string url);
    }

    public class VirusTotalService : IVirusTotalService
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VirusTotalService> _logger;
        private readonly string? _apiKey;

        // Static semaphore to strictly limit concurrent processing
        private static readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(1, 1);
        private static readonly Queue<DateTime> _requestTimestamps = new Queue<DateTime>();

        public VirusTotalService(HttpClient httpClient, IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<VirusTotalService> logger)
        {
            _httpClient = httpClient;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _apiKey = configuration["VirusTotal:ApiKey"];
            
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("x-apikey", _apiKey);
            }
        }

        public async Task<bool> IsUrlMaliciousAsync(string url)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return false;

            try
            {
                // We use base64url encoding for VT domain/url lookup
                string urlId = Base64UrlEncode(url);

                if (!await CanMakeRequestAsync())
                {
                    _logger.LogWarning("VirusTotal daily quota exceeded. Skipping scan.");
                    throw new HttpRequestException("Quota Exceeded", null, System.Net.HttpStatusCode.TooManyRequests);
                }

                await WaitForRateLimitAsync();

                var response = await _httpClient.GetAsync($"https://www.virustotal.com/api/v3/urls/{urlId}");
                
                await IncrementDailyQuotaAsync();

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    
                    if (doc.RootElement.TryGetProperty("data", out var dataElement) &&
                        dataElement.TryGetProperty("attributes", out var attributesElement) &&
                        attributesElement.TryGetProperty("last_analysis_stats", out var stats))
                    {
                        int malicious = stats.TryGetProperty("malicious", out var m) ? m.GetInt32() : 0;
                        int suspicious = stats.TryGetProperty("suspicious", out var s) ? s.GetInt32() : 0;

                        return (malicious > 0 || suspicious > 1);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new HttpRequestException("Rate limit hit from server side 429.", null, System.Net.HttpStatusCode.TooManyRequests);
                }
            }
            catch (HttpRequestException)
            {
                throw; // Rethrow to let the analyzer handle it
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking URL with VirusTotal");
            }

            return false;
        }

        private async Task WaitForRateLimitAsync()
        {
            await _rateLimitSemaphore.WaitAsync();
            try
            {
                // Free tier limit: 4 requests per minute.
                while (_requestTimestamps.Count >= 4)
                {
                    var oldest = _requestTimestamps.Peek();
                    var timeElapsed = DateTime.UtcNow - oldest;
                    
                    if (timeElapsed.TotalSeconds < 60)
                    {
                        var delayMs = (int)(60 - timeElapsed.TotalSeconds) * 1000 + 500; // Add 500ms buffer
                        await Task.Delay(delayMs);
                    }
                    else
                    {
                        _requestTimestamps.Dequeue(); // Safe to remove as it's older than 60s
                    }
                }
                
                _requestTimestamps.Enqueue(DateTime.UtcNow);
                if (_requestTimestamps.Count > 4)
                {
                    _requestTimestamps.Dequeue(); // Ensure we don't hold more than necessary
                }
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }

        private async Task<bool> CanMakeRequestAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var today = DateTime.UtcNow.Date;
            
            var record = await db.VirusTotalQuotas.FirstOrDefaultAsync(q => q.Date == today);
            if (record != null && record.RequestCount >= 495)
            {
                return false;
            }
            return true;
        }

        private async Task IncrementDailyQuotaAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var today = DateTime.UtcNow.Date;
            
            var record = await db.VirusTotalQuotas.FirstOrDefaultAsync(q => q.Date == today);
            if (record == null)
            {
                record = new VirusTotalQuota { Date = today, RequestCount = 1 };
                db.VirusTotalQuotas.Add(record);
            }
            else
            {
                record.RequestCount++;
            }
            
            await db.SaveChangesAsync();
        }

        private string Base64UrlEncode(string url)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(url);
            var base64 = Convert.ToBase64String(bytes);
            return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}
