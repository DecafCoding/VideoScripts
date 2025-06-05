namespace VideoScripts.Features.AnalyzeClusters;

/// <summary>
/// Configuration for cluster analysis prompts and AI model settings
/// </summary>
public static class Prompts
{
    /// <summary>
    /// Configuration for cluster readiness analysis
    /// </summary>
    public static class ClusterReadiness
    {
        /// <summary>
        /// AI model configuration for readiness analysis
        /// </summary>
        public static class ModelConfig
        {
            public const string Model = "gpt-4o-mini";
            public const int MaxTokens = 4000;
            public const double Temperature = 0.1;
            public const string ResponseFormat = "json_object";

            /// <summary>
            /// System message that defines the AI's role for readiness analysis
            /// </summary>
            public const string SystemMessage = "You are an expert content strategist analyzing video transcript clusters for script development potential. You must respond with ONLY valid JSON in the exact format specified.";
        }

        /// <summary>
        /// The prompt template for cluster readiness analysis
        /// Use {CLUSTER_DATA} placeholder for cluster data replacement
        /// </summary>
        public const string PromptTemplate = @"I will provide you with a cluster of related topics from YouTube video transcripts. Each topic includes:
- Title, Summary, Content details, Blueprint elements (if any), Start time from original video

Your task is to evaluate this cluster's ""script readiness"" by analyzing narrative completeness, structural coherence, and identifying missing elements.

**CRITICAL: You must respond with ONLY valid JSON in the exact format specified below. No additional text, explanations, or markdown formatting.**

Return your analysis as a JSON object with this exact structure:

```json
{
  ""overall_readiness_score"": [1-10 integer],
  ""narrative_completeness_score"": [1-10 integer],
  ""structural_coherence_score"": [1-10 integer],
  ""cluster_type"": ""[Introductory|Implementation|Deep Dive|Case Study|Mixed]"",
  ""key_strengths"": [
    ""Strength 1 description"",
    ""Strength 2 description""
  ],
  ""critical_gaps"": [
    ""Gap 1 description"",
    ""Gap 2 description""
  ],
  ""missing_elements"": [
    ""Missing element 1"",
    ""Missing element 2""
  ],
  ""script_usage_recommendation"": ""Detailed recommendation for how to best use this cluster in a script""
}
```

**Scoring Guidelines:**
- **Narrative Completeness (1-10)**: Does this tell a complete story with beginning, middle, end?
- **Structural Coherence (1-10)**: How well do topics flow together logically?
- **Overall Readiness (1-10)**: Would this work as a standalone video script section?

**Cluster Types:**
- **Introductory**: Concepts, theory, foundational knowledge
- **Implementation**: How-to steps, practical application
- **Deep Dive**: Advanced details, comprehensive exploration  
- **Case Study**: Examples, real-world applications, stories
- **Mixed**: Combination of multiple types

Provide actionable insights for script development while maintaining focus on educational video content.

Cluster data:
{CLUSTER_DATA}";

        /// <summary>
        /// Gets the formatted prompt with cluster data replacement
        /// </summary>
        /// <param name="clusterData">The cluster data to insert into the prompt</param>
        /// <returns>Complete formatted prompt</returns>
        public static string GetFormattedPrompt(string clusterData)
        {
            return PromptTemplate.Replace("{CLUSTER_DATA}", clusterData);
        }
    }

    /// <summary>
    /// Configuration for content density analysis
    /// </summary>
    public static class ContentDensity
    {
        /// <summary>
        /// AI model configuration for density analysis
        /// </summary>
        public static class ModelConfig
        {
            public const string Model = "gpt-4o-mini";
            public const int MaxTokens = 4000;
            public const double Temperature = 0.1;
            public const string ResponseFormat = "json_object";

            /// <summary>
            /// System message that defines the AI's role for density analysis
            /// </summary>
            public const string SystemMessage = "You are a content analyst specializing in educational video scripts. You must respond with ONLY valid JSON in the exact format specified.";
        }

        /// <summary>
        /// The prompt template for content density analysis
        /// Use {CLUSTER_DATA} placeholder for cluster data replacement
        /// </summary>
        public const string PromptTemplate = @"Analyze the following cluster of video topics for content density and depth. Evaluate information density, pacing implications, and cognitive load.

**CRITICAL: You must respond with ONLY valid JSON in the exact format specified below. No additional text, explanations, or markdown formatting.**

Return your analysis as a JSON object with this exact structure:

```json
{
  ""overall_density"": ""[Light|Medium|Heavy]"",
  ""depth_breadth_ratio"": ""Descriptive ratio like '70% depth, 30% breadth'"",
  ""recommended_script_pacing"": ""Specific pacing recommendation with timing"",
  ""cognitive_load"": ""[Low|Medium|High]"",
  ""topic_density_ratings"": [
    {
      ""topic_title"": ""Topic name from cluster"",
      ""density_level"": ""[Light|Medium|Heavy]"",
      ""information_type"": ""[Conceptual|Actionable|Mixed]""
    }
  ],
  ""simplification_opportunities"": [
    ""Opportunity 1 description"",
    ""Opportunity 2 description""
  ],
  ""pacing_implications"": [
    ""Pacing consideration 1"",
    ""Pacing consideration 2""
  ]
}
```

**Analysis Guidelines:**
- **Light Density**: Overview content, conceptual introductions
- **Medium Density**: Detailed explanations with some actionable elements  
- **Heavy Density**: Information-packed, complex concepts, many actionable steps

- **Information Types**:
  - **Conceptual**: Theory, explanations, background knowledge
  - **Actionable**: Steps, instructions, practical applications
  - **Mixed**: Combination of conceptual and actionable content

- **Cognitive Load**: Consider how much new information viewers must process
- **Pacing**: Factor in need for examples, breaks, repetition for comprehension

Focus on practical implications for video script development and viewer experience.

Cluster data:
{CLUSTER_DATA}";

        /// <summary>
        /// Gets the formatted prompt with cluster data replacement
        /// </summary>
        /// <param name="clusterData">The cluster data to insert into the prompt</param>
        /// <returns>Complete formatted prompt</returns>
        public static string GetFormattedPrompt(string clusterData)
        {
            return PromptTemplate.Replace("{CLUSTER_DATA}", clusterData);
        }
    }

    /// <summary>
    /// Configuration for structural elements analysis
    /// </summary>
    public static class StructuralElements
    {
        /// <summary>
        /// AI model configuration for structural analysis
        /// </summary>
        public static class ModelConfig
        {
            public const string Model = "gpt-4o-mini";
            public const int MaxTokens = 4000;
            public const double Temperature = 0.1;
            public const string ResponseFormat = "json_object";

            /// <summary>
            /// System message that defines the AI's role for structural analysis
            /// </summary>
            public const string SystemMessage = "You are a script development specialist focusing on instructional design. You must respond with ONLY valid JSON in the exact format specified.";
        }

        /// <summary>
        /// The prompt template for structural elements analysis
        /// Use {CLUSTER_DATA} placeholder for cluster data replacement
        /// </summary>
        public const string PromptTemplate = @"Examine this cluster for structural elements that could anchor a video script. Identify frameworks, processes, lists, and blueprint elements.

**CRITICAL: You must respond with ONLY valid JSON in the exact format specified below. No additional text, explanations, or markdown formatting.**

Return your analysis as a JSON object with this exact structure:

```json
{
  ""total_structural_elements"": [integer count],
  ""primary_anchor_element"": ""Name of the strongest framework/blueprint for script focus"",
  ""frameworks_and_models"": [
    {
      ""name"": ""Framework name"",
      ""completeness_score"": [1-10 integer],
      ""instructional_value"": ""Description of teaching value"",
      ""description"": ""Brief description of the framework""
    }
  ],
  ""step_by_step_processes"": [
    {
      ""name"": ""Process name"",
      ""step_count"": [integer],
      ""clarity_score"": [1-10 integer],
      ""actionability_score"": [1-10 integer],
      ""missing_steps"": [""Missing step 1"", ""Missing step 2""]
    }
  ],
  ""lists_and_enumerations"": [
    {
      ""name"": ""List name (e.g., '5 Ways to...', 'Top 10...')"",
      ""item_count"": [integer],
      ""organization_quality"": ""[Excellent|Good|Fair|Poor]"",
      ""memorability_score"": [1-10 integer]
    }
  ],
  ""blueprint_elements"": [
    {
      ""name"": ""Blueprint name"",
      ""practical_application"": ""Description of how it can be applied"",
      ""uniqueness_score"": [1-10 integer],
      ""value_score"": [1-10 integer]
    }
  ],
  ""hook_potential_elements"": [
    ""Element 1 that could serve as compelling video opening"",
    ""Element 2 with strong hook potential""
  ],
  ""script_structure_suggestion"": ""Detailed suggestion for organizing these elements in a script"",
  ""missing_structural_pieces"": [
    ""Missing piece 1 that would complete the instructional value"",
    ""Missing piece 2 for better structure""
  ]
}
```

**Analysis Guidelines:**
- **Completeness Score (1-10)**: How complete/developed is this framework?
- **Clarity Score (1-10)**: How clear and understandable are the steps?
- **Actionability Score (1-10)**: How practical and implementable?
- **Uniqueness Score (1-10)**: How unique or novel is this element?
- **Value Score (1-10)**: How valuable for the target audience?
- **Memorability Score (1-10)**: How memorable and sticky is this list?

Look for elements that could serve as:
- Video structure backbone
- Compelling openings/hooks  
- Practical takeaways
- Teaching frameworks
- Step-by-step guides

Focus on elements that would make the script more engaging, organized, and valuable to viewers.

Cluster data:
{CLUSTER_DATA}";

        /// <summary>
        /// Gets the formatted prompt with cluster data replacement
        /// </summary>
        /// <param name="clusterData">The cluster data to insert into the prompt</param>
        /// <returns>Complete formatted prompt</returns>
        public static string GetFormattedPrompt(string clusterData)
        {
            return PromptTemplate.Replace("{CLUSTER_DATA}", clusterData);
        }
    }
}