using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VideoScripts.Data.Common;

namespace VideoScripts.Features.AnalyzeClusters;

public static class Prompts
{
    /// <summary>
    /// Cluster Readiness Assessment Prompt (Cluster Readiness Score): 
    /// Some clusters might have complete narrative arcs while others are just collections of related points
    /// </summary>
    /// <returns></returns>
    public static string ClusterReadiness() => @"
You are an expert content strategist analyzing video transcript clusters for script development potential.

I will provide you with a cluster of related topics from YouTube video transcripts. Each topic includes:
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
[INSERT CLUSTER DATA HERE]";

    /// <summary>
    /// Content Density Analysis
    /// Measure depth vs breadth - is this cluster exploration-heavy or action-heavy?
    /// </summary>
    /// <returns></returns>
    public static string ContentDensity() => @"
You are a content analyst specializing in educational video scripts.

Analyze the following cluster of video topics for content density and depth. Evaluate information density, pacing implications, and cognitive load.

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
[INSERT CLUSTER DATA HERE]";

    /// <summary>
    /// Structural Elements Extraction Prompt
    /// Which clusters contain the most frameworks/blueprints that could anchor a script?
    /// </summary>
    /// <returns></returns>
    public static string StructuralElements() => @"
You are a script development specialist focusing on instructional design.

Examine this cluster for structural elements that could anchor a video script. Identify frameworks, processes, lists, and blueprint elements.

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
[INSERT CLUSTER DATA HERE]";
}