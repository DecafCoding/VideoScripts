namespace VideoScripts.Features.TopicDiscovery;

/// <summary>
/// Configuration for topic discovery prompts and AI model settings
/// </summary>
public static class Prompts
{
    /// <summary>
    /// Configuration for discovering topics from video transcripts
    /// </summary>
    public static class TopicDiscovery
    {
        /// <summary>
        /// AI model configuration for topic discovery
        /// </summary>
        public static class ModelConfig
        {
            public const string Model = "gpt-4o-mini";
            public const int MaxTokens = 3000;
            public const double Temperature = 0.2;

            /// <summary>
            /// System message that defines the AI's role and behavior for topic discovery
            /// </summary>
            public const string SystemMessage = "You are a content strategist specializing in breaking down educational videos into structured learning modules. Follow the provided framework exactly and identify distinct topics with precise timing information.";
        }

        /// <summary>
        /// The main prompt template for topic discovery
        /// </summary>
        public const string PromptTemplate = @"You are a content strategist specializing in breaking down educational videos into structured learning modules.

Analyze the YouTube transcript and identify distinct topics, sections, and key learning points. Focus on main content - skip intros, promotions, and conclusions.

Key Requirements:
• Identify natural topic transitions and thematic changes
• Extract frameworks, blueprints, or step-by-step processes
• Capture actionable insights and key takeaways  
• Use clear timestamps for each section
• Focus on valuable, educational content

Output as JSON with this exact structure:

{
  ""topics"": [
    {
      ""starttime"": ""HH:MM:SS"",
      ""title"": ""Clear, descriptive section title"",
      ""summary"": ""1-2 sentence overview of what's covered"",
      ""content"": ""Detailed breakdown including key points, steps, or concepts"",
      ""blueprint_elements"": [""step 1"", ""step 2""] // Array of steps/elements if applicable, empty array otherwise
    }
  ]
}

Guidelines:
• Use HH:MM:SS format for timestamps (00:00:00 if unclear)
• Extract numbered steps or frameworks explicitly
• Maintain speaker's original terminology for technical concepts
• Prioritize actionable, educational content
• Ensure each topic has clear value for learners";

        /// <summary>
        /// Gets the formatted prompt with transcript content
        /// </summary>
        /// <param name="transcript">The video transcript to analyze</param>
        /// <returns>Complete formatted prompt</returns>
        public static string GetFormattedPrompt(string transcript)
        {
            var promptBuilder = new System.Text.StringBuilder();
            promptBuilder.AppendLine(PromptTemplate);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Transcript to analyze:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(transcript);

            return promptBuilder.ToString();
        }
    }
}