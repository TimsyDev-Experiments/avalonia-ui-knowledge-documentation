---
tier: intermediate
topic: interactions
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 019-drag-drop.md
---

# 019V — Drag & Drop: An In-Depth Companion

**Why this exists:** The original tutorial covers the basic API surface of Avalonia 12's drag-drop system. This companion explains *how the drag-drop pipeline works internally*, *why the API changed from v11*, *how `DataTransfer` differs from the old `DataObject`*, *how to handle cross-application scenarios*, and *what happens at each stage of the drag operation*.

**Cross-reference:** Original tutorial at [019-drag-drop.md](019-drag-drop.md).

---

## 1. The drag-drop pipeline

A drag-drop operation in Avalonia proceeds through these stages:

1. **Drag initiation** — A `DragGestureRecognizer` attached to the source element detects a drag gesture (typically pointer pressed + moves beyond a threshold).
2. **DragStarting** — The `DragStarting` event fires. You populate a `DataTransfer` object with the data being dragged.
3. **Drag visual** — The platform creates a drag visual (a semi-transparent copy of the source element or a system-provided drag cursor).
4. **DragOver** — As the pointer moves over potential drop targets, the `DragOver` event fires on each target. You inspect `e.DataTransfer` and set `e.DragEffects` to indicate what the target will do with the data.
5. **Drop** — When the user releases the pointer over a valid drop target, the `Drop` event fires. You read the data from `e.DataTransfer` and process it.
6. **Completion** — `DragDrop.DoDragDropAsync` (if initiated programmatically) returns the `DragEffects` value indicating what effect was performed.

**Why the async API:** Drag-drop is a modal user operation. The calling code must wait until the user completes or cancels the drag. The `async` method does not block the UI thread — it returns a `Task<DragEffects>` that completes when the drag ends.

---

## 2. DragGestureRecognizer — how it detects drags

```xml
<TextBlock.Gestures>
  <DragGestureRecognizer DragStarting="OnDragStarting"
                         CanDrag="True" />
</TextBlock.Gestures>
```

`DragGestureRecognizer` is a `GestureRecognizer` subclass. It monitors pointer events on the associated control:

1. Listens for `PointerPressed`. Records the starting point.
2. Listens for `PointerMoved`. If the pointer moves beyond a threshold distance (typically 4-8 pixels) while the button is held, the drag begins.
3. If `CanDrag` is `true`, it fires `DragStarting`. You must set `e.Data` in the event handler — if you do not, the drag is cancelled.
4. After `DragStarting`, the platform takes over: it captures the pointer, creates a drag visual, and enters the drag loop.

**Minimum drag distance:** The user must move the pointer a few pixels after pressing before the drag starts. This prevents accidental drags when the user merely clicks. The exact threshold is platform-dependent.

**CanDrag binding:** `CanDrag` can be data-bound to a ViewModel property to conditionally enable/disable dragging:

```xml
<DragGestureRecognizer CanDrag="{Binding IsDraggable}" />
```

---

## 3. DataTransfer and DataTransferItem — what changed from v11

```csharp
private async void OnDragStarting(object? sender, DragStartingEventArgs e)
{
    if (sender is TextBlock textBlock && textBlock.DataContext is TodoItem item)
    {
        var data = new DataTransfer();
        var item = new DataTransferItem();
        item.Set(DataFormat.Text, item.Title);
        data.Add(item);

        e.Data = data;
    }
}
```

### What DataTransfer replaces

In Avalonia 11, drag data was stored in `DataObject` (which wrapped `IDataObject`). The `DataObject` class was a direct copy of WPF's `DataObject`, using COM-style `IDataObject` for cross-process drag-drop. It was complex, Windows-centric, and had threading issues.

In Avalonia 12, `DataTransfer` is a simplified container:

- It holds a list of `DataTransferItem` objects.
- Each `DataTransferItem` stores one piece of data in multiple formats.
- The format is identified by `DataFormat` (a class with a `Name` property and a static set of well-known formats).

### DataFormat well-known types

| Static field | Represents |
|---|---|
| `DataFormat.Text` | Plain text (`string`) |
| `DataFormat.Files` | File paths (`IEnumerable<string>`) |
| `DataFormat.Html` | HTML content |
| `DataFormat.Rtf` | Rich text |
| `DataFormat.Bitmap` | Bitmap image |

You can also create custom `DataFormat` instances:

```csharp
public static readonly DataFormat CustomFormat = new("MyApp.CustomFormat");
```

### Setting and getting data

Set:

```csharp
var item = new DataTransferItem();
item.Set(DataFormat.Text, "Hello");
item.Set(DataFormat.Html, "<b>Hello</b>");
data.Add(item);
```

Get (on the drop side):

```csharp
if (e.DataTransfer.Contains(DataFormat.Text))
{
    var text = await e.DataTransfer.TryGetTextAsync();
}
```

**Why `TryGetTextAsync` is async:** On some platforms (macOS, Linux), the dragged data is not immediately available — the source application streams it on request. The method uses a platform interop call that may block briefly.

---

## 4. Drop target — what DragOver should do

```csharp
private void OnDragOver(object? sender, DragEventArgs e)
{
    e.DragEffects = e.DataTransfer.Contains(DataFormat.Text)
        ? DragEffects.Copy
        : DragEffects.None;
}
```

**What `DragOver` does:** It fires repeatedly as the pointer moves over the control. The handler must set `e.DragEffects` to communicate what will happen if the user drops here. The OS uses this to:

- Change the mouse cursor (copy icon, move icon, no-drop icon).
- Indicate to the user whether the drop is valid.
- Determine whether the source should be notified of the result.

**DragEffects values:**

| Value | Cursor | Meaning |
|---|---|---|
| `Copy` | + icon | Data will be copied |
| `Move` | Move icon | Data will be moved (source should delete) |
| `Link` | Link icon | Data will be linked |
| `None` | No-drop cursor | Drop is not allowed here |
| `All` | Varies | All effects are acceptable |

**Multiple effects:** You can combine flags:

```csharp
e.DragEffects = DragEffects.Copy | DragEffects.Move;
```

The platform chooses one based on modifier keys (Ctrl = Copy, Shift = Move). The chosen effect is returned from `DoDragDropAsync`.

**Performance:** `DragOver` fires frequently (many times per second). Keep the handler fast. Do not allocate, do not query the database, do not parse the data. Check only the data format. If you need to check the actual content, defer to the `Drop` handler.

---

## 5. Drop — what happens

```csharp
private async void OnDrop(object? sender, DragEventArgs e)
{
    var text = await e.DataTransfer.TryGetTextAsync();

    if (sender is ListBox listBox && text is not null)
    {
        var vm = listBox.DataContext as MainViewModel;
        vm?.AddItem(text);
    }
}
```

**What happens step by step:**

1. The user releases the pointer over the drop target.
2. The `Drop` event fires on the target element.
3. Your handler reads the data from `e.DataTransfer`.
4. You process the data (add to collection, update state, etc.).
5. After the handler returns, the drag operation completes.
6. The source's `DoDragDropAsync` (if started from code) completes with the `DragEffects` value.

**Thread safety:** The `Drop` handler runs on the UI thread. You can safely update ViewModel properties and ObservableCollections. If you need to do async work (e.g., save a file), you can await in the handler (it is `async void`).

**What happens if the user drops outside any valid target:** The drag is cancelled. `DoDragDropAsync` completes with `DragEffects.None`. No `Drop` event fires.

---

## 6. Programmatic drag initiation

```csharp
var data = new DataTransfer();
var item = new DataTransferItem();
item.Set(DataFormat.Text, "Hello from code");
data.Add(item);

var result = await DragDrop.DoDragDropAsync(
    sourceElement,
    data,
    DragEffects.Copy | DragEffects.Move);
```

**When to use programmatic drag initiation:**

- The drag gesture is not a simple pointer drag (e.g., triggered by a button click).
- You need to start a drag from code-behind without a `DragGestureRecognizer`.
- You are implementing custom drag behavior (e.g., drag from a `DataGrid` cell).

**What `DoDragDropAsync` returns:** The `DragEffects` value chosen by the drop target (or `None` if cancelled). This tells you whether the data was copied, moved, or not dropped.

**Post-drop action:** If the result is `DragEffects.Move`, the source should delete the original data:

```csharp
var result = await DragDrop.DoDragDropAsync(sourceElement, data, DragEffects.Move);
if (result == DragEffects.Move)
{
    // Remove the item from the source collection
    Items.Remove(draggedItem);
}
```

---

## 7. Cross-application drag-drop

```csharp
private async void OnDragImage(object? sender, DragStartingEventArgs e)
{
    var filePath = Path.GetFullPath("Assets/image.png");
    var file = new DataTransferItem();
    file.Set(DataFormat.Files, new[] { filePath });
    e.Data.Add(file);
}
```

**What happens cross-app:**

1. Your app sets `DataFormat.Files` with an array of file paths.
2. The platform serializes this into the OS drag-drop system (OLE on Windows, NSDragging on macOS, XDnD on Linux).
3. The target application (e.g., Explorer/Finder, a browser, another Avalonia app) receives the file paths.
4. If the target is outside your app, it reads the files from the paths. The paths must be absolute and accessible to the target.

**File paths vs. file contents:** For small files, you can embed the file bytes directly:

```csharp
var bytes = File.ReadAllBytes("Assets/image.png");
var item = new DataTransferItem();
item.Set(DataFormat.Bitmap, bytes);
```

For large files, pass the file path and let the target read it.

**Security:** The target app receives the file path as a string. If the file is on a network share or a removable drive, the target must have permission to read it.

---

## 8. Drag between Avalonia controls within the same app

For intra-app drag-drop, you can use custom `DataFormat` instances:

```csharp
public static readonly DataFormat TodoItemFormat = new("MyApp.TodoItem");

// Source
private void OnDragStarting(object? sender, DragStartingEventArgs e)
{
    if (sender is Control c && c.DataContext is TodoItem item)
    {
        var data = new DataTransfer();
        var dataItem = new DataTransferItem();
        dataItem.Set(TodoItemFormat, item);  // Direct object reference
        data.Add(dataItem);
        e.Data = data;
    }
}

// Target
private void OnDrop(object? sender, DragEventArgs e)
{
    if (e.DataTransfer.Contains(TodoItemFormat))
    {
        var todoItem = e.DataTransfer.Get(TodoItemFormat) as TodoItem;
        // Process the item
    }
}
```

**Important:** Custom formats work only within the same process. Cross-process drag-drop uses only well-known formats (Text, Files, Bitmap, etc.) because the data must be serialized through the OS clipboard/proxy.

---

## 9. Common mistakes

**Mistake 1: AllowDrop="False" on the drop target.**

The `DropGestureRecognizer` needs `AllowDrop="True"`. Without it, the drop target never receives `DragOver` or `Drop` events.

**Mistake 2: Not setting e.DragEffects in DragOver.**

If you never set `e.DragEffects`, it defaults to `DragEffects.None` — the target always shows the no-drop cursor. Set it in every `DragOver` handler.

**Mistake 3: Using old DataObject/DataFormats from v11.**

Avalonia 12 removed `DataObject` and `DataFormats`. The new API uses `DataTransfer`, `DataTransferItem`, and `DataFormat`. Using the old types causes compilation errors.

**Mistake 4: DragStarting does not set e.Data.**

If `DragStarting` sets `e.Data = null` or does not set it at all, the drag is cancelled. Always set `e.Data` to a populated `DataTransfer`.

**Mistake 5: DragStarted from code without a source element.**

`DragDrop.DoDragDropAsync` requires a non-null `Interactive sourceElement`. Pass the control that visually "owns" the drag (the dragged item's container, the list, etc.).

**Mistake 6: Not handling cross-thread drag-drop.**

Drag-drop operations originate from the platform's input system, which runs on the UI thread. All events fire on the UI thread — no dispatcher marshaling needed.

---

## Key Takeaways

- Use `DragGestureRecognizer` for gesture-initiated drags; `DragDrop.DoDragDropAsync` for programmatic drags.
- `DataTransfer` / `DataTransferItem` replaces the old `DataObject` / `IDataObject` API from v11.
- Set `e.DragEffects` in `DragOver` to indicate whether the target accepts the drop and what effect applies.
- Use `e.DataTransfer.Contains()` to check format availability; `TryGetTextAsync()` to read text data.
- Cross-app drag uses well-known `DataFormat` values (Text, Files, Bitmap). Custom formats work in-process only.
- `AllowDrop="True"` is required on every drop target element.
- `DragDrop.DoDragDropAsync` returns the effect that was performed — check for `DragEffects.Move` to delete the source.

---

## See Also

- [019 — Drag & Drop (original)](019-drag-drop.md)
- [015 — Item Lists](015-item-lists.md) (drag items between lists)
- [019E — Drag & Drop (examples)](019-drag-drop-examples.md)
- [Avalonia Docs: Drag & Drop](https://docs.avaloniaui.net/docs/input/drag-and-drop)
