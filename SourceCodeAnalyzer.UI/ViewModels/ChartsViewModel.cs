using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using SourceCodeAnalyzer.Core.Models;

namespace SourceCodeAnalyzer.UI.ViewModels
{

    public class ChartsViewModel : ViewModelBase
    {
        private AnalysisResults _results;
        private string _selectedFileType;
        public ObservableCollection<string> AvailableFileTypes { get; set; } = new ObservableCollection<string>();
        public SeriesCollection FileTypeDetailSeries { get; set; }
        public List<string> FileTypeDetailLabels { get; set; }

        public string SelectedFileType
        {
            get => _selectedFileType;
            set
            {
                SetProperty(ref _selectedFileType, value);
                UpdateFileTypeDetailChart();
            }
        }
        public SeriesCollection ProjectFileDistributionSeries { get; set; }
        public SeriesCollection ProjectLineTypeSeries { get; set; }
        public SeriesCollection ProjectSourceLinesSeries { get; set; }

        public List<string> ProjectLabels { get; set; }
        public List<string> LineTypeLabels { get; set; } = new List<string> { "Source", "Comments", "Blank" };

        public Func<double, string> Formatter { get; set; }
        public Func<double, string> LineFormatter { get; set; }
        public AnalysisResults Results
        {
            get => _results;
            set
            {
                _results = value;
                UpdateCharts();
                OnPropertyChanged();
            }
        }

        public ChartsViewModel()
        {
            Formatter = value => value.ToString("N0");
            LineFormatter = value => value.ToString("N0");
            InitializeCharts();
        }

        private void InitializeCharts()
        {
            ProjectFileDistributionSeries = new SeriesCollection();
            ProjectLineTypeSeries = new SeriesCollection();
            ProjectSourceLinesSeries = new SeriesCollection();
            ProjectLabels = new List<string>();
            FileTypeDetailSeries = new SeriesCollection();
            FileTypeDetailLabels = new List<string>();
        }

        public void UpdateCharts()
        {
            if (Results?.SolutionSummaries == null) return;
            UpdateFileTypes();

            var allProjects = Results.SolutionSummaries
                .SelectMany(s => s.ProjectSummaries)
                .Where(p => p.TotalFiles > 0) // Ignore empty projects
                .OrderByDescending(p => p.SourceLines)
                .ToList();

            UpdateProjectFileDistributionChart(allProjects);
            UpdateProjectLineTypeChart(allProjects);
            UpdateProjectSourceLinesChart(allProjects);
        }

        private void UpdateFileTypes()
        {
            if (Results?.SolutionSummaries == null) return;

            // Populate available file types
            AvailableFileTypes.Clear();
            var fileTypes = Results.Files
                .GroupBy(f => f.Type)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();

            foreach (var type in fileTypes)
            {
                AvailableFileTypes.Add(type);
            }

            // Select first type by default
            if (AvailableFileTypes.Any() && SelectedFileType == null)
            {
                SelectedFileType = AvailableFileTypes.First();
            }
        }
        private void UpdateFileTypeDetailChart()
        {
            if (string.IsNullOrEmpty(SelectedFileType)) return;

            FileTypeDetailSeries.Clear();
            FileTypeDetailLabels.Clear();

            var fileTypeData = Results.Files
                .Where(f => f.Type == SelectedFileType)
                .OrderByDescending(f => f.TotalLines)
                .Take(20) // Limit to top 20 files for readability
                .ToList();

            if (!fileTypeData.Any()) return;

            // Add series for each metric
            FileTypeDetailSeries.Add(new StackedColumnSeries
            {
                Title = "Source Lines",
                Values = new ChartValues<int>(fileTypeData.Select(f => f.SourceLines)),
                Fill = Brushes.SteelBlue
            });

            FileTypeDetailSeries.Add(new StackedColumnSeries
            {
                Title = "Comment Lines",
                Values = new ChartValues<int>(fileTypeData.Select(f => f.CommentLines)),
                Fill = Brushes.Goldenrod
            });

            FileTypeDetailSeries.Add(new StackedColumnSeries
            {
                Title = "Blank Lines",
                Values = new ChartValues<int>(fileTypeData.Select(f => f.BlankLines)),
                Fill = Brushes.LightGray
            });

            // Set labels (file names)
            FileTypeDetailLabels.AddRange(fileTypeData.Select(f =>
                $"{Path.GetFileName(f.Path)}\n({f.TotalLines} lines)"));

            OnPropertyChanged(nameof(FileTypeDetailSeries));
            OnPropertyChanged(nameof(FileTypeDetailLabels));
        }
        
        private void UpdateProjectFileDistributionChart(List<ProjectSummary> projects)
        {
            ProjectFileDistributionSeries.Clear();

            foreach (var project in projects)
            {
                ProjectFileDistributionSeries.Add(new PieSeries
                {
                    Title = $"{project.Name} ({project.Type})",
                    Values = new ChartValues<int> { project.SourceFiles + project.ConfigFiles },
                    DataLabels = true,
                    LabelPoint = point => $"{point.Y} ({point.Participation:P1})",
                    Fill = GetProjectColor(project.Name)
                });
            }
        }

        private void UpdateProjectLineTypeChart(List<ProjectSummary> projects)
        {
            ProjectLineTypeSeries.Clear();
            ProjectLabels.Clear();

            // Add series for each line type
            ProjectLineTypeSeries.Add(new StackedColumnSeries
            {
                Title = "Source Lines",
                Values = new ChartValues<int>(projects.Select(p => p.SourceLines)),
                Fill = Brushes.SteelBlue
            });

            ProjectLineTypeSeries.Add(new StackedColumnSeries
            {
                Title = "Comment Lines",
                Values = new ChartValues<int>(projects.Select(p => p.CommentLines)),
                Fill = Brushes.Goldenrod
            });

            ProjectLineTypeSeries.Add(new StackedColumnSeries
            {
                Title = "Blank Lines",
                Values = new ChartValues<int>(projects.Select(p => p.BlankLines)),
                Fill = Brushes.LightGray
            });

            // Set project labels
            ProjectLabels.AddRange(projects.Select(p => $"{p.Name}\n({p.Type})"));
        }

        private void UpdateProjectSourceLinesChart(List<ProjectSummary> projects)
        {
            ProjectSourceLinesSeries.Clear();

            var lineSeries = new LineSeries
            {
                Title = "Source Lines",
                Values = new ChartValues<int>(projects.Select(p => p.SourceLines)),
                Fill = Brushes.Transparent,
                Stroke = Brushes.SteelBlue,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 10,
                PointForeground = Brushes.SteelBlue
            };

            ProjectSourceLinesSeries.Add(lineSeries);
        }

        private Brush GetProjectColor(string projectName)
        {
            // Generate consistent color based on project name hash
            var hash = projectName.GetHashCode();
            var r = (byte)(hash & 0xFF);
            var g = (byte)((hash >> 8) & 0xFF);
            var b = (byte)((hash >> 16) & 0xFF);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }

    public class ChartsViewModelOld : ViewModelBase
    {
        private AnalysisResults _results;

        public SeriesCollection LanguageDistributionSeries { get; set; }
        public SeriesCollection LineTypeSeries { get; set; }
        public SeriesCollection FileSizeDistributionSeries { get; set; }
        public SeriesCollection SolutionMetricsSeries { get; set; }
        public List<string> SolutionMetricsLabels { get; set; }

        public List<string> Languages { get; set; }
        public List<string> LineTypes { get; set; } = new List<string> { "Source", "Comments", "Blank" };

        public AnalysisResults Results
        {
            get => _results;
            set
            {
                _results = value;
                UpdateCharts();
                OnPropertyChanged();
            }
        }

        public ChartsViewModelOld()
        {
            InitializeCharts();
        }

        public SeriesCollection FileTypeMetricsSeries { get; set; }
        public List<string> FileTypeMetricsLabels { get; set; }

        private void InitializeCharts()
        {
            LanguageDistributionSeries = new SeriesCollection();
            LineTypeSeries = new SeriesCollection();
            FileSizeDistributionSeries = new SeriesCollection();
            FileTypeMetricsSeries = new SeriesCollection();
            FileTypeMetricsLabels = new List<string>();
            SolutionMetricsSeries = new SeriesCollection();
            SolutionMetricsLabels = new List<string>();
        }

        public void UpdateCharts()
        {
            UpdateFileTypeCharts();
            UpdateLineSeriesCharts();
            UpdateSolutionMetricsCharts();
        }
        private void UpdateFileTypeCharts()
        {
            if (Results.TotalFiles == 0) return;

            // File Type Metrics Bar Chart
            FileTypeMetricsSeries.Clear();
            FileTypeMetricsLabels.Clear();

            var topFileTypes = Results.Files
                .GroupBy(f => f.Type)
                .OrderByDescending(g => g.Sum(f => f.TotalLines))
                .Take(8) // Show top 8 file types
                .ToList();

            // Add series for each metric
            FileTypeMetricsSeries.Add(new ColumnSeries
            {
                Title = "Total Lines",
                Values = new ChartValues<int>(topFileTypes.Select(g => g.Sum(f => f.TotalLines)))
            });

            FileTypeMetricsSeries.Add(new ColumnSeries
            {
                Title = "Source Lines",
                Values = new ChartValues<int>(topFileTypes.Select(g => g.Sum(f => f.SourceLines)))
            });

            FileTypeMetricsSeries.Add(new ColumnSeries
            {
                Title = "Comment Lines",
                Values = new ChartValues<int>(topFileTypes.Select(g => g.Sum(f => f.CommentLines)))
            });

            FileTypeMetricsSeries.Add(new ColumnSeries
            {
                Title = "Blank Lines",
                Values = new ChartValues<int>(topFileTypes.Select(g => g.Sum(f => f.BlankLines)))
            });

            // Set labels
            FileTypeMetricsLabels.AddRange(topFileTypes.Select(g => g.Key));

            OnPropertyChanged(nameof(FileTypeMetricsSeries));
            OnPropertyChanged(nameof(FileTypeMetricsLabels));
        }

        private void UpdateLineSeriesCharts()
        {
            if (Results.TotalFiles == 0) return;

            // Language Distribution Pie Chart
            LanguageDistributionSeries.Clear();
            var languageGroups = Results.Files
                .GroupBy(f => f.Type)
                .OrderByDescending(g => g.Count())
                .Take(10);

            foreach (var group in languageGroups)
            {
                LanguageDistributionSeries.Add(new PieSeries
                {
                    Title = group.Key,
                    Values = new ChartValues<int> { group.Count() },
                    DataLabels = true
                });
            }

            // Line Types Stacked Bar Chart
            LineTypeSeries.Clear();
            LineTypeSeries.Add(new StackedColumnSeries
            {
                Title = "Source",
                Values = new ChartValues<int> { Results.TotalSourceLines }
            });
            LineTypeSeries.Add(new StackedColumnSeries
            {
                Title = "Comments",
                Values = new ChartValues<int> { Results.TotalCommentLines }
            });
            LineTypeSeries.Add(new StackedColumnSeries
            {
                Title = "Blank",
                Values = new ChartValues<int> { Results.TotalBlankLines }
            });

            // File Size Distribution
            FileSizeDistributionSeries.Clear();
            var sizeGroups = Results.Files
                .GroupBy(f => f.TotalLines switch
                {
                    < 100 => "Small (<100)",
                    >= 100 and < 500 => "Medium (100-500)",
                    >= 500 and < 1000 => "Large (500-1000)",
                    _ => "Very Large (1000+)"
                })
                .OrderBy(g => g.Key);

            foreach (var group in sizeGroups)
            {
                FileSizeDistributionSeries.Add(new ColumnSeries
                {
                    Title = group.Key,
                    Values = new ChartValues<int> { group.Count() }
                });
            }

            OnPropertyChanged(nameof(LanguageDistributionSeries));
            OnPropertyChanged(nameof(LineTypeSeries));
            OnPropertyChanged(nameof(FileSizeDistributionSeries));
        }

        private void UpdateSolutionMetricsCharts()
        {
            if (Results?.SolutionSummaries != null)
            {
                // Solution Metrics Chart
                SolutionMetricsSeries.Clear();
                SolutionMetricsLabels.Clear();

                foreach (var solution in Results.SolutionSummaries)
                {
                    SolutionMetricsLabels.Add(solution.Name);
                }

                SolutionMetricsSeries.Add(new ColumnSeries
                {
                    Title = "Source Files",
                    Values = new ChartValues<int>(Results.SolutionSummaries.Select(s => s.SourceFiles))
                });

                SolutionMetricsSeries.Add(new ColumnSeries
                {
                    Title = "Config Files",
                    Values = new ChartValues<int>(Results.SolutionSummaries.Select(s => s.ConfigFiles))
                });

                SolutionMetricsSeries.Add(new ColumnSeries
                {
                    Title = "Other Files",
                    Values = new ChartValues<int>(Results.SolutionSummaries.Select(s => s.OtherFiles))
                });

                OnPropertyChanged(nameof(SolutionMetricsSeries));
                OnPropertyChanged(nameof(SolutionMetricsLabels));
            }
        }
    }
}