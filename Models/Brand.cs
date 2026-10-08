namespace LuxWatch.Models;

/// <summary>
/// Represents a luxury watch brand/manufacturer in the marketplace.
/// </summary>
public class Brand
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? LogoUrl { get; private set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public string Description { get; private set; }

    private readonly List<Watch> _watches = new();
    // UML: 1 ── 0..*
    public IReadOnlyList<Watch> Watches => _watches;

    private readonly List<CollectionItem> _collectionItems = new();
    // UML: 1 ── 0..*
    public IReadOnlyList<CollectionItem> CollectionItems => _collectionItems;

    public Brand(string name, string? logoUrl = null, bool isFeatured = false, int displayOrder = 0, string description = "")
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Slug = GenerateSlug(name);
        LogoUrl = logoUrl;
        IsFeatured = isFeatured;
        DisplayOrder = displayOrder;
        Description = description;
    }

    public IReadOnlyList<Watch> GetActiveWatches() => _watches.Where(w => w.Status == WatchStatus.Active).ToList();

    public void UpdateDescription(string text) => Description = text;

    internal static string GenerateSlug(string name) => name.ToLower().Replace(" ", "-");

    internal void AddWatch(Watch watch)
    {
        if (!_watches.Contains(watch))
        {
            _watches.Add(watch);
        }
    }

    internal void AddCollectionItem(CollectionItem item)
    {
        if (!_collectionItems.Contains(item))
        {
            _collectionItems.Add(item);
        }
    }
}
