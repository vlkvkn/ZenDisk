using System.Collections.Concurrent;
using System.IO;
using ZenDisk.Models;

namespace ZenDisk.Services;

/// <summary>
/// Service for scanning disk space usage
/// </summary>
public class DiskScanner
{
    private readonly ConcurrentDictionary<string, DirectoryItem> _directoryCache = new();
    private CancellationTokenSource? _cancellationTokenSource;

    const int MaxDegreeOfParallelism = 10;

    public event EventHandler<ScanCompletedEventArgs>? ScanCompleted;

    public bool IsScanning { get; private set; }
    public DirectoryItem? RootDirectory { get; private set; }

    public async Task<DirectoryItem> ScanDirectoryAsync(string path, IProgress<ScanProgressEventArgs>? progress = null)
        => await ScanDirectoryAsync(path, false, null, progress);

    public async Task<DirectoryItem> ScanDirectoryAsync(string path, bool isDriveRoot, IProgress<ScanProgressEventArgs>? progress = null)
        => await ScanDirectoryAsync(path, isDriveRoot, null, progress);

    public async Task<DirectoryItem> ScanDirectoryAsync(string path, bool isDriveRoot, long? driveCapacityBytes, IProgress<ScanProgressEventArgs>? progress = null)
    {
        if (IsScanning)
        {
            throw new InvalidOperationException("Scan is already in progress");
        }

        IsScanning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            var directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }

            RootDirectory = new DirectoryItem(directoryInfo, isDriveRoot, driveCapacityBytes);
            await ScanDirectoryRecursiveAsync(RootDirectory, cancellationToken, progress);
            
            ScanCompleted?.Invoke(this, new ScanCompletedEventArgs(RootDirectory));
            return RootDirectory;
        }
        finally
        {
            IsScanning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public async Task<DirectoryItem> ScanDriveAsync(DriveInfo drive, IProgress<ScanProgressEventArgs>? progress = null)
    {
        long? capacityBytes = null;
        try
        {
            // TotalSize can throw when drive is not ready.
            capacityBytes = drive.TotalSize;
        }
        catch
        {
            capacityBytes = null;
        }

        return await ScanDirectoryAsync(drive.RootDirectory.FullName, true, capacityBytes, progress);
    }

    public void CancelScan()
    {
        _cancellationTokenSource?.Cancel();
    }

    private async Task ScanDirectoryRecursiveAsync(DirectoryItem directoryItem, CancellationToken cancellationToken, IProgress<ScanProgressEventArgs>? progress)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(directoryItem.FullPath);
            var totalItems = 0;
            var processedItems = 0;

            // Count total items first for progress calculation
            try
            {
                totalItems = directoryInfo.GetDirectories().Length + directoryInfo.GetFiles().Length;
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
                return;
            }

            // Process directories in parallel for better performance
            DirectoryInfo[] directories;
            try
            {
                directories = directoryInfo.GetDirectories();
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            var directoryTasks = new List<Task>();
            
            foreach (var dir in directories)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                try
                {
                    var childDirectory = new DirectoryItem(dir);
                    directoryItem.AddChild(childDirectory);
                    
                    // Process directories with limited parallelism to avoid overwhelming the system
                    if (directoryTasks.Count < MaxDegreeOfParallelism) // Limit concurrent directory scans
                    {
                        var task = Task.Run(async () =>
                        {
                            await ScanDirectoryRecursiveAsync(childDirectory, cancellationToken, progress);
                            childDirectory.CalculateSize();
                        }, cancellationToken);
                        directoryTasks.Add(task);
                    }
                    else
                    {
                        await ScanDirectoryRecursiveAsync(childDirectory, cancellationToken, progress);
                        childDirectory.CalculateSize();
                    }
                    
                    processedItems++;
                    progress?.Report(new ScanProgressEventArgs(processedItems, totalItems, directoryItem.FullPath));
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip directories we can't access
                    processedItems++;
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    // Skip directories that no longer exist
                    processedItems++;
                    continue;
                }
            }

            // Wait for all directory tasks to complete
            if (directoryTasks.Any())
            {
                await Task.WhenAll(directoryTasks);
            }

            // Process files in batches for better performance
            FileInfo[] files;
            try
            {
                files = directoryInfo.GetFiles();
            }
            catch (UnauthorizedAccessException)
            {
                directoryItem.CalculateSize();
                directoryItem.SortChildren();
                return;
            }

            const int batchSize = 100;
            
            for (int i = 0; i < files.Length; i += batchSize)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var batch = files.Skip(i).Take(batchSize);
                foreach (var file in batch.OrderByDescending(f => f.Length))
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    try
                    {
                        var fileItem = new FileItem(file);
                        directoryItem.AddChild(fileItem);
                        
                        processedItems++;
                        progress?.Report(new ScanProgressEventArgs(processedItems, totalItems, directoryItem.FullPath));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Skip files we can't access
                        processedItems++;
                        continue;
                    }
                    catch (FileNotFoundException)
                    {
                        // Skip files that no longer exist
                        processedItems++;
                        continue;
                    }
                }

                // Yield control to prevent UI freezing
                await Task.Yield();
            }

            // Calculate directory size
            directoryItem.CalculateSize();
            directoryItem.SortChildren();
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
        }
        catch (DirectoryNotFoundException)
        {
            // Skip directories that no longer exist
        }
    }

    public static DriveInfo[] GetAvailableDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .ToArray();
    }

    public static DriveListItem[] GetAvailableDriveListItems()
    {
        return GetAvailableDrives()
            .Select(DriveListItem.FromDriveInfo)
            .ToArray();
    }
}

/// <summary>
/// Event args for scan progress updates
/// </summary>
public class ScanProgressEventArgs : EventArgs
{
    public int ProcessedItems { get; }
    public int TotalItems { get; }
    public string CurrentPath { get; }
    public double ProgressPercentage => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;

    public ScanProgressEventArgs(int processedItems, int totalItems, string currentPath)
    {
        ProcessedItems = processedItems;
        TotalItems = totalItems;
        CurrentPath = currentPath;
    }
}

/// <summary>
/// Event args for scan completion
/// </summary>
public class ScanCompletedEventArgs : EventArgs
{
    public DirectoryItem RootDirectory { get; }
    public bool WasCancelled { get; }

    public ScanCompletedEventArgs(DirectoryItem rootDirectory, bool wasCancelled = false)
    {
        RootDirectory = rootDirectory;
        WasCancelled = wasCancelled;
    }
}
