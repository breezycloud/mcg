using Shared.Models.Trips;

namespace Shared.Interfaces.Trips;

public interface IShortageRecommendationService
{
    Task<List<ShortageRecommendation>> GetForTripAsync(Guid tripId, CancellationToken cancellationToken);
    Task<bool> AddAsync(ShortageRecommendation model, CancellationToken cancellationToken);
}
