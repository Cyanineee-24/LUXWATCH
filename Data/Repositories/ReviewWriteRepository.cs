using LuxWatch.Data.Rows;

namespace LuxWatch.Data.Repositories;

/// <summary>What the Rate Dealer form submits.</summary>
public sealed record NewReview(Guid OrderId, int Overall, int Shipping, int ItemAsDescribed,
                               int Communication, bool Recommends, string Comment);

/// <summary>
/// Review writes (deck Steps 7–8). Failures surface as exceptions; pages turn
/// them into messages with <see cref="SupabaseErrors.Map"/>.
///
/// RULE (slide 51): every update has a .Where on the id. An update without a
/// filter would target every row in the table.
/// The database only grants UPDATE on helpful_count and deleted_at, so every
/// update uses .Set(column, value).Update(), which sends just that column.
/// Never .Update(model): it sends every column and gets "permission denied".
/// </summary>
public interface IReviewWriteRepository
{
    /// <summary>Step 7 · Create: POST /reviews.</summary>
    Task CreateAsync(NewReview review);

    /// <summary>Step 8 · Update: PATCH helpful_count = currentCount + 1. Returns the new count.</summary>
    Task<int> MarkHelpfulAsync(Guid reviewId, int currentCount);

    /// <summary>Step 8 · Delete (soft): PATCH deleted_at = now().</summary>
    Task SoftDeleteAsync(Guid reviewId);

    /// <summary>Undo a soft delete: PATCH deleted_at = null.</summary>
    Task RestoreAsync(Guid reviewId);
}

public sealed class SupabaseReviewWriteRepository(Supabase.Client db) : IReviewWriteRepository
{
    public async Task CreateAsync(NewReview r)
    {
        // id, helpful_count, created_at and deleted_at are left to the DB defaults
        // ([PrimaryKey(…, false)] / ignoreOnInsert on ReviewRow).
        await db.From<ReviewRow>().Insert(new ReviewRow
        {
            OrderId = r.OrderId,
            Overall = r.Overall,
            Shipping = r.Shipping,
            ItemAsDescribed = r.ItemAsDescribed,
            Communication = r.Communication,
            Recommends = r.Recommends,
            Comment = r.Comment,
        });
    }

    public async Task<int> MarkHelpfulAsync(Guid reviewId, int currentCount)
    {
        var next = currentCount + 1;
        // Read-then-write: fine for class. An atomic increment would need an RPC function.
        // .Where on the id: never update without a filter.
        await db.From<ReviewRow>()
            .Where(r => r.Id == reviewId)
            .Set(r => r.HelpfulCount, next)
            .Update();
        return next;
    }

    public async Task SoftDeleteAsync(Guid reviewId)
    {
        // Soft delete (storage pattern): the row stays; reads filter deleted_at is null.
        // .Where on the id: never update without a filter.
        await db.From<ReviewRow>()
            .Where(r => r.Id == reviewId)
            .Set(r => r.DeletedAt!, DateTime.UtcNow)
            .Update();
    }

    public async Task RestoreAsync(Guid reviewId)
    {
        // Undo: clear deleted_at. Proves the soft-deleted row was never removed.
        // .Where on the id: never update without a filter.
        await db.From<ReviewRow>()
            .Where(r => r.Id == reviewId)
            .Set(r => r.DeletedAt!, null!)
            .Update();
    }
}
