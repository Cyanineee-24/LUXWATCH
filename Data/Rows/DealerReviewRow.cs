using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>public.dealer_reviews view: READ ONLY. One row per review.</summary>
[Table("dealer_reviews")]
public class DealerReviewRow : BaseModel
{
    [PrimaryKey("id", false)]       public Guid Id { get; set; }
    [Column("order_id")]            public Guid OrderId { get; set; }
    [Column("overall")]             public int Overall { get; set; }
    [Column("shipping")]            public int Shipping { get; set; }
    [Column("item_as_described")]   public int ItemAsDescribed { get; set; }
    [Column("communication")]       public int Communication { get; set; }
    [Column("recommends")]          public bool Recommends { get; set; }
    [Column("comment")]             public string Comment { get; set; } = "";
    [Column("helpful_count")]       public int HelpfulCount { get; set; }
    [Column("created_at")]          public DateTime CreatedAt { get; set; }
    [Column("deleted_at")]          public DateTime? DeletedAt { get; set; }
    [Column("order_number")]        public string OrderNumber { get; set; } = "";
    [Column("order_status")]        public string OrderStatus { get; set; } = "";
    [Column("placed_at")]           public DateTime PlacedAt { get; set; }
    [Column("order_price_php")]     public decimal OrderPricePhp { get; set; }
    [Column("order_shipping_cost_php")] public decimal OrderShippingCostPhp { get; set; }
    [Column("buyer_id")]            public Guid BuyerId { get; set; }
    [Column("author_username")]     public string AuthorUsername { get; set; } = "";
    [Column("author_name")]         public string AuthorName { get; set; } = "";
    [Column("author_country")]      public string? AuthorCountry { get; set; }
    [Column("watch_id")]            public Guid WatchId { get; set; }
    [Column("watch_slug")]          public string WatchSlug { get; set; } = "";
    [Column("watch_model")]         public string WatchModel { get; set; } = "";
    [Column("dealer_id")]           public Guid DealerId { get; set; }
    [Column("brand_name")]          public string BrandName { get; set; } = "";
}
