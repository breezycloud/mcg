using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.Drivers;

namespace Shared.Interfaces.Drivers;


public interface IDriverService
{
    Task<bool> AddAsync(Driver model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Driver model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<Driver?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Driver[]?> GetAsync(CancellationToken cancellationToken);
    Task<GridDataResponse<Driver>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);

    /// <summary>Returns the existing driver's Id if the phone number is already in use, otherwise null.</summary>
    Task<Guid?> ValidatePhoneAsync(string phone, Guid? excludeId, CancellationToken cancellationToken);

    Task<DriverImportPreviewResponse?> PreviewImportAsync(byte[] csvContent, string fileName, CancellationToken cancellationToken);
    Task<DriverImportCommitResponse?> CommitImportAsync(byte[] csvContent, string fileName, CancellationToken cancellationToken);
}