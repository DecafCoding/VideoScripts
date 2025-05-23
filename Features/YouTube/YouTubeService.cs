using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

using VideoScripts.Features.YouTube.Models;
using VideoScripts.Extensions;

namespace VideoScripts.Features.YouTube;

public class YouTubeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YouTubeService> _logger;
    private readonly string _apiKey;
    private const string BaseUrl = "https://www.googleapis.com/youtube/v3";

    public YouTubeService(IConfiguration configuration, ILogger<YouTubeService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["YouTube:ApiKey"]
            ?? throw new InvalidOperationException("YouTube:ApiKey configuration is missing");

        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Gets video information from YouTube API using video ID
    /// </summary>
    /// <param name="videoId">YouTube video ID (11 characters)</param>
    /// <returns>Video information or null if not found</returns>
    public async Task<YouTubeVideoInfo?> GetVideoInfoAsync(string videoId)
    {
        try
        {
            // Clean the video ID in case a full URL was passed
            var cleanVideoId = ExtractVideoId(videoId);

            if (string.IsNullOrEmpty(cleanVideoId))
            {
                _logger.LogWarning($"Invalid video ID provided: {videoId}");
                return null;
            }

            _logger.LogInformation($"Fetching video info for ID: {cleanVideoId}");

            var url = $"{BaseUrl}/videos" +
                     $"?part=snippet,statistics,contentDetails" +
                     $"&id={cleanVideoId}" +
                     $"&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"YouTube API request failed: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<YouTubeApiVideoResponse>(jsonContent);

            if (apiResponse?.Items == null || !apiResponse.Items.Any())
            {
                _logger.LogWarning($"No video found with ID: {cleanVideoId}");
                return null;
            }

            var videoItem = apiResponse.Items.First();
            var videoInfo = MapToVideoInfo(videoItem);

            _logger.LogInformation($"Successfully retrieved video info: {videoInfo.Title}");
            return videoInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching video info for ID {videoId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets channel information from YouTube API using channel ID
    /// </summary>
    /// <param name="channelId">YouTube channel ID</param>
    /// <returns>Channel information or null if not found</returns>
    public async Task<YouTubeChannelInfo?> GetChannelInfoAsync(string channelId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                _logger.LogWarning("Empty channel ID provided");
                return null;
            }

            _logger.LogInformation($"Fetching channel info for ID: {channelId}");

            var url = $"{BaseUrl}/channels" +
                     $"?part=snippet,statistics" +
                     $"&id={channelId}" +
                     $"&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"YouTube API request failed: {response.StatusCode} - {response.ReasonPhrase}");
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<YouTubeApiChannelResponse>(jsonContent);

            if (apiResponse?.Items == null || !apiResponse.Items.Any())
            {
                _logger.LogWarning($"No channel found with ID: {channelId}");
                return null;
            }

            var channelItem = apiResponse.Items.First();
            var channelInfo = MapToChannelInfo(channelItem);

            _logger.LogInformation($"Successfully retrieved channel info: {channelInfo.Title}");
            return channelInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching channel info for ID {channelId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets multiple videos information in a single API call (more efficient for batch operations)
    /// </summary>
    /// <param name="videoIds">List of YouTube video IDs (max 50 per request)</param>
    /// <returns>List of video information objects</returns>
    public async Task<List<YouTubeVideoInfo>> GetMultipleVideosInfoAsync(List<string> videoIds)
    {
        var results = new List<YouTubeVideoInfo>();

        if (videoIds == null || !videoIds.Any())
        {
            _logger.LogWarning("Empty video IDs list provided");
            return results;
        }

        try
        {
            // Clean all video IDs
            var cleanVideoIds = videoIds.Select(ExtractVideoId)
                                       .Where(id => !string.IsNullOrEmpty(id))
                                       .ToList();

            if (!cleanVideoIds.Any())
            {
                _logger.LogWarning("No valid video IDs found after cleaning");
                return results;
            }

            // YouTube API allows max 50 IDs per request
            const int batchSize = 50;
            var batches = cleanVideoIds.Batch(batchSize);

            foreach (var batch in batches)
            {
                var batchResults = await GetVideoBatchAsync(batch.ToList());
                results.AddRange(batchResults);
            }

            _logger.LogInformation($"Successfully retrieved info for {results.Count} videos");
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching multiple videos info: {ex.Message}");
            return results;
        }
    }

    /// <summary>
    /// Extracts video ID from various YouTube URL formats or returns the ID if already clean
    /// </summary>
    /// <param name="input">YouTube URL or video ID</param>
    /// <returns>Clean 11-character video ID or empty string if invalid</returns>
    private string ExtractVideoId(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // If it's already a clean video ID (11 characters, alphanumeric + underscore/hyphen)
        if (Regex.IsMatch(input, @"^[a-zA-Z0-9_-]{11}$"))
            return input;

        // Extract from various YouTube URL formats
        var patterns = new[]
        {
            @"(?:youtube\.com/watch\?v=|youtu\.be/|youtube\.com/embed/)([a-zA-Z0-9_-]{11})",
            @"youtube\.com/v/([a-zA-Z0-9_-]{11})",
            @"youtube\.com/watch\?.*v=([a-zA-Z0-9_-]{11})"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(input, pattern);
            if (match.Success)
                return match.Groups[1].Value;
        }

        _logger.LogWarning($"Could not extract video ID from: {input}");
        return string.Empty;
    }

    /// <summary>
    /// Gets a batch of videos from YouTube API
    /// </summary>
    private async Task<List<YouTubeVideoInfo>> GetVideoBatchAsync(List<string> videoIds)
    {
        var results = new List<YouTubeVideoInfo>();

        try
        {
            var idsString = string.Join(",", videoIds);
            var url = $"{BaseUrl}/videos" +
                     $"?part=snippet,statistics,contentDetails" +
                     $"&id={idsString}" +
                     $"&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"YouTube API batch request failed: {response.StatusCode}");
                return results;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<YouTubeApiVideoResponse>(jsonContent);

            if (apiResponse?.Items != null)
            {
                results.AddRange(apiResponse.Items.Select(MapToVideoInfo));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in batch video request: {ex.Message}");
            return results;
        }
    }

    /// <summary>
    /// Maps YouTube API video response to our domain model
    /// </summary>
    private YouTubeVideoInfo MapToVideoInfo(YouTubeApiVideoItem item)
    {
        return new YouTubeVideoInfo
        {
            VideoId = item.Id,
            Title = item.Snippet.Title,
            Description = item.Snippet.Description,
            ChannelId = item.Snippet.ChannelId,
            ChannelTitle = item.Snippet.ChannelTitle,
            PublishedAt = item.Snippet.PublishedAt,
            Duration = item.ContentDetails.Duration,
            ViewCount = long.TryParse(item.Statistics.ViewCount, out var views) ? views : 0,
            LikeCount = long.TryParse(item.Statistics.LikeCount, out var likes) ? likes : 0,
            CommentCount = long.TryParse(item.Statistics.CommentCount, out var comments) ? comments : 0,
            ThumbnailUrl = item.Snippet.Thumbnails.High?.Url ??
                          item.Snippet.Thumbnails.Medium?.Url ??
                          item.Snippet.Thumbnails.Default?.Url ?? string.Empty,
            Tags = item.Snippet.Tags ?? new List<string>()
        };
    }

    /// <summary>
    /// Maps YouTube API channel response to our domain model
    /// </summary>
    private YouTubeChannelInfo MapToChannelInfo(YouTubeApiChannelItem item)
    {
        return new YouTubeChannelInfo
        {
            ChannelId = item.Id,
            Title = item.Snippet.Title,
            Description = item.Snippet.Description,
            CustomUrl = item.Snippet.CustomUrl,
            PublishedAt = item.Snippet.PublishedAt,
            ThumbnailUrl = item.Snippet.Thumbnails.High?.Url ??
                          item.Snippet.Thumbnails.Medium?.Url ??
                          item.Snippet.Thumbnails.Default?.Url ?? string.Empty,
            SubscriberCount = long.TryParse(item.Statistics.SubscriberCount, out var subs) ? subs : 0,
            VideoCount = long.TryParse(item.Statistics.VideoCount, out var videos) ? videos : 0,
            ViewCount = long.TryParse(item.Statistics.ViewCount, out var views) ? views : 0,
            Country = item.Snippet.Country
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}