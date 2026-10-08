namespace LuxWatch.Models;

/// <summary>
/// Represents a technical specification key-value pair belonging to a watch listing (composition owned by Watch).
/// </summary>
public class WatchSpec
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Key { get; private set; }
    public string Value { get; private set; }
    public int DisplayOrder { get; private set; }

    // UML: 0..* ── 1
    public Watch Watch { get; private set; }

    public WatchSpec(Watch watch, string key, string value, int displayOrder = 0)
    {
        Watch = watch ?? throw new ArgumentNullException(nameof(watch));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        DisplayOrder = displayOrder;
    }

    public string Format() => $"{Key}: {Value}";
}
