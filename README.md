# ZenDisk - Disk Space Analyzer

A fast and simple tool for analyzing disk space usage on Windows.

## Features

- 🔍 **Disk Space Analysis** - Scan drives and folders to determine space usage
- 📊 **Size Visualization** - Progress bars show relative sizes of files and folders
- 🌳 **Tree Structure** - Convenient file system display in tree format
- ⚡ **High Performance** - Asynchronous scanning with UI virtualization
- 🎯 **Size Sorting** - Automatic sorting by size (largest to smallest)

## Technical Features

- **.NET 9** - Modern development platform
- **WPF** - Native Windows UI with virtualization support
- **MVVM Pattern** - Clean architecture with separation of logic and presentation
- **Asynchronous** - Non-blocking scanning with cancellation support
- **Parallelism** - Limited parallelism for optimal performance

## Installation and Running

1. Make sure you have .NET 9 SDK installed
2. Clone the repository
3. Run the command:
   ```bash
   dotnet run
   ```

## Usage

1. **Select a drive** from the dropdown list
2. **Click "Scan Drive"** to start analysis
3. **Monitor progress** in real-time
4. **Explore results** in the tree structure
5. **View details** in the right panel

## Project Structure

```
ZenDisk/
├── Models/                 # Data models
│   ├── DirectoryItem.cs    # Directory model
│   ├── DriveListItem.cs    # Drive model
│   ├── FileItem.cs         # File model
│   └── FileSystemItem.cs   # Base class for files/folders/drives
├── Services/               # Business logic
│   ├── DiskScanner.cs      # Disk space scanner
│   └── FileSystemFilter.cs # Filtering and search
├── ViewModels/             # ViewModels for MVVM
│   └── MainViewModel.cs    # Main ViewModel
├── Converters/             # UI converters
│   └── ValueConverters.cs  # XAML converters
└── MainWindow.xaml         # Main application window
```

## Performance

The application is optimized for working with large drives:

- **TreeView Virtualization** - Only visible elements are displayed
- **Batch Processing** - Files are processed in batches of 100
- **Limited Parallelism** - Maximum 10 concurrent folder scans
- **Asynchronous** - UI remains responsive during scanning

## Welcome to Contribute

Everybody is welcome to contribute, no matter your experience level.
Please do not be shy about making mistakes - mistakes are part of learning, and review is here to help.

See `CONTRIBUTING.md` for contribution workflow and commit message guidelines.
