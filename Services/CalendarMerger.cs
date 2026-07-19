using Ical.Net;
using Ical.Net.CalendarComponents;
using IcsCalendarAggregator.Services.Abstractions;

namespace IcsCalendarAggregator.Services;

/// <summary>
/// Merges calendar events with deduplication logic.
/// </summary>
public class CalendarMerger : ICalendarMerger
{
    private readonly ILogger<CalendarMerger> _logger;

    /// <summary>
    /// Initializes a new instance of the CalendarMerger class.
    /// </summary>
    public CalendarMerger(ILogger<CalendarMerger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Merges a list of events into a new calendar with deduplication applied.
    /// </summary>
    public (Calendar MergedCalendar, int DuplicatesRemoved) MergeEventsIntoCalendar(
        List<CalendarEvent> events)
    {
        var calendar = new Calendar();
        var deduplicated = DeduplicateEvents(events, out var duplicateCount);

        foreach (var calEvent in deduplicated)
        {
            calendar.Events.Add(calEvent);
        }

        _logger.LogInformation("Merged {EventCount} events into calendar ({DuplicatesRemoved} duplicates removed)",
            deduplicated.Count, duplicateCount);

        return (calendar, duplicateCount);
    }

    /// <summary>
    /// Deduplicates events using two-pass rules.
    /// </summary>
    private List<CalendarEvent> DeduplicateEvents(
        List<CalendarEvent> events,
        out int duplicatesRemoved)
    {
        duplicatesRemoved = 0;
        var seen = new HashSet<string>();
        var result = new List<CalendarEvent>();

        // Pass 1: Remove duplicates by UID
        var passOneResult = new Dictionary<string, CalendarEvent>();
        var pass1Duplicates = 0;

        foreach (var calEvent in events)
        {
            var uid = calEvent.Uid;
            if (!string.IsNullOrWhiteSpace(uid))
            {
                if (passOneResult.ContainsKey(uid))
                {
                    pass1Duplicates++;
                    _logger.LogDebug("Duplicate UID found: {Uid}", uid);
                }
                else
                {
                    passOneResult[uid] = calEvent;
                }
            }
            else
            {
                // No UID, keep it for now
                passOneResult[Guid.NewGuid().ToString()] = calEvent;
            }
        }

        duplicatesRemoved += pass1Duplicates;

        // Pass 2: Remove duplicates by Summary + DTSTART (case-insensitive summary)
        var pass2Duplicates = 0;

        foreach (var calEvent in passOneResult.Values)
        {
            var summaryLower = (calEvent.Summary ?? string.Empty).ToLowerInvariant();
            var dtStartKey = calEvent.DtStart?.AsSystemLocal.ToString("O") ?? "null";
            var key = $"{summaryLower}|{dtStartKey}";

            if (seen.Contains(key))
            {
                pass2Duplicates++;
                _logger.LogDebug(
                    "Duplicate event detected: Summary={Summary}, DtStart={DtStart}",
                    calEvent.Summary, calEvent.DtStart?.AsSystemLocal);
            }
            else
            {
                seen.Add(key);
                result.Add(calEvent);
            }
        }

        duplicatesRemoved += pass2Duplicates;

        _logger.LogInformation(
            "Deduplication complete: Pass 1 (UID): {Pass1}, Pass 2 (Summary+DtStart): {Pass2}, Total removed: {Total}",
            pass1Duplicates, pass2Duplicates, duplicatesRemoved);

        return result;
    }
}
