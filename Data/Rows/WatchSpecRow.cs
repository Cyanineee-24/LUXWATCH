using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>public.watch_specs (owned by a watch; no back-reference, to avoid cycles).</summary>
[Table("watch_specs")]
public class WatchSpecRow : BaseModel
{
    [PrimaryKey("id", false)]  public Guid Id { get; set; }
    [Column("watch_id")]       public Guid WatchId { get; set; }
    [Column("key")]            public string Key { get; set; } = "";
    [Column("value")]          public string Value { get; set; } = "";
    [Column("display_order")]  public int DisplayOrder { get; set; }
}
