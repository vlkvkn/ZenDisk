using System.IO;

namespace ZenDisk.Models;

/// <summary>
/// Represents a file in the file system
/// </summary>
public class FileItem : FileSystemItem
{
    public override bool IsDirectory => false;
    public string Extension { get; set; } = string.Empty;
    
    public override string Icon => GetIconByExtension(Extension);

    public FileItem(string name, string fullPath, long size)
    {
        Name = name;
        FullPath = fullPath;
        Size = size;
        Extension = Path.GetExtension(name).ToLowerInvariant();
    }

    public FileItem(FileInfo fileInfo)
    {
        Name = fileInfo.Name;
        FullPath = fileInfo.FullName;
        Size = fileInfo.Length;
        Extension = fileInfo.Extension.ToLowerInvariant();
    }

    private static string GetIconByExtension(string extension)
    {
        return extension switch
        {
            // Images
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" or ".webp" or ".svg" => "\uEB9F", // Image icon
            // Videos
            ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" or ".m4v" => "\uE714", // Video icon
            // Audio
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".wma" or ".m4a" => "\uE8D6", // Music icon
            // Documents
            ".pdf" => "\uEA90", // PDF icon
            ".doc" or ".docx" => "\uE8F5", // Word icon
            ".xls" or ".xlsx" => "\uE8F1", // Excel icon
            ".ppt" or ".pptx" => "\uE8F9", // PowerPoint icon
            ".txt" or ".rtf" => "\uE8A5", // Document icon
            // Archives
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" => "\uE8E5", // Archive icon
            // Code files
            ".cs" or ".cpp" or ".c" or ".h" or ".hpp" or ".java" or ".py" or ".js" or ".ts" or ".html" or ".css" or ".xml" or ".json" or ".yaml" or ".yml" => "\uE943", // Code icon
            // Executables
            ".exe" or ".msi" or ".dll" or ".sys" => "\uE7C3", // Application icon
            // Default file icon
            _ => "\uE8A5" // Document icon
        };
    }
}
