---
tier: advanced
topic: platform
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 087 — Platform-Specific: macOS & Linux — Examples

**Prerequisites:** [087-core](087-platform-specific-macos-linux.md)

---

## Example 1: macOS — Complete application menu with About and Preferences

**App.axaml:**
```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MyApp.App"
             Name="MyApp">
  <Application.NativeMenu.Menu>
    <NativeMenu>
      <NativeMenuItem Header="About MyApp..." Click="About_OnClick" />
      <NativeMenuItem Header="Preferences..." Click="Preferences_OnClick"
                      Gesture="Meta+Comma" />
    </NativeMenu>
  </Application.NativeMenu.Menu>
</Application>
```

**App.axaml.cs:**
```csharp
public partial class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime sv)
            sv.MainView = new MainView();
        else if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }

    private void About_OnClick(object? sender, EventArgs e)
    {
        var about = new AboutWindow();
        about.ShowDialog(CurrentWindow());
    }

    private void Preferences_OnClick(object? sender, EventArgs e)
    {
        var prefs = new PreferencesWindow();
        prefs.ShowDialog(CurrentWindow());
    }

    private static Window CurrentWindow() =>
        ((IClassicDesktopStyleApplicationLifetime)
            Current!.ApplicationLifetime!).MainWindow;
}
```

---

## Example 2: macOS — File and Edit window menus

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="MyApp.Views.EditorWindow"
        Title="Editor" Width="1024" Height="768">
  <Window.NativeMenu.Menu>
    <NativeMenu>
      <NativeMenuItem Header="File">
        <NativeMenu>
          <NativeMenuItem Header="New" Gesture="Meta+N"
                          Command="{Binding NewDocumentCommand}" />
          <NativeMenuItem Header="Open..." Gesture="Meta+O"
                          Command="{Binding OpenDocumentCommand}" />
          <NativeMenuItemSeparator />
          <NativeMenuItem Header="Save" Gesture="Meta+S"
                          Command="{Binding SaveCommand}" />
          <NativeMenuItem Header="Save As..." Gesture="Meta+Shift+S"
                          Command="{Binding SaveAsCommand}" />
          <NativeMenuItemSeparator />
          <NativeMenuItem Header="Close" Gesture="Meta+W"
                          Command="{Binding CloseCommand}" />
        </NativeMenu>
      </NativeMenuItem>
      <NativeMenuItem Header="Edit">
        <NativeMenu>
          <NativeMenuItem Header="Undo" Gesture="Meta+Z"
                          Command="{Binding UndoCommand}" />
          <NativeMenuItem Header="Redo" Gesture="Meta+Shift+Z"
                          Command="{Binding RedoCommand}" />
          <NativeMenuItemSeparator />
          <NativeMenuItem Header="Cut" Gesture="Meta+X"
                          Command="{Binding CutCommand}" />
          <NativeMenuItem Header="Copy" Gesture="Meta+C"
                          Command="{Binding CopyCommand}" />
          <NativeMenuItem Header="Paste" Gesture="Meta+V"
                          Command="{Binding PasteCommand}" />
        </NativeMenu>
      </NativeMenuItem>
      <NativeMenuItem Header="View">
        <NativeMenu>
          <NativeMenuItem Header="Toggle Sidebar" Gesture="Meta+Backslash"
                          Command="{Binding ToggleSidebarCommand}" />
          <NativeMenuItem Header="Zoom In" Gesture="Meta+Plus"
                          Command="{Binding ZoomInCommand}" />
          <NativeMenuItem Header="Zoom Out" Gesture="Meta+Minus"
                          Command="{Binding ZoomOutCommand}" />
        </NativeMenu>
      </NativeMenuItem>
    </NativeMenu>
  </Window.NativeMenu.Menu>

  <DockPanel>
    <ContentControl Content="{Binding EditorContent}" />
  </DockPanel>
</Window>
```

---

## Example 3: macOS — Dock menu with runtime modification

```xml
<Application xmlns="https://github.com/avaloniaui"
             x:Class="MyApp.App">
  <Application.NativeDock.Menu>
    <NativeMenu x:Name="DockMenu">
      <NativeMenuItem Header="New Document" Click="NewDocument_OnClick" />
      <NativeMenuItem Header="Open Recent" Click="OpenRecent_OnClick" />
    </NativeMenu>
  </Application.NativeDock.Menu>
</Application>
```

```csharp
public partial class App : Application
{
    public void AddRecentFile(string path)
    {
        var dockMenu = NativeDock.GetMenu(this);
        if (dockMenu is null) return;

        // Insert recent files below static items
        dockMenu.Items.Insert(dockMenu.Items.Count - 1,
            new NativeMenuItem($"Open: {System.IO.Path.GetFileName(path)}"));
    }
}
```

---

## Example 4: macOS — PlatformHotkeyConfiguration for custom controls

```csharp
public class RichTextBox : TextBox
{
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var hotkeys = this.GetPlatformSettings()?.HotkeyConfiguration;
        if (hotkeys is null) { base.OnKeyDown(e); return; }

        if (hotkeys.Copy.Any(g => g.Matches(e)))
        {
            CopySelectedText();
            e.Handled = true;
        }
        else if (hotkeys.Paste.Any(g => g.Matches(e)))
        {
            PasteText();
            e.Handled = true;
        }
        else if (hotkeys.Cut.Any(g => g.Matches(e)))
        {
            CutSelectedText();
            e.Handled = true;
        }
        else if (hotkeys.SelectAll.Any(g => g.Matches(e)))
        {
            SelectAll();
            e.Handled = true;
        }
        else
        {
            base.OnKeyDown(e);
        }
    }

    private void CopySelectedText() { /* custom copy */ }
    private void PasteText() { /* custom paste */ }
    private void CutSelectedText() { /* custom cut */ }
}
```

---

## Example 5: macOS — URL protocol handler

**Info.plist:**
```xml
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLName</key>
    <string>com.mycompany.myapp</string>
    <key>CFBundleTypeRole</key>
    <string>Viewer</string>
    <key>CFBundleURLSchemes</key>
    <array>
      <string>myapp</string>
    </array>
  </dict>
</array>
```

**Program.cs (handle the URL):**
```csharp
public static void Main(string[] args)
{
    // Check for URL scheme launch
    if (args.Length > 0 && args[0].StartsWith("myapp://"))
    {
        // Parse and handle the URL
        HandleUrl(args[0]);
    }

    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}

private static void HandleUrl(string url)
{
    // e.g., myapp://open/document?id=123
    var uri = new Uri(url);
    // Process the deep link
}
```

---

## Example 6: Linux — Running under WSL 2

```bash
# Install required libraries
sudo apt update
sudo apt install libice6 libsm6 libfontconfig1

# Install .NET SDK (follow https://learn.microsoft.com/dotnet/install/linux)
# Then run your app
dotnet run --project src/MyApp.Desktop
```

---

## Example 7: Linux — Testing accessibility with Accerciser

```bash
# Install tools
sudo apt install accerciser orca speech-dispatcher

# Start Orca screen reader
orca &

# In another terminal, start Accerciser
accerciser &

# Run your app
dotnet run --project src/MyApp.Desktop
```

```csharp
// App.axaml.cs — ensure automation properties are set
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        // Set accessibility properties
        AutomationProperties.SetName(this, "Main application view");
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Control);
    }
}
```

---

## Example 8: macOS — NSWindow handle for P/Invoke

```csharp
[DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
private static extern void NSWindowSetTitleWithRepresentedFilename(
    IntPtr nsWindow, IntPtr nsString);

public static void SetWindowRepresentedFile(Window window, string filePath)
{
    if (!OperatingSystem.IsMacOS()) return;

    var handle = TopLevel.GetTopLevel(window)?.TryGetPlatformHandle();
    if (handle is null || handle.HandleDescriptor != "NSWindow") return;

    var nsStr = ObjectiveCInterop.CreateNsString(filePath);
    NSWindowSetTitleWithRepresentedFilename(handle.Handle, nsStr);
}
```

---

## Key Takeaways

- NativeMenu on Application replaces the default menu; auto-appends Quit (⌘Q)
- Window menus use `NativeMenu.Menu`; "Edit" gets automatic text features from the system
- Dock menu is attached to Application via `NativeDock.Menu` and is macOS-only
- `PlatformHotkeyConfiguration` adapts shortcuts across platforms — use `Matches(e)` in custom controls
- URL protocol handlers use standard `CFBundleURLTypes` in Info.plist
- WSL 2 needs `libice6 libsm6 libfontconfig1` for Avalonia to run
- AT-SPI2 accessibility on Linux works automatically over D-Bus; test with Orca or Accerciser
- NativeControlHost on macOS requires `net10.0-macos` target framework
