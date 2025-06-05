namespace VideoScripts.Features.ShowClusters.Models;

/// <summary>
/// Details about a specific topic within a cluster
/// </summary>
public class TopicDetails
{
    public string TopicTitle { get; set; } = string.Empty;
    public string TopicSummary { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public string VideoTitle { get; set; } = string.Empty;
    public bool HasBlueprintElements { get; set; }
    public int BlueprintElementsCount { get; set; }
}