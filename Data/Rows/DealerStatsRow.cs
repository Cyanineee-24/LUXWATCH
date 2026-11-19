using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>public.dealer_stats view: READ ONLY. One row per dealer.</summary>
[Table("dealer_stats")]
public class DealerStatsRow : BaseModel
{
    [PrimaryKey("dealer_id", false)]   public Guid DealerId { get; set; }
    [Column("active_listings")]        public int ActiveListings { get; set; }
    [Column("watches_sold")]           public int WatchesSold { get; set; }
    [Column("review_count")]           public int ReviewCount { get; set; }
    [Column("avg_overall")]            public decimal? AvgOverall { get; set; }
    [Column("avg_shipping")]           public decimal? AvgShipping { get; set; }
    [Column("avg_item_as_described")]  public decimal? AvgItemAsDescribed { get; set; }
    [Column("avg_communication")]      public decimal? AvgCommunication { get; set; }
    [Column("recommend_pct")]          public int? RecommendPct { get; set; }
}
