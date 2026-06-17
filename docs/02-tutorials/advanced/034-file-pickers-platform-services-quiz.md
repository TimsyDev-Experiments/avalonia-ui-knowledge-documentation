---
tier: advanced
topic: platform-services
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 034-file-pickers-platform-services.md
---

# Quiz — File Pickers & Platform Services

```quiz
Q: How do you access the StorageProvider from a UserControl in a ViewModel-driven view?
A. Call TopLevel.GetTopLevel(this) from the UserControl code-behind after it is attached to the visual tree (correct) || TopLevel.GetTopLevel requires a control reference; pass it from the view or use a service injected into the ViewModel.
B. Resolve IStorageProvider directly from the DI container || IStorageProvider is not registered in DI by default; the correct instance lives on TopLevel.
C. Use Application.Current.StorageProvider || Application does not expose StorageProvider; it is a TopLevel-scoped service.
D. Call StorageProvider.GetDefault() || There is no static GetDefault method on StorageProvider.
Explanation: Platform services such as StorageProvider and Clipboard are accessed through TopLevel.GetTopLevel(control), available once the control is attached to a window.
```

```quiz
Q: Which code correctly opens a file picker filtered to PNG images with multi-select disabled?
A. 
```csharp
var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
{
    Title = "Select image",
    AllowMultiple = false,
    FileTypeFilter = new[]
    {
        new FilePickerFileType("PNG Images")
        {
            Patterns = new[] { "*.png" }
        }
    }
});
```
 (correct) || FilePickerOpenOptions with AllowMultiple=false and a FileTypeFilter with Patterns limits selection to single PNG files.
B. `storage.OpenFilePickerAsync("Select image", "*.png")` || OpenFilePickerAsync accepts FilePickerOpenOptions, not positional string arguments.
C. `storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Select image" })` || Without a FileTypeFilter, all file types are shown; also patterns must be specified.
D. `storage.OpenAsync(FilePickerMode.Open, "*.png")` || There is no OpenAsync method on StorageProvider.
Explanation: FilePickerOpenOptions configures title, multi-select, and file type filters. Pass the options object to OpenFilePickerAsync.
```

```quiz
Q: What does `folder.TryGetLocalPath()` return for a folder picked via OpenFolderPickerAsync?
A. The local file-system path string, or null if the folder has no local path (correct) || On some platforms (browser, mobile) the picked folder may not map to a local path; TryGetLocalPath returns null in that case.
B. A file:// URI string || TryGetLocalPath returns a plain path string, not a URI.
C. The folder's bookmark ID for persisted access || Bookmark IDs are obtained through a different API (StorageBookmarkFolder).
D. Always the full path regardless of platform || On browser/WASM there is no local file system; TryGetLocalPath returns null.
Explanation: TryGetLocalPath returns the native path when available (desktop) and null on platforms without a local file system.
```

```quiz
Q: Which clipboard API correctly puts custom structured data on the system clipboard?
A. 
```csharp
var data = new DataObject();
data.Set(DataFormats.Text, "value");
await clipboard.SetDataObjectAsync(data);
```
 (correct) || DataObject holds multiple formats; SetDataObjectAsync pushes it onto the system clipboard.
B. `await clipboard.SetDataAsync("value", DataFormats.Text)` || There is no SetDataAsync method; use SetDataObjectAsync with a DataObject.
C. `await clipboard.SetTextAsync("value")` || SetTextAsync only handles plain text, not custom data formats.
D. `await clipboard.SetDataObject("value")` || SetDataObject does not exist; the async overload SetDataObjectAsync is the correct method.
Explanation: Use DataObject to package multiple formats, then call SetDataObjectAsync to transfer them to the system clipboard.
```

```quiz
Q: A file-save operation on a Linux environment with no desktop (e.g., a kiosk) should be guarded by which check?
A. 
```csharp
if (!storage.CanSave) return;
```
 (correct) || CanSave returns false when the platform does not support file save dialogs, preventing a runtime failure.
B. `if (storage is null) return;` || The StorageProvider reference is not null; it is the capability that may be absent.
C. `if (!OperatingSystem.IsWindows()) return;` || Linux with a DE supports save; this incorrectly excludes valid platforms.
D. `if (!storage.CanOpen) return;` || CanOpen checks file-open support, not file-save support.
Explanation: Always check CanSave before invoking SaveFilePickerAsync on platforms where the dialog may not be available.
```
