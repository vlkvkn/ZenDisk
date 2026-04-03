using System.IO;

namespace ZenDisk.Models;

/// <summary>
/// Drive entry for UI lists: root path for scanning plus a readable label (letter + volume name).
/// </summary>
public sealed class DriveListItem
{
    public DriveListItem(string rootPath, string displayText)
    {
        RootPath = rootPath;
        DisplayText = displayText;
    }

    public string RootPath { get; }
    public string DisplayText { get; }

    public static DriveListItem FromDriveInfo(DriveInfo drive)
    {
        var letter = drive.Name.TrimEnd('\\', '/');
        var vol = drive.VolumeLabel?.Trim();
        var display = string.IsNullOrEmpty(vol) ? letter : $"{letter} {vol}";
        return new DriveListItem(drive.RootDirectory.FullName, display);
    }
}
