using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>
/// Row shape of public.brands as returned by the Supabase Data API.
/// Rows are mapped to the domain models in Models/ (Phase 3).
/// </summary>
[Table("brands")]
public class BrandRow : BaseModel
{
    [PrimaryKey("id", false)] public Guid Id { get; set; }
    [Column("name")]          public string Name { get; set; } = "";
    [Column("slug")]          public string Slug { get; set; } = "";
    [Column("display_order")] public int DisplayOrder { get; set; }
}
