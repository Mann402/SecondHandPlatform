using Microsoft.AspNetCore.Mvc;
using SecondHandPlatform.Services;
using SecondHandPlatform.DTOs;

namespace SecondHandPlatformTest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _email;
        public EmailController(IEmailService email) => _email = email;

        // POST api/email/send
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] EmailRequest req)
        {
            await _email.SendEmailAsync(req.To, req.Subject, req.Body);
            return NoContent();
        }
    }
}