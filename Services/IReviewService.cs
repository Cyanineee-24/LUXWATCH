using LuxWatch.Models;

namespace LuxWatch.Services;

public interface IReviewService
{
    IReadOnlyList<Review> GetAll();
    void Add(Review review);
    void MarkHelpful(Review review, User user);
    double GetAverage();
    int CountFor(int stars);
}
