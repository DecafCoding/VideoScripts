using Newtonsoft.Json;

namespace VideoScripts.Features.YouTube.Models;

public class YouTubeChannelInfo
{
    [JsonProperty("channelId")]
    public string ChannelId { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("customUrl")]
    public string CustomUrl { get; set; } = string.Empty;

    [JsonProperty("publishedAt")]
    public DateTime PublishedAt { get; set; }

    [JsonProperty("thumbnailUrl")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [JsonProperty("subscriberCount")]
    public long SubscriberCount { get; set; }

    [JsonProperty("videoCount")]
    public long VideoCount { get; set; }

    [JsonProperty("viewCount")]
    public long ViewCount { get; set; }

    [JsonProperty("country")]
    public string Country { get; set; } = string.Empty;
}