namespace LuxWatch.Models;

/// <summary>
/// Derived dealer numbers, computed by the database (dealer_stats view).
/// A read model, not a UML class: set on <see cref="Dealer.Stats"/> by the page.
/// Averages and RecommendPct are null when the dealer has no visible reviews.
/// </summary>
public sealed record DealerStats(
    Guid DealerId,
    int ActiveListings,
    int WatchesSold,
    int ReviewCount,
    decimal? AvgOverall,
    decimal? AvgShipping,
    decimal? AvgItemAsDescribed,
    decimal? AvgCommunication,
    int? RecommendPct,
    int Stars5,
    int Stars4,
    int Stars3,
    int Stars2,
    int Stars1)
{
    /// <summary>Number of visible reviews with this overall rating (1–5).</summary>
    public int CountFor(int stars) => stars switch
    {
        5 => Stars5, 4 => Stars4, 3 => Stars3, 2 => Stars2, 1 => Stars1,
        _ => 0
    };
}
