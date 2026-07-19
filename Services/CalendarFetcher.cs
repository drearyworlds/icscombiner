using Ical.Net;
using IcsCalendarAggregator.Configuration;
using IcsCalendarAggregator.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace IcsCalendarAggregator.Services;

/// <summary>
/// Fetches calendar feeds from remote sources concurrently.
/// </summary>
public class CalendarFetcher : ICalendarFetcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CalendarFetcher> _logger;
    private readonly CalendarAggregatorOptions _options;

    /// <summary>
    /// Initializes a new instance of the CalendarFetcher class.
    /// </summary>
    public CalendarFetcher(
        IHttpClientFactory httpClientFactory,
        ILogger<CalendarFetcher> logger,
        IOptions<CalendarAggregatorOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Fetches a list of calendars from the configured sources concurrently.
    /// </summary>
    public async Task<Dictionary<string, Calendar?>> FetchCalendarsAsync()
    {
        var result = new Dictionary<string, Calendar?>();
        var client = _httpClientFactory.CreateClient();

        var tasks = _options.Sources.Select(async source =>
        {
            try
            {
                _logger.LogInformation("Downloading calendar from: {SourceName} ({Url})",
                    source.Name, source.Url);

                var response = await client.GetAsync(source.Url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var calendar = Calendar.Load(content);

                _logger.LogInformation("Successfully downloaded {SourceName}",
                    source.Name);

                return (source.Name, Calendar: calendar);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download calendar from {SourceName}",
                    source.Name);
                return (source.Name, Calendar: (Calendar?)null);
            }
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (sourceName, calendar) in results)
        {
            result[sourceName] = calendar;
        }

        return result;
    }
}
