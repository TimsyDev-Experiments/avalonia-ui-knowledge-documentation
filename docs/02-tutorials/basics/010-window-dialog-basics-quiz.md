---
tier: basics
topic: windows and dialogs
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 010-window-dialog-basics.md
---

# Quiz — Window Basics and Dialogs

```quiz
Q: How does ShowDialog(parent) differ from Show() in Avalonia?
A. ShowDialog(parent) displays the window modally, blocking interaction with parent until closed, and the caller can await a result — Show() opens modelessly and returns immediately (correct) || ShowDialog takes a Window parent reference, makes the new window modal over that parent, and returns a Task<bool?> that resolves when the dialog closes. Show() returns void; the new window floats independently.
B. ShowDialog centers on screen while Show() opens at default position || Centering behavior is controlled by WindowStartupLocation, not by the Show vs ShowDialog choice.
C. Show() requires a parent window argument; ShowDialog does not || The opposite is true: ShowDialog needs a parent, Show() does not.
D. ShowDialog opens the window minimized; Show() opens it normally || WindowState is independent of the show method. Both respect the window's configured WindowState.
Explanation: Show() is modeless — the calling code continues immediately and the new window operates independently. ShowDialog(parent) is modal: it takes a parent Window reference, blocks interaction with that parent, and returns an awaitable Task<bool?> whose result is set by the dialog's Close(value) call.
```

```quiz
Q: How do you correctly obtain a parent window reference for ShowDialog in Avalonia 12?
A. TopLevel.GetTopLevel(visual) — pass any control that is in the visual tree of the parent window (correct) || In Avalonia 12, TopLevel.GetTopLevel(Visual) is the standard way to traverse from any control up to its containing TopLevel (Window or other root). The method accepts a Visual and returns the TopLevel or null.
B. (Window)Application.Current.MainWindow || Application.Current.MainWindow gives the app's main window, not the current window. If the caller is on a different window, the dialog would appear over the wrong parent.
C. Window.GetWindow(this) || This WPF method does not exist in Avalonia. There is no static Window.GetWindow in the Avalonia API.
D. this.FindParent<Window>() || There is no built-in FindParent<T> on Control in Avalonia's base class library. TopLevel.GetTopLevel is the intended API.
Explanation: TopLevel.GetTopLevel(Visual) replaced earlier patterns like casting TopLevel.FromVisual() or accessing Parent directly. Pass a control reference from the target window (e.g., the Button that triggered the command, or a UserControl in the window's visual tree) to resolve the correct parent TopLevel.
```

```quiz
Q: What does calling Close(value) do in a window opened via ShowDialog?
A. It closes the window and makes value available as the return value of the ShowDialog await expression on the caller side (correct) || Close(bool?) sets the DialogResult and closes the window. The caller's await dialog.ShowDialog(parent) resolves to that value. If the user closes the window via the title-bar X button, the result is null.
B. It serializes value to the window's Title property || Title is a separate dependency property. Close(value) does not modify any window properties; it terminates the window lifecycle.
C. It hides the window and stores value in a static cache for later retrieval || Close() removes the window from the screen and disposes its resources. There is no static cache mechanism.
D. It minimises the window and queues the value for the next ShowDialog call || Close() always terminates the window. A closed window cannot be shown again; a new instance must be created.
Explanation: When a modal dialog calls Close(value), the value is returned to the code that awaited ShowDialog. The return type is Task<bool?> — the bool? is null if dismissed via the system close button, or the value passed to Close (typically true for confirm, false for cancel). After Close(), the window is disposed.
```

```quiz
Q: Which API replaced the deprecated OpenFileDialog class in Avalonia 12, and how is it accessed?
A. TopLevel.StorageProvider.OpenFilePickerAsync() — the StorageProvider is a property on TopLevel, accessed via TopLevel.GetTopLevel(visual) (correct) || The old OpenFileDialog class was removed. The replacement is the IStorageProvider interface, obtained through TopLevel.StorageProvider. Its OpenFilePickerAsync method accepts FilePickerOpenOptions and returns an IReadOnlyList<IStorageFile>.
B. Application.Current.OpenFileDialogAsync() || Application.Current does not expose file-picker methods. The storage provider is scoped to a TopLevel, not the application singleton.
C. FilePicker.ShowAsync() from Avalonia.Controls || There is no static FilePicker class in Avalonia. The API is on IStorageProvider, which is an instance obtained from a TopLevel.
D. Window.OpenFilePicker() as a direct method on Window || Window itself does not have an OpenFilePicker method. The storage service is accessed through TopLevel, the base class for Window and other roots.
Explanation: Avalonia 12 replaces the WPF-style OpenFileDialog/ SaveFileDialog with a cross-platform IStorageProvider API. Call TopLevel.GetTopLevel(visual).StorageProvider.OpenFilePickerAsync(options) to show the native file picker. The result is a list of IStorageFile objects that provide stream access.
```
