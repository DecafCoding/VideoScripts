namespace VideoScripts.Features.ShowClusters.Models;

/// <summary>
/// Processing summary for all projects
/// </summary>
public class ProjectProcessingSummary
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectTopic { get; set; } = string.Empty;
    public int VideoCount { get; set; }
    public int TotalTopics { get; set; }
    public int ClusteredTopics { get; set; }
    public int ClusterCount { get; set; }
    public bool HasClusters { get; set; }
    public bool IsFullyProcessed { get; set; }
}
