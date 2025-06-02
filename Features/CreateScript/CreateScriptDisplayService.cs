using Microsoft.EntityFrameworkCore;
using VideoScripts.Core;
using VideoScripts.Data;

namespace VideoScripts.Features.CreateScript;

/// <summary>
/// Service for displaying CreateScript feature interactions and menus
/// </summary>
public class CreateScriptDisplayService
{
    private readonly CreateScriptHandler _createScriptHandler;
    private readonly AppDbContext _dbContext;

    public CreateScriptDisplayService(
        CreateScriptHandler createScriptHandler,
        AppDbContext dbContext)
    {
        _createScriptHandler = createScriptHandler ?? throw new ArgumentNullException(nameof(createScriptHandler));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Displays the main CreateScript menu and handles user interactions
    /// </summary>
    public async Task DisplayCreateScriptMenuAsync()
    {
        while (true)
        {
            var choice = GetCreateScriptChoice();

            switch (choice)
            {
                case "1":
                    await CreateNewScriptAsync();
                    break;
                case "2":
                    await ViewExistingScriptsAsync();
                    break;
                case "3":
                    await ViewScriptContentAsync();
                    break;
                case "q":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }

            if (choice != "q")
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }

    /// <summary>
    /// Gets user's choice for CreateScript actions
    /// </summary>
    private string GetCreateScriptChoice()
    {
        Console.Clear();
        ConsoleOutput.DisplaySectionHeader("CREATE SCRIPT");
        Console.WriteLine("What would you like to do?");
        Console.WriteLine();
        Console.WriteLine("1. Create New Script from Project");
        Console.WriteLine("2. View Existing Scripts");
        Console.WriteLine("3. View Script Content");
        Console.WriteLine("Q. Back to Main Menu");
        Console.WriteLine();

        return ConsoleOutput.GetUserInput("Enter your choice (1, 2, 3, or Q):") ?? "";
    }

    /// <summary>
    /// Creates a new script from a selected project
    /// </summary>
    private async Task CreateNewScriptAsync()
    {
        ConsoleOutput.DisplaySectionHeader("CREATE NEW SCRIPT");

        try
        {
            // Get all projects that have videos with transcripts
            var readyProjects = await GetProjectsReadyForScriptsAsync();

            if (!readyProjects.Any())
            {
                ConsoleOutput.DisplayInfo("No projects found with videos that have transcripts.");
                ConsoleOutput.DisplayInfo("Please ensure you have:");
                ConsoleOutput.DisplayInfo("1. Imported videos from Google Sheets");
                ConsoleOutput.DisplayInfo("2. Extracted transcripts for those videos");
                return;
            }

            // Display available projects
            Console.WriteLine($"Found {readyProjects.Count} project(s) ready for script creation:");
            Console.WriteLine();

            for (int i = 0; i < readyProjects.Count; i++)
            {
                var project = readyProjects[i];
                Console.WriteLine($"{i + 1}. {project.ProjectName}");
                Console.WriteLine($"   Topic: {project.ProjectTopic}");
                Console.WriteLine($"   Videos with Transcripts: {project.VideosWithTranscripts}/{project.TotalVideos}");
                Console.WriteLine($"   Existing Scripts: {project.ExistingScripts}");
                Console.WriteLine();
            }

            // Get user selection
            var projectName = await GetProjectSelectionAsync(readyProjects);
            if (string.IsNullOrEmpty(projectName))
                return;

            // Optional custom title
            var customTitle = ConsoleOutput.GetUserInput("Enter custom script title (or press Enter for auto-generated):");
            if (string.IsNullOrWhiteSpace(customTitle))
                customTitle = null;

            // Confirm creation
            Console.WriteLine();
            Console.WriteLine($"Creating script for project: {projectName}");
            if (!string.IsNullOrEmpty(customTitle))
                Console.WriteLine($"With title: {customTitle}");

            var confirm = ConsoleOutput.GetUserInput("Continue? (y/n):");
            if (confirm?.ToLower() != "y")
            {
                Console.WriteLine("Script creation cancelled.");
                return;
            }

            // Create the script
            Console.WriteLine();
            Console.WriteLine("Creating script... This may take a moment.");

            var result = await _createScriptHandler.CreateScriptFromProjectAsync(projectName, customTitle);

            // Display results
            if (result.Success)
            {
                Console.WriteLine();
                ConsoleOutput.DisplayInfo("✅ Script created successfully!");
                Console.WriteLine();
                Console.WriteLine($"Script Title: {result.ScriptTitle}");
                Console.WriteLine($"Version: {result.Version}");
                Console.WriteLine($"Word Count: {result.TotalWordCount:N0}");
                Console.WriteLine($"Estimated Speaking Time: {result.EstimatedMinutes:F1} minutes");
                Console.WriteLine($"Source Videos: {result.TranscriptCount}");
                Console.WriteLine();
                Console.WriteLine("Source Video Titles:");
                foreach (var title in result.VideoTitles)
                {
                    Console.WriteLine($"  • {title}");
                }
            }
            else
            {
                Console.WriteLine();
                ConsoleOutput.DisplayError($"❌ Script creation failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            ConsoleOutput.DisplayError($"Error creating script: {ex.Message}");
        }
    }

    /// <summary>
    /// Views existing scripts for a selected project
    /// </summary>
    private async Task ViewExistingScriptsAsync()
    {
        ConsoleOutput.DisplaySectionHeader("VIEW EXISTING SCRIPTS");

        try
        {
            // Get project selection
            var projectName = await GetProjectWithScriptsSelectionAsync();
            if (string.IsNullOrEmpty(projectName))
                return;

            // Get scripts for the project
            var scripts = await _createScriptHandler.GetProjectScriptsAsync(projectName);

            if (!scripts.Any())
            {
                ConsoleOutput.DisplayInfo($"No scripts found for project: {projectName}");
                return;
            }

            Console.WriteLine($"Scripts for project: {projectName}");
            Console.WriteLine();

            foreach (var script in scripts)
            {
                Console.WriteLine($"📄 {script.Title}");
                Console.WriteLine($"   Version: {script.Version}");
                Console.WriteLine($"   Word Count: {script.WordCount:N0}");
                Console.WriteLine($"   Created: {script.CreatedAt:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"   Created By: {script.CreatedBy}");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            ConsoleOutput.DisplayError($"Error viewing scripts: {ex.Message}");
        }
    }

    /// <summary>
    /// Views the full content of a selected script
    /// </summary>
    private async Task ViewScriptContentAsync()
    {
        ConsoleOutput.DisplaySectionHeader("VIEW SCRIPT CONTENT");

        try
        {
            // Get project selection
            var projectName = await GetProjectWithScriptsSelectionAsync();
            if (string.IsNullOrEmpty(projectName))
                return;

            // Get scripts for the project
            var scripts = await _createScriptHandler.GetProjectScriptsAsync(projectName);

            if (!scripts.Any())
            {
                ConsoleOutput.DisplayInfo($"No scripts found for project: {projectName}");
                return;
            }

            // Display script selection
            Console.WriteLine($"Select a script to view from project: {projectName}");
            Console.WriteLine();

            for (int i = 0; i < scripts.Count; i++)
            {
                var script = scripts[i];
                Console.WriteLine($"{i + 1}. {script.Title} (v{script.Version}) - {script.WordCount:N0} words");
            }

            Console.WriteLine();

            // Get script selection
            var scriptChoice = ConsoleOutput.GetUserInput($"Select script (1-{scripts.Count}) or press Enter to cancel:");
            if (string.IsNullOrWhiteSpace(scriptChoice) ||
                !int.TryParse(scriptChoice, out int selection) ||
                selection < 1 || selection > scripts.Count)
            {
                Console.WriteLine("Script viewing cancelled.");
                return;
            }

            var selectedScript = scripts[selection - 1];

            // Get full script content
            var scriptEntity = await _createScriptHandler.GetScriptByIdAsync(selectedScript.Id);
            if (scriptEntity == null)
            {
                ConsoleOutput.DisplayError("Script not found.");
                return;
            }

            // Display script content
            Console.Clear();
            ConsoleOutput.DisplaySectionHeader($"SCRIPT: {scriptEntity.Title}");
            Console.WriteLine($"Project: {scriptEntity.Project.Name}");
            Console.WriteLine($"Version: {scriptEntity.Version}");
            Console.WriteLine($"Created: {scriptEntity.CreatedAt:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"Word Count: {CreateScriptHandler.CountWords(scriptEntity.Content):N0}");
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();
            Console.WriteLine(scriptEntity.Content);
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
        }
        catch (Exception ex)
        {
            ConsoleOutput.DisplayError($"Error viewing script content: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets projects that are ready for script creation (have videos with transcripts)
    /// </summary>
    private async Task<List<ProjectScriptReadinessStatus>> GetProjectsReadyForScriptsAsync()
    {
        var projects = await _dbContext.Projects
            .Include(p => p.Videos)
            .Include(p => p.Scripts)
            .ToListAsync();

        var readyProjects = new List<ProjectScriptReadinessStatus>();

        foreach (var project in projects)
        {
            var videosWithTranscripts = project.Videos.Count(v => !string.IsNullOrWhiteSpace(v.RawTranscript));

            if (videosWithTranscripts > 0)
            {
                readyProjects.Add(new ProjectScriptReadinessStatus
                {
                    ProjectName = project.Name,
                    ProjectExists = true,
                    ProjectTopic = project.Topic,
                    TotalVideos = project.Videos.Count,
                    VideosWithTranscripts = videosWithTranscripts,
                    ExistingScripts = project.Scripts.Count,
                    IsReady = true
                });
            }
        }

        return readyProjects.OrderBy(p => p.ProjectName).ToList();
    }

    /// <summary>
    /// Gets user selection from available projects
    /// </summary>
    private async Task<string> GetProjectSelectionAsync(List<ProjectScriptReadinessStatus> projects)
    {
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
                Console.WriteLine($"Selected: {selectedProject.ProjectName}");
                return selectedProject.ProjectName;
            }

            Console.WriteLine($"Please enter a number between 1 and {projects.Count}, or press Enter to cancel.");
        }
    }

    /// <summary>
    /// Gets project selection from projects that have existing scripts
    /// </summary>
    private async Task<string> GetProjectWithScriptsSelectionAsync()
    {
        var projectsWithScripts = await _dbContext.Projects
            .Include(p => p.Scripts)
            .Where(p => p.Scripts.Any())
            .Select(p => new { p.Name, ScriptCount = p.Scripts.Count })
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (!projectsWithScripts.Any())
        {
            ConsoleOutput.DisplayInfo("No projects with scripts found.");
            ConsoleOutput.DisplayInfo("Create a script first using option 1.");
            return string.Empty;
        }

        Console.WriteLine("Select a project:");
        Console.WriteLine();

        for (int i = 0; i < projectsWithScripts.Count; i++)
        {
            var project = projectsWithScripts[i];
            Console.WriteLine($"{i + 1}. {project.Name} ({project.ScriptCount} scripts)");
        }

        Console.WriteLine();

        while (true)
        {
            var input = ConsoleOutput.GetUserInput($"Select project (1-{projectsWithScripts.Count}) or press Enter to cancel:");

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Operation cancelled.");
                return string.Empty;
            }

            if (int.TryParse(input, out int selection) && selection >= 1 && selection <= projectsWithScripts.Count)
            {
                var selectedProject = projectsWithScripts[selection - 1];
                Console.WriteLine($"Selected: {selectedProject.Name}");
                return selectedProject.Name;
            }

            Console.WriteLine($"Please enter a number between 1 and {projectsWithScripts.Count}, or press Enter to cancel.");
        }
    }
}