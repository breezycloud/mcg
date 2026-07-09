using Api.Util;
using Api.Context;
using Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Shared.Models.Auth;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Shared.Helpers;
using Shared.Enums;
using System.Runtime.InteropServices;
using Api.Services.Messages;
using Shared.Models.MessageBroker;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class Auth : ControllerBase
{
    private readonly AppDbContext _context;
    private IConfiguration Configuration { get; }
    private readonly EmailPublisherService? _mailPublisher;

#if RELEASE
    public Auth(IConfiguration configuration, AppDbContext context, EmailPublisherService mailPublisher)
    {
        _context = context;
        Configuration = configuration;
        _mailPublisher = mailPublisher;
    }
#else
    public Auth(IConfiguration configuration, AppDbContext context)
    {
        _context = context;
        Configuration = configuration;
    }
#endif

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse?>> Login(LoginModel user)
    {
        var hashedPassword = Security.Encrypt(user.HashedPassword!);
        var Email = $"%{user!.Email}%";
        var credential = await _context.Users.SingleOrDefaultAsync(i => EF.Functions.ILike(i.Email!, Email) && i.HashedPassword == hashedPassword);

        if (credential is null)
        {
            return NotFound();
        }

        var result = await BuildLoginResponseAsync(credential);
        if (result is null)
            return BadRequest("Invalid shop");

        return Ok(result);
    }

    // Shared by Login and ChangePassword — a changed password must re-issue the JWT, since
    // must_change_password is baked into the token's claims at issue time and won't reflect
    // a DB update to an already-issued token.
    private async Task<LoginResponse?> BuildLoginResponseAsync(Shared.Models.Users.User credential)
    {
        int TotalDays = 30;
        var result = new LoginResponse
        {
            Id = credential.Id,
            Role = credential.Role,
            Email = credential.Email,
            MustChangePassword = credential.MustChangePassword
        };

        if (credential.Role == UserRole.Maintenance)
        {
            // MaintenanceSupervisor is intentionally excluded — unlike Maintenance staff, they
            // oversee all maintenance locations and are not pinned to a single site's ShopId.
            var site = await _context.MaintenanceSites.FindAsync(credential.MaintenanceSiteId);
            if (site is not null)
                result.ShopId = site!.Id;
            else
                return null;
        }
        if (credential.Role == UserRole.DriverSupervisor)
        {
            result.ManagedProducts = credential.ManagedProducts;
        }

        var claim = new Claim[]
        {
            new(ClaimTypes.NameIdentifier, credential.Id.ToString()),
            new(ClaimTypes.Name, credential.ToString()!),
            new(ClaimTypes.Email, result.Email!),
            new(ClaimTypes.Role, result.Role.ToString()),
            new("managed_products", result.ManagedProducts is not null ? string.Join(",", result.ManagedProducts.Select(p => p)) : string.Empty),
            new("must_change_password", result.MustChangePassword.ToString())
        };

        var token = new JwtSecurityToken(
            null,
            null,
            claim,
            expires: DateTime.Now.AddDays(TotalDays),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["App:Key"]!)),
            SecurityAlgorithms.HmacSha512Signature));

        result.Token = new JwtSecurityTokenHandler().WriteToken(token);
        return result;
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<LoginResponse?>> ChangePassword(ChangePasswordModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.NewPassword))
        {
            return BadRequest("New password is required");
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return NotFound();
        }

        user.HashedPassword = Security.Encrypt(model.NewPassword);
        user.MustChangePassword = false;
        await _context.SaveChangesAsync();

        var result = await BuildLoginResponseAsync(user);
        if (result is null)
            return BadRequest("Invalid shop");

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<bool>> ForgotPassword(ForgotPasswordModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Email))
        {
            return BadRequest("Email is required");
        }

        var emailPattern = $"%{model.Email}%";
        var user = await _context.Users.SingleOrDefaultAsync(i => EF.Functions.ILike(i.Email!, emailPattern));

        if (user == null)
        {
            return Ok(true);
        }

        var token = Security.GenerateResetToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTimeOffset.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

#if RELEASE
        if (_mailPublisher != null)
        {
            var portalUrl = Configuration.GetValue<string>("Portal:Url") ?? "https://demo-mcc.onrender.com";
            var resetUrl = $"{portalUrl}/reset-password?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            var message = new EmailQueueMessage
            {
                To = user.Email,
                Subject = "Reset your password",
                Template = "ForgotPassword",
                TemplateModel = new AccountDetailBody
                {
                    Email = user.Email,
                    Name = user.ToString(),
                    PortalUrl = portalUrl,
                    ResetUrl = resetUrl
                }
            };

            _mailPublisher.QueueEmailAsync(message);
        }
#endif

        return Ok(true);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<bool>> ResetPassword(ResetPasswordModel model)
    {
        if (model == null || model.UserId == Guid.Empty || string.IsNullOrWhiteSpace(model.Token) || string.IsNullOrWhiteSpace(model.NewPassword))
        {
            return BadRequest("Invalid reset password request");
        }

        var user = await _context.Users.SingleOrDefaultAsync(i => i.Id == model.UserId);

        if (user == null)
        {
            return BadRequest("User not found");
        }

        if (user.PasswordResetToken != model.Token || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTimeOffset.UtcNow)
        {
            return BadRequest("Invalid or expired reset token");
        }

        user.HashedPassword = Security.Encrypt(model.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.MustChangePassword = false;
        await _context.SaveChangesAsync();

        return Ok(true);
    }
}
