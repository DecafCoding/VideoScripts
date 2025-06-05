namespace VideoScripts.Features.ClusterTopics.Models;

public class ClusterTopicsResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public List<TopicCluster> Clusters { get; set; } = new List<TopicCluster>();
}