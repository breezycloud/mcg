using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Users;
using Shared.Helpers;
using Api.Services.Messages;
using Shared.Models.MessageBroker;
using Api.Util;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EmailPublisherService _mailPublisher;

    public UsersController(AppDbContext context, EmailPublisherService mailPublisher)
    {
        _context = context;
        _mailPublisher = mailPublisher;
    }

    // POST: api/Users/SendEmail
    [HttpPost("send-email")]
    public async Task<ActionResult<bool>> SendEmailAsync(EmailQueueMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (message is null || string.IsNullOrEmpty(message.To) || string.IsNullOrEmpty(message.Subject))
            {
                return BadRequest("Invalid email message.");
            }

            message.Template = "AccountDetails";
            message.TemplateModel = new AccountDetailBody
            {
                Email = message.To,
                Name = "Aminu Aliyu",
                Password = Security.GenerateRandomPassword(),
                PortalUrl = "http://myapplication.com"
            };

            _mailPublisher.QueueEmailAsync(message);
            return Ok(true);
        }
        catch (Exception ex)
        {
            // Log the exception (not implemented here)
            return StatusCode(500, "Internal server error: " + ex.Message);
        }
    }

    // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<User>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<User> response = new();
        try
        {
            var query = _context.Users.AsQueryable();
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Where(x => EF.Functions.ILike(x.Email!, pattern) || EF.Functions.ILike(x.FirstName!, pattern)
                || EF.Functions.ILike(x.LastName!, pattern)
                || EF.Functions.ILike(x.PhoneNo!, pattern));
            }

            response.Total = await query.CountAsync();
            response.Data = [];
            var pagedQuery = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt).Skip(request.Page).Take(request.PageSize).AsAsyncEnumerable();

            await foreach (var item in pagedQuery)
            {
                response.Data.Add(item);
            }


            return response;

            
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    // GET: api/Users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }

    // GET: api/Users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return user;
    }

    // PUT: api/Users/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(Guid id, User user)
    {
        if (id != user.Id)
        {
            return BadRequest();
        }

        _context.Entry(user).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Users
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<User>> PostUser(User user)
    {
        var password = Security.GenerateRandomPassword();
        var hashedPassword = Security.Encrypt(password);
        user.HashedPassword = hashedPassword;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        // Optionally, you can send a welcome email after user creation
        var emailMessage = new EmailQueueMessage
        {
            To = user.Email,
            Subject = "Your login credentials",
            Template = "AccountDetails",
            TemplateModel = new AccountDetailBody
            {
                Email = user.Email,
                Name = user.ToString(),
                Password = password,
                PortalUrl = "http://myapplication.com"
            }
        };
        _mailPublisher.QueueEmailAsync(emailMessage);
        return CreatedAtAction("GetUser", new { id = user.Id }, user);
    }

    // DELETE: api/Users/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool UserExists(Guid id)
    {
        return _context.Users.Any(e => e.Id == id);
    }
}
