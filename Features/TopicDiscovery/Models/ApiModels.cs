using Newtonsoft.Json;

namespace VideoScripts.Features.TopicDiscovery.Models;

// OpenAI API request/response models
internal class TopicDiscoveryOpenAiRequest
{
    [JsonProperty("model")]
    public string Model { get; set; } = "gpt-4";

    [JsonProperty("messages")]
    public List<TopicDiscoveryOpenAiMessage> Messages { get; set; } = new List<TopicDiscoveryOpenAiMessage>();

    [JsonProperty("max_tokens")]
    public int MaxTokens { get; set; } = 4000;

    [JsonProperty("temperature")]
    public double Temperature { get; set; } = 0.2;

    [JsonProperty("response_format")]
    public TopicDiscoveryOpenAiResponseFormat ResponseFormat { get; set; } = new TopicDiscoveryOpenAiResponseFormat();
}

internal class TopicDiscoveryOpenAiMessage
{
    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;
}

internal class TopicDiscoveryOpenAiResponseFormat
{
    [JsonProperty("type")]
    public string Type { get; set; } = "json_object";
}

internal class TopicDiscoveryOpenAiResponse
{
    [JsonProperty("choices")]
    public List<TopicDiscoveryOpenAiChoice> Choices { get; set; } = new List<TopicDiscoveryOpenAiChoice>();
}

internal class TopicDiscoveryOpenAiChoice
{
    [JsonProperty("message")]
    public TopicDiscoveryOpenAiMessage Message { get; set; } = new TopicDiscoveryOpenAiMessage();
}

// Response parsing models
internal class TopicDiscoveryResponseData
{
    [JsonProperty("topics")]
    public List<TopicResponseItem> Topics { get; set; } = new List<TopicResponseItem>();
}

internal class TopicResponseItem
{
    [JsonProperty("starttime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    [JsonProperty("blueprint_elements")]
    public List<string>? BlueprintElements { get; set; }
}
