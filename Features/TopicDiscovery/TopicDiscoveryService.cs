using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using VideoScripts.Features.TopicDiscovery.Models;

namespace VideoScripts.Features.TopicDiscovery;

public class TopicDiscoveryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TopicDiscoveryService> _logger;
    private readonly string _apiKey;
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";

    public TopicDiscoveryService(IConfiguration configuration, ILogger<TopicDiscoveryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey configuration is missing");

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Analyzes a video transcript and breaks it down into logical subtopics using OpenAI
    /// </summary>
    /// <param name="transcript">Raw video transcript text</param>
    /// <param name="videoId">YouTube video ID for logging</param>
    /// <param name="videoTitle">Video title for context</param>
    /// <returns>Topic discovery result with subtopics and metadata</returns>
    public async Task<TopicDiscoveryResult> AnalyzeTranscriptAsync(string transcript, string videoId, string videoTitle)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(transcript))
            {
                _logger.LogWarning($"Empty transcript provided for video {videoId}");
                return CreateFailedResult("Transcript is empty or null", videoId, videoTitle);
            }

            _logger.LogInformation($"Analyzing transcript for topic discovery: {videoTitle} ({videoId})");

            // Process transcript for API consumption
            var processedTranscript = ProcessTranscriptForApi(transcript);

            // Create the analysis request
            var request = CreateTopicDiscoveryRequest(processedTranscript);

            // Send request to OpenAI
            var response = await CallOpenAiApiAsync(request);

            if (response == null)
            {
                return CreateFailedResult("Failed to get response from OpenAI API", videoId, videoTitle);
            }

            // Parse and validate the response
            var topicResult = ParseOpenAiResponse(response, videoId, videoTitle);

            if (topicResult.Success)
            {
                _logger.LogInformation($"Successfully analyzed transcript for topics: {videoTitle} - Found {topicResult.Topics.Count} topics");
            }
            else
            {
                _logger.LogWarning($"Failed to analyze transcript for topics: {videoTitle} - {topicResult.ErrorMessage}");
            }

            return topicResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing transcript for topics {videoId}: {ex.Message}");
            return CreateFailedResult($"Analysis failed: {ex.Message}", videoId, videoTitle);
        }
    }

    /// <summary>
    /// Creates the OpenAI API request with the topic discovery prompt
    /// </summary>
    private TopicDiscoveryOpenAiRequest CreateTopicDiscoveryRequest(string transcript)
    {
        var systemPrompt = GetTopicDiscoveryPrompt();
        var userPrompt = $"Transcript to analyze:\n\n{transcript}";

        return new TopicDiscoveryOpenAiRequest
        {
            Model = "gpt-4o-mini", // Changed from "gpt-4" to "gpt-4o-mini"
            Messages = new List<TopicDiscoveryOpenAiMessage>
            {
                new TopicDiscoveryOpenAiMessage { Role = "system", Content = systemPrompt },
                new TopicDiscoveryOpenAiMessage { Role = "user", Content = userPrompt }
            },
            MaxTokens = 3000, // Reduced from 4000 - gpt-4o-mini is more efficient
            Temperature = 0.2, // Keeping low temperature for consistent output
            ResponseFormat = new TopicDiscoveryOpenAiResponseFormat { Type = "json_object" }
        };
    }

    /// <summary>
    /// Sends the request to OpenAI API and handles the response
    /// </summary>
    private async Task<TopicDiscoveryOpenAiResponse?> CallOpenAiApiAsync(TopicDiscoveryOpenAiRequest request)
    {
        try
        {
            var jsonRequest = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(OpenAiApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"OpenAI API request failed: {response.StatusCode} - {errorContent}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TopicDiscoveryOpenAiResponse>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calling OpenAI API: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parses the OpenAI response and extracts topic information
    /// </summary>
    private TopicDiscoveryResult ParseOpenAiResponse(TopicDiscoveryOpenAiResponse response, string videoId, string videoTitle)
    {
        try
        {
            if (response.Choices == null || !response.Choices.Any())
            {
                return CreateFailedResult("No choices returned from OpenAI API", videoId, videoTitle);
            }

            var messageContent = response.Choices.First().Message.Content;

            if (string.IsNullOrWhiteSpace(messageContent))
            {
                return CreateFailedResult("Empty response content from OpenAI API", videoId, videoTitle);
            }

            // Parse the JSON response - expecting a JSON object with a "topics" array
            var responseData = JsonConvert.DeserializeObject<TopicDiscoveryResponseData>(messageContent);

            if (responseData?.Topics == null || !responseData.Topics.Any())
            {
                return CreateFailedResult("No topics found in OpenAI response", videoId, videoTitle);
            }

            // Convert to our domain models and validate
            var topics = new List<DiscoveredTopic>();
            foreach (var topic in responseData.Topics)
            {
                if (ValidateTopicData(topic))
                {
                    topics.Add(new DiscoveredTopic
                    {
                        StartTime = ParseTimespan(topic.StartTime),
                        Title = TruncateString(topic.Title, 200),
                        TopicSummary = TruncateString(topic.Summary, 1000),
                        Content = topic.Content ?? string.Empty,
                        BlueprintElements = topic.BlueprintElements != null
                            ? JsonConvert.SerializeObject(topic.BlueprintElements)
                            : string.Empty
                    });
                }
                else
                {
                    _logger.LogWarning($"Invalid topic data found, skipping: {topic?.Title}");
                }
            }

            if (!topics.Any())
            {
                return CreateFailedResult("No valid topics could be parsed from response", videoId, videoTitle);
            }

            return new TopicDiscoveryResult
            {
                Success = true,
                VideoId = videoId,
                VideoTitle = videoTitle,
                Topics = topics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error parsing OpenAI response: {ex.Message}");
            return CreateFailedResult($"Failed to parse response: {ex.Message}", videoId, videoTitle);
        }
    }

    /// <summary>
    /// Gets the optimized topic discovery prompt for GPT-4o-mini
    /// </summary>
    private string GetTopicDiscoveryPrompt()
    {
        return @"You are a content strategist specializing in breaking down educational videos into structured learning modules.

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
    }

    /// <summary>
    /// Processes transcript for API consumption with 150k token limit
    /// </summary>
    private string ProcessTranscriptForApi(string transcript)
    {
        // Target: 150,000 tokens maximum
        // Rough conversion: 1 token ≈ 4 characters for English text
        const int maxCharacters = 600000; // ~150k tokens worth of input

        if (transcript.Length <= maxCharacters)
        {
            _logger.LogInformation($"Processing transcript: {transcript.Length:N0} characters (~{transcript.Length / 4:N0} tokens)");
            return transcript;
        }

        _logger.LogInformation($"Transcript exceeds limit ({transcript.Length:N0} characters), truncating to {maxCharacters:N0} characters (~150k tokens)");

        // Truncate at logical boundaries to preserve content quality
        var truncated = transcript.Substring(0, maxCharacters);
        var lastPeriod = truncated.LastIndexOf(". ");
        var lastNewline = truncated.LastIndexOf("\n");

        var bestBreakpoint = Math.Max(lastPeriod, lastNewline);

        if (bestBreakpoint > maxCharacters * 0.85) // Use boundary if it's reasonable (within 15% of limit)
        {
            var result = truncated.Substring(0, bestBreakpoint + 1);
            _logger.LogInformation($"Truncated at natural boundary: {result.Length:N0} characters");
            return result;
        }

        _logger.LogInformation($"Truncated at character limit: {maxCharacters:N0} characters");
        return truncated;
    }

    /// <summary>
    /// Validates topic data from API response
    /// </summary>
    private bool ValidateTopicData(TopicResponseItem topic)
    {
        return topic != null &&
               !string.IsNullOrWhiteSpace(topic.Title) &&
               !string.IsNullOrWhiteSpace(topic.Summary) &&
               !string.IsNullOrWhiteSpace(topic.StartTime);
    }

    /// <summary>
    /// Parses timespan from various string formats (HH:MM:SS, MM:SS, etc.)
    /// </summary>
    private TimeSpan ParseTimespan(string timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString))
            return TimeSpan.Zero;

        try
        {
            // Handle various time formats
            var timePatterns = new[]
            {
                @"^(\d{1,2}):(\d{2}):(\d{2})$", // HH:MM:SS
                @"^(\d{1,2}):(\d{2})$",         // MM:SS
                @"^(\d+)$"                      // Seconds only
            };

            foreach (var pattern in timePatterns)
            {
                var match = Regex.Match(timeString, pattern);
                if (match.Success)
                {
                    if (match.Groups.Count == 4) // HH:MM:SS
                    {
                        return new TimeSpan(
                            int.Parse(match.Groups[1].Value),
                            int.Parse(match.Groups[2].Value),
                            int.Parse(match.Groups[3].Value));
                    }
                    else if (match.Groups.Count == 3) // MM:SS
                    {
                        return new TimeSpan(0,
                            int.Parse(match.Groups[1].Value),
                            int.Parse(match.Groups[2].Value));
                    }
                    else if (match.Groups.Count == 2) // Seconds only
                    {
                        return TimeSpan.FromSeconds(int.Parse(match.Groups[1].Value));
                    }
                }
            }

            // Fallback: try direct TimeSpan parsing
            if (TimeSpan.TryParse(timeString, out var result))
                return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to parse timespan '{timeString}': {ex.Message}");
        }

        return TimeSpan.Zero;
    }

    /// <summary>
    /// Truncates string to specified length while preserving word boundaries
    /// </summary>
    private string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input ?? string.Empty;

        // Find the last space before the max length to avoid cutting words
        var truncated = input.Substring(0, maxLength);
        var lastSpace = truncated.LastIndexOf(' ');

        if (lastSpace > 0 && lastSpace > maxLength * 0.8) // Only use word boundary if it's not too far back
            return truncated.Substring(0, lastSpace) + "...";

        return truncated + "...";
    }

    /// <summary>
    /// Creates a failed TopicDiscoveryResult with error information
    /// </summary>
    private TopicDiscoveryResult CreateFailedResult(string errorMessage, string videoId, string videoTitle)
    {
        return new TopicDiscoveryResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            VideoId = videoId,
            VideoTitle = videoTitle,
            Topics = new List<DiscoveredTopic>()
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}