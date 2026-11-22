using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>
/// public.reviews: used for WRITES (Phase 5). No references.
/// Insert: helpful_count / created_at / deleted_at are left to the DB defaults.
/// Update: only helpful_count and deleted_at are granted, so Phase 5 must use
///   .Set(x => x.HelpfulCount, …) / .Set(x => x.DeletedAt, …).Update(),
///   never .Update(wholeRow) (that sends every column → permission denied).
/// </summary>
[Table("reviews")]
public class ReviewRow : BaseModel
{
    [PrimaryKey("id", false)]       public Guid Id { get; set; }
    [Column("order_id")]            public Guid OrderId { get; set; }
    [Column("overall")]             public int Overall { get; set; }
    [Column("shipping")]            public int Shipping { get; set; }
    [Column("item_as_described")]   public int ItemAsDescribed { get; set; }
    [Column("communication")]       public int Communication { get; set; }
    [Column("recommends")]          public bool Recommends { get; set; }
    [Column("comment")]             public string Comment { get; set; } = "";
    [Column("helpful_count", ignoreOnInsert: true)] public int HelpfulCount { get; set; }
    [Column("created_at", ignoreOnInsert: true)]    public DateTime CreatedAt { get; set; }
    [Column("deleted_at", ignoreOnInsert: true)]    public DateTime? DeletedAt { get; set; }
}
