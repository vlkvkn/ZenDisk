using ZenDisk.Models;

namespace ZenDisk.Services;

/// <summary>
/// Service for filtering file system items
/// </summary>
public static class FileSystemFilter
{
    /// <summary>
    /// Filters items by minimum size
    /// </summary>
    public static IEnumerable<FileSystemItem> FilterByMinSize(IEnumerable<FileSystemItem> items, long minSizeBytes)
    {
        return items.Where(item => item.Size >= minSizeBytes);
    }

    /// <summary>
    /// Filters items by file extension
    /// </summary>
    public static IEnumerable<FileSystemItem> FilterByExtension(IEnumerable<FileSystemItem> items, string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return items;

        var ext = extension.StartsWith(".") ? extension : $".{extension}";
        return items.Where(item => 
            !item.IsDirectory && 
            item.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters items by name pattern
    /// </summary>
    public static IEnumerable<FileSystemItem> FilterByName(IEnumerable<FileSystemItem> items, string namePattern)
    {
        if (string.IsNullOrEmpty(namePattern))
            return items;

        return items.Where(item => 
            item.Name.Contains(namePattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets top N largest items
    /// </summary>
    public static IEnumerable<FileSystemItem> GetTopLargest(IEnumerable<FileSystemItem> items, int count)
    {
        return items.OrderByDescending(item => item.Size).Take(count);
    }

    /// <summary>
    /// Gets only directories
    /// </summary>
    public static IEnumerable<FileSystemItem> GetDirectoriesOnly(IEnumerable<FileSystemItem> items)
    {
        return items.Where(item => item.IsDirectory);
    }

    /// <summary>
    /// Gets only files
    /// </summary>
    public static IEnumerable<FileSystemItem> GetFilesOnly(IEnumerable<FileSystemItem> items)
    {
        return items.Where(item => !item.IsDirectory);
    }
}
