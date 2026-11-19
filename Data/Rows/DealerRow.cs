using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace LuxWatch.Data.Rows;

/// <summary>
/// public.dealers. The owning user is embedded in the same request
/// (dealers.user_id → users), because the domain Dealer requires its owner.
/// [Reference] defaults to IgnoreOnInsert/IgnoreOnUpdate = true: never written.
/// </summary>
[Table("dealers")]
public class DealerRow : BaseModel
{
    [PrimaryKey("id", false)]   public Guid Id { get; set; }
    [Column("user_id")]         public Guid UserId { get; set; }
    [Column("name")]            public string Name { get; set; } = "";
    [Column("country")]         public string Country { get; set; } = "";
    [Column("since")]           public DateTime Since { get; set; }          // date → DateOnly in mapping
    [Column("ships_within")]    public string? ShipsWithin { get; set; }
    [Column("responds_within")] public string? RespondsWithin { get; set; }
    [Column("is_verified")]     public bool IsVerified { get; set; }
    [Column("created_at")]      public DateTime CreatedAt { get; set; }

    // user_id is a NOT NULL FK, so INNER is safe here.
    [Reference(typeof(UserRow), ReferenceAttribute.JoinType.Inner)]
    public UserRow? Owner { get; set; }
}
