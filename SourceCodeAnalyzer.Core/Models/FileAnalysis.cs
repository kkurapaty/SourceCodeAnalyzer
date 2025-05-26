namespace SourceCodeAnalyzer.Core.Models;

public class FileAnalysis
{
    public string Path { get; set; }
    public string Type { get; set; }
    public int TotalLines { get; set; }
    public int SourceLines { get; set; }
    public int CommentLines { get; set; }
    public int BlankLines { get; set; }
}