using Shared.Helpers;
using Shared.Models.Trucks;

namespace Shared.Interfaces.Trucks;


public interface ITruckService
{
    Task<bool> AddAsync(Truck model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Truck model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Truck?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Truck[]?> GetAsync(CancellationToken cancellationToken);
    Task<Truck[]?> GetAsync(string status, CancellationToken cancellationToken);
    Task<Truck[]?> GetTrucksAvailableAsync(CancellationToken cancellationToken);
    Task<GridDataResponse<Truck>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
    ValueTask<bool> ValidateEntry(string type, string value, CancellationToken cancellationToken);

    Task ExportToExcel<T>(List<T> data, string fileName);
    Task ExportToPdf<T>(List<T> data, string fileName);
}