using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using VideoScripts.Configuration;
using VideoScripts.Core;
using VideoScripts.Features.ShowClusters;
using VideoScripts.Features.AnalyzeClusters;
using VideoScripts.Data;
using System.Text;

namespace VideoScripts;

internal class Program
{
    static async Task Main(string[] args)
    {
        // Enable UTF-8 encoding for console to support emojis
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        // Setup configuration
        var config = ConfigurationSetup.BuildConfiguration();

        // Setup dependency injection
        var services = new ServiceCollection();
        ServiceConfiguration.ConfigureServices(services, config);
        var serviceProvider = services.BuildServiceProvider();

        // Setup and migrate database
        await DatabaseSetup.InitializeDatabaseAsync(config);

        // Display startup information
        ConsoleOutput.DisplayStartupInfo(config);

        // Main menu loop
        await RunMainMenuAsync(config, serviceProvider);

        // Clean up
        await serviceProvider.DisposeAsync();
    }

    /// <summary>
    /// Runs the main menu loop allowing multiple operations
    /// </summary>
    private static async Task RunMainMenuAsync(IConfiguration config, ServiceProvider serviceProvider)
    {
        while (true)
        {
            var choice = GetUserChoice();

            switch (choice)
            {
                case "1":
                    await ShowClustersAsync(serviceProvider);
                    break;
                case "2":
                    await AnalyzeClustersAsync(serviceProvider);
                    break;
                case "3":
                    await ProcessAllStepsAsync(config, serviceProvider);
                    break;
                case "4":
                    await RunSpecificProcessAsync(config, serviceProvider);
                    break;
                case "q":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Gets user's choice for what to do
    /// </summary>
    private static string GetUserChoice()
    {
        Console.Clear();
        ConsoleOutput.DisplaySectionHeader("MAIN MENU");
        Console.WriteLine("What would you like to do?");
        Console.WriteLine();
        Console.WriteLine("1. View Project Clusters");
        Console.WriteLine("2. Analyze Cluster Content (AI Analysis)");
        Console.WriteLine("3. Process All Steps (Import → Transcripts → Topics → Summaries → Clustering)");
        Console.WriteLine("4. Run Specific Process Step");
        Console.WriteLine("Q. Quit");
        Console.WriteLine();

        return ConsoleOutput.GetUserInput("Enter your choice (1, 2, 3, 4, or Q):") ?? "";
    }

    /// <summary>
    /// Shows clusters using the existing service
    /// </summary>
    private static async Task ShowClustersAsync(ServiceProvider serviceProvider)
    {
        var showClustersService = serviceProvider.GetRequiredService<ShowClustersService>();
        await showClustersService.DisplayClusterMenuAsync();
    }

    /// <summary>
    /// Shows cluster analysis using the new analysis service
    /// </summary>
    private static async Task AnalyzeClustersAsync(ServiceProvider serviceProvider)
    {
        var analyzeClustersService = serviceProvider.GetRequiredService<AnalyzeClustersDisplayService>();
        await analyzeClustersService.DisplayAnalysisMenuAsync();
    }

    /// <summary>
    /// Executes all processing steps (existing functionality)
    /// </summary>
    private static async Task ProcessAllStepsAsync(IConfiguration config, ServiceProvider serviceProvider)
    {
        ConsoleOutput.DisplaySectionHeader("PROCESSING ALL STEPS");
        Console.WriteLine("This will run the complete pipeline: Import → Transcripts → Topics → Summaries → Clustering");
        Console.WriteLine();

        var confirm = ConsoleOutput.GetUserInput("Continue? (y/n):");
        if (confirm?.ToLower() != "y")
        {
            Console.WriteLine("Operation cancelled.");
            return;
        }

        await ExecuteNormalProcessingAsync(config, serviceProvider);
    }

    /// <summary>
    /// Runs a specific processing step based on user selection
    /// </summary>
    private static async Task RunSpecificProcessAsync(IConfiguration config, ServiceProvider serviceProvider)
    {
        var processChoice = GetProcessChoice();
        if (string.IsNullOrEmpty(processChoice))
            return;

        // For Google Sheets import, we don't need a project name
        if (processChoice == "1")
        {
            await RunImportProcessAsync(config, serviceProvider);
            return;
        }

        // For other processes, get project selection
        var projectName = await GetProjectNameAsync(serviceProvider);
        if (string.IsNullOrEmpty(projectName))
            return;

        var processingOrchestrator = serviceProvider.GetRequiredService<ProcessingOrchestrator>();

        switch (processChoice)
        {
            case "2":
                await processingOrchestrator.ProcessTranscriptsForProjectAsync(projectName);
                break;
            case "3":
                await processingOrchestrator.ProcessTopicDiscoveryForProjectAsync(projectName);
                break;
            case "4":
                await processingOrchestrator.ProcessSummariesForProjectAsync(projectName);
                break;
            case "5":
                await processingOrchestrator.ProcessClusteringForProjectAsync(projectName);
                break;
        }
    }

    /// <summary>
    /// Gets user's choice for which process to run
    /// </summary>
    private static string GetProcessChoice()
    {
        ConsoleOutput.DisplaySectionHeader("SELECT PROCESS TO RUN");
        Console.WriteLine("Which process would you like to run?");
        Console.WriteLine();
        Console.WriteLine("1. Import Videos from Google Sheets");
        Console.WriteLine("2. Extract Transcripts");
        Console.WriteLine("3. Discover Topics");
        Console.WriteLine("4. Generate AI Summaries");
        Console.WriteLine("5. Cluster Topics");
        Console.WriteLine();

        while (true)
        {
            var input = ConsoleOutput.GetUserInput("Enter your choice (1-5) or press Enter to cancel:");

            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            if (input.Length == 1 && "12345".Contains(input))
                return input;

            Console.WriteLine("Please enter a number between 1 and 5.");
        }
    }

    /// <summary>
    /// Gets project name from user by displaying available projects and allowing selection by number
    /// </summary>
    private static async Task<string> GetProjectNameAsync(ServiceProvider serviceProvider)
    {
        ConsoleOutput.DisplaySectionHeader("SELECT PROJECT");

        try
        {
            // Get database context to retrieve projects
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Get all projects with basic statistics
            var projects = await dbContext.Projects
                .Include(p => p.Videos)
                .ThenInclude(v => v.TranscriptTopics)
                .Include(p => p.TopicClusters)
                .Select(p => new ProjectSelectionInfo
                {
                    Name = p.Name,
                    Topic = p.Topic,
                    VideoCount = p.Videos.Count,
                    TopicCount = p.Videos.SelectMany(v => v.TranscriptTopics).Count(),
                    ClusterCount = p.TopicClusters.Count,
                    CreatedAt = p.CreatedAt
                })
                .OrderBy(p => p.Name)
                .ToListAsync();

            if (!projects.Any())
            {
                ConsoleOutput.DisplayInfo("No projects found in database.");
                ConsoleOutput.DisplayInfo("Please run 'Import Videos from Google Sheets' first to create projects.");
                Console.WriteLine();
                ConsoleOutput.GetUserInput("Press Enter to continue...");
                return string.Empty;
            }

            // Display available projects
            Console.WriteLine($"Found {projects.Count} project(s):");
            Console.WriteLine();

            for (int i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                var statusInfo = GetProjectStatusDisplay(project);

                Console.WriteLine($"{i + 1}. {project.Name}");
                Console.WriteLine($"   Topic: {project.Topic}");
                Console.WriteLine($"   {statusInfo}");
                Console.WriteLine($"   Created: {project.CreatedAt:yyyy-MM-dd}");
                Console.WriteLine();
            }

            // Get user selection
            while (true)
            {
                var input = ConsoleOutput.GetUserInput($"Select a project (1-{projects.Count}) or press Enter to cancel:");

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Operation cancelled.");
                    return string.Empty;
                }

                if (int.TryParse(input, out int selection) && selection >= 1 && selection <= projects.Count)
                {
                    var selectedProject = projects[selection - 1];
                    Console.WriteLine($"Selected: {selectedProject.Name}");
                    return selectedProject.Name;
                }

                Console.WriteLine($"Please enter a number between 1 and {projects.Count}, or press Enter to cancel.");
            }
        }
        catch (Exception ex)
        {
            ConsoleOutput.DisplayError($"Error retrieving projects: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets a display string showing the current processing status of a project
    /// </summary>
    private static string GetProjectStatusDisplay(ProjectSelectionInfo project)
    {
        var parts = new List<string>();

        parts.Add($"Videos: {project.VideoCount}");

        if (project.TopicCount > 0)
            parts.Add($"Topics: {project.TopicCount}");

        if (project.ClusterCount > 0)
            parts.Add($"Clusters: {project.ClusterCount}");

        var status = string.Join(" | ", parts);

        // Add processing status indicator
        if (project.VideoCount > 0 && project.TopicCount > 0 && project.ClusterCount > 0)
            status += " ✅ Fully Processed";
        else if (project.VideoCount > 0 && project.TopicCount > 0)
            status += " 🔄 Needs Clustering";
        else if (project.VideoCount > 0)
            status += " 📝 Needs Processing";
        else
            status += " 🆕 New Project";

        return status;
    }

    /// <summary>
    /// Runs the import process from Google Sheets
    /// </summary>
    private static async Task RunImportProcessAsync(IConfiguration config, ServiceProvider serviceProvider)
    {
        ConsoleOutput.DisplaySectionHeader("IMPORT VIDEOS FROM GOOGLE SHEETS");

        // Initialize Google Sheets service
        var googleService = GoogleSheetsSetup.InitializeGoogleService(config);
        if (googleService == null)
        {
            ConsoleOutput.DisplayError("Failed to initialize Google Sheets service.");
            return;
        }

        var spreadsheetId = googleService.FindSpreadsheetIdByName(config["Google:SpreadsheetName"]);
        if (spreadsheetId == null)
        {
            ConsoleOutput.DisplayError($"Spreadsheet '{config["Google:SpreadsheetName"]}' not found.");
            return;
        }

        var processingOrchestrator = serviceProvider.GetRequiredService<ProcessingOrchestrator>();
        await processingOrchestrator.ProcessGoogleSheetsImportAsync(googleService, spreadsheetId);
    }

    /// <summary>
    /// Executes the normal processing workflow (existing functionality)
    /// </summary>
    private static async Task ExecuteNormalProcessingAsync(IConfiguration config, ServiceProvider serviceProvider)
    {
        // Initialize Google Sheets service
        var googleService = GoogleSheetsSetup.InitializeGoogleService(config);
        if (googleService == null) return;

        var spreadsheetId = googleService.FindSpreadsheetIdByName(config["Google:SpreadsheetName"]);
        if (spreadsheetId == null)
        {
            ConsoleOutput.DisplayError($"Spreadsheet '{config["Google:SpreadsheetName"]}' not found.");
            return;
        }

        // Get processing handlers from DI
        var processingOrchestrator = serviceProvider.GetRequiredService<ProcessingOrchestrator>();

        // Execute main processing workflow
        await processingOrchestrator.ExecuteMainWorkflowAsync(googleService, spreadsheetId);
    }
}

/// <summary>
/// Helper class for project selection information
/// </summary>
public class ProjectSelectionInfo
{
    public string Name { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public int VideoCount { get; set; }
    public int TopicCount { get; set; }
    public int ClusterCount { get; set; }
    public DateTime CreatedAt { get; set; }
}