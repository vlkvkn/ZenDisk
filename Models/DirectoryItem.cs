using System.IO;

namespace ZenDisk.Models;

/// <summary>
/// Represents a directory in the file system
/// </summary>
public class DirectoryItem : FileSystemItem
{
    private readonly bool _isDriveRoot;
    private readonly long? _driveCapacityBytes;

    public override bool IsDirectory => true;
    public override string Icon => _isDriveRoot ? "\uEDA2" : "\uED25"; // Drive or folder icon
    
    public override bool IsDriveRoot => _isDriveRoot;

    // For drive roots, show progress as % of the whole drive capacity.
    public override double SizePercentage
        => _isDriveRoot && _driveCapacityBytes.HasValue && _driveCapacityBytes.Value > 0
            ? (double)Size / _driveCapacityBytes.Value * 100
            : base.SizePercentage;

    public override string DriveCapacityFormatted
        => _isDriveRoot && _driveCapacityBytes.HasValue && _driveCapacityBytes.Value > 0
            ? FormatSize(_driveCapacityBytes.Value)
            : string.Empty;

    public DirectoryItem(string name, string fullPath, bool isDriveRoot = false, long? driveCapacityBytes = null)
    {
        Name = name;
        FullPath = fullPath;
        _isDriveRoot = isDriveRoot;
        _driveCapacityBytes = driveCapacityBytes;
    }

    public DirectoryItem(DirectoryInfo directoryInfo, bool isDriveRoot = false, long? driveCapacityBytes = null)
    {
        Name = directoryInfo.Name;
        FullPath = directoryInfo.FullName;
        _isDriveRoot = isDriveRoot;
        _driveCapacityBytes = driveCapacityBytes;
    }

    public void CalculateSize()
    {
        Size = 0;
        foreach (var child in Children)
        {
            Size += child.Size;
        }
    }
}
