using Newtonsoft.Json;

namespace VideoScripts.Features.AnalyzeClusters.Models;

/// <summary>
/// Cluster readiness analysis evaluating script development potential
/// </summary>
public class ClusterReadinessAnalysis
{
    [JsonProperty("overall_readiness_score")]
    public int OverallReadinessScore { get; set; }

    [JsonProperty("narrative_completeness_score")]
    public int NarrativeCompletenessScore { get; set; }

    [JsonProperty("structural_coherence_score")]
    public int StructuralCoherenceScore { get; set; }

    [JsonProperty("cluster_type")]
    public string ClusterType { get; set; } = string.Empty;

    [JsonProperty("key_strengths")]
    public List<string> KeyStrengths { get; set; } = new List<string>();

    [JsonProperty("critical_gaps")]
    public List<string> CriticalGaps { get; set; } = new List<string>();

    [JsonProperty("missing_elements")]
    public List<string> MissingElements { get; set; } = new List<string>();

    [JsonProperty("script_usage_recommendation")]
    public string ScriptUsageRecommendation { get; set; } = string.Empty;
}

/// <summary>
/// Content density analysis measuring depth vs breadth
/// </summary>
public class ContentDensityAnalysis
{
    [JsonProperty("overall_density")]
    public string OverallDensity { get; set; } = string.Empty; // Light/Medium/Heavy

    [JsonProperty("depth_breadth_ratio")]
    public string DepthBreadthRatio { get; set; } = string.Empty;

    [JsonProperty("recommended_script_pacing")]
    public string RecommendedScriptPacing { get; set; } = string.Empty;

    [JsonProperty("cognitive_load")]
    public string CognitiveLoad { get; set; } = string.Empty; // Low/Medium/High

    [JsonProperty("topic_density_ratings")]
    public List<TopicDensityRating> TopicDensityRatings { get; set; } = new List<TopicDensityRating>();

    [JsonProperty("simplification_opportunities")]
    public List<string> SimplificationOpportunities { get; set; } = new List<string>();

    [JsonProperty("pacing_implications")]
    public List<string> PacingImplications { get; set; } = new List<string>();
}

/// <summary>
/// Individual topic density rating
/// </summary>
public class TopicDensityRating
{
    [JsonProperty("topic_title")]
    public string TopicTitle { get; set; } = string.Empty;

    [JsonProperty("density_level")]
    public string DensityLevel { get; set; } = string.Empty; // Light/Medium/Heavy

    [JsonProperty("information_type")]
    public string InformationType { get; set; } = string.Empty; // Conceptual/Actionable/Mixed
}

/// <summary>
/// Structural elements analysis identifying frameworks and blueprints
/// </summary>
public class StructuralElementsAnalysis
{
    [JsonProperty("total_structural_elements")]
    public int TotalStructuralElements { get; set; }

    [JsonProperty("primary_anchor_element")]
    public string PrimaryAnchorElement { get; set; } = string.Empty;

    [JsonProperty("frameworks_and_models")]
    public List<FrameworkElement> FrameworksAndModels { get; set; } = new List<FrameworkElement>();

    [JsonProperty("step_by_step_processes")]
    public List<ProcessElement> StepByStepProcesses { get; set; } = new List<ProcessElement>();

    [JsonProperty("lists_and_enumerations")]
    public List<ListElement> ListsAndEnumerations { get; set; } = new List<ListElement>();

    [JsonProperty("blueprint_elements")]
    public List<BlueprintElement> BlueprintElements { get; set; } = new List<BlueprintElement>();

    [JsonProperty("hook_potential_elements")]
    public List<string> HookPotentialElements { get; set; } = new List<string>();

    [JsonProperty("script_structure_suggestion")]
    public string ScriptStructureSuggestion { get; set; } = string.Empty;

    [JsonProperty("missing_structural_pieces")]
    public List<string> MissingStructuralPieces { get; set; } = new List<string>();
}

/// <summary>
/// Framework or model element found in the cluster
/// </summary>
public class FrameworkElement
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("completeness_score")]
    public int CompletenessScore { get; set; }

    [JsonProperty("instructional_value")]
    public string InstructionalValue { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Step-by-step process found in the cluster
/// </summary>
public class ProcessElement
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("step_count")]
    public int StepCount { get; set; }

    [JsonProperty("clarity_score")]
    public int ClarityScore { get; set; }

    [JsonProperty("actionability_score")]
    public int ActionabilityScore { get; set; }

    [JsonProperty("missing_steps")]
    public List<string> MissingSteps { get; set; } = new List<string>();
}

/// <summary>
/// List or enumeration found in the cluster
/// </summary>
public class ListElement
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("item_count")]
    public int ItemCount { get; set; }

    [JsonProperty("organization_quality")]
    public string OrganizationQuality { get; set; } = string.Empty;

    [JsonProperty("memorability_score")]
    public int MemorabilityScore { get; set; }
}

/// <summary>
/// Blueprint element found in the cluster
/// </summary>
public class BlueprintElement
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("practical_application")]
    public string PracticalApplication { get; set; } = string.Empty;

    [JsonProperty("uniqueness_score")]
    public int UniquenessScore { get; set; }

    [JsonProperty("value_score")]
    public int ValueScore { get; set; }
}

// OpenAI API request/response models
internal class AnalyzeClustersOpenAiRequest
{
    [JsonProperty("model")]
    public string Model { get; set; } = "gpt-4o-mini";

    [JsonProperty("messages")]
    public List<AnalyzeClustersOpenAiMessage> Messages { get; set; } = new List<AnalyzeClustersOpenAiMessage>();

    [JsonProperty("max_tokens")]
    public int MaxTokens { get; set; } = 3000;

    [JsonProperty("temperature")]
    public double Temperature { get; set; } = 0.2;

    [JsonProperty("response_format")]
    public AnalyzeClustersOpenAiResponseFormat ResponseFormat { get; set; } = new AnalyzeClustersOpenAiResponseFormat();
}

internal class AnalyzeClustersOpenAiMessage
{
    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;
}

internal class AnalyzeClustersOpenAiResponseFormat
{
    [JsonProperty("type")]
    public string Type { get; set; } = "json_object";
}

internal class AnalyzeClustersOpenAiResponse
{
    [JsonProperty("choices")]
    public List<AnalyzeClustersOpenAiChoice> Choices { get; set; } = new List<AnalyzeClustersOpenAiChoice>();
}

internal class AnalyzeClustersOpenAiChoice
{
    [JsonProperty("message")]
    public AnalyzeClustersOpenAiMessage Message { get; set; } = new AnalyzeClustersOpenAiMessage();
}