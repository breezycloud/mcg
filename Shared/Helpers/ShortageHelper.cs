using Shared.Models.Trips;

namespace Shared.Helpers;

public static class ShortageHelper
{
    // A CCU recommendation, once recorded, is the authoritative shortage figure for that trip —
    // it replaces the raw (loaded - discharged) computation everywhere shortage is reported.
    // "Latest wins" (by ReceivedDate) matches ShortageRecommendationsController.GetForTrip and
    // ViewTrip.razor's existing "current recommendation" selection. Callers are still
    // responsible for their own eligibility gating (final discharge required, CNG exclusion,
    // etc.) before calling this — it only decides which NUMBER to report once a trip already
    // qualifies, never makes an ineligible trip eligible.
    public static decimal ResolveShortageAmount(decimal rawShortageAmount, IEnumerable<ShortageRecommendation>? recommendations)
    {
        var latest = recommendations?.OrderByDescending(r => r.ReceivedDate).FirstOrDefault();
        return latest?.RecommendedShortageAmount ?? rawShortageAmount;
    }
}
