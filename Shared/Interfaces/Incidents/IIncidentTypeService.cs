using Shared.Helpers;
using Shared.Models.Incidents;

namespace Shared.Interfaces.Incidents;

public interface IIncidentTypeService
{
    Task<bool> AddAsync(IncidentType model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(IncidentType model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<IncidentType[]?> GetAsync(CancellationToken cancellationToken);
    Task<IncidentType?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GridDataResponse<IncidentType>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}