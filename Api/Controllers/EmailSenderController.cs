using Microsoft.AspNetCore.Mvc;
using Api.Services.Messages;
using Shared.Models.MessageBroker;
using System.Threading.Tasks;
using Shared.Models.Users;

namespace Api.Controllers;

// API /Controllers/EmailController.cs
[ApiController]
[Route("api/[controller]")]
public class EmailController() : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        await Api.Services.EmailSender.SendEmailAsync(request.Email, request.Name, request.templateId);
        return Ok(new { message = "Email sent successfully" });
    }
}