using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureMailBackend.Data;
using SecureMailBackend.Models;

namespace SecureMailBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
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

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            int userId = await GetUserIdFromTokenAsync();
            if (userId == 0) return Unauthorized(new { message = "Unauthorized session" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found" });

            var activeIntegrations = await _context.EmailIntegrations
                .Where(e => e.UserId == userId && e.Status == "active")
                .Select(e => new { e.Id, e.Provider, e.ProviderAccountEmail, e.CreatedAt })
                .ToListAsync();

            return Ok(new
            {
                user.FullName,
                user.Email,
                user.Phone,
                Integrations = activeIntegrations
            });
        }

        [HttpPost("device-token")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterTokenDto dto)
        {
            int userId = await GetUserIdFromTokenAsync();
            if (userId == 0) return Unauthorized(new { message = "Unauthorized session" });

            if (string.IsNullOrEmpty(dto.FcmToken) || string.IsNullOrEmpty(dto.Platform))
                return BadRequest("Token and Platform are required.");

            var existingToken = await _context.DeviceTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.FcmToken == dto.FcmToken);

            if (existingToken != null)
            {
                existingToken.LastSeenAt = DateTime.UtcNow;
            }
            else
            {
                _context.DeviceTokens.Add(new DeviceToken
                {
                    UserId = userId,
                    Platform = dto.Platform.ToUpper(),
                    FcmToken = dto.FcmToken,
                    CreatedAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Device token registered successfully" });
        }
    }

    public class RegisterTokenDto
    {
        public string FcmToken { get; set; }
        public string Platform { get; set; }
    }
}
