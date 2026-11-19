namespace LuxWatch.Models;

/// <summary>
/// Status of funds held in escrow for an order.
/// </summary>
public enum PaymentStatus
{
    Held,
    Released,
    Refunded
}
