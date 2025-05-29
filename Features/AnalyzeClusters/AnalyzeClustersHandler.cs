using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VideoScripts.Data;
using VideoScripts.Features.AnalyzeClusters.Models;

namespace VideoScripts.Features.AnalyzeClusters;

public class AnalyzeClustersHandler
{
    private readonly AppDbContext _dbContext;
    private readonly AnalyzeClustersService _analysisService;
    private readonly ILogger<AnalyzeClustersHandler> _logger;

    public AnalyzeClustersHandler(
        AppDbContext dbContext,
        AnalyzeClustersService analysisService,
        ILogger<AnalyzeClustersHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes all clusters in a project
    /// </summary>
    /// <param name="projectName">Name of the project to analyze</param>
    /// <returns>Analysis results for all clusters in the project</returns>
    public async Task<ProjectClusterAnalysisResult> AnalyzeProjectClustersAsync(string projectName)
    {
        var result = new ProjectClusterAnalysisResult
        {
            ProjectName = projectName,
            ClusterAnalyses = new List<ClusterAnalysisResult>()
        };

        try
        {
            _logger.LogInformation($"Starting cluster analysis for project: {projectName}");

            // Get project with all clusters and their topics
            var project = await GetProjectWithClustersAsync(projectName);

            if (project == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Project '{projectName}' not found";
                return result;
            }

            if (!project.TopicClusters.Any())
            {
                result.Success = false;
                result.ErrorMessage = $"Project '{projectName}' has no clusters to analyze";
                return result;
            }

            _logger.LogInformation($"Found {project.TopicClusters.Count} clusters to analyze");

            // Analyze each cluster
            foreach (var cluster in project.TopicClusters.OrderBy(c => c.DisplayOrder))
            {
                var clusterAnalysis = await _analysisService.AnalyzeClusterAsync(cluster, projectName);
                result.ClusterAnalyses.Add(clusterAnalysis);

                if (clusterAnalysis.Success)
                {
                    result.SuccessfulAnalyses++;
                }
                else
                {
                    result.FailedAnalyses++;
                }
            }

            result.Success = result.SuccessfulAnalyses > 0;
            result.TotalClusters = project.TopicClusters.Count;

            _logger.LogInformation($"Completed cluster analysis for project '{projectName}': {result.SuccessfulAnalyses} successful, {result.FailedAnalyses} failed");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing clusters for project {projectName}: {ex.Message}");
            result.Success = false;
            result.ErrorMessage = $"Analysis failed: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Analyzes a specific cluster by cluster ID
    /// </summary>
    /// <param name="clusterId">ID of the cluster to analyze</param>
    /// <returns>Analysis result for the specific cluster</returns>
    public async Task<ClusterAnalysisResult> AnalyzeSpecificClusterAsync(Guid clusterId)
    {
        try
        {
            var cluster = await GetClusterWithTopicsAsync(clusterId);

            if (cluster == null)
            {
                return new ClusterAnalysisResult
                {
                    Success = false,
                    ErrorMessage = $"Cluster with ID {clusterId} not found",
                    ClusterId = clusterId
                };
            }

            var projectName = cluster.Project?.Name ?? "Unknown Project";
            return await _analysisService.AnalyzeClusterAsync(cluster, projectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing cluster {clusterId}: {ex.Message}");
            return new ClusterAnalysisResult
            {
                Success = false,
                ErrorMessage = $"Analysis failed: {ex.Message}",
                ClusterId = clusterId
            };
        }
    }

    /// <summary>
    /// Gets clusters available for analysis with basic information
    /// </summary>
    /// <param name="projectName">Name of the project</param>
    /// <returns>List of clusters available for analysis</returns>
    public async Task<List<ClusterAnalysisInfo>> GetClustersForAnalysisAsync(string projectName)
    {
        try
        {
            var clusters = await _dbContext.TopicClusters
                .Include(tc => tc.TopicAssignments)
                .ThenInclude(ta => ta.TranscriptTopic)
                .Where(tc => tc.Project.Name == projectName)
                .OrderBy(tc => tc.DisplayOrder)
                .Select(tc => new ClusterAnalysisInfo
                {
                    ClusterId = tc.Id,
                    ClusterName = tc.ClusterName,
                    DisplayOrder = tc.DisplayOrder,
                    TopicCount = tc.TopicAssignments.Count,
                    HasBlueprintElements = tc.TopicAssignments.Any(ta =>
                        ta.TranscriptTopic.BluePrintElements != null && ta.TranscriptTopic.BluePrintElements != ""),
                    TotalContentLength = tc.TopicAssignments.Sum(ta =>
                        (ta.TranscriptTopic.Content == null ? 0 : ta.TranscriptTopic.Content.Length) +
                        (ta.TranscriptTopic.TopicSummary == null ? 0 : ta.TranscriptTopic.TopicSummary.Length))
                })
                .ToListAsync();

            return clusters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting clusters for analysis in project {projectName}: {ex.Message}");
            return new List<ClusterAnalysisInfo>();
        }
    }

    /// <summary>
    /// Gets analysis status for all projects
    /// </summary>
    /// <returns>Summary of projects and their cluster analysis readiness</returns>
    public async Task<List<ProjectAnalysisStatus>> GetProjectAnalysisStatusAsync()
    {
        try
        {
            var projects = await _dbContext.Projects
                .Include(p => p.TopicClusters)
                .ThenInclude(tc => tc.TopicAssignments)
                .Select(p => new ProjectAnalysisStatus
                {
                    ProjectName = p.Name,
                    ProjectTopic = p.Topic,
                    TotalClusters = p.TopicClusters.Count,
                    TotalTopics = p.TopicClusters.SelectMany(tc => tc.TopicAssignments).Count(),
                    HasClusters = p.TopicClusters.Any(),
                    ClustersWithBlueprints = p.TopicClusters.Count(tc =>
                        tc.TopicAssignments.Any(ta =>
                            !string.IsNullOrWhiteSpace(ta.TranscriptTopic.BluePrintElements))),
                    IsReadyForAnalysis = p.TopicClusters.Any()
                })
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            return projects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting project analysis status: {ex.Message}");
            return new List<ProjectAnalysisStatus>();
        }
    }

    /// <summary>
    /// Gets project with all clusters and their complete topic information
    /// </summary>
    private async Task<Data.Entities.ProjectEntity?> GetProjectWithClustersAsync(string projectName)
    {
        return await _dbContext.Projects
            .Include(p => p.TopicClusters)
            .ThenInclude(tc => tc.TopicAssignments)
            .ThenInclude(ta => ta.TranscriptTopic)
            .ThenInclude(tt => tt.Video)
            .FirstOrDefaultAsync(p => p.Name == projectName);
    }

    /// <summary>
    /// Gets a specific cluster with all topic information
    /// </summary>
    private async Task<Data.Entities.TopicClusterEntity?> GetClusterWithTopicsAsync(Guid clusterId)
    {
        return await _dbContext.TopicClusters
            .Include(tc => tc.Project)
            .Include(tc => tc.TopicAssignments)
            .ThenInclude(ta => ta.TranscriptTopic)
            .ThenInclude(tt => tt.Video)
            .FirstOrDefaultAsync(tc => tc.Id == clusterId);
    }
}

// Supporting models for the handler
public class ProjectClusterAnalysisResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int TotalClusters { get; set; }
    public int SuccessfulAnalyses { get; set; }
    public int FailedAnalyses { get; set; }
    public List<ClusterAnalysisResult> ClusterAnalyses { get; set; } = new List<ClusterAnalysisResult>();
}

public class ClusterAnalysisInfo
{
    public Guid ClusterId { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int TopicCount { get; set; }
    public bool HasBlueprintElements { get; set; }
    public int TotalContentLength { get; set; }
}

public class ProjectAnalysisStatus
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectTopic { get; set; } = string.Empty;
    public int TotalClusters { get; set; }
    public int TotalTopics { get; set; }
    public bool HasClusters { get; set; }
    public int ClustersWithBlueprints { get; set; }
    public bool IsReadyForAnalysis { get; set; }
}