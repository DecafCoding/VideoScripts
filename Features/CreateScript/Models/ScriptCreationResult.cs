namespace VideoScripts.Features.CreateScript.Models;

/// <summary>
/// Result model for script creation operations
/// </summary>
public class ScriptCreationResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ScriptTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Version { get; set; }
    public int TotalWordCount { get; set; }
    public double EstimatedMinutes { get; set; }
    public List<string> VideoTitles { get; set; } = new List<string>();
    public int TranscriptCount { get; set; }

    // Token usage information
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}
