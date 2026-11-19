namespace LuxWatch.Models;

/// <summary>
/// Represents a category classification for watches (e.g. Diver, Chronograph).
/// </summary>
public class WatchType
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; private set; }
    public string Slug { get; private set; }
    public int DisplayOrder { get; set; }

    private readonly List<Watch> _watches = new();
    // UML: 0..* ── 0..*
    public IReadOnlyList<Watch> Watches => _watches;

    public WatchType(string name, int displayOrder = 0)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Slug = Brand.GenerateSlug(name);
        DisplayOrder = displayOrder;
    }

    public IReadOnlyList<Watch> GetWatches() => _watches;

    public void Rename(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Slug = Brand.GenerateSlug(name);
    }

    internal void AddWatchInternal(Watch watch)
    {
        if (!_watches.Contains(watch))
        {
            _watches.Add(watch);
        }
    }
}
