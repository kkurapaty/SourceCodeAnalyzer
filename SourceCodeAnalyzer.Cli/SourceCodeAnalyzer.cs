using SourceCodeAnalyzer.Core.Models;
using System.IO;

namespace SourceCodeAnalyzer.Cli
{
    public class SourceCodeAnalyzer
    {
        private readonly Dictionary<string, FileTypeInfo> _fileTypePatterns = new Dictionary<string, FileTypeInfo>
        {
            // C# files
            { ".cs", new FileTypeInfo("C#", "//", "/*", "*/") },

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
            { ".xml", new FileTypeInfo("XML", null, "<!--", "-->") },

            // Config files
            { ".config", new FileTypeInfo("Config", null, "<!--", "-->") },
            { ".json", new FileTypeInfo("JSON", null, null, null) },

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
            { ".rs", new FileTypeInfo("Rust", "//", "/*", "*/") }
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

        public AnalysisResults AnalyzeFolder(string folderPath)
        {
            var results = new AnalysisResults();
            var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

            // Find solutions
            results.Solutions = FindSolutions(folderPath);

            // Analyze each file
            foreach (var filePath in allFiles)
            {
                var extension = Path.GetExtension(filePath).ToLower();
                if (_fileTypePatterns.TryGetValue(extension, out var fileTypeInfo))
                {
                    var fileAnalysis = AnalyzeFile(filePath, fileTypeInfo);
                    results.AddFile(fileAnalysis);
                }
                else
                {
                    // For unknown file types, just count them
                    results.AddFile(new FileAnalysis
                    {
                        Path = filePath,
                        Type = "Unknown",
                        TotalLines = CountLines(filePath),
                        SourceLines = 0,
                        CommentLines = 0,
                        BlankLines = 0
                    });
                }
            }

            return results;
        }

        private List<SolutionInfo> FindSolutions(string folderPath)
        {
            var solutions = new List<SolutionInfo>();

            foreach (var solutionPattern in _solutionFilePatterns)
            {
                foreach (var solutionFile in Directory.GetFiles(folderPath, solutionPattern,
                             SearchOption.AllDirectories))
                {
                    var solution = new SolutionInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(solutionFile),
                        Path = solutionFile,
                        Projects = new List<ProjectInfo>()
                    };

                    // Find projects for this solution
                    foreach (var projectType in _projectFilePatterns)
                    {
                        foreach (var projectPattern in projectType.Value)
                        {
                            foreach (var projectFile in Directory.GetFiles(Path.GetDirectoryName(solutionFile),
                                         projectPattern, SearchOption.AllDirectories))
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

        private FileAnalysis AnalyzeFile(string filePath, FileTypeInfo fileTypeInfo)
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
                lines = File.ReadAllLines(filePath);
            }
            catch (Exception)
            {
                // Skip files that can't be read
                return analysis;
            }

            analysis.TotalLines = lines.Length;

            foreach (var line in lines)
            {
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

        private int CountLines(string filePath)
        {
            try
            {
                return File.ReadAllLines(filePath).Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}