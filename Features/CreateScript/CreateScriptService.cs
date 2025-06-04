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
    /// <returns>Generated script content and token usage information</returns>
    public async Task<(string content, OpenAIUsage usage)> CreateScriptFromTranscriptsAsync(string projectTopic, List<(string title, string transcript)> videoData)
    {
        try
        {
            _logger.LogInformation($"Creating script for project topic: {projectTopic} with {videoData.Count} transcripts using model: {Prompts.CreateScript.ModelConfig.Model}");

            // Build the prompt using the centralized configuration
            var prompt = Prompts.CreateScript.GetFormattedPrompt(projectTopic, videoData);

            // Create OpenAI request using centralized model configuration
            var request = new OpenAIRequest
            {
                Model = Prompts.CreateScript.ModelConfig.Model,
                MaxTokens = Prompts.CreateScript.ModelConfig.MaxTokens,
                Temperature = Prompts.CreateScript.ModelConfig.Temperature,
                Messages = new List<OpenAIMessage>
                {
                    new OpenAIMessage
                    {
                        Role = "system",
                        Content = Prompts.CreateScript.ModelConfig.SystemMessage
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

            // Extract token usage information
            var usage = openAIResponse.Usage ?? new OpenAIUsage();

            _logger.LogInformation($"Successfully generated script with {generatedScript.Length} characters using {usage.TotalTokens} tokens (Model: {Prompts.CreateScript.ModelConfig.Model})");

            return (generatedScript, usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating script: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}