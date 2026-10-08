namespace LuxWatch.Models;

/// <summary>
/// Represents an expert physical authenticity verification inspection report for an Order.
/// </summary>
public class AuthenticationReport
{
    // TODO: move to config
    public const decimal FreeThresholdPhp = 580_000m;

    // TODO: move to config
    public const decimal FlatFeePhp = 5_000m;

    public Guid Id { get; } = Guid.NewGuid();

    public AuthenticationStatus Status { get; private set; }
    public DateTime? InspectedAt { get; private set; }
    public string? CertificateNumber { get; private set; }
    public string? Notes { get; private set; }

    // UML: 0..1 ── 1
    public Order Order { get; private set; }

    public AuthenticationReport(Order order)
    {
        Order = order ?? throw new ArgumentNullException(nameof(order));
        Status = AuthenticationStatus.Pending;
    }

    public void Pass(string certNo)
    {
        Status = AuthenticationStatus.Passed;
        CertificateNumber = certNo;
        InspectedAt = DateTime.UtcNow;
    }

    public void Fail(string reason)
    {
        Status = AuthenticationStatus.Failed;
        Notes = reason;
        InspectedAt = DateTime.UtcNow;
    }

    internal decimal ComputeFee(decimal pricePhp)
    {
        return pricePhp >= FreeThresholdPhp ? 0m : FlatFeePhp;
    }
}
