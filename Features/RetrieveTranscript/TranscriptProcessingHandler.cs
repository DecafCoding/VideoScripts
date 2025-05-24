using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoScripts.Data;
using VideoScripts.Features.RetrieveTranscript.Models;

namespace VideoScripts.Features.RetrieveTranscript;

public class TranscriptProcessingHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TranscriptService _transcriptService;
    private readonly ILogger<TranscriptProcessingHandler> _logger;

    public TranscriptProcessingHandler(
        AppDbContext dbContext,
        TranscriptService transcriptService,
        ILogger<TranscriptProcessingHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _transcriptService = transcriptService ?? throw new ArgumentNullException(nameof(transcriptService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes transcripts for all videos in a project that don't have transcripts yet
    /// </summary>
    /// <param name="projectName">Name of the project to process</param>
    /// <returns>Processing result with success status and details</returns>
    public async Task<TranscriptProcessingResult> ProcessProjectTranscriptsAsync(string projectName)
    {
        var result = new TranscriptProcessingResult
        {
            ProjectName = projectName,
            ProcessedTranscripts = new List<ProcessedTranscriptInfo>()
        };

        try
        {
            _logger.LogInformation($"Starting transcript processing for project: {projectName}");

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

            // Get videos that don't have transcripts
            var videosNeedingTranscripts = project.Videos
                .Where(v => string.IsNullOrWhiteSpace(v.RawTranscript))
                .ToList();

            if (!videosNeedingTranscripts.Any())
            {
                _logger.LogInformation($"All videos in project '{projectName}' already have transcripts");
                result.Success = true;
                result.ErrorMessage = "All videos already have transcripts";
                return result;
            }

            _logger.LogInformation($"Found {videosNeedingTranscripts.Count} videos needing transcripts");

            // Process each video individually
            foreach (var video in videosNeedingTranscripts)
            {
                var processedInfo = await ProcessSingleVideoTranscriptAsync(video);
                result.ProcessedTranscripts.Add(processedInfo);
            }

            // Determine overall success
            var successCount = result.ProcessedTranscripts.Count(pt => pt.Success);
            result.Success = successCount > 0;
            result.SuccessfulCount = successCount;
            result.FailedCount = result.ProcessedTranscripts.Count - successCount;

            _logger.LogInformation($"Transcript processing completed for project '{projectName}': {successCount} successful, {result.FailedCount} failed");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing transcripts for project {projectName}: {ex.Message}");
            result.Success = false;
            result.ErrorMessage = $"Processing failed: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Processes transcript for a single video by video ID
    /// </summary>
    /// <param name="videoId">YouTube video ID</param>
    /// <returns>Processing result for the single video</returns>
    public async Task<ProcessedTranscriptInfo> ProcessSingleVideoTranscriptByIdAsync(string videoId)
    {
        try
        {
            var video = await _dbContext.Videos
                .FirstOrDefaultAsync(v => v.YTId == videoId);

            if (video == null)
            {
                return new ProcessedTranscriptInfo
                {
                    VideoId = videoId,
                    Title = "Unknown",
                    Success = false,
                    Message = "Video not found in database"
                };
            }

            return await ProcessSingleVideoTranscriptAsync(video);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing transcript for video {videoId}: {ex.Message}");
            return new ProcessedTranscriptInfo
            {
                VideoId = videoId,
                Title = "Unknown",
                Success = false,
                Message = $"Processing failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Processes transcript for a single video entity
    /// </summary>
    private async Task<ProcessedTranscriptInfo> ProcessSingleVideoTranscriptAsync(Data.Entities.VideoEntity video)
    {
        var processedInfo = new ProcessedTranscriptInfo
        {
            VideoId = video.YTId,
            Title = video.Title
        };

        try
        {
            _logger.LogInformation($"Processing transcript for video: {video.Title} ({video.YTId})");

            // Check if video already has transcript
            if (!string.IsNullOrWhiteSpace(video.RawTranscript))
            {
                processedInfo.Success = true;
                processedInfo.Message = "Video already has transcript";
                return processedInfo;
            }

            // Build YouTube URL for the video
            var videoUrl = $"https://www.youtube.com/watch?v={video.YTId}";

            // Scrape and save transcript
            var transcriptResult = await _transcriptService.ScrapeAndSaveVideoAsync(videoUrl);

            if (transcriptResult.Success)
            {
                processedInfo.Success = true;
                processedInfo.Message = "Transcript retrieved and saved successfully";
                processedInfo.TranscriptLength = transcriptResult.Subtitles?.Length ?? 0;

                _logger.LogInformation($"Successfully processed transcript for video: {video.Title}");
            }
            else
            {
                processedInfo.Success = false;
                processedInfo.Message = transcriptResult.ErrorMessage ?? "Failed to retrieve transcript";

                _logger.LogWarning($"Failed to process transcript for video: {video.Title} - {processedInfo.Message}");
            }

            return processedInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing transcript for video {video.YTId}: {ex.Message}");
            processedInfo.Success = false;
            processedInfo.Message = $"Processing failed: {ex.Message}";
            return processedInfo;
        }
    }

    /// <summary>
    /// Gets transcript processing status for a project
    /// </summary>
    /// <param name="projectName">Name of the project</param>
    /// <returns>Status information about transcript processing</returns>
    public async Task<TranscriptProcessingStatus> GetProjectTranscriptStatusAsync(string projectName)
    {
        try
        {
            var project = await _dbContext.Projects
                .Include(p => p.Videos)
                .FirstOrDefaultAsync(p => p.Name == projectName);

            if (project == null)
            {
                return new TranscriptProcessingStatus
                {
                    ProjectName = projectName,
                    ProjectExists = false
                };
            }

            var totalVideos = project.Videos.Count;
            var videosWithTranscripts = project.Videos.Count(v => !string.IsNullOrWhiteSpace(v.RawTranscript));
            var videosWithoutTranscripts = totalVideos - videosWithTranscripts;

            return new TranscriptProcessingStatus
            {
                ProjectName = projectName,
                ProjectExists = true,
                TotalVideos = totalVideos,
                VideosWithTranscripts = videosWithTranscripts,
                VideosWithoutTranscripts = videosWithoutTranscripts,
                IsComplete = videosWithoutTranscripts == 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting transcript status for project {projectName}: {ex.Message}");
            return new TranscriptProcessingStatus
            {
                ProjectName = projectName,
                ProjectExists = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

// Supporting models for the handler
public class TranscriptProcessingResult
{
    public bool Success { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }
    public List<ProcessedTranscriptInfo> ProcessedTranscripts { get; set; } = new List<ProcessedTranscriptInfo>();
}

public class ProcessedTranscriptInfo
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TranscriptLength { get; set; }
}

public class TranscriptProcessingStatus
{
    public string ProjectName { get; set; } = string.Empty;
    public bool ProjectExists { get; set; }
    public int TotalVideos { get; set; }
    public int VideosWithTranscripts { get; set; }
    public int VideosWithoutTranscripts { get; set; }
    public bool IsComplete { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}