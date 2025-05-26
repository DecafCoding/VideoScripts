using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoScripts.Data;
using VideoScripts.Data.Entities;
using VideoScripts.Features.ClusterTopics.Models;

namespace VideoScripts.Features.ClusterTopics;

public class ClusterTopicsHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ClusterTopicsService _clusterService;
    private readonly ILogger<ClusterTopicsHandler> _logger;

    public ClusterTopicsHandler(
        AppDbContext dbContext,
        ClusterTopicsService clusterService,
        ILogger<ClusterTopicsHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _clusterService = clusterService ?? throw new ArgumentNullException(nameof(clusterService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes topic clustering for all topics in a project that haven't been clustered yet
    /// </summary>
    /// <param name="projectName">Name of the project to process</param>
    /// <returns>Processing result with success status and details</returns>
    public async Task<ClusteringProcessingResult> ProcessProjectClusteringAsync(string projectName)
    {
        var result = new ClusteringProcessingResult
        {
            ProjectName = projectName,
            ProcessedClusters = new List<ProcessedClusterInfo>()
        };

        try
        {
            _logger.LogInformation($"Starting topic clustering for project: {projectName}");

            // Get project with topics
            var project = await GetProjectWithTopicsAsync(projectName);

            if (project == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Project '{projectName}' not found";
                return result;
            }

            // Get all topics that need clustering (topics with no cluster assignment)
            var unclusteredTopics = await GetUnclusteredTopicsAsync(project.Id);

            if (!unclusteredTopics.Any())
            {
                _logger.LogInformation($"All topics in project '{projectName}' are already clustered");
                result.Success = true;
                result.ErrorMessage = "All topics are already clustered";
                return result;
            }

            _logger.LogInformation($"Found {unclusteredTopics.Count} unclustered topics");

            // Clear any existing clusters for this project to ensure fresh clustering
            await ClearExistingClustersAsync(project.Id);

            // Analyze topics using AI clustering
            var clusteringResult = await _clusterService.AnalyzeAndClusterTopicsAsync(unclusteredTopics, projectName);

            if (clusteringResult.Success)
            {
                // Save clustering results to database
                await SaveClusteringResultsAsync(project.Id, clusteringResult);

                // Build processing result
                result.Success = true;
                result.SuccessfulCount = clusteringResult.Clusters.Count;
                result.ProcessedClusters = clusteringResult.Clusters.Select(c => new ProcessedClusterInfo
                {
                    ClusterName = c.ClusterName,
                    ClusterDescription = c.ClusterDescription,
                    TopicCount = c.Topics.Count,
                    DisplayOrder = c.DisplayOrder,
                    Success = true,
                    Message = "Cluster created successfully"
                }).ToList();

                _logger.LogInformation($"Successfully processed clustering for project '{projectName}': {result.SuccessfulCount} clusters created");
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = clusteringResult.ErrorMessage;
                _logger.LogWarning($"Failed to process clustering for project: {projectName} - {result.ErrorMessage}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing clustering for project {projectName}: {ex.Message}");
            result.Success = false;
            result.ErrorMessage = $"Processing failed: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Gets clustering processing status for a project
    /// </summary>
    /// <param name="projectName">Name of the project</param>
    /// <returns>Status information about clustering processing</returns>
    public async Task<ClusteringProcessingStatus> GetProjectClusteringStatusAsync(string projectName)
    {
        try
        {
            var project = await _dbContext.Projects
                .Include(p => p.Videos)
                .ThenInclude(v => v.TranscriptTopics)
                .ThenInclude(t => t.ClusterAssignment)
                .Include(p => p.TopicClusters)
                .FirstOrDefaultAsync(p => p.Name == projectName);

            if (project == null)
            {
                return new ClusteringProcessingStatus
                {
                    ProjectName = projectName,
                    ProjectExists = false
                };
            }

            var allTopics = project.Videos.SelectMany(v => v.TranscriptTopics).ToList();
            var clusteredTopics = allTopics.Where(t => t.ClusterAssignment != null).ToList();
            var unclusteredTopics = allTopics.Where(t => t.ClusterAssignment == null).ToList();

            return new ClusteringProcessingStatus
            {
                ProjectName = projectName,
                ProjectExists = true,
                TotalTopics = allTopics.Count,
                ClusteredTopics = clusteredTopics.Count,
                UnclusteredTopics = unclusteredTopics.Count,
                TotalClusters = project.TopicClusters.Count,
                IsComplete = unclusteredTopics.Count == 0 && allTopics.Any()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting clustering status for project {projectName}: {ex.Message}");
            return new ClusteringProcessingStatus
            {
                ProjectName = projectName,
                ProjectExists = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets all clusters for a specific project with their topics
    /// </summary>
    /// <param name="projectName">Project name</param>
    /// <returns>List of clusters with their assigned topics</returns>
    public async Task<List<ProjectClusterInfo>> GetProjectClustersAsync(string projectName)
    {
        try
        {
            var project = await _dbContext.Projects
                .Include(p => p.TopicClusters)
                .ThenInclude(tc => tc.TopicAssignments)
                .ThenInclude(ta => ta.TranscriptTopic)
                .FirstOrDefaultAsync(p => p.Name == projectName);

            if (project == null)
            {
                return new List<ProjectClusterInfo>();
            }

            return project.TopicClusters
                .OrderBy(tc => tc.DisplayOrder)
                .Select(tc => new ProjectClusterInfo
                {
                    ClusterId = tc.Id,
                    ClusterName = tc.ClusterName,
                    DisplayOrder = tc.DisplayOrder,
                    TopicCount = tc.TopicAssignments.Count,
                    Topics = tc.TopicAssignments.Select(ta => new ClusterTopicInfo
                    {
                        TopicId = ta.TranscriptTopicId,
                        TopicTitle = ta.TranscriptTopic.Title,
                        TopicSummary = ta.TranscriptTopic.TopicSummary,
                        StartTime = ta.TranscriptTopic.StartTime
                    }).OrderBy(t => t.StartTime).ToList()
                }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting clusters for project {projectName}: {ex.Message}");
            return new List<ProjectClusterInfo>();
        }
    }

    /// <summary>
    /// Gets project with topics for clustering analysis
    /// </summary>
    private async Task<ProjectEntity?> GetProjectWithTopicsAsync(string projectName)
    {
        return await _dbContext.Projects
            .Include(p => p.Videos)
            .ThenInclude(v => v.TranscriptTopics)
            .ThenInclude(t => t.ClusterAssignment)
            .FirstOrDefaultAsync(p => p.Name == projectName);
    }

    /// <summary>
    /// Gets all topics that haven't been assigned to clusters yet
    /// </summary>
    private async Task<List<TranscriptTopicEntity>> GetUnclusteredTopicsAsync(Guid projectId)
    {
        return await _dbContext.TranscriptTopics
            .Include(t => t.Video)
            .Include(t => t.ClusterAssignment)
            .Where(t => t.Video.ProjectId == projectId && t.ClusterAssignment == null)
            .OrderBy(t => t.Video.PublishedAt)
            .ThenBy(t => t.StartTime)
            .ToListAsync();
    }

    /// <summary>
    /// Clears existing clusters for a project to ensure fresh clustering
    /// </summary>
    private async Task ClearExistingClustersAsync(Guid projectId)
    {
        try
        {
            // Get existing clusters for the project
            var existingClusters = await _dbContext.TopicClusters
                .Include(tc => tc.TopicAssignments)
                .Where(tc => tc.ProjectId == projectId)
                .ToListAsync();

            if (existingClusters.Any())
            {
                // Remove cluster assignments first
                foreach (var cluster in existingClusters)
                {
                    _dbContext.Entry(cluster).Collection(c => c.TopicAssignments).Load();
                    _dbContext.RemoveRange(cluster.TopicAssignments);
                }

                // Remove clusters
                _dbContext.RemoveRange(existingClusters);

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Cleared {existingClusters.Count} existing clusters for project");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error clearing existing clusters: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Saves clustering results to the database
    /// </summary>
    private async Task SaveClusteringResultsAsync(Guid projectId, ClusterTopicsResult clusteringResult)
    {
        try
        {
            var clusterEntities = new List<TopicClusterEntity>();
            var assignmentEntities = new List<TopicClusterAssignmentEntity>();

            foreach (var cluster in clusteringResult.Clusters)
            {
                // Create cluster entity
                var clusterEntity = new TopicClusterEntity
                {
                    ProjectId = projectId,
                    ClusterName = cluster.ClusterName,
                    DisplayOrder = cluster.DisplayOrder,
                    CreatedBy = "ClusterTopicsService",
                    LastModifiedBy = "ClusterTopicsService"
                };

                _dbContext.TopicClusters.Add(clusterEntity);
                await _dbContext.SaveChangesAsync(); // Save to get the ID

                clusterEntities.Add(clusterEntity);

                // Create topic assignments
                foreach (var topicAssignment in cluster.Topics)
                {
                    var assignmentEntity = new TopicClusterAssignmentEntity
                    {
                        TopicClusterId = clusterEntity.Id,
                        TranscriptTopicId = topicAssignment.TopicId,
                        CreatedBy = "ClusterTopicsService",
                        LastModifiedBy = "ClusterTopicsService"
                    };

                    assignmentEntities.Add(assignmentEntity);
                }
            }

            // Save all assignments
            _dbContext.TopicClusterAssignments.AddRange(assignmentEntities);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Saved {clusterEntities.Count} clusters and {assignmentEntities.Count} topic assignments to database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving clustering results to database: {ex.Message}");
            throw;
        }
    }
}

// Supporting models for the handler
public class ClusteringProcessingResult
{
    public bool Success { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }
    public List<ProcessedClusterInfo> ProcessedClusters { get; set; } = new List<ProcessedClusterInfo>();
}

public class ProcessedClusterInfo
{
    public string ClusterName { get; set; } = string.Empty;
    public string ClusterDescription { get; set; } = string.Empty;
    public int TopicCount { get; set; }
    public int DisplayOrder { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ClusteringProcessingStatus
{
    public string ProjectName { get; set; } = string.Empty;
    public bool ProjectExists { get; set; }
    public int TotalTopics { get; set; }
    public int ClusteredTopics { get; set; }
    public int UnclusteredTopics { get; set; }
    public int TotalClusters { get; set; }
    public bool IsComplete { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ProjectClusterInfo
{
    public Guid ClusterId { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int TopicCount { get; set; }
    public List<ClusterTopicInfo> Topics { get; set; } = new List<ClusterTopicInfo>();
}

public class ClusterTopicInfo
{
    public Guid TopicId { get; set; }
    public string TopicTitle { get; set; } = string.Empty;
    public string TopicSummary { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
}