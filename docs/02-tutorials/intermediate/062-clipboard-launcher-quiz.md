---
tier: intermediate
topic: services
avalonia-version: 12.0.4
quiz-format: multiple-choice
---

# 062Q — Clipboard & Launcher (quiz)

## Q1. How do you access the clipboard service in Avalonia?

- [ ] A. `Clipboard.Default`
- [ ] B. `TopLevel.Clipboard` or `window.Clipboard`
- [ ] C. `IClipboard.Instance`
- [ ] D. `Application.Clipboard`

**Answer:** B. `IClipboard` is accessed from a `TopLevel` (e.g., `Window`) instance.

---

## Q2. Which method writes plain text to the clipboard?

- [ ] A. `clipboard.WriteTextAsync("text")`
- [ ] B. `clipboard.SetTextAsync("text")`
- [ ] C. `clipboard.PutText("text")`
- [ ] D. `clipboard.Text = "text"`

**Answer:** B. `SetTextAsync(string)` is the extension method for writing text.

---

## Q3. What does `FlushAsync()` do on the clipboard?

- [ ] A. Clears the clipboard
- [ ] B. Persists lazy clipboard data so it survives app shutdown
- [ ] C. Copies data to a backup
- [ ] D. Refreshes stale clipboard handles

**Answer:** B. `FlushAsync()` forces the system to persist lazy data on Windows and X11.

---

## Q4. Which clipboard method should you avoid calling inside TryGetRawAsync implementations?

- [ ] A. `SetTextAsync`
- [ ] B. `Dispatcher.UIThread.InvokeAsync`
- [ ] C. `TryGetTextAsync`
- [ ] D. `ClearAsync`

**Answer:** B. `TryGetRawAsync` may be called from any thread; calling the dispatcher causes deadlocks.

---

## Q5. Which method opens a URL in the default browser?

- [ ] A. `launcher.OpenUri(uri)`
- [ ] B. `launcher.LaunchUriAsync(uri)`
- [ ] C. `launcher.StartUri(uri)`
- [ ] D. `launcher.NavigateTo(uri)`

**Answer:** B. `LaunchUriAsync(Uri)` starts the default app for the URI scheme.

---

## Q6. What does `LaunchUriAsync` return?

- [ ] A. `true` if the target app opened successfully
- [ ] B. `true` if the OS accepted the request
- [ ] C. A `Process` object for the launched app
- [ ] D. `void`

**Answer:** B. Returns `bool` — `true` means the OS accepted the request, not that the app opened.

---

## Q7. Which is NOT a universal data format?

- [ ] A. `DataFormat.Text`
- [ ] B. `DataFormat.File`
- [ ] C. `DataFormat.Html`
- [ ] D. `DataFormat.Bitmap`

**Answer:** C. Avalonia has three universal formats: Text, File, and Bitmap. HTML is a platform format.

---

## Q8. When using `SetDataAsync`, who owns the `IAsyncDataTransfer` object?

- [ ] A. The caller must dispose it
- [ ] B. Avalonia takes ownership and disposes it automatically
- [ ] C. It lives forever
- [ ] D. The garbage collector handles it

**Answer:** B. Do NOT dispose the object passed to `SetDataAsync` — Avalonia takes ownership.

---

## Q9. Which Launcher extension methods are available only on desktop platforms?

- [ ] A. `LaunchUriAsync`
- [ ] B. `LaunchFileInfoAsync` and `LaunchDirectoryInfoAsync`
- [ ] C. `LaunchFileAsync`
- [ ] D. All Launcher methods are cross-platform

**Answer:** B. `LaunchFileInfoAsync(FileInfo)` and `LaunchDirectoryInfoAsync(DirectoryInfo)` require non-sandboxed desktop access.

---

## Q10. What kind of DataFormat should you use for app-internal clipboard formats?

- [ ] A. `Universal`
- [ ] B. `Platform`
- [ ] C. `Application`
- [ ] D. `Custom`

**Answer:** C. `DataFormat.CreateStringApplicationFormat(...)` or `CreateBytesApplicationFormat` for app-specific formats.

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

- [062 — Clipboard & Launcher (core)](062-clipboard-launcher.md)
- [062V — Clipboard & Launcher (verbose)](062-clipboard-launcher-verbose.md)
- [062E — Clipboard & Launcher (examples)](062-clipboard-launcher-examples.md)
