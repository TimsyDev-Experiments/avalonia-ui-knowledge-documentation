---
tier: advanced
topic: build
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 042-multi-targeting-desktop-browser-mobile.md
---

# 042E — Multi-Targeting: Desktop, Browser, and Mobile: Real-World Examples

**What this is:** Two complete scenarios that apply the multi-targeting project structure, lifetime dispatch, platform-specific code, and adaptive UI patterns from the tutorial to concrete cross-platform applications.

**Prerequisites:** [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md), [042V — Verbose Companion](042-multi-targeting-desktop-browser-mobile-verbose.md)

---

## Example 1: Adaptive Navigation Shell (Desktop Tabs vs Mobile Bottom Bar)

### Goal

Create a shared navigation shell that renders as a `TabControl` on desktop (with tab headers at the top) and as a bottom navigation bar on mobile (icon + label buttons, single content area below). Both use the same ViewModel and navigation logic. On browser, use the mobile layout.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DemoApp.ViewModels;

public partial class NavSection : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private object? _content;
}

public partial class AdaptiveShellViewModel : ObservableObject
{
    [ObservableProperty]
    private NavSection? _activeSection;

    [ObservableProperty]
    private bool _isDesktopLayout;

    public ObservableCollection<NavSection> Sections { get; } = new();

    public AdaptiveShellViewModel()
    {
        IsDesktopLayout = !OperatingSystem.IsBrowser()
                          && !OperatingSystem.IsAndroid()
                          && !OperatingSystem.IsIOS();

        Sections.Add(new NavSection
        {
            Title = "Dashboard",
            Icon = "📊",
            Content = new DashboardViewModel()
        });
        Sections.Add(new NavSection
        {
            Title = "Settings",
            Icon = "⚙️",
            Content = new SettingsViewModel()
        });
        Sections.Add(new NavSection
        {
            Title = "Profile",
            Icon = "👤",
            Content = new ProfileViewModel()
        });

        ActiveSection = Sections.FirstOrDefault();
        if (ActiveSection is not null)
            ActiveSection.IsSelected = true;
    }

    [RelayCommand]
    private void NavigateTo(NavSection? section)
    {
        if (section is null) return;

        if (ActiveSection is not null)
            ActiveSection.IsSelected = false;

        section.IsSelected = true;
        ActiveSection = section;
    }
}
```

### Desktop Layout (TabControl)

```xml
<!-- ShellView.axaml — used on all platforms, styles adapt -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DemoApp.ViewModels"
             x:Class="DemoApp.Views.ShellView"
             x:DataType="vm:AdaptiveShellViewModel">
  <!-- Desktop layout: TabControl with headers -->
  <TabControl ItemsSource="{Binding Sections}"
              SelectedItem="{Binding ActiveSection}"
              IsVisible="{Binding IsDesktopLayout}">
    <TabControl.ItemTemplate>
      <DataTemplate x:DataType="vm:NavSection">
        <TextBlock Text="{Binding Title}" />
      </DataTemplate>
    </TabControl.ItemTemplate>
    <TabControl.ContentTemplate>
      <DataTemplate x:DataType="vm:NavSection">
        <ContentControl Content="{Binding Content}" />
      </DataTemplate>
    </TabControl.ContentTemplate>
  </TabControl>

  <!-- Mobile layout: bottom nav + content -->
  <Grid RowDefinitions="*,Auto" Margin="0"
        IsVisible="{Binding IsDesktopLayout, Converter={StaticResource InvertBool}}">
    <!-- Content area -->
    <Border Grid.Row="0" Padding="8">
      <ContentControl Content="{Binding ActiveSection.Content}" />
    </Border>

    <!-- Bottom navigation bar -->
    <Border Grid.Row="1"
            Background="{DynamicResource SurfaceBrush}"
            BoxShadow="0 -2 8 0 #30000000">
      <ItemsControl ItemsSource="{Binding Sections}"
                    x:DataType="vm:AdaptiveShellViewModel">
        <ItemsControl.ItemsPanel>
          <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal"
                        HorizontalAlignment="Center" Spacing="0" />
          </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
          <DataTemplate x:DataType="vm:NavSection">
            <Button Command="{Binding $parent.DataContext.NavigateToCommand}"
                    CommandParameter="{Binding .}"
                    Background="Transparent"
                    Width="72" Height="56" Padding="0">
              <StackPanel Spacing="2" VerticalAlignment="Center">
                <TextBlock Text="{Binding Icon}" FontSize="20"
                           HorizontalAlignment="Center" />
                <TextBlock Text="{Binding Title}" FontSize="10"
                           HorizontalAlignment="Center" />
              </StackPanel>
            </Button>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </Border>
  </Grid>
</UserControl>
```

### Platform-Specific Styles (XAML Selector)

```xml
<!-- App.axaml -->
<Application.Styles>
  <Style Selector="TabControl:platform(windows)">
    <Setter Property="FontSize" Value="13" />
  </Style>
  <Style Selector="TabControl:platform(macos)">
    <Setter Property="FontSize" Value="12" />
  </Style>
  <Style Selector="Button:platform(browser)">
    <Setter Property="Cursor" Value="Pointer" />
  </Style>
</Application.Styles>
```

### Shared App.axaml.cs

```csharp
public override void OnFrameworkInitializationCompleted()
{
    var shellVm = new AdaptiveShellViewModel();

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow = new MainWindow
        {
            DataContext = shellVm
        };
        desktop.MainWindow.Show();
    }
    else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
    {
        singleView.MainView = new ShellView
        {
            DataContext = shellVm
        };
    }

    base.OnFrameworkInitializationCompleted();
}
```

### How It Works

1. **Layout switching via `IsDesktopLayout`** — The ViewModel detects the platform using `OperatingSystem.IsBrowser()` / `IsAndroid()` / `IsIOS()`. The XAML contains both layouts, each gated by `IsVisible`. Only one layout is visible at a time — the other has `IsVisible = false` and does not participate in layout.

2. **Shared navigation state** — Both layouts bind to the same `Sections` collection and `ActiveSection`. Clicking a tab (desktop) or a bottom nav button (mobile) calls `NavigateToCommand` with the same `NavSection`. The content area shows `ActiveSection.Content`.

3. **`NavigateToCommand` with CommandParameter** — The mobile layout uses `Command="{Binding $parent.DataContext.NavigateToCommand}"` with `CommandParameter="{Binding .}"`. The `$parent.DataContext` walks up to the `AdaptiveShellViewModel` level, because the button's own `DataContext` is a `NavSection`.

4. **Platform selector styles** — The `:platform()` pseudo-class selector applies platform-specific tweaks without `#if` directives. `TabControl:platform(windows)` sets `FontSize` to 13 on Windows, 12 on macOS. These selectors are evaluated at runtime.

5. **Separate content ViewModels** — `DashboardViewModel`, `SettingsViewModel`, and `ProfileViewModel` are each instantiated once in the constructor. They live for the lifetime of the shell. This is fine for a few sections; for many sections, lazy-load via `ContentTemplate` selector.

### Design Decisions and Trade-offs

- **Both layouts compiled into binary** — The XAML for both layouts is included in the shared library. Binary size increases slightly, but the code is trivially proven correct (no runtime layout selection that might fail).
- **`OperatingSystem` APIs over `#if`** — The detection is runtime, not compile-time. This lets a single binary decide the layout. The NativeAOT compiler evaluates the conditions statically and eliminates dead branches, so the desktop layout code does not increase the WASM binary size.
- **No virtualization of sections** — The `TabControl` creates one content presenter per tab. For 3-5 sections this is fine. For 50+ sections, lazy-load with `ContentTemplate` or virtualizing `TabStrip`.

---

## Example 2: Platform-Specific File Export (Desktop Save Dialog vs Mobile Share Sheet)

### Goal

Export a generated document to a file. On desktop, show the OS save-file dialog. On Android and iOS, use the platform share sheet (via `Clipboard` + share intent). On browser, download the file via a Blob URL. All paths share the same document generation logic.

### ViewModel

```csharp
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DemoApp.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private readonly IStorageProvider _storage;
    private readonly ITopLevelService _topLevel;

    public ExportViewModel(IStorageProvider storage, ITopLevelService topLevel)
    {
        _storage = storage;
        _topLevel = topLevel;
    }

    [ObservableProperty]
    private string _documentContent = "Sample report content.\nGenerated on: " + DateTime.Now.ToString("g");

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private string? _statusMessage;

    [RelayCommand]
    private async Task ExportAsync()
    {
        IsExporting = true;
        StatusMessage = null;

        try
        {
            if (OperatingSystem.IsBrowser())
            {
                await ExportBrowserAsync();
            }
            else if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
            {
                await ExportMobileAsync();
            }
            else
            {
                await ExportDesktopAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private async Task ExportDesktopAsync()
    {
        var file = await _storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Document",
            SuggestedFileName = "report.txt",
            DefaultExtension = "txt",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Text Files")
                {
                    Patterns = new[] { "*.txt" }
                }
            }
        });

        if (file is null)
        {
            StatusMessage = "Export cancelled.";
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(DocumentContent);

        StatusMessage = $"Saved to {file.Name}";
    }

    private async Task ExportBrowserAsync()
    {
        // Browser: use the download manager via a data URI
        var base64 = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(DocumentContent));

        var script = $"const a = document.createElement('a'); " +
                     $"a.href = 'data:text/plain;base64,{base64}'; " +
                     $"a.download = 'report.txt'; " +
                     $"a.click();";

        // Execute JavaScript via WebView or Avalonia.Web interop
        // Avalonia.Web exposes a JS interop mechanism
        if (_topLevel is IWebTopLevelService web)
        {
            await web.ExecuteScriptAsync(script);
            StatusMessage = "Download started.";
        }
    }

    private async Task ExportMobileAsync()
    {
        // Mobile: copy to clipboard and hint at sharing
        var clipboard = _topLevel.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(DocumentContent);
            StatusMessage = "Content copied to clipboard. Use your OS share sheet to save.";
        }
        else
        {
            StatusMessage = "Clipboard unavailable.";
        }
    }
}

public interface ITopLevelService
{
    IClipboard? Clipboard { get; }
}

public interface IWebTopLevelService : ITopLevelService
{
    Task ExecuteScriptAsync(string script);
}
```

### Desktop Export in View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DemoApp.ViewModels"
             x:Class="DemoApp.Views.ExportView"
             x:DataType="vm:ExportViewModel">
  <Grid RowDefinitions="Auto,*,Auto" Margin="16" Spacing="12">
    <TextBlock Grid.Row="0" Text="Export Document"
               FontSize="18" FontWeight="Bold" />

    <Border Grid.Row="1"
            Background="{DynamicResource CardBrush}"
            CornerRadius="8" Padding="12">
      <ScrollViewer>
        <TextBlock Text="{Binding DocumentContent}"
                   FontFamily="Consolas" TextWrapping="Wrap" />
      </ScrollViewer>
    </Border>

    <Grid Grid.Row="2" ColumnDefinitions="*,Auto" Spacing="8">
      <TextBlock Grid.Column="0"
                 Text="{Binding StatusMessage}"
                 Foreground="{DynamicResource SystemAccentColor}"
                 VerticalAlignment="Center"
                 TextTrimming="CharacterEllipsis" />
      <Button Grid.Column="1"
              Content="Export"
              Command="{Binding ExportCommand}"
              IsEnabled="{Binding IsExporting, Converter={StaticResource InvertBool}}" />
    </Grid>
  </Grid>
</UserControl>
```

### Platform-Specific Export Handling via Conditional Compilation

```csharp
#if BROWSER
// src/MyApp.Browser/Services/BrowserTopLevelService.cs
public class BrowserTopLevelService : IWebTopLevelService
{
    public IClipboard? Clipboard => null; // Browser clipboard requires user gesture

    public async Task ExecuteScriptAsync(string script)
    {
        // Use Avalonia.Web JS interop
        await JSHost.ImportAsync("export", "main.js");
        // Execute the script
    }
}
#endif

#if ANDROID || IOS
// src/MyApp.Mobile/Services/MobileTopLevelService.cs
public class MobileTopLevelService : ITopLevelService
{
    private readonly TopLevel _topLevel;

    public MobileTopLevelService(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    public IClipboard? Clipboard => _topLevel.Clipboard;
}
#endif

#if !BROWSER && !ANDROID && !IOS
// src/MyApp.Desktop/Services/DesktopTopLevelService.cs
public class DesktopTopLevelService : ITopLevelService
{
    private readonly TopLevel _topLevel;

    public DesktopTopLevelService(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    public IClipboard? Clipboard => _topLevel.Clipboard;
}
#endif
```

### Dependency Injection Registration

```csharp
// In each platform-specific Program.cs, register the platform service:

// Desktop Program.cs
services.AddSingleton<ITopLevelService>(sp =>
{
    var topLevel = TopLevel.GetTopLevel(desktop.MainWindow!);
    return new DesktopTopLevelService(topLevel!);
});

// Browser Program.cs
services.AddSingleton<IWebTopLevelService>(new BrowserTopLevelService());
services.AddSingleton<ITopLevelService>(sp => sp.GetRequiredService<IWebTopLevelService>());
```

### How It Works

1. **Platform branching in ViewModel** — `ExportAsync` uses `OperatingSystem.IsBrowser()`, `IsAndroid()`, `IsIOS()` to select the export strategy. Each path is a separate method. The compiler eliminates unreachable branches in NativeAOT builds.

2. **Desktop path** — Uses `IStorageProvider.SaveFilePickerAsync` with a text file filter. The user picks a location. `OpenWriteAsync` writes the content. This is the most traditional flow.

3. **Browser path** — Constructs a data URI (`data:text/plain;base64,...`) and injects a script that triggers the browser's download manager via a dynamically-created `<a>` element. This avoids needing the File System Access API (Chromium-only).

4. **Mobile path** — Copies content to clipboard and shows instructions. A real mobile app would use the platform share intent (`Intent.ActionSend` on Android, `UIActivityViewController` on iOS). Avalonia does not expose these directly — use a platform-specific launcher or the `Avalonia.Android` interop APIs.

5. **`#if` for service registration** — The interface implementations live in separate platform-specific projects. `#if` directives in the shared project would also work, but separating the files keeps the shared code clean.

### Design Decisions and Trade-offs

- **Three distinct UX paths** — Desktop gets a file dialog (user expects it), browser gets a download (user expects it), mobile gets clipboard (acceptable but not great). The ideal mobile path uses platform share intents, which requires platform-specific code in the Android/iOS launcher projects.
- **`ITopLevelService` abstraction** — The ViewModel needs `IStorageProvider` and `IClipboard`, which it already has via `TopLevel`. But `TopLevel` requires a visual tree reference. The service abstracts the acquisition away from the ViewModel. On desktop, `TopLevel` is available from the main window. On browser, `TopLevel` is the single view root.
- **`OperatingSystem.IsBrowser()` at runtime vs compile-time** — Using `OperatingSystem` APIs allows a single build output to handle all platforms if needed (for debugging). The trade-off is that the condition branches exist in the IL for all targets, though NativeAOT trims them.

---

## Comparison: What the Two Examples Demonstrate

| Aspect | Example 1 — Adaptive Shell | Example 2 — Platform Export |
|--------|----------------------------|-----------------------------|
| Platform detection | `OperatingSystem.IsBrowser/Android/iOS` | `OperatingSystem.IsBrowser/Android/iOS` |
| Layout adaptation | Two XAML layouts gated by `IsVisible` | Single VM command, three methods |
| Desktop UI | `TabControl` with headers | `SaveFilePickerAsync` dialog |
| Mobile UI | Bottom navigation bar | Clipboard + share sheet hint |
| Browser UI | Same as mobile layout | Data URI + JS download |
| Shared assembly | All ViewModels + all XAML | All ViewModels + all XAML |
| Platform-specific files | None (all in shared) | `ITopLevelService` implementations per project |
| `#if` usage | None | Service registrations in launcher projects |
| `:platform()` selector | Yes (`TabControl:platform(windows)`) | No |
| NativeAOT compatibility | Full (compiled bindings, `OperatingSystem` dead-stripping) | Full |

## See Also

- [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md) — the original tutorial
- [042V — Multi-Targeting: Desktop, Browser, Mobile (verbose companion)](042-multi-targeting-desktop-browser-mobile-verbose.md)
- [037 — App Lifetimes and Splash Screen](037-app-lifetimes-splash-screen.md) — lifetime dispatch in `App.axaml.cs`
- [034 — File Pickers and Platform Services](034-file-pickers-platform-services.md) — `IStorageProvider` for desktop/mobile file access
- [035 — Custom Dialogs and Window Management](035-custom-dialogs-window-management.md) — overlay dialogs for single-view platforms
- [Avalonia Docs: Mobile and Browser](https://docs.avaloniaui.net/docs/platform-specific-guides/mobile-browser)
