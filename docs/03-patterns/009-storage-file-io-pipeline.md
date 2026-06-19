---
tier: advanced
topic: platform
estimated: 12 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Pattern: Storage & File I/O Pipeline

**What you'll learn:** A robust pipeline for opening, saving, and managing files using the Avalonia `IStorageProvider`, with fallback patterns and progress reporting.

**Prerequisites:** [060 — Storage Service](../02-tutorials/intermediate/060-multibinding-prioritybinding.md), [063 — Storage Service](../02-tutorials/intermediate/063-storage-service.md)

---

## Problem

An application needs to open and save files with platform-native dialogs, buffer large files to disk without blocking the UI, persist recently used folders via bookmarks, and report progress to the user.

---

## Solution: FileOperationService

```csharp
public interface IFileOperationService
{
    Task<string?> OpenTextFileAsync(string title, string? filter = null, CancellationToken ct = default);
    Task<bool> SaveTextFileAsync(string content, string title, string? defaultName = null, string? filter = null, CancellationToken ct = default);
    Task<IStorageFile?> OpenFileAsync(IReadOnlyList<FilePickerFileType>? filters = null, CancellationToken ct = default);
    Task<IStorageFile?> SaveFileAsync(string? suggestedName = null, IReadOnlyList<FilePickerFileType>? filters = null, CancellationToken ct = default);
}

public class FileOperationService(TopLevel topLevel) : IFileOperationService
{
    private IStorageProvider Storage => topLevel.StorageProvider;

    public async Task<string?> OpenTextFileAsync(string title, string? filter = null, CancellationToken ct = default)
    {
        var file = await OpenFileAsync(filter is not null
            ? [new FilePickerFileType("Documents") { Patterns = [filter] }]
            : null, ct);

        if (file is null) return null;

        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct);
    }

    public async Task<bool> SaveTextFileAsync(string content, string title, string? defaultName = null,
        string? filter = null, CancellationToken ct = default)
    {
        var file = await SaveFileAsync(defaultName, filter is not null
            ? [new FilePickerFileType("Documents") { Patterns = [filter] }]
            : null, ct);

        if (file is null) return false;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content.AsMemory(), ct);
        return true;
    }

    public async Task<IStorageFile?> OpenFileAsync(IReadOnlyList<FilePickerFileType>? filters = null, CancellationToken ct = default)
    {
        var result = await Storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = false,
            FileTypeFilter = filters
        });
        return result?.FirstOrDefault();
    }

    public async Task<IStorageFile?> SaveFileAsync(string? suggestedName = null,
        IReadOnlyList<FilePickerFileType>? filters = null, CancellationToken ct = default)
    {
        return await Storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save File",
            SuggestedFileName = suggestedName,
            DefaultExtension = filters?.FirstOrDefault()?.Patterns?.FirstOrDefault(),
            FileTypeChoices = filters
        });
    }
}
```

---

## DI registration

```csharp
// In composition root
services.AddSingleton<IFileOperationService>(sp =>
{
    var topLevel = TopLevel.GetTopLevel(Application.Current?.MainWindow)!;
    return new FileOperationService(topLevel);
});
```

---

## ViewModel usage

```csharp
public partial class DocumentViewModel(IFileOperationService fileOps) : ObservableObject
{
    [ObservableProperty] private string _content = "";
    [ObservableProperty] private string? _currentFilePath;
    [ObservableProperty] private bool _isSaving;

    [RelayCommand]
    private async Task OpenAsync(CancellationToken ct)
    {
        var text = await fileOps.OpenTextFileAsync("Open Document", "*.txt", ct);
        if (text is not null) Content = text;
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        IsSaving = true;
        try
        {
            var success = await fileOps.SaveTextFileAsync(Content, "Save Document",
                "document.txt", "*.txt", ct);
            if (success) /* mark clean */;
        }
        finally { IsSaving = false; }
    }
}
```

---

## Storage bookmarks (persisting folder access)

```csharp
// Save bookmark
var folder = await Storage.OpenFolderPickerAsync(new FolderPickerOpenOptions());
if (folder.Count > 0)
{
    var bookmark = await folder[0].SaveBookmarkAsync();
    Settings.Default.LastFolderBookmark = bookmark;
}

// Restore bookmark
var bookmarkId = Settings.Default.LastFolderBookmark;
if (bookmarkId is not null)
{
    var restored = await Storage.OpenStorageBookmarkAsync(bookmarkId);
    if (restored is IStorageFolder folder)
        // use folder
}
```

---

## Progress reporting for large files

```csharp
public async Task CopyLargeFileAsync(IStorageFile source, IStorageFile dest,
    IProgress<double> progress, CancellationToken ct)
{
    await using var src = await source.OpenReadAsync();
    await using var dst = await dest.OpenWriteAsync();
    var buffer = new byte[81920];
    long total = src.Length, read = 0;

    int bytes;
    while ((bytes = await src.ReadAsync(buffer, ct)) > 0)
    {
        await dst.WriteAsync(buffer.AsMemory(0, bytes), ct);
        read += bytes;
        progress.Report((double)read / total);
    }
}
```
