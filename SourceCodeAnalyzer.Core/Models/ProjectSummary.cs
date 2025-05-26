namespace SourceCodeAnalyzer.Core.Models;

public class ProjectSummary
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Type { get; set; }
    public int TotalFiles { get; set; }
    public int SourceFiles { get; set; }
    public int ConfigFiles { get; set; }
    public int OtherFiles { get; set; }
    public int TotalLines { get; set; }
    public int SourceLines { get; set; }
    public int CommentLines { get; set; }
    public int BlankLines { get; set; }
}