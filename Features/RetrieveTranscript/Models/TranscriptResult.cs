namespace VideoScripts.Features.RetrieveTranscript.Models;

public class TranscriptResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelUrl { get; set; } = string.Empty;
    public long SubscriberCount { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public string Duration { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Subtitles { get; set; } = string.Empty;
}