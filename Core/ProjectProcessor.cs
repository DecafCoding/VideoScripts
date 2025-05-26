using VideoScripts.Features.RetrieveTranscript;
using VideoScripts.Features.TranscriptSummary;
using VideoScripts.Features.TopicDiscovery;
using VideoScripts.Features.ClusterTopics;

namespace VideoScripts.Core;

/// <summary>
/// Handles processing of individual projects through the complete pipeline
/// </summary>
public static class ProjectProcessor
{
    /// <summary>
    /// Processes the complete pipeline for a project: transcripts, topic discovery, then summaries
    /// </summary>
    public static async Task ProcessFullProjectPipelineAsync(
        TranscriptProcessingHandler transcriptHandler,
        TopicDiscoveryHandler topicDiscoveryHandler,
        ClusterTopicsHandler clusterTopicsHandler,
        TranscriptSummaryHandler summaryHandler,
        string projectName)
    {
        ConsoleOutput.DisplaySectionHeader($"PROCESSING PIPELINE FOR PROJECT: {projectName}");

        // Step 1: Process transcripts
        await ProcessProjectTranscriptsAsync(transcriptHandler, projectName);

        // Step 2: Process topic discovery (after transcripts are complete)
        await ProcessProjectTopicDiscoveryAsync(topicDiscoveryHandler, projectName);

        // Step 3: Process summaries (after transcripts are complete)
        await ProcessProjectSummariesAsync(summaryHandler, projectName);

        // Step 4: Process topic clustering (after topic discovery is complete)
        await ProcessProjectClusteringAsync(clusterTopicsHandler, projectName);
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
    /// Processes topic discovery for a specific project
    /// </summary>
    private static async Task ProcessProjectTopicDiscoveryAsync(TopicDiscoveryHandler topicDiscoveryHandler, string projectName)
    {
        ConsoleOutput.DisplaySubsectionHeader($"TOPIC DISCOVERY PROCESSING: {projectName}");

        try
        {
            // Get current status
            var status = await topicDiscoveryHandler.GetProjectTopicStatusAsync(projectName);

            if (!status.ProjectExists)
            {
                Console.WriteLine($"Project '{projectName}' not found");
                return;
            }

            Console.WriteLine($"  Topic Discovery Status:");
            Console.WriteLine($"   - Total Videos: {status.TotalVideos}");
            Console.WriteLine($"   - With Transcripts: {status.VideosWithTranscripts}");
            Console.WriteLine($"   - With Topics: {status.VideosWithTopics}");
            Console.WriteLine($"   - Needing Topics: {status.VideosNeedingTopics}");
            Console.WriteLine($"   - Total Topics: {status.TotalTopicsCount}");

            if (status.IsComplete)
            {
                Console.WriteLine($"All videos with transcripts already have topics");
                return;
            }

            if (status.VideosNeedingTopics == 0)
            {
                Console.WriteLine($"No videos with transcripts found for topic discovery");
                return;
            }

            // Process topic discovery
            var result = await topicDiscoveryHandler.ProcessProjectTopicsAsync(projectName);

            if (result.Success)
            {
                Console.WriteLine($"  Topic discovery processing completed:");
                Console.WriteLine($"   - Successful: {result.SuccessfulCount}");
                Console.WriteLine($"   - Failed: {result.FailedCount}");

                // Show details for each video
                foreach (var topicInfo in result.ProcessedTopics)
                {
                    var statusIcon = topicInfo.Success ? "✅" : "❌";
                    var topicCountInfo = topicInfo.Success ? $"({topicInfo.TopicCount} topics)" : "";
                    Console.WriteLine($"   {statusIcon} {topicInfo.Title} {topicCountInfo} - {topicInfo.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Topic discovery processing failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing topic discovery: {ex.Message}");
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

    /// <summary>
    /// Processes topic clustering for a specific project
    /// </summary>
    private static async Task ProcessProjectClusteringAsync(ClusterTopicsHandler clusterTopicsHandler, string projectName)
    {
        ConsoleOutput.DisplaySubsectionHeader($"TOPIC CLUSTERING PROCESSING: {projectName}");

        try
        {
            // Get current status
            var status = await clusterTopicsHandler.GetProjectClusteringStatusAsync(projectName);

            if (!status.ProjectExists)
            {
                Console.WriteLine($"Project '{projectName}' not found");
                return;
            }

            Console.WriteLine($"  Topic Clustering Status:");
            Console.WriteLine($"   - Total Topics: {status.TotalTopics}");
            Console.WriteLine($"   - Clustered Topics: {status.ClusteredTopics}");
            Console.WriteLine($"   - Unclustered Topics: {status.UnclusteredTopics}");
            Console.WriteLine($"   - Total Clusters: {status.TotalClusters}");

            if (status.IsComplete)
            {
                Console.WriteLine($"All topics are already clustered");
                return;
            }

            if (status.UnclusteredTopics == 0)
            {
                Console.WriteLine($"No topics found for clustering");
                return;
            }

            // Process topic clustering
            var result = await clusterTopicsHandler.ProcessProjectClusteringAsync(projectName);

            if (result.Success)
            {
                Console.WriteLine($"  Topic clustering processing completed:");
                Console.WriteLine($"   - Successful: {result.SuccessfulCount}");
                Console.WriteLine($"   - Failed: {result.FailedCount}");

                // Show details for each cluster
                foreach (var cluster in result.ProcessedClusters)
                {
                    var statusIcon = cluster.Success ? "✅" : "❌";
                    Console.WriteLine($"   {statusIcon} {cluster.ClusterName} ({cluster.TopicCount} topics)");
                    Console.WriteLine($"       Order: {cluster.DisplayOrder} - {cluster.Message}");
                    if (!string.IsNullOrWhiteSpace(cluster.ClusterDescription))
                    {
                        Console.WriteLine($"       Description: {cluster.ClusterDescription}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Topic clustering processing failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing topic clustering: {ex.Message}");
        }
    }
}