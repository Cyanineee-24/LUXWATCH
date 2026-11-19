using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>
/// public.watches. Brand, dealer (with owner), images and specs are embedded via
/// [Reference], so ONE .Get() returns everything the detail page needs (no N+1).
/// [Reference] defaults to IgnoreOnInsert/IgnoreOnUpdate = true: never written.
/// Status/Condition stay strings here; mapping parses them with Enum.Parse.
/// </summary>
[Table("watches")]
public class WatchRow : BaseModel
{
    [PrimaryKey("id", false)]         public Guid Id { get; set; }
    [Column("slug")]                  public string Slug { get; set; } = "";
    [Column("brand_id")]              public Guid BrandId { get; set; }
    [Column("dealer_id")]             public Guid DealerId { get; set; }
    [Column("model")]                 public string Model { get; set; } = "";
    [Column("reference")]             public string? Reference { get; set; }
    [Column("subtitle")]              public string? Subtitle { get; set; }
    [Column("description")]           public string? Description { get; set; }
    [Column("condition")]             public string Condition { get; set; } = "";
    [Column("year")]                  public int? Year { get; set; }
    [Column("year_is_approximate")]   public bool YearIsApproximate { get; set; }
    [Column("has_box")]               public bool HasBox { get; set; }
    [Column("has_papers")]            public bool HasPapers { get; set; }
    [Column("price_php")]             public decimal PricePhp { get; set; }
    [Column("shipping_cost_php")]     public decimal ShippingCostPhp { get; set; }
    [Column("location_country_code")] public string? LocationCountryCode { get; set; }
    [Column("views_in_48h")]          public int ViewsIn48h { get; set; }
    [Column("listed_date")]           public DateTime ListedDate { get; set; }   // date → DateOnly in mapping
    [Column("delivery_window")]       public string? DeliveryWindow { get; set; }
    [Column("status")]                public string Status { get; set; } = "";

    // Brand/dealer are NOT NULL FKs, so INNER is safe. Images/specs MUST be LEFT:
    // the client's default is !inner, which silently drops a watch that has no
    // specs or no images (e.g. the Sold watches have no specs).
    [Reference(typeof(BrandRow), ReferenceAttribute.JoinType.Inner)]
    public BrandRow? Brand { get; set; }

    [Reference(typeof(DealerRow), ReferenceAttribute.JoinType.Inner)]
    public DealerRow? Dealer { get; set; }

    [Reference(typeof(WatchImageRow), ReferenceAttribute.JoinType.Left)]
    public List<WatchImageRow> Images { get; set; } = new();

    [Reference(typeof(WatchSpecRow), ReferenceAttribute.JoinType.Left)]
    public List<WatchSpecRow> Specs { get; set; } = new();
}
