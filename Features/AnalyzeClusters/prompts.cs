using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VideoScripts.Data.Common;

namespace VideoScripts.Features.AnalyzeClusters;

public static class Prompts
{
    // Using static expression-bodied methods, later plan on storing prompts and prompts versions in the db.

    /// <summary>
    /// Cluster Readiness Assesment Prompt (Cluster Readiness Score): 
    /// Some clusters might have complete narrative arcs while others are just collections of related points
    /// </summary>
    /// <returns></returns>
    public static string ClusterReadiness() => @"
                You are an expert content strategist analyzing video transcript clusters for script development potential.

                I will provide you with a cluster of related topics from YouTube video transcripts. Each topic includes:
                - Title
                - Summary 
                - Content details
                - Blueprint elements (if any)
                - Start time from original video

                Your task is to evaluate this cluster's ""script readiness"" by analyzing:

                1. **Narrative Completeness (Score 1-10)**
                   - Does this cluster tell a complete story or journey?
                   - Are there clear beginning, middle, and end elements?
                   - Would this work as a standalone video script section?

                2. **Structural Coherence (Score 1-10)**
                   - How well do the topics flow together?
                   - Are there logical connections between topics?
                   - Is there a clear teaching progression?

                3. **Missing Elements**
                   - What key concepts or steps are missing?
                   - What context would a viewer need that isn't provided?
                   - What transitions or examples would strengthen the flow?

                4. **Recommended Cluster Type**
                   - Is this an ""Introductory"" cluster (concepts, theory)?
                   - Is this an ""Implementation"" cluster (how-to, steps)?
                   - Is this a ""Deep Dive"" cluster (advanced, detailed)?
                   - Is this a ""Case Study"" cluster (examples, stories)?

                Provide your analysis in this format:
                - Overall Readiness Score: X/10
                - Cluster Type: [Type]
                - Key Strengths: [Bullet points]
                - Critical Gaps: [Bullet points]
                - Script Usage Recommendation: [How to best use this cluster in a script]

                Cluster data:
                [INSERT CLUSTER DATA HERE]";

    /// <summary>
    /// Content Density Analysis
    /// Measure depth vs breadth - is this cluster exploration-heavy or action-heavy?
    /// </summary>
    /// <returns></returns>
    public static string ContentDensity() => @"
                You are a content analyst specializing in educational video scripts.

                Analyze the following cluster of video topics for content density and depth. For each topic in the cluster, evaluate:

                1. **Information Density**
                   - How much actionable information vs conceptual explanation?
                   - Rate each topic: Light (overview), Medium (detailed), Heavy (comprehensive)

                2. **Depth vs Breadth Balance**
                   - Does this cluster go deep into few concepts or cover many concepts shallowly?
                   - What's the ratio of exploration to actionable content?

                3. **Pacing Implications**
                   - Based on density, how long would this cluster take to present effectively?
                   - Where would viewers need breathing room or examples?

                4. **Cognitive Load Assessment**
                   - How much new information is introduced?
                   - How complex are the concepts?
                   - What's the assumed knowledge level?

                Provide your analysis as:
                - Overall Density: [Light/Medium/Heavy]
                - Depth/Breadth Ratio: [e.g., ""70% depth, 30% breadth""]
                - Recommended Script Pacing: [e.g., ""Needs 5-7 minutes with 2 example breaks""]
                - Cognitive Load: [Low/Medium/High]
                - Simplification Opportunities: [Where content could be streamlined]

                Cluster data:
                [INSERT CLUSTER DATA HERE]";

    /// <summary>
    /// Structural Elements Extraction Prompt
    /// Which clusters contain the most frameworks/blueprints that could anchor a script?
    /// </summary>
    /// <returns></returns>
    public static string StructuralElements() => @"
                You are a script development specialist focusing on instructional design.

                Examine this cluster for structural elements that could anchor a video script. Identify and categorize:

                1. **Frameworks & Models**
                   - List all systematic approaches or mental models
                   - Rate their completeness (1-10)
                   - Note their instructional value

                2. **Step-by-Step Processes**
                   - Identify all sequential instructions
                   - Check for missing steps
                   - Assess clarity and actionability

                3. **Lists & Enumerations**
                   - Find all numbered or bulleted concepts
                   - Evaluate their organization
                   - Consider their memorability

                4. **Blueprint Elements**
                   - Extract all blueprint/template elements
                   - Assess their practical application
                   - Rate their uniqueness/value

                5. **Hook Potential**
                   - Which elements could serve as compelling video openings?
                   - What promises or outcomes are implied?

                Format your response as:
                - Total Structural Elements Found: [Number]
                - Primary Anchor Element: [The strongest framework/blueprint for script focus]
                - Supporting Elements: [List with brief descriptions]
                - Script Structure Suggestion: [How to organize these elements in a script]
                - Missing Structural Pieces: [What would complete the instructional value]

                Cluster data:
                [INSERT CLUSTER DATA HERE]";
}
