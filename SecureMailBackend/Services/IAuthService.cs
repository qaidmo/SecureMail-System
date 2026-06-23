using SecureMailBackend.DTOs;
using SecureMailBackend.Models;

namespace SecureMailBackend.Services
{
    public interface IAuthService
    {
        Task<User?> Register(RegisterDto registerDto);

        Task<string?> Login(LoginDto loginDto);

        Task<bool> IsEmailRegistered(string email);

        Task<bool> VerifyEmail(string email, string otpCode);

        string HashPassword(string password);

        bool VerifyPassword(string password, string hashedPassword);
    }
}