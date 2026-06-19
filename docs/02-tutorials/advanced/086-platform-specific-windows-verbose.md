---
tier: advanced
topic: platform
estimated: 30 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 086 — Platform-Specific: Windows — Verbose

**Prerequisites:** [086-core](086-platform-specific-windows.md)

---

## 1. Rendering pipeline

Avalonia on Windows uses SkiaSharp backed by **Direct3D 11**. The rendering mode can be configured at startup:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .With(new Win32PlatformOptions
    {
        RenderingMode = new[]
        {
            Win32RenderingMode.Wgl,     // OpenGL via WGL
            Win32RenderingMode.D3D11,   // Direct3D 11 (default)
            Win32RenderingMode.Software // CPU fallback
        }
    });
```

The modes are tried in order. If the first fails, Avalonia falls through to the next. Software fallback triggers automatically in:
- Remote Desktop sessions
- Virtual machines without GPU passthrough
- When no suitable GPU driver is available

---

## 2. Transparency levels — fallback chain

Avalonia processes `TransparencyLevelHint` as a priority list. On Windows:

| Requested | Available? | Result |
|---|---|---|
| `Mica` | Win 11 | Mica backdrop |
| `Mica` | Win 10 | Falls to `AcrylicBlur` if 1803+, else `Transparent` |
| `AcrylicBlur` | Win 10 1803+ | Acrylic backdrop |
| `AcrylicBlur` | < 1803 | Falls to `Transparent` |
| `Transparent` | Always | Fully transparent (per-pixel alpha) |

```csharp
var level = myWindow.ActualTransparencyLevel;
if (level == WindowTransparencyLevel.Mica)
{
    // Mica is active
}
```

---

## 3. Custom title bar — advanced

### Drag regions and interactive controls

Controls inside the `TitleBar`-marked region still receive input normally. Nest interactive areas inside a separate element:

```xml
<Border Grid.Row="0" Background="#2D2D2D"
        WindowDecorationProperties.ElementRole="TitleBar">
  <Grid ColumnDefinitions="Auto,*,Auto">
    <Image Source="/Assets/icon.png" Width="20" Height="20" Margin="12,0" />
    <TextBlock Grid.Column="1" Text="MyApp" Foreground="White"
               VerticalAlignment="Center" />
    <StackPanel Grid.Column="2" Orientation="Horizontal">
      <Button Click="Minimize_Click" Content="—" />
      <Button Click="Maximize_Click" Content="□" />
      <Button Click="Close_Click" Content="✕" />
    </StackPanel>
  </Grid>
</Border>
```

### WindowDecorations modes

| Value | Effect |
|---|---|
| `None` | No system chrome — you draw everything |
| `Full` | System min/max/close buttons visible (disabled buttons hidden, not greyed) |
| `Default` | System default |

---

## 4. Dark mode — Windows 10 workaround

```csharp
[DllImport("dwmapi.dll")]
private static extern int DwmSetWindowAttribute(
    IntPtr hwnd, int attr, ref int value, int size);

private void SetDarkTitleBar(Window window, bool isDark)
{
    if (!OperatingSystem.IsWindows()) return;
    var handle = window.TryGetPlatformHandle()?.Handle;
    if (handle is null) return;

    int value = isDark ? 1 : 0;
    DwmSetWindowAttribute(handle.Value, 20, ref value, sizeof(int));
}
```

Attribute `20` = `DWMWA_USE_IMMERSIVE_DARK_MODE`. This is undocumented and works on Windows 10 build 18985+.

Call it after window opens and on `ActualThemeVariantChanged`:

```csharp
protected override void OnOpened(EventArgs e)
{
    base.OnOpened(e);
    SetDarkTitleBar(this, ActualThemeVariant == ThemeVariant.Dark);
    ActualThemeVariantChanged += (_, _) =>
        SetDarkTitleBar(this, ActualThemeVariant == ThemeVariant.Dark);
}
```

---

## 5. High DPI — pixel snapping

Avalonia does not snap layout to pixel boundaries by default — it preserves sub-pixel precision. This can cause 1px lines to appear blurry at non-integer scaling factors.

For sharp 1px borders, use `BoxShadow` or set `UseLayoutRounding`:

```xml
<Border BorderBrush="Black" BorderThickness="1"
        UseLayoutRounding="True" />
```

`UseLayoutRounding` snaps positions and sizes to whole device pixels, which eliminates blur at the cost of sub-pixel precision.

---

## 6. WinForms interop — project structure

```
MyApp.sln
├── MyApp/                        # Cross-platform Avalonia class library
│   ├── App.axaml / App.axaml.cs
│   ├── MainView.axaml
│   ├── ViewModels/
│   └── MyApp.csproj
├── MyApp.Desktop/                # Standalone desktop (optional, for XAML previewer)
│   ├── Program.cs
│   └── MyApp.Desktop.csproj
└── MyApp.WinForms/               # Existing WinForms app
    ├── MainForm.cs
    ├── Program.cs
    └── MyApp.WinForms.csproj
```

`MyApp.WinForms.csproj` references:
- `Avalonia.Desktop` (package)
- `Avalonia.Win32.Interoperability` (package)
- `MyApp.csproj` (project)

---

## 7. NativeControlHost lifecycle

| Method | When to override |
|---|---|
| `CreateNativeControlCore` | Create the native HWND/NSView |
| `DestroyNativeControlCore` | Destroy native handle |
| `ArrangeOverride` | Position the native window within the layout slot |

Native controls are positioned using `MoveWindow` / `SetWindowPos` in the arrange pass.

---

## 8. SmartScreen and code signing

| Certificate type | Trust timeline |
|---|---|
| EV certificate | Immediate SmartScreen bypass |
| Microsoft Trusted Signing | Immediate SmartScreen bypass |
| OV certificate | 3–6 months reputation building |

To accelerate OV reputation: submit to [Microsoft ISG](https://www.microsoft.com/en-us/wdsi/filesubmission), distribute through well-known sources, and never switch certificates.

---

## Key Takeaways

- Rendering modes are tried in order — specify `Software` last for automatic GPU fallback
- Transparency level hint is a fallback chain — check `ActualTransparencyLevel` to know what's active
- Custom title bar drag region is marked via `WindowDecorationProperties.ElementRole="TitleBar"`
- Dark title bar on Win 10 uses undocumented DWM API (attribute 20)
- `UseLayoutRounding` snaps to pixel grid for sharp 1px borders at non-integer scales
- WinForms interop requires `Avalonia.Win32.Interoperability` package and `SetupWithoutStarting()`
- Standalone Avalonia windows from WinForms need `WinFormsAvaloniaMessageFilter` for keyboard input
- OV certificates require 3–6 months of reputation building; EV and Trusted Signing bypass SmartScreen immediately
