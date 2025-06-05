namespace VideoScripts.Features.ClusterTopics.Models;

public class TopicAssignment
{
    public Guid TopicId { get; set; }
    public string TopicTitle { get; set; } = string.Empty;
    public string TopicSummary { get; set; } = string.Empty;
    public string AssignmentReason { get; set; } = string.Empty;
}