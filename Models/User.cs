using System.Security.Cryptography;
using System.Text;

namespace LuxWatch.Models;

/// <summary>
/// Represents a registered user account (collector, buyer, or dealer owner).
/// </summary>
public class User
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Username { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Country { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // UML: 1 ── 0..1
    public Dealer? Dealer { get; internal set; }

    private readonly List<Favorite> _favorites = new();
    // UML: 1 ── 0..*
    public IReadOnlyList<Favorite> Favorites => _favorites;

    private readonly List<Order> _orders = new();
    // UML: 1 ── 0..*
    public IReadOnlyList<Order> Orders => _orders;

    private readonly List<CollectionItem> _collectionItems = new();
    // UML: 1 ── 0..*
    public IReadOnlyList<CollectionItem> CollectionItems => _collectionItems;

    private readonly List<Review> _markedHelpful = new();
    // UML: 0..* ── 0..*
    public IReadOnlyList<Review> MarkedHelpful => _markedHelpful;

    public User(string username, string fullName, string email, string passwordHash, string country, DateTime? createdAt = null)
    {
        Username = username ?? throw new ArgumentNullException(nameof(username));
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        Country = country ?? throw new ArgumentNullException(nameof(country));
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    // TODO: real auth
    public bool Login(string pw)
    {
        return HashPassword(pw) == PasswordHash;
    }

    internal static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    public Dealer BecomeDealer(string name, string? country = null, string shipsWithin = "Usually ships within 24 hours", string respondsWithin = "Replies within 24 hours")
    {
        if (Dealer != null)
        {
            throw new InvalidOperationException("User already has an associated dealer profile.");
        }
        var dealer = new Dealer(this, name, country ?? Country, DateOnly.FromDateTime(DateTime.Today), shipsWithin, respondsWithin);
        Dealer = dealer;
        return dealer;
    }

    public Favorite AddFavorite(Watch watch)
    {
        var existing = _favorites.FirstOrDefault(f => f.Watch == watch);
        if (existing != null)
        {
            return existing;
        }

        var favorite = new Favorite(this, watch);
        _favorites.Add(favorite);
        watch.AddFavorite(favorite);
        return favorite;
    }

    public Order PlaceOrder(Watch watch)
    {
        if (!watch.TryReserve(out var error))
        {
            throw new InvalidOperationException(error ?? "Cannot reserve watch to place order.");
        }

        var orderNumber = $"LW-{DateTime.UtcNow.Year}-{Random.Shared.Next(1000, 9999)}";
        var order = new Order(this, watch, orderNumber);
        _orders.Add(order);
        watch.AddOrder(order);
        return order;
    }

    public void MarkHelpful(Review review)
    {
        review.AddHelpfulVote(this);
    }

    internal void AddHelpfulReview(Review review)
    {
        if (!_markedHelpful.Contains(review))
        {
            _markedHelpful.Add(review);
        }
    }

    internal void RemoveFavoriteInternal(Favorite favorite)
    {
        _favorites.Remove(favorite);
    }

    internal void AddOrderInternal(Order order)
    {
        if (!_orders.Contains(order))
        {
            _orders.Add(order);
        }
    }

    internal void AddCollectionItemInternal(CollectionItem item)
    {
        if (!_collectionItems.Contains(item))
        {
            _collectionItems.Add(item);
        }
    }
}
