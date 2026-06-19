---
tier: intermediate
topic: services
estimated: 14 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 063 — Storage Service

**What you'll learn:** How to open and save files, pick folders, work with storage items, and use bookmarks with the `IStorageProvider` API.

**Prerequisites:** [001 — Project Setup](../basics/001-project-setup.md)

---

## 1. Accessing the storage provider

```csharp
var storage = TopLevel.GetTopLevel(control)?.StorageProvider;
// or from a window:
var storage = window.StorageProvider;
```

Check platform capability:

```csharp
bool canOpen   = storage.CanOpen;
bool canSave   = storage.CanSave;
bool canPickFolder = storage.CanPickFolder;
```

---

## 2. Opening files

```csharp
var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
{
    Title = "Select a file",
    AllowMultiple = false,
    FileTypeFilter = new[] { FilePickerFileTypes.TextPlain, FilePickerFileTypes.All }
});

if (files.Count > 0)
{
    var file = files[0];
    var text = await file.OpenTextAsync(); // read as string
}
```

### Multiple files

```csharp
var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
{
    AllowMultiple = true,
    FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
});
```

---

## 3. Saving files

```csharp
var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
{
    Title = "Save report",
    SuggestedFileName = "report.txt",
    DefaultExtension = "txt",
    FileTypeChoices = new[] { FilePickerFileTypes.TextPlain },
    ShowOverwritePrompt = true
});

if (file is not null)
{
    using var stream = await file.OpenWriteAsync();
    using var writer = new StreamWriter(stream);
    await writer.WriteAsync(content);
}
```

### Save with selected file type

```csharp
var result = await storage.SaveFilePickerWithResultAsync(new FilePickerSaveOptions
{
    Title = "Export",
    FileTypeChoices = new[] { FilePickerFileTypes.ImagePng, FilePickerFileTypes.ImageJpg }
});
// result.StorageFile — the saved file (or null)
// result.SelectedFileType — the format the user picked
```

---

## 4. Picking folders

```csharp
var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
{
    Title = "Select output folder",
    AllowMultiple = false,
    SuggestedStartLocation = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
});
```

---

## 5. Built-in file type filters

| Constant | Patterns |
|----------|----------|
| `FilePickerFileTypes.All` | `*.*` |
| `FilePickerFileTypes.TextPlain` | `*.txt` |
| `FilePickerFileTypes.ImageAll` | `*.png, *.jpg, *.jpeg, *.gif, *.bmp, *.webp` |
| `FilePickerFileTypes.ImageJpg` | `*.jpg, *.jpeg` |
| `FilePickerFileTypes.ImagePng` | `*.png` |
| `FilePickerFileTypes.ImageWebP` | `*.webp` |
| `FilePickerFileTypes.Pdf` | `*.pdf` |

### Custom file type

```csharp
var csvType = new FilePickerFileType("CSV Files")
{
    Patterns = new[] { "*.csv" },
    AppleUniformTypeIdentifiers = new[] { "public.comma-separated-values-text" },
    MimeTypes = new[] { "text/csv" }
};
```

---

## 6. Storage items

```csharp
public interface IStorageItem
{
    string Name { get; }
    Uri Path { get; }
    Task DeleteAsync();
    Task<IStorageItem?> MoveAsync(IStorageFolder destination);
}

public interface IStorageFile : IStorageItem
{
    Task<Stream> OpenReadAsync();
    Task<Stream> OpenWriteAsync();
    Task<string> OpenTextAsync();
}

public interface IStorageFolder : IStorageItem
{
    Task<IReadOnlyList<IStorageItem>> GetItemsAsync();
}
```

---

## 7. Well-known folders

```csharp
var desktop = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Desktop);
var documents = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
var music = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Music);
var pictures = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Pictures);
var videos = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Videos);
```

---

## 8. File/folder from path (desktop only)

```csharp
var file = await storage.TryGetFileFromPathAsync(@"C:\data\input.txt");
var folder = await storage.TryGetFolderFromPathAsync(@"C:\data");
```

---

## Key Takeaways

- Access `IStorageProvider` from `TopLevel.StorageProvider`
- Use `OpenFilePickerAsync` / `SaveFilePickerAsync` / `OpenFolderPickerAsync`
- Define custom file type filters with `FilePickerFileType`
- `IStorageFile` provides `OpenReadAsync`, `OpenWriteAsync`, `OpenTextAsync`
- Use bookmarks to persist access to picked files across sessions
- Well-known folders available via `TryGetWellKnownFolderAsync`
- Path-based access (`TryGetFileFromPathAsync`) works on desktop only

---

## See Also

- [063V — Storage Service (verbose)](063-storage-service-verbose.md)
- [063E — Storage Service (examples)](063-storage-service-examples.md)
- [Avalonia Docs: Storage Provider](https://docs.avaloniaui.net/docs/services/storage/storage-provider)
- [Avalonia Docs: File Picker Options](https://docs.avaloniaui.net/docs/services/storage/file-picker-options)
- [062 — Clipboard & Launcher](062-clipboard-launcher.md)
