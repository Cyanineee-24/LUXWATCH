namespace LuxWatch.Models;

/// <summary>
/// Lifecycle status of a watch listing on the marketplace.
/// </summary>
public enum WatchStatus
{
    Draft,
    PendingReview,
    Active,
    Reserved,
    Sold
}
