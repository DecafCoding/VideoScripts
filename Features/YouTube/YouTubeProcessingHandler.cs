// Features/YouTube/YouTubeProcessingHandler.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoScripts.Data;
using VideoScripts.Data.Entities;
using VideoScripts.Features.YouTube.Models;

namespace VideoScripts.Features.YouTube;

public class YouTubeProcessingHandler
{
    private readonly AppDbContext _dbContext;
    private readonly YouTubeService _youTubeService;
    private readonly ILogger<YouTubeProcessingHandler> _logger;

    public YouTubeProcessingHandler(
        AppDbContext dbContext,
        YouTubeService youTubeService,
        ILogger<YouTubeProcessingHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _youTubeService = youTubeService ?? throw new ArgumentNullException(nameof(youTubeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes a Google Sheets row containing project name and video URLs
    /// </summary>
    /// <param name="projectName">Name of the project from Google Sheets</param>
    /// <param name="videoUrls">List of YouTube video URLs from the sheet columns</param>
    /// <returns>Processing result with success status and details</returns>
    public async Task<YouTubeProcessingResult> ProcessSheetRowAsync(string projectName, List<string> videoUrls)
    {
        var result = new YouTubeProcessingResult
        {
            ProjectName = projectName,
            ProcessedVideos = new List<ProcessedVideoInfo>()
        };

        try
        {
            _logger.LogInformation($"Processing project: {projectName} with {videoUrls.Count} video URLs");

            // Filter out empty URLs
            var validVideoUrls = videoUrls.Where(url => !string.IsNullOrWhiteSpace(url)).ToList();

            if (!validVideoUrls.Any())
            {
                result.Success = false;
                result.ErrorMessage = "No valid video URLs provided";
                return result;
            }

            // Create or get existing project
            var project = await GetOrCreateProjectAsync(projectName);

            // Extract video IDs from URLs
            var videoIds = validVideoUrls.Select(ExtractVideoIdFromUrl).Where(id => !string.IsNullOrEmpty(id)).ToList();

            if (!videoIds.Any())
            {
                result.Success = false;
                result.ErrorMessage = "No valid YouTube video IDs could be extracted from the provided URLs";
                return result;
            }

            // Get video information from YouTube API
            var videoInfos = await _youTubeService.GetMultipleVideosInfoAsync(videoIds);

            if (!videoInfos.Any())
            {
                result.Success = false;
                result.ErrorMessage = "Failed to retrieve video information from YouTube API";
                return result;
            }

            // Process each video
            foreach (var videoInfo in videoInfos)
            {
                var processedVideo = await ProcessSingleVideoAsync(videoInfo, project);
                result.ProcessedVideos.Add(processedVideo);
            }

            // Save all changes
            await _dbContext.SaveChangesAsync();

            result.Success = true;
            result.Project = project;

            _logger.LogInformation($"Successfully processed {result.ProcessedVideos.Count} videos for project: {projectName}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing sheet row for project {projectName}: {ex.Message}");
            result.Success = false;
            result.ErrorMessage = $"Processing failed: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Processes a single video and creates/updates database entities
    /// </summary>
    private async Task<ProcessedVideoInfo> ProcessSingleVideoAsync(YouTubeVideoInfo videoInfo, ProjectEntity project)
    {
        var processedVideo = new ProcessedVideoInfo
        {
            VideoId = videoInfo.VideoId,
            Title = videoInfo.Title,
            Success = false
        };

        try
        {
            // Check if video already exists
            var existingVideo = await _dbContext.Videos
                .Include(v => v.Channel)
                .FirstOrDefaultAsync(v => v.YTId == videoInfo.VideoId);

            if (existingVideo != null)
            {
                _logger.LogInformation($"Video already exists in database: {videoInfo.Title}");

                // Update project association if needed
                if (existingVideo.ProjectId != project.Id)
                {
                    existingVideo.ProjectId = project.Id;
                    existingVideo.LastModifiedAt = DateTime.UtcNow;
                    existingVideo.LastModifiedBy = "YouTubeProcessingHandler";
                }

                processedVideo.Success = true;
                processedVideo.Message = "Video already exists - updated project association";
                processedVideo.VideoEntity = existingVideo;
                return processedVideo;
            }

            // Get or create channel
            var channel = await GetOrCreateChannelAsync(videoInfo);

            // Create new video entity
            var videoEntity = await CreateVideoEntityAsync(videoInfo, channel, project);

            processedVideo.Success = true;
            processedVideo.Message = "Video created successfully";
            processedVideo.VideoEntity = videoEntity;
            processedVideo.ChannelEntity = channel;

            _logger.LogInformation($"Successfully processed video: {videoInfo.Title}");
            return processedVideo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing video {videoInfo.VideoId}: {ex.Message}");
            processedVideo.Success = false;
            processedVideo.Message = $"Failed to process video: {ex.Message}";
            return processedVideo;
        }
    }

    /// <summary>
    /// Gets existing project or creates new one
    /// </summary>
    private async Task<ProjectEntity> GetOrCreateProjectAsync(string projectName)
    {
        var existingProject = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Name == projectName);

        if (existingProject != null)
        {
            _logger.LogInformation($"Using existing project: {projectName}");
            return existingProject;
        }

        var newProject = new ProjectEntity
        {
            Name = projectName,
            Topic = $"Project for {projectName}", // You might want to make this configurable
            CreatedBy = "YouTubeProcessingHandler",
            LastModifiedBy = "YouTubeProcessingHandler"
        };

        _dbContext.Projects.Add(newProject);
        _logger.LogInformation($"Created new project: {projectName}");

        return newProject;
    }

    /// <summary>
    /// Gets existing channel or creates new one using YouTube API
    /// </summary>
    private async Task<ChannelEntity> GetOrCreateChannelAsync(YouTubeVideoInfo videoInfo)
    {
        var existingChannel = await _dbContext.Channels
            .FirstOrDefaultAsync(c => c.YTId == videoInfo.ChannelId);

        if (existingChannel != null)
        {
            // Update last check date
            existingChannel.LastCheckDate = DateTime.UtcNow;
            existingChannel.LastModifiedAt = DateTime.UtcNow;
            existingChannel.LastModifiedBy = "YouTubeProcessingHandler";

            _logger.LogInformation($"Using existing channel: {videoInfo.ChannelTitle}");
            return existingChannel;
        }

        // Get detailed channel information from YouTube API
        var channelInfo = await _youTubeService.GetChannelInfoAsync(videoInfo.ChannelId);

        var newChannel = new ChannelEntity
        {
            YTId = videoInfo.ChannelId,
            Title = channelInfo?.Title ?? videoInfo.ChannelTitle,
            Description = channelInfo?.Description ?? string.Empty,
            ThumbnailURL = channelInfo?.ThumbnailUrl ?? string.Empty,
            VideoCount = (int)(channelInfo?.VideoCount ?? 0),
            SubscriberCount = (int)(channelInfo?.SubscriberCount ?? 0),
            PublishedAt = channelInfo?.PublishedAt ?? DateTime.UtcNow,
            LastCheckDate = DateTime.UtcNow,
            CreatedBy = "YouTubeProcessingHandler",
            LastModifiedBy = "YouTubeProcessingHandler"
        };

        _dbContext.Channels.Add(newChannel);
        _logger.LogInformation($"Created new channel: {newChannel.Title}");

        return newChannel;
    }

    /// <summary>
    /// Creates a new video entity from YouTube video information
    /// </summary>
    private async Task<VideoEntity> CreateVideoEntityAsync(YouTubeVideoInfo videoInfo, ChannelEntity channel, ProjectEntity project)
    {
        var videoEntity = new VideoEntity
        {
            YTId = videoInfo.VideoId,
            Title = TruncateString(videoInfo.Title, 200),
            Description = TruncateString(videoInfo.Description, 5000),
            ChannelId = channel.Id,
            ProjectId = project.Id,
            ViewCount = (int)Math.Min(videoInfo.ViewCount, int.MaxValue),
            LikeCount = (int)Math.Min(videoInfo.LikeCount, int.MaxValue),
            CommentCount = (int)Math.Min(videoInfo.CommentCount, int.MaxValue),
            Duration = ParseYouTubeDuration(videoInfo.Duration),
            PublishedAt = videoInfo.PublishedAt,
            CreatedBy = "YouTubeProcessingHandler",
            LastModifiedBy = "YouTubeProcessingHandler"
            // RawTranscript, VideoTopic, MainSummary, StructuredContent will be populated later by other services
        };

        _dbContext.Videos.Add(videoEntity);
        _logger.LogInformation($"Created new video entity: {videoEntity.Title}");

        return videoEntity;
    }

    /// <summary>
    /// Extracts YouTube video ID from various URL formats
    /// </summary>
    private string ExtractVideoIdFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        // Use the same logic as YouTubeService
        var patterns = new[]
        {
            @"(?:youtube\.com/watch\?v=|youtu\.be/|youtube\.com/embed/)([a-zA-Z0-9_-]{11})",
            @"youtube\.com/v/([a-zA-Z0-9_-]{11})",
            @"youtube\.com/watch\?.*v=([a-zA-Z0-9_-]{11})"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, pattern);
            if (match.Success)
                return match.Groups[1].Value;
        }

        return string.Empty;
    }

    /// <summary>
    /// Parses YouTube duration format (PT4M13S) to seconds
    /// </summary>
    private int ParseYouTubeDuration(string duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
            return 0;

        try
        {
            // YouTube uses ISO 8601 duration format (PT4M13S)
            var timeSpan = System.Xml.XmlConvert.ToTimeSpan(duration);
            return (int)timeSpan.TotalSeconds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to parse duration '{duration}': {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Truncates string to specified length while preserving word boundaries
    /// </summary>
    private string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input ?? string.Empty;

        // Find the last space before the max length to avoid cutting words
        var truncated = input.Substring(0, maxLength);
        var lastSpace = truncated.LastIndexOf(' ');

        if (lastSpace > 0 && lastSpace > maxLength * 0.8) // Only use word boundary if it's not too far back
            return truncated.Substring(0, lastSpace) + "...";

        return truncated + "...";
    }
}

// Supporting models for the handler
public class YouTubeProcessingResult
{
    public bool Success { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public ProjectEntity? Project { get; set; }
    public List<ProcessedVideoInfo> ProcessedVideos { get; set; } = new List<ProcessedVideoInfo>();
}

public class ProcessedVideoInfo
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public VideoEntity? VideoEntity { get; set; }
    public ChannelEntity? ChannelEntity { get; set; }
}