using VideoScripts.Core;
using VideoScripts.Features.AnalyzeClusters.Models;

namespace VideoScripts.Features.AnalyzeClusters;

/// <summary>
/// Service for displaying cluster analysis results in a formatted way
/// </summary>
public class AnalyzeClustersDisplayService
{
    private readonly AnalyzeClustersHandler _handler;

    public AnalyzeClustersDisplayService(AnalyzeClustersHandler handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Displays the main cluster analysis menu
    /// </summary>
    public async Task DisplayAnalysisMenuAsync()
    {
        ConsoleOutput.DisplaySectionHeader("CLUSTER CONTENT ANALYSIS");

        // Get projects with analysis readiness status
        var projects = await _handler.GetProjectAnalysisStatusAsync();

        if (!projects.Any())
        {
            ConsoleOutput.DisplayInfo("No projects found.");
            return;
        }

        var readyProjects = projects.Where(p => p.IsReadyForAnalysis).ToList();

        if (!readyProjects.Any())
        {
            ConsoleOutput.DisplayInfo("No projects with clusters ready for analysis.");
            await DisplayProjectStatusAsync(projects);
            return;
        }

        // Display available projects
        ConsoleOutput.DisplayInfo($"Found {readyProjects.Count} project(s) ready for analysis:");
        Console.WriteLine();

        for (int i = 0; i < readyProjects.Count; i++)
        {
            var project = readyProjects[i];
            Console.WriteLine($"{i + 1}. {project.ProjectName}");
            Console.WriteLine($"   Topic: {project.ProjectTopic}");
            Console.WriteLine($"   Clusters: {project.TotalClusters} | Topics: {project.TotalTopics}");
            Console.WriteLine($"   Blueprints: {project.ClustersWithBlueprints} clusters contain blueprint elements");
            Console.WriteLine();
        }

        // Get user selection
        var selection = GetProjectSelection(readyProjects.Count);
        if (selection.HasValue)
        {
            var selectedProject = readyProjects[selection.Value - 1];
            await DisplayProjectAnalysisOptionsAsync(selectedProject.ProjectName);
        }
    }

    /// <summary>
    /// Displays analysis options for a specific project
    /// </summary>
    private async Task DisplayProjectAnalysisOptionsAsync(string projectName)
    {
        while (true)
        {
            ConsoleOutput.DisplaySectionHeader($"ANALYSIS OPTIONS: {projectName.ToUpper()}");

            Console.WriteLine("What would you like to do?");
            Console.WriteLine();
            Console.WriteLine("1. Analyze All Clusters");
            Console.WriteLine("2. Analyze Specific Cluster");
            Console.WriteLine("3. View Cluster Information");
            Console.WriteLine("4. Back to Project Selection");
            Console.WriteLine();

            var choice = ConsoleOutput.GetUserInput("Enter your choice (1-4):");

            switch (choice)
            {
                case "1":
                    await AnalyzeAllClustersAsync(projectName);
                    break;
                case "2":
                    await AnalyzeSpecificClusterAsync(projectName);
                    break;
                case "3":
                    await DisplayClusterInformationAsync(projectName);
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }

            Console.WriteLine();
            ConsoleOutput.GetUserInput("Press Enter to continue...");
        }
    }

    /// <summary>
    /// Analyzes all clusters in a project
    /// </summary>
    private async Task AnalyzeAllClustersAsync(string projectName)
    {
        ConsoleOutput.DisplaySectionHeader($"ANALYZING ALL CLUSTERS: {projectName}");

        var result = await _handler.AnalyzeProjectClustersAsync(projectName);

        if (!result.Success)
        {
            ConsoleOutput.DisplayError($"Analysis failed: {result.ErrorMessage}");
            return;
        }

        ConsoleOutput.DisplayInfo($"Analysis completed: {result.SuccessfulAnalyses}/{result.TotalClusters} clusters analyzed successfully");
        Console.WriteLine();

        foreach (var clusterAnalysis in result.ClusterAnalyses)
        {
            DisplayClusterAnalysisResult(clusterAnalysis);
        }
    }

    /// <summary>
    /// Analyzes a specific cluster selected by the user
    /// </summary>
    private async Task AnalyzeSpecificClusterAsync(string projectName)
    {
        ConsoleOutput.DisplaySectionHeader($"SELECT CLUSTER TO ANALYZE: {projectName}");

        var clusters = await _handler.GetClustersForAnalysisAsync(projectName);

        if (!clusters.Any())
        {
            ConsoleOutput.DisplayInfo("No clusters found for analysis.");
            return;
        }

        // Display available clusters
        for (int i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];
            Console.WriteLine($"{i + 1}. {cluster.ClusterName}");
            Console.WriteLine($"   Order: {cluster.DisplayOrder} | Topics: {cluster.TopicCount}");
            Console.WriteLine($"   Content Length: {cluster.TotalContentLength:N0} chars");
            Console.WriteLine($"   Has Blueprints: {(cluster.HasBlueprintElements ? "Yes" : "No")}");
            Console.WriteLine();
        }

        var selection = GetClusterSelection(clusters.Count);
        if (selection.HasValue)
        {
            var selectedCluster = clusters[selection.Value - 1];
            ConsoleOutput.DisplayInfo($"Analyzing cluster: {selectedCluster.ClusterName}...");

            var result = await _handler.AnalyzeSpecificClusterAsync(selectedCluster.ClusterId);
            DisplayClusterAnalysisResult(result);
        }
    }

    /// <summary>
    /// Displays basic cluster information without analysis
    /// </summary>
    private async Task DisplayClusterInformationAsync(string projectName)
    {
        ConsoleOutput.DisplaySectionHeader($"CLUSTER INFORMATION: {projectName}");

        var clusters = await _handler.GetClustersForAnalysisAsync(projectName);

        if (!clusters.Any())
        {
            ConsoleOutput.DisplayInfo("No clusters found.");
            return;
        }

        foreach (var cluster in clusters)
        {
            ConsoleOutput.DisplaySubsectionHeader($"CLUSTER {cluster.DisplayOrder}: {cluster.ClusterName}");
            Console.WriteLine($"Topics: {cluster.TopicCount}");
            Console.WriteLine($"Total Content Length: {cluster.TotalContentLength:N0} characters");
            Console.WriteLine($"Has Blueprint Elements: {(cluster.HasBlueprintElements ? "Yes" : "No")}");

            // Calculate content density category
            var densityCategory = cluster.TotalContentLength switch
            {
                < 1000 => "Light",
                < 5000 => "Medium",
                _ => "Heavy"
            };
            Console.WriteLine($"Estimated Content Density: {densityCategory}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Displays the complete analysis result for a cluster
    /// </summary>
    private void DisplayClusterAnalysisResult(ClusterAnalysisResult result)
    {
        if (!result.Success)
        {
            Console.WriteLine($"❌ {result.ClusterName}: {result.ErrorMessage}");
            return;
        }

        ConsoleOutput.DisplaySubsectionHeader($"ANALYSIS: {result.ClusterName}");

        // Display Readiness Analysis
        if (result.ReadinessAnalysis != null)
        {
            DisplayReadinessAnalysis(result.ReadinessAnalysis);
        }

        // Display Density Analysis
        if (result.DensityAnalysis != null)
        {
            DisplayDensityAnalysis(result.DensityAnalysis);
        }

        // Display Structural Analysis
        if (result.StructuralAnalysis != null)
        {
            DisplayStructuralAnalysis(result.StructuralAnalysis);
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Displays readiness analysis results
    /// </summary>
    private void DisplayReadinessAnalysis(ClusterReadinessAnalysis analysis)
    {
        Console.WriteLine("📊 CLUSTER READINESS ANALYSIS");
        Console.WriteLine($"   Overall Readiness Score: {analysis.OverallReadinessScore}/10 {GetScoreEmoji(analysis.OverallReadinessScore)}");
        Console.WriteLine($"   Narrative Completeness: {analysis.NarrativeCompletenessScore}/10 {GetScoreEmoji(analysis.NarrativeCompletenessScore)}");
        Console.WriteLine($"   Structural Coherence: {analysis.StructuralCoherenceScore}/10 {GetScoreEmoji(analysis.StructuralCoherenceScore)}");
        Console.WriteLine($"   Cluster Type: {analysis.ClusterType}");

        if (analysis.KeyStrengths.Any())
        {
            Console.WriteLine("   ✅ Key Strengths:");
            foreach (var strength in analysis.KeyStrengths)
            {
                Console.WriteLine($"     • {strength}");
            }
        }

        if (analysis.CriticalGaps.Any())
        {
            Console.WriteLine("   ⚠️  Critical Gaps:");
            foreach (var gap in analysis.CriticalGaps)
            {
                Console.WriteLine($"     • {gap}");
            }
        }

        if (analysis.MissingElements.Any())
        {
            Console.WriteLine("   🔍 Missing Elements:");
            foreach (var element in analysis.MissingElements)
            {
                Console.WriteLine($"     • {element}");
            }
        }

        if (!string.IsNullOrWhiteSpace(analysis.ScriptUsageRecommendation))
        {
            Console.WriteLine($"   💡 Script Recommendation:");
            Console.WriteLine($"     {analysis.ScriptUsageRecommendation}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Displays density analysis results
    /// </summary>
    private void DisplayDensityAnalysis(ContentDensityAnalysis analysis)
    {
        Console.WriteLine("📈 CONTENT DENSITY ANALYSIS");
        Console.WriteLine($"   Overall Density: {analysis.OverallDensity} {GetDensityEmoji(analysis.OverallDensity)}");
        Console.WriteLine($"   Cognitive Load: {analysis.CognitiveLoad} {GetLoadEmoji(analysis.CognitiveLoad)}");
        Console.WriteLine($"   Depth/Breadth Balance: {analysis.DepthBreadthRatio}");

        if (!string.IsNullOrWhiteSpace(analysis.RecommendedScriptPacing))
        {
            Console.WriteLine($"   ⏱️  Recommended Pacing: {analysis.RecommendedScriptPacing}");
        }

        if (analysis.TopicDensityRatings.Any())
        {
            Console.WriteLine("   📋 Individual Topic Ratings:");
            foreach (var rating in analysis.TopicDensityRatings)
            {
                var densityEmoji = GetDensityEmoji(rating.DensityLevel);
                var typeEmoji = GetInformationTypeEmoji(rating.InformationType);
                Console.WriteLine($"     {densityEmoji} {rating.TopicTitle}");
                Console.WriteLine($"       Density: {rating.DensityLevel} | Type: {rating.InformationType} {typeEmoji}");
            }
        }

        if (analysis.SimplificationOpportunities.Any())
        {
            Console.WriteLine("   🎯 Simplification Opportunities:");
            foreach (var opportunity in analysis.SimplificationOpportunities)
            {
                Console.WriteLine($"     • {opportunity}");
            }
        }

        if (analysis.PacingImplications.Any())
        {
            Console.WriteLine("   ⏳ Pacing Considerations:");
            foreach (var implication in analysis.PacingImplications)
            {
                Console.WriteLine($"     • {implication}");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Displays structural analysis results
    /// </summary>
    private void DisplayStructuralAnalysis(StructuralElementsAnalysis analysis)
    {
        Console.WriteLine("🏗️ STRUCTURAL ELEMENTS ANALYSIS");
        Console.WriteLine($"   Total Structural Elements: {analysis.TotalStructuralElements}");

        if (!string.IsNullOrWhiteSpace(analysis.PrimaryAnchorElement))
        {
            Console.WriteLine($"   🎯 Primary Anchor: {analysis.PrimaryAnchorElement}");
        }

        if (analysis.FrameworksAndModels.Any())
        {
            Console.WriteLine("   🧠 Frameworks & Models:");
            foreach (var framework in analysis.FrameworksAndModels)
            {
                Console.WriteLine($"     • {framework.Name} {GetScoreEmoji(framework.CompletenessScore)}");
                Console.WriteLine($"       Completeness: {framework.CompletenessScore}/10");
                if (!string.IsNullOrWhiteSpace(framework.InstructionalValue))
                {
                    Console.WriteLine($"       Value: {framework.InstructionalValue}");
                }
            }
        }

        if (analysis.StepByStepProcesses.Any())
        {
            Console.WriteLine("   📝 Step-by-Step Processes:");
            foreach (var process in analysis.StepByStepProcesses)
            {
                Console.WriteLine($"     • {process.Name}");
                Console.WriteLine($"       Steps: {process.StepCount} | Clarity: {process.ClarityScore}/10 | Actionability: {process.ActionabilityScore}/10");
                if (process.MissingSteps.Any())
                {
                    Console.WriteLine($"       Missing: {string.Join(", ", process.MissingSteps)}");
                }
            }
        }

        if (analysis.ListsAndEnumerations.Any())
        {
            Console.WriteLine("   📊 Lists & Enumerations:");
            foreach (var list in analysis.ListsAndEnumerations)
            {
                Console.WriteLine($"     • {list.Name}");
                Console.WriteLine($"       Items: {list.ItemCount} | Quality: {list.OrganizationQuality} | Memorability: {list.MemorabilityScore}/10");
            }
        }

        if (analysis.BlueprintElements.Any())
        {
            Console.WriteLine("   🔧 Blueprint Elements:");
            foreach (var blueprint in analysis.BlueprintElements)
            {
                Console.WriteLine($"     • {blueprint.Name}");
                Console.WriteLine($"       Uniqueness: {blueprint.UniquenessScore}/10 | Value: {blueprint.ValueScore}/10");
                if (!string.IsNullOrWhiteSpace(blueprint.PracticalApplication))
                {
                    Console.WriteLine($"       Application: {blueprint.PracticalApplication}");
                }
            }
        }

        if (analysis.HookPotentialElements.Any())
        {
            Console.WriteLine("   🎣 Hook Potential Elements:");
            foreach (var hook in analysis.HookPotentialElements)
            {
                Console.WriteLine($"     • {hook}");
            }
        }

        if (!string.IsNullOrWhiteSpace(analysis.ScriptStructureSuggestion))
        {
            Console.WriteLine($"   📋 Script Structure Suggestion:");
            Console.WriteLine($"     {analysis.ScriptStructureSuggestion}");
        }

        if (analysis.MissingStructuralPieces.Any())
        {
            Console.WriteLine("   🔍 Missing Structural Pieces:");
            foreach (var missing in analysis.MissingStructuralPieces)
            {
                Console.WriteLine($"     • {missing}");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Gets emoji for score visualization
    /// </summary>
    private string GetScoreEmoji(int score)
    {
        return score switch
        {
            >= 8 => "🟢",
            >= 6 => "🟡",
            >= 4 => "🟠",
            _ => "🔴"
        };
    }

    /// <summary>
    /// Gets emoji for density level
    /// </summary>
    private string GetDensityEmoji(string density)
    {
        return density?.ToLower() switch
        {
            "light" => "🔵",
            "medium" => "🟡",
            "heavy" => "🔴",
            _ => "⚪"
        };
    }

    /// <summary>
    /// Gets emoji for cognitive load
    /// </summary>
    private string GetLoadEmoji(string load)
    {
        return load?.ToLower() switch
        {
            "low" => "🟢",
            "medium" => "🟡",
            "high" => "🔴",
            _ => "⚪"
        };
    }

    /// <summary>
    /// Gets emoji for information type
    /// </summary>
    private string GetInformationTypeEmoji(string type)
    {
        return type?.ToLower() switch
        {
            "conceptual" => "🧠",
            "actionable" => "⚡",
            "mixed" => "🔄",
            _ => "📄"
        };
    }

    /// <summary>
    /// Displays status of all projects for reference
    /// </summary>
    private async Task DisplayProjectStatusAsync(List<ProjectAnalysisStatus> projects)
    {
        ConsoleOutput.DisplaySubsectionHeader("PROJECT STATUS");

        foreach (var project in projects)
        {
            var status = project.IsReadyForAnalysis ? "✅ Ready" : "❌ Not Ready";
            Console.WriteLine($"{status} {project.ProjectName}");
            Console.WriteLine($"    Topic: {project.ProjectTopic}");
            Console.WriteLine($"    Clusters: {project.TotalClusters} | Topics: {project.TotalTopics}");

            if (!project.IsReadyForAnalysis)
            {
                Console.WriteLine($"    Reason: {(project.HasClusters ? "No clusters found" : "No topics clustered yet")}");
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
    /// Gets user's cluster selection
    /// </summary>
    private int? GetClusterSelection(int maxOptions)
    {
        while (true)
        {
            var input = ConsoleOutput.GetUserInput($"Select a cluster (1-{maxOptions}) or 'q' to back:");

            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "q")
            {
                return null;
            }

            if (int.TryParse(input, out int selection) && selection >= 1 && selection <= maxOptions)
            {
                return selection;
            }

            Console.WriteLine($"Please enter a number between 1 and {maxOptions}, or 'q' to go back.");
        }
    }
}