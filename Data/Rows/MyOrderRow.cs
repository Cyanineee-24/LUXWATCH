using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>
/// Read shape for "My orders": the order (with its own price snapshot) plus the
/// watch (slug, model, status, brand, images) and whether it has a review, all
/// embedded so ONE request loads a page of order cards. Read only.
///
/// includeInQuery: false on every reference, and the repository passes the
/// select string itself (<see cref="SelectColumns"/>): the client ignores the
/// join type on NESTED references and would emit watch_images!inner, which
/// silently drops an order whose watch has no images. The attributes are still
/// what maps the embedded JSON onto these properties.
/// </summary>
[Table("orders")]
public class MyOrderRow : BaseModel
{
    [PrimaryKey("id", false)]      public Guid Id { get; set; }
    [Column("order_number")]       public string OrderNumber { get; set; } = "";
    [Column("buyer_id")]           public Guid BuyerId { get; set; }
    [Column("price_php")]          public decimal PricePhp { get; set; }
    [Column("shipping_cost_php")]  public decimal ShippingCostPhp { get; set; }
    [Column("placed_at")]          public DateTime PlacedAt { get; set; }
    [Column("status")]             public string Status { get; set; } = "";

    public const string SelectColumns =
        "id,order_number,buyer_id,price_php,shipping_cost_php,placed_at,status," +
        "watches!inner(id,slug,model,status,brands!inner(id,name),watch_images(id,watch_id,url,alt_text,sort_order))," +
        "reviews(id)";

    [Reference(typeof(MyOrderWatchRow), ReferenceAttribute.JoinType.Inner, includeInQuery: false)]
    public MyOrderWatchRow? Watch { get; set; }

    // One-to-one (reviews.order_id is UNIQUE): an object or null. A soft-deleted
    // review still counts as "reviewed": one review per order, forever.
    [Reference(typeof(ReviewIdRow), ReferenceAttribute.JoinType.Left, includeInQuery: false)]
    public ReviewIdRow? Review { get; set; }
}

[Table("watches")]
public class MyOrderWatchRow : BaseModel
{
    [PrimaryKey("id", false)]  public Guid Id { get; set; }
    [Column("slug")]           public string Slug { get; set; } = "";
    [Column("model")]          public string Model { get; set; } = "";
    [Column("status")]         public string Status { get; set; } = "";

    [Reference(typeof(BrandNameRow), ReferenceAttribute.JoinType.Inner, includeInQuery: false)]
    public BrandNameRow? Brand { get; set; }

    [Reference(typeof(WatchImageRow), ReferenceAttribute.JoinType.Left, includeInQuery: false)]
    public List<WatchImageRow> Images { get; set; } = new();
}
