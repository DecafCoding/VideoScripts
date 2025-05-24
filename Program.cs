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

            // Get all unimported rows
            var unimportedRows = googleService.GetUnimportedRows(spreadsheetId);

            if (!unimportedRows.Any())
            {
                Console.WriteLine("No unimported rows found in the spreadsheet.");

                // Check if user wants to process transcripts for existing projects
                Console.WriteLine("\nWould you like to process transcripts for existing projects? (y/n)");
                var response = Console.ReadLine();

                if (response?.ToLower() == "y")
                {
                    await ProcessExistingProjectTranscripts(transcriptHandler, dbContext);
                }

                return;
            }

            Console.WriteLine($"Found {unimportedRows.Count} unimported row(s):");
            Console.WriteLine(new string('=', 80));

            var processedRowNumbers = new List<int>();
            var projectsToProcessTranscripts = new List<string>();

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
                        Console.WriteLine($"✅ Successfully processed {result.ProcessedVideos.Count} videos");

                        foreach (var video in result.ProcessedVideos)
                        {
                            var status = video.Success ? "✅" : "❌";
                            Console.WriteLine($"   {status} {video.Title} - {video.Message}");
                        }

                        processedRowNumbers.Add(rowNumber);

                        // Add project to transcript processing queue
                        if (!projectsToProcessTranscripts.Contains(projectName))
                        {
                            projectsToProcessTranscripts.Add(projectName);
                        }
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

            // Process transcripts for new projects
            if (projectsToProcessTranscripts.Any())
            {
                Console.WriteLine(new string('=', 80));
                Console.WriteLine("TRANSCRIPT PROCESSING");
                Console.WriteLine(new string('=', 80));

                foreach (var projectName in projectsToProcessTranscripts)
                {
                    await ProcessProjectTranscripts(transcriptHandler, projectName);
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
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
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
        }

        private static async Task ProcessProjectTranscripts(TranscriptProcessingHandler transcriptHandler, string projectName)
        {
            Console.WriteLine($"\nProcessing transcripts for project: {projectName}");
            Console.WriteLine(new string('-', 50));

            try
            {
                // Get current status
                var status = await transcriptHandler.GetProjectTranscriptStatusAsync(projectName);

                if (!status.ProjectExists)
                {
                    Console.WriteLine($"❌ Project '{projectName}' not found");
                    return;
                }

                Console.WriteLine($"📊 Project Status:");
                Console.WriteLine($"   Total Videos: {status.TotalVideos}");
                Console.WriteLine($"   With Transcripts: {status.VideosWithTranscripts}");
                Console.WriteLine($"   Without Transcripts: {status.VideosWithoutTranscripts}");

                if (status.IsComplete)
                {
                    Console.WriteLine($"✅ All videos already have transcripts");
                    return;
                }

                // Process transcripts
                var result = await transcriptHandler.ProcessProjectTranscriptsAsync(projectName);

                if (result.Success)
                {
                    Console.WriteLine($"✅ Transcript processing completed:");
                    Console.WriteLine($"   Successful: {result.SuccessfulCount}");
                    Console.WriteLine($"   Failed: {result.FailedCount}");

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
                    Console.WriteLine($"❌ Transcript processing failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing transcripts: {ex.Message}");
            }
        }

        private static async Task ProcessExistingProjectTranscripts(TranscriptProcessingHandler transcriptHandler, AppDbContext dbContext)
        {
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("PROCESSING EXISTING PROJECT TRANSCRIPTS");
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
                    // Get status for each project
                    var status = await transcriptHandler.GetProjectTranscriptStatusAsync(project.Name);

                    Console.WriteLine($"\n📁 {project.Name}:");
                    Console.WriteLine($"   Total Videos: {status.TotalVideos}");
                    Console.WriteLine($"   With Transcripts: {status.VideosWithTranscripts}");
                    Console.WriteLine($"   Without Transcripts: {status.VideosWithoutTranscripts}");

                    if (status.VideosWithoutTranscripts > 0)
                    {
                        Console.WriteLine($"   🔄 Processing {status.VideosWithoutTranscripts} video(s)...");
                        await ProcessProjectTranscripts(transcriptHandler, project.Name);
                    }
                    else
                    {
                        Console.WriteLine($"   ✅ All videos have transcripts");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing existing projects: {ex.Message}");
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