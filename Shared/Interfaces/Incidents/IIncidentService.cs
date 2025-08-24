using Shared.Helpers;
using Shared.Models.Incidents;

namespace Shared.Interfaces.Incidents;

public interface IIncidentService
{
    Task<bool> AddAsync(Incident model, CancellationToken cancellationToken);    
    Task<bool> UpdateAsync(Incident model, CancellationToken cancellationToken);    
    Task<bool> AddHistoryAsync(IncidentHistory model, CancellationToken cancellationToken);
    Task<bool> DeleteHistoryAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Incident?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GridDataResponse<Incident>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}