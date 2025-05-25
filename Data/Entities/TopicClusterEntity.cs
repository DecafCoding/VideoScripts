using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VideoScripts.Data.Common;

namespace VideoScripts.Data.Entities;

public class TopicClusterEntity : BaseEntity
{
    [Required]
    [ForeignKey("Project")]
    public Guid ProjectId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ClusterName { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    // Navigation properties
    public virtual ProjectEntity Project { get; set; } = null!;

    public virtual ICollection<TopicClusterAssignmentEntity> TopicAssignments { get; set; } = [];
}