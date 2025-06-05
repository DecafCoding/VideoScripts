using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using VideoScripts.Features.ClusterTopics.Models;
using VideoScripts.Data.Entities;

namespace VideoScripts.Features.ClusterTopics;

public class ClusterTopicsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClusterTopicsService> _logger;
    private readonly string _apiKey;
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";

    public ClusterTopicsService(IConfiguration configuration, ILogger<ClusterTopicsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey configuration is missing");

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Analyzes topics and groups them into logical clusters using OpenAI
    /// </summary>
    /// <param name="topics">List of transcript topics to analyze</param>
    /// <param name="projectName">Project name for context</param>
    /// <returns>Clustering result with organized topic groups</returns>
    public async Task<ClusterTopicsResult> AnalyzeAndClusterTopicsAsync(List<TranscriptTopicEntity> topics, string projectName)
    {
        try
        {
            if (!topics?.Any() == true)
            {
                _logger.LogWarning($"Empty topic list provided for project {projectName}");
                return CreateFailedResult("No topics provided for clustering", projectName);
            }

            _logger.LogInformation($"Analyzing {topics.Count} topics for clustering in project: {projectName} using model: {Prompts.ClusterTopics.ModelConfig.Model}");

            // Create the clustering request using centralized configuration
            var request = CreateClusteringRequest(topics, projectName);

            // Send request to OpenAI
            var response = await CallOpenAiApiAsync(request);

            if (response == null)
            {
                return CreateFailedResult("Failed to get response from OpenAI API", projectName);
            }

            // Parse and validate the response
            var clusterResult = ParseOpenAiResponse(response, topics, projectName);

            if (clusterResult.Success)
            {
                _logger.LogInformation($"Successfully clustered topics for project: {projectName} - Created {clusterResult.Clusters.Count} clusters (Model: {Prompts.ClusterTopics.ModelConfig.Model})");
            }
            else
            {
                _logger.LogWarning($"Failed to cluster topics for project: {projectName} - {clusterResult.ErrorMessage}");
            }

            return clusterResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error clustering topics for project {projectName}: {ex.Message}");
            return CreateFailedResult($"Clustering failed: {ex.Message}", projectName);
        }
    }

    /// <summary>
    /// Creates the OpenAI API request using centralized prompt and model configuration
    /// </summary>
    private ClusterTopicsOpenAiRequest CreateClusteringRequest(List<TranscriptTopicEntity> topics, string projectName)
    {
        // Get the formatted prompt using the centralized configuration
        var userPrompt = Prompts.ClusterTopics.GetFormattedPrompt(projectName, topics);

        return new ClusterTopicsOpenAiRequest
        {
            Model = Prompts.ClusterTopics.ModelConfig.Model,
            Messages = new List<ClusterTopicsOpenAiMessage>
            {
                new ClusterTopicsOpenAiMessage
                {
                    Role = "system",
                    Content = Prompts.ClusterTopics.ModelConfig.SystemMessage
                },
                new ClusterTopicsOpenAiMessage
                {
                    Role = "user",
                    Content = userPrompt
                }
            },
            MaxTokens = Prompts.ClusterTopics.ModelConfig.MaxTokens,
            Temperature = Prompts.ClusterTopics.ModelConfig.Temperature,
            ResponseFormat = new ClusterTopicsOpenAiResponseFormat { Type = "json_object" }
        };
    }

    /// <summary>
    /// Sends the request to OpenAI API and handles the response
    /// </summary>
    private async Task<ClusterTopicsOpenAiResponse?> CallOpenAiApiAsync(ClusterTopicsOpenAiRequest request)
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
            return JsonConvert.DeserializeObject<ClusterTopicsOpenAiResponse>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calling OpenAI API: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parses the OpenAI response and creates cluster assignments
    /// </summary>
    private ClusterTopicsResult ParseOpenAiResponse(ClusterTopicsOpenAiResponse response, List<TranscriptTopicEntity> originalTopics, string projectName)
    {
        try
        {
            if (response.Choices == null || !response.Choices.Any())
            {
                return CreateFailedResult("No choices returned from OpenAI API", projectName);
            }

            var messageContent = response.Choices.First().Message.Content;

            if (string.IsNullOrWhiteSpace(messageContent))
            {
                return CreateFailedResult("Empty response content from OpenAI API", projectName);
            }

            // Parse the JSON response
            var responseData = JsonConvert.DeserializeObject<ClusteringResponseData>(messageContent);

            if (responseData?.Clusters == null || !responseData.Clusters.Any())
            {
                return CreateFailedResult("No clusters found in OpenAI response", projectName);
            }

            // Convert to our domain models
            var clusters = new List<TopicCluster>();
            foreach (var clusterData in responseData.Clusters)
            {
                if (string.IsNullOrWhiteSpace(clusterData.ClusterName))
                    continue;

                var cluster = new TopicCluster
                {
                    ClusterName = TruncateString(clusterData.ClusterName, 200),
                    ClusterDescription = clusterData.ClusterDescription ?? string.Empty,
                    DisplayOrder = clusterData.DisplayOrder,
                    Topics = new List<TopicAssignment>()
                };

                // Map topics to this cluster
                foreach (var topicRef in clusterData.Topics ?? new List<ClusterTopicItem>())
                {
                    if (topicRef.TopicIndex >= 0 && topicRef.TopicIndex < originalTopics.Count)
                    {
                        var originalTopic = originalTopics[topicRef.TopicIndex];
                        cluster.Topics.Add(new TopicAssignment
                        {
                            TopicId = originalTopic.Id,
                            TopicTitle = originalTopic.Title,
                            TopicSummary = originalTopic.TopicSummary,
                            AssignmentReason = topicRef.AssignmentReason ?? string.Empty
                        });
                    }
                }

                clusters.Add(cluster);
            }

            if (!clusters.Any())
            {
                return CreateFailedResult("No valid clusters could be created from response", projectName);
            }

            return new ClusterTopicsResult
            {
                Success = true,
                ProjectName = projectName,
                Clusters = clusters
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error parsing OpenAI response: {ex.Message}");
            return CreateFailedResult($"Failed to parse response: {ex.Message}", projectName);
        }
    }

    /// <summary>
    /// Truncates string to specified length while preserving word boundaries
    /// </summary>
    private string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input ?? string.Empty;

        var truncated = input.Substring(0, maxLength);
        var lastSpace = truncated.LastIndexOf(' ');

        if (lastSpace > 0 && lastSpace > maxLength * 0.8)
            return truncated.Substring(0, lastSpace) + "...";

        return truncated + "...";
    }

    /// <summary>
    /// Creates a failed ClusterTopicsResult with error information
    /// </summary>
    private ClusterTopicsResult CreateFailedResult(string errorMessage, string projectName)
    {
        return new ClusterTopicsResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ProjectName = projectName,
            Clusters = new List<TopicCluster>()
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}