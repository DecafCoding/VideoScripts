namespace VideoScripts.Features.ShowClusters.Models;

/// <summary>
/// Detailed cluster information for a specific project
/// </summary>
public class ProjectClusterDetails
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectTopic { get; set; } = string.Empty;
    public List<ClusterDetails> Clusters { get; set; } = new List<ClusterDetails>();
}
