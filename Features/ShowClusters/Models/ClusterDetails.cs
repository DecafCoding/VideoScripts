namespace VideoScripts.Features.ShowClusters.Models;

/// <summary>
/// Details about a specific cluster
/// </summary>
public class ClusterDetails
{
    public string ClusterName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int TopicCount { get; set; }
    public List<TopicDetails> Topics { get; set; } = new List<TopicDetails>();
}
