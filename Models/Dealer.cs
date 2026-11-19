namespace LuxWatch.Models;

/// <summary>
/// Represents a seller profile owned by a User in the marketplace.
/// </summary>
public class Dealer
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; private set; }
    public string Country { get; private set; }
    public DateOnly Since { get; private set; }
    public string ShipsWithin { get; private set; }
    public string RespondsWithin { get; private set; }
    public bool IsVerified { get; private set; }

    // TEMP (front-end only): remove once real data exists
    public int SeedWatchesSoldBaseline { get; init; }

    // TEMP (front-end only): remove once real data exists
    public int SeedActiveListingsBaseline { get; init; }

    // UML: 0..1 ── 1
    public User Owner { get; private set; }

    private readonly List<Watch> _watches = new();
    // UML: 1 ── 0..*
    public IReadOnlyList<Watch> Watches => _watches;

    public int WatchesSold => _watches.Count(w => w.Status == WatchStatus.Sold) + SeedWatchesSoldBaseline;

    public int ActiveListings => _watches.Count(w => w.Status == WatchStatus.Active) + SeedActiveListingsBaseline;

    public Dealer(
        User owner,
        string name,
        string country,
        DateOnly since,
        string shipsWithin,
        string respondsWithin,
        bool isVerified = false,
        int seedWatchesSoldBaseline = 0,
        int seedActiveListingsBaseline = 0)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Country = country ?? throw new ArgumentNullException(nameof(country));
        Since = since;
        ShipsWithin = shipsWithin;
        RespondsWithin = respondsWithin;
        IsVerified = isVerified;
        SeedWatchesSoldBaseline = seedWatchesSoldBaseline;
        SeedActiveListingsBaseline = seedActiveListingsBaseline;

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
        var reviews = _watches
            .SelectMany(w => w.Orders)
            .Select(o => o.Review)
            .Where(r => r != null)
            .ToList();

        return reviews.Count > 0 ? reviews.Average(r => r!.Overall) : 0.0;
    }

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
