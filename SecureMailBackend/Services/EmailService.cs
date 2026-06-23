using System.Net;
using System.Net.Mail;

namespace SecureMailBackend.Services
{
    public interface IEmailService
    {
        Task<bool> SendOtpEmailAsync(string toEmail, string toName, string otpCode);
    }

    public class SmtpEmailService : IEmailService
    {
      
        private readonly string _senderEmail = "@gmail.com";
        private readonly string _appPassword = ""; 
        
        public async Task<bool> SendOtpEmailAsync(string toEmail, string toName, string otpCode)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_senderEmail, "SecureMail Security"),
                    Subject = "SecureMail - رمز تفعيل حسابك",
                    Body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px; background-color: #f9f9f9;'>
                            <h2 style='color: #2563eb; text-align: center;'>SecureMail System</h2>
                            <p style='font-size: 16px; color: #333;'>مرحباً <strong>{toName}</strong>،</p>
                            <p style='font-size: 16px; color: #333;'>لقد تلقينا طلباً لإنشاء حساب جديد باستخدام هذا البريد الإلكتروني.</p>
                            <p style='font-size: 16px; color: #333;'>رمز التفعيل (OTP) الخاص بك هو:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; background: #2563eb; color: #fff; padding: 15px 30px; border-radius: 8px;'>{otpCode}</span>
                            </div>
                            <p style='font-size: 14px; color: #666; text-align: center;'>هذا الرمز صالح لمدة 15 دقيقة فقط. إذا لم تطلب هذا الرمز، يرجى تجاهل هذه الرسالة.</p>
                            <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
                            <p style='font-size: 12px; color: #999; text-align: center;'>&copy; 2026 SecureMail System. جميع الحقوق محفوظة.</p>
                        </div>",
                    IsBodyHtml = true
                };

                message.To.Add(new MailAddress(toEmail, toName));

                using var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(_senderEmail, _appPassword),
                    EnableSsl = true,
                };

                await smtpClient.SendMailAsync(message);

                Console.WriteLine($"[EMAIL SMTP] Successfully sent OTP to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL EXCEPTION SMTP] {ex.Message}");
                return false;
            }
        }
    }
}
