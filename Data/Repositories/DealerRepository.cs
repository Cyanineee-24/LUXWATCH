using LuxWatch.Data.Rows;
using static Supabase.Postgrest.Constants;

namespace LuxWatch.Data.Repositories;

/// <summary>
/// Derived dealer numbers from the dealer_stats view (read model, not a UML class).
/// Averages are null when the dealer has no visible reviews.
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
    int? RecommendPct);

public interface IDealerRepository
{
    Task<DealerStats?> GetStatsAsync(Guid dealerId);
}

public sealed class SupabaseDealerRepository(Supabase.Client db) : IDealerRepository
{
    public async Task<DealerStats?> GetStatsAsync(Guid dealerId)
    {
        var res = await db.From<DealerStatsRow>()
            .Filter("dealer_id", Operator.Equals, dealerId.ToString())
            .Limit(1)
            .Get();

        var r = res.Models.FirstOrDefault();
        return r is null
            ? null
            : new DealerStats(r.DealerId, r.ActiveListings, r.WatchesSold, r.ReviewCount,
                              r.AvgOverall, r.AvgShipping, r.AvgItemAsDescribed, r.AvgCommunication,
                              r.RecommendPct);
    }
}
