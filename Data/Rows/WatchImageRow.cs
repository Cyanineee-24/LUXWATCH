using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>public.watch_images (owned by a watch; no back-reference, to avoid cycles).</summary>
[Table("watch_images")]
public class WatchImageRow : BaseModel
{
    [PrimaryKey("id", false)]  public Guid Id { get; set; }
    [Column("watch_id")]       public Guid WatchId { get; set; }
    [Column("url")]            public string Url { get; set; } = "";
    [Column("alt_text")]       public string? AltText { get; set; }
    [Column("sort_order")]     public int SortOrder { get; set; }
}
