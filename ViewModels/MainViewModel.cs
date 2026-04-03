using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using ZenDisk.Models;
using ZenDisk.Services;

namespace ZenDisk.ViewModels;

/// <summary>
/// Main view model for the application
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly DiskScanner _diskScanner;
    private FileSystemItem? _selectedItem;
    private string _statusText = "Ready";
    private double _progressValue;
    private bool _isScanning;
    private string _currentPath = string.Empty;

    public MainViewModel()
    {
        _diskScanner = new DiskScanner();
        _diskScanner.ScanCompleted += OnScanCompleted;

        AvailableDrives = new ObservableCollection<DriveListItem>(DiskScanner.GetAvailableDriveListItems());
        RootItems = new ObservableCollection<FileSystemItem>();

        ScanCommand = new RelayCommand<string>(async (path) => await ScanPathAsync(path), CanScan);
        CancelScanCommand = new RelayCommand(CancelScan, () => IsScanning);
        RefreshDrivesCommand = new RelayCommand(RefreshDrives);
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
    }

    public ObservableCollection<DriveListItem> AvailableDrives { get; }
    public ObservableCollection<FileSystemItem> RootItems { get; }

    public FileSystemItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged(nameof(SelectedItem));
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set
        {
            _progressValue = value;
            OnPropertyChanged(nameof(ProgressValue));
        }
    }

    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            _isScanning = value;
            OnPropertyChanged(nameof(IsScanning));
            OnPropertyChanged(nameof(IsNotScanning));
            ((RelayCommand<string>)ScanCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelScanCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// True when not scanning (for enabling/disabling UI that should be inactive during scan).
    /// </summary>
    public bool IsNotScanning => !IsScanning;

    public string CurrentPath
    {
        get => _currentPath;
        set
        {
            _currentPath = value;
            OnPropertyChanged(nameof(CurrentPath));
        }
    }

    public ICommand ScanCommand { get; }
    public ICommand CancelScanCommand { get; }
    public ICommand RefreshDrivesCommand { get; }
    public ICommand BrowseFolderCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task ScanPathAsync(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            IsScanning = true;
            StatusText = "Scanning...";
            ProgressValue = 0;
            RootItems.Clear();

            // Detect if the path is a drive root (e.g., "C:\")
            var isDriveRoot = false;
            try
            {
                var root = Path.GetPathRoot(path);
                if (!string.IsNullOrEmpty(root) &&
                    string.Equals(root.TrimEnd('\\'), path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    isDriveRoot = true;
                }
            }
            catch
            {
                // If anything goes wrong, just treat it as a normal directory
                isDriveRoot = false;
            }

            var progress = new Progress<ScanProgressEventArgs>(OnProgressUpdate);
            DirectoryItem result;
            if (isDriveRoot)
            {
                // We need the drive capacity to show drive-root percent correctly.
                var driveRoot = Path.GetPathRoot(path);
                try
                {
                    if (!string.IsNullOrEmpty(driveRoot))
                    {
                        var drive = new DriveInfo(driveRoot);
                        result = drive.IsReady
                            ? await _diskScanner.ScanDriveAsync(drive, progress)
                            : await _diskScanner.ScanDirectoryAsync(path, true, progress);
                    }
                    else
                    {
                        result = await _diskScanner.ScanDirectoryAsync(path, true, progress);
                    }
                }
                catch
                {
                    result = await _diskScanner.ScanDirectoryAsync(path, true, progress);
                }
            }
            else
            {
                result = await _diskScanner.ScanDirectoryAsync(path, false, progress);
            }
            
            RootItems.Add(result);
            StatusText = $"Scan completed. Total size: {result.FormattedSize}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            CurrentPath = string.Empty;
            IsScanning = false;
        }
    }

    private bool CanScan(string? path)
    {
        return !IsScanning && !string.IsNullOrEmpty(path);
    }

    private void CancelScan()
    {
        _diskScanner.CancelScan();
        CurrentPath = string.Empty;
        IsScanning = false;
        StatusText = "Scan cancelled";
    }

    private void RefreshDrives()
    {
        AvailableDrives.Clear();
        foreach (var item in DiskScanner.GetAvailableDriveListItems())
        {
            AvailableDrives.Add(item);
        }
    }

    private void BrowseFolder()
    {
        // This will be handled in the code-behind
    }


    private void OnProgressUpdate(ScanProgressEventArgs e)
    {
        if (!IsScanning)
            return;

        ProgressValue = e.ProgressPercentage;
        CurrentPath = e.CurrentPath;
        StatusText = $"Scanning... {e.ProcessedItems}/{e.TotalItems} items";
    }

    private void OnScanCompleted(object? sender, ScanCompletedEventArgs e)
    {
        if (!e.WasCancelled)
        {
            StatusText = $"Scan completed. Total size: {e.RootDirectory.FormattedSize}";
        }
    }

    /// <summary>
    /// Updates the status text with the current total size
    /// </summary>
    public void UpdateStatusAfterDeletion()
    {
        if (RootItems.Any())
        {
            var totalSize = RootItems.Sum(item => item.Size);
            var formattedSize = FileSystemItem.FormatSize(totalSize);
            StatusText = $"Scan completed. Total size: {formattedSize}";
        }
    }

    /// <summary>
    /// Updates the status text by subtracting the specified size from the current total
    /// </summary>
    /// <param name="sizeToSubtract">Size to subtract in bytes</param>
    public void UpdateStatusAfterDeletion(long sizeToSubtract)
    {
        if (RootItems.Any())
        {
            var currentTotalSize = RootItems.Sum(item => item.Size);
            var newTotalSize = currentTotalSize - sizeToSubtract;
            var formattedSize = FileSystemItem.FormatSize(newTotalSize);
            StatusText = $"Scan completed. Total size: {formattedSize}";
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Simple relay command implementation
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? (() => true);
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute();

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// Relay command with parameter
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool> _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? (_ => true);
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute((T?)parameter);

    public void Execute(object? parameter) => _execute((T?)parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
