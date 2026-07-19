using Ical.Net;

namespace IcsCalendarAggregator.Services.Abstractions;

/// <summary>
/// Defines the main calendar aggregation orchestration service.
/// </summary>
public interface ICalendarAggregatorService
{
    /// <summary>
    /// Aggregates calendars from all configured sources.
    /// </summary>
    /// <returns>The merged calendar, or null if no valid calendars could be produced.</returns>
    Task<Calendar?> AggregateCalendarsAsync();
}
