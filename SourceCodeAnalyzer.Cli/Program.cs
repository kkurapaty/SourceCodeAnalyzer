using System;
using System.IO;
using System.Linq;

namespace SourceCodeAnalyzer.Cli
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Source Code Analyzer");
            Console.WriteLine("====================");

            //if (args.Length == 0)
            //{
            //    Console.WriteLine("Please provide a folder path as an argument.");
            //    return;
            //}

            string folderPath = @"D:\Dev\OnMusic";//args[0];
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"The folder '{folderPath}' does not exist.");
                return;
            }

            AnalyzeSourceCode(folderPath);
        }

        private static void AnalyzeSourceCode(string folderPath)
        {
            var analyzer = new SourceCodeAnalyzer();
            var results = analyzer.AnalyzeFolder(folderPath);
            Console.WriteLine("\nAnalysis Results:");
            Console.WriteLine("================\n");

            // Print solution/project statistics
            Console.WriteLine("Solutions and Projects:");
            foreach (var solution in results.Solutions)
            {
                Console.WriteLine($"Solution: {solution.Name}");
                Console.WriteLine($"    Path: {solution.Path}");
                Console.WriteLine($"Projects: {solution.Projects.Count}");
                foreach (var project in solution.Projects)
                {
                    Console.WriteLine($"    Project: {project.Name} ({project.Type})");
                    Console.WriteLine($"       Path: {project.Path}");
                }
                Console.WriteLine();
            }

            // Print file type statistics
            Console.WriteLine("\nFile Statistics by Type:");
            foreach (var group in results.Files.GroupBy(f => f.Type).OrderByDescending(g => g.Count()))
            {
                Console.WriteLine($"{group.Key}: {group.Count()} files");
            }

            // Print line statistics
            Console.WriteLine("\nLine Statistics:");
            Console.WriteLine($"Total Files: {results.TotalFiles:##.###}");
            Console.WriteLine($"Total Lines: {results.TotalLines:##.###}");
            Console.WriteLine($"Source Lines: {results.TotalSourceLines:##.###}");
            Console.WriteLine($"Comment Lines: {results.TotalCommentLines:##.###}");
            Console.WriteLine($"Blank Lines: {results.TotalBlankLines:##.###}");
        }
    }
}