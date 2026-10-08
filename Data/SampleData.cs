using LuxWatch.Models;

namespace LuxWatch.Data;

public static class SampleData
{
    // ── Current logged-in demo user ──
    public static readonly User CurrentUser = new(
        username: "marcusvance",
        fullName: "Marcus Vance",
        email: "marcus.vance@horology.com",
        passwordHash: User.HashPassword("password123"),
        country: "PH",
        createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    );

    // ── 20 Landing-page brands in landing page order ──
    public static readonly IReadOnlyList<Brand> Brands = new List<Brand>
    {
        new("Rolex", isFeatured: true, displayOrder: 0, description: "Iconic Swiss luxury watch manufacturer founded in 1905."),
        new("Patek Philippe", isFeatured: true, displayOrder: 1, description: "Geneva's premier independent family-owned manufacture."),
        new("Audemars Piguet", isFeatured: true, displayOrder: 2, description: "Master of high complications and creator of the Royal Oak."),
        new("Panerai", isFeatured: false, displayOrder: 3, description: "Italian design combined with Swiss horological excellence."),
        new("Breitling", isFeatured: false, displayOrder: 4, description: "Specialist in chronographs and technical aviation watches."),
        new("Omega", isFeatured: true, displayOrder: 5, description: "Precision watchmaking, chronometers of the Moon and the Olympics."),
        new("Seiko", isFeatured: false, displayOrder: 6, description: "Pioneering Japanese watchmaking and craftsmanship."),
        new("TAG Heuer", isFeatured: false, displayOrder: 7, description: "Avant-garde Swiss chronographs rooted in motor racing."),
        new("IWC", isFeatured: false, displayOrder: 8, description: "Engineered luxury timepieces from Schaffhausen."),
        new("Tudor", isFeatured: true, displayOrder: 9, description: "Born for daring adventurers, exceptional robust reliability."),
        new("Cartier", isFeatured: true, displayOrder: 10, description: "The jeweler of kings and king of shaped watchmakers."),
        new("Vacheron Constantin", isFeatured: true, displayOrder: 11, description: "The oldest continuously operating watch manufacture since 1755."),
        new("Jaeger-LeCoultre", isFeatured: false, displayOrder: 12, description: "The watchmaker's watchmaker from the Vallée de Joux."),
        new("Hublot", isFeatured: false, displayOrder: 13, description: "The Art of Fusion in modern luxury horology."),
        new("Zenith", isFeatured: false, displayOrder: 14, description: "Home of the high-frequency El Primero chronograph caliber."),
        new("Breguet", isFeatured: false, displayOrder: 15, description: "Inventor of the tourbillon and pioneer of Haute Horlogerie."),
        new("Blancpain", isFeatured: false, displayOrder: 16, description: "Creator of the modern diving watch and pure mechanical traditions."),
        new("Grand Seiko", isFeatured: false, displayOrder: 17, description: "Japanese perfection, Zaratsu polishing, and Spring Drive innovation."),
        new("Richard Mille", isFeatured: false, displayOrder: 18, description: "A racing machine on the wrist with space-age materials."),
        new("Chopard", isFeatured: false, displayOrder: 19, description: "Artisan watchmaking, Happy Diamonds, and Mille Miglia elegance.")
    };

    // ── Watch categories/types in order ──
    public static readonly IReadOnlyList<WatchType> WatchTypes = new List<WatchType>
    {
        new("Diver", displayOrder: 0),
        new("Chronograph", displayOrder: 1),
        new("Dress", displayOrder: 2),
        new("Vintage", displayOrder: 3),
        new("Limited Edition", displayOrder: 4)
    };

    // ── Sample seller profile ──
    private static readonly User SampleSeller = new(
        username: "hauszeitwerk",
        fullName: "Haus Zeitwerk",
        email: "service@zeitwerk.example.de",
        passwordHash: User.HashPassword("sellersecret"),
        country: "DE",
        createdAt: new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    );

    public static readonly Dealer SampleDealer = new(
        owner: SampleSeller,
        name: "Haus Zeitwerk GmbH",
        country: "Germany",
        since: new DateOnly(2015, 1, 1),
        shipsWithin: "Usually ships in-stock items within 24 hours",
        respondsWithin: "Replies within 10 hours",
        isVerified: true,
        seedWatchesSoldBaseline: 200,
        seedActiveListingsBaseline: 82
    );

    // ── Primary featured listing ──
    public static readonly Watch SampleWatch = InitSampleWatch();

    // ── Seed reviews attached to historical completed orders ──
    public static readonly List<Review> SeedReviews = InitSeedReviews();

    private static Watch InitSampleWatch()
    {
        var rolex = Brands.First(b => b.Name == "Rolex");
        var diverType = WatchTypes.First(t => t.Name == "Diver");

        var watch = new Watch(
            brand: rolex,
            dealer: SampleDealer,
            slug: "rolex-submariner-14060",
            model: "Submariner (No Date)",
            reference: "14060",
            subtitle: "Automatic men's watch, Ref. 14060",
            description: "An exceptional neo-vintage Rolex Submariner Ref. 14060 featuring a clean two-line dial and luminous tritium hour markers with subtle patina. Retaining sharp factory lug bevels and the iconic aluminum bezel insert, this reference captures the purest essence of the modern classic dive watch. Fully inspected, authentic throughout, and performing within chronometer standards.",
            condition: Condition.PreOwned,
            year: "1998 (approx.)",
            hasBox: false,
            hasPapers: false,
            pricePhp: 561563m,
            shippingCostPhp: 4500m,
            locationCountryCode: "DE",
            listedDate: new DateOnly(2026, 9, 15),
            deliveryWindow: "Oct 1 – Oct 17",
            viewsIn48h: 1148,
            status: WatchStatus.Active
        );

        watch.AddImage("/images/landing_page_hero_macro.jpg", "Rolex Submariner dial macro");
        watch.AddImage("/images/login_hero_art.jpg", "Rolex Submariner case profile");
        watch.AddImage("/images/register_hero_art.jpg", "Rolex Submariner oyster bracelet");
        watch.AddImage("/images/landing_page_hero_macro.jpg", "Rolex Submariner bezel close-up");
        watch.AddImage("/images/login_hero_art.jpg", "Rolex Submariner caseback");

        watch.AddSpec("Brand", "Rolex");
        watch.AddSpec("Model", "Submariner (No Date)");
        watch.AddSpec("Reference", "14060");
        watch.AddSpec("Movement", "Automatic");
        watch.AddSpec("Case material", "Steel");
        watch.AddSpec("Case diameter", "40 mm");
        watch.AddSpec("Water resistance", "300 m");
        watch.AddSpec("Year of production", "1998 (approx.)");
        watch.AddSpec("Condition", "Used (Good)");
        watch.AddSpec("Scope of delivery", "No original box, no original papers");

        watch.AddType(diverType);

        return watch;
    }

    private static List<Review> InitSeedReviews()
    {
        var omega = Brands.First(b => b.Name == "Omega");
        var rolex = Brands.First(b => b.Name == "Rolex");
        var tudor = Brands.First(b => b.Name == "Tudor");
        var grandSeiko = Brands.First(b => b.Name == "Grand Seiko");

        var reviews = new List<Review>();

        // Review 1: Andreas H.
        var user1 = new User("andreash", "Andreas H.", "andreas.h@example.de", User.HashPassword("pass"), "DE");
        var watch1 = new Watch(omega, SampleDealer, "omega-speedmaster-reduced-351050", "Speedmaster Reduced", "3510.50", "Automatic chronograph", "Classic Speedmaster Reduced", Condition.PreOwned, "2002", true, true, 185000m, 3500m, "DE", new DateOnly(2026, 8, 1), "Delivered", 320, WatchStatus.Sold);
        watch1.AddImage("/images/landing_page_hero_macro.jpg", "Omega Speedmaster");
        var order1 = Order.CreateSeededCompletedOrder(user1, watch1, "LW-2026-0301", new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc));
        reviews.Add(new Review(order1, 5, 5, 5, 5, true, "Everything went smoothly. Packed carefully and exactly as pictured.", new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc), seedHelpfulBaseline: 3));

        // Review 2: Malissa W.
        var user2 = new User("malissaw", "Malissa W.", "malissa.w@example.com", User.HashPassword("pass"), "US");
        var watch2 = new Watch(rolex, SampleDealer, "rolex-datejust-16234", "Datejust", "16234", "Automatic men's watch", "Rolex Datejust 16234", Condition.PreOwned, "1995", true, false, 320000m, 4500m, "DE", new DateOnly(2026, 8, 10), "Delivered", 410, WatchStatus.Sold);
        watch2.AddImage("/images/login_hero_art.jpg", "Rolex Datejust");
        var order2 = Order.CreateSeededCompletedOrder(user2, watch2, "LW-2026-0302", new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc));
        reviews.Add(new Review(order2, 5, 5, 5, 5, true, "Exceeded expectations. Great communication and it arrived within days.", new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc), seedHelpfulBaseline: 0));

        // Review 3: James C.
        var user3 = new User("jamesc", "James C.", "james.c@example.sg", User.HashPassword("pass"), "SG");
        var watch3 = new Watch(tudor, SampleDealer, "tudor-black-bay-58-1", "Black Bay", "58", "Automatic diver watch", "Tudor Black Bay Fifty-Eight", Condition.PreOwned, "2021", true, true, 210000m, 3500m, "DE", new DateOnly(2026, 8, 15), "Delivered", 550, WatchStatus.Sold);
        watch3.AddImage("/images/register_hero_art.jpg", "Tudor Black Bay 58");
        var order3 = Order.CreateSeededCompletedOrder(user3, watch3, "LW-2026-0303", new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc));
        reviews.Add(new Review(order3, 5, 5, 5, 4, true, "Authentication paperwork was thorough. The dealer answered every question before I paid.", new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc), seedHelpfulBaseline: 0));

        // Review 4: Camille R.
        var user4 = new User("camiller", "Camille R.", "camille.r@example.fr", User.HashPassword("pass"), "FR");
        var watch4 = new Watch(tudor, SampleDealer, "tudor-black-bay-58-2", "Black Bay", "58", "Automatic diver watch", "Tudor Black Bay 58", Condition.PreOwned, "2020", true, true, 205000m, 3500m, "DE", new DateOnly(2026, 8, 20), "Delivered", 290, WatchStatus.Sold);
        watch4.AddImage("/images/landing_page_hero_macro.jpg", "Tudor Black Bay 58");
        var order4 = Order.CreateSeededCompletedOrder(user4, watch4, "LW-2026-0304", new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc));
        reviews.Add(new Review(order4, 4, 3, 5, 5, true, "The watch was perfect, but shipping took four days longer than quoted.", new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc), seedHelpfulBaseline: 0));

        // Review 5: Kenji T.
        var user5 = new User("kenjit", "Kenji T.", "kenji.t@example.jp", User.HashPassword("pass"), "JP");
        var watch5 = new Watch(grandSeiko, SampleDealer, "grand-seiko-sbga211", "SBGA211", "", "Spring Drive Snowflake", "Grand Seiko Snowflake", Condition.PreOwned, "2022", true, true, 310000m, 4000m, "DE", new DateOnly(2026, 8, 12), "Delivered", 480, WatchStatus.Sold);
        watch5.AddImage("/images/login_hero_art.jpg", "Grand Seiko SBGA211");
        var order5 = Order.CreateSeededCompletedOrder(user5, watch5, "LW-2026-0305", new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc));
        reviews.Add(new Review(order5, 5, 5, 5, 5, true, "Easy and straightforward. Everything matched the listing.", new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc), seedHelpfulBaseline: 0));

        // Review 6: Hossam E.
        var user6 = new User("hossame", "Hossam E.", "hossam.e@example.ae", User.HashPassword("pass"), "AE");
        var watch6 = new Watch(omega, SampleDealer, "omega-seamaster-225450", "Seamaster", "2254.50", "Professional 300M", "Omega Seamaster 2254.50", Condition.PreOwned, "2006", true, false, 195000m, 3500m, "DE", new DateOnly(2026, 8, 5), "Delivered", 620, WatchStatus.Sold);
        watch6.AddImage("/images/register_hero_art.jpg", "Omega Seamaster");
        var order6 = Order.CreateSeededCompletedOrder(user6, watch6, "LW-2026-0306", new DateTime(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Utc));
        reviews.Add(new Review(order6, 5, 5, 5, 5, true, "Fast replies and insured shipping. Would buy from this dealer again.", new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Utc), seedHelpfulBaseline: 0));

        // Review 7: Ben C.
        var user7 = new User("benc", "Ben C.", "ben.c@example.com", User.HashPassword("pass"), "US");
        var watch7 = new Watch(rolex, SampleDealer, "rolex-explorer-14270", "Explorer", "14270", "Sports watch", "Rolex Explorer 14270", Condition.PreOwned, "1999", true, true, 345000m, 4500m, "DE", new DateOnly(2026, 7, 28), "Delivered", 710, WatchStatus.Sold);
        watch7.AddImage("/images/landing_page_hero_macro.jpg", "Rolex Explorer");
        var order7 = Order.CreateSeededCompletedOrder(user7, watch7, "LW-2026-0307", new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc));
        reviews.Add(new Review(order7, 5, 5, 5, 5, true, "Honest description of the condition. Very happy.", new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc), seedHelpfulBaseline: 0));

        // Review 8: Lea M.
        var user8 = new User("leam", "Lea M.", "lea.m@example.de", User.HashPassword("pass"), "DE");
        var watch8 = new Watch(rolex, SampleDealer, "rolex-datejust-16014", "Datejust", "16014", "Silver dial", "Rolex Datejust 16014", Condition.PreOwned, "1984", false, false, 280000m, 3500m, "DE", new DateOnly(2026, 7, 20), "Delivered", 430, WatchStatus.Sold);
        watch8.AddImage("/images/login_hero_art.jpg", "Rolex Datejust");
        var order8 = Order.CreateSeededCompletedOrder(user8, watch8, "LW-2026-0308", new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc));
        reviews.Add(new Review(order8, 3, 4, 3, 4, false, "The case had more wear than the photos suggested. The dealer was polite and offered a partial refund.", new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc), seedHelpfulBaseline: 0));

        return reviews;
    }

    public static Order CreateDemoCompletedOrder(Watch watch)
    {
        var placedAt = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
        var deliveredAt = new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);
        return Order.CreateSeededCompletedOrder(CurrentUser, watch, "LW-2026-0412", placedAt, deliveredAt);
    }

    public static Watch? Find(string slug)
    {
        if (string.Equals(SampleWatch.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return SampleWatch;
        }

        return null;
    }
}
