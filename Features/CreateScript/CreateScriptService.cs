using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using VideoScripts.Features.CreateScript.Models;

namespace VideoScripts.Features.CreateScript;

/// <summary>
/// Service for creating scripts using OpenAI API
/// </summary>
public class CreateScriptService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CreateScriptService> _logger;
    private readonly string _apiKey;
    private const string OpenAIApiUrl = "https://api.openai.com/v1/chat/completions";

    public CreateScriptService(
        IConfiguration configuration,
        ILogger<CreateScriptService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey configuration is missing");

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    /// <summary>
    /// Creates a script from raw transcripts using OpenAI
    /// </summary>
    /// <param name="projectTopic">The project topic to focus the script on</param>
    /// <param name="videoData">List of video titles and their raw transcripts</param>
    /// <returns>Generated script content</returns>
    public async Task<string> CreateScriptFromTranscriptsAsync(string projectTopic, List<(string title, string transcript)> videoData)
    {
        try
        {
            _logger.LogInformation($"Creating script for project topic: {projectTopic} with {videoData.Count} transcripts");

            // Build the prompt with the project topic and transcripts
            var prompt = BuildScriptPrompt(projectTopic, videoData);

            // Create OpenAI request
            var request = new OpenAIRequest
            {
                Model = "gpt-4o",
                MaxTokens = 4000,
                Temperature = 0.7,
                Messages = new List<OpenAIMessage>
                {
                    new OpenAIMessage
                    {
                        Role = "system",
                        Content = "You are an expert YouTube scriptwriter who creates engaging, retention-focused video scripts. Follow the provided framework exactly and create compelling content that keeps viewers watching."
                    },
                    new OpenAIMessage
                    {
                        Role = "user",
                        Content = prompt
                    }
                }
            };

            // Make API call
            var jsonContent = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(OpenAIApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"OpenAI API request failed: {response.StatusCode} - {errorContent}");
                throw new HttpRequestException($"OpenAI API request failed: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var openAIResponse = JsonConvert.DeserializeObject<OpenAIResponse>(responseContent);

            if (openAIResponse?.Error != null)
            {
                _logger.LogError($"OpenAI API error: {openAIResponse.Error.Message}");
                throw new InvalidOperationException($"OpenAI API error: {openAIResponse.Error.Message}");
            }

            if (openAIResponse?.Choices == null || !openAIResponse.Choices.Any())
            {
                _logger.LogWarning("No choices returned from OpenAI API");
                throw new InvalidOperationException("No script content generated");
            }

            var generatedScript = openAIResponse.Choices[0].Message.Content;

            if (string.IsNullOrWhiteSpace(generatedScript))
            {
                _logger.LogWarning("Empty script content returned from OpenAI");
                throw new InvalidOperationException("Empty script content generated");
            }

            _logger.LogInformation($"Successfully generated script with {generatedScript.Length} characters");
            return generatedScript;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating script: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds the complete prompt for script creation including the base prompt and transcript data
    /// </summary>
    private string BuildScriptPrompt(string projectTopic, List<(string title, string transcript)> videoData)
    {
        var promptBuilder = new StringBuilder();

        // Start with the base prompt, replacing the placeholder with the actual topic
        var basePrompt = Prompts.CreateScript().Replace("[INSERT TOPIC]", projectTopic);
        promptBuilder.AppendLine(basePrompt);

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
    private string TruncateTranscript(string transcript, int maxWords)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            return string.Empty;

        var words = transcript.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length <= maxWords)
            return transcript;

        var truncated = string.Join(" ", words.Take(maxWords));
        return truncated + "... [transcript truncated for length]";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}