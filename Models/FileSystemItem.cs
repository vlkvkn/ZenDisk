using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ZenDisk.Models;

/// <summary>
/// Base class for file system items (files and directories)
/// </summary>
public abstract class FileSystemItem : INotifyPropertyChanged
{
    private long _size;
    private bool _isExpanded;
    private bool _isSelected;

    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public FileSystemItem? Parent { get; set; }
    public ObservableCollection<FileSystemItem> Children { get; set; } = new();

    public long Size
    {
        get => _size;
        set
        {
            _size = value;
            OnPropertyChanged(nameof(Size));
            OnPropertyChanged(nameof(FormattedSize));
            OnPropertyChanged(nameof(SizePercentage));
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            OnPropertyChanged(nameof(IsExpanded));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public string FormattedSize => FormatSize(Size);
    
    // Percent used by the tree progress bar.
    // Default behavior: relative to parent size (0 for root unless overridden by derived types).
    public virtual double SizePercentage => Parent?.Size > 0 ? (double)Size / Parent.Size * 100 : 0;

    // True only for drive root items (e.g., "C:\") scanned from the drive selector.
    public virtual bool IsDriveRoot => false;

    // Full drive capacity (only meaningful when IsDriveRoot == true).
    public virtual string DriveCapacityFormatted => string.Empty;

    public abstract bool IsDirectory { get; }
    public abstract string Icon { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static string FormatSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }

    public void AddChild(FileSystemItem child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public void SortChildren()
    {
        var sortedChildren = Children.OrderByDescending(c => c.Size).ToList();
        Children.Clear();
        foreach (var child in sortedChildren)
        {
            Children.Add(child);
        }
    }

    /// <summary>
    /// Recursively updates the size of this item and all its parents
    /// </summary>
    public void UpdateSizeRecursively()
    {
        if (IsDirectory && this is DirectoryItem directoryItem)
        {
            directoryItem.CalculateSize();
        }
        
        // Update parent sizes recursively
        var currentParent = Parent;
        while (currentParent != null)
        {
            if (currentParent.IsDirectory && currentParent is DirectoryItem parentDirectory)
            {
                parentDirectory.CalculateSize();
            }
            currentParent = currentParent.Parent;
        }
    }

    /// <summary>
    /// Subtracts the specified size from this item and all its parents
    /// </summary>
    /// <param name="sizeToSubtract">Size to subtract in bytes</param>
    public void SubtractSizeFromParents(long sizeToSubtract)
    {
        // Subtract from all parent directories
        var currentParent = Parent;
        while (currentParent != null)
        {
            if (currentParent.IsDirectory)
            {
                currentParent.Size -= sizeToSubtract;
            }
            currentParent = currentParent.Parent;
        }
    }
}
