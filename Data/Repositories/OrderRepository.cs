using LuxWatch.Data.Rows;
using LuxWatch.Models;
using LuxWatch.Services;
using static Supabase.Postgrest.Constants;

namespace LuxWatch.Data.Repositories;

/// <summary>An order the current user may still review (read model for the Rate Dealer picker).</summary>
public sealed record ReviewableOrder(Guid OrderId, string OrderNumber, DateTime PlacedAt, string BrandName, string WatchModel, string WatchSlug);

/// <summary>A card on "My orders" (read model). Prices are the ORDER's snapshot.</summary>
public sealed record MyOrder(
    Guid OrderId,
    string OrderNumber,
    DateTime PlacedAt,
    OrderStatus Status,
    decimal PricePhp,
    decimal ShippingCostPhp,
    string WatchSlug,
    string BrandName,
    string WatchModel,
    WatchStatus WatchStatus,
    string? ImageUrl,
    bool IsReviewed)
{
    public decimal TotalPhp => PricePhp + ShippingCostPhp;
}

/// <summary>One page of the current user's orders. HasNextPage comes from fetching pageSize + 1 rows.</summary>
public sealed record MyOrderPage(IReadOnlyList<MyOrder> Items, bool HasNextPage);

/// <summary>Result of place/cancel/confirm: the order row the database function returned.</summary>
public sealed record OrderResult(Guid OrderId, string OrderNumber, OrderStatus Status);

public interface IOrderRepository
{
    /// <summary>
    /// The buyer's Delivered/Completed orders on this dealer's watches that have
    /// no review yet, newest first. ONE request (embed + anti-join).
    /// </summary>
    Task<IReadOnlyList<ReviewableOrder>> GetReviewableAsync(Guid buyerId, Guid dealerId);

    /// <summary>The current user's orders, newest first, one page (1-based). ONE request.</summary>
    Task<MyOrderPage> GetMineAsync(int page, int pageSize);

    /// <summary>rpc/place_order: Active watch → PendingPayment order, watch Reserved.</summary>
    Task<OrderResult> PlaceAsync(Guid watchId);

    /// <summary>rpc/cancel_order: order Cancelled, watch back to Active.</summary>
    Task<OrderResult> CancelAsync(Guid orderId);

    /// <summary>rpc/confirm_delivery (demo shortcut): order Completed, watch Sold.</summary>
    Task<OrderResult> ConfirmDeliveryAsync(Guid orderId);
}

/// <summary>
/// Order writes go through database FUNCTIONS (POST /rpc/…), never through
/// INSERT/UPDATE on orders or watches: anon has no write grant on those tables.
/// The functions validate, lock rows and copy the price from the database.
/// Failures surface as exceptions; pages map them with SupabaseErrors.MapOrder.
/// </summary>
public sealed class SupabaseOrderRepository(Supabase.Client db, ICurrentUserService currentUser) : IOrderRepository
{
    public async Task<IReadOnlyList<ReviewableOrder>> GetReviewableAsync(Guid buyerId, Guid dealerId)
    {
        // orders?select=*,watches!inner(id,slug,model,dealer_id,brands!inner(id,name)),reviews(id)
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

    public async Task<MyOrderPage> GetMineAsync(int page, int pageSize)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Pages are 1-based.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var me = await RequireUserAsync();
        var from = (page - 1) * pageSize;

        // orders?select=…,watches!inner(…,brands!inner(id,name),watch_images(…)),reviews(id)
        //       &buyer_id=eq.…&order=placed_at.desc&offset/limit = pageSize + 1 rows
        var res = await db.From<MyOrderRow>()
            .Select(MyOrderRow.SelectColumns)       // explicit: images must be a LEFT embed
            .Filter("buyer_id", Operator.Equals, me.Id.ToString())
            .Order("placed_at", Ordering.Descending)
            .Order("order_number", Ordering.Descending)   // tie-breaker: stable paging
            .Range(from, from + pageSize)
            .Get();

        var rows = res.Models;
        var items = rows.Take(pageSize).Select(o => new MyOrder(
            o.Id,
            o.OrderNumber,
            o.PlacedAt,
            ParseEnum<OrderStatus>(o.Status, "orders.status"),
            o.PricePhp,
            o.ShippingCostPhp,
            o.Watch?.Slug ?? "",
            o.Watch?.Brand?.Name ?? "",
            o.Watch?.Model ?? "",
            ParseEnum<WatchStatus>(o.Watch?.Status ?? "", "watches.status"),
            o.Watch?.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url,
            o.Review is not null)).ToList();

        return new MyOrderPage(items, rows.Count > pageSize);
    }

    public async Task<OrderResult> PlaceAsync(Guid watchId)
    {
        var me = await RequireUserAsync();
        // TODO (auth): the database will use auth.uid() instead of p_buyer_id.
        var row = await db.Rpc<OrderRow>("place_order", new Dictionary<string, object>
        {
            ["p_watch_id"] = watchId,
            ["p_buyer_id"] = me.Id,
        });
        return ToResult(row);
    }

    public async Task<OrderResult> CancelAsync(Guid orderId) =>
        ToResult(await CallOrderFunctionAsync("cancel_order", orderId));

    public async Task<OrderResult> ConfirmDeliveryAsync(Guid orderId) =>
        ToResult(await CallOrderFunctionAsync("confirm_delivery", orderId));

    private async Task<OrderRow?> CallOrderFunctionAsync(string function, Guid orderId)
    {
        var me = await RequireUserAsync();
        return await db.Rpc<OrderRow>(function, new Dictionary<string, object>
        {
            ["p_order_id"] = orderId,
            ["p_buyer_id"] = me.Id,
        });
    }

    private async Task<User> RequireUserAsync() =>
        await currentUser.GetAsync()
        ?? throw new InvalidOperationException("The demo user (Demo:CurrentUsername) was not found in the database.");

    private static OrderResult ToResult(OrderRow? row) =>
        row is null
            ? throw new InvalidOperationException("The database function returned no order.")
            : new OrderResult(row.Id, row.OrderNumber, ParseEnum<OrderStatus>(row.Status, "orders.status"));

    private static T ParseEnum<T>(string value, string column) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: false, out var result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidOperationException($"Unknown {typeof(T).Name} '{value}' in column '{column}'.");

    private static readonly object? NullValue = null;
}
