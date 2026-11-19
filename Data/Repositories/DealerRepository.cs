using LuxWatch.Data.Rows;
using LuxWatch.Models;
using static Supabase.Postgrest.Constants;

namespace LuxWatch.Data.Repositories;

public interface IDealerRepository
{
    /// <summary>Derived numbers from the dealer_stats view; null if the dealer doesn't exist.</summary>
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
                              r.RecommendPct,
                              r.Stars5, r.Stars4, r.Stars3, r.Stars2, r.Stars1);
    }
}
