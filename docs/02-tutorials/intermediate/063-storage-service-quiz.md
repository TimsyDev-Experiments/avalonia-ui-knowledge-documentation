---
tier: intermediate
topic: services
avalonia-version: 12.0.4
quiz-format: multiple-choice
---

# 063Q — Storage Service (quiz)

## Q1. How do you access the storage provider?

- [ ] A. `StorageProvider.Default`
- [ ] B. `TopLevel.StorageProvider` or `window.StorageProvider`
- [ ] C. `IStorageProvider.Instance`
- [ ] D. `Application.StorageProvider`

**Answer:** B. Access `IStorageProvider` from a `TopLevel` (e.g., `Window`) instance.

---

## Q2. Which method opens a file picker dialog?

- [ ] A. `storage.OpenFileAsync(options)`
- [ ] B. `storage.OpenFilePickerAsync(options)`
- [ ] C. `storage.ShowOpenDialog(options)`
- [ ] D. `storage.PickFileAsync(options)`

**Answer:** B. `OpenFilePickerAsync(FilePickerOpenOptions)` opens a file picker and returns selected files.

---

## Q3. What should you do before calling OpenFilePickerAsync to avoid runtime errors?

- [ ] A. Check `storage.CanOpen`
- [ ] B. Check `storage.IsAvailable`
- [ ] C. Initialize the picker
- [ ] D. Call `storage.PrepareAsync()`

**Answer:** A. Check `storage.CanOpen` first since some platforms don't support file pickers.

---

## Q4. How do you write content after saving a file?

- [ ] A. `file.WriteTextAsync(content)`
- [ ] B. `file.OpenWriteAsync()` then write to the stream
- [ ] C. `File.WriteAllText(file.Path, content)`
- [ ] D. `file.SaveAsync(content)`

**Answer:** B. `OpenWriteAsync()` returns a writeable stream.

---

## Q5. What does `SaveFilePickerWithResultAsync` return that `SaveFilePickerAsync` does not?

- [ ] A. The file size
- [ ] B. The selected file type filter
- [ ] C. The file creation date
- [ ] D. A cancellation token

**Answer:** B. It returns a `SaveFilePickerResult` with both the storage file and the `SelectedFileType` the user picked.

---

## Q6. Which of these is NOT a built-in FilePickerFileType?

- [ ] A. `FilePickerFileTypes.TextPlain`
- [ ] B. `FilePickerFileTypes.ImageAll`
- [ ] C. `FilePickerFileTypes.Video`
- [ ] D. `FilePickerFileTypes.Pdf`

**Answer:** C. There is no built-in `Video` file type. Avalonia provides All, TextPlain, ImageAll, ImageJpg, ImagePng, ImageWebP, and Pdf.

---

## Q7. How do you persist file access across app restarts?

- [ ] A. Save the file path to settings
- [ ] B. Use `IStorageBookmarkFile.SaveBookmarkAsync()` and store the bookmark ID
- [ ] C. Cache the file content
- [ ] D. Register the file with the OS

**Answer:** B. Bookmarks provide a platform-appropriate way to persist access across sessions.

---

## Q8. Which method retrieves special system folders like Documents or Desktop?

- [ ] A. `storage.GetSystemFolder()`
- [ ] B. `storage.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)`
- [ ] C. `storage.GetSpecialFolder()`
- [ ] D. `Environment.GetFolderPath()`

**Answer:** B. `TryGetWellKnownFolderAsync` is the cross-platform method for well-known folders.

---

## Q9. True or False: `TryGetFileFromPathAsync` works on all platforms including browser.

- [ ] A. True
- [ ] B. False

**Answer:** B. False. Path-based access is desktop-only (Windows, macOS, Linux). Browser/Android/iOS don't support it.

---

## Q10. Which interfaces must a bookmarked file implement?

- [ ] A. `IStorageFile` only
- [ ] B. `IStorageBookmarkFile` (extends `IStorageFile`)
- [ ] C. `IStorageItem` only
- [ ] D. `IBookmark` only

**Answer:** B. `IStorageBookmarkFile` extends `IStorageFile` and adds `SaveBookmarkAsync()`. The same pattern applies for folders via `IStorageBookmarkFolder`.

---

## Scoring

| Score | Interpretation |
|-------|---------------|
| 10/10 | Expert |
| 8-9 | Strong understanding |
| 6-7 | Getting there |
| <6 | Review the core tutorial |

---

## See Also

- [063 — Storage Service (core)](063-storage-service.md)
- [063V — Storage Service (verbose)](063-storage-service-verbose.md)
- [063E — Storage Service (examples)](063-storage-service-examples.md)
