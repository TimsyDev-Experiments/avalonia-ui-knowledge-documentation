---
tier: advanced
topic: platform-services
estimated: 30 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 034 -- File Pickers and Platform Services

**What you'll learn:** How to use Avalonia's `StorageProvider` for file and folder pickers, the clipboard service, and other platform services accessed through `TopLevel`.

**Prerequisites:** [001 -- Project Setup](../basics/001-project-setup.md), [010 -- Window and Dialog Basics](../basics/010-window-dialog-basics.md)

---

## 1. Access the TopLevel

From any control or view:

```csharp
var topLevel = TopLevel.GetTopLevel(this);
if (topLevel is null) return; // Not attached to visual tree yet
```

All platform services live on `TopLevel`:

```csharp
var storage = topLevel.StorageProvider;
var clipboard = topLevel.Clipboard;
var screens = topLevel.Screens;
var focusManager = topLevel.FocusManager;
var platform = topLevel.PlatformSettings;
```

## 2. Open a file picker

```csharp
var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
{
    Title = "Select an image",
    AllowMultiple = false,
    FileTypeFilter = new[]
    {
        new FilePickerFileType("PNG Images")
        {
            Patterns = new[] { "*.png" },
            AppleUniformTypeIdentifiers = new[] { "public.png" },
            MimeTypes = new[] { "image/png" }
        },
        FilePickerFileTypes.ImageAll,
        FilePickerFileTypes.All
    }
});

if (files.Count > 0)
{
    var file = files[0];
    await using var stream = await file.OpenReadAsync();
    // Read from stream
}
```

## 3. Save a file

```csharp
var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
{
    Title = "Save report",
    SuggestedFileName = "report.txt",
    DefaultExtension = "txt",
    FileTypeChoices = new[]
    {
        new FilePickerFileType("Text Files")
        {
            Patterns = new[] { "*.txt" }
        }
    },
    ShowOverwritePrompt = true
});

if (file is not null)
{
    await using var stream = await file.OpenWriteAsync();
    using var writer = new StreamWriter(stream);
    await writer.WriteAsync(content);
}
```

## 4. Open a folder picker

```csharp
var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
{
    Title = "Select output directory",
    AllowMultiple = false
});

if (folders.Count > 0)
{
    var folder = folders[0];
    var path = folder.TryGetLocalPath();
}
```

## 5. Use clipboard service

```csharp
// Copy text
await clipboard.SetTextAsync("Hello from Avalonia!");

// Paste text
var text = await clipboard.GetTextAsync();

// Get clipboard formats
var formats = await clipboard.GetFormatsAsync();

// Set custom data
var data = new DataObject();
data.Set(DataFormats.Text, "Custom data");
await clipboard.SetDataObjectAsync(data);

// Clear
await clipboard.ClearAsync();
```

## 6. Detect platform capabilities

```csharp
if (storage.CanOpen)
{
    // File open is supported
}

if (storage.CanSave)
{
    // File save is supported
}

if (storage.CanPickFolder)
{
    // Folder picker is supported
}
```

## Platform compatibility

| Feature | Windows | macOS | Linux | Browser | Android | iOS |
|---------|---------|-------|-------|---------|---------|-----|
| Open file picker | Full | Full | Full | Chrome only | Full | Full |
| Save file picker | Full | Full | Full | Chrome only | Full | Full |
| Folder picker | Full | Full | Full | Chrome only | Full | Full |
| Clipboard | Full | Full | Full | Full | Limited | Limited |
| TryGetFileFromPath | Full | Full | Full | No | No | No |
| Bookmarks | Path-based | Path-based | Path-based | Full | URI-based | URI-based |

## Key takeaways

- Access platform services through `TopLevel.GetTopLevel(control)`
- `StorageProvider` handles file open, save, and folder pickers
- `Clipboard` supports text and `DataObject` transfer
- File type filters use platform-specific hints (patterns, UTIs, MIME types)
- Check `CanOpen`/`CanSave`/`CanPickFolder` before showing pickers
- Bookmark IDs persist access across app restarts (macOS sandbox, Android)
