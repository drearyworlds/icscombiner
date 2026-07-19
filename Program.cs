using IcsCalendarAggregator.Configuration;
using IcsCalendarAggregator.Services;
using IcsCalendarAggregator.Services.Abstractions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        outputTemplate:
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.Configure<CalendarAggregatorOptions>(
    builder.Configuration.GetSection("CalendarAggregator"));

builder.Services.AddHttpClient();

// Register application services
builder.Services.AddScoped<ICalendarFetcher, CalendarFetcher>();
builder.Services.AddScoped<ICalendarFilter, CalendarFilter>();
builder.Services.AddScoped<ICalendarMerger, CalendarMerger>();
builder.Services.AddSingleton<ICalendarCache, CalendarCache>();
builder.Services.AddScoped<ICalendarAggregatorService, CalendarAggregatorService>();

var app = builder.Build();

app.UseHttpsRedirection();

// Endpoints
app.MapGet("/calendar.ics", CalendarEndpoints.GetCalendarIcs)
    .WithName("GetCalendarIcs")
    .WithOpenApi()
    .Produces("text/calendar")
    .Produces(500);

app.MapGet("/health", CalendarEndpoints.GetHealth)
    .WithName("GetHealth")
    .WithOpenApi()
    .Produces(200);

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Calendar endpoints for the aggregator API.
/// </summary>
public static class CalendarEndpoints
{
    /// <summary>
    /// Gets the aggregated ICS calendar feed.
    /// </summary>
    public static async Task<IResult> GetCalendarIcs(ICalendarAggregatorService aggregatorService,
        ILogger<Program> logger)
    {
        try
        {
            var calendar = await aggregatorService.AggregateCalendarsAsync();

            if (calendar == null)
            {
                logger.LogError("Failed to produce any valid calendar");
                return Results.StatusCode(500);
            }

            var icsContent = calendar.ToString();
            return Results.Content(icsContent, "text/calendar; charset=utf-8");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving calendar");
            return Results.StatusCode(500);
        }
    }

    /// <summary>
    /// Gets the health status of the aggregator service.
    /// </summary>
    public static IResult GetHealth()
    {
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
