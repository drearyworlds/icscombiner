using Ical.Net;

namespace IcsCalendarAggregator.Services.Abstractions;

/// <summary>
/// Defines methods for fetching calendar feeds from remote sources.
/// </summary>
public interface ICalendarFetcher
{
    /// <summary>
    /// Fetches a list of calendars from the configured sources concurrently.
    /// </summary>
    /// <returns>A dictionary mapping source names to their calendars, or null if fetch failed.</returns>
    Task<Dictionary<string, Calendar?>> FetchCalendarsAsync();
}
