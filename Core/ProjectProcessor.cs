using VideoScripts.Features.RetrieveTranscript;
using VideoScripts.Features.TranscriptSummary;

namespace VideoScripts.Core;

/// <summary>
/// Handles processing of individual projects through the complete pipeline
/// </summary>
public static class ProjectProcessor
{
    /// <summary>
    /// Processes the complete pipeline for a project: transcripts then summaries
    /// </summary>
    public static async Task ProcessFullProjectPipelineAsync(
        TranscriptProcessingHandler transcriptHandler,
        TranscriptSummaryHandler summaryHandler,
        string projectName)
    {
        ConsoleOutput.DisplaySectionHeader($"PROCESSING PIPELINE FOR PROJECT: {projectName}");

        // Step 1: Process transcripts
        await ProcessProjectTranscriptsAsync(transcriptHandler, projectName);

        // Step 2: Process summaries (only after transcripts are complete)
        await ProcessProjectSummariesAsync(summaryHandler, projectName);
    }

    /// <summary>
    /// Processes transcripts for a specific project
    /// </summary>
    private static async Task ProcessProjectTranscriptsAsync(TranscriptProcessingHandler transcriptHandler, string projectName)
    {
        ConsoleOutput.DisplaySubsectionHeader($"TRANSCRIPT PROCESSING: {projectName}");

        try
        {
            // Get current status
            var status = await transcriptHandler.GetProjectTranscriptStatusAsync(projectName);

            if (!status.ProjectExists)
            {
                Console.WriteLine($"Project '{projectName}' not found");
                return;
            }

            Console.WriteLine($"  Transcript Status:");
            Console.WriteLine($"   - Total Videos: {status.TotalVideos}");
            Console.WriteLine($"   - With Transcripts: {status.VideosWithTranscripts}");
            Console.WriteLine($"   - Without Transcripts: {status.VideosWithoutTranscripts}");

            if (status.IsComplete)
            {
                Console.WriteLine($"All videos already have transcripts");
                return;
            }

            // Process transcripts
            var result = await transcriptHandler.ProcessProjectTranscriptsAsync(projectName);

            if (result.Success)
            {
                Console.WriteLine($"  Transcript processing completed:");
                Console.WriteLine($"   - Successful: {result.SuccessfulCount}");
                Console.WriteLine($"   - Failed: {result.FailedCount}");

                // Show details for each video
                foreach (var transcript in result.ProcessedTranscripts)
                {
                    var statusIcon = transcript.Success ? "✅" : "❌";
                    var lengthInfo = transcript.Success ? $"({transcript.TranscriptLength} chars)" : "";
                    Console.WriteLine($"   {statusIcon} {transcript.Title} {lengthInfo} - {transcript.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Transcript processing failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing transcripts: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes AI summaries for a specific project
    /// </summary>
    private static async Task ProcessProjectSummariesAsync(TranscriptSummaryHandler summaryHandler, string projectName)
    {
        ConsoleOutput.DisplaySubsectionHeader($"AI SUMMARY PROCESSING: {projectName}");

        try
        {
            // Get current status
            var status = await summaryHandler.GetProjectSummaryStatusAsync(projectName);

            if (!status.ProjectExists)
            {
                Console.WriteLine($"Project '{projectName}' not found");
                return;
            }

            Console.WriteLine($" Summary Status:");
            Console.WriteLine($"  - Total Videos: {status.TotalVideos}");
            Console.WriteLine($"  - With Transcripts: {status.VideosWithTranscripts}");
            Console.WriteLine($"  - With Summaries: {status.VideosWithSummaries}");
            Console.WriteLine($"  - Needing Summaries: {status.VideosNeedingSummaries}");

            if (status.IsComplete)
            {
                Console.WriteLine($"All videos with transcripts already have summaries");
                return;
            }

            if (status.VideosNeedingSummaries == 0)
            {
                Console.WriteLine($"No videos with transcripts found to summarize");
                return;
            }

            // Process summaries
            var result = await summaryHandler.ProcessProjectSummariesAsync(projectName);

            if (result.Success)
            {
                Console.WriteLine($" Summary processing completed:");
                Console.WriteLine($"  - Successful: {result.SuccessfulCount}");
                Console.WriteLine($"  - Failed: {result.FailedCount}");

                // Show details for each video
                foreach (var summary in result.ProcessedSummaries)
                {
                    var statusIcon = summary.Success ? "Success: " : "Fail: ";
                    var topicInfo = summary.Success && !string.IsNullOrWhiteSpace(summary.VideoTopic)
                        ? $"Topic: {summary.VideoTopic.Substring(0, Math.Min(50, summary.VideoTopic.Length))}..."
                        : "";
                    var lengthInfo = summary.Success ? $"({summary.SummaryLength} chars)" : "";
                    Console.WriteLine($"   {statusIcon} {summary.Title} {lengthInfo}");
                    if (!string.IsNullOrWhiteSpace(topicInfo))
                    {
                        Console.WriteLine($"       {topicInfo}");
                    }
                    Console.WriteLine($"       {summary.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Summary processing failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing summaries: {ex.Message}");
        }
    }
}