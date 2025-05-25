using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VideoScripts.Data.Common;

namespace VideoScripts.Data.Entities;

public class TopicClusterAssignmentEntity : BaseEntity
{
    [Required]
    [ForeignKey("TopicCluster")]
    public Guid TopicClusterId { get; set; }

    [Required]
    [ForeignKey("TranscriptTopic")]
    public Guid TranscriptTopicId { get; set; }

    // Navigation properties
    public virtual TopicClusterEntity TopicCluster { get; set; } = null!;

    public virtual TranscriptTopicEntity TranscriptTopic { get; set; } = null!;
}