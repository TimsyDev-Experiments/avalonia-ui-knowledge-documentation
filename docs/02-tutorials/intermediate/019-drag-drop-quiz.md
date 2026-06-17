---
tier: intermediate
topic: interactions
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 019-drag-drop.md
---

# Quiz — Drag & Drop

```quiz
Q: Which API replaces the old `DataObject` class in Avalonia 12's drag-drop system?
A. DragPackage || Incorrect — there is no DragPackage type in the Avalonia 12 drag-drop API.
B. DataTransfer and DataTransferItem (correct) || Correct — Avalonia 12 uses DataTransfer as the data container and DataTransferItem to hold individual payloads.
C. ClipboardData || Incorrect — ClipboardData is not a drag-drop type; the clipboard uses a separate API.
D. DragDataCollection || Incorrect — there is no DragDataCollection type in Avalonia 12.
Explanation: DataTransfer replaces DataObject, and DataTransferItem replaces the per-format value storage for drag-drop operations.
```

```quiz
Q: What method initiates a drag operation programmatically (not from a gesture recognizer) in Avalonia 12?
A. DragDrop.DoDragDrop || Incorrect — DoDragDrop was the synchronous v11 API; it was renamed in v12.
B. DragDrop.StartDragAsync || Incorrect — no such method exists; the correct name is DoDragDropAsync.
C. DragDrop.InitiateDrag || Incorrect — there is no InitiateDrag method in the DragDrop class.
D. DragDrop.DoDragDropAsync (correct) || Correct — DoDragDropAsync is the async v12 replacement that accepts source element, DataTransfer, and allowed DragEffects.
Explanation: DragDrop.DoDragDropAsync is the async entry point for programmatic drag initiation, returning the DragEffects result.
```

```quiz
Q: Which property must be set to `True` on a control for it to receive drop events?
A. CanDrag || Incorrect — CanDrag is on DragGestureRecognizer, which controls drag source behavior, not drop target acceptance.
B. IsHitTestVisible || Incorrect — IsHitTestVisible affects pointer hit-testing but does not enable drop events.
C. AllowDrop (correct) || Correct — AllowDrop="True" must be set on any control that should accept drop events via DropGestureRecognizer.
D. IsEnabled || Incorrect — IsEnabled controls general interactivity but does not specifically enable drop targeting.
Explanation: AllowDrop is the boolean property that registers a control as a drop target in the Avalonia input system.
```

```quiz
Q: Identify the bug in this DragOver handler:
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormat.Text)
            ? DragEffects.Copy
            : DragEffects.None;
    }
A. DragEventArgs does not have a DragEffects property || Incorrect — DragEventArgs does have a DragEffects property for setting the visual effect.
B. The `e.Data` property should be `e.DataTransfer` instead (correct) || Correct — in Avalonia 12, DragEventArgs.Data was renamed to DragEventArgs.DataTransfer; `e.Data` does not exist.
C. DataFormat.Text is not a valid format in Avalonia || Incorrect — DataFormat.Text is valid; the issue is with accessing data on the event args.
D. DragEffects.Copy should be DragEffects.Move for text drops || Incorrect — Copy vs Move is a semantic choice; either is valid and not a bug.
Explanation: Avalonia 12 renamed `DragEventArgs.Data` to `DragEventArgs.DataTransfer`; using the old property name will cause a compilation error.
```

```quiz
Q: What does the `DragEffects` enum control in a DragOver handler?
A. The animation played when the drag starts || Incorrect — DragEffects controls cursor feedback and the result of the drop, not drag-start animation.
B. The visual cursor feedback (copy, move, link, no-drop) shown over the target (correct) || Correct — setting DragEffects.Copy shows a + icon, DragEffects.Move shows a move icon, and DragEffects.None shows the no-drop cursor.
C. The opacity of the dragged element || Incorrect — DragEffects does not affect element opacity.
D. The allowed mouse buttons during drag || Incorrect — mouse button filtering is not controlled by DragEffects.
Explanation: DragEffects tells both the system and the drag source what operation is allowed, which is reflected in the cursor shown to the user.
```
