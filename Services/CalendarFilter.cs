using Ical.Net.CalendarComponents;
using IcsCalendarAggregator.Configuration;
using IcsCalendarAggregator.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace IcsCalendarAggregator.Services;

/// <summary>
/// Filters calendar events based on configured rules.
/// </summary>
public class CalendarFilter : ICalendarFilter
{
    private readonly ILogger<CalendarFilter> _logger;
    private readonly CalendarAggregatorOptions _options;

    /// <summary>
    /// Initializes a new instance of the CalendarFilter class.
    /// </summary>
    public CalendarFilter(
        ILogger<CalendarFilter> logger,
        IOptions<CalendarAggregatorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Filters a list of events based on configured rules.
    /// </summary>
    public (List<CalendarEvent> FilteredEvents, int BlockedCount) FilterEvents(
        List<CalendarEvent> events)
    {
        var filtered = new List<CalendarEvent>();
        var blockedCount = 0;

        var pastDate = DateTime.Now.AddDays(-_options.PastDays);
        var futureDate = DateTime.Now.AddDays(_options.FutureDays);

        var blockedTitlesLower = _options.BlockedTitles
            .Select(t => t.ToLowerInvariant())
            .ToHashSet();

        foreach (var calEvent in events)
        {
            // Check if title is in blocked list (case-insensitive)
            if (!string.IsNullOrWhiteSpace(calEvent.Summary) &&
                blockedTitlesLower.Contains(calEvent.Summary.ToLowerInvariant()))
            {
                _logger.LogDebug("Blocking event with title: {Title}", calEvent.Summary);
                blockedCount++;
                continue;
            }

            // Check if event start is in the valid time range
            if (calEvent.DtStart?.AsSystemLocal < pastDate ||
                calEvent.DtStart?.AsSystemLocal > futureDate)
            {
                _logger.LogDebug(
                    "Blocking event {Title} due to time constraints (Start: {Start})",
                    calEvent.Summary, calEvent.DtStart?.AsSystemLocal);
                blockedCount++;
                continue;
            }

            filtered.Add(calEvent);
        }

        _logger.LogInformation("Filtered events: {Blocked} blocked, {Remaining} remaining",
            blockedCount, filtered.Count);

        return (filtered, blockedCount);
    }
}
