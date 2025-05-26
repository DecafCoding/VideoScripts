using VideoScripts.Core;
using VideoScripts.Features.ShowClusters.Models;

namespace VideoScripts.Features.ShowClusters;

/// <summary>
/// Service for displaying cluster information in a formatted way
/// </summary>
public class ShowClustersService
{
    private readonly ShowClustersHandler _handler;

    public ShowClustersService(ShowClustersHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Displays the main cluster selection menu
    /// </summary>
    public async Task DisplayClusterMenuAsync()
    {
        ConsoleOutput.DisplaySectionHeader("CLUSTER VIEWER");

        // Get projects with clusters
        var projects = await _handler.GetProjectsWithClustersAsync();

        if (!projects.Any())
        {
            ConsoleOutput.DisplayInfo("No projects with clusters found.");

            // Show all projects and their status
            await DisplayAllProjectsStatusAsync();
            return;
        }

        // Display available projects
        ConsoleOutput.DisplayInfo($"Found {projects.Count} project(s) with clusters:");
        Console.WriteLine();

        for (int i = 0; i < projects.Count; i++)
        {
            var project = projects[i];
            Console.WriteLine($"{i + 1}. {project.ProjectName}");
            Console.WriteLine($"   Topic: {project.ProjectTopic}");
            Console.WriteLine($"   Clusters: {project.ClusterCount} | Topics: {project.TotalTopics}");
            Console.WriteLine();
        }

        // Get user selection
        var selection = GetProjectSelection(projects.Count);
        if (selection.HasValue)
        {
            var selectedProject = projects[selection.Value - 1];
            await DisplayProjectClustersAsync(selectedProject.ProjectName);
        }
    }

    /// <summary>
    /// Displays detailed cluster information for a specific project
    /// </summary>
    private async Task DisplayProjectClustersAsync(string projectName)
    {
        ConsoleOutput.DisplaySectionHeader($"CLUSTERS FOR PROJECT: {projectName.ToUpper()}");

        var projectDetails = await _handler.GetProjectClusterDetailsAsync(projectName);

        if (projectDetails == null)
        {
            ConsoleOutput.DisplayError($"Could not retrieve cluster details for project '{projectName}'");
            return;
        }

        if (!projectDetails.Clusters.Any())
        {
            ConsoleOutput.DisplayInfo("This project has no clusters yet.");
            return;
        }

        Console.WriteLine($"Project Topic: {projectDetails.ProjectTopic}");
        Console.WriteLine($"Total Clusters: {projectDetails.Clusters.Count}");
        Console.WriteLine($"Total Topics: {projectDetails.Clusters.Sum(c => c.TopicCount)}");
        Console.WriteLine();

        // Display each cluster
        foreach (var cluster in projectDetails.Clusters)
        {
            DisplayClusterDetails(cluster);
        }

        // Wait for user input before returning
        Console.WriteLine();
        ConsoleOutput.GetUserInput("Press Enter to return to menu...");
    }

    /// <summary>
    /// Displays details for a single cluster
    /// </summary>
    private void DisplayClusterDetails(ClusterDetails cluster)
    {
        ConsoleOutput.DisplaySubsectionHeader($"CLUSTER {cluster.DisplayOrder}: {cluster.ClusterName}");
        Console.WriteLine($"Topics in this cluster: {cluster.TopicCount}");
        Console.WriteLine();

        foreach (var topic in cluster.Topics)
        {
            Console.WriteLine($"📝 {topic.TopicTitle}");
            Console.WriteLine($"   ⏱️  {FormatTimeSpan(topic.StartTime)} | Video: {topic.VideoTitle}");

            if (!string.IsNullOrWhiteSpace(topic.TopicSummary))
            {
                Console.WriteLine($"   📋 {topic.TopicSummary}");
            }

            if (topic.HasBlueprintElements && topic.BlueprintElementsCount > 0)
            {
                Console.WriteLine($"   🔧 Blueprint Elements: {topic.BlueprintElementsCount}");
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Displays status of all projects for reference
    /// </summary>
    private async Task DisplayAllProjectsStatusAsync()
    {
        ConsoleOutput.DisplaySubsectionHeader("ALL PROJECTS STATUS");

        var allProjects = await _handler.GetAllProjectsSummaryAsync();

        if (!allProjects.Any())
        {
            ConsoleOutput.DisplayInfo("No projects found in database.");
            return;
        }

        Console.WriteLine();
        foreach (var project in allProjects)
        {
            var status = GetProjectStatusIcon(project);
            Console.WriteLine($"{status} {project.ProjectName}");
            Console.WriteLine($"    Topic: {project.ProjectTopic}");
            Console.WriteLine($"    Videos: {project.VideoCount} | Topics: {project.TotalTopics} | Clusters: {project.ClusterCount}");

            if (project.TotalTopics > 0)
            {
                var clusterPercentage = project.TotalTopics > 0
                    ? (project.ClusteredTopics * 100) / project.TotalTopics
                    : 0;
                Console.WriteLine($"    Clustered: {project.ClusteredTopics}/{project.TotalTopics} ({clusterPercentage}%)");
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Gets user's project selection
    /// </summary>
    private int? GetProjectSelection(int maxOptions)
    {
        while (true)
        {
            var input = ConsoleOutput.GetUserInput($"Select a project (1-{maxOptions}) or 'q' to quit:");

            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "q")
            {
                return null;
            }

            if (int.TryParse(input, out int selection) && selection >= 1 && selection <= maxOptions)
            {
                return selection;
            }

            Console.WriteLine($"Please enter a number between 1 and {maxOptions}, or 'q' to quit.");
        }
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

    /// <summary>
    /// Gets status icon for project based on processing state
    /// </summary>
    private string GetProjectStatusIcon(ProjectProcessingSummary project)
    {
        if (project.IsFullyProcessed)
            return "Fully Processed"; // Fully processed with clusters
        else if (project.TotalTopics > 0 && !project.HasClusters)
            return "No Clusters"; // Has topics but no clusters
        else if (project.VideoCount > 0 && project.TotalTopics == 0)
            return "No Topics"; // Has videos but no topics
        else if (project.VideoCount == 0)
            return "No Videos"; // New project with no videos
        else
            return "??"; // Unknown state
    }
}
