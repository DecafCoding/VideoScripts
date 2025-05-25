using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoScripts.Data;
using VideoScripts.Data.Entities;
using VideoScripts.Features.TopicDiscovery.Models;

namespace VideoScripts.Features.TopicDiscovery;

public class TopicDiscoveryHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TopicDiscoveryService _topicDiscoveryService;
    private readonly ILogger<TopicDiscoveryHandler> _logger;

    public TopicDiscoveryHandler(
        AppDbContext dbContext,
        TopicDiscoveryService topicDiscoveryService,
        ILogger<TopicDiscoveryHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _topicDiscoveryService = topicDiscoveryService ?? throw new ArgumentNullException(nameof(topicDiscoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes topic discovery for all videos in a project that have transcripts but no topics yet
    /// </summary>
    /// <param name="projectName">Name of the project to process</param>
    /// <returns>Processing result with success status and details</returns>
    public async Task<TopicDiscoveryProcessingResult> ProcessProjectTopicsAsync(string projectName)
    {
        var result = new TopicDiscoveryProcessingResult
        {
            ProjectName = projectName,
            ProcessedTopics = new List<ProcessedTopicInfo>()
        };

        try
        {
            _logger.LogInformation($"Starting topic discovery processing for project: {projectName}");

            // Get project and its videos
            var project = await _dbContext.Projects
                .Include(p => p.Videos)
                .ThenInclude(v => v.TranscriptTopics)
                .FirstOrDefaultAsync(p => p.Name == projectName);

            if (project == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Project '{projectName}' not found";
                return result;
            }

            // Get videos that have transcripts but no topics yet
            var videosNeedingTopics = project.Videos
                .Where(v => !string.IsNullOrWhiteSpace(v.RawTranscript) &&
                           !v.TranscriptTopics.Any())
                .ToList();

            if (!videosNeedingTopics.Any())
            {
                _logger.LogInformation($"All videos with transcripts in project '{projectName}' already have topics");
                result.Success = true;
                result.ErrorMessage = "All videos with transcripts already have topics";
                return result;
            }

            _logger.LogInformation($"Found {videosNeedingTopics.Count} videos needing topic discovery");

            // Process each video individually
            foreach (var video in videosNeedingTopics)
            {
                var processedInfo = await ProcessSingleVideoTopicsAsync(video);
                result.ProcessedTopics.Add(processedInfo);
            }

            // Determine overall success
            var successCount = result.ProcessedTopics.Count(pt => pt.Success);
            result.Success = successCount > 0;
            result.SuccessfulCount = successCount;
            result.FailedCount = result.ProcessedTopics.Count - successCount;

            _logger.LogInformation($"Topic discovery processing completed for project '{projectName}': {successCount} successful, {result.FailedCount} failed");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing topic discovery for project {projectName}: {ex.Message}");
            result.Success = false;
            result.ErrorMessage = $"Processing failed: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Processes topic discovery for a single video by video ID
    /// </summary>
    /// <param name="videoId">YouTube video ID</param>
    /// <returns>Processing result for the single video</returns>
    public async Task<ProcessedTopicInfo> ProcessSingleVideoTopicsByIdAsync(string videoId)
    {
        try
        {
            var video = await _dbContext.Videos
                .Include(v => v.TranscriptTopics)
                .FirstOrDefaultAsync(v => v.YTId == videoId);

            if (video == null)
            {
                return new ProcessedTopicInfo
                {
                    VideoId = videoId,
                    Title = "Unknown",
                    Success = false,
                    Message = "Video not found in database"
                };
            }

            return await ProcessSingleVideoTopicsAsync(video);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing topic discovery for video {videoId}: {ex.Message}");
            return new ProcessedTopicInfo
            {
                VideoId = videoId,
                Title = "Unknown",
                Success = false,
                Message = $"Processing failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Processes topic discovery for a single video entity
    /// </summary>
    private async Task<ProcessedTopicInfo> ProcessSingleVideoTopicsAsync(VideoEntity video)
    {
        var processedInfo = new ProcessedTopicInfo
        {
            VideoId = video.YTId,
            Title = video.Title
        };

        try
        {
            _logger.LogInformation($"Processing topic discovery for video: {video.Title} ({video.YTId})");

            // Check if video already has topics
            if (video.TranscriptTopics.Any())
            {
                processedInfo.Success = true;
                processedInfo.Message = "Video already has topics";
                processedInfo.TopicCount = video.TranscriptTopics.Count;
                return processedInfo;
            }

            // Check if video has transcript
            if (string.IsNullOrWhiteSpace(video.RawTranscript))
            {
                processedInfo.Success = false;
                processedInfo.Message = "Video has no transcript to analyze";
                return processedInfo;
            }

            // Analyze transcript using AI for topic discovery
            var topicResult = await _topicDiscoveryService.AnalyzeTranscriptAsync(
                video.RawTranscript,
                video.YTId,
                video.Title);

            if (topicResult.Success && topicResult.Topics.Any())
            {
                // Save topics to database
                await SaveTopicsToDatabase(video, topicResult.Topics);

                processedInfo.Success = true;
                processedInfo.Message = "Topics discovered and saved successfully";
                processedInfo.TopicCount = topicResult.Topics.Count;

                _logger.LogInformation($"Successfully processed topic discovery for video: {video.Title} - Found {topicResult.Topics.Count} topics");
            }
            else
            {
                processedInfo.Success = false;
                processedInfo.Message = topicResult.ErrorMessage ?? "Failed to discover topics";

                _logger.LogWarning($"Failed to process topic discovery for video: {video.Title} - {processedInfo.Message}");
            }

            return processedInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing topic discovery for video {video.YTId}: {ex.Message}");
            processedInfo.Success = false;
            processedInfo.Message = $"Processing failed: {ex.Message}";
            return processedInfo;
        }
    }

    /// <summary>
    /// Saves discovered topics to the database as TranscriptTopicEntity records
    /// </summary>
    private async Task SaveTopicsToDatabase(VideoEntity video, List<DiscoveredTopic> topics)
    {
        try
        {
            var transcriptTopics = new List<TranscriptTopicEntity>();

            foreach (var topic in topics)
            {
                var transcriptTopic = new TranscriptTopicEntity
                {
                    VideoId = video.Id,
                    StartTime = topic.StartTime,
                    Title = topic.Title,
                    TopicSummary = topic.TopicSummary,
                    Content = topic.Content,
                    BluePrintElements = topic.BlueprintElements,
                    IsSelected = false, // Default to false - can be updated later by user selection
                    CreatedBy = "TopicDiscoveryService",
                    LastModifiedBy = "TopicDiscoveryService"
                };

                transcriptTopics.Add(transcriptTopic);
            }

            _dbContext.TranscriptTopics.AddRange(transcriptTopics);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Saved {transcriptTopics.Count} topics to database for video: {video.Title}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving topics to database: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets topic discovery processing status for a project
    /// </summary>
    /// <param name="projectName">Name of the project</param>
    /// <returns>Status information about topic discovery processing</returns>
    public async Task<TopicDiscoveryProcessingStatus> GetProjectTopicStatusAsync(string projectName)
    {
        try
        {
            var project = await _dbContext.Projects
                .Include(p => p.Videos)
                .ThenInclude(v => v.TranscriptTopics)
                .FirstOrDefaultAsync(p => p.Name == projectName);

            if (project == null)
            {
                return new TopicDiscoveryProcessingStatus
                {
                    ProjectName = projectName,
                    ProjectExists = false
                };
            }

            var totalVideos = project.Videos.Count;
            var videosWithTranscripts = project.Videos.Count(v => !string.IsNullOrWhiteSpace(v.RawTranscript));
            var videosWithTopics = project.Videos.Count(v => v.TranscriptTopics.Any());
            var videosNeedingTopics = project.Videos.Count(v =>
                !string.IsNullOrWhiteSpace(v.RawTranscript) && !v.TranscriptTopics.Any());
            var totalTopicsCount = project.Videos.SelectMany(v => v.TranscriptTopics).Count();

            return new TopicDiscoveryProcessingStatus
            {
                ProjectName = projectName,
                ProjectExists = true,
                TotalVideos = totalVideos,
                VideosWithTranscripts = videosWithTranscripts,
                VideosWithTopics = videosWithTopics,
                VideosNeedingTopics = videosNeedingTopics,
                TotalTopicsCount = totalTopicsCount,
                IsComplete = videosNeedingTopics == 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting topic discovery status for project {projectName}: {ex.Message}");
            return new TopicDiscoveryProcessingStatus
            {
                ProjectName = projectName,
                ProjectExists = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets all topics for a specific video
    /// </summary>
    /// <param name="videoId">YouTube video ID</param>
    /// <returns>List of topics for the video</returns>
    public async Task<List<TranscriptTopicEntity>> GetVideoTopicsAsync(string videoId)
    {
        try
        {
            var video = await _dbContext.Videos
                .Include(v => v.TranscriptTopics)
                .FirstOrDefaultAsync(v => v.YTId == videoId);

            return video?.TranscriptTopics.OrderBy(t => t.StartTime).ToList() ?? new List<TranscriptTopicEntity>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting topics for video {videoId}: {ex.Message}");
            return new List<TranscriptTopicEntity>();
        }
    }

    /// <summary>
    /// Toggles the selection status of a specific topic
    /// </summary>
    /// <param name="topicId">Topic entity ID</param>
    /// <param name="isSelected">New selection status</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> ToggleTopicSelectionAsync(Guid topicId, bool isSelected)
    {
        try
        {
            var topic = await _dbContext.TranscriptTopics.FindAsync(topicId);
            if (topic == null)
            {
                _logger.LogWarning($"Topic with ID {topicId} not found");
                return false;
            }

            topic.IsSelected = isSelected;
            topic.LastModifiedAt = DateTime.UtcNow;
            topic.LastModifiedBy = "TopicDiscoveryHandler";

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error toggling topic selection for {topicId}: {ex.Message}");
            return false;
        }
    }
}

// Supporting models for the handler
public class TopicDiscoveryProcessingResult
{
    public bool Success { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }
    public List<ProcessedTopicInfo> ProcessedTopics { get; set; } = new List<ProcessedTopicInfo>();
}

public class ProcessedTopicInfo
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TopicCount { get; set; }
}

public class TopicDiscoveryProcessingStatus
{
    public string ProjectName { get; set; } = string.Empty;
    public bool ProjectExists { get; set; }
    public int TotalVideos { get; set; }
    public int VideosWithTranscripts { get; set; }
    public int VideosWithTopics { get; set; }
    public int VideosNeedingTopics { get; set; }
    public int TotalTopicsCount { get; set; }
    public bool IsComplete { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}