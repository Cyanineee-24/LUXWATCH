using LuxWatch.Models;

namespace LuxWatch.Data;

public static class SampleData
{
    private static readonly Watch SampleWatch = new(
        Slug: "rolex-submariner-14060",
        Brand: "Rolex",
        Model: "Submariner (No Date)",
        Reference: "14060",
        Subtitle: "Automatic men's watch, Ref. 14060",
        Condition: "Used (Good)",
        Year: "1998 (approx.)",
        HasBox: false,
        HasPapers: false,
        PricePhp: 561563m,
        Description: "An exceptional neo-vintage Rolex Submariner Ref. 14060 featuring a clean two-line dial and luminous tritium hour markers with subtle patina. Retaining sharp factory lug bevels and the iconic aluminum bezel insert, this reference captures the purest essence of the modern classic dive watch. Fully inspected, authentic throughout, and performing within chronometer standards.",
        Images: new[]
        {
            "/images/landing_page_hero_macro.jpg",
            "/images/login_hero_art.jpg",
            "/images/register_hero_art.jpg",
            "/images/landing_page_hero_macro.jpg",
            "/images/login_hero_art.jpg"
        },
        Specs: new Dictionary<string, string>
        {
            ["Brand"] = "Rolex",
            ["Model"] = "Submariner (No Date)",
            ["Reference"] = "14060",
            ["Movement"] = "Automatic",
            ["Case material"] = "Steel",
            ["Case diameter"] = "40 mm",
            ["Water resistance"] = "300 m",
            ["Year of production"] = "1998 (approx.)",
            ["Condition"] = "Used (Good)",
            ["Scope of delivery"] = "No original box, no original papers"
        },
        Dealer: new Dealer(
            Name: "Haus Zeitwerk GmbH",
            Country: "Germany",
            Since: 2015,
            WatchesSold: 208,
            ActiveListings: 83,
            ShipsWithin: "Usually ships in-stock items within 24 hours",
            RespondsWithin: "Replies within 10 hours"
        ),
        DeliveryWindow: "Oct 1 – Oct 17",
        ViewsIn48h: 1148
    );

    public static Watch? Find(string slug)
    {
        if (string.Equals(SampleWatch.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return SampleWatch;
        }

        return null;
    }
}
