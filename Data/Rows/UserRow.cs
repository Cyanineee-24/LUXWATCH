using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>
/// public.users, public columns only. anon has a COLUMN-level select grant
/// (id, username, full_name, country, created_at): email is not readable with
/// the publishable key, and there is no password column (Supabase Auth).
///
/// When embedded via [Reference] the client names these columns explicitly
/// (users!inner(id,username,full_name,country,created_at)). Never add a column
/// here that anon can't read, and never query db.From&lt;UserRow&gt;() without an
/// explicit .Select("id,username,full_name,country,created_at"): a top-level
/// query defaults to select=*, which is refused once email is hidden.
/// </summary>
[Table("users")]
public class UserRow : BaseModel
{
    [PrimaryKey("id", false)]  public Guid Id { get; set; }
    [Column("username")]       public string Username { get; set; } = "";
    [Column("full_name")]      public string FullName { get; set; } = "";
    [Column("country")]        public string? Country { get; set; }
    [Column("created_at")]     public DateTime CreatedAt { get; set; }
}
