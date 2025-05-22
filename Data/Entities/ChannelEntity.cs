using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VideoScripts.Data.Common;

namespace VideoScripts.Data.Entities;

public class ChannelEntity : BaseEntity
{
    [Required]
    public string YTId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string? Title { get; set; }
    
    [MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ThumbnailURL { get; set; } = string.Empty;
    
    public int VideoCount { get; set; }
    
    public int SubscriberCount { get; set; }
    
    public DateTime PublishedAt { get; set; }
    
    public DateTime? LastCheckDate { get; set; }

    // Navigation property
    public virtual ICollection<VideoEntity> Videos { get; set; } = [];
}
