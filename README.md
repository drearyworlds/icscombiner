# ICS Calendar Aggregator

A .NET 8 ASP.NET Core Minimal API that aggregates multiple remote ICS calendar feeds into a single merged calendar.

## Features

- ✅ Fetch multiple ICS feeds concurrently
- ✅ Robust error handling (continues if a feed fails)
- ✅ Event filtering by date range and title
- ✅ Intelligent event deduplication
- ✅ In-memory caching with configurable TTL
- ✅ Comprehensive logging with Serilog
- ✅ Dependency injection and clean architecture
- ✅ RFC 5545 compliant output
- ✅ Full nullable reference type support

## Getting Started

### Prerequisites

- .NET 8 SDK
- A recent version of Visual Studio Code, Visual Studio, or Rider (optional)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/drearyworlds/icscombiner.git
cd icscombiner
```

2. Restore NuGet packages:
```bash
dotnet restore
```

3. The calendar sources are pre-configured in `appsettings.json` with the Huntsville, AL event feeds.

### Running the Application

```bash
dotnet run
```

The application will start and listen on `https://localhost:5001`.

## API Endpoints

### GET /calendar.ics
Returns the aggregated ICS calendar feed.

**Response:**
- Content-Type: `text/calendar; charset=utf-8`
- Status: 200 OK (if at least one calendar could be produced)
- Status: 500 Internal Server Error (if no valid calendars could be produced)

Example:
```bash
curl -H "Accept: text/calendar" https://localhost:5001/calendar.ics > calendar.ics
```

### GET /health
Returns the health status of the aggregator service.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

## Configuration

Edit `appsettings.json` to customize behavior:

| Setting | Default | Description |
|---------|---------|-------------|
| `CacheMinutes` | 30 | How long to cache the merged calendar |
| `PastDays` | 30 | Include events from this many days in the past |
| `FutureDays` | 540 | Include events up to this many days in the future |
| `BlockedTitles` | [] | Event titles to exclude (case-insensitive) |
| `Sources` | [] | Array of calendar feed URLs to aggregate |

### Pre-configured Sources

The application comes pre-configured with three Huntsville, AL event sources:

1. **Timely Events** - `https://timelyapp.time.ly/api/calendars/54704218/export?format=ics`
2. **Stove House** - `https://www.stovehouse.com/events/list/?ical=1`
3. **Huntsville City Calendar** - `https://www.huntsvilleal.gov/city-calendar/month/?ical=1`

## Architecture

The application follows a layered, testable architecture:

### Services

- **CalendarFetcher**: Downloads calendars from remote sources concurrently
- **CalendarFilter**: Removes events based on date range and title rules
- **CalendarMerger**: Combines events with intelligent deduplication
- **CalendarCache**: Provides in-memory caching
- **CalendarAggregatorService**: Orchestrates the entire pipeline

### Key Design Principles

- **Dependency Injection**: All services registered with the DI container
- **Async/Await**: Fully asynchronous throughout
- **Error Resilience**: Continues processing if individual feeds fail
- **Nullable Reference Types**: Full adoption of C# 8+ nullable types
- **Logging**: Comprehensive logging with Serilog
- **Testability**: Services are abstracted and mockable

## Event Deduplication

The deduplication algorithm uses a two-pass approach:

### Pass 1: UID Matching
If two events have the same UID (calendar-specific unique identifier), keep only the first.

### Pass 2: Content Matching
If UIDs differ, consider events duplicates when:
- Summary matches exactly (case-insensitive)
- DTSTART is identical

When duplicates are found, the first encountered event is kept.

## Event Filtering

Events are removed if:
1. **Summary matches a blocked title** (case-insensitive comparison)
2. **Start time is before** `DateTime.Now - PastDays`
3. **Start time is after** `DateTime.Now + FutureDays`

## Logging

Comprehensive logging is provided via Serilog. Configure log levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "IcsCalendarAggregator": "Debug"
    }
  }
}
```

**Example log output:**
```
[2024-01-15 10:30:15.123 +00:00] [INF] Downloading calendar from: Timely Events (https://timelyapp.time.ly/api/calendars/54704218/export?format=ics)
[2024-01-15 10:30:15.456 +00:00] [INF] Successfully downloaded Timely Events
[2024-01-15 10:30:15.789 +00:00] [INF] === Calendar Aggregation Summary ===
[2024-01-15 10:30:15.790 +00:00] [INF] Downloaded:
[2024-01-15 10:30:15.791 +00:00] [INF]   Timely Events: 143
[2024-01-15 10:30:15.792 +00:00] [INF]   Stove House: 56
[2024-01-15 10:30:15.793 +00:00] [INF]   Huntsville City Calendar: 98
[2024-01-15 10:30:15.794 +00:00] [INF] Blocked: 12
[2024-01-15 10:30:15.795 +00:00] [INF] Duplicates: 17
[2024-01-15 10:30:15.796 +00:00] [INF] Remaining: 268
[2024-01-15 10:30:15.797 +00:00] [INF] =====================================
```

## Error Handling

The application implements graceful error handling:

- **Individual Feed Failures**: If one or more feeds fail to download, the application continues processing the remaining feeds
- **Partial Success**: Returns a valid calendar if at least one feed succeeds
- **Complete Failure**: Returns HTTP 500 only if no valid calendars could be produced
- **Detailed Logging**: All failures are logged for troubleshooting

## Performance Considerations

- **Concurrent Downloads**: All calendar feeds are downloaded in parallel for optimal performance
- **In-Memory Caching**: The merged calendar is cached to reduce processing overhead
- **Streaming Output**: The calendar is streamed to clients without full buffering
- **Efficient Deduplication**: Uses hash-based lookups for O(n) deduplication

## Building for Production

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "IcsCalendarAggregator.dll"]
```

Build and run:
```bash
docker build -t ics-aggregator .
docker run -p 80:80 ics-aggregator
```

### Kubernetes

Use environment variables to override configuration:

```yaml
env:
  - name: CalendarAggregator__CacheMinutes
    value: "60"
  - name: CalendarAggregator__Sources__0__Name
    value: "Example"
  - name: CalendarAggregator__Sources__0__Url
    value: "https://example.com/calendar.ics"
```

## Development

### Running Tests

The architecture supports unit testing. Example test:

```csharp
[Test]
public async Task AggregateCalendarsAsync_WithFailedSource_ContinuesProcessing()
{
    // Arrange
    var mockFetcher = new Mock<ICalendarFetcher>();
    mockFetcher.Setup(f => f.FetchCalendarsAsync())
        .ReturnsAsync(new Dictionary<string, Calendar?> 
        { 
            { "Source1", new Calendar() },
            { "Source2", null }
        });

    var service = new CalendarAggregatorService(
        mockFetcher.Object,
        // ... other mocks
    );

    // Act
    var result = await service.AggregateCalendarsAsync();

    // Assert
    Assert.NotNull(result);
}
```

## Troubleshooting

### "No events found in any calendar"
- Check that calendar URLs are accessible and return valid ICS data
- Verify network connectivity
- Check logs for download or parsing errors

### Cache not refreshing
- Check the `CacheMinutes` setting in `appsettings.json`
- Wait for the cache to expire naturally
- Restart the application to clear the cache

### Events being filtered out
- Review `PastDays` and `FutureDays` settings
- Check `BlockedTitles` for accidental matches
- Check logs for filtering details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is open source and available under the MIT License.

## Support

For issues, questions, or suggestions, please open an issue on GitHub.
