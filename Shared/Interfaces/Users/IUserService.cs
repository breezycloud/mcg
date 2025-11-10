using Shared.Helpers;
using Shared.Models.Users;

namespace Shared.Interfaces.Users;


public interface IUserService
{
    Task<bool> AddAsync(User model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(User model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task SendEmailAsync(string email, string name, int templateId, CancellationToken cancellationToken);
    Task SendEmailAsync(EmailRequest request, CancellationToken cancellationToken);
    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<User[]?> GetAsync(CancellationToken cancellationToken);    
    Task<User[]?> GetAsync(string status, CancellationToken cancellationToken);
    Task<User[]?> GetAllAsync(Guid maintenanceSiteId, CancellationToken cancellationToken);
    Task<GridDataResponse<User>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
    ValueTask<bool> ValidateEntry(string type, string value, CancellationToken cancellationToken);
}