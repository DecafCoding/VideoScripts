using Newtonsoft.Json;

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

/// <summary>
/// Request model for OpenAI API calls
/// </summary>
internal class OpenAIRequest
{
    [JsonProperty("model")]
    public string Model { get; set; } = "gpt-4";

    [JsonProperty("messages")]
    public List<OpenAIMessage> Messages { get; set; } = new List<OpenAIMessage>();

    [JsonProperty("max_tokens")]
    public int MaxTokens { get; set; } = 4000;

    [JsonProperty("temperature")]
    public double Temperature { get; set; } = 0.7;
}

/// <summary>
/// Message model for OpenAI API
/// </summary>
internal class OpenAIMessage
{
    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Response model for OpenAI API
/// </summary>
internal class OpenAIResponse
{
    [JsonProperty("choices")]
    public List<OpenAIChoice> Choices { get; set; } = new List<OpenAIChoice>();

    [JsonProperty("usage")]
    public OpenAIUsage? Usage { get; set; }

    [JsonProperty("error")]
    public OpenAIError? Error { get; set; }
}

/// <summary>
/// Choice model for OpenAI API response
/// </summary>
internal class OpenAIChoice
{
    [JsonProperty("message")]
    public OpenAIMessage Message { get; set; } = new OpenAIMessage();
}

/// <summary>
/// Usage model for OpenAI API response (token tracking)
/// </summary>
public class OpenAIUsage
{
    [JsonProperty("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonProperty("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>
/// Error model for OpenAI API response
/// </summary>
internal class OpenAIError
{
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;
}