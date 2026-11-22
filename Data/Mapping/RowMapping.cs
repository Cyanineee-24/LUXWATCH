using LuxWatch.Data.Rows;
using LuxWatch.Models;

namespace LuxWatch.Data.Mapping;

/// <summary>
/// Turns Supabase rows (Data/Rows) into the UML domain models (Models/).
/// Pages only ever see the domain models.
///
/// Domain objects link to each other (Watch → Brand/Dealer, Dealer → Owner),
/// and their constructors register themselves with their parents
/// (e.g. new Watch(...) calls dealer.AddWatch). A <see cref="MappingContext"/>
/// makes sure one database row becomes one object per mapping call, so the
/// same dealer is shared by all of its watches instead of being duplicated.
/// </summary>
public static class RowMapping
{
    // ── Enums: parse loudly. A typo or an unknown DB value throws instead of
    //    silently defaulting to the first enum member.
    private static T ParseEnum<T>(string value, string column) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: false, out var result) && Enum.IsDefined(result)
            ? result
            : throw new InvalidOperationException($"Unknown {typeof(T).Name} '{value}' in column '{column}'.");

    /// <summary>year + year_is_approximate → the display string the UI has always shown: "1998 (approx.)" / "2002".</summary>
    public static string FormatYear(int? year, bool isApproximate) =>
        year is null ? "" : isApproximate ? $"{year} (approx.)" : year.Value.ToString();

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

    public static User ToDomain(this UserRow row) =>
        new(row.Username, row.FullName,
            country: row.Country ?? "",
            createdAt: AsUtc(row.CreatedAt))
        { Id = row.Id };

    public static Brand ToDomain(this BrandRow row) =>
        new(row.Name, row.LogoUrl, row.IsFeatured, row.DisplayOrder, row.Description ?? "")
        { Id = row.Id };

    public static Dealer ToDomain(this DealerRow row, MappingContext? ctx = null)
    {
        ctx ??= new MappingContext();
        if (ctx.Dealers.TryGetValue(row.Id, out var existing)) return existing;

        var ownerRow = row.Owner
            ?? throw new InvalidOperationException($"Dealer {row.Id} was loaded without its owner (users reference).");
        var owner = ctx.Users.TryGetValue(ownerRow.Id, out var u) ? u : ctx.Users[ownerRow.Id] = ownerRow.ToDomain();

        var dealer = new Dealer(
            owner, row.Name, row.Country, DateOnly.FromDateTime(row.Since),
            row.ShipsWithin ?? "", row.RespondsWithin ?? "", row.IsVerified)
        { Id = row.Id };
        // Stats (WatchesSold / ActiveListings / ratings) are NOT set here: the page
        // sets dealer.Stats from IDealerRepository.GetStatsAsync (dealer_stats view).
        return ctx.Dealers[row.Id] = dealer;
    }

    public static Watch ToDomain(this WatchRow row, MappingContext? ctx = null)
    {
        ctx ??= new MappingContext();
        if (ctx.Watches.TryGetValue(row.Id, out var existing)) return existing;

        var brandRow = row.Brand
            ?? throw new InvalidOperationException($"Watch '{row.Slug}' was loaded without its brand reference.");
        var dealerRow = row.Dealer
            ?? throw new InvalidOperationException($"Watch '{row.Slug}' was loaded without its dealer reference.");

        var brand = ctx.Brands.TryGetValue(brandRow.Id, out var b) ? b : ctx.Brands[brandRow.Id] = brandRow.ToDomain();
        var dealer = dealerRow.ToDomain(ctx);

        var watch = new Watch(
            brand, dealer,
            row.Slug, row.Model,
            reference: row.Reference ?? "",
            subtitle: row.Subtitle ?? "",
            description: row.Description ?? "",
            condition: ParseEnum<Condition>(row.Condition, "watches.condition"),
            year: FormatYear(row.Year, row.YearIsApproximate),
            row.HasBox, row.HasPapers,
            row.PricePhp, row.ShippingCostPhp,
            locationCountryCode: row.LocationCountryCode ?? "",
            listedDate: DateOnly.FromDateTime(row.ListedDate),
            deliveryWindow: row.DeliveryWindow ?? "",
            viewsIn48h: row.ViewsIn48h,
            status: ParseEnum<WatchStatus>(row.Status, "watches.status"))
        { Id = row.Id };

        // Children arrive with the watch; order them here. AddImage/AddSpec
        // assign SortOrder/DisplayOrder 0..n in this order.
        foreach (var img in row.Images.OrderBy(i => i.SortOrder))
            watch.AddImage(img.Url, img.AltText ?? "");
        foreach (var spec in row.Specs.OrderBy(s => s.DisplayOrder))
            watch.AddSpec(spec.Key, spec.Value);

        return ctx.Watches[row.Id] = watch;
    }

    /// <summary>
    /// dealer_reviews row → Review. Review is immutable and needs a full Order
    /// (buyer + Watch), and Watch needs Brand + Dealer + Owner. The view only
    /// carries the review card's columns, so the caller passes the Watch,
    /// loaded in ONE batched request for the whole page (see ReviewReadRepository).
    /// </summary>
    public static Review ToDomain(this DealerReviewRow row, Watch watch, MappingContext? ctx = null)
    {
        ctx ??= new MappingContext();

        var buyer = ctx.Users.TryGetValue(row.BuyerId, out var u)
            ? u
            : ctx.Users[row.BuyerId] = new User(
                row.AuthorUsername, row.AuthorName,
                country: row.AuthorCountry ?? "")
            { Id = row.BuyerId };

        // Order(buyer, watch, number) snapshots price from the watch and starts as
        // PendingPayment; PlacedAt/Status are then set from the database.
        var order = new Order(buyer, watch, row.OrderNumber)
        {
            Id = row.OrderId,
            PlacedAt = AsUtc(row.PlacedAt),
            Status = ParseEnum<OrderStatus>(row.OrderStatus, "dealer_reviews.order_status"),
        };

        return new Review(
            order,
            row.Overall, row.Shipping, row.ItemAsDescribed, row.Communication,
            row.Recommends, row.Comment,
            createdAt: AsUtc(row.CreatedAt),
            seedHelpfulBaseline: row.HelpfulCount)   // helpful_count stands in for HelpfulVoters (no auth yet)
        { Id = row.Id };
    }
}

/// <summary>Identity map for one mapping pass: one object per database row.</summary>
public sealed class MappingContext
{
    public Dictionary<Guid, User> Users { get; } = new();
    public Dictionary<Guid, Brand> Brands { get; } = new();
    public Dictionary<Guid, Dealer> Dealers { get; } = new();
    public Dictionary<Guid, Watch> Watches { get; } = new();
}
