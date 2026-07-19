namespace IcsCalendarAggregator.Configuration;

/// <summary>
/// Configuration options for the calendar aggregator.
/// </summary>
public class CalendarAggregatorOptions
{
    /// <summary>
    /// Gets or sets the cache duration in minutes.
    /// </summary>
    public int CacheMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of days in the past to include events from.
    /// </summary>
    public int PastDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of days in the future to include events.
    /// </summary>
    public int FutureDays { get; set; } = 540;

    /// <summary>
    /// Gets or sets the list of event titles to block (case-insensitive).
    /// </summary>
    public List<string> BlockedTitles { get; set; } = new();

    /// <summary>
    /// Gets or sets the calendar sources to aggregate.
    /// </summary>
    public List<CalendarSource> Sources { get; set; } = new();
}

/// <summary>
/// Represents a single calendar source.
/// </summary>
public class CalendarSource
{
    /// <summary>
    /// Gets or sets the name of the calendar source.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the ICS feed.
    /// </summary>
    public string Url { get; set; } = string.Empty;
}
