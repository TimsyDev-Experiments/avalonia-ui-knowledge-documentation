---
tier: advanced
topic: platform
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 086 — Platform-Specific: Windows

**What you'll learn:** Windows-specific Avalonia features — Mica/acrylic, custom chrome, dark mode, high DPI, Win32 interop, WinForms hosting, system tray, and troubleshooting.

**Prerequisites:** [037 — App Lifetimes & Splash Screen](037-app-lifetimes-splash-screen.md)

---

## 1. How Avalonia runs on Windows

Avalonia uses **Win32 API** directly — no additional workloads. Rendering is Skia backed by Direct3D, with automatic software fallback.

Cross-compile Windows builds from macOS or Linux — no Windows SDK or Visual Studio required on the build machine.

---

## 2. Window transparency and Mica

| Level | Effect | Min. version |
|---|---|---|
| `Transparent` | Fully transparent background | Windows 7+ |
| `AcrylicBlur` | Blurred, semi-transparent backdrop | Win 10 1803+ |
| `Mica` | System-tinted DWM material | Windows 11 |

```xml
<Window xmlns="https://github.com/avaloniaui"
        TransparencyLevelHint="Mica"
        Background="Transparent">
</Window>
```

- The `Background` must be `Transparent` for any effect to take hold.
- If unavailable (e.g., Mica on Windows 10), it falls back through the list.
- Check at runtime: `window.ActualTransparencyLevel`.
- Set a non-transparent fallback for battery-saver / remote-desktop scenarios.

---

## 3. Custom title bars

```xml
<Window ExtendClientAreaToDecorationsHint="True"
        WindowDecorations="None">
  <Grid RowDefinitions="32,*">
    <Border Grid.Row="0" Background="#2D2D2D"
            WindowDecorationProperties.ElementRole="TitleBar">
      <TextBlock Text="My App" Foreground="White"
                 VerticalAlignment="Center" Margin="12,0" />
    </Border>
    <Border Grid.Row="1">
      <TextBlock Text="Content area" />
    </Border>
  </Grid>
</Window>
```

| Property | Effect |
|---|---|
| `ExtendClientAreaToDecorationsHint="True"` | Push content into title bar |
| `WindowDecorations="None"` | Remove system chrome entirely |
| `WindowDecorations="Full"` | Keep min/max/close buttons visible in your custom bar |
| `WindowDecorationProperties.ElementRole="TitleBar"` | Mark draggable region |

---

## 4. Dark mode detection

```csharp
var settings = myControl.GetPlatformSettings();
if (settings?.GetColorValues() is { ThemeVariant: ThemeVariant.Dark })
{
    // System dark mode
}

if (settings is not null)
{
    settings.ColorValuesChanged += (sender, values) =>
    {
        // React to theme/accent changes in real time
    };
}
```

- `FluentTheme` switches automatically.
- On Windows 11, the native title bar follows `RequestedThemeVariant` automatically.
- On Windows 10, use a custom title bar or P/Invoke `DwmSetWindowAttribute` (undocumented).

---

## 5. High DPI / per-monitor scaling

Avalonia is per-monitor DPI-aware by default. All layout uses device-independent pixels.

```csharp
var screen = myWindow.Screens.ScreenFromWindow(myWindow);
var scaling = screen?.Scaling ?? 1.0;
```

Avalonia automatically picks `@2x` asset variants at 200%+ scaling.

For blurry images, provide multiple resolution variants:

```
/Assets/logo.png        (1x)
/Assets/logo@2x.png     (2x)
```

---

## 6. NativeControlHost — embedding Win32 controls

```csharp
public class Win32Editor : NativeControlHost
{
    protected override IPlatformHandle CreateNativeControlCore(
        IPlatformHandle parent)
    {
        if (OperatingSystem.IsWindows())
        {
            var hwnd = CreateWindowEx(0, "EDIT", "",
                WS_CHILD | WS_VISIBLE | ES_MULTILINE,
                0, 0, 100, 100,
                parent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            return new PlatformHandle(hwnd, "HWND");
        }
        return base.CreateNativeControlCore(parent);
    }
}
```

**Limitations:** No transparency, no transforms, always on top of Avalonia content, clipped to host bounds only.

---

## 7. Retrieving the HWND

```csharp
var handle = TopLevel.GetTopLevel(myControl)?.TryGetPlatformHandle();
// handle.Handle is IntPtr (HWND on Windows)
// handle.HandleDescriptor is "HWND"
```

---

## 8. WinForms interoperability

### Embed Avalonia in a WinForms app

```
<PackageReference Include="Avalonia.Desktop" />
<PackageReference Include="Avalonia.Win32.Interoperability" />
```

```csharp
// Program.cs
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .SetupWithoutStarting();
Application.Run(new MainForm());
```

```csharp
// Form constructor
winFormsAvaloniaControlHost1.Content = new MainView
{
    DataContext = new MainViewModel()
};
```

### Standalone Avalonia windows from WinForms

```csharp
using AvaloniaWindow = Avalonia.Controls.Window;

private void OpenButton_Click(object sender, EventArgs e)
{
    var window = new AvaloniaWindow
    {
        Width = 300, Height = 300,
        Content = new MainView()
    };
    window.Show();
}
```

### Keyboard routing

```csharp
[STAThread]
static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.AddMessageFilter(new WinFormsAvaloniaMessageFilter());

    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .SetupWithoutStarting();

    Application.Run(new MainForm());
}
```

---

## 9. System tray

```xml
<TrayIcon Icon="/Assets/app-icon.ico" ToolTipText="My App">
  <TrayIcon.Menu>
    <NativeMenu>
      <NativeMenuItem Header="Show Window" Click="Show_Click" />
      <NativeMenuItemSeparator />
      <NativeMenuItem Header="Exit" Click="Exit_Click" />
    </NativeMenu>
  </TrayIcon.Menu>
</TrayIcon>
```

- Requires `.ico` format on Windows.
- Minimize to tray: set `ShowInTaskbar="False"` on minimize, restore from click handler.

---

## 10. Platform services (Windows behavior)

| Service | Windows implementation |
|---|---|
| Clipboard | Win32 clipboard (text, HTML, RTF, file lists) |
| File dialogs | `IFileDialog` — filters, initial directory, multi-select |
| Drag & drop | OLE drag-and-drop, Explorer file drops |
| Launcher | Default browser for URIs, associated app for files |

---

## 11. Troubleshooting

| Symptom | Fix |
|---|---|
| Blank/black window | Force software rendering via `Win32RenderingMode.Software`; update GPU drivers |
| Flicker on resize | Set matching `Background` on Window; avoid heavy resize recalculations |
| Blurry images on HiDPI | Provide `@2x` asset variants |
| Dark title bar on Win 10 | Use custom title bar or P/Invoke `DwmSetWindowAttribute(20)` |
| SmartScreen warning | Build reputation with OV certs; submit to Microsoft ISG |
| Off-screen window on restore | Validate saved coordinates against `Screens.All` |
| Taskbar icon missing with custom chrome | Ensure `ShowInTaskbar="True"` |

---

## Key Takeaways

- Avalonia on Windows uses **Win32 + Direct3D/Skia** with software fallback; no platform-specific workloads
- **Mica** and **AcrylicBlur** require `Background="Transparent"`; check `ActualTransparencyLevel` at runtime
- **Custom title bars** use `ExtendClientAreaToDecorationsHint` + `WindowDecorationProperties.ElementRole`
- **Dark mode** is automatic via `FluentTheme`; Win 10 title bar needs custom chrome or DWM P/Invoke
- **High DPI** is per-monitor, all DIP-based; provide `@2x` assets for sharp bitmap rendering
- **WinForms interop** via `WinFormsAvaloniaControlHost` for embedding; standalone windows need message filter for keyboard
- **NativeControlHost** embeds HWND controls but has z-order and transparency limitations
- **TrayIcon** requires `.ico` format; minimize to tray with `ShowInTaskbar="False"`
- **Force software rendering** (`Win32RenderingMode.Software`) as a diagnostic first step for rendering issues

---

## See Also

- [Native Platform Interop](https://docs.avaloniaui.net/docs/app-development/native-interop)
- [Troubleshooting: Windows](https://docs.avaloniaui.net/troubleshooting/platform-specific-issues/windows)
- [042 — Multi-Targeting](042-multi-targeting-desktop-browser-mobile.md)
- [TrayIcon](https://docs.avaloniaui.net/controls/navigation/trayicon)
- [Packaging for Windows](https://docs.avaloniaui.net/tools/parcel/packaging-for-windows)
