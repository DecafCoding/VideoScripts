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

/// <summary>
/// Detailed cluster information for a specific project
/// </summary>
public class ProjectClusterDetails
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectTopic { get; set; } = string.Empty;
    public List<ClusterDetails> Clusters { get; set; } = new List<ClusterDetails>();
}

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
