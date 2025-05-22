using System.ComponentModel.DataAnnotations;

namespace VideoScripts.Data.Entities;

public class ChannelEntity
{
    [Required]
    public string YTId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ThumbnailURL { get; set; }
    public int VideoCount { get; set; }
    public int SubscriberCount { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime? LastCheckDate { get; set; }
    //Relationships
    public virtual ICollection<VideoEntity> Videos { get; set; } = new List<VideoEntity>();
}
