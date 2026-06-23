using SecureMailBackend.Data;
using SecureMailBackend.DTOs;
using SecureMailBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace SecureMailBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public AuthService(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService; 
        }

        public async Task<User?> Register(RegisterDto registerDto)
        {
            var normalizedEmail = registerDto.Email.ToLower().Trim();

            if (await IsEmailRegistered(normalizedEmail))
                return null;

            var otpCode = new Random().Next(100000, 999999).ToString();

            var user = new User
            {
                FullName = registerDto.FullName.Trim(),
                Email = normalizedEmail,
                Phone = registerDto.Phone,
                PasswordHash = HashPassword(registerDto.Password),
                Status = "pending", 
                OtpCode = otpCode,
                OtpExpiry = DateTime.UtcNow.AddMinutes(15), 
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            Task.Run(() => _emailService.SendOtpEmailAsync(user.Email, user.FullName, otpCode));

            return user;
        }

        public async Task<string?> Login(LoginDto loginDto)
        {
            var email = loginDto.Email.ToLower().Trim();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.Status != "active" || !VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return null; 
            }

            var session = new UserSession
            {
                UserId = user.Id,
                SessionToken = Guid.NewGuid().ToString(), 
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24), 
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            return session.SessionToken;
        }

        public async Task<bool> IsEmailRegistered(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email.ToLower().Trim());
        }

        public async Task<bool> VerifyEmail(string email, string otpCode)
        {
            var normalizedEmail = email.ToLower().Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null || user.Status != "pending")
                return false;

            if (user.OtpCode == otpCode && user.OtpExpiry > DateTime.UtcNow)
            {
                user.Status = "active";
                user.OtpCode = null; 
                user.OtpExpiry = null;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}