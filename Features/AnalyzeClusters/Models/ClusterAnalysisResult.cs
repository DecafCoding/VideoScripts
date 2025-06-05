namespace VideoScripts.Features.AnalyzeClusters.Models;

/// <summary>
/// Result from cluster analysis containing all three analysis types
/// </summary>
public class ClusterAnalysisResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Guid ClusterId { get; set; }
    public string ClusterName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;

    public ClusterReadinessAnalysis? ReadinessAnalysis { get; set; }
    public ContentDensityAnalysis? DensityAnalysis { get; set; }
    public StructuralElementsAnalysis? StructuralAnalysis { get; set; }
}