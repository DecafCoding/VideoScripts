using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using VideoScripts.Data.Common;

namespace VideoScripts.Data.Entities;

public class ScriptEntity : BaseEntity
{
    [Required]
    [ForeignKey("Project")]
    public Guid ProjectId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;

    public int Version { get; set; }

    // Navigation property
    public virtual ProjectEntity Project { get; set; } = null!;
}
