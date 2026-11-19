namespace LuxWatch.Models;

/// <summary>
/// Represents a user's saved watch (association class between User and Watch).
/// </summary>
public class Favorite
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime SavedAt { get; private set; }

    // UML: 0..* ── 1
    public User User { get; private set; }

    // UML: 0..* ── 1
    public Watch Watch { get; private set; }

    public Favorite(User user, Watch watch, DateTime? savedAt = null)
    {
        User = user ?? throw new ArgumentNullException(nameof(user));
        Watch = watch ?? throw new ArgumentNullException(nameof(watch));
        SavedAt = savedAt ?? DateTime.UtcNow;
    }

    public void Remove()
    {
        User.RemoveFavoriteInternal(this);
        Watch.RemoveFavorite(this);
    }
}
