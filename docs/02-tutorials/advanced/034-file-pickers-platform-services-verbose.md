---
tier: advanced
topic: platform-services
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 034-file-pickers-platform-services.md
---

# 034V — File Pickers and Platform Services: An In-Depth Companion

**Why this exists.** The original tutorial shows how to call file pickers and clipboard APIs. This companion explains how `TopLevel` connects your control to the OS, why `StorageProvider` is an abstraction and not a direct API, what each file-type hint does, when `CanOpen`/`CanSave`/`CanPickFolder` returns false, and how bookmarks interact with OS sandboxes.

**Read this alongside:** [034 — File Pickers and Platform Services](034-file-pickers-platform-services.md)

---

## 1. What `TopLevel` is and why it is the hub

`TopLevel` is the base class for `Window`, `Popup`, and any visual root. It owns the connection to the OS windowing system. Every platform service — file dialogs, clipboard, screen info, focus, input methods — is exposed through `TopLevel` because those services require an OS-level handle (HWND, NSWindow, GtkWindow, Android Activity, etc.).

```csharp
var topLevel = TopLevel.GetTopLevel(this);
if (topLevel is null) return;
```

`TopLevel.GetTopLevel(control)` walks the visual tree upward until it finds a `TopLevel`. It returns `null` if the control is not attached to a visual tree yet — for example, during construction or after removal. Always guard against null.

```csharp
var storage = topLevel.StorageProvider;
var clipboard = topLevel.Clipboard;
```

Each property retrieves the platform-specific implementation of the interface. On Windows, `StorageProvider` wraps the Win32 `IFileOpenDialog`/`IFileSaveDialog` COM APIs. On macOS, it wraps `NSOpenPanel`/`NSSavePanel`. On browser, it wraps the JavaScript `window.showOpenFilePicker()` (Chromium only). The abstraction lets your code be identical across all platforms.

**Common mistake:** calling `TopLevel.GetTopLevel` from a constructor or before the control is added to the tree. The result is `null` and every property access throws `NullReferenceException`.

---

## 2. `StorageProvider` — why it exists and how it works

`IStorageProvider` defines six operations:

| Method | Purpose | Returns |
|--------|---------|---------|
| `OpenFilePickerAsync` | Show OS open-file dialog | `IReadOnlyList<IStorageFile>` |
| `SaveFilePickerAsync` | Show OS save-file dialog | `IStorageFile?` |
| `OpenFolderPickerAsync` | Show OS folder picker | `IReadOnlyList<IStorageFolder>` |
| `TryGetFileFromPath` | Resolve a file path to an `IStorageFile` | `IStorageFile?` |
| `TryGetFolderFromPath` | Resolve a folder path to an `IStorageFolder` | `IStorageFolder?` |
| `TryGetWellKnownFolder` | Get a well-known folder (Documents, Desktop, etc.) | `IStorageFolder?` |

Each method returns an `IStorageItem` (either `IStorageFile` or `IStorageFolder`). These are OS-native wrappers that support `OpenReadAsync()`/`OpenWriteAsync()` for stream access and `TryGetLocalPath()` for a file-system path string.

**Why stream access instead of a path:** On browser (WASM) and mobile (Android/iOS), the OS does not grant your app direct file-system access to the picked file. The platform gives you a read-only stream handle or a content URI. `IStorageFile.OpenReadAsync()` wraps this correctly. `TryGetLocalPath()` returns `null` on those platforms.

```csharp
if (files.Count > 0)
{
    var file = files[0];
    await using var stream = await file.OpenReadAsync();
}
```

`await using` disposes the stream when the block exits. Always dispose streams — file handles can be exclusive on some platforms.

---

## 3. `FilePickerFileType` — why three hint systems

```csharp
new FilePickerFileType("PNG Images")
{
    Patterns = new[] { "*.png" },
    AppleUniformTypeIdentifiers = new[] { "public.png" },
    MimeTypes = new[] { "image/png" }
}
```

Each OS has a different native file-type filtering mechanism:

- **Patterns** (`*.png`): Used on Windows (Win32 `COMDLG_FILTERSPEC`) and Linux (GTK file filter). The OS matches file name patterns.
- **AppleUniformTypeIdentifiers** (`public.png`): Used on macOS. macOS uses UTIs (Uniform Type Identifiers) instead of file extensions. `public.png` is the UTI for PNG. Without this, the macOS dialog may show files as greyed out or not filter at all.
- **MimeTypes** (`image/png`): Used on browser (WASM) for the `accept` attribute on the file input. The browser uses MIME types to filter the file picker.

Provide all three for cross-platform correctness. On the platform where a hint is not used, it is ignored.

`FilePickerFileTypes.ImageAll` and `FilePickerFileTypes.All` are static presets. `ImageAll` includes common image types (PNG, JPEG, GIF, BMP, WebP). `All` means no filter.

**Common mistake:** providing only `Patterns` and testing only on Windows. The macOS dialog shows unfiltered because `AppleUniformTypeIdentifiers` is missing.

---

## 4. Save file picker — why `DefaultExtension` and `ShowOverwritePrompt`

```csharp
var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
{
    Title = "Save report",
    SuggestedFileName = "report.txt",
    DefaultExtension = "txt",
    FileTypeChoices = new[] { /* ... */ },
    ShowOverwritePrompt = true
});
```

- `SuggestedFileName`: pre-fills the file name field. On macOS, this is the `nameFieldStringValue`.
- `DefaultExtension`: appended if the user omits an extension. The OS may already do this natively (Windows appends the selected filter's extension automatically).
- `ShowOverwritePrompt`: on Windows, this enables the built-in overwrite confirmation dialog. On macOS, the save panel shows an alert by default when the file exists. On browser, the prompt is handled by the JS API.

`OpenWriteAsync()` returns a write stream. On sandboxed platforms, the OS saves to a temporary location and then moves the file atomically to the final destination.

---

## 5. Folder picker — why `TryGetLocalPath`

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

`TryGetLocalPath()` attempts to resolve the storage item to a file-system path. On desktop, this succeeds and returns `"C:\Users\...\folder"` or `"/home/.../folder"`. On browser and Android content URIs, it returns `null` because the OS does not expose a file-system path.

When `TryGetLocalPath()` returns `null`, use `OpenReadAsync()` to read the item content, or store the `IStorageFolder` reference for later access. You cannot pass the path to APIs that expect a real file path (like `Directory.CreateDirectory`).

---

## 6. Clipboard — text vs `DataObject`

```csharp
await clipboard.SetTextAsync("Hello from Avalonia!");
var text = await clipboard.GetTextAsync();
```

`SetTextAsync`/`GetTextAsync` handle plain text. The clipboard service on each platform translates this to the native format (CF_UNICODETEXT on Windows, NSPasteboardStringType on macOS).

```csharp
var data = new DataObject();
data.Set(DataFormats.Text, "Custom data");
await clipboard.SetDataObjectAsync(data);
```

`DataObject` supports multiple formats in one clipboard operation. Use this when you need to put both text and a custom object on the clipboard.

```csharp
var formats = await clipboard.GetFormatsAsync();
```

Returns the list of data formats currently on the OS clipboard. On Windows, this maps to `EnumClipboardFormats`. Use this to decide which read operation to call.

**Common mistake:** calling `SetDataObjectAsync` with a reference-type object that has no registered serialization. The clipboard must be able to serialize the data for cross-process transfer. Non-serializable objects silently fail.

---

## 7. `CanOpen`/`CanSave`/`CanPickFolder` — why check

```csharp
if (storage.CanOpen)
{
    // File open is supported
}
```

These properties return `false` on platforms where the operation is not supported:

| Property | False when |
|----------|-----------|
| `CanOpen` | WASM on non-Chromium browsers (Firefox, Safari) — the File System Access API is Chromium-only |
| `CanSave` | Same as `CanOpen` |
| `CanPickFolder` | WASM (no folder picker API), some older Linux DEs |

Always guard picker calls with these checks. On unsupported platforms, the picker throws `PlatformNotSupportedException`.

---

## 8. Bookmarks — what they are and when they matter

Some platforms (macOS sandbox, Android) grant temporary access to a picked file. Once the app restarts, the access token expires. A bookmark is a persistent, serialized token that re-establishes access across app restarts.

```csharp
// Save bookmark (pseudo-code)
var bookmarkId = await storage.SaveBookmarkAsync(file);

// Restore access later
var restoredFile = await storage.OpenBookmarkAsync(bookmarkId);
```

Bookmarks are stored as strings. Persist them in application settings. On macOS, the bookmark data is a `NSData` blob that the security-scoped bookmark API produces. On Android, it is a content URI. On Windows and standard Linux, bookmarks are not needed — file paths work across restarts.

**When to use:** any app that lets the user pick a working directory or recently-opened files and must persist that access after restart (image editors, document viewers, file managers).

---

## Key differences from the original

| Concept | Original says | Why it matters |
|---------|---------------|----------------|
| `TopLevel.GetTopLevel` | Call from any control | Must be after attachment to visual tree |
| File type hints | Three properties | Each targets a different OS; skipping one breaks filtering on that OS |
| `CanOpen` | Check before picker | Prevents `PlatformNotSupportedException` on non-Chromium browsers |
| `TryGetLocalPath` | Returns path | Returns `null` on browser/mobile — code must handle both paths |
| Bookmarks | Mentioned in table | Essential for macOS sandbox and Android content URIs |

---

## See Also

- [034 — File Pickers and Platform Services](034-file-pickers-platform-services.md) — the original tutorial
- [034E — File Pickers and Platform Services (examples)](034-file-pickers-platform-services-examples.md)
- [037 — App Lifetimes and Splash Screen](037-app-lifetimes-splash-screen.md) — platform-aware bootstrapping
- [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md) — platform-specific code strategies
- [Avalonia Docs: StorageProvider](https://docs.avaloniaui.net/docs/concepts/storage-provider)
