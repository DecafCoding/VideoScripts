using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using VideoScripts.Data.Common;

namespace VideoScripts.Data.Entities;

public class VideoEntity : BaseEntity
{
    [ForeignKey("Project")]
    public Guid? ProjectId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string YTId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(5000)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [ForeignKey("Channel")]
    public Guid ChannelId { get; set; }
    
    public int ViewCount { get; set; }
    
    public int LikeCount { get; set; }
    
    public int CommentCount { get; set; }
    
    public int Duration { get; set; }
    
    public DateTime PublishedAt { get; set; }
    
    public string RawTranscript { get; set; } = string.Empty;

    // Populated from AI output
    [MaxLength(200)]
    public string VideoTopic { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string MainSummary { get; set; } = string.Empty;

    public string StructuredContent { get; set; } = string.Empty;

    // Navigation properties
    public virtual ProjectEntity Project { get; set; } = null!;

    public virtual ChannelEntity Channel { get; set; } = null!;
    
    public virtual ICollection<TranscriptTopicEntity> TranscriptTopics { get; set; } = [];
}
