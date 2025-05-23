using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VideoScripts.Data;
using VideoScripts.Features.YouTube;

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

            // Clean up
            await serviceProvider.DisposeAsync();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
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