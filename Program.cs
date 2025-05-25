using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using VideoScripts.Data;
using VideoScripts.Features.YouTube;
using VideoScripts.Features.RetrieveTranscript;
using VideoScripts.Features.TranscriptSummary;

namespace VideoScripts
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Setup configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.develop.json", optional: true, reloadOnChange: true)
                .Build();

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services, config);
            var serviceProvider = services.BuildServiceProvider();

            // Configure database context
            var connectionString = config.GetConnectionString("DefaultConnection");
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            // Create database context
            using var dbContext = new AppDbContext(dbContextOptions);
            await dbContext.Database.MigrateAsync();

            // Access configuration values
            var greeting = config["AppSettings:Greeting"];
            var environment = config["AppSettings:Environment"];
            Console.WriteLine(greeting);
            Console.WriteLine($"Environment: {environment}");

            // Google Drive/Sheets integration
            var credentialsPath = config["Google:ServiceAccountCredentialsPath"];
            var spreadsheetName = config["Google:SpreadsheetName"];

            if (string.IsNullOrEmpty(credentialsPath) || string.IsNullOrEmpty(spreadsheetName))
            {
                Console.WriteLine("Google credentials path or spreadsheet name not configured.");
                return;
            }

            var googleService = new GoogleDriveService(credentialsPath);
            var spreadsheetId = googleService.FindSpreadsheetIdByName(spreadsheetName);

            if (spreadsheetId == null)
            {
                Console.WriteLine($"Spreadsheet '{spreadsheetName}' not found.");
                return;
            }

            // Get processing handlers
            var youTubeHandler = serviceProvider.GetRequiredService<YouTubeProcessingHandler>();
            var transcriptHandler = serviceProvider.GetRequiredService<TranscriptProcessingHandler>();
            var summaryHandler = serviceProvider.GetRequiredService<TranscriptSummaryHandler>();

            // Get all unimported rows
            var unimportedRows = googleService.GetUnimportedRows(spreadsheetId);

            if (!unimportedRows.Any())
            {
                Console.WriteLine("No unimported rows found in the spreadsheet.");

                // Check if user wants to process existing projects
                Console.WriteLine("\nWould you like to process existing projects? (y/n)");
                var response = Console.ReadLine();

                if (response?.ToLower() == "y")
                {
                    await ProcessExistingProjects(transcriptHandler, summaryHandler, dbContext);
                }

                return;
            }

            Console.WriteLine($"Found {unimportedRows.Count} unimported row(s):");
            Console.WriteLine(new string('=', 80));

            var processedRowNumbers = new List<int>();
            var projectsToProcess = new List<string>();

            // Process YouTube videos first
            foreach (var (row, rowNumber, headers) in unimportedRows)
            {
                Console.WriteLine($"\nProcessing Row {rowNumber}:");
                Console.WriteLine(new string('-', 40));

                var projectName = GetCellValue(row, headers, "Project Name");
                var videoUrls = new List<string>
                {
                    GetCellValue(row, headers, "Video 1"),
                    GetCellValue(row, headers, "Video 2"),
                    GetCellValue(row, headers, "Video 3"),
                    GetCellValue(row, headers, "Video 4"),
                    GetCellValue(row, headers, "Video 5")
                };

                if (string.IsNullOrWhiteSpace(projectName))
                {
                    Console.WriteLine($"Skipping row {rowNumber}: No project name found");
                    continue;
                }

                Console.WriteLine($"Project: {projectName}");
                Console.WriteLine($"Video URLs: {string.Join(", ", videoUrls.Where(url => !string.IsNullOrWhiteSpace(url)))}");

                try
                {
                    // Process the row using YouTube service
                    var result = await youTubeHandler.ProcessSheetRowAsync(projectName, videoUrls);

                    if (result.Success)
                    {
                        Console.WriteLine($"Successfully processed {result.ProcessedVideos.Count} videos");

                        foreach (var video in result.ProcessedVideos)
                        {
                            var status = video.Success ? "✅" : "❌";
                            Console.WriteLine($"   {status} {video.Title} - {video.Message}");
                        }

                        processedRowNumbers.Add(rowNumber);

                        // Add project to processing queue
                        if (!projectsToProcess.Contains(projectName))
                        {
                            projectsToProcess.Add(projectName);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to process row: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing row {rowNumber}: {ex.Message}");
                }

                Console.WriteLine();
            }

            // Mark processed rows as imported
            if (processedRowNumbers.Any())
            {
                Console.WriteLine($"Marking {processedRowNumbers.Count} row(s) as imported...");

                var updatedCount = await googleService.MarkRowsAsImportedAsync(
                    spreadsheetId,
                    processedRowNumbers,
                    unimportedRows.First().headers);

                Console.WriteLine($"Successfully marked {updatedCount} cells as imported.");
            }

            Console.WriteLine("\nYouTube processing completed!");

            // Process transcripts and summaries for new projects
            if (projectsToProcess.Any())
            {
                foreach (var projectName in projectsToProcess)
                {
                    await ProcessFullProjectPipeline(transcriptHandler, summaryHandler, projectName);
                }
            }

            Console.WriteLine("\nAll processing completed!");

            // Clean up
            await serviceProvider.DisposeAsync();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(configuration);

            // Add logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsoleFormatter<SimpleConsoleFormatter, SimpleConsoleFormatterOptions>();
                builder.AddConsole(options => options.FormatterName = "simple");
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            });

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
        }

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

        /// <summary>
        /// Processes the complete pipeline for a project: transcripts then summaries
        /// </summary>
        private static async Task ProcessFullProjectPipeline(
            TranscriptProcessingHandler transcriptHandler,
            TranscriptSummaryHandler summaryHandler,
            string projectName)
        {
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"PROCESSING PIPELINE FOR PROJECT: {projectName}");
            Console.WriteLine(new string('=', 80));

            // Step 1: Process transcripts
            await ProcessProjectTranscripts(transcriptHandler, projectName);

            // Step 2: Process summaries (only after transcripts are complete)
            await ProcessProjectSummaries(summaryHandler, projectName);
        }

        /// <summary>
        /// Processes transcripts for a specific project
        /// </summary>
        private static async Task ProcessProjectTranscripts(TranscriptProcessingHandler transcriptHandler, string projectName)
        {
            Console.WriteLine($"\nTRANSCRIPT PROCESSING: {projectName}");
            Console.WriteLine(new string('-', 50));

            try
            {
                // Get current status
                var status = await transcriptHandler.GetProjectTranscriptStatusAsync(projectName);

                if (!status.ProjectExists)
                {
                    Console.WriteLine($"Project '{projectName}' not found");
                    return;
                }

                Console.WriteLine($"  Transcript Status:");
                Console.WriteLine($"   - Total Videos: {status.TotalVideos}");
                Console.WriteLine($"   - With Transcripts: {status.VideosWithTranscripts}");
                Console.WriteLine($"   - Without Transcripts: {status.VideosWithoutTranscripts}");

                if (status.IsComplete)
                {
                    Console.WriteLine($"All videos already have transcripts");
                    return;
                }

                // Process transcripts
                var result = await transcriptHandler.ProcessProjectTranscriptsAsync(projectName);

                if (result.Success)
                {
                    Console.WriteLine($"  Transcript processing completed:");
                    Console.WriteLine($"   - Successful: {result.SuccessfulCount}");
                    Console.WriteLine($"   - Failed: {result.FailedCount}");

                    // Show details for each video
                    foreach (var transcript in result.ProcessedTranscripts)
                    {
                        var status_icon = transcript.Success ? "✅" : "❌";
                        var lengthInfo = transcript.Success ? $"({transcript.TranscriptLength} chars)" : "";
                        Console.WriteLine($"   {status_icon} {transcript.Title} {lengthInfo} - {transcript.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Transcript processing failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing transcripts: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes AI summaries for a specific project
        /// </summary>
        private static async Task ProcessProjectSummaries(TranscriptSummaryHandler summaryHandler, string projectName)
        {
            Console.WriteLine($"\n AI SUMMARY PROCESSING: {projectName}");
            Console.WriteLine(new string('-', 50));

            try
            {
                // Get current status
                var status = await summaryHandler.GetProjectSummaryStatusAsync(projectName);

                if (!status.ProjectExists)
                {
                    Console.WriteLine($"Project '{projectName}' not found");
                    return;
                }

                Console.WriteLine($" Summary Status:");
                Console.WriteLine($"  - Total Videos: {status.TotalVideos}");
                Console.WriteLine($"  - With Transcripts: {status.VideosWithTranscripts}");
                Console.WriteLine($"  - With Summaries: {status.VideosWithSummaries}");
                Console.WriteLine($"  - Needing Summaries: {status.VideosNeedingSummaries}");

                if (status.IsComplete)
                {
                    Console.WriteLine($"All videos with transcripts already have summaries");
                    return;
                }

                if (status.VideosNeedingSummaries == 0)
                {
                    Console.WriteLine($"No videos with transcripts found to summarize");
                    return;
                }

                // Process summaries
                var result = await summaryHandler.ProcessProjectSummariesAsync(projectName);

                if (result.Success)
                {
                    Console.WriteLine($" Summary processing completed:");
                    Console.WriteLine($"  - Successful: {result.SuccessfulCount}");
                    Console.WriteLine($"  - Failed: {result.FailedCount}");

                    // Show details for each video
                    foreach (var summary in result.ProcessedSummaries)
                    {
                        var status_icon = summary.Success ? "✅" : " X ";
                        var topicInfo = summary.Success && !string.IsNullOrWhiteSpace(summary.VideoTopic)
                            ? $"Topic: {summary.VideoTopic.Substring(0, Math.Min(50, summary.VideoTopic.Length))}..."
                            : "";
                        var lengthInfo = summary.Success ? $"({summary.SummaryLength} chars)" : "";
                        Console.WriteLine($"   {status_icon} {summary.Title} {lengthInfo}");
                        if (!string.IsNullOrWhiteSpace(topicInfo))
                        {
                            Console.WriteLine($"       {topicInfo}");
                        }
                        Console.WriteLine($"       {summary.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Summary processing failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing summaries: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes existing projects for transcripts and summaries
        /// </summary>
        private static async Task ProcessExistingProjects(
            TranscriptProcessingHandler transcriptHandler,
            TranscriptSummaryHandler summaryHandler,
            AppDbContext dbContext)
        {
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("PROCESSING EXISTING PROJECTS");
            Console.WriteLine(new string('=', 80));

            try
            {
                // Get all projects
                var projects = await dbContext.Projects.ToListAsync();

                if (!projects.Any())
                {
                    Console.WriteLine("No projects found in database.");
                    return;
                }

                Console.WriteLine($"Found {projects.Count} project(s):");

                foreach (var project in projects)
                {
                    Console.WriteLine($"\nProcessing: {project.Name}");
                    await ProcessFullProjectPipeline(transcriptHandler, summaryHandler, project.Name);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing existing projects: {ex.Message}");
            }
        }

        private static string GetCellValue(IList<object> row, IList<string> headers, string columnName)
        {
            var columnIndex = headers.IndexOf(columnName);
            if (columnIndex >= 0 && columnIndex < row.Count)
            {
                return row[columnIndex]?.ToString()?.Trim() ?? string.Empty;
            }
            return string.Empty;
        }
    }
}