namespace VideoScripts.Features.ClusterTopics.Models;

public class TopicCluster
{
    public string ClusterName { get; set; } = string.Empty;
    public string ClusterDescription { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<TopicAssignment> Topics { get; set; } = new List<TopicAssignment>();
}
