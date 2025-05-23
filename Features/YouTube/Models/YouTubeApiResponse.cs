using Newtonsoft.Json;

namespace VideoScripts.Features.YouTube.Models;

// Internal API response models for YouTube Data API v3
internal class YouTubeApiVideoResponse
{
    [JsonProperty("items")]
    public List<YouTubeApiVideoItem> Items { get; set; } = new List<YouTubeApiVideoItem>();
}

internal class YouTubeApiVideoItem
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("snippet")]
    public YouTubeApiVideoSnippet Snippet { get; set; } = new YouTubeApiVideoSnippet();

    [JsonProperty("statistics")]
    public YouTubeApiVideoStatistics Statistics { get; set; } = new YouTubeApiVideoStatistics();

    [JsonProperty("contentDetails")]
    public YouTubeApiVideoContentDetails ContentDetails { get; set; } = new YouTubeApiVideoContentDetails();
}

internal class YouTubeApiVideoSnippet
{
    [JsonProperty("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonProperty("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("thumbnails")]
    public YouTubeApiThumbnails Thumbnails { get; set; } = new YouTubeApiThumbnails();

    [JsonProperty("channelTitle")]
    public string ChannelTitle { get; set; } = string.Empty;

    [JsonProperty("tags")]
    public List<string> Tags { get; set; } = new List<string>();
}

internal class YouTubeApiVideoStatistics
{
    [JsonProperty("viewCount")]
    public string ViewCount { get; set; } = "0";

    [JsonProperty("likeCount")]
    public string LikeCount { get; set; } = "0";

    [JsonProperty("commentCount")]
    public string CommentCount { get; set; } = "0";
}

internal class YouTubeApiVideoContentDetails
{
    [JsonProperty("duration")]
    public string Duration { get; set; } = string.Empty;
}

internal class YouTubeApiChannelResponse
{
    [JsonProperty("items")]
    public List<YouTubeApiChannelItem> Items { get; set; } = new List<YouTubeApiChannelItem>();
}

internal class YouTubeApiChannelItem
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("snippet")]
    public YouTubeApiChannelSnippet Snippet { get; set; } = new YouTubeApiChannelSnippet();

    [JsonProperty("statistics")]
    public YouTubeApiChannelStatistics Statistics { get; set; } = new YouTubeApiChannelStatistics();
}

internal class YouTubeApiChannelSnippet
{
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("customUrl")]
    public string CustomUrl { get; set; } = string.Empty;

    [JsonProperty("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonProperty("thumbnails")]
    public YouTubeApiThumbnails Thumbnails { get; set; } = new YouTubeApiThumbnails();

    [JsonProperty("country")]
    public string Country { get; set; } = string.Empty;
}

internal class YouTubeApiChannelStatistics
{
    [JsonProperty("viewCount")]
    public string ViewCount { get; set; } = "0";

    [JsonProperty("subscriberCount")]
    public string SubscriberCount { get; set; } = "0";

    [JsonProperty("videoCount")]
    public string VideoCount { get; set; } = "0";
}

internal class YouTubeApiThumbnails
{
    [JsonProperty("high")]
    public YouTubeApiThumbnail High { get; set; } = new YouTubeApiThumbnail();

    [JsonProperty("medium")]
    public YouTubeApiThumbnail Medium { get; set; } = new YouTubeApiThumbnail();

    [JsonProperty("default")]
    public YouTubeApiThumbnail Default { get; set; } = new YouTubeApiThumbnail();
}

internal class YouTubeApiThumbnail
{
    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;
}