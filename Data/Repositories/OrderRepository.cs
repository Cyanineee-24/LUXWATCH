using LuxWatch.Data.Rows;
using static Supabase.Postgrest.Constants;

namespace LuxWatch.Data.Repositories;

/// <summary>An order the current user may still review (read model for the Rate Dealer picker).</summary>
public sealed record ReviewableOrder(Guid OrderId, string OrderNumber, DateTime PlacedAt, string BrandName, string WatchModel, string WatchSlug);

public interface IOrderRepository
{
    /// <summary>
    /// The buyer's Delivered/Completed orders on this dealer's watches that have
    /// no review yet, newest first. ONE request (embed + anti-join).
    /// </summary>
    Task<IReadOnlyList<ReviewableOrder>> GetReviewableAsync(Guid buyerId, Guid dealerId);
}

public sealed class SupabaseOrderRepository(Supabase.Client db) : IOrderRepository
{
    public async Task<IReadOnlyList<ReviewableOrder>> GetReviewableAsync(Guid buyerId, Guid dealerId)
    {
        // orders?select=*,watches!inner(id,slug,model,dealer_id,brands!inner(id,name)),reviews!left(id)
        //       &buyer_id=eq.…&watches.dealer_id=eq.…&status=in.(Delivered,Completed)&reviews=is.null
        var res = await db.From<ReviewableOrderRow>()
            .Filter("buyer_id", Operator.Equals, buyerId.ToString())
            .Filter("watches.dealer_id", Operator.Equals, dealerId.ToString())
            .Filter("status", Operator.In, new List<string> { "Delivered", "Completed" })
            .Filter("reviews", Operator.Is, NullValue)
            .Order("placed_at", Ordering.Descending)
            .Get();

        return res.Models
            .Where(o => o.Watch is not null && o.Review is null)   // defensive; the database already filtered
            .Select(o => new ReviewableOrder(o.Id, o.OrderNumber, o.PlacedAt,
                                             o.Watch!.Brand?.Name ?? "", o.Watch.Model, o.Watch.Slug))
            .ToList();
    }

    private static readonly object? NullValue = null;
}
