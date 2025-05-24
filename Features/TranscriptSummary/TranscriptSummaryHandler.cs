using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoScripts.Data;
using VideoScripts.Features.TranscriptSummary.Models;

namespace VideoScripts.Features.TranscriptSummary;

public class TranscriptSummaryHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TranscriptSummaryService _summaryService;
    private readonly ILogger<TranscriptSummaryHandler> _logger;

    public TranscriptSummaryHandler(
        AppDbContext dbContext,
        TranscriptSummaryService summaryService,
        ILogger<TranscriptSummaryHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _summaryService = summaryService ?? throw new ArgumentNullException(nameof(summaryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes summaries for all videos in a project that have transcripts but no summaries
    /// </summary>
    /// <param name="projectName">Name of the project to process</param>
    /// <returns>Processing result with success status and details</returns>
    public async Task<SummaryProcessingResult> ProcessProjectSummariesAsync(string projectName)
    {
        var result = new SummaryProcessingResult
        {
            ProjectName = projectName,
            ProcessedSummaries = new List<ProcessedSummaryInfo>()
        };

        try
        {
            _logger.LogInformation($"Starting summary processing for project: {projectName}");

            // Get project and its videos
            var project = await _dbContext.Projects
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.Name == projectName);

            if (project == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Project '{projectName}' not found";
                return result;
            }

            // Get videos that have transcripts but no summaries
            var videosNeedingSummaries = project.Videos
                .Where(v => !string.IsNullOrWhiteSpace(v.RawTranscript) &&
                           string.IsNullOrWhiteSpace(v.VideoTopic))
                .ToList();

            if (!videosNeedingSummaries.Any())
            {
                _logger.LogInformation($"All videos with transcripts in project '{projectName}' already have summaries");
                result.Success = true;
                result.ErrorMessage = "All videos with transcripts already have summaries";
                return result;
            }

            _logger.LogInformation($"Found {videosNeedingSummaries.Count} videos needing summaries");

            // Process each video individually
            foreach (var video in videosNeedingSummaries)
            {
                var processedInfo = await ProcessSingleVideoSummaryAsync(video);
                result.ProcessedSummaries.Add(processedInfo);
            }

            // Determine overall success
            var successCount = result.ProcessedSummaries.Count(ps => ps.Success);
            result.Success = successCount > 0;
            result.SuccessfulCount = successCount;
            result.FailedCount = result.ProcessedSummaries.Count - successCount;

            _logger.LogInformation($"Summary processing completed for project '{projectName}': {successCount} successful, {result.FailedCount} failed");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing summaries for project {projectName}: {ex.Message}");
            result.Success = false;
            result.ErrorMessage = $"Processing failed: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Processes summary for a single video by video ID
    /// </summary>
    /// <param name="videoId">YouTube video ID</param>
    /// <returns>Processing result for the single video</returns>
    public async Task<ProcessedSummaryInfo> ProcessSingleVideoSummaryByIdAsync(string videoId)
    {
        try
        {
            var video = await _dbContext.Videos
                .FirstOrDefaultAsync(v => v.YTId == videoId);

            if (video == null)
            {
                return new ProcessedSummaryInfo
                {
                    VideoId = videoId,
                    Title = "Unknown",
                    Success = false,
                    Message = "Video not found in database"
                };
            }

            return await ProcessSingleVideoSummaryAsync(video);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing summary for video {videoId}: {ex.Message}");
            return new ProcessedSummaryInfo
            {
                VideoId = videoId,
                Title = "Unknown",
                Success = false,
                Message = $"Processing failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Processes summary for a single video entity
    /// </summary>
    private async Task<ProcessedSummaryInfo> ProcessSingleVideoSummaryAsync(Data.Entities.VideoEntity video)
    {
        var processedInfo = new ProcessedSummaryInfo
        {
            VideoId = video.YTId,
            Title = video.Title
        };

        try
        {
            _logger.LogInformation($"Processing summary for video: {video.Title} ({video.YTId})");

            // Check if video already has summary
            if (!string.IsNullOrWhiteSpace(video.VideoTopic))
            {
                processedInfo.Success = true;
                processedInfo.Message = "Video already has summary";
                return processedInfo;
            }

            // Check if video has transcript
            if (string.IsNullOrWhiteSpace(video.RawTranscript))
            {
                processedInfo.Success = false;
                processedInfo.Message = "Video has no transcript to analyze";
                return processedInfo;
            }

            // Analyze transcript using AI
            var summaryResult = await _summaryService.AnalyzeTranscriptAsync(
                video.RawTranscript,
                video.YTId,
                video.Title);

            if (summaryResult.Success)
            {
                // Save summary to database
                await SaveSummaryToDatabase(video, summaryResult);

                processedInfo.Success = true;
                processedInfo.Message = "Summary generated and saved successfully";
                processedInfo.VideoTopic = summaryResult.VideoTopic;
                processedInfo.SummaryLength = summaryResult.MainSummary.Length;

                _logger.LogInformation($"Successfully processed summary for video: {video.Title}");
            }
            else
            {
                processedInfo.Success = false;
                processedInfo.Message = summaryResult.ErrorMessage ?? "Failed to generate summary";

                _logger.LogWarning($"Failed to process summary for video: {video.Title} - {processedInfo.Message}");
            }

            return processedInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing summary for video {video.YTId}: {ex.Message}");
            processedInfo.Success = false;
            processedInfo.Message = $"Processing failed: {ex.Message}";
            return processedInfo;
        }
    }

    /// <summary>
    /// Saves the AI-generated summary to the database
    /// </summary>
    private async Task SaveSummaryToDatabase(Data.Entities.VideoEntity video, SummaryResult summaryResult)
    {
        try
        {
            video.VideoTopic = summaryResult.VideoTopic;
            video.MainSummary = summaryResult.MainSummary;
            video.StructuredContent = summaryResult.StructuredContent;
            video.LastModifiedAt = DateTime.UtcNow;
            video.LastModifiedBy = "TranscriptSummaryService";

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Saved summary to database for video: {video.Title}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving summary to database: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets summary processing status for a project
    /// </summary>
    /// <param name="projectName">Name of the project</param>
    /// <returns>Status information about summary processing</returns>
    public async Task<SummaryProcessingStatus> GetProjectSummaryStatusAsync(string projectName)
    {
        try
        {
            var project = await _dbContext.Projects
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.Name == projectName);

            if (project == null)
            {
                return new SummaryProcessingStatus
                {
                    ProjectName = projectName,
                    ProjectExists = false
                };
            }

            var totalVideos = project.Videos.Count;
            var videosWithTranscripts = project.Videos.Count(v => !string.IsNullOrWhiteSpace(v.RawTranscript));
            var videosWithSummaries = project.Videos.Count(v => !string.IsNullOrWhiteSpace(v.VideoTopic));
            var videosNeedingSummaries = project.Videos.Count(v =>
                !string.IsNullOrWhiteSpace(v.RawTranscript) && string.IsNullOrWhiteSpace(v.VideoTopic));

            return new SummaryProcessingStatus
            {
                ProjectName = projectName,
                ProjectExists = true,
                TotalVideos = totalVideos,
                VideosWithTranscripts = videosWithTranscripts,
                VideosWithSummaries = videosWithSummaries,
                VideosNeedingSummaries = videosNeedingSummaries,
                IsComplete = videosNeedingSummaries == 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting summary status for project {projectName}: {ex.Message}");
            return new SummaryProcessingStatus
            {
                ProjectName = projectName,
                ProjectExists = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

// Supporting models for the handler
public class SummaryProcessingResult
{
    public bool Success { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }
    public List<ProcessedSummaryInfo> ProcessedSummaries { get; set; } = new List<ProcessedSummaryInfo>();
}

public class ProcessedSummaryInfo
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string VideoTopic { get; set; } = string.Empty;
    public int SummaryLength { get; set; }
}

public class SummaryProcessingStatus
{
    public string ProjectName { get; set; } = string.Empty;
    public bool ProjectExists { get; set; }
    public int TotalVideos { get; set; }
    public int VideosWithTranscripts { get; set; }
    public int VideosWithSummaries { get; set; }
    public int VideosNeedingSummaries { get; set; }
    public bool IsComplete { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}