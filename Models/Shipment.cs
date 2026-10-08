namespace LuxWatch.Models;

/// <summary>
/// Represents the tracked physical shipment for an Order.
/// </summary>
public class Shipment
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Carrier { get; private set; }
    public string TrackingNumber { get; private set; }
    public DateTime ShippedAt { get; internal set; }
    public DateTime? DeliveredAt { get; internal set; }

    // UML: 0..1 ── 1
    public Order Order { get; private set; }

    public Shipment(Order order, string carrier, string trackingNumber)
    {
        Order = order ?? throw new ArgumentNullException(nameof(order));
        Carrier = carrier ?? throw new ArgumentNullException(nameof(carrier));
        TrackingNumber = trackingNumber ?? throw new ArgumentNullException(nameof(trackingNumber));
        ShippedAt = DateTime.UtcNow;
    }

    public void MarkDelivered(DateTime at)
    {
        DeliveredAt = at;
    }

    public string GetTrackingUrl() => $"https://track.luxwatch.example/{TrackingNumber}";
}
