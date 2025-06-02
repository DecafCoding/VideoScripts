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

    // Token usage tracking for OpenAI API calls
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }

    // Navigation property
    public virtual ProjectEntity Project { get; set; } = null!;
}