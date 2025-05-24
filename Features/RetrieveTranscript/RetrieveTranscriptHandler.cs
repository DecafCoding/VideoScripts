using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoScripts.Data;
using VideoScripts.Data.Entities;

namespace VideoScripts.Features.RetrieveTranscript;

public class RetrieveTranscriptHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TranscriptService _transcriptService;
    private readonly ILogger<RetrieveTranscriptHandler> _logger;

    public RetrieveTranscriptHandler(
        AppDbContext dbContext,
        TranscriptService transcriptService,
        ILogger<RetrieveTranscriptHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _transcriptService = transcriptService ?? throw new ArgumentNullException(nameof(transcriptService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes all videos in the database that don't have transcripts
    /// </summary>
    /// <returns>Summary of transcript retrieval results</returns>
    public async Task<TranscriptProcessingResult> ProcessMissingTranscriptsAsync()
    {
        var result = new TranscriptProcessingResult();

        try
        {
            _logger.LogInformation("Starting transcript retrieval process for videos with missing transcripts");

            // Get videos without transcripts
            var videosWithoutTranscripts = await GetVideosWithoutTranscriptsAsync();

            if (!videosWithoutTranscripts.Any())
            {
                _logger.LogInformation("No videos found without transcripts");
                result.Success = true;
                result.Message = "No videos found without transcripts";
                return result;
            }

            _logger.LogInformation($"Found {videosWithoutTranscripts.Count} videos without transcripts");
            result.TotalVideosProcessed = videosWithoutTranscripts.Count;

            // Process each video
            foreach (var video in videosWithoutTranscripts)
            {
                var videoResult = await ProcessSingleVideoTranscriptAsync(video);
                result.VideoResults.Add(videoResult);

                if (videoResult.Success)
                {
                    result.SuccessfulTranscripts++;
                }
                else
                {
                    result.FailedTranscripts++;
                }

                // Add small delay between requests to be respectful to the API
                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            // Save all changes to database
            await _dbContext.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Completed processing {result.TotalVideosProcessed} videos. " +
                           $"Successful: {result.SuccessfulTranscripts}, Failed: {result.FailedTranscripts}";

            _logger.LogInformation(result.Message);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during transcript retrieval process: {ex.Message}");
            result.Success = false;
            result.Message = $"Process failed: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Gets all videos from database that don't have transcripts
    /// </summary>
    private async Task<List<VideoEntity>> GetVideosWithoutTranscriptsAsync()
    {
        return await _dbContext.Videos
            .Include(v => v.Channel) // Include channel for logging purposes
            .Where(v => string.IsNullOrEmpty(v.RawTranscript))
            .OrderBy(v => v.CreatedAt) // Process oldest videos first
            .ToListAsync();
    }

    /// <summary>
    /// Processes transcript retrieval for a single video
    /// </summary>
    private async Task<VideoTranscriptResult> ProcessSingleVideoTranscriptAsync(VideoEntity video)
    {
        var videoResult = new VideoTranscriptResult
        {
            VideoId = video.YTId,
            VideoTitle = video.Title,
            Success = false
        };

        try
        {
            _logger.LogInformation($"Processing transcript for video: {video.Title} (ID: {video.YTId})");

            // Construct YouTube URL from video ID
            var youtubeUrl = $"https://www.youtube.com/watch?v={video.YTId}";

            // Call TranscriptService to get transcript
            var transcriptResult = await _transcriptService.ScrapeVideoAsync(youtubeUrl);

            if (transcriptResult == null)
            {
                videoResult.ErrorMessage = "Failed to retrieve transcript from Apify service";
                _logger.LogWarning($"Failed to retrieve transcript for video {video.YTId}: No result from Apify");
                return videoResult;
            }

            if (string.IsNullOrWhiteSpace(transcriptResult.Subtitles))
            {
                videoResult.ErrorMessage = "No subtitles found for this video";
                _logger.LogWarning($"No subtitles found for video {video.YTId}");
                return videoResult;
            }

            // Update video entity with transcript
            video.RawTranscript = transcriptResult.Subtitles;
            video.LastModifiedAt = DateTime.UtcNow;
            video.LastModifiedBy = "RetrieveTranscriptHandler";

            videoResult.Success = true;
            videoResult.TranscriptLength = transcriptResult.Subtitles.Length;
            videoResult.Message = "Transcript retrieved and saved successfully";

            _logger.LogInformation($"Successfully retrieved transcript for video {video.YTId} - Length: {videoResult.TranscriptLength} characters");
            return videoResult;
        }
        catch (Exception ex)
        {
            videoResult.ErrorMessage = $"Error processing video: {ex.Message}";
            _logger.LogError(ex, $"Error processing transcript for video {video.YTId}: {ex.Message}");
            return videoResult;
        }
    }
}

// Supporting models for the handler
public class TranscriptProcessingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalVideosProcessed { get; set; }
    public int SuccessfulTranscripts { get; set; }
    public int FailedTranscripts { get; set; }
    public List<VideoTranscriptResult> VideoResults { get; set; } = new List<VideoTranscriptResult>();
}

public class VideoTranscriptResult
{
    public string VideoId { get; set; } = string.Empty;
    public string VideoTitle { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int TranscriptLength { get; set; }
}