using System.Globalization;
using LuxWatch.Data;
using LuxWatch.Models;

namespace LuxWatch.Services;

public class ReviewService : IReviewService
{
    // Baseline: 139 historic ratings (sample reviews below are already included in these numbers)
    private const int BaseTotal = 139;
    private static readonly Dictionary<int, int> BaseCounts = new() { [5] = 125, [4] = 11, [3] = 2, [2] = 0, [1] = 1 };
    private const int BaseShippingSum = 681;   // averages to 4.9
    private const int BaseItemSum = 695;       // averages to 5.0
    private const int BaseCommSum = 681;       // averages to 4.9
    private const int BaseRecommend = 134;

    private readonly List<Review> _seed = SampleData.SeedReviews;
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

    public IReadOnlyList<Review> GetAll() => All.ToList();

    public double GetAverage() => Average;

    public void Add(Review review)
    {
        _added.Insert(0, review);
        OnChange?.Invoke();
    }

    public void MarkHelpful(Review review, User user)
    {
        if (review.AddHelpfulVote(user))
        {
            OnChange?.Invoke();
        }
    }

    public void MarkHelpful(Review review)
    {
        MarkHelpful(review, SampleData.CurrentUser);
    }

    public static string Format(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
}
