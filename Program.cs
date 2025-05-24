// Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VideoScripts.Data;
using VideoScripts.Features.YouTube;
using VideoScripts.Features.RetrieveTranscript;

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
            Console.WriteLine();

            // Main menu for choosing operation
            Console.WriteLine("Select an operation:");
            Console.WriteLine("1. Process YouTube videos from Google Sheets");
            Console.WriteLine("2. Retrieve missing transcripts");
            Console.WriteLine("3. Exit");
            Console.Write("Enter your choice (1-3): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await ProcessYouTubeVideosFromSheets(config, serviceProvider);
                    break;
                case "2":
                    await ProcessMissingTranscripts(serviceProvider);
                    break;
                case "3":
                    Console.WriteLine("Goodbye!");
                    break;
                default:
                    Console.WriteLine("Invalid choice. Exiting.");
                    break;
            }

            // Clean up
            await serviceProvider.DisposeAsync();
        }

        /// <summary>
        /// Processes YouTube videos from Google Sheets (existing functionality)
        /// </summary>
        private static async Task ProcessYouTubeVideosFromSheets(IConfiguration config, ServiceProvider serviceProvider)
        {
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("PROCESSING YOUTUBE VIDEOS FROM GOOGLE SHEETS");
            Console.WriteLine(new string('=', 80));

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

            // Get YouTube processing handler
            var youTubeHandler = serviceProvider.GetRequiredService<YouTubeProcessingHandler>();

            // Get all unimported rows
            var unimportedRows = googleService.GetUnimportedRows(spreadsheetId);

            if (!unimportedRows.Any())
            {
                Console.WriteLine("No unimported rows found in the spreadsheet.");
                return;
            }

            Console.WriteLine($"Found {unimportedRows.Count} unimported row(s):");
            Console.WriteLine(new string('=', 80));

            var processedRowNumbers = new List<int>();

            foreach (var (row, rowNumber, headers) in unimportedRows)
            {
                Console.WriteLine($"\nProcessing Row {rowNumber}:");
                Console.WriteLine(new string('-', 40));

                // Extract project name and video URLs from the row
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
                        Console.WriteLine($"✅ Successfully processed {result.ProcessedVideos.Count} videos");

                        foreach (var video in result.ProcessedVideos)
                        {
                            var status = video.Success ? "✅" : "❌";
                            Console.WriteLine($"   {status} {video.Title} - {video.Message}");
                        }

                        processedRowNumbers.Add(rowNumber);
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to process row: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error processing row {rowNumber}: {ex.Message}");
                }

                Console.WriteLine();
            }

            // Mark all processed rows as imported in a batch operation
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
        }

        /// <summary>
        /// Processes missing transcripts for videos in the database
        /// </summary>
        private static async Task ProcessMissingTranscripts(ServiceProvider serviceProvider)
        {
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("RETRIEVING MISSING TRANSCRIPTS");
            Console.WriteLine(new string('=', 80));

            try
            {
                var transcriptHandler = serviceProvider.GetRequiredService<RetrieveTranscriptHandler>();
                var result = await transcriptHandler.ProcessMissingTranscriptsAsync();

                Console.WriteLine($"\nTranscript Processing Results:");
                Console.WriteLine(new string('-', 50));
                Console.WriteLine($"Status: {(result.Success ? "✅ SUCCESS" : "❌ FAILED")}");
                Console.WriteLine($"Message: {result.Message}");

                if (result.TotalVideosProcessed > 0)
                {
                    Console.WriteLine($"Total Videos Processed: {result.TotalVideosProcessed}");
                    Console.WriteLine($"Successful: {result.SuccessfulTranscripts}");
                    Console.WriteLine($"Failed: {result.FailedTranscripts}");

                    if (result.VideoResults.Any())
                    {
                        Console.WriteLine("\nDetailed Results:");
                        Console.WriteLine(new string('-', 50));

                        foreach (var videoResult in result.VideoResults)
                        {
                            var status = videoResult.Success ? "✅" : "❌";
                            Console.WriteLine($"{status} {videoResult.VideoTitle} (ID: {videoResult.VideoId})");

                            if (videoResult.Success)
                            {
                                Console.WriteLine($"   📝 Transcript Length: {videoResult.TranscriptLength:N0} characters");
                            }
                            else
                            {
                                Console.WriteLine($"   ❌ Error: {videoResult.ErrorMessage}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Critical error during transcript processing: {ex.Message}");
            }

            Console.WriteLine("\nTranscript processing completed!");
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(configuration);

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add HttpClient
            services.AddHttpClient();

            // Add our custom services
            services.AddScoped<YouTubeService>();
            services.AddScoped<YouTubeProcessingHandler>();
            services.AddScoped<TranscriptService>();
            services.AddScoped<RetrieveTranscriptHandler>();

            // Add Entity Framework
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
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