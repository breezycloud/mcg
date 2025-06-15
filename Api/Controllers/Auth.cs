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

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class Auth : ControllerBase
{
    // private readonly AppDbContext _context;
    private readonly AppDbContext _context;
    private IConfiguration Configuration { get; }
    private LoginResponse? _result;
    public Auth(IConfiguration configuration, AppDbContext context)
    {
        _context = context;
        Configuration = configuration;
    }
    private string? pattern = string.Empty;

   // GET: api/<Auth>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse?>> Login(LoginModel user)
    {
        int TotalDays = 30;
        var hashedPassword = Security.Encrypt(user.HashedPassword!);
        var Email = $"%{user!.Email}%";
        var credential = await _context.Users.SingleOrDefaultAsync(i => EF.Functions.ILike(i.Email!, Email) && i.HashedPassword == hashedPassword);

        if (credential is null)
        {
            return NotFound();
        }
        else   
        {
            _result = new();
            _result!.Id = credential.Id;
            _result!.Role = credential.Role;
            _result.Email = credential.Email;
            if (credential.Role == UserRole.Maintenance)
            {
                var site = await _context.MaintenanceSites.FindAsync(credential.Id);
                if (site is not null)
                    _result.ShopId = site!.Id;
                else
                    return BadRequest("Invalid shop");
            }
        }                 


        var claim = new Claim[]
        {
            new Claim(ClaimTypes.Email, _result.Email!),
            new Claim(ClaimTypes.Role, _result.Role.ToString())
        };



        var token = new JwtSecurityToken(
            null,
            null,
            claim,
            expires: DateTime.Now.AddDays(TotalDays),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["App:Key"]!)),
            SecurityAlgorithms.HmacSha512Signature));

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        _result.Token = jwt;            
        return Ok(_result);
    }
}
