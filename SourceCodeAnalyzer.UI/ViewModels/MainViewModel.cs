using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using Microsoft.Win32;
using SourceCodeAnalyzer.Core.Models;
using SourceCodeAnalyzer.Core.Services;
using System.Windows.Threading;

namespace SourceCodeAnalyzer.UI.ViewModels;

public class MainViewModel : ViewModelBase, IProgressReporter
{
    #region - Private Declarations -
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isCancellationRequested;
    private readonly ISourceCodeAnalyzer _sourceCodeAnalyzer;
    private string _folderPath;
    private bool _isAnalyzing;
    private AnalysisResults _results = new AnalysisResults();
    private string _statusMessage;
    private int _currentProgress;
    private int _totalFiles;
    private ObservableCollection<FileAnalysis> _recentFiles = new ObservableCollection<FileAnalysis>();
    private ChartsViewModel _chartsViewModel;
    #endregion

    #region - Constructor -
    public MainViewModel()
    {
        _sourceCodeAnalyzer = new SourceCodeAnalyzerService();
        ChartsViewModel = new ChartsViewModel();
        BrowseCommand = new RelayCommand(BrowseFolder);
        AnalyzeCommand = new RelayCommand(async () => await AnalyzeFolderAsync(), CanAnalyze);
        CancelCommand = new RelayCommand(CancelAnalysis, () => IsAnalyzing && !IsCancellationRequested);
        AboutCommand = new RelayCommand(() => MessageBox.Show("Source Code Analyzer v1.0\n\nDeveloped by Kiran Kurapaty\n\nkkurapaty@outlook.com", "About", MessageBoxButton.OK, MessageBoxImage.Information));
    }
    #endregion

    #region - Properties -
    public bool IsCancellationRequested
    {
        get => _isCancellationRequested;
        set => SetProperty(ref _isCancellationRequested, value);
    }

    public int CurrentProgress
    {
        get => _currentProgress;
        set => SetProperty(ref _currentProgress, value);
    }

    public int TotalFiles
    {
        get => _totalFiles;
        set => SetProperty(ref _totalFiles, value);
    }

    public ObservableCollection<FileAnalysis> RecentFiles
    {
        get => _recentFiles;
        set => SetProperty(ref _recentFiles, value);
    }

    public ChartsViewModel ChartsViewModel
    {
        get => _chartsViewModel;
        set => SetProperty(ref _chartsViewModel, value);
    }

    public string FolderPath
    {
        get => _folderPath;
        set => SetProperty(ref _folderPath, value);
    }

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        set => SetProperty(ref _isAnalyzing, value);
    }

    public AnalysisResults Results
    {
        get => _results;
        set => SetProperty(ref _results, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    #endregion

    #region - Commands -
    public ICommand BrowseCommand { get; }
    public ICommand AnalyzeCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AboutCommand { get; }
    #endregion

    #region - Private Methods -
    private void BrowseFolder()
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog().GetValueOrDefault())
        {
            FolderPath = dialog.FolderName;
        }
    }

    private bool CanAnalyze() => !string.IsNullOrWhiteSpace(FolderPath) && !IsAnalyzing && !IsCancellationRequested;

    private async Task AnalyzeFolderAsync()
    {
        try
        {
            CurrentProgress = 0;
            IsAnalyzing = true;
            IsCancellationRequested = false;
            RecentFiles.Clear();
            Results = new AnalysisResults();

            _cancellationTokenSource = new CancellationTokenSource();

            await _sourceCodeAnalyzer.AnalyzeFolderAsync(FolderPath, this, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Handled in ReportCompletionAsync
        }
        catch (Exception ex)
        {
            await ReportCompletionAsync(false, $"Error: {ex.Message}", Results);
        }
    }

    private void CancelAnalysis()
    {
        IsCancellationRequested = true;
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Cancelling...";
    }

    #endregion

    #region - IProgressReporter Implementation -
    public async Task ReportProgressAsync(int current, int total, FileAnalysis result, CancellationToken ct)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CurrentProgress = current;
            TotalFiles = total;

            if (RecentFiles.Count >= 10)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }
            RecentFiles.Insert(0, result);

            OnPropertyChanged(nameof(Results));
        }, DispatcherPriority.Background, ct);
    }

    public async Task ReportMessageAsync(string message, CancellationToken ct)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusMessage = message;
        }, DispatcherPriority.Background, ct);
    }

    public async Task ReportCompletionAsync(bool isSuccess, string message, AnalysisResults results)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusMessage = message;
            if (isSuccess)
            {
                Results = results;
                ChartsViewModel.Results = results;
            }
            IsAnalyzing = false;
            IsCancellationRequested = false;
        });
    }
    #endregion
}