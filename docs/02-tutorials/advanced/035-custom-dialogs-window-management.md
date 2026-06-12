---
tier: advanced
topic: windowing
estimated: 35 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 035 -- Custom Dialogs and Advanced Window Management

**What you'll learn:** How to create reusable dialog windows with MVVM, manage multiple windows, prevent close, use custom chrome, and overlay dialogs.

**Prerequisites:** [010 -- Window and Dialog Basics](../basics/010-window-dialog-basics.md), [016 -- Window and Dialog Management](../intermediate/016-window-dialog-management.md)

---

## 1. Create a reusable dialog window with MVVM

Define the view model with a close callback:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;

namespace DemoApp.ViewModels;

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

The dialog window:

```xml
<Window x:Class="DemoApp.Views.ConfirmDialog"
        xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Confirm" Width="400" Height="200"
        WindowStartupLocation="CenterOwner"
        CanResize="False" ShowInTaskbar="False">
  <Grid RowDefinitions="*,Auto" Margin="20">
    <TextBlock Grid.Row="0" Text="{Binding Message}"
               TextWrapping="Wrap" VerticalAlignment="Center" />
    <StackPanel Grid.Row="1" Orientation="Horizontal"
                HorizontalAlignment="Right" Spacing="8">
      <Button Content="Cancel" Command="{Binding CancelCommand}" />
      <Button Content="OK" Command="{Binding ConfirmCommand}" />
    </StackPanel>
  </Grid>
</Window>
```

```csharp
public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }
}
```

## 2. Create a DialogService

```csharp
public interface IDialogService
{
    Task<bool> ConfirmAsync(string message);
}

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

Wire it up in `App.axaml.cs`:

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

## 3. Multi-window tracking

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

Open a non-modal tool window:

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

## 4. Prevent dialog close on unsaved changes

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

## 5. Custom chrome / chromeless window

```xml
<Window WindowDecorations="None"
        ExtendClientAreaToDecorationsHint="True"
        TransparencyLevelHint="AcrylicBlur"
        Background="Transparent">
  <Grid RowDefinitions="32,*">
    <Border Grid.Row="0" Background="#1E1E2E"
            WindowDecorationProperties.ElementRole="TitleBar">
      <TextBlock Text="Custom Chrome" Margin="12,0"
                 VerticalAlignment="Center" Foreground="White" />
    </Border>
    <ContentControl Grid.Row="1" Content="Content area" />
  </Grid>
</Window>
```

`WindowDecorationProperties.ElementRole` values:

| Value | Behavior |
|-------|----------|
| `TitleBar` | Draggable title bar region |
| `MinimizeButton` | Minimize button behavior |
| `MaximizeButton` | Maximize/restore button |
| `CloseButton` | Close button |
| `ResizeN/S/E/W` | Resize grip for edge |
| `ResizeNE/NW/SE/SW` | Resize grip for corner |

## 6. Overlay dialogs (in-window)

For platforms without separate windows (WASM, mobile), use an overlay:

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
    <Border Background="{DynamicResource SurfaceBrush}"
            CornerRadius="12" Padding="32"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            BoxShadow="0 8 24 0 #40000000">
      <StackPanel Spacing="16">
        <TextBlock Text="Overlay Dialog"
                   FontSize="18" FontWeight="Bold" />
        <TextBlock Text="This works on all platforms." />
        <Button Content="Close"
                Command="{Binding HideOverlayCommand}" />
      </StackPanel>
    </Border>
  </Border>
</Grid>
```

## Key takeaways

- Dialog windows need `WindowStartupLocation="CenterOwner"` and `ShowInTaskbar="False"`
- Use an `IDialogService` to keep dialog logic out of ViewModels
- Handle `Window.Closing` with `e.Cancel = true` to prevent close
- `WindowDecorationProperties.ElementRole` enables custom title bars with native drag
- Overlay dialogs work on platforms that cannot open separate windows
- Use `TopLevel.GetTopLevel(control) as Window` to find the parent from any view
