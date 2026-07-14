using Api.Util;
using Api.Context;
using Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
    private readonly IWebHostEnvironment _env;

#if RELEASE
    public Auth(IConfiguration configuration, AppDbContext context, EmailPublisherService mailPublisher, IWebHostEnvironment env)
    {
        _context = context;
        Configuration = configuration;
        _mailPublisher = mailPublisher;
        _env = env;
    }
#else
    public Auth(IConfiguration configuration, AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        Configuration = configuration;
        _env = env;
    }
#endif

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse?>> Login(LoginModel user)
    {
        // LoginModel.HashedPassword is plaintext despite the name — it's bound straight from the
        // login form's password input and only ever hashed here, server-side.
        var plainPassword = user.HashedPassword!;
        var credential = await _context.Users.SingleOrDefaultAsync(i => EF.Functions.ILike(i.Email!, user!.Email!));

        if (credential is null || !Security.VerifyPassword(plainPassword, credential.HashedPassword))
        {
            return NotFound();
        }

        // Transparent upgrade: an account still carrying the old SHA-512/hardcoded-salt hash
        // gets re-hashed with the new scheme the moment it next logs in successfully — avoids
        // forcing every existing user through a disruptive mass password reset.
        if (Security.IsLegacyHash(credential.HashedPassword))
        {
            credential.HashedPassword = Security.HashPassword(plainPassword);
            await _context.SaveChangesAsync();
        }

        var result = await BuildLoginResponseAsync(credential);
        if (result is null)
            return BadRequest("Invalid shop");

        result.RefreshToken = await IssueRefreshTokenAsync(credential.Id, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse?>> RefreshToken(RefreshTokenModel model, CancellationToken cancellationToken)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.RefreshToken))
        {
            return BadRequest("Refresh token is required");
        }

        var tokenHash = Security.HashToken(model.RefreshToken);
        var stored = await _context.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        // A token that's expired or was already revoked (including one already consumed by a
        // prior refresh — rotation means each refresh token is single-use) is rejected the same
        // way as one that was never issued at all, rather than distinguishing the reason —
        // nothing legitimate depends on knowing which case it was, and a specific error just
        // helps an attacker learn about the token's state.
        if (stored is null || !stored.IsActive)
        {
            return Unauthorized();
        }

        var user = await _context.Users.SingleOrDefaultAsync(u => u.Id == stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var result = await BuildLoginResponseAsync(user);
        if (result is null)
            return BadRequest("Invalid shop");

        // Rotate: this refresh token is now spent. Revoking it here (rather than deleting) keeps
        // a record — if it's ever presented again, that's a signal the token was copied/stolen,
        // not just an expired session.
        var newPlaintext = Security.GenerateRefreshToken();
        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Security.HashToken(newPlaintext),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.ReplacedByTokenId = newToken.Id;
        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync(cancellationToken);

        result.RefreshToken = newPlaintext;
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenModel model, CancellationToken cancellationToken)
    {
        // Deliberately not [Authorize] — logout must still work even if the access token has
        // already expired (the whole reason a separate refresh token exists). Possessing the
        // refresh token is itself the credential needed to revoke it; nothing else to check.
        if (model is not null && !string.IsNullOrWhiteSpace(model.RefreshToken))
        {
            var tokenHash = Security.HashToken(model.RefreshToken);
            var stored = await _context.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
            if (stored is not null && stored.RevokedAt is null)
            {
                stored.RevokedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return NoContent();
    }

    // Access tokens are short-lived now that a real refresh-token flow exists (see IssueRefreshTokenAsync
    // and the /refresh endpoint below) — a stolen access token has a narrow window instead of the
    // 30 days (then 24 hours) it used to. Deactivation also takes effect immediately regardless
    // of remaining token lifetime (Program.cs's OnTokenValidated).
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(30);

    // Shared by Login, ChangePassword, and RefreshToken — a changed password (or a refresh) must
    // re-issue the JWT, since must_change_password and role are baked into the token's claims at
    // issue time and won't reflect a DB update to an already-issued token. Does NOT touch refresh
    // tokens itself — callers that need one call IssueRefreshTokenAsync separately, since not
    // every caller of this method should mint a new refresh token (e.g. ChangePassword keeps the
    // caller's existing session's refresh token as-is).
    private async Task<LoginResponse?> BuildLoginResponseAsync(Shared.Models.Users.User credential)
    {
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
            issuer: Configuration["App:Issuer"],
            audience: Configuration["App:Audience"],
            claim,
            expires: DateTime.UtcNow.Add(AccessTokenLifetime),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["App:Key"]!)),
            SecurityAlgorithms.HmacSha512Signature));

        result.Token = new JwtSecurityTokenHandler().WriteToken(token);
        return result;
    }

    // Issues a brand-new refresh token row and returns the plaintext — the only moment the
    // plaintext ever exists outside the caller's own device; only its hash is persisted.
    private async Task<string> IssueRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var plaintext = Security.GenerateRefreshToken();
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Security.HashToken(plaintext),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });
        await _context.SaveChangesAsync(cancellationToken);
        return plaintext;
    }

    // A password change/reset is a strong signal to kill every other active session — if the
    // change was prompted by a compromised account, this is what actually locks the attacker's
    // session out (the JWT itself can't be revoked early, but its refresh token can, so the
    // attacker's access dies within one AccessTokenLifetime instead of staying valid for 30 days).
    private async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow), cancellationToken);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<LoginResponse?>> ChangePassword(ChangePasswordModel model, CancellationToken cancellationToken)
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

        var user = await _context.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.HashedPassword = Security.HashPassword(model.NewPassword!);
        user.MustChangePassword = false;
        await _context.SaveChangesAsync(cancellationToken);
        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

        var result = await BuildLoginResponseAsync(user);
        if (result is null)
            return BadRequest("Invalid shop");

        result.RefreshToken = await IssueRefreshTokenAsync(user.Id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<bool>> ForgotPassword(ForgotPasswordModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Email))
        {
            return BadRequest("Email is required");
        }

        var user = await _context.Users.SingleOrDefaultAsync(i => EF.Functions.ILike(i.Email!, model.Email));

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
                    ResetUrl = resetUrl,
                    IsTestEnvironment = !_env.IsProduction()
                }
            };

            _mailPublisher.QueueEmailAsync(message);
        }
#endif

        return Ok(true);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<bool>> ResetPassword(ResetPasswordModel model, CancellationToken cancellationToken)
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

        // Constant-time comparison — same pattern as ApiKeyAuthFilter — prevents a timing
        // side-channel from narrowing down a valid reset token character by character.
        var storedToken = user.PasswordResetToken;
        var tokensMatch = storedToken != null
            && storedToken.Length == model.Token.Length
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(storedToken),
                Encoding.UTF8.GetBytes(model.Token));

        if (!tokensMatch || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTimeOffset.UtcNow)
        {
            return BadRequest("Invalid or expired reset token");
        }

        user.HashedPassword = Security.HashPassword(model.NewPassword!);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.MustChangePassword = false;
        await _context.SaveChangesAsync(cancellationToken);
        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

        return Ok(true);
    }
}
