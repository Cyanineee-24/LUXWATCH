using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>public.orders. Status stays a string; mapping parses it.</summary>
[Table("orders")]
public class OrderRow : BaseModel
{
    [PrimaryKey("id", false)]      public Guid Id { get; set; }
    [Column("order_number")]       public string OrderNumber { get; set; } = "";
    [Column("buyer_id")]           public Guid BuyerId { get; set; }
    [Column("watch_id")]           public Guid WatchId { get; set; }
    [Column("price_php")]          public decimal PricePhp { get; set; }
    [Column("shipping_cost_php")]  public decimal ShippingCostPhp { get; set; }
    [Column("placed_at")]          public DateTime PlacedAt { get; set; }
    [Column("status")]             public string Status { get; set; } = "";
}
