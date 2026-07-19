using Ical.Net;
using Ical.Net.CalendarComponents;

namespace IcsCalendarAggregator.Services.Abstractions;

/// <summary>
/// Defines methods for merging and deduplicating calendar events.
/// </summary>
public interface ICalendarMerger
{
    /// <summary>
    /// Merges a list of events into a new calendar with deduplication applied.
    /// </summary>
    /// <param name="events">The events to merge.</param>
    /// <returns>A tuple containing the merged calendar and the count of duplicates removed.</returns>
    (Calendar MergedCalendar, int DuplicatesRemoved) MergeEventsIntoCalendar(
        List<CalendarEvent> events);
}
