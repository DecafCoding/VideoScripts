using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace VideoScripts.Data.Entities;

public class ScriptEntity
{
    [Required]
    [ForeignKey("Project")]
    public Guid ProjectId { get; set; }
    [Required]
    public string Title { get; set; }
    public string? Content { get; set; }
    public int Version { get; set; }
    // Navigation property
    public virtual ProjectEntity Project { get; set; }

}
