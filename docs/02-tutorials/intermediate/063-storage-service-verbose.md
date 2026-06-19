---
tier: intermediate
topic: services
estimated: 18 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 063V — Storage Service (verbose companion)

**What this covers:** Bookmark persistence, managed file picker internals, storage item lifecycle, and cross-platform edge cases.

**Prerequisites:** 063 — Storage Service core

---

## 1. Bookmarks

Bookmarks persist file/folder access across app restarts. The bookmark ID is a string you store (e.g., in settings).

### Saving a bookmark

```csharp
var file = (await storage.OpenFilePickerAsync(...))[0];
if (file is IStorageBookmarkFile bookmarkFile)
{
    string bookmarkId = await bookmarkFile.SaveBookmarkAsync();
    // Store bookmarkId in settings
    Preferences.Default.Set("lastFile", bookmarkId);
}
```

### Opening a bookmark

```csharp
string? bookmarkId = Preferences.Default.Get<string>("lastFile", null);
if (bookmarkId is not null)
{
    var file = await storage.OpenFileBookmarkAsync(bookmarkId);
    if (file is not null)
    {
        using var stream = await file.OpenReadAsync();
        // Read content
    }
}
```

### Bookmark platform support

| Platform | Support |
|----------|---------|
| Windows | ✓ (returns file path) |
| macOS | ✓ (sandboxed apps) |
| Linux | ✓ (returns file path) |
| Browser | ✓ (IndexedDB) |
| Android | ✓ |
| iOS | ✓ |

---

## 2. Managed file picker

On desktop platforms, Avalonia can use a managed (custom window) file picker instead of the native OS dialog:

```csharp
// In Program.cs
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .With(new ManagedFileDialogOptions
    {
        // Customize the managed dialog appearance
    })
    .UseManagedFileDialog(); // Override native pickers
```

This is useful when you need consistent behavior across platforms.

---

## 3. Storage item operations

```csharp
// Read text content
string content = await file.OpenTextAsync();

// Read as stream
using var readStream = await file.OpenReadAsync();
byte[] buffer = new byte[readStream.Length];
await readStream.ReadAsync(buffer);

// Write
using var writeStream = await file.OpenWriteAsync();
using var writer = new StreamWriter(writeStream);
await writer.WriteAsync("Hello");

// Delete
await file.DeleteAsync();

// Move to another folder
var destination = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
var moved = await file.MoveAsync(destination);
```

### Folder contents

```csharp
var folder = (await storage.OpenFolderPickerAsync(...))[0];
var items = await folder.GetItemsAsync();
foreach (var item in items)
{
    if (item is IStorageFile f)
        Console.WriteLine($"File: {f.Name}");
    else if (item is IStorageFolder d)
        Console.WriteLine($"Folder: {d.Name}");
}
```

---

## 4. SuggestedStartLocation

Set the initial directory of the picker:

```csharp
var docs = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);

var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
{
    SuggestedStartLocation = docs
});
```

The system may ignore this if it doesn't have access to the folder.

---

## 5. Platform-specific notes

| Platform | Notes |
|----------|-------|
| Windows | Native pickers; flush clipboard on exit |
| macOS | Sandboxed apps require bookmarks for persistent access |
| Linux | DBus file picker may ignore start location; set `UseDBusFilePicker=false` in `X11PlatformOptions` for GTK |
| Browser | Chromium-only for save; bookmark via IndexedDB |
| Android | Content URIs, not file paths |
| iOS | Sandboxed; bookmark for re-access |

---

## 6. Error handling

```csharp
try
{
    var files = await storage.OpenFilePickerAsync(options);
    if (files.Count == 0)
    {
        // User cancelled
        return;
    }
}
catch (InvalidOperationException ex)
{
    // Platform doesn't support file picker (check CanOpen first)
    Console.WriteLine($"File picker unavailable: {ex.Message}");
}
catch (UnauthorizedAccessException ex)
{
    // No permission to access the selected file
    Console.WriteLine($"Access denied: {ex.Message}");
}
```

---

## See Also

- [063 — Storage Service (core)](063-storage-service.md)
- [063E — Storage Service (examples)](063-storage-service-examples.md)
- [Avalonia Docs: Bookmarks](https://docs.avaloniaui.net/docs/services/storage/bookmarks)
- [Avalonia Docs: Storage Items](https://docs.avaloniaui.net/docs/services/storage/storage-item)
