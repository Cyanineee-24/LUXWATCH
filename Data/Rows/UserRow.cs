using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>public.users (no password column: login moves to Supabase Auth).</summary>
[Table("users")]
public class UserRow : BaseModel
{
    [PrimaryKey("id", false)]  public Guid Id { get; set; }
    [Column("username")]       public string Username { get; set; } = "";
    [Column("full_name")]      public string FullName { get; set; } = "";
    [Column("email")]          public string Email { get; set; } = "";
    [Column("country")]        public string? Country { get; set; }
    [Column("created_at")]     public DateTime CreatedAt { get; set; }
}
