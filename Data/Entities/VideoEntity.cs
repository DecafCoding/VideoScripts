using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace VideoScripts.Data.Entities;

public class VideoEntity
{
    [ForeignKey("Project")]
    public Guid? ProjectId { get; set; }
    [Required]
    public string YTId { get; set; }
    [Required]
    public string Title { get; set; }
    [Required]
    public string Description { get; set; }
    [Required]
    [ForeignKey("Channel")]
    public Guid ChannelId { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int Duration { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? RawTranscript
    {
        get; set;
    public string? Summary { get; set; }
    // Navigation properties
    public virtual ProjectEntity Project { get; set; }
    public virtual ChannelEntity Channel { get; set; }
    public virtual ICollection<TranscriptTopicEntity> TranscriptTopics { get; set; } = new List<TranscriptTopicEntity>();

}
