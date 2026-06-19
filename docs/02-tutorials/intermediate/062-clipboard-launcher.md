---
tier: intermediate
topic: services
estimated: 10 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 062 — Clipboard & Launcher

**What you'll learn:** How to read/write the system clipboard, handle custom data formats, and open URIs/files with the Launcher service.

**Prerequisites:** [001 — Project Setup](../basics/001-project-setup.md)

---

## 1. Accessing the clipboard

Get `IClipboard` from a `TopLevel` (e.g., `Window`):

```csharp
var clipboard = TopLevel.GetTopLevel(control)?.Clipboard;
// or from a window:
var clipboard = window.Clipboard;
```

### Reading text

```csharp
var text = await clipboard?.TryGetTextAsync();
```

### Writing text

```csharp
await clipboard?.SetTextAsync("Hello from Avalonia!");
```

### Clearing

```csharp
await clipboard?.ClearAsync();
```

---

## 2. Other universal formats

| Format | Read | Write |
|--------|------|-------|
| Text | `TryGetTextAsync()` | `SetTextAsync(string)` |
| File | `TryGetFileAsync()` | `SetFileAsync(IStorageItem)` |
| Bitmap | `TryGetBitmapAsync()` | `SetBitmapAsync(Bitmap)` |

```csharp
var bitmap = await clipboard?.TryGetBitmapAsync();
if (bitmap is not null)
    Console.WriteLine($"Image: {bitmap.PixelSize}");
```

### Multiple files

```csharp
var files = await clipboard?.TryGetFilesAsync();
await clipboard?.SetFilesAsync(storageItems);
```

---

## 3. Custom data formats

```csharp
// Application-specific format
var myFormat = DataFormat.CreateStringApplicationFormat("myapp-custom");
await clipboard?.SetValueAsync(myFormat, "internal data");
var data = await clipboard?.TryGetValueAsync(myFormat);

// Platform-specific format
var htmlFormat = DataFormat.CreateStringPlatformFormat("text/html");
await clipboard?.SetValueAsync(htmlFormat, "<b>bold</b>");
```

### DataTransfer for multiple formats

```csharp
var item = new DataTransferItem();
item.Set(DataFormat.Text, "Plain text version");
item.Set(DataFormat.CreateStringPlatformFormat("text/html"), "<b>HTML version</b>");

var data = new DataTransfer();
data.Add(item);
await clipboard?.SetDataAsync(data);
```

---

## 4. IAsyncDataTransfer (advanced reading)

For bulk clipboard access, use `TryGetDataAsync()`:

```csharp
using var data = await clipboard?.TryGetDataAsync();
if (data is not null)
{
    foreach (var format in data.Formats)
        Console.WriteLine($"Format: {format.Name}");

    var text = await data.TryGetTextAsync();
    var files = await data.TryGetFilesAsync();
}
```

---

## 5. Launcher basics

```csharp
var launcher = TopLevel.GetTopLevel(control)?.Launcher;
```

### Open a URL

```csharp
bool success = await launcher.LaunchUriAsync(new Uri("https://avaloniaui.net"));
```

### Open a file from storage

```csharp
var files = await storageProvider.OpenFilePickerAsync(...);
if (files.Count > 0)
    await launcher.LaunchFileAsync(files[0]);
```

### Open a file path (desktop only)

```csharp
var file = new FileInfo(@"C:\docs\report.pdf");
bool success = await launcher.LaunchFileInfoAsync(file);

var folder = new DirectoryInfo(@"C:\docs");
bool opened = await launcher.LaunchDirectoryInfoAsync(folder);
```

---

## 6. Platform compatibility

| Feature | Windows | macOS | Linux | Browser |
|---------|---------|-------|-------|---------|
| `LaunchUriAsync` | ✓ | ✓ | ✓ | ✓ |
| `LaunchFileAsync` | ✓ | ✓ | ✓ | ✗ |
| `LaunchFileInfoAsync` | ✓ | ✓ | ✓ | ✗ |
| Clipboard text | ✓ | ✓ | ✓ | ✓ |
| Clipboard bitmap | ✓ | ✓ | ✓ | partial |
| Custom formats | ✓ | ✓ | ✓ | ✗ |

---

## 7. Clipboard flushing

On Windows/macOS/Linux, clipboard data is lazy. Call `FlushAsync()` to persist after app closes:

```csharp
await clipboard?.SetTextAsync("persistent text");
await clipboard?.FlushAsync(); // Persist after shutdown
```

---

## Key Takeaways

- Access clipboard/launcher from `TopLevel` or `Window`
- Use extension methods (`SetTextAsync`, `TryGetTextAsync`) for common formats
- `DataTransfer` + `DataTransferItem` for multi-format clipboard writes
- `Launcher.LaunchUriAsync` opens URLs in default browser
- `FlushAsync()` persists clipboard data after app exit
- `DataFormat` supports universal, platform-specific, and app-specific formats

---

## See Also

- [062V — Clipboard & Launcher (verbose)](062-clipboard-launcher-verbose.md)
- [062E — Clipboard & Launcher (examples)](062-clipboard-launcher-examples.md)
- [Avalonia Docs: Clipboard](https://docs.avaloniaui.net/docs/services/clipboard)
- [Avalonia Docs: Launcher](https://docs.avaloniaui.net/docs/services/launcher)
- [063 — Storage Service](063-storage-service.md)
