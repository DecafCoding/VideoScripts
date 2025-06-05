namespace VideoScripts.Features.ClusterTopics;

/// <summary>
/// Configuration for topic clustering prompts and AI model settings
/// </summary>
public static class Prompts
{
    /// <summary>
    /// Configuration for clustering video transcript topics
    /// </summary>
    public static class ClusterTopics
    {
        /// <summary>
        /// AI model configuration for topic clustering
        /// </summary>
        public static class ModelConfig
        {
            public const string Model = "gpt-4o-mini";
            public const int MaxTokens = 3000;
            public const double Temperature = 0.2;

            /// <summary>
            /// System message that defines the AI's role and behavior for clustering
            /// </summary>
            public const string SystemMessage = "You are an expert content strategist specializing in organizing educational content into logical learning modules.";
        }

        /// <summary>
        /// The main prompt template for topic clustering
        /// Use {PROJECT_NAME} placeholder for project name replacement
        /// </summary>
        public const string PromptTemplate = @"Your task is to analyze a list of video transcript topics and group them into meaningful clusters that would make sense for script generation and content organization.

Guidelines:
• Group related topics by theme, complexity level, or learning progression
• Create clusters that tell a cohesive story or learning path
• Prioritize logical flow and content coherence over perfect balance
• Consider if topics build upon each other or are standalone concepts
• Look for natural groupings like: Introduction/Basics, Core Concepts, Advanced Techniques, Implementation, etc.

For each cluster:
• Choose a clear, descriptive name (2-8 words)
• Provide a brief description of what the cluster covers
• Assign a display order (1,2,3...) for logical presentation sequence
• For each topic assignment, briefly explain why it fits in that cluster

Output as JSON with this exact structure:

{
  ""clusters"": [
    {
      ""cluster_name"": ""Introduction & Basics"",
      ""cluster_description"": ""Foundational concepts and getting started"",
      ""display_order"": 1,
      ""topics"": [
        {
          ""topic_index"": 0,
          ""assignment_reason"": ""Foundational concept that others build upon""
        },
        {
          ""topic_index"": 3,
          ""assignment_reason"": ""Basic setup information""
        }
      ]
    }
  ]
}

Important:
• Use topic_index to reference topics (0-based index from the provided list)
• Ensure every topic is assigned to exactly one cluster
• Keep cluster names concise but descriptive
• Focus on creating a logical learning progression

Project: {PROJECT_NAME}";

        /// <summary>
        /// Gets the formatted prompt with project name replacement and topic data
        /// </summary>
        /// <param name="projectName">The project name to insert into the prompt</param>
        /// <param name="topics">Topic data to include in the analysis</param>
        /// <returns>Complete formatted prompt for topic clustering</returns>
        public static string GetFormattedPrompt(string projectName, List<VideoScripts.Data.Entities.TranscriptTopicEntity> topics)
        {
            var prompt = PromptTemplate.Replace("{PROJECT_NAME}", projectName);

            var promptBuilder = new System.Text.StringBuilder();
            promptBuilder.AppendLine(prompt);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"Total Topics to Cluster: {topics.Count}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Topics to analyze and cluster:");

            for (int i = 0; i < topics.Count; i++)
            {
                var topic = topics[i];
                promptBuilder.AppendLine($"{i}: {topic.Title}");
                promptBuilder.AppendLine($"   Summary: {topic.TopicSummary}");

                // Include blueprint elements if available
                if (!string.IsNullOrWhiteSpace(topic.BluePrintElements))
                {
                    promptBuilder.AppendLine($"   Blueprint: {topic.BluePrintElements}");
                }
                promptBuilder.AppendLine();
            }

            return promptBuilder.ToString();
        }
    }
}