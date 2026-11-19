namespace LuxWatch.Models;

/// <summary>
/// Represents an escrow payment transaction held for an Order.
/// </summary>
public class Payment
{
    public Guid Id { get; } = Guid.NewGuid();

    public decimal AmountPhp { get; private set; }
    public string Method { get; private set; }
    public DateTime HeldAt { get; internal set; }
    public DateTime? ReleasedAt { get; internal set; }
    public PaymentStatus Status { get; internal set; }

    // UML: 0..1 ── 1
    public Order Order { get; private set; }

    public Payment(Order order, decimal amountPhp, string method)
    {
        Order = order ?? throw new ArgumentNullException(nameof(order));
        AmountPhp = amountPhp;
        Method = method ?? throw new ArgumentNullException(nameof(method));
        HeldAt = DateTime.UtcNow;
        Status = PaymentStatus.Held;
    }

    public void Release()
    {
        if (Status != PaymentStatus.Held)
        {
            throw new InvalidOperationException($"Cannot release payment in state '{Status}'. Must be Held.");
        }
        Status = PaymentStatus.Released;
        ReleasedAt = DateTime.UtcNow;
    }

    public void Refund()
    {
        if (Status != PaymentStatus.Held)
        {
            throw new InvalidOperationException($"Cannot refund payment in state '{Status}'. Must be Held.");
        }
        Status = PaymentStatus.Refunded;
    }
}
