using Newtonsoft.Json;

namespace VideoScripts.Features.ClusterTopics.Models;

public class ClusterTopicsResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public List<TopicCluster> Clusters { get; set; } = new List<TopicCluster>();
}

public class TopicCluster
{
    public string ClusterName { get; set; } = string.Empty;
    public string ClusterDescription { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<TopicAssignment> Topics { get; set; } = new List<TopicAssignment>();
}

public class TopicAssignment
{
    public Guid TopicId { get; set; }
    public string TopicTitle { get; set; } = string.Empty;
    public string TopicSummary { get; set; } = string.Empty;
    public string AssignmentReason { get; set; } = string.Empty;
}

// OpenAI API request/response models
internal class ClusterTopicsOpenAiRequest
{
    [JsonProperty("model")]
    public string Model { get; set; } = "gpt-4o-mini";

    [JsonProperty("messages")]
    public List<ClusterTopicsOpenAiMessage> Messages { get; set; } = new List<ClusterTopicsOpenAiMessage>();

    [JsonProperty("max_tokens")]
    public int MaxTokens { get; set; } = 3000;

    [JsonProperty("temperature")]
    public double Temperature { get; set; } = 0.2;

    [JsonProperty("response_format")]
    public ClusterTopicsOpenAiResponseFormat ResponseFormat { get; set; } = new ClusterTopicsOpenAiResponseFormat();
}

internal class ClusterTopicsOpenAiMessage
{
    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;
}

internal class ClusterTopicsOpenAiResponseFormat
{
    [JsonProperty("type")]
    public string Type { get; set; } = "json_object";
}

internal class ClusterTopicsOpenAiResponse
{
    [JsonProperty("choices")]
    public List<ClusterTopicsOpenAiChoice> Choices { get; set; } = new List<ClusterTopicsOpenAiChoice>();
}

internal class ClusterTopicsOpenAiChoice
{
    [JsonProperty("message")]
    public ClusterTopicsOpenAiMessage Message { get; set; } = new ClusterTopicsOpenAiMessage();
}

// Response parsing models
internal class ClusteringResponseData
{
    [JsonProperty("clusters")]
    public List<ClusterResponseItem> Clusters { get; set; } = new List<ClusterResponseItem>();
}

internal class ClusterResponseItem
{
    [JsonProperty("cluster_name")]
    public string ClusterName { get; set; } = string.Empty;

    [JsonProperty("cluster_description")]
    public string ClusterDescription { get; set; } = string.Empty;

    [JsonProperty("display_order")]
    public int DisplayOrder { get; set; }

    [JsonProperty("topics")]
    public List<ClusterTopicItem> Topics { get; set; } = new List<ClusterTopicItem>();
}

internal class ClusterTopicItem
{
    [JsonProperty("topic_index")]
    public int TopicIndex { get; set; }

    [JsonProperty("assignment_reason")]
    public string AssignmentReason { get; set; } = string.Empty;
}