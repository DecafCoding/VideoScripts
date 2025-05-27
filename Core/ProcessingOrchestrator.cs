using Microsoft.EntityFrameworkCore;
using VideoScripts.Data;
using VideoScripts.Features.YouTube;
using VideoScripts.Features.RetrieveTranscript;
using VideoScripts.Features.TranscriptSummary;
using VideoScripts.Features.TopicDiscovery;
using VideoScripts.Features.ClusterTopics;

namespace VideoScripts.Core;

/// <summary>
/// Orchestrates the main processing workflow for YouTube videos, transcripts, topic discovery, and summaries
/// </summary>
public class ProcessingOrchestrator
{
    private readonly AppDbContext _dbContext;
    private readonly YouTubeProcessingHandler _youTubeHandler;
    private readonly TranscriptProcessingHandler _transcriptHandler;
    private readonly TopicDiscoveryHandler _topicDiscoveryHandler;
    private readonly TranscriptSummaryHandler _summaryHandler;
    private readonly ClusterTopicsHandler _clusterTopicsHandler;

    public ProcessingOrchestrator(
        AppDbContext dbContext,
        YouTubeProcessingHandler youTubeHandler,
        TranscriptProcessingHandler transcriptHandler,
        TopicDiscoveryHandler topicDiscoveryHandler,
        TranscriptSummaryHandler summaryHandler,
        ClusterTopicsHandler clusterTopicsHandler)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _youTubeHandler = youTubeHandler ?? throw new ArgumentNullException(nameof(youTubeHandler));
        _transcriptHandler = transcriptHandler ?? throw new ArgumentNullException(nameof(transcriptHandler));
        _topicDiscoveryHandler = topicDiscoveryHandler ?? throw new ArgumentNullException(nameof(topicDiscoveryHandler));
        _summaryHandler = summaryHandler ?? throw new ArgumentNullException(nameof(summaryHandler));
        _clusterTopicsHandler = clusterTopicsHandler ?? throw new ArgumentNullException(nameof(clusterTopicsHandler));
    }

    /// <summary>
    /// Executes the main processing workflow (original method - runs all steps)
    /// </summary>
    public async Task ExecuteMainWorkflowAsync(GoogleDriveService googleService, string spreadsheetId)
    {
        // Get all unimported rows from Google Sheets
        var unimportedRows = googleService.GetUnimportedRows(spreadsheetId);

        if (!unimportedRows.Any())
        {
            ConsoleOutput.DisplayInfo("No unimported rows found in the spreadsheet.");

            // Check if user wants to process existing projects
            var response = ConsoleOutput.GetUserInput("\nWould you like to process existing projects? (y/n)");
            if (response?.ToLower() == "y")
            {
                await ProcessExistingProjectsAsync();
            }
            return;
        }

        // Process new rows from Google Sheets
        await ProcessNewRowsAsync(googleService, spreadsheetId, unimportedRows);

        ConsoleOutput.DisplayInfo("\nAll processing completed!");
    }

    #region Individual Processing Methods

    /// <summary>
    /// Processes only Google Sheets import step
    /// </summary>
    public async Task ProcessGoogleSheetsImportAsync(GoogleDriveService googleService, string spreadsheetId)
    {
        ConsoleOutput.DisplaySectionHeader("GOOGLE SHEETS IMPORT");

        var unimportedRows = googleService.GetUnimportedRows(spreadsheetId);

        if (!unimportedRows.Any())
        {
            ConsoleOutput.DisplayInfo("No unimported rows found in the spreadsheet.");
            return;
        }

        ConsoleOutput.DisplayInfo($"Found {unimportedRows.Count} unimported row(s):");

        var processedRowNumbers = new List<int>();

        // Process YouTube videos import only
        foreach (var (row, rowNumber, headers) in unimportedRows)
        {
            Console.WriteLine($"\nProcessing Row {rowNumber}:");
            Console.WriteLine(new string('-', 40));

            var projectName = SheetDataExtractor.GetCellValue(row, headers, "Project Name");
            var videoUrls = SheetDataExtractor.GetVideoUrls(row, headers);

            if (string.IsNullOrWhiteSpace(projectName))
            {
                Console.WriteLine($"Skipping row {rowNumber}: No project name found");
                continue;
            }

            Console.WriteLine($"Project: {projectName}");
            Console.WriteLine($"Video URLs: {string.Join(", ", videoUrls.Where(url => !string.IsNullOrWhiteSpace(url)))}");

            try
            {
                var result = await _youTubeHandler.ProcessSheetRowAsync(projectName, videoUrls);

                if (result.Success)
                {
                    Console.WriteLine($"Successfully processed {result.ProcessedVideos.Count} videos");

                    foreach (var video in result.ProcessedVideos)
                    {
                        var status = video.Success ? "✅" : "❌";
                        Console.WriteLine($"   {status} {video.Title} - {video.Message}");
                    }

                    processedRowNumbers.Add(rowNumber);
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
        }

        // Mark processed rows as imported
        await MarkRowsAsImported(googleService, spreadsheetId, processedRowNumbers, unimportedRows);
        ConsoleOutput.DisplayInfo("Import processing completed!");
    }

    /// <summary>
    /// Processes only transcript extraction for a specific project
    /// </summary>
    public async Task ProcessTranscriptsForProjectAsync(string projectName)
    {
        ConsoleOutput.DisplaySectionHeader($"TRANSCRIPT PROCESSING: {projectName}");

        if (!await ProjectExistsAsync(projectName))
        {
            ConsoleOutput.DisplayError($"Project '{projectName}' not found.");
            return;
        }

        await ProjectProcessor.ProcessProjectTranscriptsAsync(_transcriptHandler, projectName);
    }

    /// <summary>
    /// Processes only topic discovery for a specific project
    /// </summary>
    public async Task ProcessTopicDiscoveryForProjectAsync(string projectName)
    {
        ConsoleOutput.DisplaySectionHeader($"TOPIC DISCOVERY PROCESSING: {projectName}");

        if (!await ProjectExistsAsync(projectName))
        {
            ConsoleOutput.DisplayError($"Project '{projectName}' not found.");
            return;
        }

        await ProjectProcessor.ProcessProjectTopicDiscoveryAsync(_topicDiscoveryHandler, projectName);
    }

    /// <summary>
    /// Processes only AI summaries for a specific project
    /// </summary>
    public async Task ProcessSummariesForProjectAsync(string projectName)
    {
        ConsoleOutput.DisplaySectionHeader($"AI SUMMARY PROCESSING: {projectName}");

        if (!await ProjectExistsAsync(projectName))
        {
            ConsoleOutput.DisplayError($"Project '{projectName}' not found.");
            return;
        }

        await ProjectProcessor.ProcessProjectSummariesAsync(_summaryHandler, projectName);
    }

    /// <summary>
    /// Processes only topic clustering for a specific project
    /// </summary>
    public async Task ProcessClusteringForProjectAsync(string projectName)
    {
        ConsoleOutput.DisplaySectionHeader($"TOPIC CLUSTERING PROCESSING: {projectName}");

        if (!await ProjectExistsAsync(projectName))
        {
            ConsoleOutput.DisplayError($"Project '{projectName}' not found.");
            return;
        }

        await ProjectProcessor.ProcessProjectClusteringAsync(_clusterTopicsHandler, projectName);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Checks if a project exists in the database
    /// </summary>
    private async Task<bool> ProjectExistsAsync(string projectName)
    {
        return await _dbContext.Projects.AnyAsync(p => p.Name == projectName);
    }

    /// <summary>
    /// Processes new rows from Google Sheets (original method)
    /// </summary>
    private async Task ProcessNewRowsAsync(
        GoogleDriveService googleService,
        string spreadsheetId,
        List<(IList<object> row, int rowNumber, IList<string> headers)> unimportedRows)
    {
        ConsoleOutput.DisplayInfo($"Found {unimportedRows.Count} unimported row(s):");
        ConsoleOutput.DisplaySectionHeader("PROCESSING NEW GOOGLE SHEETS ROWS");

        var processedRowNumbers = new List<int>();
        var projectsToProcess = new List<string>();

        // Process YouTube videos first
        foreach (var (row, rowNumber, headers) in unimportedRows)
        {
            Console.WriteLine($"\nProcessing Row {rowNumber}:");
            Console.WriteLine(new string('-', 40));

            var projectName = SheetDataExtractor.GetCellValue(row, headers, "Project Name");
            var videoUrls = SheetDataExtractor.GetVideoUrls(row, headers);

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
                var result = await _youTubeHandler.ProcessSheetRowAsync(projectName, videoUrls);

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
        await MarkRowsAsImported(googleService, spreadsheetId, processedRowNumbers, unimportedRows);

        ConsoleOutput.DisplayInfo("\nYouTube processing completed!");

        // Process transcripts, topic discovery, and summaries for new projects
        if (projectsToProcess.Any())
        {
            foreach (var projectName in projectsToProcess)
            {
                await ProjectProcessor.ProcessFullProjectPipelineAsync(
                            _transcriptHandler, _topicDiscoveryHandler, _clusterTopicsHandler, _summaryHandler, projectName);
            }
        }
    }

    /// <summary>
    /// Marks processed rows as imported in Google Sheets
    /// </summary>
    private async Task MarkRowsAsImported(
        GoogleDriveService googleService,
        string spreadsheetId,
        List<int> processedRowNumbers,
        List<(IList<object> row, int rowNumber, IList<string> headers)> unimportedRows)
    {
        if (processedRowNumbers.Any())
        {
            Console.WriteLine($"Marking {processedRowNumbers.Count} row(s) as imported...");

            var updatedCount = await googleService.MarkRowsAsImportedAsync(
                spreadsheetId,
                processedRowNumbers,
                unimportedRows.First().headers);

            Console.WriteLine($"Successfully marked {updatedCount} cells as imported.");
        }
    }

    /// <summary>
    /// Processes existing projects for transcripts, topic discovery, and summaries
    /// </summary>
    private async Task ProcessExistingProjectsAsync()
    {
        ConsoleOutput.DisplaySectionHeader("PROCESSING EXISTING PROJECTS");

        try
        {
            // Get all projects
            var projects = await _dbContext.Projects.ToListAsync();

            if (!projects.Any())
            {
                ConsoleOutput.DisplayInfo("No projects found in database.");
                return;
            }

            ConsoleOutput.DisplayInfo($"Found {projects.Count} project(s):");

            foreach (var project in projects)
            {
                Console.WriteLine($"\nProcessing: {project.Name}");
                await ProjectProcessor.ProcessFullProjectPipelineAsync(
                    _transcriptHandler, _topicDiscoveryHandler, _clusterTopicsHandler, _summaryHandler, project.Name);
            }
        }
        catch (Exception ex)
        {
            ConsoleOutput.DisplayError($"Error processing existing projects: {ex.Message}");
        }
    }

    #endregion
}