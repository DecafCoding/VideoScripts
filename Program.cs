using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VideoScripts.Data;
using VideoScripts.Configuration;
using VideoScripts.Core;

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

        // Clean up
        await serviceProvider.DisposeAsync();
    }
}