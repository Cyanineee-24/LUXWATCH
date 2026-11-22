namespace LuxWatch.Services;

/// <summary>
/// One "Helpful" vote per review per browser session (in memory; resets on reload).
/// TEMP until Supabase Auth: becomes a review_helpful (user_id, review_id) table
/// with a unique key, so the database enforces one vote per user.
/// </summary>
public sealed class HelpfulVoteTracker
{
    private readonly HashSet<Guid> voted = new();

    public bool HasVoted(Guid reviewId) => voted.Contains(reviewId);

    public void MarkVoted(Guid reviewId) => voted.Add(reviewId);
}
