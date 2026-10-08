namespace LuxWatch.Models;

/// <summary>
/// Display formatting helpers for domain model types.
/// </summary>
public static class ModelExtensions
{
    public static string PublicName(this User user)
    {
        if (string.IsNullOrWhiteSpace(user.FullName))
        {
            return user.Username;
        }

        var parts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return parts[0];
        }

        var lastInitial = parts[^1].TrimEnd('.').FirstOrDefault();
        return lastInitial != default ? $"{parts[0]} {char.ToUpperInvariant(lastInitial)}." : parts[0];
    }

    public static string Label(this Condition condition) => condition switch
    {
        Condition.PreOwned => "Pre-owned",
        Condition.BrandNew => "Brand new",
        _ => condition.ToString()
    };
}
