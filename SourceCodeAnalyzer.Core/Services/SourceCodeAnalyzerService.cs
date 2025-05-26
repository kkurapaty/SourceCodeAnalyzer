using System.Collections.Concurrent;
using System.IO;
using SourceCodeAnalyzer.Core.Models;

namespace SourceCodeAnalyzer.Core.Services;

public class SourceCodeAnalyzerService : ISourceCodeAnalyzer
{
    private readonly string[] _excludedDirectories = { "bin", "obj", "node_modules", "packages", "dist", "build", ".vs", ".git" };
    private readonly string[] _excludedFilePatterns = { "*.dll", "*.exe", "*.pdb", "*.cache", "*.suo", "*.user" };
    private readonly Dictionary<string, FileTypeInfo> _fileTypePatterns = new Dictionary<string, FileTypeInfo>
    {
        // C# files
        { ".cs", new FileTypeInfo("C#", "//", "/*", "*/") },
        { ".csproj", new FileTypeInfo("C# Project", null, "<!--", "-->") },
        { ".sln", new FileTypeInfo(".NET Solution", null, "<!--", "-->") },
        // VB.NET files
        { ".vb", new FileTypeInfo("VB.NET", "'", "'", "'") },
        // C/C++ files
        { ".c", new FileTypeInfo("C", "//", "/*", "*/") },
        { ".cpp", new FileTypeInfo("C++", "//", "/*", "*/") },
        { ".h", new FileTypeInfo("C/C++ Header", "//", "/*", "*/") },
        // Java files
        { ".java", new FileTypeInfo("Java", "//", "/*", "*/") },
        // Delphi files
        { ".pas", new FileTypeInfo("Delphi", "//", "{", "}") },
        { ".dpr", new FileTypeInfo("Delphi Project", "//", "{", "}") },
        // JavaScript/TypeScript files
        { ".js", new FileTypeInfo("JavaScript", "//", "/*", "*/") },
        { ".ts", new FileTypeInfo("TypeScript", "//", "/*", "*/") },
        // SQL files
        { ".sql", new FileTypeInfo("SQL", "--", "/*", "*/") },
        // PowerShell files
        { ".ps1", new FileTypeInfo("PowerShell", "#", "<#", "#>") },
        // Python files
        { ".py", new FileTypeInfo("Python", "#", "\"\"\"", "\"\"\"") },
        // HTML/XML files
        { ".html", new FileTypeInfo("HTML", null, "<!--", "-->") },
        { ".htm", new FileTypeInfo("HTML", null, "<!--", "-->") },
        { ".xaml", new FileTypeInfo("WPF", null, "<!--", "-->") },
        { ".xml", new FileTypeInfo("XML", null, "<!--", "-->") },
        { ".razor", new FileTypeInfo("Blazor", null, "@*", "*@") },
        { ".aspx", new FileTypeInfo("ASP .NET", null, "<%--", "--%>") },
        // Config files
        { ".config", new FileTypeInfo("Config", null, "<!--", "-->") },
        { ".json", new FileTypeInfo("JSON", null, null, null) },
        { ".setting", new FileTypeInfo("Config", null, null, null) },
        // Shell scripts
        { ".sh", new FileTypeInfo("Shell Script", "#", ": '", "'") },
        // CSS files
        { ".css", new FileTypeInfo("CSS", null, "/*", "*/") },
        // PHP files
        { ".php", new FileTypeInfo("PHP", "//", "/*", "*/") },
        // Ruby files
        { ".rb", new FileTypeInfo("Ruby", "#", "=begin", "=end") },
        // Go files
        { ".go", new FileTypeInfo("Go", "//", "/*", "*/") },
        // Rust files
        { ".rs", new FileTypeInfo("Rust", "//", "/*", "*/") },
        // Images
        { ".ico", new FileTypeInfo("Icons", null, null, null) },
        { ".jpg", new FileTypeInfo("Image", null, null, null) },
        { ".gif", new FileTypeInfo("Image", null, null, null) },
        { ".png", new FileTypeInfo("Image", null, null, null) },
        // Fonts
        { ".ttf", new FileTypeInfo("Font", null, null, null) },
        { ".ott", new FileTypeInfo("Font", null, null, null) },
        // Resource
        { ".res", new FileTypeInfo("Resource", null, null, null) },
        { ".md", new FileTypeInfo("Markdown", null, null, null) },
    };

    private readonly string[] _solutionFilePatterns = { "*.sln" };
    private readonly Dictionary<string, string[]> _projectFilePatterns = new Dictionary<string, string[]>
    {
        { ".NET", new[] { "*.csproj", "*.vbproj", "*.fsproj" } },
        { "Delphi", new[] { "*.dproj" } },
        { "Java", new[] { "pom.xml", "build.gradle", "*.iml" } },
        { "Node.js", new[] { "package.json" } },
        { "Python", new[] { "requirements.txt", "setup.py" } }
    };

    public async Task<AnalysisResults> AnalyzeFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        var results = new AnalysisResults();
        var allFiles = await GetFilesAsync(folderPath, "*.*", SearchOption.AllDirectories, cancellationToken);

        // Check for cancellation before starting
        cancellationToken.ThrowIfCancellationRequested();

        // Find solutions
        results.Solutions = await FindSolutionsAsync(folderPath, cancellationToken);

        // Analyze each file
        foreach (var filePath in allFiles)
        {
            // Check for cancellation before starting
            cancellationToken.ThrowIfCancellationRequested();

            var extension = Path.GetExtension(filePath).ToLower();
            if (_fileTypePatterns.TryGetValue(extension, out var fileTypeInfo))
            {
                var fileAnalysis = await AnalyzeFileAsync(filePath, fileTypeInfo, cancellationToken);
                results.AddFile(fileAnalysis);
            }
            else
            {
                // For unknown file types, just count them
                var totalLines = await CountLinesAsync(filePath, cancellationToken);
                results.AddFile(new FileAnalysis
                {
                    Path = filePath,
                    Type = "Unknown",
                    TotalLines = totalLines,
                    SourceLines = 0,
                    CommentLines = 0,
                    BlankLines = 0
                });
            }
        }
        return results;
    }

    public async Task<AnalysisResults> AnalyzeFolderAsync(string folderPath, IProgressReporter progressReporter, CancellationToken cancellationToken)
    {
        var results = new AnalysisResults();

        try
        {
            // Phase 1: Discover files
            await progressReporter.ReportMessageAsync("Discovering files (excluding output directories)...", cancellationToken);
            var allFiles = await GetFilesAsync(folderPath, cancellationToken);
            await progressReporter.ReportMessageAsync($"Found {allFiles.Length} files after exclusion filters", cancellationToken);

            // Phase 2: Find solutions
            await progressReporter.ReportMessageAsync("Discovering solutions...", cancellationToken);
            results.Solutions = await FindSolutionsAsync(folderPath, cancellationToken);
            await progressReporter.ReportMessageAsync($"Found {results.Solutions.Count} solutions", cancellationToken);

            // Phase 3: Analyze files
            var fileAnalyses = new ConcurrentBag<FileAnalysis>();
            int processedFiles = 0;
            int totalFiles = allFiles.Length;

            await progressReporter.ReportMessageAsync("Starting analysis...", cancellationToken);

            await Parallel.ForEachAsync(allFiles, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            }, async (filePath, ct) =>
            {
                ct.ThrowIfCancellationRequested();

                var extension = Path.GetExtension(filePath).ToLower();
                FileAnalysis fileAnalysis;

                if (_fileTypePatterns.TryGetValue(extension, out var fileTypeInfo))
                {
                    fileAnalysis = await AnalyzeFileAsync(filePath, fileTypeInfo, ct);
                }
                else
                {
                    fileAnalysis = new FileAnalysis
                    {
                        Path = filePath,
                        Type = "Unknown",
                        TotalLines = await CountLinesAsync(filePath, ct),
                        SourceLines = 0,
                        CommentLines = 0,
                        BlankLines = 0
                    };
                }

                fileAnalyses.Add(fileAnalysis);
                var current = Interlocked.Increment(ref processedFiles);
                await progressReporter.ReportProgressAsync(current, totalFiles, fileAnalysis, ct);
            });

            // Add all collected analyses to results
            foreach (var analysis in fileAnalyses)
            {
                results.AddFile(analysis);
            }
            results.GenerateSummaries();
            await progressReporter.ReportCompletionAsync(true, "Analysis completed successfully", results);
            return results;
        }
        catch (OperationCanceledException)
        {
            await progressReporter.ReportCompletionAsync(false, "Analysis was cancelled", results);
            return results;
        }
        catch (Exception ex)
        {
            await progressReporter.ReportCompletionAsync(false, $"Analysis failed: {ex.Message}", results);
            throw;
        }
    }

    private async Task<string[]> GetFilesAsync(string folderPath, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
                IgnoreInaccessible = true
            };

            // First get all files that don't match excluded patterns
            var files = Directory.EnumerateFiles(folderPath, "*.*", options)
                .Where(file => !_excludedFilePatterns.Any(pattern =>
                    file.EndsWith(pattern.Substring(1)) || // For patterns like "*.ext"
                    new Wildcard(pattern).IsMatch(Path.GetFileName(file))));

            // Then filter out files in excluded directories
            files = files.Where(file => !_excludedDirectories.Any(dir =>
                file.Contains($"{Path.DirectorySeparatorChar}{dir}{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}{dir}")));

            return files.ToArray();
        }, cancellationToken);
    }

    private async Task<List<SolutionInfo>> FindSolutionsAsync(string folderPath, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var solutions = new List<SolutionInfo>();
            cancellationToken.ThrowIfCancellationRequested();

            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
                IgnoreInaccessible = true
            };

            foreach (var solutionPattern in _solutionFilePatterns)
            {
                foreach (var solutionFile in Directory.EnumerateFiles(folderPath, solutionPattern, options))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Skip solutions in excluded directories
                    if (_excludedDirectories.Any(dir =>
                        solutionFile.Contains($"{Path.DirectorySeparatorChar}{dir}{Path.DirectorySeparatorChar}")))
                    {
                        continue;
                    }

                    var solution = new SolutionInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(solutionFile),
                        Path = solutionFile,
                        Projects = new List<ProjectInfo>()
                    };

                    foreach (var projectType in _projectFilePatterns)
                    {
                        foreach (var projectPattern in projectType.Value)
                        {
                            var projectDir = Path.GetDirectoryName(solutionFile);
                            foreach (var projectFile in Directory.EnumerateFiles(projectDir, projectPattern, options))
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                // Skip projects in excluded directories
                                if (_excludedDirectories.Any(dir =>
                                    projectFile.Contains($"{Path.DirectorySeparatorChar}{dir}{Path.DirectorySeparatorChar}")))
                                {
                                    continue;
                                }

                                solution.Projects.Add(new ProjectInfo
                                {
                                    Name = Path.GetFileNameWithoutExtension(projectFile),
                                    Path = projectFile,
                                    Type = projectType.Key
                                });
                            }
                        }
                    }

                    solutions.Add(solution);
                }
            }

            return solutions;
        }, cancellationToken);
    }

    private async Task<FileAnalysis> AnalyzeFileAsync(string filePath, FileTypeInfo fileTypeInfo, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var analysis = new FileAnalysis
            {
                Path = filePath,
                Type = fileTypeInfo.LanguageName,
                TotalLines = 0,
                SourceLines = 0,
                CommentLines = 0,
                BlankLines = 0
            };

            cancellationToken.ThrowIfCancellationRequested();
            var lines = File.ReadAllLines(filePath);
            analysis.TotalLines = lines.Length;

            bool inBlockComment = false;
            foreach (var line in lines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    analysis.BlankLines++;
                    continue;
                }

                if (inBlockComment)
                {
                    analysis.CommentLines++;
                    if (fileTypeInfo.BlockCommentEnd != null && trimmedLine.Contains(fileTypeInfo.BlockCommentEnd))
                    {
                        inBlockComment = false;
                    }
                    continue;
                }

                if (fileTypeInfo.BlockCommentStart != null && trimmedLine.Contains(fileTypeInfo.BlockCommentStart))
                {
                    analysis.CommentLines++;
                    if (fileTypeInfo.BlockCommentEnd != null && !trimmedLine.Contains(fileTypeInfo.BlockCommentEnd))
                    {
                        inBlockComment = true;
                    }
                    continue;
                }

                if (fileTypeInfo.LineComment != null && trimmedLine.StartsWith(fileTypeInfo.LineComment))
                {
                    analysis.CommentLines++;
                    continue;
                }

                analysis.SourceLines++;
            }

            return analysis;
        }, cancellationToken);
    }

    private async Task<List<SolutionInfo>> FindSolutionsAsync1(string folderPath, CancellationToken cancellationToken)
    {
        var solutions = new List<SolutionInfo>();

        foreach (var solutionPattern in _solutionFilePatterns)
        {
            // Check for cancellation before starting
            cancellationToken.ThrowIfCancellationRequested();

            var solutionFiles = await GetFilesAsync(folderPath, solutionPattern, SearchOption.AllDirectories, cancellationToken);
            foreach (var solutionFile in solutionFiles)
            {
                // Check for cancellation before starting
                cancellationToken.ThrowIfCancellationRequested();

                var solution = new SolutionInfo
                {
                    Name = Path.GetFileNameWithoutExtension(solutionFile),
                    Path = solutionFile,
                    Projects = new List<ProjectInfo>()
                };

                // Find projects for this solution
                foreach (var projectType in _projectFilePatterns)
                {
                    // Check for cancellation before starting
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (var projectPattern in projectType.Value)
                    {
                        // Use Path.GetDirectoryName to get the solution directory
                        var solutionDirectory = Path.GetDirectoryName(solutionFile) ?? string.Empty;
                        if (string.IsNullOrEmpty(solutionDirectory))
                        {
                            continue; // Skip if the directory is invalid
                        }
                        // Check for cancellation before starting
                        cancellationToken.ThrowIfCancellationRequested();
                        foreach (var projectFile in await GetFilesAsync(solutionDirectory, projectPattern, SearchOption.AllDirectories, cancellationToken))
                        {
                            solution.Projects.Add(new ProjectInfo
                            {
                                Name = Path.GetFileNameWithoutExtension(projectFile),
                                Path = projectFile,
                                Type = projectType.Key
                            });
                        }
                    }
                }

                solutions.Add(solution);
            }
        }

        return solutions;
    }

    private async Task<FileAnalysis> AnalyzeFileAsync1(string filePath, FileTypeInfo fileTypeInfo, CancellationToken cancellationToken)
    {
        var analysis = new FileAnalysis
        {
            Path = filePath,
            Type = fileTypeInfo.LanguageName,
            TotalLines = 0,
            SourceLines = 0,
            CommentLines = 0,
            BlankLines = 0
        };

        bool inBlockComment = false;
        string[] lines;

        try
        {
            lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        }
        catch (Exception)
        {
            // Skip files that can't be read
            return analysis;
        }

        analysis.TotalLines = lines.Length;

        foreach (var line in lines)
        {
            // Check for cancellation before starting
            cancellationToken.ThrowIfCancellationRequested();

            var trimmedLine = line.Trim();

            // Check for blank lines
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                analysis.BlankLines++;
                continue;
            }

            // Handle block comments
            if (inBlockComment)
            {
                analysis.CommentLines++;
                if (fileTypeInfo.BlockCommentEnd != null && trimmedLine.Contains(fileTypeInfo.BlockCommentEnd))
                {
                    inBlockComment = false;
                }
                continue;
            }

            if (fileTypeInfo.BlockCommentStart != null && trimmedLine.Contains(fileTypeInfo.BlockCommentStart))
            {
                analysis.CommentLines++;
                if (fileTypeInfo.BlockCommentEnd != null && !trimmedLine.Contains(fileTypeInfo.BlockCommentEnd))
                {
                    inBlockComment = true;
                }
                continue;
            }

            // Check for single line comments
            if (fileTypeInfo.LineComment != null && trimmedLine.StartsWith(fileTypeInfo.LineComment))
            {
                analysis.CommentLines++;
                continue;
            }

            // If we got here, it's a source line
            analysis.SourceLines++;
        }

        return analysis;
    }

    private async Task<int> CountLinesAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
            return lines.Length;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private async Task<List<string>> GetFilesAsync(string folderPath, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken)
    {
        return await Task.Run(() => Directory.GetFiles(folderPath, searchPattern, searchOption).ToList(), cancellationToken);
    }
}