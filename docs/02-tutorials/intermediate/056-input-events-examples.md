---
tier: intermediate
topic: input
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 056-input-events.md
---

# 056E — Input Events: Real-World Examples

**What this is:** Two worked examples showing pointer event and gesture recognizer patterns in real app scenarios. Read [056 — Input Events](056-input-events.md) and [056V — Input Events (verbose companion)](056-input-events-verbose.md) first.

---

## Example 1: Drawing Canvas with Pen Pressure

### Goal

Build a drawing canvas that supports:
- Mouse and pen drawing
- Pen pressure affects stroke width
- Pointer capture for continuous drawing
- Cursor changes per tool
- Right-click for color picker

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:SketchPad.ViewModels"
        x:Class="SketchPad.Views.MainWindow"
        x:DataType="vm:MainViewModel"
        Title="Sketch Pad" Width="800" Height="600">

  <Grid RowDefinitions="Auto,*">
    <!-- Toolbar -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="4" Spacing="4">
      <RadioButton GroupName="tool" Content="Pen"
                   IsChecked="{Binding IsPenMode}" />
      <RadioButton GroupName="tool" Content="Eraser"
                   IsChecked="{Binding IsEraserMode}" />
      <Separator Width="8" />
      <Button Content="Clear" Command="{Binding ClearCommand}" />
    </StackPanel>

    <!-- Drawing surface -->
    <Border Grid.Row="1" Background="White"
            Cursor="{Binding CurrentCursor}">
      <Border.GestureRecognizers>
        <ScrollGestureRecognizer CanHorizontallyScroll="True"
                                  CanVerticallyScroll="False" />
      </Border.GestureRecognizers>

      <Image Name="CanvasImage" Stretch="None"
             Source="{Binding Bitmap}" />
    </Border>
  </Grid>
</Window>
```

### Code-behind (drawing logic)

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace SketchPad.Views;

public partial class MainWindow : Window
{
    private WriteableBitmap? _bitmap;
    private bool _isDrawing;
    private Point _lastPoint;

    public MainWindow()
    {
        InitializeComponent();
        CreateBitmap(800, 600);

        // Subscribe to pointer events on the image
        CanvasImage.PointerPressed += OnCanvasPointerPressed;
        CanvasImage.PointerMoved += OnCanvasPointerMoved;
        CanvasImage.PointerReleased += OnCanvasPointerReleased;
    }

    private void CreateBitmap(int width, int height)
    {
        _bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888);
        CanvasImage.Source = _bitmap;
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDrawing = true;
        _lastPoint = e.GetPosition(CanvasImage);

        // Capture pointer to continue drawing even outside bounds
        e.Pointer.Capture(CanvasImage);

        if (e.Properties.IsRightButtonPressed)
        {
            // Right-click: could show color picker
            // Handled separately in a real app
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDrawing) return;

        Point current = e.GetPosition(CanvasImage);
        float pressure = e.Properties.Pressure;

        // Pen: pressure scales stroke (0.5–5px)
        // Mouse: constant 2px stroke
        double strokeWidth = e.Pointer.Type == PointerType.Pen
            ? 0.5 + (pressure * 4.5)
            : 2.0;

        DrawLine(_lastPoint, current, strokeWidth);
        _lastPoint = current;
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDrawing = false;
        e.Pointer.Capture(null); // Release capture
    }

    private unsafe void DrawLine(Point from, Point to, double width)
    {
        using var locked = _bitmap!.Lock();
        // Direct pixel manipulation (simplified — a real
        // implementation would use Bresenham or Skia)
        var buffer = (uint*)locked.Address;
        int stride = locked.RowBytes / 4;
        int x1 = (int)from.X, y1 = (int)from.Y;
        int x2 = (int)to.X, y2 = (int)to.Y;

        // Simple thick point at endpoints
        int r = (int)(width / 2);
        // ... pixel drawing logic omitted for brevity
    }
}
```

### ViewModel

```csharp
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SketchPad.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isPenMode = true;

    [ObservableProperty]
    private bool _isEraserMode;

    public Cursor CurrentCursor =>
        IsPenMode ? new Cursor(StandardCursorType.Cross) :
                    new Cursor(StandardCursorType.SizeAll);

    [RelayCommand]
    private void Clear()
    {
        // Reset bitmap to white
    }
}
```

### Key points

- `e.Pointer.Capture(CanvasImage)` — continuous drawing even if pointer exits bounds
- `e.Pointer.Type == PointerType.Pen` — pressure-sensitive stroke
- `e.GetPosition(CanvasImage)` — coordinates relative to the image
- `e.Properties.Pressure` — 0.0–1.0 for pen
- `ScrollGestureRecognizer` attached to allow panning in a real zoomed canvas

---

## Example 2: Image Viewer with Pinch-Zoom and Pan

### Goal

Build an image viewer with:
- Pinch-to-zoom via `PinchGestureRecognizer`
- Touch panning via `ScrollGestureRecognizer`
- Mouse wheel zoom
- Double-tap to reset
- Visual feedback (scale indicator)

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="ImageViewer.MainWindow"
        Title="Image Viewer" Width="900" Height="700">

  <Grid>
    <!-- Image with gesture recognizers -->
    <Border Name="ImageContainer" ClipToBounds="True"
            Background="{StaticResource GridBackground}">
      <Image Name="ImageView" Stretch="Uniform"
             Source="/sample-photo.jpg"
             RenderTransformOrigin="0.5,0.5">
        <Image.RenderTransform>
          <TransformGroup>
            <ScaleTransform x:Name="ScaleTransform" ScaleX="1" ScaleY="1" />
            <TranslateTransform x:Name="TranslateTransform"
                                X="0" Y="0" />
          </TransformGroup>
        </Image.RenderTransform>

        <Image.GestureRecognizers>
          <PinchGestureRecognizer />
          <ScrollGestureRecognizer CanHorizontallyScroll="True"
                                    CanVerticallyScroll="True"
                                    IsScrollInertiaEnabled="True" />
        </Image.GestureRecognizers>
      </Image>
    </Border>

    <!-- Zoom indicator overlay -->
    <Border HorizontalAlignment="Right" VerticalAlignment="Top"
            Margin="8" Padding="8,4"
            Background="#88000000" CornerRadius="4">
      <TextBlock x:Name="ZoomLabel" Text="{Binding ZoomPercent}"
                 Foreground="White" FontSize="14" />
    </Border>
  </Grid>
</Window>
```

### Code-behind

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace ImageViewer;

public partial class MainWindow : Window
{
    private double _currentScale = 1.0;
    private const double MinScale = 0.25;
    private const double MaxScale = 5.0;
    private double _translateX, _translateY;

    public string ZoomPercent => $"{_currentScale * 100:F0}%";

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // Subscribe to gesture events from gesture recognizers
        ImageView.AddHandler(
            InputElement.PinchEvent, OnPinch);

        ImageView.AddHandler(
            InputElement.ScrollGestureEvent, OnScrollGesture);

        // Mouse wheel zoom
        ImageView.PointerWheelChanged += OnPointerWheelChanged;

        // Double-tap reset
        ImageView.AddHandler(
            InputElement.DoubleTappedEvent, OnDoubleTapped);
    }

    private void OnPinch(object? sender, PinchEventArgs e)
    {
        // e.Scale is relative to the start of the pinch
        double newScale = _currentScale * e.Scale;
        newScale = Math.Clamp(newScale, MinScale, MaxScale);

        _currentScale = newScale;
        ScaleTransform.ScaleX = newScale;
        ScaleTransform.ScaleY = newScale;

        UpdateZoomLabel();
    }

    private void OnScrollGesture(object? sender, ScrollGestureEventArgs e)
    {
        // e.Delta is the scroll vector
        _translateX += e.Delta.X;
        _translateY += e.Delta.Y;

        TranslateTransform.X = _translateX;
        TranslateTransform.Y = _translateY;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Zoom at mouse position
        Point mousePos = e.GetPosition(ImageView);
        double zoomFactor = e.Delta.Y > 0 ? 1.1 : 1.0 / 1.1;

        double newScale = Math.Clamp(
            _currentScale * zoomFactor, MinScale, MaxScale);

        // Adjust translation so zoom is centered on mouse position
        double ratio = newScale / _currentScale;
        _translateX = mousePos.X - ratio * (mousePos.X - _translateX);
        _translateY = mousePos.Y - ratio * (mousePos.Y - _translateY);

        _currentScale = newScale;
        ScaleTransform.ScaleX = newScale;
        ScaleTransform.ScaleY = newScale;
        TranslateTransform.X = _translateX;
        TranslateTransform.Y = _translateY;

        UpdateZoomLabel();
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Reset zoom to 100%
        _currentScale = 1.0;
        _translateX = 0;
        _translateY = 0;

        ScaleTransform.ScaleX = 1.0;
        ScaleTransform.ScaleY = 1.0;
        TranslateTransform.X = 0;
        TranslateTransform.Y = 0;

        UpdateZoomLabel();
    }

    private void UpdateZoomLabel()
    {
        ZoomLabel.Text = $"{_currentScale * 100:F0}%";
    }
}
```

### Key points

- `PinchGestureRecognizer` + `ScrollGestureRecognizer` on the same control — only one active at a time
- `e.Scale` is relative, not absolute — multiply by current scale
- `e.Delta.X/Y` for scroll gesture position deltas
- `PointerWheelChanged` with `e.Delta.Y` for mouse wheel zoom
- `RenderTransformOrigin="0.5,0.5"` so scale transform is centered
- `e.PreventGestureRecognition()` not needed here since gesture recognizers and mouse wheel work independently

---

## See Also

- [056 — Input Events (core tutorial)](056-input-events.md)
- [056V — Input Events (verbose companion)](056-input-events-verbose.md)
- [056Q — Input Events (quiz)](056-input-events-quiz.md)
