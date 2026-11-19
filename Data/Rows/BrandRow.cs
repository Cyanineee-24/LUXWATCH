using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>public.brands. Only repositories use row classes; pages see Models/.</summary>
[Table("brands")]
public class BrandRow : BaseModel
{
    [PrimaryKey("id", false)]  public Guid Id { get; set; }
    [Column("name")]           public string Name { get; set; } = "";
    [Column("slug")]           public string Slug { get; set; } = "";
    [Column("logo_url")]       public string? LogoUrl { get; set; }
    [Column("description")]    public string? Description { get; set; }
    [Column("is_featured")]    public bool IsFeatured { get; set; }
    [Column("display_order")]  public int DisplayOrder { get; set; }
}
