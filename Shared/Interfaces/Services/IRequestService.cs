using Shared.Helpers;
using Shared.Models.Services;

namespace Shared.Interfaces.Services;

public interface IRequestService
{
    Task<bool> AddAsync(ServiceRequest model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(ServiceRequest model, CancellationToken cancellationToken);
    Task<bool> AddHistoryAsync(ServiceRequestHistory model, CancellationToken cancellationToken);
    Task<bool> DeleteHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<ServiceRequest?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GridDataResponse<ServiceRequest>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
    Task<GridDataResponse<ServiceRequest>?> ReportPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}