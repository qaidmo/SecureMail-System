using Microsoft.AspNetCore.Mvc;
using SecureMailBackend.Services;

namespace SecureMailBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyzerController : ControllerBase
    {
        private readonly IEmailAnalyzerService _analyzerService;

        public AnalyzerController(IEmailAnalyzerService analyzerService)
        {
            _analyzerService = analyzerService;
        }

        [HttpGet("email")]
        public async Task<IActionResult> AnalyzeEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "الرجاء إرسال بريد إلكتروني للتحليل." });
            }

            var result = await _analyzerService.AnalyzeEmailAsync(email);
            return Ok(result);
        }
    }
}
