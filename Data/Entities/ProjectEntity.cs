using System.ComponentModel.DataAnnotations;
using VideoScripts.Data.Common;

namespace VideoScripts.Data.Entities;

public class ProjectEntity : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<VideoEntity> Videos { get; set; } = [];
    public virtual ICollection<ScriptEntity> Scripts { get; set; } = [];
}
