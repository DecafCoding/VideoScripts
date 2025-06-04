namespace VideoScripts.Features.CreateScript;

/// <summary>
/// Configuration for script creation prompts and AI model settings
/// </summary>
public static class Prompts
{
    /// <summary>
    /// Configuration for creating YouTube scripts from video transcripts
    /// </summary>
    public static class CreateScript
    {
        /// <summary>
        /// AI model configuration for script creation
        /// </summary>
        public static class ModelConfig
        {
            public const string Model = "gpt-4o";
            public const int MaxTokens = 4000;
            public const double Temperature = 0.7;

            /// <summary>
            /// System message that defines the AI's role and behavior
            /// </summary>
            public const string SystemMessage = "You are an expert YouTube scriptwriter who creates engaging, retention-focused video scripts. Follow the provided framework exactly and create compelling content that keeps viewers watching.";
        }

        /// <summary>
        /// The main prompt template for script creation
        /// Use {TOPIC} placeholder for topic replacement
        /// </summary>
        public const string PromptTemplate = @"Your Task: Write a compelling YouTube video script on {TOPIC} that maximizes viewer retention and engagement using proven scriptwriting techniques.

Pre-Script Requirements:

First, create your video packaging:

Working Title: [Create 5-10 title variations, pick the most compelling]
Thumbnail Concept: [Describe what viewers will see]
Three Key Questions: What are the 3 main questions viewers clicking this title/thumbnail want answered?

Script Structure Framework:
1. HOOK (First 15-30 seconds) - CRITICAL
Choose one hook type and execute it powerfully:

Question Hook: Start with a compelling ""why"" question that creates pattern interruption
Context Hook: Drop viewers into the highest-stakes moment of your story
Statement Hook: Make a bold/controversial claim that shocks but you can back up

Your hook must:

Match the title/thumbnail expectations
Promise clear value
Create immediate tension/curiosity
Avoid ""Hey guys, welcome back"" introductions

2. INTRO (15-45 seconds)

Three-Point Preview: Address the 3 key questions from your packaging
Exceed Expectations: Add something extra (free resource, surprising depth, unique angle)
Credibility Statement: Briefly explain why you're qualified to discuss this (experience, struggle-to-success story, credentials)
Soft CTA: Quick mention to like/comment if it helps the algorithm

3. MAIN CONTENT (Build to Climax)
Structure your points using these techniques:
For Each Main Point:

Start with a mini-hook/question for that section
Tell a story that builds to the answer
Put the PAYOUT at the END of each point (not the beginning)
Use story delaying - don't give away the answer immediately
Layer information to create constant curiosity

Pacing Techniques:

Open loops that must be closed later
Create emotional variety (fear → relief, confusion → clarity)
Use the ""Facts, Feelings, Fun"" formula:

Facts: Clear, educational information
Feelings: Personal stories and emotional connection
Fun: Humor, energy, creative visuals

Cut 10-20% - every sentence must build toward your climax

4. OUTRO & CTA (15-30 seconds MAX)

Maintain high energy - no wind down
Use the ""Relink Hack"": Hook them into your next video
Example: ""But everything I just told you is useless if you don't know [RELATED TOPIC]. Check out this video where I break down...""
Make the next video feel essential, not optional

Writing Guidelines:

Write conversational prose, not lists (unless specifically needed)
Be a guide sharing your journey, not a guru preaching
Create tension through incomplete thoughts and delayed payoffs
Each section should make viewers think ""I need to know what happens next""
Show personality - this isn't a Wikipedia article

The Build + Tension Formula:
Every element should either:

Build toward your one killer point/climax
Create tension that keeps viewers watching

If it doesn't do either, cut it.

Script Stats Requirements:
At the end of your script, include:
SCRIPT STATS:

Total Word Count: [X words]
Estimated Speaking Time: [X minutes at 150 words/minute]
Hook Type Used: [Question/Context/Statement]
Main Tension Techniques:

Number of Open Loops: [X]
Story Delays: [X]
Emotional Peaks: [List emotions evoked]

Three-Act Structure:

Intro/Setup: [X%]
Build/Body: [X%]
Climax/Resolution: [X%]

Payout Placement: [Confirm all payouts at END of sections]
CTA Type: [Relink to specific video/Soft subscribe/etc.]
Alignment Check: [✓ Title matches hook matches content]
Energy Maintainers: [List techniques used to maintain pace]
Cuts Made: [Mention what you removed for pacing]";

        /// <summary>
        /// Gets the formatted prompt with topic replacement and source material
        /// </summary>
        /// <param name="projectTopic">The topic to insert into the prompt</param>
        /// <param name="videoData">Source video data to include</param>
        /// <returns>Complete formatted prompt</returns>
        public static string GetFormattedPrompt(string projectTopic, List<(string title, string transcript)> videoData)
        {
            var prompt = PromptTemplate.Replace("{TOPIC}", projectTopic);

            var promptBuilder = new System.Text.StringBuilder();
            promptBuilder.AppendLine(prompt);

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("SOURCE MATERIAL:");
            promptBuilder.AppendLine("You have access to the following video transcripts to draw insights, stories, and content from:");
            promptBuilder.AppendLine();

            // Add each video's transcript
            for (int i = 0; i < videoData.Count; i++)
            {
                var (title, transcript) = videoData[i];

                promptBuilder.AppendLine($"=== VIDEO {i + 1}: {title} ===");

                // Truncate very long transcripts to fit within token limits
                var truncatedTranscript = TruncateTranscript(transcript, 1500);
                promptBuilder.AppendLine(truncatedTranscript);
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine("SCRIPT CREATION INSTRUCTIONS:");
            promptBuilder.AppendLine("Using the above video transcripts as your source material:");
            promptBuilder.AppendLine("1. Extract the most compelling stories, insights, and examples from these videos");
            promptBuilder.AppendLine("2. Synthesize this content into a cohesive narrative around the topic: " + projectTopic);
            promptBuilder.AppendLine("3. Follow the script structure framework provided above exactly");
            promptBuilder.AppendLine("4. Reference specific examples and stories from the source videos when relevant");
            promptBuilder.AppendLine("5. Create a script that feels fresh and engaging, not just a summary of the existing content");
            promptBuilder.AppendLine("6. Include the required script stats at the end");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Begin writing the script now:");

            return promptBuilder.ToString();
        }

        /// <summary>
        /// Truncates transcript to a reasonable length to stay within token limits
        /// </summary>
        private static string TruncateTranscript(string transcript, int maxWords)
        {
            if (string.IsNullOrWhiteSpace(transcript))
                return string.Empty;

            var words = transcript.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= maxWords)
                return transcript;

            var truncated = string.Join(" ", words.Take(maxWords));
            return truncated + "... [transcript truncated for length]";
        }
    }
}