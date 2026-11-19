namespace LuxWatch.Models;

/// <summary>
/// Represents a watch owned in a user's private collection vault.
/// </summary>
public class CollectionItem
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Model { get; private set; }
    public string Reference { get; private set; }
    public string SerialNumber { get; private set; }
    public DateOnly PurchasedOn { get; private set; }
    public decimal PurchasePricePhp { get; private set; }
    public decimal EstimatedValuePhp { get; private set; }
    public string Notes { get; private set; }
    public string? PhotoUrl { get; private set; }

    // UML: 0..* ── 1
    public User Owner { get; private set; }

    // UML: 0..* ── 1
    public Brand Brand { get; private set; }

    // UML: 0..1 ── 0..1
    public Watch? SourceListing { get; private set; }

    public decimal Gain => EstimatedValuePhp - PurchasePricePhp;

    public CollectionItem(
        User owner,
        Brand brand,
        string model,
        string reference,
        string serialNumber,
        DateOnly purchasedOn,
        decimal purchasePricePhp,
        decimal estimatedValuePhp,
        string notes = "",
        string? photoUrl = null)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Brand = brand ?? throw new ArgumentNullException(nameof(brand));
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Reference = reference ?? throw new ArgumentNullException(nameof(reference));
        SerialNumber = serialNumber;
        PurchasedOn = purchasedOn;
        PurchasePricePhp = purchasePricePhp;
        EstimatedValuePhp = estimatedValuePhp;
        Notes = notes;
        PhotoUrl = photoUrl;

        owner.AddCollectionItemInternal(this);
        brand.AddCollectionItem(this);
    }

    public void UpdateValue(decimal newValuePhp)
    {
        EstimatedValuePhp = newValuePhp;
    }

    public void LinkListing(Watch watch)
    {
        SourceListing = watch;
        watch.CollectionItem = this;
    }
}
