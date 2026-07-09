using Microsoft.AspNetCore.Authorization;
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
    private readonly EmailPublisherService? _mailPublisher;
    private readonly IConfiguration _configuration;

    #if RELEASE

        public UsersController(AppDbContext context, EmailPublisherService mailPublisher, IConfiguration configuration)
        {
            _context = context;
            _mailPublisher = mailPublisher;
            _configuration = configuration;
        }
    #endif

     #if DEBUG
        public UsersController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
     #endif
    

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
            var pagedQuery = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt).Skip(request.Paging).Take(request.PageSize).AsAsyncEnumerable();

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

    // GET: api/Users
    [HttpGet("by-site/{id}")]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Users.AsNoTracking().Where(x => x.MaintenanceSiteId == id).ToListAsync(cancellationToken);
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

        if (user.SupervisorId.HasValue && await WouldCreateSupervisorCycleAsync(user.Id, user.SupervisorId.Value))
        {
            return BadRequest("This supervisor assignment would create a cycle (directly or transitively supervising themselves).");
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
    [Authorize(Roles = "Admin,Master")]
    public async Task<ActionResult<User>> PostUser(User user)
    {
        var password = Security.GenerateRandomPassword();
        user.HashedPassword = Security.Encrypt(password);
        user.MustChangePassword = true;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (_mailPublisher != null)
        {
            var portalUrl = _configuration.GetValue<string>("Portal:Url") ?? "https://demo-mcc.onrender.com";
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
                    PortalUrl = portalUrl
                }
            };
            _mailPublisher.QueueEmailAsync(emailMessage);
        }

        return CreatedAtAction("GetUser", new { id = user.Id }, user);
    }

    // DELETE: api/Users/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Master")]
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

    // Walks the proposed supervisor's chain upward looking for userId — catches both a direct
    // self-assignment and a transitive cycle (A supervises B, B supervises A). The visited-set
    // guard also stops the walk if it encounters an already-existing cycle elsewhere in the data
    // rather than looping forever.
    private async Task<bool> WouldCreateSupervisorCycleAsync(Guid userId, Guid supervisorId)
    {
        var visited = new HashSet<Guid>();
        Guid? currentId = supervisorId;

        while (currentId.HasValue)
        {
            if (currentId.Value == userId)
                return true;

            if (!visited.Add(currentId.Value))
                break;

            currentId = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == currentId.Value)
                .Select(u => u.SupervisorId)
                .FirstOrDefaultAsync();
        }

        return false;
    }
}
