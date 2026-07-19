using Ical.Net;
using IcsCalendarAggregator.Configuration;
using IcsCalendarAggregator.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace IcsCalendarAggregator.Services;

/// <summary>
/// Implements in-memory caching for aggregated calendars.
/// </summary>
public class CalendarCache : ICalendarCache
{
    private readonly ILogger<CalendarCache> _logger;
    private readonly CalendarAggregatorOptions _options;
    private Calendar? _cachedCalendar;
    private DateTime _cacheExpirationTime = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the CalendarCache class.
    /// </summary>
    public CalendarCache(
        ILogger<CalendarCache> logger,
        IOptions<CalendarAggregatorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Tries to get a cached calendar.
    /// </summary>
    public bool TryGetCachedCalendar(out Calendar? calendar)
    {
        calendar = null;

        if (_cachedCalendar == null)
        {
            _logger.LogDebug("Cache miss: no calendar cached");
            return false;
        }

        if (DateTime.UtcNow > _cacheExpirationTime)
        {
            _logger.LogDebug("Cache expired");
            _cachedCalendar = null;
            return false;
        }

        calendar = _cachedCalendar;
        _logger.LogInformation("Cache hit: returning cached calendar");
        return true;
    }

    /// <summary>
    /// Sets the calendar in the cache with the configured duration.
    /// </summary>
    public void SetCalendar(Calendar calendar)
    {
        _cachedCalendar = calendar;
        _cacheExpirationTime = DateTime.UtcNow.AddMinutes(_options.CacheMinutes);
        _logger.LogInformation("Calendar cached for {CacheMinutes} minutes", _options.CacheMinutes);
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        _cachedCalendar = null;
        _cacheExpirationTime = DateTime.MinValue;
        _logger.LogInformation("Cache cleared");
    }
}
