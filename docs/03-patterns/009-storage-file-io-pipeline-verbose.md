---
tier: advanced
topic: platform
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 009-storage-file-io-pipeline.md
---

# 009V — Storage & File I/O Pipeline: An In-Depth Companion

You should already have read: [009 — Storage & File I/O Pipeline](009-storage-file-io-pipeline.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Understanding IStorageProvider

Avalonia's `IStorageProvider` is the platform abstraction for file picking and folder access. It is available from any `TopLevel` (Window, Popup, etc.).

| Platform | Underlying API |
|---|---|
| Windows | WinRT `FileOpenPicker` / `FileSavePicker` (COM) |
| macOS | `NSOpenPanel` / `NSSavePanel` |
| Linux | Gtk `FileChooserDialog` (via portal) |
| Browser | `<input type="file">` / `showSaveFilePicker()` |

The `FileOperationService` wraps `IStorageProvider` to provide a simpler API for text-file operations while exposing the full `IStorageFile` for binary or custom serialization needs.

---

## 2. FileOperationService — Complete Implementation

```csharp
public interface IFileOperationService
{
    // Text helpers
    Task<string?> OpenTextFileAsync(string title, string? filter = null, CancellationToken ct = default);
    Task<bool> SaveTextFileAsync(string content, string title, string? defaultName = null, string? filter = null, CancellationToken ct = default);

    // File pickers
    Task<IStorageFile?> OpenFileAsync(IReadOnlyList<FilePickerFileType>? filters = null, CancellationToken ct = default);
    Task<IStorageFile?> SaveFileAsync(string? suggestedName = null, IReadOnlyList<FilePickerFileType>? filters = null, CancellationToken ct = default);
    Task<IReadOnlyList<IStorageFile>> OpenMultipleFilesAsync(IReadOnlyList<FilePickerFileType>? filters = null, CancellationToken ct = default);

    // Folder pickers
    Task<IStorageFolder?> OpenFolderAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IStorageFolder>> OpenMultipleFoldersAsync(CancellationToken ct = default);

    // Bookmark management
    Task<string?> SaveBookmarkAsync(IStorageBookmarkItem item, CancellationToken ct = default);
    Task<IStorageFolder?> RestoreBookmarkAsync(string bookmarkId, CancellationToken ct = default);
    Task ReleaseBookmarkAsync(string bookmarkId);

    // Binary helpers
    Task<byte[]?> ReadAllBytesAsync(IStorageFile file, CancellationToken ct = default);
    Task<bool> WriteAllBytesAsync(IStorageFile file, byte[] data, CancellationToken ct = default);
}

public sealed class FileOperationService : IFileOperationService
{
    private readonly TopLevel _topLevel;
    private IStorageProvider Storage => _topLevel.StorageProvider;

    public FileOperationService(TopLevel topLevel)
    {
        _topLevel = topLevel ?? throw new ArgumentNullException(nameof(topLevel));
    }

    // ─── Text Helpers ────────────────────────────────────────

    public async Task<string?> OpenTextFileAsync(
        string title, string? filter = null, CancellationToken ct = default)
    {
        var file = await OpenFileAsync(
            filter is not null
                ? [new FilePickerFileType("Documents") { Patterns = [filter] }]
                : null,
            ct);

        if (file is null) return null;

        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct);
    }

    public async Task<bool> SaveTextFileAsync(
        string content, string title, string? defaultName = null,
        string? filter = null, CancellationToken ct = default)
    {
        var file = await SaveFileAsync(defaultName,
            filter is not null
                ? [new FilePickerFileType("Documents") { Patterns = [filter] }]
                : null,
            ct);

        if (file is null) return false;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content.AsMemory(), ct);
        return true;
    }

    // ─── File Pickers ────────────────────────────────────────

    public async Task<IStorageFile?> OpenFileAsync(
        IReadOnlyList<FilePickerFileType>? filters = null, CancellationToken ct = default)
    {
        var result = await Storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = false,
            FileTypeFilter = filters
        });
        return result?.FirstOrDefault();
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenMultipleFilesAsync(
        IReadOnlyList<FilePickerFileType>? filters = null, CancellationToken ct = default)
    {
        var result = await Storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Files",
            AllowMultiple = true,
            FileTypeFilter = filters
        });
        return result;
    }

    public async Task<IStorageFile?> SaveFileAsync(
        string? suggestedName = null,
        IReadOnlyList<FilePickerFileType>? filters = null,
        CancellationToken ct = default)
    {
        var options = new FilePickerSaveOptions
        {
            Title = "Save File",
            SuggestedFileName = suggestedName,
            DefaultExtension = filters?.FirstOrDefault()?.Patterns?.FirstOrDefault(),
            FileTypeChoices = filters
        };

        // If no filters specified, add "All Files" as a fallback
        if (options.FileTypeChoices is null || options.FileTypeChoices.Count == 0)
        {
            options.FileTypeChoices = new[]
            {
                new FilePickerFileType("All Files") { Patterns = ["*.*"] }
            };
        }

        return await Storage.SaveFilePickerAsync(options);
    }

    // ─── Folder Pickers ──────────────────────────────────────

    public async Task<IStorageFolder?> OpenFolderAsync(CancellationToken ct = default)
    {
        var result = await Storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Folder",
            AllowMultiple = false
        });
        return result?.FirstOrDefault();
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenMultipleFoldersAsync(CancellationToken ct = default)
    {
        var result = await Storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Open Folders",
            AllowMultiple = true
        });
        return result;
    }

    // ─── Bookmark Management ─────────────────────────────────

    public async Task<string?> SaveBookmarkAsync(IStorageBookmarkItem item, CancellationToken ct = default)
    {
        return await item.SaveBookmarkAsync();
    }

    public async Task<IStorageFolder?> RestoreBookmarkAsync(string bookmarkId, CancellationToken ct = default)
    {
        return await Storage.OpenStorageBookmarkAsync(bookmarkId);
    }

    public async Task ReleaseBookmarkAsync(string bookmarkId)
    {
        await Storage.ReleaseStorageBookmarkAsync(bookmarkId);
    }

    // ─── Binary Helpers ──────────────────────────────────────

    public async Task<byte[]?> ReadAllBytesAsync(IStorageFile file, CancellationToken ct = default)
    {
        await using var stream = await file.OpenReadAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);
        return memoryStream.ToArray();
    }

    public async Task<bool> WriteAllBytesAsync(IStorageFile file, byte[] data, CancellationToken ct = default)
    {
        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(data.AsMemory(), ct);
        return true;
    }
}
```

---

## 3. DI Registration — Multiple Strategies

### Strategy 1: Singleton Resolved from MainWindow

```csharp
// Program.cs
services.AddSingleton<IFileOperationService>(sp =>
{
    var topLevel = TopLevel.GetTopLevel(Application.Current?.MainWindow)!;
    return new FileOperationService(topLevel);
});
```

**Caveat:** If `MainWindow` is null at registration time (e.g. during `OnFrameworkInitializationCompleted`), defer resolution:

```csharp
// Lazy singleton — resolves TopLevel on first access
services.AddSingleton<IFileOperationService>(_ =>
{
    var lifetime = Application.Current?.ApplicationLifetime;
    if (lifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var mainWindow = desktop.MainWindow;
        return new FileOperationService(mainWindow);
    }
    throw new InvalidOperationException("FileOperationService requires a desktop lifetime");
});
```

### Strategy 2: Scoped to Each Window

```csharp
// Each window gets its own service instance bound to its TopLevel
services.AddScoped<IFileOperationService>(sp =>
{
    var window = sp.GetRequiredService<Window>();
    return new FileOperationService(window);
});
```

### Strategy 3: Singleton with Fallback TopLevel

For applications that may not have a window at the time of service call (e.g., background tasks), use all open windows as fallback:

```csharp
public sealed class FallbackFileOperationService : IFileOperationService
{
    private TopLevel? GetTopLevel()
    {
        var lifetime = Application.Current?.ApplicationLifetime;
        if (lifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Try active window first, then main window
            var active = desktop.Windows.FirstOrDefault(w => w.IsActive);
            if (active is not null) return TopLevel.GetTopLevel(active);
            return TopLevel.GetTopLevel(desktop.MainWindow);
        }
        if (lifetime is ISingleViewApplicationLifetime singleView)
            return TopLevel.GetTopLevel(singleView.MainView);

        return null;
    }
}
```

---

## 4. ViewModel Patterns — Deep Dive

### Full Document ViewModel with Lifecycle

```csharp
public sealed partial class DocumentViewModel : ObservableObject, IDataErrorInfo
{
    private readonly IFileOperationService _fileOps;
    private readonly IDialogService _dialogs;

    [ObservableProperty]
    private string _content = "";

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private string _fileName = "Untitled";

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isOpening;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsNew => CurrentFilePath is null;

    public DocumentViewModel(IFileOperationService fileOps, IDialogService dialogs)
    {
        _fileOps = fileOps;
        _dialogs = dialogs;
    }

    [RelayCommand]
    private async Task NewAsync()
    {
        if (IsDirty)
        {
            var save = await _dialogs.ConfirmAsync("Save changes to current document?");
            if (save == DialogResult.Yes)
                await SaveAsync();
        }
        Content = "";
        CurrentFilePath = null;
        FileName = "Untitled";
        IsDirty = false;
    }

    [RelayCommand]
    private async Task OpenAsync(CancellationToken ct)
    {
        if (IsDirty)
        {
            var save = await _dialogs.ConfirmAsync("Save changes to current document?");
            if (save == DialogResult.Yes)
                await SaveAsync();
        }

        IsOpening = true;
        ErrorMessage = null;

        try
        {
            var text = await _fileOps.OpenTextFileAsync("Open Document", "*.txt;*.md", ct);
            if (text is not null)
            {
                Content = text;
                CurrentFilePath = null; // will be set by the service
                FileName = Path.GetFileName(CurrentFilePath ?? "Untitled");
                IsDirty = false;
            }
        }
        catch (OperationCanceledException) { /* user cancelled */ }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to open file: {ex.Message}";
        }
        finally
        {
            IsOpening = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        if (CurrentFilePath is null)
        {
            await SaveAsAsync(ct);
            return;
        }

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var success = await _fileOps.SaveTextFileAsync(Content,
                "Save Document", Path.GetFileName(CurrentFilePath), "*.txt", ct);
            if (success)
            {
                IsDirty = false;
                ErrorMessage = null;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save file: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsAsync(CancellationToken ct)
    {
        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var success = await _fileOps.SaveTextFileAsync(Content,
                "Save Document As", "document.txt", "*.txt", ct);
            if (success)
            {
                IsDirty = false;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save file: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    partial void OnContentChanged(string value)
    {
        IsDirty = true;
    }
}
```

---

## 5. Storage Bookmarks — Deep Dive

### Persisting Folder Access Across Sessions

Bookmarks allow the application to re-access a previously opened folder without forcing the user to navigate to it again. This is critical for applications that work with project directories.

```csharp
public sealed class BookmarkManager
{
    private readonly IFileOperationService _fileOps;
    private readonly string _bookmarksFilePath;

    private Dictionary<string, string> _bookmarks = new();

    public BookmarkManager(IFileOperationService fileOps)
    {
        _fileOps = fileOps;
        _bookmarksFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MyApp", "bookmarks.json");
        LoadBookmarks();
    }

    public async Task<string?> SaveFolderBookmarkAsync(CancellationToken ct = default)
    {
        var folder = await _fileOps.OpenFolderAsync(ct);
        if (folder is null) return null;

        var bookmark = await _fileOps.SaveBookmarkAsync(folder, ct);
        if (bookmark is not null)
        {
            _bookmarks[folder.Name] = bookmark;
            SaveBookmarks();
        }
        return bookmark;
    }

    public async Task<IStorageFolder?> RestoreFolderAsync(string name, CancellationToken ct = default)
    {
        if (_bookmarks.TryGetValue(name, out var bookmarkId))
            return await _fileOps.RestoreBookmarkAsync(bookmarkId, ct);
        return null;
    }

    public void RemoveBookmark(string name)
    {
        _bookmarks.Remove(name);
        SaveBookmarks();
    }

    public IReadOnlyDictionary<string, string> GetAllBookmarks() => _bookmarks;

    private void LoadBookmarks()
    {
        try
        {
            if (File.Exists(_bookmarksFilePath))
            {
                var json = File.ReadAllText(_bookmarksFilePath);
                _bookmarks = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                    ?? new Dictionary<string, string>();
            }
        }
        catch { /* corrupt file — start fresh */ }
    }

    private void SaveBookmarks()
    {
        var dir = Path.GetDirectoryName(_bookmarksFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(_bookmarks);
        File.WriteAllText(_bookmarksFilePath, json);
    }
}
```

### Project Manager with Bookmarks

```csharp
public sealed partial class ProjectManagerViewModel : ObservableObject
{
    private readonly BookmarkManager _bookmarks;
    private readonly IFileOperationService _fileOps;

    [ObservableProperty]
    private ObservableCollection<ProjectItem> _recentProjects = new();

    [ObservableProperty]
    private string? _currentProjectPath;

    public ProjectManagerViewModel(BookmarkManager bookmarks, IFileOperationService fileOps)
    {
        _bookmarks = bookmarks;
        _fileOps = fileOps;
        LoadRecentProjects();
    }

    private void LoadRecentProjects()
    {
        RecentProjects.Clear();
        foreach (var (name, _) in _bookmarks.GetAllBookmarks())
            RecentProjects.Add(new ProjectItem { Name = name });
    }

    [RelayCommand]
    private async Task OpenProjectAsync(CancellationToken ct)
    {
        var folder = await _fileOps.OpenFolderAsync(ct);
        if (folder is null) return;

        var bookmark = await _fileOps.SaveBookmarkAsync(folder, ct);
        if (bookmark is not null)
        {
            RecentProjects.Insert(0, new ProjectItem
            {
                Name = folder.Name,
                BookmarkId = bookmark
            });
            CurrentProjectPath = folder.Path.LocalPath;
        }
    }

    [RelayCommand]
    private async Task OpenRecentProjectAsync(ProjectItem item, CancellationToken ct)
    {
        var folder = await _bookmarks.RestoreFolderAsync(item.Name, ct);
        if (folder is not null)
            CurrentProjectPath = folder.Path.LocalPath;
        else
            RecentProjects.Remove(item); // bookmark expired
    }
}

public sealed class ProjectItem
{
    public string Name { get; init; } = "";
    public string? BookmarkId { get; init; }
}
```

---

## 6. Progress Reporting — Full Pipeline

### Copy Operation with Progress

```csharp
public sealed class FileCopyService
{
    public async Task CopyFileWithProgressAsync(
        IStorageFile source,
        IStorageFile destination,
        IProgress<FileCopyProgress> progress,
        CancellationToken ct)
    {
        await using var srcStream = await source.OpenReadAsync();
        await using var dstStream = await destination.OpenWriteAsync();

        var totalBytes = srcStream.Length;
        var buffer = new byte[81920];
        long bytesRead = 0;
        int bytes;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while ((bytes = await srcStream.ReadAsync(buffer, ct)) > 0)
        {
            await dstStream.WriteAsync(buffer.AsMemory(0, bytes), ct);
            bytesRead += bytes;
            var elapsed = sw.Elapsed;

            progress.Report(new FileCopyProgress
            {
                BytesCopied = bytesRead,
                TotalBytes = totalBytes,
                Percentage = (double)bytesRead / totalBytes * 100,
                SpeedBytesPerSecond = elapsed.TotalSeconds > 0
                    ? bytesRead / elapsed.TotalSeconds
                    : 0,
                EstimatedTimeRemaining = elapsed.TotalSeconds > 0
                    ? TimeSpan.FromSeconds(
                        (totalBytes - bytesRead) / (bytesRead / elapsed.TotalSeconds))
                    : TimeSpan.Zero
            });
        }
    }
}

public sealed record FileCopyProgress
{
    public long BytesCopied { get; init; }
    public long TotalBytes { get; init; }
    public double Percentage { get; init; }
    public double SpeedBytesPerSecond { get; init; }
    public TimeSpan EstimatedTimeRemaining { get; init; }

    public string FormattedSpeed
    {
        get
        {
            if (SpeedBytesPerSecond > 1_000_000)
                return $"{SpeedBytesPerSecond / 1_000_000:F1} MB/s";
            if (SpeedBytesPerSecond > 1_000)
                return $"{SpeedBytesPerSecond / 1_000:F1} KB/s";
            return $"{SpeedBytesPerSecond:F0} B/s";
        }
    }

    public string FormattedProgress => $"{Percentage:F0}%";
}
```

### ViewModel with Progress Binding

```csharp
public sealed partial class FileTransferViewModel : ObservableObject
{
    private readonly FileCopyService _copyService;
    private readonly IFileOperationService _fileOps;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isTransferring;

    [ObservableProperty]
    private string _transferSpeed = "";

    [ObservableProperty]
    private string _estimatedTime = "";

    public FileTransferViewModel(FileCopyService copyService, IFileOperationService fileOps)
    {
        _copyService = copyService;
        _fileOps = fileOps;
    }

    [RelayCommand]
    private async Task CopyFileAsync(CancellationToken ct)
    {
        var source = await _fileOps.OpenFileAsync(ct: ct);
        if (source is null) return;

        var dest = await _fileOps.SaveFileAsync(source.Name, ct: ct);
        if (dest is null) return;

        IsTransferring = true;
        StatusText = "Copying...";

        var progress = new Progress<FileCopyProgress>(p =>
        {
            ProgressPercentage = p.Percentage;
            TransferSpeed = p.FormattedSpeed;
            EstimatedTime = p.EstimatedTimeRemaining.TotalMinutes > 1
                ? $"{p.EstimatedTimeRemaining.Minutes} min {p.EstimatedTimeRemaining.Seconds} sec"
                : $"{p.EstimatedTimeRemaining.Seconds} sec";
            StatusText = $"{p.FormattedProgress} — {p.FormattedSpeed}";
        });

        try
        {
            await _copyService.CopyFileWithProgressAsync(source, dest, progress, ct);
            StatusText = "Copy complete";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Copy cancelled";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsTransferring = false;
        }
    }
}
```

---

## 7. File Type Filters — Best Practices

```csharp
// Predefined filter sets
public static class FileTypeFilters
{
    public static readonly IReadOnlyList<FilePickerFileType> AllFiles =
        [new FilePickerFileType("All Files") { Patterns = ["*.*"] }];

    public static readonly IReadOnlyList<FilePickerFileType> TextFiles =
        [new FilePickerFileType("Text Files") { Patterns = ["*.txt", "*.md", "*.rtf"] }];

    public static readonly IReadOnlyList<FilePickerFileType> Images =
    [
        new FilePickerFileType("Images")
        {
            Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp"],
            MimeTypes = ["image/png", "image/jpeg", "image/gif", "image/bmp", "image/webp"]
        }
    ];

    public static readonly IReadOnlyList<FilePickerFileType> Documents =
        [new FilePickerFileType("Documents") { Patterns = ["*.pdf", "*.docx", "*.xlsx"] }];

    public static IReadOnlyList<FilePickerFileType> ProjectFiles =>
    [
        new FilePickerFileType("Project Files") { Patterns = ["*.myapp", "*.myappproj"] },
        new FilePickerFileType("All Files") { Patterns = ["*.*"] }
    ];
}
```

---

## 8. Testing File Operations

```csharp
[TestClass]
public sealed class FileOperationServiceTests
{
    [TestMethod]
    public async Task OpenTextFileAsync_ReturnsNull_WhenPickerCancelled()
    {
        var topLevel = new Window(); // in test, this would be a mock
        var service = new FileOperationService(topLevel);
        // In a real test, mock IStorageProvider to return empty result
    }

    [TestMethod]
    public async Task SaveTextFileAsync_ReturnsFalse_WhenPickerCancelled()
    {
        var topLevel = new Window();
        var service = new FileOperationService(topLevel);
        // Mock IStorageProvider.SaveFilePickerAsync to return null
    }

    [TestMethod]
    public void Constructor_ThrowsOnNullTopLevel()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new FileOperationService(null!));
    }
}
```

---

## Summary: Core vs. Verbose

| Concept | Core | Verbose |
|---|---|---|
| Service interface | Basic 4 methods | Full 12-method interface with folders, bookmarks, binary |
| Implementation | Basic text helpers | Complete implementation with error handling, multiple file types |
| DI registration | Simple singleton | 3 strategies: singleton, scoped, fallback |
| ViewModel | Simple Open/Save | Full lifecycle: New, Open, Save, SaveAs, Dirty, Error |
| Bookmarks | Basic snippet | `BookmarkManager` with persistence, `ProjectManager` |
| Progress reporting | Basic copy loop | `FileCopyProgress` with speed/ETA, full ViewModel binding |
| File type filters | — | `FileTypeFilters` static class with best practices |
| Testing | — | Unit test structure with mock guidance |
