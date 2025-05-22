using System.ComponentModel.DataAnnotations;

namespace VideoScripts.Data.Entities;

public class ProjectEntity
{
    [Required]
    public string Name { get; set; }
    [Required]
    public string Topic { get; set; }
    // Navigation properties
    public virtual ICollection<VideoEntity> Videos { get; set; } = new List<VideoEntity>();
    public virtual ICollection<ScriptEntity> Scripts { get; set; } = new List<ScriptEntity>();

}
