---
tier: advanced
topic: platform
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 086 — Platform-Specific: Windows — Examples

**Prerequisites:** [086-core](086-platform-specific-windows.md)

---

## Example 1: Mica window with fallback

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="MyApp.Views.MainWindow"
        Title="MicaApp"
        Width="1024" Height="768"
        TransparencyLevelHint="Mica,AcrylicBlur,Transparent"
        Background="Transparent">
  <Grid RowDefinitions="Auto,*">
    <Border Grid.Row="0" Background="#2D2D2D" Padding="12">
      <TextBlock Text="Mica Window" Foreground="White" FontSize="18" />
    </Border>

    <Border Grid.Row="1" Background="#F0F0F0" Padding="24">
      <TextBlock TextWrapping="Wrap"
                 Text="This window uses Mica on Windows 11, AcrylicBlur on Win 10 1803+, and Transparent on older versions." />
    </Border>
  </Grid>
</Window>
```

```csharp
protected override void OnOpened(EventArgs e)
{
    base.OnOpened(e);

    // Log active transparency level
    var level = ActualTransparencyLevel;
    Debug.WriteLine($"Active transparency: {level}");

    // Ensure content is readable if transparency falls back
    if (level == WindowTransparencyLevel.Transparent)
    {
        // Apply fallback background
        Background = new SolidColorBrush(Color.Parse("#F0F0F0"));
    }
}
```

---

## Example 2: Custom title bar with window controls

```xml
<Window ExtendClientAreaToDecorationsHint="True"
        WindowDecorations="None"
        Width="900" Height="600">
  <Grid RowDefinitions="32,*">
    <Border Grid.Row="0" Background="#1E1E2E"
            WindowDecorationProperties.ElementRole="TitleBar">
      <Grid ColumnDefinitions="Auto,*,Auto">
        <Image Source="/Assets/icon.png" Width="18" Height="18"
               Margin="12,0" />
        <TextBlock Grid.Column="1" Text="Code Editor"
                   Foreground="#CDD6F4" VerticalAlignment="Center"
                   FontSize="14" />
        <StackPanel Grid.Column="2" Orientation="Horizontal">
          <Button x:Name="MinimizeBtn" Content="—" Width="40"
                  Background="Transparent" Foreground="#CDD6F4"
                  BorderThickness="0"
                  Click="Minimize_Click" />
          <Button x:Name="MaximizeBtn" Content="□" Width="40"
                  Background="Transparent" Foreground="#CDD6F4"
                  BorderThickness="0"
                  Click="Maximize_Click" />
          <Button x:Name="CloseBtn" Content="✕" Width="40"
                  Background="Transparent" Foreground="#CDD6F4"
                  BorderThickness="0"
                  Click="Close_Click" />
        </StackPanel>
      </Grid>
    </Border>

    <Border Grid.Row="1" Background="#1E1E2E" Padding="16">
      <TextBlock Text="Editor content goes here"
                 Foreground="#CDD6F4" />
    </Border>
  </Grid>
</Window>
```

```csharp
public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void Minimize_Click(object? _, RoutedEventArgs _2) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object? _, RoutedEventArgs _2) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object? _, RoutedEventArgs _2) => Close();
}
```

---

## Example 3: Dark title bar on Windows 10

```csharp
public partial class DarkTitleBarWindow : Window
{
    public DarkTitleBarWindow() => InitializeComponent();

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        SetDarkMode(ActualThemeVariant == ThemeVariant.Dark);
        ActualThemeVariantChanged += (_, _) =>
            SetDarkMode(ActualThemeVariant == ThemeVariant.Dark);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr,
        ref int value, int size);

    private void SetDarkMode(bool isDark)
    {
        if (!OperatingSystem.IsWindows()) return;
        var handle = TryGetPlatformHandle()?.Handle;
        if (handle is null) return;

        int value = isDark ? 1 : 0;
        DwmSetWindowAttribute(handle.Value, 20, ref value, sizeof(int));
    }
}
```

---

## Example 4: Forcing software rendering for diagnostics

```csharp
public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new Win32PlatformOptions
        {
            RenderingMode = new[]
            {
                Win32RenderingMode.Software
            }
        })
        .LogToTrace();
```

---

## Example 5: High DPI asset loading

```csharp
// ViewModel
public partial class ProductViewModel : ObservableObject
{
    [ObservableProperty] private Bitmap? _thumbnail;

    public async Task LoadThumbnailAsync(string imagePath, double scaling)
    {
        using var stream = File.OpenRead(imagePath);
        // Decode at target resolution based on screen scaling
        int targetWidth = (int)(120 * scaling);
        _thumbnail = Bitmap.DecodeToWidth(stream, targetWidth);
    }
}
```

---

## Example 6: Embedding Win32 WebView in Avalonia

```csharp
public class Win32WebView : NativeControlHost
{
    protected override IPlatformHandle CreateNativeControlCore(
        IPlatformHandle parent)
    {
        if (OperatingSystem.IsWindows())
        {
            // Create Edge WebView2 or IE WebBrowser control
            // (simplified — real implementation requires WebView2 init)
            var hwnd = CreateWindowEx(0, "Shell Embedding", "",
                WS_CHILD | WS_VISIBLE,
                0, 0, 100, 100,
                parent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            return new PlatformHandle(hwnd, "HWND");
        }
        return base.CreateNativeControlCore(parent);
    }

    protected override void DestroyNativeControlCore(
        IPlatformHandle control)
    {
        if (OperatingSystem.IsWindows())
            DestroyWindow(control.Handle);
        else
            base.DestroyNativeControlCore(control);
    }
}
```

---

## Example 7: Tray icon with minimize-to-tray

```xml
<Window xmlns="https://github.com/avaloniaui"
        x:Class="MyApp.Views.MainWindow">
  <TrayIcon Icon="/Assets/app-icon.ico" ToolTipText="My Application">
    <TrayIcon.Menu>
      <NativeMenu>
        <NativeMenuItem Header="Show Window" Click="ShowWindow_Click" />
        <NativeMenuItemSeparator />
        <NativeMenuItem Header="Exit" Click="Exit_Click" />
      </NativeMenu>
    </TrayIcon.Menu>
  </TrayIcon>
</Window>
```

```csharp
public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Minimize to tray instead of closing
        if (e.CloseReason == WindowCloseReason.User)
        {
            e.Cancel = true;
            Hide();
            ShowInTaskbar = false;
        }
        base.OnClosing(e);
    }

    private void ShowWindow_Click(object? _, EventArgs _2)
    {
        Show();
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Exit_Click(object? _, EventArgs _2) =>
        Application.Current?.Shutdown();
}
```

---

## Example 8: Retrieving HWND for Win32 P/Invoke

```csharp
public static IntPtr GetWindowHandle(Control control)
{
    var topLevel = TopLevel.GetTopLevel(control);
    return topLevel?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
}

// Usage: set window always-on-top via Win32 API
[DllImport("user32.dll")]
private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
    int x, int y, int w, int h, uint flags);

public static void SetAlwaysOnTop(Control control, bool alwaysOnTop)
{
    var hwnd = GetWindowHandle(control);
    if (hwnd == IntPtr.Zero) return;

    var insertAfter = alwaysOnTop
        ? new IntPtr(-1)  // HWND_TOPMOST
        : new IntPtr(-2); // HWND_NOTOPMOST

    SetWindowPos(hwnd, insertAfter, 0, 0, 0, 0,
        (uint)(0x0001 | 0x0002)); // SWP_NOMOVE | SWP_NOSIZE
}
```

---

## Key Takeaways

- Mica windows need `Background="Transparent"` + `TransparencyLevelHint` — check `ActualTransparencyLevel` for fallback logic
- Custom title bars with `ExtendClientAreaToDecorationsHint` and `WindowDecorationProperties.ElementRole="TitleBar"`
- Dark title bar on Win 10 requires DWM P/Invoke — respond to `ActualThemeVariantChanged`
- Force `Win32RenderingMode.Software` to isolate GPU-related rendering bugs
- Decode bitmaps at target resolution using `scaling` factor for sharp HiDPI rendering
- Tray icon with `ShowInTaskbar` toggle supports minimize-to-tray workflow
- Retrieve HWND via `TryGetPlatformHandle()` for advanced Win32 P/Invoke
