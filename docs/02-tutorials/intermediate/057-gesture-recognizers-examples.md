---
tier: intermediate
topic: input
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 057-gesture-recognizers.md
---

# 057E — Gesture Recognizers: Real-World Examples

**What this is:** Two worked examples combining multiple gesture recognizers and a custom recognizer. Read [057 — Gesture Recognizers](057-gesture-recognizers.md) and [057V — Verbose Companion](057-gesture-recognizers-verbose.md) first.

---

## Example 1: Photo Gallery with Carousel + Zoom + Pull-to-Refresh

### Goal

Build a photo gallery that supports:
- Swipe left/right to navigate between photos (carousel)
- Pinch-to-zoom on the current photo
- Pull-to-refresh to reload the gallery
- Scroll panning when zoomed in
- Device-specific gesture routing (touch for navigation, mouse for pointer events)

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="PhotoGallery.MainWindow"
        Title="Photo Gallery" Width="900" Height="700">

  <Grid>
    <!-- Photo display area -->
    <Border Name="PhotoContainer" ClipToBounds="True"
            Background="#1a1a1a">
      <Image Name="CurrentPhoto" Stretch="Uniform"
             RenderTransformOrigin="0.5,0.5">
        <Image.RenderTransform>
          <TransformGroup>
            <ScaleTransform x:Name="ZoomTransform" ScaleX="1" ScaleY="1" />
            <TranslateTransform x:Name="PanTransform" X="0" Y="0" />
          </TransformGroup>
        </Image.RenderTransform>

        <Image.GestureRecognizers>
          <!-- Swipe for page navigation -->
          <SwipeGestureRecognizer CanHorizontallySwipe="True"
                                   IsMouseEnabled="False"
                                   Threshold="80" />

          <!-- Pinch for zoom -->
          <PinchGestureRecognizer />

          <!-- Scroll for panning when zoomed in -->
          <ScrollGestureRecognizer CanHorizontallyScroll="True"
                                    CanVerticallyScroll="True"
                                    IsScrollInertiaEnabled="True" />
        </Image.GestureRecognizers>
      </Image>
    </Border>

    <!-- Pull-to-refresh indicator at top -->
    <Border Name="RefreshIndicator"
            VerticalAlignment="Top" Height="60"
            Background="#444" IsVisible="False">
      <TextBlock Text="Release to refresh..."
                 Foreground="White"
                 HorizontalAlignment="Center"
                 VerticalAlignment="Center" />
    </Border>
  </Grid>
</Window>
```

### Code-behind

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;

namespace PhotoGallery;

public partial class MainWindow : Window
{
    private readonly List<string> _photos = new()
    {
        "/photos/photo1.jpg", "/photos/photo2.jpg",
        "/photos/photo3.jpg", "/photos/photo4.jpg",
    };
    private int _currentIndex;
    private double _currentZoom = 1.0;
    private double _panX, _panY;
    private bool _isRefreshing;

    public MainWindow()
    {
        InitializeComponent();

        // Swipe — page navigation
        CurrentPhoto.AddHandler(
            InputElement.SwipeGestureEndedEvent, OnSwipeEnded);

        // Pinch — zoom
        CurrentPhoto.AddHandler(
            InputElement.PinchEvent, OnPinch);
        CurrentPhoto.AddHandler(
            InputElement.PinchEndedEvent, OnPinchEnded);

        // Scroll — pan
        CurrentPhoto.AddHandler(
            InputElement.ScrollGestureEvent, OnScroll);

        // Pull-to-refresh on container
        PhotoContainer.AddHandler(
            InputElement.PullGestureEvent, OnPull);
        PhotoContainer.AddHandler(
            InputElement.PullGestureEndedEvent, OnPullEnded);
    }

    private void OnSwipeEnded(object? sender, SwipeGestureEndedEventArgs e)
    {
        // Velocity gate: only fast swipes navigate
        if (Math.Abs(e.Velocity.X) < 400) return;

        if (e.Velocity.X < 0 && _currentIndex < _photos.Count - 1)
            NavigateTo(_currentIndex + 1);
        else if (e.Velocity.X > 0 && _currentIndex > 0)
            NavigateTo(_currentIndex - 1);
    }

    private void OnPinch(object? sender, PinchGestureEventArgs e)
    {
        // e.Scale is relative to pinch start
        double newZoom = _currentZoom * e.Scale;
        newZoom = Math.Clamp(newZoom, 0.5, 5.0);

        ZoomTransform.ScaleX = newZoom;
        ZoomTransform.ScaleY = newZoom;
    }

    private void OnPinchEnded(object? sender, PinchGestureEndedEventArgs e)
    {
        // Commit the end scale
        _currentZoom = ZoomTransform.ScaleX;
    }

    private void OnScroll(object? sender, ScrollGestureEventArgs e)
    {
        // Only pan when zoomed in
        if (_currentZoom <= 1.0) return;

        _panX += e.Delta.X;
        _panY += e.Delta.Y;

        PanTransform.X = _panX;
        PanTransform.Y = _panY;
    }

    private void OnPull(object? sender, PullGestureEventArgs e)
    {
        if (_isRefreshing) return;

        // Show indicator when pulled past threshold
        RefreshIndicator.IsVisible = e.Distance > 40;
    }

    private async void OnPullEnded(object? sender, PullGestureEndedEventArgs e)
    {
        if (e.Distance > 60 && !_isRefreshing)
        {
            _isRefreshing = true;
            RefreshIndicator.IsVisible = true;

            // Simulate reload
            await System.Threading.Tasks.Task.Delay(1500);

            _isRefreshing = false;
            RefreshIndicator.IsVisible = false;
        }
    }

    private void NavigateTo(int index)
    {
        _currentIndex = index;
        CurrentPhoto.Source = new Avalonia.Media.Imaging.Bitmap(
            _photos[index]);

        // Reset zoom
        _currentZoom = 1.0;
        _panX = _panY = 0;
        ZoomTransform.ScaleX = ZoomTransform.ScaleY = 1.0;
        PanTransform.X = PanTransform.Y = 0;
    }
}
```

### Key points

- Three recognizers on one control — only one active at a time
- `SwipeGestureRecognizer` with `IsMouseEnabled="False"` — only touch triggers navigation
- `PinchGestureRecognizer` — `e.Scale` is relative, multiplied by current zoom
- `ScrollGestureRecognizer` — only pans when zoomed in (`_currentZoom > 1.0`)
- `PullGestureRecognizer` on parent container (separate from photo)
- Velocity gating on swipe (400 px/s threshold)

---

## Example 2: Custom Hold-to-Edit Recognizer

### Goal

Build a custom recognizer that detects a long press (hold) and enters an "edit mode" on a control, with haptic-like visual feedback.

### Custom recognizer

```csharp
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace HoldEditApp.Input;

public class HoldToEditEventArgs : RoutedEventArgs
{
    public HoldToEditEventArgs(RoutedEvent routedEvent,
        Point position, IPointer pointer)
        : base(routedEvent)
    {
        Position = position;
        Pointer = pointer;
    }

    public Point Position { get; }
    public IPointer Pointer { get; }
}

public class HoldToEditRecognizer : GestureRecognizer
{
    public static readonly RoutedEvent<HoldToEditEventArgs> HoldToEditEvent =
        RoutedEvent.Register<HoldToEditRecognizer, HoldToEditEventArgs>(
            "HoldToEdit", RoutingStrategies.Bubble);

    private bool _isTracking;
    private Point _startPoint;
    private IPointer? _activePointer;
    private IDisposable? _holdTimer;

    public TimeSpan HoldDuration { get; set; } = TimeSpan.FromMilliseconds(600);
    public double MaxMoveDistance { get; set; } = 15;

    // Preview event for visual feedback
    public event EventHandler<HoldProgressEventArgs>? HoldProgress;

    protected override void PointerPressed(PointerPressedEventArgs e)
    {
        if (_isTracking) return;

        _isTracking = true;
        _startPoint = e.GetPosition(null);
        _activePointer = e.Pointer;

        // Start hold timer
        _holdTimer?.Dispose();
        _holdTimer = DispatcherTimer.RunOnce(
            OnHoldCompleted, HoldDuration);
    }

    protected override void PointerMoved(PointerEventArgs e)
    {
        if (!_isTracking) return;

        double distance = (e.GetPosition(null) - _startPoint).Length;
        if (distance > MaxMoveDistance)
        {
            CancelHold();
        }
    }

    protected override void PointerReleased(PointerReleasedEventArgs e)
    {
        CancelHold();
    }

    private void OnHoldCompleted()
    {
        if (!_isTracking) return;

        // Raise the hold event
        var args = new HoldToEditEventArgs(
            HoldToEditEvent,
            _startPoint,
            _activePointer!);

        // Source is the last pointer-pressed element
        _activePointer?.Captured?.RaiseEvent(args);

        _isTracking = false;
    }

    private void CancelHold()
    {
        _isTracking = false;
        _holdTimer?.Dispose();
        _holdTimer = null;
    }
}

public class HoldProgressEventArgs : EventArgs
{
    public double Progress { get; set; } // 0.0 to 1.0
}
```

### Usage in an app

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:input="using:HoldEditApp.Input"
        x:Class="HoldEditApp.MainWindow"
        Title="Hold to Edit" Width="400" Height="300">

  <ListBox Name="ItemList">
    <ListBox.GestureRecognizers>
      <input:HoldToEditRecognizer />
    </ListBox.GestureRecognizers>

    <ListBoxItem Content="Item 1" Tag="1" />
    <ListBoxItem Content="Item 2" Tag="2" />
    <ListBoxItem Content="Item 3" Tag="3" />
  </ListBox>
</Window>
```

```csharp
using Avalonia.Input;
using HoldEditApp.Input;

namespace HoldEditApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ItemList.AddHandler(
            HoldToEditRecognizer.HoldToEditEvent, OnHoldToEdit);
    }

    private void OnHoldToEdit(object? sender, HoldToEditEventArgs e)
    {
        if (e.Source is ListBoxItem item && item.DataContext is { } ctx)
        {
            // Enter edit mode on this item
            item.IsSelected = true;
            item.Focus();

            // Show contextual edit UI
            EditDialog(item.Content?.ToString() ?? "");
        }
    }

    private async void EditDialog(string currentValue)
    {
        // Show an edit dialog (simplified)
        var dialog = new Window
        {
            Content = new TextBlock
            {
                Text = $"Editing: {currentValue}",
                Margin = new(20)
            },
            Width = 300,
            Height = 200,
        };
        await dialog.ShowDialog(this);
    }
}
```

### Key points

- Custom recognizer subclassing `GestureRecognizer`
- Uses `DispatcherTimer` for hold duration
- Cancel hold if pointer moves more than `MaxMoveDistance`
- Raises custom routed event (`HoldToEditEvent`)
- `e.Pointer.Captured` is the element under the pointer when the hold completes

---

## See Also

- [057 — Gesture Recognizers (core tutorial)](057-gesture-recognizers.md)
- [057V — Gesture Recognizers (verbose companion)](057-gesture-recognizers-verbose.md)
- [057Q — Gesture Recognizers (quiz)](057-gesture-recognizers-quiz.md)
