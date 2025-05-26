using System.IO;

namespace SourceCodeAnalyzer.Core.Models;

public class AnalysisResults
{
    public List<SolutionInfo> Solutions { get; set; } = new List<SolutionInfo>();
    public List<FileAnalysis> Files { get; set; } = new List<FileAnalysis>();
    public List<SolutionSummary> SolutionSummaries { get; set; } = new List<SolutionSummary>();

    public int TotalFiles => Files.Count;
    public int TotalLines => Files.Sum(f => f.TotalLines);
    public int TotalSourceLines => Files.Sum(f => f.SourceLines);
    public int TotalCommentLines => Files.Sum(f => f.CommentLines);
    public int TotalBlankLines => Files.Sum(f => f.BlankLines);

    public void AddFile(FileAnalysis fileAnalysis)
    {
        Files.Add(fileAnalysis);
    }

    // Add this method to generate summaries
    public void GenerateSummaries()
    {
        SolutionSummaries.Clear();

        foreach (var solution in Solutions)
        {
            var solutionSummary = new SolutionSummary
            {
                Name = solution.Name,
                Path = solution.Path,
                TotalProjects = solution.Projects.Count
            };

            foreach (var project in solution.Projects)
            {
                var projectFiles = Files.Where(f => f.Path.StartsWith(Path.GetDirectoryName(project.Path))).ToList();

                var projectSummary = new ProjectSummary
                {
                    Name = project.Name,
                    Path = project.Path,
                    Type = project.Type,
                    TotalFiles = projectFiles.Count,
                    SourceFiles = projectFiles.Count(f => IsSourceFile(f.Type)),
                    ConfigFiles = projectFiles.Count(f => IsConfigFile(f.Type)),
                    OtherFiles = projectFiles.Count(f => !IsSourceFile(f.Type) && !IsConfigFile(f.Type)),
                    TotalLines = projectFiles.Sum(f => f.TotalLines),
                    SourceLines = projectFiles.Sum(f => f.SourceLines),
                    CommentLines = projectFiles.Sum(f => f.CommentLines),
                    BlankLines = projectFiles.Sum(f => f.BlankLines)
                };

                solutionSummary.ProjectSummaries.Add(projectSummary);

                // Aggregate to solution totals
                solutionSummary.TotalFiles += projectSummary.TotalFiles;
                solutionSummary.SourceFiles += projectSummary.SourceFiles;
                solutionSummary.ConfigFiles += projectSummary.ConfigFiles;
                solutionSummary.OtherFiles += projectSummary.OtherFiles;
                solutionSummary.TotalLines += projectSummary.TotalLines;
                solutionSummary.SourceLines += projectSummary.SourceLines;
                solutionSummary.CommentLines += projectSummary.CommentLines;
                solutionSummary.BlankLines += projectSummary.BlankLines;
            }

            SolutionSummaries.Add(solutionSummary);
        }
    }

    private bool IsSourceFile(string fileType)
    {
        return fileType switch
        {
            "C#" or "VB.NET" or "C" or "C++" or "Java" or "Python"
                or "JavaScript" or "TypeScript" or "Delphi" => true,
            _ => false
        };
    }

    private bool IsConfigFile(string fileType)
    {
        return fileType switch
        {
            "Config" or "JSON" or "XML" => true,
            _ => false
        };
    }
    
    public void Clear()
    {
        Files.Clear();
        Solutions.Clear();
    }
}