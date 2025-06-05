namespace VideoScripts.Features.TopicDiscovery.Models;

public class TopicDiscoveryResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public string VideoTitle { get; set; } = string.Empty;
    public List<DiscoveredTopic> Topics { get; set; } = new List<DiscoveredTopic>();
}