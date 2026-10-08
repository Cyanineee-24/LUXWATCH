namespace LuxWatch.Models;

/// <summary>
/// Fulfillment and escrow lifecycle status of an order.
/// </summary>
public enum OrderStatus
{
    PendingPayment,
    InEscrow,
    Shipped,
    Delivered,
    Completed,
    Cancelled
}
