namespace LuxWatch.Models;

/// <summary>
/// Represents a single physical luxury watch listing on the marketplace.
/// </summary>
public class Watch
{
    public const int PopularThreshold = 1000;

    public Guid Id { get; } = Guid.NewGuid();

    public string Slug { get; private set; }
    public string Model { get; private set; }
    public string Reference { get; private set; }
    public string Subtitle { get; private set; }
    public string Description { get; private set; }
    public Condition Condition { get; private set; }
    public string Year { get; private set; }
    public bool HasBox { get; private set; }
    public bool HasPapers { get; private set; }
    public decimal PricePhp { get; private set; }
    public decimal ShippingCostPhp { get; private set; }
    public string LocationCountryCode { get; private set; }
    public int ViewsIn48h { get; private set; }
    public DateOnly ListedDate { get; private set; }
    public string DeliveryWindow { get; private set; }
    public WatchStatus Status { get; internal set; }

    // UML: 0..* ── 1
    public Brand Brand { get; private set; }

    // UML: 0..* ── 1
    public Dealer Dealer { get; private set; }

    private readonly List<WatchType> _types = new();
    // UML: 0..* ── 0..*
    public IReadOnlyList<WatchType> Types => _types;

    private readonly List<WatchImage> _images = new();
    // UML: 1 ── 1..*
    public IReadOnlyList<WatchImage> Images => _images;

    private readonly List<WatchSpec> _specs = new();
    // UML: 1 ── 0..*
    public IReadOnlyList<WatchSpec> Specs => _specs;

    private readonly List<Order> _orders = new();
    // UML: 1 ── 0..*
    public IReadOnlyList<Order> Orders => _orders;

    private readonly List<Favorite> _favorites = new();
    // UML: 1 ── 0..*
    public IReadOnlyList<Favorite> Favorites => _favorites;

    // UML: 0..1 ── 0..1
    public CollectionItem? CollectionItem { get; internal set; }

    public bool IsPopular => ViewsIn48h > PopularThreshold;

    public Watch(
        Brand brand,
        Dealer dealer,
        string slug,
        string model,
        string reference,
        string subtitle,
        string description,
        Condition condition,
        string year,
        bool hasBox,
        bool hasPapers,
        decimal pricePhp,
        decimal shippingCostPhp,
        string locationCountryCode,
        DateOnly listedDate,
        string deliveryWindow,
        int viewsIn48h = 0,
        WatchStatus status = WatchStatus.Draft)
    {
        Brand = brand ?? throw new ArgumentNullException(nameof(brand));
        Dealer = dealer ?? throw new ArgumentNullException(nameof(dealer));
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        Subtitle = subtitle;
        Description = description;
        Condition = condition;
        Year = year;
        HasBox = hasBox;
        HasPapers = hasPapers;
        PricePhp = pricePhp;
        ShippingCostPhp = shippingCostPhp;
        LocationCountryCode = locationCountryCode;
        ListedDate = listedDate;
        DeliveryWindow = deliveryWindow;
        ViewsIn48h = viewsIn48h;
        Status = status;

        brand.AddWatch(this);
        dealer.AddWatch(this);
    }

    public void ChangePrice(decimal newPricePhp)
    {
        if (newPricePhp <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newPricePhp), "Price must be greater than zero.");
        }
        if (Status == WatchStatus.Sold)
        {
            throw new InvalidOperationException("Cannot change price of a sold watch.");
        }
        PricePhp = newPricePhp;
    }

    public void SubmitForReview()
    {
        if (Status != WatchStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot submit watch for review in state '{Status}'. Must be Draft.");
        }
        if (_images.Count == 0)
        {
            throw new InvalidOperationException("Watch must have at least one image before submission.");
        }
        Status = WatchStatus.PendingReview;
    }

    public bool TryReserve(out string? error)
    {
        if (Status != WatchStatus.Active)
        {
            error = $"Cannot reserve watch in state '{Status}'. Watch must be Active.";
            return false;
        }
        Status = WatchStatus.Reserved;
        error = null;
        return true;
    }

    public void MarkSold()
    {
        if (Status != WatchStatus.Reserved)
        {
            throw new InvalidOperationException($"Cannot mark watch as sold in state '{Status}'. Must be Reserved.");
        }
        Status = WatchStatus.Sold;
    }

    public void RecordView()
    {
        ViewsIn48h++;
    }

    public WatchImage AddImage(string url, string altText = "")
    {
        var image = new WatchImage(this, url, altText, _images.Count);
        _images.Add(image);
        return image;
    }

    public WatchSpec AddSpec(string key, string value)
    {
        var spec = new WatchSpec(this, key, value, _specs.Count);
        _specs.Add(spec);
        return spec;
    }

    public void AddType(WatchType type)
    {
        if (!_types.Contains(type))
        {
            _types.Add(type);
            type.AddWatchInternal(this);
        }
    }

    internal void Release()
    {
        if (Status == WatchStatus.Reserved)
        {
            Status = WatchStatus.Active;
        }
    }

    internal void SetPrimaryImage(WatchImage primary)
    {
        primary.SortOrder = 0;
        int next = 1;
        foreach (var img in _images.Where(i => i != primary).OrderBy(i => i.SortOrder))
        {
            img.SortOrder = next++;
        }
        _images.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
    }

    internal void AddOrder(Order order)
    {
        if (!_orders.Contains(order))
        {
            _orders.Add(order);
        }
    }

    internal void AddFavorite(Favorite favorite)
    {
        if (!_favorites.Contains(favorite))
        {
            _favorites.Add(favorite);
        }
    }

    internal void RemoveFavorite(Favorite favorite)
    {
        _favorites.Remove(favorite);
    }
}
