namespace LuxWatch.Models;

/// <summary>
/// Represents a gallery image belonging to a watch listing (composition owned by Watch).
/// </summary>
public class WatchImage
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Url { get; private set; }
    public string AltText { get; private set; }
    public int SortOrder { get; internal set; }

    // UML: 1..* ── 1
    public Watch Watch { get; private set; }

    public WatchImage(Watch watch, string url, string altText = "", int sortOrder = 0)
    {
        Watch = watch ?? throw new ArgumentNullException(nameof(watch));
        Url = url ?? throw new ArgumentNullException(nameof(url));
        AltText = altText;
        SortOrder = sortOrder;
    }

    public void SetAsPrimary()
    {
        Watch.SetPrimaryImage(this);
    }

    public void UpdateAltText(string text)
    {
        AltText = text;
    }
}
