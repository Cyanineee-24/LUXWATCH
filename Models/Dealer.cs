namespace LuxWatch.Models;

/// <summary>
/// Represents a seller profile owned by a User in the marketplace.
/// </summary>
public class Dealer
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; private set; }
    public string Country { get; private set; }
    public DateOnly Since { get; private set; }
    public string ShipsWithin { get; private set; }
    public string RespondsWithin { get; private set; }
    public bool IsVerified { get; private set; }

    // UML: 0..1 ── 1
    public User Owner { get; private set; }

    private readonly List<Watch> _watches = new();
    // UML: 1 ── 0..*
    public IReadOnlyList<Watch> Watches => _watches;

    /// <summary>
    /// Derived numbers computed by the database (dealer_stats view). When set,
    /// the counts and ratings below come from here instead of from whatever
    /// watches/orders happen to be loaded in memory.
    /// </summary>
    public DealerStats? Stats { get; set; }

    public int WatchesSold => Stats?.WatchesSold ?? _watches.Count(w => w.Status == WatchStatus.Sold);

    public int ActiveListings => Stats?.ActiveListings ?? _watches.Count(w => w.Status == WatchStatus.Active);

    public int ReviewCount => Stats?.ReviewCount ?? LoadedReviews().Count;

    public Dealer(
        User owner,
        string name,
        string country,
        DateOnly since,
        string shipsWithin,
        string respondsWithin,
        bool isVerified = false)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Country = country ?? throw new ArgumentNullException(nameof(country));
        Since = since;
        ShipsWithin = shipsWithin;
        RespondsWithin = respondsWithin;
        IsVerified = isVerified;

        owner.Dealer = this;
    }

    public void CreateListing(Watch draft)
    {
        if (draft.Dealer != this)
        {
            throw new InvalidOperationException("Draft watch belongs to a different dealer.");
        }
        if (!_watches.Contains(draft))
        {
            _watches.Add(draft);
        }
    }

    public double GetAverageRating()
    {
        if (Stats is not null)
        {
            return Stats.AvgOverall is decimal avg ? (double)avg : 0.0;
        }

        var reviews = LoadedReviews();
        return reviews.Count > 0 ? reviews.Average(r => r.Overall) : 0.0;
    }

    private List<Review> LoadedReviews() => _watches
        .SelectMany(w => w.Orders)
        .Select(o => o.Review)
        .OfType<Review>()
        .ToList();

    public void Verify()
    {
        IsVerified = true;
    }

    internal void AddWatch(Watch watch)
    {
        if (!_watches.Contains(watch))
        {
            _watches.Add(watch);
        }
    }
}
