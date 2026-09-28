using System.Globalization;
using LuxWatch.Models;

namespace LuxWatch.Services;

public class ReviewService
{
    // Baseline: 139 historic ratings (sample reviews below are already included in these numbers)
    private const int BaseTotal = 139;
    private static readonly Dictionary<int, int> BaseCounts = new() { [5] = 125, [4] = 11, [3] = 2, [2] = 0, [1] = 1 };
    private const int BaseShippingSum = 681;   // averages to 4.9
    private const int BaseItemSum = 695;       // averages to 5.0
    private const int BaseCommSum = 681;       // averages to 4.9
    private const int BaseRecommend = 134;

    private readonly List<Review> _seed = SampleReviews();
    private readonly List<Review> _added = new();

    public event Action? OnChange;

    // Newest-added first, then the seed list.
    public IEnumerable<Review> All => _added.Concat(_seed);

    public int Total => BaseTotal + _added.Count;
    public int CountFor(int stars) => BaseCounts[stars] + _added.Count(r => r.Overall == stars);
    public double Average => (BaseCounts.Sum(kv => kv.Key * kv.Value) + _added.Sum(r => r.Overall)) / (double)Total;
    public double ShippingAvg => (BaseShippingSum + _added.Sum(r => r.Shipping)) / (double)Total;
    public double ItemAvg => (BaseItemSum + _added.Sum(r => r.ItemAsDescribed)) / (double)Total;
    public double CommunicationAvg => (BaseCommSum + _added.Sum(r => r.Communication)) / (double)Total;
    public int RecommendCount => BaseRecommend + _added.Count(r => r.Recommends);

    public void Add(Review review) { _added.Insert(0, review); OnChange?.Invoke(); }
    public void MarkHelpful(Review review) { review.Helpful++; OnChange?.Invoke(); }

    public static string Format(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    private static List<Review> SampleReviews() => new()
    {
        new Review
        {
            Author = "Andreas H.",
            Country = "DE",
            Date = new DateOnly(2026, 9, 24),
            Overall = 5,
            Shipping = 5,
            ItemAsDescribed = 5,
            Communication = 5,
            Recommends = true,
            WatchPurchased = "Omega Speedmaster Reduced 3510.50",
            Comment = "Everything went smoothly. Packed carefully and exactly as pictured.",
            Helpful = 3
        },
        new Review
        {
            Author = "Malissa W.",
            Country = "US",
            Date = new DateOnly(2026, 9, 22),
            Overall = 5,
            Shipping = 5,
            ItemAsDescribed = 5,
            Communication = 5,
            Recommends = true,
            WatchPurchased = "Rolex Datejust 16234",
            Comment = "Exceeded expectations. Great communication and it arrived within days.",
            Helpful = 0
        },
        new Review
        {
            Author = "James C.",
            Country = "SG",
            Date = new DateOnly(2026, 9, 18),
            Overall = 5,
            Shipping = 5,
            ItemAsDescribed = 5,
            Communication = 4,
            Recommends = true,
            WatchPurchased = "Tudor Black Bay 58",
            Comment = "Authentication paperwork was thorough. The dealer answered every question before I paid.",
            Helpful = 0
        },
        new Review
        {
            Author = "Camille R.",
            Country = "FR",
            Date = new DateOnly(2026, 9, 11),
            Overall = 4,
            Shipping = 3,
            ItemAsDescribed = 5,
            Communication = 5,
            Recommends = true,
            WatchPurchased = "Tudor Black Bay 58",
            Comment = "The watch was perfect, but shipping took four days longer than quoted.",
            Helpful = 0
        },
        new Review
        {
            Author = "Kenji T.",
            Country = "JP",
            Date = new DateOnly(2026, 9, 3),
            Overall = 5,
            Shipping = 5,
            ItemAsDescribed = 5,
            Communication = 5,
            Recommends = true,
            WatchPurchased = "Grand Seiko SBGA211",
            Comment = "Easy and straightforward. Everything matched the listing.",
            Helpful = 0
        },
        new Review
        {
            Author = "Hossam E.",
            Country = "AE",
            Date = new DateOnly(2026, 8, 27),
            Overall = 5,
            Shipping = 5,
            ItemAsDescribed = 5,
            Communication = 5,
            Recommends = true,
            WatchPurchased = "Omega Seamaster 2254.50",
            Comment = "Fast replies and insured shipping. Would buy from this dealer again.",
            Helpful = 0
        },
        new Review
        {
            Author = "Ben C.",
            Country = "US",
            Date = new DateOnly(2026, 8, 20),
            Overall = 5,
            Shipping = 5,
            ItemAsDescribed = 5,
            Communication = 5,
            Recommends = true,
            WatchPurchased = "Rolex Explorer 14270",
            Comment = "Honest description of the condition. Very happy.",
            Helpful = 0
        },
        new Review
        {
            Author = "Lea M.",
            Country = "DE",
            Date = new DateOnly(2026, 8, 14),
            Overall = 3,
            Shipping = 4,
            ItemAsDescribed = 3,
            Communication = 4,
            Recommends = false,
            WatchPurchased = "Rolex Datejust 16014",
            Comment = "The case had more wear than the photos suggested. The dealer was polite and offered a partial refund.",
            Helpful = 0
        }
    };
}
