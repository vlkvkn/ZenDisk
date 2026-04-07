using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using Microsoft.Win32;
using ZenDisk.Models;
using ZenDisk.ViewModels;

namespace ZenDisk;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private FileSystemItem? _lastSelectedItem;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        
        // Initialize drive combo box
        DriveComboBox.ItemsSource = _viewModel.AvailableDrives;
        if (_viewModel.AvailableDrives.Any())
        {
            DriveComboBox.SelectedIndex = 0;
        }
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is FileSystemItem selectedItem)
        {
            _viewModel.SelectedItem = selectedItem;
            _lastSelectedItem = selectedItem;
        }
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select folder to analyze"
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.ScanCommand.Execute(dialog.FolderName);
        }
    }

    private void ShowAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };

        aboutWindow.ShowDialog();
    }

    private void TreeView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (FileSystemTreeView.SelectedItem is FileItem fileItem)
        {
            OpenFile(fileItem.FullPath);
        }
    }

    private void OpenFile(string filePath)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };
            
            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open file:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OpenFolder(string folderPath)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = folderPath,
                UseShellExecute = true
            };
            
            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open folder:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void DeleteItem(FileSystemItem item)
    {
        try
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete '{item.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Store the size before deletion
                var deletedSize = item.Size;
                
                if (item.IsDirectory)
                {
                    Directory.Delete(item.FullPath, true);
                }
                else
                {
                    File.Delete(item.FullPath);
                }

                // Remove from parent's children collection
                item.Parent?.Children.Remove(item);
                
                // Subtract the deleted size from all parent directories
                item.SubtractSizeFromParents(deletedSize);
                
                // Update status text by subtracting the deleted size
                _viewModel.UpdateStatusAfterDeletion(deletedSize);

                MessageBox.Show("Item deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to delete item:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void TreeView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            var selectedItems = GetSelectedItems().ToList();

            if (!selectedItems.Any() && _lastSelectedItem is not null)
            {
                selectedItems.Add(_lastSelectedItem);
            }

            if (!selectedItems.Any())
            {
                return;
            }

            if (selectedItems.Count == 1)
            {
                DeleteItem(selectedItems[0]);
            }
            else
            {
                DeleteItems(selectedItems);
            }
        }
    }

    private void ContextMenu_Open_Click(object sender, RoutedEventArgs e)
    {
        if (FileSystemTreeView.SelectedItem is FileItem fileItem)
        {
            OpenFile(fileItem.FullPath);
        }
    }

    private void ContextMenu_OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (FileSystemTreeView.SelectedItem is DirectoryItem directoryItem)
        {
            OpenFolder(directoryItem.FullPath);
        }
    }

    private void ContextMenu_Delete_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = GetSelectedItems().ToList();

        if (!selectedItems.Any() && _lastSelectedItem is not null)
        {
            selectedItems.Add(_lastSelectedItem);
        }

        if (!selectedItems.Any())
        {
            return;
        }

        if (selectedItems.Count == 1)
        {
            DeleteItem(selectedItems[0]);
        }
        else
        {
            DeleteItems(selectedItems);
        }
    }

    private void FileSystemTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        // Let the default TreeView behavior handle clicks on the expander toggle
        var expander = VisualUpwardSearch<System.Windows.Controls.Primitives.ToggleButton>(source);
        if (expander is not null)
        {
            return;
        }

        var treeViewItem = VisualUpwardSearch<TreeViewItem>(source);
        if (treeViewItem?.DataContext is not FileSystemItem clickedItem)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;

        if (modifiers == ModifierKeys.Shift && _lastSelectedItem is not null)
        {
            SelectRange(_lastSelectedItem, clickedItem);
        }
        else if (modifiers == ModifierKeys.Control)
        {
            clickedItem.IsSelected = !clickedItem.IsSelected;
            _lastSelectedItem = clickedItem.IsSelected ? clickedItem : _lastSelectedItem;
            _viewModel.SelectedItem = clickedItem;
        }
        else
        {
            ClearAllSelections();
            clickedItem.IsSelected = true;
            _lastSelectedItem = clickedItem;
            _viewModel.SelectedItem = clickedItem;
        }
    }

    private void SelectRange(FileSystemItem startItem, FileSystemItem endItem)
    {
        if (startItem == endItem)
        {
            ClearAllSelections();
            startItem.IsSelected = true;
            _lastSelectedItem = startItem;
            _viewModel.SelectedItem = startItem;
            return;
        }

        ObservableCollection<FileSystemItem> collection;

        if (startItem.Parent == endItem.Parent)
        {
            collection = startItem.Parent?.Children ?? _viewModel.RootItems;
        }
        else if (startItem.Parent is null && endItem.Parent is null)
        {
            collection = _viewModel.RootItems;
        }
        else
        {
            ClearAllSelections();
            endItem.IsSelected = true;
            _lastSelectedItem = endItem;
            _viewModel.SelectedItem = endItem;
            return;
        }

        var startIndex = collection.IndexOf(startItem);
        var endIndex = collection.IndexOf(endItem);

        if (startIndex == -1 || endIndex == -1)
        {
            return;
        }

        if (endIndex < startIndex)
        {
            (startIndex, endIndex) = (endIndex, startIndex);
        }

        ClearAllSelections();

        for (var i = startIndex; i <= endIndex; i++)
        {
            collection[i].IsSelected = true;
        }

        _lastSelectedItem = endItem;
        _viewModel.SelectedItem = endItem;
    }

    private static T? VisualUpwardSearch<T>(DependencyObject source) where T : DependencyObject
    {
        while (source != null && source is not T)
        {
            source = VisualTreeHelper.GetParent(source);
        }

        return source as T;
    }

    private IEnumerable<FileSystemItem> GetSelectedItems()
    {
        IEnumerable<FileSystemItem> Traverse(IEnumerable<FileSystemItem> items)
        {
            foreach (var item in items)
            {
                if (item.IsSelected)
                {
                    yield return item;
                }

                if (item.Children.Any())
                {
                    foreach (var child in Traverse(item.Children))
                    {
                        yield return child;
                    }
                }
            }
        }

        return Traverse(_viewModel.RootItems);
    }

    private void ClearAllSelections()
    {
        void Clear(IEnumerable<FileSystemItem> items)
        {
            foreach (var item in items)
            {
                item.IsSelected = false;
                if (item.Children.Any())
                {
                    Clear(item.Children);
                }
            }
        }

        Clear(_viewModel.RootItems);
    }

    private void DeleteItems(IEnumerable<FileSystemItem> items)
    {
        var itemList = items.Distinct().ToList();
        if (!itemList.Any())
        {
            return;
        }

        var itemSet = new HashSet<FileSystemItem>(itemList);

        bool HasSelectedAncestor(FileSystemItem item)
        {
            var parent = item.Parent;
            while (parent is not null)
            {
                if (itemSet.Contains(parent))
                {
                    return true;
                }

                parent = parent.Parent;
            }

            return false;
        }

        var topLevelItems = itemList
            .Where(item => !HasSelectedAncestor(item))
            .ToList();

        if (!topLevelItems.Any())
        {
            return;
        }

        var totalSize = topLevelItems.Sum(i => i.Size);

        var confirmationMessage = topLevelItems.Count == 1
            ? $"Are you sure you want to delete '{topLevelItems[0].Name}'?"
            : $"Are you sure you want to delete {topLevelItems.Count} items (total size: {FileSystemItem.FormatSize(totalSize)})?";

        var result = MessageBox.Show(
            confirmationMessage,
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        long deletedTotalSize = 0;

        foreach (var item in topLevelItems)
        {
            try
            {
                var deletedSize = item.Size;

                if (item.IsDirectory)
                {
                    Directory.Delete(item.FullPath, true);
                }
                else
                {
                    File.Delete(item.FullPath);
                }

                item.Parent?.Children.Remove(item);
                item.SubtractSizeFromParents(deletedSize);
                deletedTotalSize += deletedSize;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to delete '{item.Name}':\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        if (deletedTotalSize > 0)
        {
            _viewModel.UpdateStatusAfterDeletion(deletedTotalSize);
            MessageBox.Show(
                topLevelItems.Count == 1 ? "Item deleted successfully." : "Items deleted successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        ClearAllSelections();
        _lastSelectedItem = null;
        _viewModel.SelectedItem = null;
    }
}