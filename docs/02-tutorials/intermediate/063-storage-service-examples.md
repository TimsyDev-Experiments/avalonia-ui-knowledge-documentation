---
tier: intermediate
topic: services
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 063E — Storage Service (examples)

## Example 1: Open and read a text file

```csharp
[RelayCommand]
private async Task OpenFile()
{
    var storage = GetStorageProvider();
    if (storage is null || !storage.CanOpen) return;

    var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
    {
        Title = "Open text file",
        AllowMultiple = false,
        FileTypeFilter = new[] { FilePickerFileTypes.TextPlain }
    });

    if (files.Count > 0)
    {
        var content = await files[0].OpenTextAsync();
        FileContent = content;
    }
}

private IStorageProvider? GetStorageProvider() =>
    TopLevel.GetTopLevel(VisualRoot)?.StorageProvider;
```

---

## Example 2: Save text to a file

```csharp
[RelayCommand]
private async Task SaveFile()
{
    var storage = GetStorageProvider();
    if (storage is null || !storage.CanSave) return;

    var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
    {
        Title = "Save file",
        SuggestedFileName = "document.txt",
        DefaultExtension = "txt",
        FileTypeChoices = new[] { FilePickerFileTypes.TextPlain },
        ShowOverwritePrompt = true
    });

    if (file is not null)
    {
        using var stream = await file.OpenWriteAsync();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(FileContent);
    }
}
```

---

## Example 3: Export with format selection

```csharp
[RelayCommand]
private async Task ExportImage()
{
    var storage = GetStorageProvider();
    var result = await storage.SaveFilePickerWithResultAsync(new FilePickerSaveOptions
    {
        Title = "Export image",
        SuggestedFileName = "export",
        FileTypeChoices = new[]
        {
            FilePickerFileTypes.ImagePng,
            FilePickerFileTypes.ImageJpg,
            FilePickerFileTypes.ImageWebP,
        }
    });

    if (result.StorageFile is null) return;

    var format = result.SelectedFileType == FilePickerFileTypes.ImageJpg ? "jpeg"
               : result.SelectedFileType == FilePickerFileTypes.ImageWebP ? "webp"
               : "png";

    // Encode and save in the chosen format
    using var stream = await result.StorageFile.OpenWriteAsync();
    await EncodeImageAsync(stream, format);
}
```

---

## Example 4: Pick a folder and list contents

```csharp
[RelayCommand]
private async Task PickFolder()
{
    var storage = GetStorageProvider();
    if (storage is null || !storage.CanPickFolder) return;

    var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
    {
        Title = "Select folder",
        AllowMultiple = false,
        SuggestedStartLocation =
            await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop)
    });

    if (folders.Count > 0)
    {
        var items = await folders[0].GetItemsAsync();
        FolderContents = items.Select(i => $"[{i.GetType().Name}] {i.Name}").ToList();
    }
}
```

---

## Example 5: Bookmark persistence with settings

```csharp
public partial class PersistentFileViewModel : ObservableObject
{
    private const string BookmarkKey = "LastFileBookmark";

    [ObservableProperty] private string _lastFilePath = "";

    [RelayCommand]
    private async Task OpenAndBookmark()
    {
        var storage = GetStorageProvider();
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select file",
            FileTypeFilter = new[] { FilePickerFileTypes.TextPlain }
        });

        if (files.Count > 0 && files[0] is IStorageBookmarkFile bf)
        {
            var bookmark = await bf.SaveBookmarkAsync();
            Preferences.Default.Set(BookmarkKey, bookmark);
            LastFilePath = files[0].Path.ToString();
        }
    }

    [RelayCommand]
    private async Task RestoreBookmarked()
    {
        var bookmarkId = Preferences.Default.Get<string>(BookmarkKey, null);
        if (bookmarkId is null) return;

        var storage = GetStorageProvider();
        var file = await storage.OpenFileBookmarkAsync(bookmarkId);
        if (file is not null)
        {
            var content = await file.OpenTextAsync();
            FileContent = content;
        }
    }
}
```

---

## Example 6: Multi-file selection

```csharp
[RelayCommand]
private async Task OpenMultipleFiles()
{
    var storage = GetStorageProvider();
    var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
    {
        Title = "Select images",
        AllowMultiple = true,
        FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
    });

    SelectedFiles.Clear();
    foreach (var file in files)
    {
        using var stream = await file.OpenReadAsync();
        var bitmap = new Bitmap(stream);
        SelectedFiles.Add(bitmap);
    }
}
```

---

## Example 7: Copy file to a new location

```csharp
[RelayCommand]
private async Task CopyFile()
{
    var storage = GetStorageProvider();
    var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
    {
        Title = "Select file to copy",
        AllowMultiple = false
    });
    if (files.Count == 0) return;
    var sourceFile = files[0];

    var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
    {
        Title = "Select destination folder"
    });
    if (folders.Count == 0) return;
    var destFolder = folders[0];

    await sourceFile.MoveAsync(destFolder);
    Status = $"Moved {sourceFile.Name} to {destFolder.Name}";
}
```

---

## See Also

- [063 — Storage Service (core)](063-storage-service.md)
- [063V — Storage Service (verbose)](063-storage-service-verbose.md)
- [062 — Clipboard & Launcher](062-clipboard-launcher.md)
