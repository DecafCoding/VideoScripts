using Newtonsoft.Json;

namespace VideoScripts.Features.YouTube.Models;

public class YouTubeVideoInfo
{
    [JsonProperty("videoId")]
    public string VideoId { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonProperty("channelTitle")]
    public string ChannelTitle { get; set; } = string.Empty;

    [JsonProperty("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonProperty("duration")]
    public string Duration { get; set; } = string.Empty;

    [JsonProperty("viewCount")]
    public long ViewCount { get; set; }

    [JsonProperty("likeCount")]
    public long LikeCount { get; set; }

    [JsonProperty("commentCount")]
    public long CommentCount { get; set; }

    [JsonProperty("thumbnailUrl")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [JsonProperty("tags")]
    public List<string> Tags { get; set; } = new List<string>();
}
