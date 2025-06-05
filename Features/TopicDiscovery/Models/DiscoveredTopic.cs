namespace VideoScripts.Features.TopicDiscovery.Models;

public class DiscoveredTopic
{
    public TimeSpan StartTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TopicSummary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string BlueprintElements { get; set; } = string.Empty; // JSON string of array
}