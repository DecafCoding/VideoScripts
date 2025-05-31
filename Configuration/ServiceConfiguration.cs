using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using VideoScripts.Data;
using VideoScripts.Features.YouTube;
using VideoScripts.Features.RetrieveTranscript;
using VideoScripts.Features.TranscriptSummary;
using VideoScripts.Features.TopicDiscovery;
using VideoScripts.Core;
using Microsoft.Extensions.Logging.Abstractions;
using VideoScripts.Features.ClusterTopics;
using VideoScripts.Features.ShowClusters;
using VideoScripts.Features.AnalyzeClusters;
using VideoScripts.Features.CreateScript;

namespace VideoScripts.Configuration;

public static class ServiceConfiguration
{
    /// <summary>
    /// Configures all application services for dependency injection
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        // Add logging with custom formatter
        ConfigureLogging(services);

        // Add HttpClient
        services.AddHttpClient();

        // Add Entity Framework
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Add YouTube services
        services.AddScoped<YouTubeService>();
        services.AddScoped<YouTubeProcessingHandler>();

        // Add Transcript services
        services.AddScoped<TranscriptService>();
        services.AddScoped<TranscriptProcessingHandler>();

        // Add Summary services
        services.AddScoped<TranscriptSummaryService>();
        services.AddScoped<TranscriptSummaryHandler>();

        // Add Topic Discovery services
        services.AddScoped<TopicDiscoveryService>();
        services.AddScoped<TopicDiscoveryHandler>();

        // Add ClusterTopics services
        services.AddScoped<ClusterTopicsService>();
        services.AddScoped<ClusterTopicsHandler>();

        // Add ShowClusters services
        services.AddScoped<ShowClustersHandler>();
        services.AddScoped<ShowClustersService>();

        // Add AnalyzeClusters services
        services.AddScoped<AnalyzeClustersService>();
        services.AddScoped<AnalyzeClustersHandler>();
        services.AddScoped<AnalyzeClustersDisplayService>();

        // Add CreateScript services
        services.AddScoped<CreateScriptService>();
        services.AddScoped<CreateScriptHandler>();
        services.AddScoped<CreateScriptDisplayService>();

        // Add core orchestration service
        services.AddScoped<ProcessingOrchestrator>();
    }

    /// <summary>
    /// Configures logging with simple console formatter
    /// </summary>
    private static void ConfigureLogging(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsoleFormatter<SimpleConsoleFormatter, SimpleConsoleFormatterOptions>();
            builder.AddConsole(options => options.FormatterName = "simple");
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        });
    }
}

/// <summary>
/// Simple console formatter for cleaner log output
/// </summary>
public sealed class SimpleConsoleFormatter : ConsoleFormatter
{
    private readonly SimpleConsoleFormatterOptions _options;

    public SimpleConsoleFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options)
        : base("simple")
    {
        _options = options.CurrentValue;
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
    {
        var logLevel = logEntry.LogLevel;
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        if (message == null)
            return;

        // Simple format: [LEVEL] Message
        var levelString = GetLogLevelString(logLevel);
        textWriter.WriteLine($"{levelString} {message}");

        // Write exception if present
        if (logEntry.Exception != null)
        {
            textWriter.WriteLine($"Exception: {logEntry.Exception}");
        }
    }

    private static string GetLogLevelString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "[TRACE]",
        LogLevel.Debug => "[DEBUG]",
        LogLevel.Information => "[INFO]",
        LogLevel.Warning => "[WARN]",
        LogLevel.Error => "[ERROR]",
        LogLevel.Critical => "[CRITICAL]",
        _ => "[UNKNOWN]"
    };
}

public class SimpleConsoleFormatterOptions : ConsoleFormatterOptions
{
    // Add any custom options here if needed
}