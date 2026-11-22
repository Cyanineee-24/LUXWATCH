using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>
/// Read shape for "orders the current user can still review" (IOrderRepository).
/// One request: orders + embedded watch (with brand) + embedded review, where
/// the review embed is used as an ANTI-JOIN (reviews=is.null → no review yet).
/// Read only; [Reference] is never sent on writes.
/// </summary>
[Table("orders")]
public class ReviewableOrderRow : BaseModel
{
    [PrimaryKey("id", false)]  public Guid Id { get; set; }
    [Column("order_number")]   public string OrderNumber { get; set; } = "";
    [Column("placed_at")]      public DateTime PlacedAt { get; set; }
    [Column("status")]         public string Status { get; set; } = "";

    // INNER: also filters orders to the dealer via watches.dealer_id=eq.…
    [Reference(typeof(ReviewableWatchRow), ReferenceAttribute.JoinType.Inner)]
    public ReviewableWatchRow? Watch { get; set; }

    // LEFT + reviews=is.null: keep only orders WITHOUT a review (soft-deleted
    // reviews still count: one review per order, forever).
    [Reference(typeof(ReviewIdRow), ReferenceAttribute.JoinType.Left)]
    public ReviewIdRow? Review { get; set; }
}

[Table("watches")]
public class ReviewableWatchRow : BaseModel
{
    [PrimaryKey("id", false)]  public Guid Id { get; set; }
    [Column("slug")]           public string Slug { get; set; } = "";
    [Column("model")]          public string Model { get; set; } = "";
    [Column("dealer_id")]      public Guid DealerId { get; set; }

    [Reference(typeof(BrandNameRow), ReferenceAttribute.JoinType.Inner)]
    public BrandNameRow? Brand { get; set; }
}

[Table("brands")]
public class BrandNameRow : BaseModel
{
    [PrimaryKey("id", false)]  public Guid Id { get; set; }
    [Column("name")]           public string Name { get; set; } = "";
}

[Table("reviews")]
public class ReviewIdRow : BaseModel
{
    [PrimaryKey("id", false)]  public Guid Id { get; set; }
}
