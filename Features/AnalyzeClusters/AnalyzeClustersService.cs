using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using VideoScripts.Data.Entities;
using VideoScripts.Features.AnalyzeClusters.Models;

namespace VideoScripts.Features.AnalyzeClusters;

public class AnalyzeClustersService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnalyzeClustersService> _logger;
    private readonly string _apiKey;
    private const string OpenAiApiUrl = "https://api.openai.com/v1/chat/completions";

    public AnalyzeClustersService(IConfiguration configuration, ILogger<AnalyzeClustersService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey configuration is missing");

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Performs comprehensive analysis on a cluster including readiness, density, and structural elements
    /// </summary>
    /// <param name="cluster">Topic cluster to analyze</param>
    /// <param name="projectName">Project name for context</param>
    /// <returns>Complete cluster analysis result</returns>
    public async Task<ClusterAnalysisResult> AnalyzeClusterAsync(TopicClusterEntity cluster, string projectName)
    {
        var result = new ClusterAnalysisResult
        {
            ClusterId = cluster.Id,
            ClusterName = cluster.ClusterName,
            ProjectName = projectName
        };

        try
        {
            _logger.LogInformation($"Starting comprehensive analysis for cluster: {cluster.ClusterName}");

            // Build cluster data for analysis
            var clusterData = BuildClusterDataForAnalysis(cluster);

            // Run all three analyses
            var readinessTask = PerformReadinessAnalysisAsync(clusterData);
            var densityTask = PerformDensityAnalysisAsync(clusterData);
            var structuralTask = PerformStructuralAnalysisAsync(clusterData);

            // Wait for all analyses to complete
            await Task.WhenAll(readinessTask, densityTask, structuralTask);

            // Collect results
            result.ReadinessAnalysis = await readinessTask;
            result.DensityAnalysis = await densityTask;
            result.StructuralAnalysis = await structuralTask;

            // Determine overall success
            result.Success = result.ReadinessAnalysis != null ||
                           result.DensityAnalysis != null ||
                           result.StructuralAnalysis != null;

            if (!result.Success)
            {
                result.ErrorMessage = "All analysis types failed to complete";
            }

            _logger.LogInformation($"Completed analysis for cluster: {cluster.ClusterName}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing cluster {cluster.ClusterName}: {ex.Message}");
            result.Success = false;
            result.ErrorMessage = $"Analysis failed: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Performs cluster readiness analysis
    /// </summary>
    private async Task<ClusterReadinessAnalysis?> PerformReadinessAnalysisAsync(string clusterData)
    {
        var response = string.Empty;

        try
        {
            var prompt = Prompts.ClusterReadiness().Replace("[INSERT CLUSTER DATA HERE]", clusterData);
            response = await CallOpenAiApiAsync(prompt, "readiness");

            if (response != null)
            {
                var analysis = JsonConvert.DeserializeObject<ClusterReadinessAnalysis>(response);

                // Validate the analysis has required fields
                if (analysis != null && ValidateReadinessAnalysis(analysis))
                {
                    return analysis;
                }
                else
                {
                    _logger.LogWarning("Readiness analysis validation failed - missing required fields");
                }
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Failed to parse readiness analysis JSON: {Response}", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in readiness analysis: {Message}", ex.Message);
        }

        return null;
    }

    /// <summary>
    /// Performs content density analysis
    /// </summary>
    private async Task<ContentDensityAnalysis?> PerformDensityAnalysisAsync(string clusterData)
    {
        var response = string.Empty;

        try
        {
            var prompt = Prompts.ContentDensity().Replace("[INSERT CLUSTER DATA HERE]", clusterData);
            response = await CallOpenAiApiAsync(prompt, "density");

            if (response != null)
            {
                var analysis = JsonConvert.DeserializeObject<ContentDensityAnalysis>(response);

                // Validate the analysis has required fields
                if (analysis != null && ValidateDensityAnalysis(analysis))
                {
                    return analysis;
                }
                else
                {
                    _logger.LogWarning("Density analysis validation failed - missing required fields");
                }
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Failed to parse density analysis JSON: {Response}", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in density analysis: {Message}", ex.Message);
        }

        return null;
    }

    /// <summary>
    /// Performs structural elements analysis
    /// </summary>
    private async Task<StructuralElementsAnalysis?> PerformStructuralAnalysisAsync(string clusterData)
    {
        var response = string.Empty;

        try
        {
            var prompt = Prompts.StructuralElements().Replace("[INSERT CLUSTER DATA HERE]", clusterData);
            response = await CallOpenAiApiAsync(prompt, "structural");

            if (response != null)
            {
                var analysis = JsonConvert.DeserializeObject<StructuralElementsAnalysis>(response);

                // Validate the analysis has required fields
                if (analysis != null && ValidateStructuralAnalysis(analysis))
                {
                    return analysis;
                }
                else
                {
                    _logger.LogWarning("Structural analysis validation failed - missing required fields");
                }
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Failed to parse structural analysis JSON: {Response}", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in structural analysis: {Message}", ex.Message);
        }

        return null;
    }

    /// <summary>
    /// Builds formatted cluster data string for AI analysis
    /// </summary>
    private string BuildClusterDataForAnalysis(TopicClusterEntity cluster)
    {
        var dataBuilder = new StringBuilder();

        dataBuilder.AppendLine($"CLUSTER: {cluster.ClusterName}");
        dataBuilder.AppendLine($"DISPLAY ORDER: {cluster.DisplayOrder}");
        dataBuilder.AppendLine($"TOTAL TOPICS: {cluster.TopicAssignments.Count}");
        dataBuilder.AppendLine();

        // Add each topic with its details
        foreach (var assignment in cluster.TopicAssignments.OrderBy(ta => ta.TranscriptTopic.StartTime))
        {
            var topic = assignment.TranscriptTopic;

            dataBuilder.AppendLine($"TOPIC: {topic.Title}");
            dataBuilder.AppendLine($"START TIME: {FormatTimeSpan(topic.StartTime)}");
            dataBuilder.AppendLine($"VIDEO: {topic.Video?.Title ?? "Unknown"}");
            dataBuilder.AppendLine($"SUMMARY: {topic.TopicSummary}");

            if (!string.IsNullOrWhiteSpace(topic.Content))
            {
                dataBuilder.AppendLine($"CONTENT: {topic.Content}");
            }

            if (!string.IsNullOrWhiteSpace(topic.BluePrintElements))
            {
                dataBuilder.AppendLine($"BLUEPRINT ELEMENTS: {topic.BluePrintElements}");
            }

            dataBuilder.AppendLine();
            dataBuilder.AppendLine("---");
            dataBuilder.AppendLine();
        }

        return dataBuilder.ToString();
    }

    /// <summary>
    /// Calls OpenAI API with the specified prompt
    /// </summary>
    private async Task<string?> CallOpenAiApiAsync(string prompt, string analysisType)
    {
        try
        {
            var request = new AnalyzeClustersOpenAiRequest
            {
                Model = "gpt-4o-mini",
                Messages = new List<AnalyzeClustersOpenAiMessage>
                {
                    new AnalyzeClustersOpenAiMessage
                    {
                        Role = "user",
                        Content = prompt
                    }
                },
                MaxTokens = 4000, // Increased for more detailed JSON responses
                Temperature = 0.1, // Lower temperature for more consistent JSON structure
                ResponseFormat = new AnalyzeClustersOpenAiResponseFormat { Type = "json_object" }
            };

            var jsonRequest = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(OpenAiApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"OpenAI API request failed for {analysisType}: {response.StatusCode} - {errorContent}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<AnalyzeClustersOpenAiResponse>(responseContent);

            if (apiResponse?.Choices?.Any() == true)
            {
                var messageContent = apiResponse.Choices.First().Message.Content;

                // Clean up the response to ensure it's valid JSON
                var cleanedContent = CleanJsonResponse(messageContent);

                // Validate it's proper JSON before returning
                if (IsValidJson(cleanedContent))
                {
                    return cleanedContent;
                }
                else
                {
                    _logger.LogWarning($"Invalid JSON response from OpenAI for {analysisType}: {messageContent}");
                    return null;
                }
            }

            _logger.LogWarning($"No valid response from OpenAI API for {analysisType}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calling OpenAI API for {analysisType}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Cleans up JSON response from OpenAI to ensure it's parseable
    /// </summary>
    private string CleanJsonResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return response;

        // Remove any markdown formatting if present
        response = response.Trim();

        // Remove ```json and ``` if present
        if (response.StartsWith("```json"))
        {
            response = response.Substring(7);
        }
        if (response.StartsWith("```"))
        {
            response = response.Substring(3);
        }
        if (response.EndsWith("```"))
        {
            response = response.Substring(0, response.Length - 3);
        }

        return response.Trim();
    }

    /// <summary>
    /// Validates if a string is valid JSON
    /// </summary>
    private bool IsValidJson(string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            return false;

        try
        {
            JsonConvert.DeserializeObject(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates readiness analysis has required fields
    /// </summary>
    private bool ValidateReadinessAnalysis(ClusterReadinessAnalysis analysis)
    {
        return analysis.OverallReadinessScore > 0 &&
               analysis.NarrativeCompletenessScore > 0 &&
               analysis.StructuralCoherenceScore > 0 &&
               !string.IsNullOrWhiteSpace(analysis.ClusterType);
    }

    /// <summary>
    /// Validates density analysis has required fields
    /// </summary>
    private bool ValidateDensityAnalysis(ContentDensityAnalysis analysis)
    {
        return !string.IsNullOrWhiteSpace(analysis.OverallDensity) &&
               !string.IsNullOrWhiteSpace(analysis.CognitiveLoad) &&
               !string.IsNullOrWhiteSpace(analysis.DepthBreadthRatio);
    }

    /// <summary>
    /// Validates structural analysis has required fields
    /// </summary>
    private bool ValidateStructuralAnalysis(StructuralElementsAnalysis analysis)
    {
        return analysis.TotalStructuralElements >= 0 &&
               !string.IsNullOrWhiteSpace(analysis.PrimaryAnchorElement);
    }

    /// <summary>
    /// Formats TimeSpan to readable string
    /// </summary>
    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        else
            return $"{timeSpan.Minutes}:{timeSpan.Seconds:D2}";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}