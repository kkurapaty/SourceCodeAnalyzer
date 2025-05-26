using SourceCodeAnalyzer.Core.Models;

namespace SourceCodeAnalyzer.Core.Services
{
    public interface ISourceCodeAnalyzer
    {
        Task<AnalysisResults> AnalyzeFolderAsync(string folderPath, CancellationToken cancellationToken);
        Task<AnalysisResults> AnalyzeFolderAsync(string folderPath, IProgressReporter progressReporter, CancellationToken cancellationToken);
    }

    public interface IProgressReporter
    {
        Task ReportProgressAsync(int current, int total, FileAnalysis result, CancellationToken ct);
        Task ReportMessageAsync(string message, CancellationToken ct);
        Task ReportCompletionAsync(bool isSuccess, string message, AnalysisResults results);
    }
}
