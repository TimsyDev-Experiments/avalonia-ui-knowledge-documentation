---
tier: intermediate
topic: services
estimated: 16 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 062V — Clipboard & Launcher (verbose companion)

**What this covers:** IClipboard interface deep dive, DataFormat internals, async data transfer lifecycles, and launcher edge cases.

**Prerequisites:** 062 — Clipboard & Launcher core

---

## 1. IClipboard interface

```csharp
public interface IClipboard
{
    Task<IAsyncDataTransfer?> TryGetDataAsync();
    Task<IAsyncDataTransfer?> TryGetInProcessDataAsync();
    Task SetDataAsync(IAsyncDataTransfer data);
    Task ClearAsync();
    Task FlushAsync();
}
```

### TryGetInProcessDataAsync

Retrieves the exact `IAsyncDataTransfer` previously placed by `SetDataAsync` without touching the OS clipboard. Returns `null` if clipboard content changed or platform doesn't support it. Supported on Windows, macOS, X11.

---

## 2. DataFormat kinds

| Kind | Scope | Identifier rules |
|------|-------|-----------------|
| `Universal` | Cross-platform | Built-in: Text, File, Bitmap |
| `Platform` | OS-specific | Must match OS clipboard naming |
| `Application` | Your app only | ASCII letters, digits, `.`, `-` |

### Platform format identifier differences

| Format | Windows | macOS | Linux |
|--------|---------|-------|-------|
| HTML | `HTML Format` | `public.html` | `text/html` |
| RTF | `Rich Text Format` | `public.rtf` | `text/rtf` |

```csharp
var htmlFormat = OperatingSystem.IsMacOS()
    ? DataFormat.CreateStringPlatformFormat("public.html")
    : DataFormat.CreateStringPlatformFormat("text/html");
```

---

## 3. DataTransfer lifecycle

```csharp
// Creating data to write
var item = DataTransferItem.CreateText("Hello");
var data = new DataTransfer();
data.Add(item);

// Ownership transfers to clipboard
await clipboard.SetDataAsync(data);
// Do NOT dispose "data" — Avalonia owns it now

// Reading later
using var readData = await clipboard.TryGetDataAsync();
if (readData is not null)
{
    try
    {
        var text = await readData.TryGetTextAsync();
        // use text
    }
    finally
    {
        readData.Dispose();
    }
}
```

The `using` on `TryGetDataAsync` result is required — it holds native resources.

---

## 4. Thread safety

`IAsyncDataTransferItem.TryGetRawAsync()` may be called from **any thread**. Never call `Dispatcher.UIThread` inside it — will deadlock.

```csharp
// BAD — will deadlock on some platforms
item.TryGetRawAsync = format =>
{
    return Dispatcher.UIThread.InvokeAsync(() => ComputeValue(format));
};

// GOOD — compute synchronously or return a cached value
item.TryGetRawAsync = format =>
{
    return Task.FromResult<object?>(_cachedValues[format]);
};
```

---

## 5. Launcher return values

`LaunchUriAsync` returns `true` if the OS **accepted** the request, not whether the target app actually opened. Reasons for `false`:

- No app registered for the scheme
- OS policy blocked the request (e.g., sandbox)
- Invalid URI scheme

```csharp
bool accepted = await launcher.LaunchUriAsync(new Uri("steam://run/730"));
if (!accepted)
{
    // Fall back to showing a message or opening a web URL
    await launcher.LaunchUriAsync(new Uri("https://store.steampowered.com/app/730"));
}
```

---

## 6. FlushAsync

On Windows, `FlushAsync()` persists lazy clipboard data so it survives app shutdown. On Linux (X11) it also persists. On macOS, data persists without flushing. On browser/Android/iOS, FlushAsync is a no-op.

```csharp
// Without flush, data disappears when app exits
await clipboard.SetTextAsync("Important");
await clipboard.FlushAsync(); // Now available after exit
```

---

## 7. Clipboard events

Avalonia does not expose native clipboard-change events. Polling alternatives:

- Check clipboard on `Window.Activated`
- Use a platform-specific timer (e.g., `DispatcherTimer` polling every 500ms)
- On Windows, use `ClipboardNotification` Win32 API via `NativeControlHost`

---

## See Also

- [062 — Clipboard & Launcher (core)](062-clipboard-launcher.md)
- [062E — Clipboard & Launcher (examples)](062-clipboard-launcher-examples.md)
- [Drag and Drop](https://docs.avaloniaui.net/docs/input-interaction/drag-and-drop)
- [TopLevel](https://docs.avaloniaui.net/docs/fundamentals/top-level)
