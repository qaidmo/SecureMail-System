using Microsoft.AspNetCore.Mvc;
using SecureMailBackend.DTOs;
using SecureMailBackend.Services;

namespace SecureMailBackend.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var user = await _authService.Register(registerDto);

            if (user == null)
            {
                return BadRequest(new { message = "هذا البريد مسجل مسبقاً / Email already registered" });
            }

            return Ok(new { message = "تم إنشاء الحساب بنجاح / Account created successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var token = await _authService.Login(loginDto);

            if (token == null)
            {
                return Unauthorized(new { message = "بيانات غير صحيحة أو الحساب معطل / Invalid credentials or account disabled" });
            }

            return Ok(new
            {
                token = token,
                message = "تم تسجيل الدخول بنجاح / Login successful"
            });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] OtpVerificationDto verifyDto)
        {
            var isVerified = await _authService.VerifyEmail(verifyDto.Email, verifyDto.OtpCode);

            if (!isVerified)
            {
                return BadRequest(new { message = "رمز التحقق غير صحيح أو انتهت صلاحيته / Invalid or expired OTP code" });
            }

            return Ok(new { message = "تم تفعيل الحساب بنجاح، يمكنك تسجيل الدخول الآن / Account verified successfully" });
        }
    }
}