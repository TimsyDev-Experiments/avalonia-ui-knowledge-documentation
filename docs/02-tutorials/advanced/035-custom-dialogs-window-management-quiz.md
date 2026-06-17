---
tier: advanced
topic: windowing
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 035-custom-dialogs-window-management.md
---

# Quiz — Custom Dialogs

```quiz
Q: Which Window property is required when calling ShowDialog on a modal dialog?
A. WindowStartupLocation="CenterOwner" and ShowInTaskbar="False" (correct) || CenterOwner positions the dialog relative to the owner; ShowInTaskbar=False prevents duplicate taskbar entries.
B. WindowDecorations="None" and CanResize="False" || Decorations and resize control appearance, not modal dialog behavior.
C. Topmost="True" and WindowState="Normal" || Topmost is unrelated to modal dialogs; it forces the window above all others.
D. ExtendClientAreaToDecorationsHint="True" || This extends content into the title bar region, irrelevant to modal dialog setup.
Explanation: Modal dialogs should be centered on the owner and hidden from the taskbar. ShowInTaskbar=False and CenterOwner are the standard pattern.
```

```quiz
Q: How does a ViewModel communicate a dialog result back to the caller without a direct Window reference?
A. Use an IDialogService that creates the dialog, binds the ViewModel, awaits ShowDialog, and returns the result (correct) || IDialogService abstracts window creation and result marshaling so ViewModels stay testable and window-agnostic.
B. Store the result in a static property on the Application class || Static state is fragile, untestable, and does not support concurrent dialogs.
C. Raise an event on the ViewModel that the Window code-behind listens to || This couples the ViewModel to the window; the ViewModel should not know about Window events.
D. Use Application.Current.Windows.OfType<ConfirmDialog>().First() to read a property || Searching all open windows is brittle and violates MVVM separation.
Explanation: An IDialogService decouples the ViewModel from Window. The ViewModel calls a method and receives a Task<bool>.
```

```quiz
Q: Which override prevents a window from closing when the user has unsaved changes?
A. 
```csharp
protected override void OnClosing(WindowClosingEventArgs e)
{
    if (HasUnsavedChanges) { e.Cancel = true; _ = ShowSavePromptAsync(); }
}
```
 (correct) || Setting e.Cancel = true in OnClosing aborts the close. The async save prompt is shown after cancellation.
B. `protected override void OnClosed(EventArgs e) { if (HasUnsavedChanges) ShowSavePrompt(); }` || OnClosed fires after the window has already closed — too late to prevent it.
C. `protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e) { if (e.Property == Window.ClosingReasonProperty) ... }` || ClosingReason is not a real property; this approach does not exist.
D. Handle the `Window.Close` routed event || Window does not expose a Close routed event; the closing lifecycle uses OnClosing.
Explanation: OnClosing receives WindowClosingEventArgs with a Cancel property. Set it to true to prevent the close, then show a save prompt asynchronously.
```

```quiz
Q: Which ElementRole on a Border within a chromeless window enables native drag behavior?
A. TitleBar (correct) || Setting WindowDecorationProperties.ElementRole="TitleBar" marks the region as a draggable title bar that integrates with the OS window manager.
B. Draggable || There is no Draggable role in Avalonia's WindowDecorationProperties.
C. Header || ElementRole values use specific constants (TitleBar, MinimizeButton, etc.), not generic strings like Header.
D. Caption || The correct role name is TitleBar, not Caption.
Explanation: WindowDecorationProperties.ElementRole="TitleBar" makes any element a drag region. The OS handles movement while the window has WindowDecorations="None".
```

```quiz
Q: Why use an overlay (in-window) dialog pattern instead of ShowDialog?
A. ShowDialog opens a separate OS window, which is unsupported on WASM and some mobile targets (correct) || On platforms without multiple window support (WASM, Android, iOS), overlay dialogs render within the existing window.
B. Overlay dialogs are always faster than native windows || Performance is not the primary reason; platform capability drives the choice.
C. ShowDialog cannot return a Task<bool> result || ShowDialog<bool?> returns a Task<bool?>, so it does support async results.
D. Overlay dialogs automatically support acrylic blur effects || AcrylicBlur works on native windows too; it is not exclusive to overlays.
Explanation: WASM, Android, and iOS cannot open separate OS windows. Overlay dialogs rendered in a Grid with a semi-transparent background are the cross-platform alternative.
```
