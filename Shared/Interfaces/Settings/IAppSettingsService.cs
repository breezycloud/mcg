using Shared.Models.Settings;

namespace Shared.Interfaces.Settings;

public interface IAppSettingsService
{
    Task<NotificationSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(NotificationSettings settings, CancellationToken cancellationToken = default);
}
