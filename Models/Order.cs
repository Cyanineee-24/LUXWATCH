namespace LuxWatch.Models;

/// <summary>
/// Represents an escrow purchase order for a luxury watch listing.
/// </summary>
public class Order
{
    public Guid Id { get; } = Guid.NewGuid();

    public string OrderNumber { get; private set; }
    public decimal PricePhp { get; private set; }
    public decimal ShippingCostPhp { get; private set; }
    public DateTime PlacedAt { get; internal set; }
    public OrderStatus Status { get; internal set; }

    // UML: 0..* ── 1
    public User Buyer { get; private set; }

    // UML: 0..* ── 1
    public Watch Watch { get; private set; }

    // UML: 1 ── 0..1
    public Payment? Payment { get; internal set; }

    // UML: 1 ── 0..1
    public Shipment? Shipment { get; internal set; }

    // UML: 1 ── 0..1
    public Review? Review { get; internal set; }

    // UML: 1 ── 0..1
    public AuthenticationReport? AuthenticationReport { get; internal set; }

    public Dealer Dealer => Watch.Dealer;

    public Order(User buyer, Watch watch, string orderNumber)
    {
        Buyer = buyer ?? throw new ArgumentNullException(nameof(buyer));
        Watch = watch ?? throw new ArgumentNullException(nameof(watch));
        OrderNumber = orderNumber ?? throw new ArgumentNullException(nameof(orderNumber));
        PricePhp = watch.PricePhp;
        ShippingCostPhp = watch.ShippingCostPhp;
        PlacedAt = DateTime.UtcNow;
        Status = OrderStatus.PendingPayment;
    }

    public Payment Pay(string method)
    {
        if (Status != OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException($"Cannot pay for order in state '{Status}'. Must be PendingPayment.");
        }
        var payment = new Payment(this, PricePhp + ShippingCostPhp, method);
        Payment = payment;
        Status = OrderStatus.InEscrow;
        return payment;
    }

    public Shipment Ship(string carrier)
    {
        if (Status != OrderStatus.InEscrow)
        {
            throw new InvalidOperationException($"Cannot ship order in state '{Status}'. Must be InEscrow.");
        }
        var trackingNumber = $"LW-TRK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var shipment = new Shipment(this, carrier, trackingNumber);
        Shipment = shipment;
        Status = OrderStatus.Shipped;
        return shipment;
    }

    public void ConfirmDelivery()
    {
        if (Status != OrderStatus.Shipped)
        {
            throw new InvalidOperationException($"Cannot confirm delivery in state '{Status}'. Must be Shipped.");
        }
        if (Shipment == null)
        {
            throw new InvalidOperationException("Shipment does not exist for this order.");
        }
        Shipment.MarkDelivered(DateTime.UtcNow);
        Status = OrderStatus.Delivered;
    }

    public void Complete()
    {
        if (Status != OrderStatus.Delivered)
        {
            throw new InvalidOperationException($"Cannot complete order in state '{Status}'. Must be Delivered.");
        }
        Payment?.Release();
        Watch.MarkSold();
        Status = OrderStatus.Completed;
    }

    public void Cancel()
    {
        if (Status != OrderStatus.PendingPayment && Status != OrderStatus.InEscrow)
        {
            throw new InvalidOperationException($"Cannot cancel order in state '{Status}'. Only PendingPayment or InEscrow can be cancelled.");
        }
        Payment?.Refund();
        Watch.Release();
        Status = OrderStatus.Cancelled;
    }

    internal void AttachReview(Review review)
    {
        if (Review != null)
        {
            throw new InvalidOperationException("Order already has an attached review.");
        }
        Review = review;
    }

    internal AuthenticationReport BeginAuthentication()
    {
        if (AuthenticationReport != null)
        {
            throw new InvalidOperationException("Authentication report already exists for this order.");
        }
        if (Status != OrderStatus.InEscrow)
        {
            throw new InvalidOperationException($"Cannot begin authentication in state '{Status}'. Must be InEscrow.");
        }
        var report = new AuthenticationReport(this);
        AuthenticationReport = report;
        return report;
    }

    internal static Order CreateSeededCompletedOrder(User buyer, Watch watch, string orderNumber, DateTime placedAt, DateTime deliveredAt)
    {
        var order = new Order(buyer, watch, orderNumber)
        {
            PlacedAt = placedAt,
            Status = OrderStatus.Completed
        };
        var payment = new Payment(order, watch.PricePhp + watch.ShippingCostPhp, "Escrow")
        {
            HeldAt = placedAt,
            ReleasedAt = deliveredAt,
            Status = PaymentStatus.Released
        };
        order.Payment = payment;

        var shipment = new Shipment(order, "DHL Express", $"LW-TRK-{order.Id.ToString()[..8].ToUpperInvariant()}")
        {
            ShippedAt = placedAt.AddDays(1),
            DeliveredAt = deliveredAt
        };
        order.Shipment = shipment;

        buyer.AddOrderInternal(order);
        watch.AddOrder(order);
        return order;
    }
}
