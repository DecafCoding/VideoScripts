using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoScripts.Data;
using VideoScripts.Data.Entities;
using VideoScripts.Features.CreateScript.Models;

namespace VideoScripts.Features.CreateScript;

/// <summary>
/// Handler for creating scripts from project video transcripts
/// </summary>
public class CreateScriptHandler
{
    private readonly AppDbContext _dbContext;
    private readonly CreateScriptService _createScriptService;
    private readonly ILogger<CreateScriptHandler> _logger;

    public CreateScriptHandler(
        AppDbContext dbContext,
        CreateScriptService createScriptService,
        ILogger<CreateScriptHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _createScriptService = createScriptService ?? throw new ArgumentNullException(nameof(createScriptService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a script from all video transcripts in a project
    /// </summary>
    /// <param name="projectName">Name of the project to create script for</param>
    /// <param name="customTitle">Optional custom title for the script (if null, generates one)</param>
    /// <returns>Script creation result</returns>
    public async Task<ScriptCreationResult> CreateScriptFromProjectAsync(string projectName, string? customTitle = null)
    {
        var result = new ScriptCreationResult
        {
            ProjectName = projectName
        };

        try
        {
            _logger.LogInformation($"Creating script for project: {projectName}");

            // Get project with videos that have transcripts
            var project = await _dbContext.Projects
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.Name == projectName);

            if (project == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Project '{projectName}' not found";
                return result;
            }

            // Get videos with transcripts
            var videosWithTranscripts = project.Videos
                .Where(v => !string.IsNullOrWhiteSpace(v.RawTranscript))
                .ToList();

            if (!videosWithTranscripts.Any())
            {
                result.Success = false;
                result.ErrorMessage = "No videos with transcripts found in this project";
                return result;
            }

            _logger.LogInformation($"Found {videosWithTranscripts.Count} videos with transcripts");

            // Prepare video data for script creation
            var videoData = videosWithTranscripts.Select(v => (v.Title, v.RawTranscript)).ToList();

            // Get next version number for this project
            var nextVersion = await GetNextScriptVersionAsync(project.Id);

            // Create script title
            var scriptTitle = customTitle ?? GenerateScriptTitle(project.Name, project.Topic, nextVersion);

            // Generate script using AI service
            var scriptContent = await _createScriptService.CreateScriptFromTranscriptsAsync(project.Topic, videoData);

            // Calculate script statistics
            var wordCount = CountWords(scriptContent);
            var estimatedMinutes = wordCount / 150.0; // Assuming 150 words per minute speaking rate

            // Save script to database
            var scriptEntity = new ScriptEntity
            {
                ProjectId = project.Id,
                Title = scriptTitle,
                Content = scriptContent,
                Version = nextVersion,
                CreatedBy = "CreateScriptHandler",
                LastModifiedBy = "CreateScriptHandler"
            };

            _dbContext.Scripts.Add(scriptEntity);
            await _dbContext.SaveChangesAsync();

            // Build success result
            result.Success = true;
            result.ScriptTitle = scriptTitle;
            result.Content = scriptContent;
            result.Version = nextVersion;
            result.TotalWordCount = wordCount;
            result.EstimatedMinutes = estimatedMinutes;
            result.VideoTitles = videosWithTranscripts.Select(v => v.Title).ToList();
            result.TranscriptCount = videosWithTranscripts.Count;

            _logger.LogInformation($"Successfully created script '{scriptTitle}' with {wordCount} words");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating script for project {projectName}: {ex.Message}");
            result.Success = false;
            result.ErrorMessage = $"Script creation failed: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Gets all scripts for a project with basic information
    /// </summary>
    /// <param name="projectName">Name of the project</param>
    /// <returns>List of scripts in the project</returns>
    public async Task<List<ScriptSummaryInfo>> GetProjectScriptsAsync(string projectName)
    {
        try
        {
            var scripts = await _dbContext.Scripts
                .Include(s => s.Project)
                .Where(s => s.Project.Name == projectName)
                .OrderByDescending(s => s.Version)
                .Select(s => new ScriptSummaryInfo
                {
                    Id = s.Id,
                    Title = s.Title,
                    Version = s.Version,
                    WordCount = CountWords(s.Content),
                    CreatedAt = s.CreatedAt,
                    CreatedBy = s.CreatedBy
                })
                .ToListAsync();

            return scripts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting scripts for project {projectName}: {ex.Message}");
            return new List<ScriptSummaryInfo>();
        }
    }

    /// <summary>
    /// Gets the full script content by script ID
    /// </summary>
    /// <param name="scriptId">ID of the script</param>
    /// <returns>Full script content or null if not found</returns>
    public async Task<ScriptEntity?> GetScriptByIdAsync(Guid scriptId)
    {
        try
        {
            return await _dbContext.Scripts
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == scriptId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting script {scriptId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks if a project exists and has videos with transcripts
    /// </summary>
    /// <param name="projectName">Name of the project</param>
    /// <returns>Project readiness status</returns>
    public async Task<ProjectScriptReadinessStatus> GetProjectReadinessAsync(string projectName)
    {
        try
        {
            var project = await _dbContext.Projects
                .Include(p => p.Videos)
                .Include(p => p.Scripts)
                .FirstOrDefaultAsync(p => p.Name == projectName);

            if (project == null)
            {
                return new ProjectScriptReadinessStatus
                {
                    ProjectName = projectName,
                    ProjectExists = false
                };
            }

            var totalVideos = project.Videos.Count;
            var videosWithTranscripts = project.Videos.Count(v => !string.IsNullOrWhiteSpace(v.RawTranscript));
            var existingScripts = project.Scripts.Count;

            return new ProjectScriptReadinessStatus
            {
                ProjectName = projectName,
                ProjectExists = true,
                ProjectTopic = project.Topic,
                TotalVideos = totalVideos,
                VideosWithTranscripts = videosWithTranscripts,
                ExistingScripts = existingScripts,
                IsReady = videosWithTranscripts > 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting project readiness for {projectName}: {ex.Message}");
            return new ProjectScriptReadinessStatus
            {
                ProjectName = projectName,
                ProjectExists = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets the next version number for scripts in a project
    /// </summary>
    private async Task<int> GetNextScriptVersionAsync(Guid projectId)
    {
        var maxVersion = await _dbContext.Scripts
            .Where(s => s.ProjectId == projectId)
            .MaxAsync(s => (int?)s.Version) ?? 0;

        return maxVersion + 1;
    }

    /// <summary>
    /// Generates a script title based on project information
    /// </summary>
    private string GenerateScriptTitle(string projectName, string projectTopic, int version)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd");
        return $"{projectName} - {projectTopic} Script v{version} ({timestamp})";
    }

    /// <summary>
    /// Counts words in a text string
    /// </summary>
    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }
}

/// <summary>
/// Summary information about a script
/// </summary>
public class ScriptSummaryInfo
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Version { get; set; }
    public int WordCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Status information about a project's readiness for script creation
/// </summary>
public class ProjectScriptReadinessStatus
{
    public string ProjectName { get; set; } = string.Empty;
    public bool ProjectExists { get; set; }
    public string ProjectTopic { get; set; } = string.Empty;
    public int TotalVideos { get; set; }
    public int VideosWithTranscripts { get; set; }
    public int ExistingScripts { get; set; }
    public bool IsReady { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}