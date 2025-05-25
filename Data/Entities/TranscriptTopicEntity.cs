using Microsoft.EntityFrameworkCore.Metadata.Internal;

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using VideoScripts.Data.Common;

namespace VideoScripts.Data.Entities;

public class TranscriptTopicEntity : BaseEntity
{
    [Required]
    [ForeignKey("Video")]
    public Guid VideoId { get; set; }

    // Populated from AI output
    public TimeSpan StartTime { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string TopicSummary { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string BluePrintElements { get; set; } = string.Empty;

    public bool IsSelected { get; set; }

    // Navigation properties
    public virtual VideoEntity Video { get; set; } = null!;

    public virtual TopicClusterAssignmentEntity? ClusterAssignment { get; set; }
}