---
tier: advanced
topic: windowing
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 035-custom-dialogs-window-management.md
---

# 035E — Custom Dialogs and Window Management: Real-World Examples

**What this is:** Two complete scenarios that apply modal dialogs, non-modal tool windows, close prevention, custom chrome, and overlay dialogs from the tutorial to concrete applications.

**Prerequisites:** [035 — Custom Dialogs and Window Management](035-custom-dialogs-window-management.md), [035V — Verbose Companion](035-custom-dialogs-window-management-verbose.md)

---

## Example 1: Unsaved-Changes Prompt with Overlay Fallback

### Goal

Show a "save changes?" confirmation when the user closes a document editor. On desktop, use a modal `Window` dialog via `IDialogService`. On single-view platforms (browser, mobile), use an in-window overlay instead. Both paths share the same ViewModel logic.

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DemoApp.ViewModels;

public partial class DocumentEditorViewModel : ObservableObject
{
    private readonly IConfirmService _confirm;

    public DocumentEditorViewModel(IConfirmService confirm)
    {
        _confirm = confirm;
    }

    [ObservableProperty]
    private string _documentContent = string.Empty;

    [ObservableProperty]
    private string _documentName = "Untitled";

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private bool _isOverlayVisible;

    [ObservableProperty]
    private string? _pendingAction;

    partial void OnDocumentContentChanged(string value)
    {
        HasUnsavedChanges = true;
    }

    public async Task<bool> TryCloseAsync()
    {
        if (!HasUnsavedChanges)
            return true;

        var result = await _confirm.ShowAsync(
            "Unsaved Changes",
            $"Save changes to \"{DocumentName}\" before closing?");

        if (result == ConfirmResult.Cancel)
            return false;

        if (result == ConfirmResult.Yes)
            await SaveAsync();

        return true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Simulate save
        await Task.Delay(500);
        HasUnsavedChanges = false;
    }

    [RelayCommand]
    private void ShowOverlay()
    {
        PendingAction = "close";
        IsOverlayVisible = true;
    }

    [RelayCommand]
    private void OverlaySave()
    {
        IsOverlayVisible = false;
        _ = SaveAsync();
    }

    [RelayCommand]
    private void OverlayDiscard()
    {
        IsOverlayVisible = false;
        HasUnsavedChanges = false;
    }

    [RelayCommand]
    private void OverlayCancel()
    {
        IsOverlayVisible = false;
    }
}

public enum ConfirmResult { Yes, No, Cancel }

public interface IConfirmService
{
    Task<ConfirmResult> ShowAsync(string title, string message);
}
```

### Desktop Dialog Implementation

```csharp
// src/DesktopApp/Services/ConfirmService.cs
using Avalonia.Controls;
using DemoApp.ViewModels;

namespace DesktopApp.Services;

public class ConfirmService : IConfirmService
{
    private readonly Window _owner;

    public ConfirmService(Window owner)
    {
        _owner = owner;
    }

    public async Task<ConfirmResult> ShowAsync(string title, string message)
    {
        var vm = new ConfirmDialogViewModel(title, message);
        var dialog = new ConfirmDialog
        {
            DataContext = vm
        };
        var result = await dialog.ShowDialog<ConfirmResult?>(_owner);
        return result ?? ConfirmResult.Cancel;
    }
}

// ConfirmDialogViewModel (desktop-specific, holds Window reference)
public partial class ConfirmDialogViewModel : ObservableObject
{
    private readonly Window _dialog;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _message;

    public ConfirmDialogViewModel(string title, string message)
    {
        _title = title;
        _message = message;
        _dialog = null!;
    }

    public void Attach(Window dialog) => _dialog = dialog;

    [RelayCommand]
    private void Yes() => _dialog.Close(ConfirmResult.Yes);

    [RelayCommand]
    private void No() => _dialog.Close(ConfirmResult.No);

    [RelayCommand]
    private void Cancel() => _dialog.Close(ConfirmResult.Cancel);
}
```

### Desktop Dialog View (XAML)

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:DemoApp.ViewModels"
        x:Class="DemoApp.Views.ConfirmDialog"
        x:DataType="vm:ConfirmDialogViewModel"
        Title="{Binding Title}" Width="420" Height="180"
        WindowStartupLocation="CenterOwner"
        CanResize="False" ShowInTaskbar="False">
  <Grid RowDefinitions="*,Auto" Margin="20" Spacing="16">
    <StackPanel Grid.Row="0" VerticalAlignment="Center" Spacing="8">
      <TextBlock Text="{Binding Title}"
                 FontSize="16" FontWeight="Bold" />
      <TextBlock Text="{Binding Message}"
                 TextWrapping="Wrap" />
    </StackPanel>
    <StackPanel Grid.Row="1" Orientation="Horizontal"
                HorizontalAlignment="Right" Spacing="8">
      <Button Content="Save" Command="{Binding YesCommand}" />
      <Button Content="Discard" Command="{Binding NoCommand}" />
      <Button Content="Cancel" Command="{Binding CancelCommand}" />
    </StackPanel>
  </Grid>
</Window>
```

### Overlay for Single-View Platforms

```xml
<!-- Embedded in the editor view, shown when IsOverlayVisible is true -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DemoApp.ViewModels"
             x:Class="DemoApp.Views.DocumentEditorView"
             x:DataType="vm:DocumentEditorViewModel">
  <Grid>
    <!-- Editor content -->
    <Grid RowDefinitions="Auto,*,Auto" Margin="16" Spacing="8">
      <TextBox Grid.Row="0"
               Text="{Binding DocumentName}"
               FontSize="14" FontWeight="Bold" />
      <TextBox Grid.Row="1"
               Text="{Binding DocumentContent}"
               AcceptsReturn="True"
               TextWrapping="Wrap"
               Watermark="Start typing..." />
      <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="8">
        <Button Content="Close"
                Command="{Binding ShowOverlayCommand}" />
      </StackPanel>
    </Grid>

    <!-- Overlay dialog -->
    <Border Background="#80000000"
            IsVisible="{Binding IsOverlayVisible}">
      <Border Background="{DynamicResource SurfaceBrush}"
              CornerRadius="12" Padding="28"
              HorizontalAlignment="Center"
              VerticalAlignment="Center"
              BoxShadow="0 8 32 0 #60000000"
              MinWidth="320">
        <StackPanel Spacing="16">
          <TextBlock Text="Unsaved Changes"
                     FontSize="18" FontWeight="Bold" />
          <TextBlock Text="{Binding DocumentName, StringFormat='Save changes to \"{0}\" before closing?'}"
                     TextWrapping="Wrap" />
          <StackPanel Orientation="Horizontal"
                      HorizontalAlignment="Right" Spacing="8">
            <Button Content="Save"
                    Command="{Binding OverlaySaveCommand}" />
            <Button Content="Discard"
                    Command="{Binding OverlayDiscardCommand}" />
            <Button Content="Cancel"
                    Command="{Binding OverlayCancelCommand}" />
          </StackPanel>
        </StackPanel>
      </Border>
    </Border>
  </Grid>
</UserControl>
```

### How It Works

1. **Shared ViewModel, dual UI** — `DocumentEditorViewModel` knows about `IConfirmService` (abstract) and `IsOverlayVisible` (for single-view). On desktop, `IConfirmService` opens a modal `Window`. On single-view, the ViewModel sets `IsOverlayVisible = true`. Both paths use the same `ConfirmResult` enum.

2. **Close interception** — The `Window.Closing` event calls `await TryCloseAsync()`. If the user cancels, `e.Cancel = true` prevents close. If they confirm, `HasUnsavedChanges` is cleared and the window closes naturally.

3. **Overlay focus trap** — The overlay does not automatically trap focus. The user can still click elements behind the backdrop. In production, add an `EventManager` handler that suppresses pointer events behind the overlay, or wrap the overlay content in a `Panel` that captures all input.

4. **Fire-and-forget problem** — `ShowOverlayCommand` sets `PendingAction = "close"` and shows the overlay. The actual close must wait for the user to respond. The overlay commands (`OverlaySave`, `OverlayDiscard`, `OverlayCancel`) handle the response and then close the window if appropriate.

### Design Decisions and Trade-offs

- **`IConfirmService` vs direct `Window` reference** — The service keeps the ViewModel free of `Window` imports. The trade-off is an extra interface and two implementations (desktop + overlay).
- **Overlay always present** — The overlay `Border` is always in the visual tree but hidden via `IsVisible`. This is simpler than dynamically loading it but means all platforms pay the memory cost.
- **No `Window.Closing` for overlay** — The overlay cannot intercept browser tab close. The `beforeunload` event is the only option on WASM, and it has limited customization.

---

## Example 2: Floating Toolbox with Custom Chrome

### Goal

A non-modal floating toolbox window with custom chrome (no OS title bar, draggable via custom region, resize handles) that stays connected to the main editor window. The toolbox follows its owner when minimized/restored and can be docked via a snap region.

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DemoApp.ViewModels;

public partial class ToolboxViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isPinned = true;

    [ObservableProperty]
    private double _opacity = 0.92;

    [ObservableProperty]
    private bool _showGrid;

    [ObservableProperty]
    private string _selectedTool = "Pointer";

    public List<string> Tools { get; } = new()
    {
        "Pointer", "Move", "Rotate", "Scale",
        "Fill", "Stroke", "Eyedropper", "Eraser"
    };

    [RelayCommand]
    private void SelectTool(string tool)
    {
        SelectedTool = tool;
    }

    [RelayCommand]
    private void TogglePin()
    {
        IsPinned = !IsPinned;
    }
}
```

### Toolbox Window View (XAML)

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:DemoApp.ViewModels"
        x:Class="DemoApp.Views.ToolboxWindow"
        x:DataType="vm:ToolboxViewModel"
        Width="220" Height="400"
        WindowDecorations="None"
        ExtendClientAreaToDecorationsHint="True"
        TransparencyLevelHint="AcrylicBlur"
        Background="Transparent"
        ShowInTaskbar="False"
        Topmost="False"
        CanResize="True">
  <Border CornerRadius="8" BorderThickness="1"
          BorderBrush="#30FFFFFF"
          Background="#F01E1E2E">
    <Grid RowDefinitions="36,*,Auto" Margin="0">
      <!-- Drag region -->
      <Border Grid.Row="0"
              WindowDecorationProperties.ElementRole="TitleBar"
              Background="Transparent" Padding="12,0">
        <Grid ColumnDefinitions="*,Auto,Auto">
          <TextBlock Grid.Column="0"
                     Text="Tools"
                     FontSize="13" FontWeight="SemiBold"
                     VerticalAlignment="Center" />
          <Button Grid.Column="1"
                  WindowDecorationProperties.ElementRole="MinimizeButton"
                  Content="─" FontSize="10" Width="24" Height="24"
                  Background="Transparent" />
          <Button Grid.Column="2"
                  WindowDecorationProperties.ElementRole="CloseButton"
                  Content="✕" FontSize="10" Width="24" Height="24"
                  Background="Transparent" />
        </Grid>
      </Border>

      <!-- Tool buttons -->
      <ScrollViewer Grid.Row="1" Margin="4,0">
        <ItemsControl ItemsSource="{Binding Tools}"
                      x:DataType="vm:ToolboxViewModel">
          <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="x:String">
              <RadioButton Content="{Binding .}"
                           GroupName="ToolGroup"
                           IsChecked="{Binding $parent.DataContext.SelectedTool, Converter={StaticResource StringEqualsConverter}, ConverterParameter={Binding .}}"
                           Margin="4,2" Height="28" />
            </RadioButton>
          </DataTemplate>
        </ItemsControl>
      </ScrollViewer>

      <!-- Pin/opacity footer -->
      <Border Grid.Row="2" Padding="8,4"
              WindowDecorationProperties.ElementRole="ResizeSE">
        <Grid ColumnDefinitions="Auto,*,Auto">
          <ToggleButton Grid.Column="0"
                        IsChecked="{Binding IsPinned}"
                        Content="📌" Width="24" Height="20"
                        Background="Transparent" />
          <Slider Grid.Column="1"
                  Value="{Binding Opacity}"
                  Minimum="0.3" Maximum="1.0"
                  Margin="8,0" />
        </Grid>
      </Border>
    </Grid>
  </Border>
</Window>
```

### Opening the Toolbox from the Main Window

```csharp
public partial class MainViewModel : ObservableObject
{
    private readonly Window _mainWindow;
    private ToolboxWindow? _toolbox;

    public MainViewModel(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    [RelayCommand]
    private void OpenToolbox()
    {
        if (_toolbox is { IsVisible: true })
        {
            _toolbox.Activate();
            return;
        }

        _toolbox = new ToolboxWindow
        {
            DataContext = new ToolboxViewModel()
        };
        WindowManager.Register(_toolbox);
        _toolbox.Show(_mainWindow);
    }
}
```

### How It Works

1. **Custom chrome via `WindowDecorationProperties.ElementRole`** — The title bar `Border` uses `ElementRole="TitleBar"` so the OS can drag the window when the user clicks and holds there. Without this, the window would be undraggable because `WindowDecorations="None"` removes the native title bar.

2. **Custom minimize and close buttons** — The minimize and close `Button` elements use `ElementRole="MinimizeButton"` and `ElementRole="CloseButton"`. This maps to the OS hit-test constants, so clicking these triggers the expected OS window behavior (minimize to taskbar, close).

3. **Resize corner** — The footer `Border` uses `ElementRole="ResizeSE"`. Combined with `CanResize="True"`, the user can drag the southeast corner to resize the toolbox. The southeast corner is chosen because it is the most intuitive resize grip.

4. **Owner relationship** — `toolbox.Show(_mainWindow)` establishes ownership. When the main window is minimized, the toolbox hides. When the main window restores, the toolbox restores. The toolbox always stays above the main window in Z-order.

5. **Re-activation on re-open** — `OpenToolbox` checks if `_toolbox` is already visible and calls `Activate()` instead of creating a second instance. This prevents duplicate tool windows.

### Design Decisions and Trade-offs

- **`AcrylicBlur` on custom chrome** — `TransparencyLevelHint="AcrylicBlur"` creates a modern translucent look. On Windows, this works well. On Linux (X11), acrylic blur is not supported and falls back to transparent. Test on all target platforms.
- **Manual minimize/close buttons vs native** — Custom buttons give full visual control but lose platform-specific behaviors (double-click title bar to maximize, right-click title bar for system menu). Add these behaviors via event handlers if needed.
- **`Pin` vs auto-hide** — The pin toggle is a UX signal but does not actually change window behavior in this example. A real implementation would switch between `Topmost = true` (pinned) and letting the toolbox fall behind (unpinned).
- **`WindowManager.Register` for tracking** — Without registration, the main window could not close all toolboxes on shutdown. The `WindowManager.CloseAll()` is called in `App.OnFrameworkInitializationCompleted` exit logic.

---

## Comparison: What the Two Examples Demonstrate

| Aspect | Example 1 — Unsaved-Changes Prompt | Example 2 — Floating Toolbox |
|--------|--------------------------------------|-------------------------------|
| Dialog type | Modal (`ShowDialog`) | Non-modal (`Show`) |
| ViewModel coupling | `IConfirmService` abstraction | Direct `Window` reference |
| Cross-platform | Desktop dialog + overlay fallback | Desktop-only (custom chrome) |
| Close prevention | `Window.Closing` + `e.Cancel` | Not applicable |
| Custom chrome | No | Yes (`WindowDecorations="None"` + `ElementRole`) |
| Owner relationship | Dialog owned by parent | Toolbox stays above owner |
| Re-activation | N/A (new dialog each time) | `Activate()` on existing instance |
| Platform limitation | Overlay for single-view | Custom chrome not supported on WASM |

## See Also

- [035 — Custom Dialogs and Window Management](035-custom-dialogs-window-management.md) — the original tutorial
- [035V — Custom Dialogs and Window Management (verbose companion)](035-custom-dialogs-window-management-verbose.md)
- [037 — App Lifetimes and Splash Screen](037-app-lifetimes-splash-screen.md) — wiring window management at startup
- [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md) — overlay dialogs on single-view platforms
- [Avalonia Docs: Windows](https://docs.avaloniaui.net/docs/concepts/windows)
