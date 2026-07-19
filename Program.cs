using IcsCalendarAggregator.Configuration;
using IcsCalendarAggregator.Services;
using IcsCalendarAggregator.Services.Abstractions;
using Serilog;

public class Program
{
    public static async global::System.Threading.Tasks.Task Main(string[] args)
    {
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

        // Handle command line argument for offline generation
        var commandLineArgs = Environment.GetCommandLineArgs();
        if (commandLineArgs.Contains("--output-file"))
        {
            var outputIndex = Array.IndexOf(commandLineArgs, "--output-file");
            if (outputIndex >= 0 && outputIndex + 1 < commandLineArgs.Length)
            {
                var outputPath = commandLineArgs[outputIndex + 1];
                var aggregatorService = app.Services.GetRequiredService<ICalendarAggregatorService>();
                var calendar = await aggregatorService.AggregateCalendarsAsync();

                if (calendar != null)
                {
                    File.WriteAllText(outputPath, calendar.ToString());
                    Log.Information("Calendar written to {OutputPath}", outputPath);
                }
                else
                {
                    Log.Error("Failed to generate calendar");
                }

                Log.CloseAndFlush();
                return;
            }
        }

        app.UseHttpsRedirection();

        // Endpoints
        app.MapGet("/calendar.ics", CalendarEndpoints.GetCalendarIcs)
            .WithName("GetCalendarIcs")
            .Produces(StatusCodes.Status200OK, typeof(string), "text/calendar")
            .Produces(StatusCodes.Status500InternalServerError);

        app.MapGet("/health", CalendarEndpoints.GetHealth)
            .WithName("GetHealth")
            .Produces(StatusCodes.Status200OK);

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
    }
}

/// <summary>
/// Calendar endpoints for the aggregator API.
/// </summary>
internal static class CalendarEndpoints
{
    /// <summary>
    /// Gets the aggregated ICS calendar feed.
    /// </summary>
    public static async Task<IResult> GetCalendarIcs(
        ICalendarAggregatorService aggregatorService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("CalendarEndpoint");
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
