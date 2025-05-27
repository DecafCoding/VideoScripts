using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VideoScripts.Configuration;
using VideoScripts.Core;
using VideoScripts.Features.ShowClusters;

namespace VideoScripts;

internal class Program
{
    static async Task Main(string[] args)
    {
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
                    await ProcessAllStepsAsync(config, serviceProvider);
                    break;
                case "3":
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
        Console.WriteLine("2. Process All Steps (Import → Transcripts → Topics → Summaries → Clustering)");
        Console.WriteLine("3. Run Specific Process Step");
        Console.WriteLine("Q. Quit");
        Console.WriteLine();

        return ConsoleOutput.GetUserInput("Enter your choice (1, 2, 3, or Q):") ?? "";
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

        var projectName = GetProjectName();
        if (string.IsNullOrEmpty(projectName))
            return;

        var processingOrchestrator = serviceProvider.GetRequiredService<ProcessingOrchestrator>();

        switch (processChoice)
        {
            case "1":
                await RunImportProcessAsync(config, serviceProvider);
                break;
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
    /// Gets project name from user input
    /// </summary>
    private static string GetProjectName()
    {
        Console.WriteLine();
        var projectName = ConsoleOutput.GetUserInput("Enter project name (or press Enter to cancel):");

        if (string.IsNullOrWhiteSpace(projectName))
        {
            Console.WriteLine("Operation cancelled.");
            return string.Empty;
        }

        return projectName.Trim();
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