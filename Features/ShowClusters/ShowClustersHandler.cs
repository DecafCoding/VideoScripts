using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoScripts.Data;
using VideoScripts.Features.ShowClusters.Models;

namespace VideoScripts.Features.ShowClusters;

public class ShowClustersHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ShowClustersHandler> _logger;

    public ShowClustersHandler(AppDbContext dbContext, ILogger<ShowClustersHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all available projects that have clusters
    /// </summary>
    /// <returns>List of projects with cluster information</returns>
    public async Task<List<ProjectClusterSummary>> GetProjectsWithClustersAsync()
    {
        try
        {
            var projects = await _dbContext.Projects
                .Include(p => p.TopicClusters)
                .ThenInclude(tc => tc.TopicAssignments)
                .Where(p => p.TopicClusters.Any())
                .Select(p => new ProjectClusterSummary
                {
                    ProjectName = p.Name,
                    ProjectTopic = p.Topic,
                    ClusterCount = p.TopicClusters.Count,
                    TotalTopics = p.TopicClusters.SelectMany(tc => tc.TopicAssignments).Count()
                })
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            return projects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving projects with clusters: {Message}", ex.Message);
            return new List<ProjectClusterSummary>();
        }
    }

    /// <summary>
    /// Gets detailed cluster information for a specific project
    /// </summary>
    /// <param name="projectName">Name of the project</param>
    /// <returns>Complete cluster hierarchy for the project</returns>
    public async Task<ProjectClusterDetails?> GetProjectClusterDetailsAsync(string projectName)
    {
        try
        {
            var project = await _dbContext.Projects
                .Include(p => p.TopicClusters)
                .ThenInclude(tc => tc.TopicAssignments)
                .ThenInclude(ta => ta.TranscriptTopic)
                .ThenInclude(tt => tt.Video)
                .FirstOrDefaultAsync(p => p.Name == projectName);

            if (project == null)
            {
                _logger.LogWarning("Project '{ProjectName}' not found", projectName);
                return null;
            }

            if (!project.TopicClusters.Any())
            {
                _logger.LogInformation("Project '{ProjectName}' has no clusters", projectName);
                return new ProjectClusterDetails
                {
                    ProjectName = projectName,
                    ProjectTopic = project.Topic,
                    Clusters = new List<ClusterDetails>()
                };
            }

            var clusterDetails = project.TopicClusters
                .OrderBy(tc => tc.DisplayOrder)
                .Select(tc => new ClusterDetails
                {
                    ClusterName = tc.ClusterName,
                    DisplayOrder = tc.DisplayOrder,
                    TopicCount = tc.TopicAssignments.Count,
                    Topics = tc.TopicAssignments
                        .Select(ta => new TopicDetails
                        {
                            TopicTitle = ta.TranscriptTopic.Title,
                            TopicSummary = ta.TranscriptTopic.TopicSummary,
                            StartTime = ta.TranscriptTopic.StartTime,
                            VideoTitle = ta.TranscriptTopic.Video.Title,
                            HasBlueprintElements = !string.IsNullOrWhiteSpace(ta.TranscriptTopic.BluePrintElements),
                            BlueprintElementsCount = GetBlueprintElementsCount(ta.TranscriptTopic.BluePrintElements)
                        })
                        .OrderBy(t => t.StartTime)
                        .ToList()
                })
                .ToList();

            return new ProjectClusterDetails
            {
                ProjectName = projectName,
                ProjectTopic = project.Topic,
                Clusters = clusterDetails
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cluster details for project '{ProjectName}': {Message}",
                projectName, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Gets a summary of all projects and their processing status
    /// </summary>
    /// <returns>Summary of all projects with their cluster status</returns>
    public async Task<List<ProjectProcessingSummary>> GetAllProjectsSummaryAsync()
    {
        try
        {
            var projects = await _dbContext.Projects
                .Include(p => p.Videos)
                .ThenInclude(v => v.TranscriptTopics)
                .ThenInclude(tt => tt.ClusterAssignment)
                .Include(p => p.TopicClusters)
                .Select(p => new ProjectProcessingSummary
                {
                    ProjectName = p.Name,
                    ProjectTopic = p.Topic,
                    VideoCount = p.Videos.Count,
                    TotalTopics = p.Videos.SelectMany(v => v.TranscriptTopics).Count(),
                    ClusteredTopics = p.Videos.SelectMany(v => v.TranscriptTopics)
                        .Count(tt => tt.ClusterAssignment != null),
                    ClusterCount = p.TopicClusters.Count,
                    HasClusters = p.TopicClusters.Any(),
                    IsFullyProcessed = p.Videos.Any() &&
                                    p.Videos.SelectMany(v => v.TranscriptTopics).Any() &&
                                    p.TopicClusters.Any()
                })
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            return projects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving projects summary: {Message}", ex.Message);
            return new List<ProjectProcessingSummary>();
        }
    }

    /// <summary>
    /// Counts blueprint elements from JSON string
    /// </summary>
    private int GetBlueprintElementsCount(string blueprintElements)
    {
        if (string.IsNullOrWhiteSpace(blueprintElements))
            return 0;

        try
        {
            // Simple count by looking for array elements in JSON
            var elementCount = blueprintElements.Split(',').Length;
            return elementCount > 1 ? elementCount : 0;
        }
        catch
        {
            return 0;
        }
    }
}