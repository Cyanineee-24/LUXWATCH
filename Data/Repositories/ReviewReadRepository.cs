using LuxWatch.Data.Mapping;
using LuxWatch.Data.Rows;
using LuxWatch.Models;
using static Supabase.Postgrest.Constants;

namespace LuxWatch.Data.Repositories;

public interface IReviewReadRepository
{
    /// <summary>Visible (not soft-deleted) reviews for a dealer, newest first, one page (1-based).</summary>
    Task<IReadOnlyList<Review>> GetForDealerAsync(Guid dealerId, int page, int pageSize);
}

/// <summary>
/// Two requests per page, never one per review:
///   1. the page of dealer_reviews (filtered, sorted, ranged in the database);
///   2. ONE batched watches?id=in.(…) request for the distinct watches on that page.
/// Step 2 exists because the domain Review needs a full Order → Watch → Dealer → Owner,
/// which the view does not carry (see the Phase 3 report).
/// </summary>
public sealed class SupabaseReviewReadRepository(Supabase.Client db) : IReviewReadRepository
{
    public async Task<IReadOnlyList<Review>> GetForDealerAsync(Guid dealerId, int page, int pageSize)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Pages are 1-based.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var from = (page - 1) * pageSize;

        // 1. Visible reviews: deleted_at is null is filtered IN THE QUERY (not in the view).
        var reviewsRes = await db.From<DealerReviewRow>()
            .Filter("dealer_id", Operator.Equals, dealerId.ToString())
            .Filter("deleted_at", Operator.Is, NullValue)
            .Order("created_at", Ordering.Descending)
            .Range(from, from + pageSize - 1)
            .Get();

        var reviewRows = reviewsRes.Models;
        if (reviewRows.Count == 0) return Array.Empty<Review>();

        // 2. All watches for this page in ONE request (with brand, dealer + owner).
        var watchIds = reviewRows.Select(r => r.WatchId.ToString()).Distinct().ToList();
        var watchesRes = await db.From<WatchRow>()
            .Filter("id", Operator.In, watchIds)
            .Get();

        var ctx = new MappingContext();
        var watches = watchesRes.Models.ToDictionary(w => w.Id, w => w.ToDomain(ctx));

        return reviewRows
            .Select(r => r.ToDomain(
                watches.TryGetValue(r.WatchId, out var w)
                    ? w
                    : throw new InvalidOperationException($"Watch {r.WatchId} for review {r.Id} was not returned."),
                ctx))
            .ToList();
    }

    // PostgREST "is.null": the client renders a null criterion with Operator.Is as "null".
    private static readonly object? NullValue = null;
}
