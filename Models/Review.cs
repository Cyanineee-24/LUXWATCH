namespace LuxWatch.Models;

/// <summary>
/// Represents a buyer's review and rating tied to a completed order trade.
/// </summary>
public class Review
{
    public Guid Id { get; } = Guid.NewGuid();

    public int Overall { get; private set; }
    public int Shipping { get; private set; }
    public int ItemAsDescribed { get; private set; }
    public int Communication { get; private set; }
    public bool Recommends { get; private set; }
    public string Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // TEMP (front-end only): remove once real data exists
    public int SeedHelpfulBaseline { get; init; }

    // UML: 0..1 ── 1
    public Order Order { get; private set; }

    private readonly List<User> _helpfulVoters = new();
    // UML: 0..* ── 0..*
    public IReadOnlyList<User> HelpfulVoters => _helpfulVoters;

    public User Author => Order.Buyer;

    public string Country => Order.Buyer.Country;

    public Dealer Dealer => Order.Dealer;

    public string WatchPurchased => string.IsNullOrWhiteSpace(Order.Watch.Reference)
        ? $"{Order.Watch.Brand.Name} {Order.Watch.Model}".Trim()
        : $"{Order.Watch.Brand.Name} {Order.Watch.Model} {Order.Watch.Reference}".Trim();

    public int Helpful => _helpfulVoters.Count + SeedHelpfulBaseline;

    public Review(
        Order order,
        int overall,
        int shipping,
        int itemAsDescribed,
        int communication,
        bool recommends,
        string comment,
        DateTime? createdAt = null,
        int seedHelpfulBaseline = 0)
    {
        Order = order ?? throw new ArgumentNullException(nameof(order));
        Overall = overall;
        Shipping = shipping;
        ItemAsDescribed = itemAsDescribed;
        Communication = communication;
        Recommends = recommends;
        Comment = comment;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        SeedHelpfulBaseline = seedHelpfulBaseline;

        if (!IsValid())
        {
            throw new ArgumentException("Review data is invalid or order status is not Delivered or Completed.");
        }

        order.AttachReview(this);
    }

    public bool IsValid()
    {
        if (Overall < 1 || Overall > 5) return false;
        if (Shipping < 1 || Shipping > 5) return false;
        if (ItemAsDescribed < 1 || ItemAsDescribed > 5) return false;
        if (Communication < 1 || Communication > 5) return false;

        var trimmed = Comment?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length < 10 || trimmed.Length > 500) return false;

        if (Order == null || (Order.Status != OrderStatus.Delivered && Order.Status != OrderStatus.Completed))
        {
            return false;
        }

        return true;
    }

    public bool AddHelpfulVote(User user)
    {
        if (_helpfulVoters.Contains(user))
        {
            return false;
        }

        _helpfulVoters.Add(user);
        user.AddHelpfulReview(this);
        return true;
    }
}
