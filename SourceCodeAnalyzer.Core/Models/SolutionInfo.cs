namespace SourceCodeAnalyzer.Core.Models;

public class SolutionInfo
{
    public string Name { get; set; }
    public string Path { get; set; }
    public List<ProjectInfo> Projects { get; set; } = new List<ProjectInfo>();
}