namespace IcsCalendarAggregator.Models;

/// <summary>
/// Represents the result of calendar aggregation with statistics.
/// </summary>
public class AggregationResult
{
    /// <summary>
    /// Gets or sets the count of downloaded events by source name.
    /// </summary>
    public Dictionary<string, int> DownloadedBySources { get; set; } = new();

    /// <summary>
    /// Gets or sets the count of blocked events.
    /// </summary>
    public int BlockedCount { get; set; }

    /// <summary>
    /// Gets or sets the count of duplicate events removed.
    /// </summary>
    public int DuplicatesCount { get; set; }

    /// <summary>
    /// Gets or sets the count of remaining events in the final calendar.
    /// </summary>
    public int RemainingCount { get; set; }

    /// <summary>
    /// Gets or sets the list of download failures.
    /// </summary>
    public List<DownloadFailure> Failures { get; set; } = new();

    /// <summary>
    /// Gets the total count of events downloaded across all sources.
    /// </summary>
    public int TotalDownloaded => DownloadedBySources.Values.Sum();
}

/// <summary>
/// Represents a download failure for a calendar source.
/// </summary>
public class DownloadFailure
{
    /// <summary>
    /// Gets or sets the source name.
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}
