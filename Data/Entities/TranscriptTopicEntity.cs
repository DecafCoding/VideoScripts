using Microsoft.EntityFrameworkCore.Metadata.Internal;

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace VideoScripts.Data.Entities;

public class TranscriptTopicEntity
{
    [Required]
    [ForeignKey("Video")]
    public Guid VideoId { get; set; }
    public TimeSpan StartTime { get; set; }
    public string? Content { get; set; }
    public string? TopicSummary { get; set; }
    public bool IsSelected { get; set; }
    // Navigation property
    public virtual VideoEntity Video { get; set; }
}

}
