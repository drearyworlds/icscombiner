using Ical.Net.CalendarComponents;

namespace IcsCalendarAggregator.Services.Abstractions;

/// <summary>
/// Defines methods for filtering calendar events.
/// </summary>
public interface ICalendarFilter
{
    /// <summary>
    /// Filters a list of events based on configured rules.
    /// </summary>
    /// <param name="events">The events to filter.</param>
    /// <returns>A tuple containing the filtered events and the count of blocked events.</returns>
    (List<CalendarEvent> FilteredEvents, int BlockedCount) FilterEvents(List<CalendarEvent> events);
}
