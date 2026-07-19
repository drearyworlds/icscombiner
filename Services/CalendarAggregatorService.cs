using Ical.Net;
using Ical.Net.CalendarComponents;
using IcsCalendarAggregator.Models;
using IcsCalendarAggregator.Services.Abstractions;

namespace IcsCalendarAggregator.Services;

/// <summary>
/// Orchestrates the calendar aggregation process.
/// </summary>
public class CalendarAggregatorService : ICalendarAggregatorService
{
    private readonly ICalendarCache _cache;
    private readonly ICalendarFetcher _fetcher;
    private readonly ICalendarFilter _filter;
    private readonly ILogger<CalendarAggregatorService> _logger;
    private readonly ICalendarMerger _merger;

    /// <summary>
    /// Initializes a new instance of the CalendarAggregatorService class.
    /// </summary>
    public CalendarAggregatorService(
        ICalendarFetcher fetcher,
        ICalendarFilter filter,
        ICalendarMerger merger,
        ICalendarCache cache,
        ILogger<CalendarAggregatorService> logger)
    {
        _fetcher = fetcher;
        _filter = filter;
        _merger = merger;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Aggregates calendars from all configured sources.
    /// </summary>
    public async Task<Calendar?> AggregateCalendarsAsync()
    {
        // Check cache first
        if (_cache.TryGetCachedCalendar(out var cachedCalendar))
        {
            return cachedCalendar;
        }

        _logger.LogInformation("Starting calendar aggregation");

        // Step 1: Fetch calendars from all sources
        var calendars = await _fetcher.FetchCalendarsAsync();

        if (calendars == null || calendars.Count == 0)
        {
            _logger.LogError("No calendars could be fetched from any source");
            return null;
        }

        var result = new AggregationResult();

        // Step 2: Collect all events from valid calendars
        var allEvents = new List<CalendarEvent>();

        foreach (var (sourceName, calendar) in calendars)
        {
            if (calendar == null)
            {
                result.Failures.Add(new DownloadFailure
                {
                    SourceName = sourceName,
                    Error = "Failed to parse or download calendar"
                });
                continue;
            }

            var eventCount = calendar.Events.Count;
            result.DownloadedBySources[sourceName] = eventCount;
            allEvents.AddRange(calendar.Events);
        }

        result.TotalDownloaded = allEvents.Count;

        if (allEvents.Count == 0)
        {
            _logger.LogError("No events found in any calendar");
            return null;
        }

        _logger.LogInformation("Downloaded {TotalEvents} events from {SourceCount} sources",
            allEvents.Count, result.DownloadedBySources.Count);

        // Step 3: Filter events
        var (filteredEvents, blockedCount) = _filter.FilterEvents(allEvents);
        result.BlockedCount = blockedCount;

        if (filteredEvents.Count == 0)
        {
            _logger.LogWarning("All events were filtered out");
            return null;
        }

        // Step 4: Merge and deduplicate events
        var (mergedCalendar, duplicatesRemoved) = _merger.MergeEventsIntoCalendar(filteredEvents);
        result.DuplicatesCount = duplicatesRemoved;
        result.RemainingCount = mergedCalendar.Events.Count;

        // Log aggregation statistics
        LogAggregationResult(result);

        // Cache the result
        _cache.SetCalendar(mergedCalendar);

        return mergedCalendar;
    }

    /// <summary>
    /// Logs the aggregation result with statistics.
    /// </summary>
    private void LogAggregationResult(AggregationResult result)
    {
        _logger.LogInformation("=== Calendar Aggregation Summary ===");

        _logger.LogInformation("Downloaded:");
        foreach (var (sourceName, count) in result.DownloadedBySources)
        {
            _logger.LogInformation("  {SourceName}: {Count}", sourceName, count);
        }

        if (result.Failures.Count > 0)
        {
            _logger.LogWarning("Failed Sources:");
            foreach (var failure in result.Failures)
            {
                _logger.LogWarning("  {SourceName}: {Error}", failure.SourceName, failure.Error);
            }
        }

        _logger.LogInformation("Blocked: {BlockedCount}", result.BlockedCount);
        _logger.LogInformation("Duplicates: {DuplicatesCount}", result.DuplicatesCount);
        _logger.LogInformation("Remaining: {RemainingCount}", result.RemainingCount);
        _logger.LogInformation("=====================================");
    }
}
