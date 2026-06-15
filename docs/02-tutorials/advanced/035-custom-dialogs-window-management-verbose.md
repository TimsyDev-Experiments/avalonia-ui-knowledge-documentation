---
tier: advanced
topic: windowing
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 035-custom-dialogs-window-management.md
---

# 035V — Custom Dialogs and Advanced Window Management: An In-Depth Companion

**Why this exists.** The original tutorial walks through creating dialog windows, a dialog service, multi-window management, close prevention, custom chrome, and overlay dialogs. This companion explains the window ownership model, why `ShowDialog<TResult>` returns a `Task`, how `WindowClosingEventArgs.Cancel` truly works, what `ExtendClientAreaToDecorationsHint` does at the OS level, and why the overlay pattern exists.

**Read this alongside:** [035 — Custom Dialogs and Advanced Window Management](035-custom-dialogs-window-management.md)

---

## 1. Window ownership model — `ShowDialog` vs `Show`

Avalonia windows operate in two modes:

- **Modal** (`ShowDialog<TResult>`): blocks input to the owner window until the dialog closes. The return value is the `TResult` passed to `Window.Close(TResult)`. Modal dialogs require an owner window — passing `null` throws `ArgumentNullException`.
- **Non-modal** (`Show`): opens independently. The window can be brought to front, minimized, and interacted with alongside the owner.

```csharp
// Modal — returns Task<bool?> that completes when the dialog closes
var result = await dialog.ShowDialog<bool?>(_mainWindow);

// Non-modal — opens without blocking
tool.Show(_mainWindow);
```

The `Task<TResult>` returned by `ShowDialog` completes when `Window.Close(TResult)` is called inside the dialog. If the user closes via the OS title-bar close button (which calls `Close()` without arguments), the result is `default(TResult)` — for `bool?` that is `null`.

```csharp
public async Task<bool> ConfirmAsync(string message)
{
    var dialog = new ConfirmDialog();
    var vm = new ConfirmDialogViewModel(dialog, message);
    dialog.DataContext = vm;
    var result = await dialog.ShowDialog<bool?>(_mainWindow);
    return result == true;  // Handles both false and null (OS close)
}
```

Always treat `null` (OS close) as cancellation, equivalent to `false`.

**Common mistake:** omitting the owner parameter or passing a window that is not yet shown. The dialog cannot be modal without a visible owner.

---

## 2. Why the ViewModel receives a `Window` reference

```csharp
public partial class ConfirmDialogViewModel : ObservableObject
{
    private readonly Window _dialog;

    [ObservableProperty]
    private string _message = string.Empty;

    public ConfirmDialogViewModel(Window dialog, string message)
    {
        _dialog = dialog;
        Message = message;
    }

    [RelayCommand]
    private void Confirm() => _dialog.Close(true);

    [RelayCommand]
    private void Cancel() => _dialog.Close(false);
}
```

The ViewModel holds a reference to the `Window` purely for calling `Close(TResult)`. This is a pragmatic trade-off: it couples the ViewModel to the view layer, but keeps the code in one place rather than requiring code-behind event handlers.

Alternatives:

- **Code-behind button handlers** — move `Close()` calls to the window's code-behind. This keeps ViewModels clean of view references but requires wiring per dialog.
- **`Interaction.RequestClose` attached property** — bind a close request to a property on the ViewModel. More complex but fully decoupled.
- **Dialog result via messenger** — send a close message that the window subscribes to.

The "ViewModel holds Window" pattern is the simplest. Accept it for small-to-medium apps; switch to `IInteraction` or messenger patterns for larger codebases.

**Common mistake:** storing the `Window` reference across operations after the dialog is closed. Closed windows are disposed.

---

## 3. XAML with compiled bindings

The original tutorial uses reflection bindings. With `x:DataType` and compiled bindings:

```xml
<Window x:Class="DemoApp.Views.ConfirmDialog"
        xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:viewModels="using:DemoApp.ViewModels"
        x:DataType="viewModels:ConfirmDialogViewModel"
        Title="Confirm" Width="400" Height="200"
        WindowStartupLocation="CenterOwner"
        CanResize="False" ShowInTaskbar="False">
  <Grid RowDefinitions="*,Auto" Margin="20">
    <TextBlock Grid.Row="0"
               Text="{Binding Message}"
               TextWrapping="Wrap" VerticalAlignment="Center" />
    <StackPanel Grid.Row="1" Orientation="Horizontal"
                HorizontalAlignment="Right" Spacing="8">
      <Button Content="Cancel"
              Command="{Binding CancelCommand}" />
      <Button Content="OK"
              Command="{Binding ConfirmCommand}" />
    </StackPanel>
  </Grid>
</Window>
```

`x:DataType` at the `Window` level enables compiled bindings for all child elements. If `Message` is renamed or removed, the XAML compiler emits a compile-time error instead of a silent runtime failure. Compiled bindings are required for NativeAOT (see [039 — NativeAOT and Trimming](039-nativeaot-trimming.md)).

---

## 4. `IDialogService` — why abstract the dialog creation

```csharp
public interface IDialogService
{
    Task<bool> ConfirmAsync(string message);
}
```

The dialog service exists for two reasons:

1. **Testability:** ViewModels that call `ConfirmAsync` can be tested by injecting a mock `IDialogService` that returns `true` or `false` without creating a real window.
2. **Decoupling:** the ViewModel never references `ConfirmDialog`, `Window`, or Avalonia.Controls. It only knows `Task<bool> ConfirmAsync(string)`.

```csharp
public class DialogService : IDialogService
{
    private Window? _mainWindow;

    public void Initialize(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public async Task<bool> ConfirmAsync(string message)
    {
        var dialog = new ConfirmDialog();
        var vm = new ConfirmDialogViewModel(dialog, message);
        dialog.DataContext = vm;
        var result = await dialog.ShowDialog<bool?>(_mainWindow);
        return result == true;
    }
}
```

`Initialize` is called during app startup (in `App.axaml.cs`) to set the owner window. This avoids passing the owner through every `ConfirmAsync` call.

**Common mistake:** calling `ShowDialog` before the owner window is shown. The owner must be visible for a modal dialog to display correctly. The original tutorial wires `Initialize` after creating `MainWindow` and before assigning it to `desktop.MainWindow`:

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var mainWindow = new MainWindow();
        var dialogService = Program.AppHost.Services
            .GetRequiredService<IDialogService>();
        dialogService.Initialize(mainWindow);
        desktop.MainWindow = mainWindow;
    }
    base.OnFrameworkInitializationCompleted();
}
```

However, `MainWindow` has not been shown yet at this point. `ShowDialog` when the owner has not been shown is safe — `ShowDialog` calls `Show` on the owner if needed — but the UX is jarring. Consider showing the main window first, or show the dialog only after `MainWindow` is loaded.

---

## 5. Multi-window tracking — why you need a `WindowManager`

```csharp
public static class WindowManager
{
    private static readonly List<Window> _openWindows = new();

    public static IReadOnlyList<Window> OpenWindows => _openWindows;

    public static void Register(Window window)
    {
        _openWindows.Add(window);
        window.Closed += (_, _) => _openWindows.Remove(window);
    }

    public static void CloseAll()
    {
        foreach (var window in _openWindows.ToList())
            window.Close();
    }
}
```

Avalonia does not provide a built-in collection of all open windows. The `Application.Windows` property exists in some frameworks but is not part of Avalonia's public API. The `WindowManager` singleton solves:

- **Global close-all:** tray apps need to close all windows on shutdown.
- **Track window count:** for `ShutdownMode.OnLastWindowClose`.
- **Broadcast to all windows:** apply theme changes, culture changes, etc.

`.ToList()` in `CloseAll` is critical — `window.Close()` triggers `Closed` which calls `_openWindows.Remove(window)`, modifying the collection during enumeration. `.ToList()` creates a snapshot before iteration.

**Common mistake:** not calling `Register` after creating a tool window. The window operates fine but is invisible to the manager.

---

## 6. Non-modal tool window with owner

```csharp
[RelayCommand]
private void OpenToolWindow()
{
    var tool = new ToolWindow
    {
        DataContext = new ToolViewModel()
    };
    WindowManager.Register(tool);
    tool.Show(_mainWindow); // Stays on top of owner
}
```

`Window.Show(owner)` establishes an owner-owned relationship:

- The tool window stays above the owner (Z-order).
- When the owner is minimized, the tool window is hidden.
- When the owner is restored, the tool window is restored.
- The tool window can still be moved independently.

Omitting the owner parameter (`tool.Show()`) creates an independent top-level window. Users can Alt+Tab to it independently, and it can be behind the main window.

---

## 7. Preventing close — how `WindowClosingEventArgs.Cancel` works

```csharp
protected override void OnClosing(WindowClosingEventArgs e)
{
    if (HasUnsavedChanges)
    {
        e.Cancel = true;
        _ = ShowSavePromptAsync();
    }
    base.OnClosing(e);
}
```

`WindowClosingEventArgs.Cancel = true` tells the OS windowing system to abort the close. On Windows, this sends `WM_CLOSE` with the `MAINWIN_CLOSE` return value indicating "do not close". On macOS, it prevents the `windowShouldClose:` delegate from proceeding.

`e.Cancel` only works on desktop platforms. On browser (WASM), there is no reliable window-close interception — the browser tab close event (`beforeunload`) has limited support. On mobile, the OS controls the activity lifecycle and `OnClosing` may not fire.

Setting `e.Cancel = true` does not automatically dispose the window. The window remains in its current state. The prompt is launched with `_ = ShowSavePromptAsync()` (fire-and-forget) because `OnClosing` is synchronous — you cannot `await` inside it.

**For async close prevention:** set `e.Cancel = true` immediately, show the async prompt, and call `Close()` again if the user confirms. The `Closed` event fires only when close is not cancelled.

**Common mistake:** blocking the UI thread inside `OnClosing`. The OS expects a fast response — blocking freezes the close animation.

---

## 8. Custom chrome — what `ExtendClientAreaToDecorationsHint` does

```xml
<Window WindowDecorations="None"
        ExtendClientAreaToDecorationsHint="True"
        TransparencyLevelHint="AcrylicBlur"
        Background="Transparent">
```

These properties work together to replace the native title bar with custom content:

- **`WindowDecorations="None"`:** hides the native title bar, system menu, minimize/maximize/close buttons. The window still has a resize border unless you set `CanResize="False"` and remove the border with `WindowState="Normal"` and platform-specific hacks.
- **`ExtendClientAreaToDecorationsHint="True"`:** tells the OS to extend the client area into the non-client (title bar) region. On Windows, this calls `DwmExtendFrameIntoClientArea`. On macOS, it extends into the title bar area.
- **`TransparencyLevelHint="AcrylicBlur"`:** requests the platform compositor to render background acrylic blur behind the window. Windows uses `SetWindowCompositionAttribute` with `AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND`. macOS uses `NSVisualEffectView`.

```xml
<Border Grid.Row="0" Background="#1E1E2E"
        WindowDecorationProperties.ElementRole="TitleBar">
```

`WindowDecorationProperties.ElementRole` is an attached property on `Border` (and other controls) that tells the OS which hit-test role the region serves. The values map to Win32 `WM_NCHITTEST` return values:

| Value | `WM_NCHITTEST` constant | Behavior |
|-------|------------------------|----------|
| `TitleBar` | `HTCAPTION` | Draggable — sends `WM_NCLBUTTONDOWN` + `HTCAPTION` |
| `MinimizeButton` | `HTMINBUTTON` | Triggers minimize |
| `MaximizeButton` | `HTMAXBUTTON` | Triggers maximize/restore |
| `CloseButton` | `HTCLOSE` | Triggers close |
| `ResizeN/S/E/W` | `HTTOP`/`HTBOTTOM`/`HTLEFT`/`HTRIGHT` | Resize cursor, drag-to-resize |
| `ResizeNE/NW/SE/SW` | `HTTOPLEFT`/... | Corner resize |

Without `ElementRole`, custom chrome windows cannot be moved or resized by the user — only by code. The attached property hooks into the OS hit-test message and returns the correct region code.

**Common mistake:** setting `WindowDecorations="None"` without `ExtendClientAreaToDecorationsHint="True"`. The client area does not extend upward, and the custom title bar starts below the hidden native title bar space, leaving a dead zone at the top.

---

## 9. Overlay dialogs — why they exist

```xml
<Grid>
  <!-- Main content -->
  <StackPanel Margin="20">
    <Button Content="Show Overlay"
            Command="{Binding ShowOverlayCommand}" />
  </StackPanel>

  <!-- Overlay -->
  <Border Background="#80000000"
          IsVisible="{Binding IsOverlayVisible}">
    ...
  </Border>
</Grid>
```

Overlay dialogs exist because not all platforms support multiple windows:

- **Browser (WASM):** single-view only. `Window.ShowDialog` is unavailable.
- **Android/iOS:** single-activity/single-window model. `ShowDialog` throws `PlatformNotSupportedException`.

The overlay is a `Border` at the same Grid level as the main content, layered on top via Z-order (same Grid cell, declared after the main content = rendered on top).

The semi-transparent `Background="#80000000"` creates the modal backdrop effect. The inner `Border` provides a card surface with rounded corners and a drop shadow (`BoxShadow`).

**Key differences from a real window:**

| Feature | Real window | Overlay |
|---------|-------------|---------|
| Position | OS-managed, draggable | Fixed to view layout |
| Z-order | Cross-app | Within-app only |
| Focus trap | OS-enforced | Must implement manually |
| Resize | OS-managed | Placed inside a layout |
| Show/Hide | `ShowDialog`/`Close` | `IsVisible` binding |

---

## Key differences from the original

| Concept | Original says | Why it matters |
|---------|---------------|----------------|
| XAML bindings | Reflection | No `x:DataType` — breaks under NativeAOT |
| Owner window | Passed to `ShowDialog` | Must be visible or `ShowDialog` shows it first |
| Close prevention | `e.Cancel = true` | Desktop-only; no effect on browser/mobile |
| `ElementRole` | Table of values | Each maps to a Win32 `WM_NCHITTEST` constant |
| Overlay | Shown as pattern | Only reliable cross-platform dialog pattern |

---

## See Also

- [035 — Custom Dialogs and Advanced Window Management](035-custom-dialogs-window-management.md) — the original tutorial
- [035E — Custom Dialogs and Window Management (examples)](035-custom-dialogs-window-management-examples.md)
- [016 — Window and Dialog Management](../intermediate/016-window-dialog-management.md) — basics of ShowDialog and Show
- [037 — App Lifetimes and Splash Screen](037-app-lifetimes-splash-screen.md) — lifetime-aware window management
- [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md) — overlay dialogs on single-view platforms
- [039 — NativeAOT and Trimming](039-nativeaot-trimming.md) — compiled binding requirements
- [Avalonia Docs: Windows](https://docs.avaloniaui.net/docs/concepts/windows)
