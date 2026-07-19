using Ical.Net;

namespace IcsCalendarAggregator.Services.Abstractions;

/// <summary>
/// Defines methods for caching merged calendar results.
/// </summary>
public interface ICalendarCache
{
    /// <summary>
    /// Tries to get a cached calendar.
    /// </summary>
    /// <param name="calendar">The cached calendar, or null if not found or expired.</param>
    /// <returns>True if a valid cached calendar was found; otherwise false.</returns>
    bool TryGetCachedCalendar(out Calendar? calendar);

    /// <summary>
    /// Sets the calendar in the cache with the configured duration.
    /// </summary>
    /// <param name="calendar">The calendar to cache.</param>
    void SetCalendar(Calendar calendar);

    /// <summary>
    /// Clears the cache.
    /// </summary>
    void Clear();
}
