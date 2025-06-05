namespace VideoScripts.Features.ShowClusters.Models;

/// <summary>
/// Summary information about projects that have clusters
/// </summary>
public class ProjectClusterSummary
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectTopic { get; set; } = string.Empty;
    public int ClusterCount { get; set; }
    public int TotalTopics { get; set; }
}
