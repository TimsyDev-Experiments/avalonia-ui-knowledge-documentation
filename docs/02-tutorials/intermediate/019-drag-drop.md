---
tier: intermediate
topic: interactions
estimated: 8 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 019 — Drag & Drop

**What you'll learn:** Implement drag-and-drop operations between controls using Avalonia 12's async drag-drop API.

**Prerequisites:** [002 — Command Binding](../basics/002-command-binding.md)

---

## 1. Enable drag from a control

```xml
<TextBlock Text="{Binding Title}"
           AllowDrop="False">

  <!-- Drag starting -->
  <TextBlock.Gestures>
    <DragGestureRecognizer DragStarting="OnDragStarting"
                           CanDrag="True" />
  </TextBlock.Gestures>
</TextBlock>
```

In code-behind:

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

// Avalonia 12 uses DataTransfer/DataTransferItem instead of the old DataObject/DataFormats
```

---

## 2. Enable drop on a target

```xml
<ListBox ItemsSource="{Binding Items}"
         AllowDrop="True">

  <ListBox.Gestures>
    <DropGestureRecognizer DragOver="OnDragOver"
                           Drop="OnDrop" />
  </ListBox.Gestures>
</ListBox>
```

```csharp
private void OnDragOver(object? sender, DragEventArgs e)
{
    // Only accept text drops
    e.DragEffects = e.DataTransfer.Contains(DataFormat.Text)
        ? DragEffects.Copy
        : DragEffects.None;
}

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

> In Avalonia 12, `DragEventArgs.Data` is now `DragEventArgs.DataTransfer`.

---

## 3. Drag between applications

```xml
<!-- Source -->
<Image Source="/Assets/image.png">
  <Image.Gestures>
    <DragGestureRecognizer DragStarting="OnDragImage"
                           CanDrag="True" />
  </Image.Gestures>
</Image>
```

```csharp
private async void OnDragImage(object? sender, DragStartingEventArgs e)
{
    var filePath = Path.GetFullPath("Assets/image.png");
    var file = new DataTransferItem();
    file.Set(DataFormat.Files, new[] { filePath });
    e.Data.Add(file);
}
```

---

## 4. Async drag-drop (Avalonia 12 API)

```csharp
// Initiate drag from code (not gesture)
var data = new DataTransfer();
var item = new DataTransferItem();
item.Set(DataFormat.Text, "Hello from code");
data.Add(item);

var result = await DragDrop.DoDragDropAsync(
    sourceElement,
    data,
    DragEffects.Copy | DragEffects.Move);

// result tells you which effect was performed
```

In v12, `DragDrop.DoDragDrop` was renamed to `DragDrop.DoDragDropAsync`.

---

## 5. Drop effects

```csharp
e.DragEffects = DragEffects.Copy;   // Show + icon
e.DragEffects = DragEffects.Move;   // Show move icon
e.DragEffects = DragEffects.Link;   // Show link icon
e.DragEffects = DragEffects.None;   // Show no-drop cursor
```

Combine with `DragDrop.DoDragDropAsync` response to apply the selected effect.

---

## Key Takeaways

- Use `DragGestureRecognizer` for drag source, `DropGestureRecognizer` for drop target
- `DataTransfer` / `DataTransferItem` replace the old `DataObject` API in v12
- `DragDrop.DoDragDropAsync` is the async replacement for `DragDrop.DoDragDrop`
- Always set `AllowDrop="True"` on drop targets
- Check `e.DataTransfer.Contains()` in `DragOver` to validate drop data

---

## See Also

- [034 — Drag & Drop Workflows](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/34-dragdrop-workflows.md) (plugin ref)
- [Avalonia Docs: Drag & Drop](https://docs.avaloniaui.net/docs/input/drag-and-drop)
