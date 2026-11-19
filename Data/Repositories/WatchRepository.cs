using LuxWatch.Data.Mapping;
using LuxWatch.Data.Rows;
using LuxWatch.Models;
using static Supabase.Postgrest.Constants;

namespace LuxWatch.Data.Repositories;

/// <summary>One page of listings. HasNextPage comes from fetching pageSize + 1 rows.</summary>
public sealed record WatchPage(IReadOnlyList<Watch> Items, bool HasNextPage);

public interface IWatchRepository
{
    /// <summary>Active listings, newest first, one page (1-based). One request, no separate count.</summary>
    Task<WatchPage> GetActiveAsync(int page, int pageSize);

    /// <summary>One watch with brand, dealer (+ owner), images and specs, in one request.</summary>
    Task<Watch?> GetBySlugAsync(string slug);
}

public sealed class SupabaseWatchRepository(Supabase.Client db) : IWatchRepository
{
    public async Task<WatchPage> GetActiveAsync(int page, int pageSize)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Pages are 1-based.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var from = (page - 1) * pageSize;
        var to = from + pageSize;              // inclusive → pageSize + 1 rows: the extra one = "has next page"

        // Filter, sort and page in the database; uses the (status, listed_date desc) index.
        var res = await db.From<WatchRow>()
            .Filter("status", Operator.Equals, "Active")
            .Order("listed_date", Ordering.Descending)
            .Order("slug", Ordering.Ascending)   // tie-breaker: stable paging when dates are equal
            .Range(from, to)
            .Get();

        var rows = res.Models;
        var hasNext = rows.Count > pageSize;
        var ctx = new MappingContext();
        var items = rows.Take(pageSize).Select(r => r.ToDomain(ctx)).ToList();
        return new WatchPage(items, hasNext);
    }

    public async Task<Watch?> GetBySlugAsync(string slug)
    {
        var res = await db.From<WatchRow>()
            .Filter("slug", Operator.Equals, slug)
            .Limit(1)
            .Get();

        return res.Models.FirstOrDefault()?.ToDomain();
    }
}
