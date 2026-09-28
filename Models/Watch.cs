namespace LuxWatch.Models;

public record Dealer(
    string Name,
    string Country,
    int Since,
    int WatchesSold,
    int ActiveListings,
    string ShipsWithin,
    string RespondsWithin
);

public record Watch(
    string Slug,
    string Brand,
    string Model,
    string Reference,
    string Subtitle,
    string Condition,
    string Year,
    bool HasBox,
    bool HasPapers,
    decimal PricePhp,
    string Description,
    string[] Images,
    Dictionary<string, string> Specs,
    Dealer Dealer,
    string DeliveryWindow,
    int ViewsIn48h
);
