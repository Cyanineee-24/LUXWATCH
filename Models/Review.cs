namespace LuxWatch.Models;

public class Review
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Author { get; init; }          // "Andreas H."
    public required string Country { get; init; }         // two-letter code, "DE"
    public required DateOnly Date { get; init; }
    public required int Overall { get; init; }            // 1 to 5
    public required int Shipping { get; init; }           // 1 to 5
    public required int ItemAsDescribed { get; init; }    // 1 to 5
    public required int Communication { get; init; }      // 1 to 5
    public required bool Recommends { get; init; }
    public required string Comment { get; init; }
    public required string WatchPurchased { get; init; }
    public int Helpful { get; set; }
}
