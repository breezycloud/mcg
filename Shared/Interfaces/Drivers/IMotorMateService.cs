using Shared.Helpers;
using Shared.Models.Drivers;

namespace Shared.Interfaces.Drivers;

public interface IMotorMateService
{
    Task<bool> AddAsync(MotorMate model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(MotorMate model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<MotorMate?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<MotorMate[]?> GetAsync(CancellationToken cancellationToken);
    Task<GridDataResponse<MotorMate>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
}
