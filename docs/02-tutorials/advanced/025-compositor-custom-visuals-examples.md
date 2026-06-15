---
tier: advanced
topic: rendering
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 025-compositor-custom-visuals.md
---

# 025E — Compositor & Custom Visuals: Real-World Examples

**Applies to:** [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md) | [025V — In-Depth Companion](025-compositor-custom-visuals-verbose.md)

---

## Example 1: ParallaxStarfield

### Goal

A background control that renders hundreds of stars moving at different speeds to create a parallax depth effect. Stars are `Visual` subclasses positioned and transformed by the compositor via `RequestCompositionUpdate`. The control uses no layout — all positioning is done through `Bounds` and `RenderTransform`.

### ViewModel

```csharp
// ViewModels/SpaceViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class SpaceViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isAnimating = true;
}
```

### XAML View

```xml
<!-- Views/SpaceView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:controls="using:MyApp.Controls"
             x:DataType="vm:SpaceViewModel">
  <Grid>
    <controls:ParallaxStarfield StarCount="300"
                                 Speed="0.5"
                                 StarSizeMin="1"
                                 StarSizeMax="3"
                                 IsAnimating="{Binding IsAnimating}" />
  </Grid>
</UserControl>
```

### How It Works

1. `ParallaxStarfield` extends `Control`. It creates N `Star` objects (a `Visual` subclass) in its constructor and adds them to `VisualChildren`.
2. Each `Star` has:
   - `Position` (Point) — the star's current coordinates.
   - `Depth` (0.0–1.0) — determines speed multiplier and opacity. Deep stars move slower and are dimmer.
   - `Radius` (1–3) — rendered as a filled ellipse in `Render(DrawingContext)`.
3. In `OnAttachedToVisualTree`, the control calls `Compositor?.RequestCompositionUpdate(OnFrame)`. The returned `IDisposable` is stored and disposed in `OnDetachedFromVisualTree`.
4. `OnFrame(Compositor, CompositionUpdateEventArgs)` iterates all stars. Each star's position is updated: `position.X -= speed * depth * deltaTime`. Stars that exit the left edge are recycled to the right edge with a random Y and depth.
5. The `Star.Render` override draws a small ellipse. Because `InvalidateVisual()` is called on each `Star` every frame, the compositor re-renders them. However, since the stars are small (1–3px ellipses) and the compositor caches their drawing, the per-frame cost is low.

### C# Implementation

```csharp
// Controls/ParallaxStarfield.cs
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace MyApp.Controls;

public class Star : Visual
{
    public Point Position { get; set; }
    public double Depth { get; set; }
    public double Radius { get; set; } = 1.5;

    public override void Render(DrawingContext context)
    {
        var opacity = 0.3 + (0.7 * Depth);
        var color = new ImmutableSolidColorBrush(Colors.White, opacity);
        context.FillEllipse(color, new Rect(Position.X - Radius, Position.Y - Radius, Radius * 2, Radius * 2));
    }
}

public class ParallaxStarfield : Control
{
    private readonly Random _random = new();
    private Star[]? _stars;
    private IDisposable? _compositionRequest;
    private TimeSpan _lastTick;

    public static readonly StyledProperty<int> StarCountProperty =
        AvaloniaProperty.Register<ParallaxStarfield, int>(nameof(StarCount), 200);

    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<ParallaxStarfield, double>(nameof(Speed), 0.5);

    public static readonly StyledProperty<double> StarSizeMinProperty =
        AvaloniaProperty.Register<ParallaxStarfield, double>(nameof(StarSizeMin), 1);

    public static readonly StyledProperty<double> StarSizeMaxProperty =
        AvaloniaProperty.Register<ParallaxStarfield, double>(nameof(StarSizeMax), 3);

    public static readonly StyledProperty<bool> IsAnimatingProperty =
        AvaloniaProperty.Register<ParallaxStarfield, bool>(nameof(IsAnimating), true);

    public int StarCount
    {
        get => GetValue(StarCountProperty);
        set => SetValue(StarCountProperty, value);
    }

    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public double StarSizeMin
    {
        get => GetValue(StarSizeMinProperty);
        set => SetValue(StarSizeMinProperty, value);
    }

    public double StarSizeMax
    {
        get => GetValue(StarSizeMaxProperty);
        set => SetValue(StarSizeMaxProperty, value);
    }

    public bool IsAnimating
    {
        get => GetValue(IsAnimatingProperty);
        set => SetValue(IsAnimatingProperty, value);
    }

    static ParallaxStarfield()
    {
        AffectsRender<ParallaxStarfield>(StarCountProperty, SpeedProperty,
            StarSizeMinProperty, StarSizeMaxProperty, IsAnimatingProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StarCountProperty)
            RecreateStars();
    }

    private void RecreateStars()
    {
        if (_stars is not null)
            foreach (var star in _stars)
                VisualChildren.Remove(star);

        var count = Math.Max(0, StarCount);
        _stars = new Star[count];

        for (int i = 0; i < count; i++)
        {
            var star = new Star
            {
                Position = new Point(
                    _random.NextDouble() * (Bounds.Width > 0 ? Bounds.Width : 800),
                    _random.NextDouble() * (Bounds.Height > 0 ? Bounds.Height : 600)),
                Depth = _random.NextDouble(),
                Radius = StarSizeMin + _random.NextDouble() * (StarSizeMax - StarSizeMin),
            };
            _stars[i] = star;
            VisualChildren.Add(star);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RecreateStars();
        _compositionRequest = Compositor?.RequestCompositionUpdate(OnFrame);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _compositionRequest?.Dispose();
        _compositionRequest = null;
    }

    private void OnFrame(Compositor compositor, CompositionUpdateEventArgs args)
    {
        if (!IsAnimating || _stars is null) return;

        var dt = (args.TotalTime - _lastTick).TotalSeconds;
        _lastTick = args.TotalTime;
        if (dt <= 0 || dt > 0.1) dt = 0.016;

        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0) return;

        foreach (var star in _stars)
        {
            star.Position = new Point(
                star.Position.X - (Speed * (0.2 + 0.8 * star.Depth) * dt * 60),
                star.Position.Y);

            if (star.Position.X < -star.Radius)
            {
                star.Position = new Point(
                    width + star.Radius,
                    _random.NextDouble() * height);
                star.Depth = _random.NextDouble();
                star.Radius = StarSizeMin + _random.NextDouble() * (StarSizeMax - StarSizeMin);
            }

            star.InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return availableSize;
    }
}
```

---

### Design Decisions

- **`Visual` subclass over drawing all stars in one `Render` override.** Each `Star` has its own transform and dirty state. The compositor can independently track which stars moved. Drawing all 300 stars in a single `Render` call would require a single `InvalidateVisual()` on the parent, which redraws everything — including stars that did not move.
- **Depth as a simple float.** Three depth layers (far/medium/near) would be simpler but less smooth. Continuous depth values (0.0–1.0) produce a natural parallax spread. Map depth to speed multiplier (0.2–1.0) and opacity (0.3–1.0).
- **Recycling stars at the right edge.** When a star's X goes below 0, reset `X = Bounds.Width` and randomize Y and depth. This avoids allocating new `Star` objects or managing a pool.
- **No `MeasureOverride`.** The starfield fills the parent. `MeasureOverride` returns `availableSize` unchanged. Stars are never measured — they are positioned manually via `Bounds`.

### Edge Cases

- **`Bounds` is empty (height or width = 0) during startup.** The control has not been laid out yet. Stars cannot be placed at valid positions. Defer star creation to `ArrangeOverride` or the first `OnFrame` call when `Bounds.Width > 0`.
- **Compositor is null (designer, unit test).** Guard all compositor calls with `Compositor?.RequestCompositionUpdate(...)`. In design mode, the stars remain at their initial positions and never move.
- **Window resize.** `Bounds` changes. Stars that are now outside the new bounds should be repositioned. In `OnFrame`, clamp star positions to the current `Bounds`.
- **Star count is very large (1000+).** Each `Star` is a `Visual` — a lightweight node, but 1000 nodes still consume memory. For 5000+ particles, use a single custom `Visual` that draws all particles in its `Render` override via a shared vertex buffer pattern.

---

## Example 2: MiniMapControl

### Goal

A minimap overlay that shows a scaled-down view of a scrollable content area. The minimap renders the content at low resolution using `RenderTargetBitmap`. A draggable viewport rectangle on the minimap indicates the visible region and allows the user to scroll the main content by dragging.

### ViewModel

```csharp
// ViewModels/EditorViewModel.cs
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    [ObservableProperty]
    private double _scrollOffsetX;

    [ObservableProperty]
    private double _scrollOffsetY;

    [ObservableProperty]
    private double _scrollableWidth = 2000;

    [ObservableProperty]
    private double _scrollableHeight = 2000;

    [ObservableProperty]
    private double _viewportWidth = 800;

    [ObservableProperty]
    private double _viewportHeight = 600;
}
```

### XAML View

```xml
<!-- Views/EditorView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:controls="using:MyApp.Controls"
             x:DataType="vm:EditorViewModel">
  <Grid>
    <!-- Main scrollable content -->
    <ScrollViewer x:Name="MainScroll"
                  HorizontalScrollBarVisibility="Auto"
                  VerticalScrollBarVisibility="Auto">
      <controls:CanvasControl Width="{Binding ScrollableWidth}"
                               Height="{Binding ScrollableHeight}" />
    </ScrollViewer>

    <!-- Minimap overlay in bottom-right corner -->
    <Border Background="#CC1a1a2e" CornerRadius="8"
            VerticalAlignment="Bottom" HorizontalAlignment="Right"
            Margin="16" Padding="4"
            Width="200" Height="150">
      <controls:MiniMapControl
          ScrollableWidth="{Binding ScrollableWidth}"
          ScrollableHeight="{Binding ScrollableHeight}"
          ViewportWidth="{Binding ViewportWidth}"
          ViewportHeight="{Binding ViewportHeight}"
          ScrollOffsetX="{Binding ScrollOffsetX, Mode=TwoWay}"
          ScrollOffsetY="{Binding ScrollOffsetY, Mode=TwoWay}"
          SourceElement="{Binding #MainScroll.Content}" />
    </Border>
  </Grid>
</UserControl>
```

### How It Works

1. `MiniMapControl` extends `Control`. It takes a `SourceElement` (`Control?`), the scrollable content dimensions, and the current scroll offsets (two-way binding).
2. In `Render(DrawingContext)`, the control:
   - Computes the scale factor: `scaleX = Bounds.Width / ScrollableWidth`, `scaleY = Bounds.Height / ScrollableHeight`.
   - Creates a `RenderTargetBitmap` at the minimap's pixel size if the source content has changed (tracked by a version counter or hash).
   - Renders the source element into the `RenderTargetBitmap` via `RenderTargetBitmap.Render(SourceElement)`.
   - Draws the bitmap scaled to fit the control bounds.
   - Draws a translucent rectangle representing the viewport: `new Rect(ScrollOffsetX * scaleX, ScrollOffsetY * scaleY, ViewportWidth * scaleX, ViewportHeight * scaleY)`.
3. The minimap handles pointer input for viewport dragging:
   - `OnPointerPressed` records the drag start position and the scroll offset at that point.
   - `OnPointerMoved` during drag computes the new scroll offset: `newScrollX = (pointerX / scaleX) - viewportWidth / 2`.
   - `OnPointerReleased` ends the drag.
    - The new scroll offsets are written to the two-way bound properties, which the `ScrollViewer` reflects via its own bindings.

### C# Implementation

```csharp
// Controls/MiniMapControl.cs
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Imaging;

namespace MyApp.Controls;

public class MiniMapControl : Control
{
    private bool _isDragging;
    private Point _dragStart;
    private double _dragStartOffsetX;
    private double _dragStartOffsetY;
    private RenderTargetBitmap? _cachedBitmap;
    private int _sourceVersion;

    public static readonly StyledProperty<Control?> SourceElementProperty =
        AvaloniaProperty.Register<MiniMapControl, Control?>(nameof(SourceElement));

    public static readonly StyledProperty<double> ScrollableWidthProperty =
        AvaloniaProperty.Register<MiniMapControl, double>(nameof(ScrollableWidth), 1);

    public static readonly StyledProperty<double> ScrollableHeightProperty =
        AvaloniaProperty.Register<MiniMapControl, double>(nameof(ScrollableHeight), 1);

    public static readonly StyledProperty<double> ViewportWidthProperty =
        AvaloniaProperty.Register<MiniMapControl, double>(nameof(ViewportWidth), 100);

    public static readonly StyledProperty<double> ViewportHeightProperty =
        AvaloniaProperty.Register<MiniMapControl, double>(nameof(ViewportHeight), 100);

    public static readonly StyledProperty<double> ScrollOffsetXProperty =
        AvaloniaProperty.Register<MiniMapControl, double>(nameof(ScrollOffsetX), 0,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> ScrollOffsetYProperty =
        AvaloniaProperty.Register<MiniMapControl, double>(nameof(ScrollOffsetY), 0,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public Control? SourceElement
    {
        get => GetValue(SourceElementProperty);
        set => SetValue(SourceElementProperty, value);
    }

    public double ScrollableWidth
    {
        get => GetValue(ScrollableWidthProperty);
        set => SetValue(ScrollableWidthProperty, value);
    }

    public double ScrollableHeight
    {
        get => GetValue(ScrollableHeightProperty);
        set => SetValue(ScrollableHeightProperty, value);
    }

    public double ViewportWidth
    {
        get => GetValue(ViewportWidthProperty);
        set => SetValue(ViewportWidthProperty, value);
    }

    public double ViewportHeight
    {
        get => GetValue(ViewportHeightProperty);
        set => SetValue(ViewportHeightProperty, value);
    }

    public double ScrollOffsetX
    {
        get => GetValue(ScrollOffsetXProperty);
        set => SetValue(ScrollOffsetXProperty, value);
    }

    public double ScrollOffsetY
    {
        get => GetValue(ScrollOffsetYProperty);
        set => SetValue(ScrollOffsetYProperty, value);
    }

    static MiniMapControl()
    {
        AffectsRender<MiniMapControl>(SourceElementProperty, ScrollableWidthProperty,
            ScrollableHeightProperty, ViewportWidthProperty, ViewportHeightProperty,
            ScrollOffsetXProperty, ScrollOffsetYProperty);
    }

    public override void Render(DrawingContext context)
    {
        var scW = ScrollableWidth;
        var scH = ScrollableHeight;
        if (scW <= 0 || scH <= 0) return;

        var scaleX = Bounds.Width / scW;
        var scaleY = Bounds.Height / scH;

        if (SourceElement is not null && SourceElement.IsAttachedToVisualTree)
        {
            var srcVersion = SourceElement.GetValue(Visual.BoundsProperty.GetHashCode());
            if (_cachedBitmap is null)
            {
                _cachedBitmap = new RenderTargetBitmap(
                    Math.Max(1, (int)Bounds.Width),
                    Math.Max(1, (int)Bounds.Height));
            }
            _cachedBitmap.Render(SourceElement);
            context.DrawImage(_cachedBitmap, new Rect(0, 0, Bounds.Width, Bounds.Height));
        }

        var vpX = ScrollOffsetX * scaleX;
        var vpY = ScrollOffsetY * scaleY;
        var vpW = ViewportWidth * scaleX;
        var vpH = ViewportHeight * scaleY;

        var viewportRect = new Rect(vpX, vpY, vpW, vpH);
        context.FillRectangle(new ImmutableSolidColorBrush(Colors.White, 0.15), viewportRect);
        context.DrawRectangle(new ImmutablePen(Colors.White, 1), viewportRect);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        _isDragging = true;
        _dragStart = point;
        _dragStartOffsetX = ScrollOffsetX;
        _dragStartOffsetY = ScrollOffsetY;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_isDragging) return;

        var point = e.GetPosition(this);
        var scW = ScrollableWidth;
        var scH = ScrollableHeight;
        var vpW = ViewportWidth;
        var vpH = ViewportHeight;

        var scaleX = Bounds.Width / scW;
        var scaleY = Bounds.Height / scH;

        var deltaX = (point.X - _dragStart.X) / scaleX;
        var deltaY = (point.Y - _dragStart.Y) / scaleY;

        ScrollOffsetX = Math.Clamp(_dragStartOffsetX + deltaX, 0, scW - vpW);
        ScrollOffsetY = Math.Clamp(_dragStartOffsetY + deltaY, 0, scH - vpH);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }
}
```

---

### Design Decisions

- **`RenderTargetBitmap.Render()` for content capture.** This renders the source element into an off-screen bitmap. It is not real-time — call it only when the source content changes (throttled to max 5 times per second). Do not call it per frame.
- **Throttled re-render.** If the source content changes frequently (e.g., during canvas drawing), debounce the re-render with a 200ms timer. The minimap does not need to update at 60fps — 5fps is sufficient for navigation context.
- **Viewport rectangle as a visual indicator.** A semi-transparent `Color` fill with a white border makes the viewport clearly visible. The rectangle is drawn in `Render` using `context.FillRectangle` and `context.DrawRectangle`.
- **Scale constrained to fit.** If the content is smaller than the minimap in one dimension, center it rather than stretching. This prevents distortion for documents that are primarily one orientation.

### Edge Cases

- **`SourceElement` is null.** Draw a placeholder message "No content" and disable pointer interaction.
- **`ScrollableWidth` or `ScrollableHeight` is 0 (no content).** The scale factors become `Infinity`. Guard with `if (ScrollableWidth <= 0 || ScrollableHeight <= 0) return`.
- **`RenderTargetBitmap.Render()` on a null source or unmounted element.** Call only when `SourceElement` is non-null and attached to a visual tree. Check `SourceElement.IsAttachedToVisualTree`.
- **Drag goes outside minimap bounds.** Clamp the computed scroll offset to `0` to `(ScrollableWidth - ViewportWidth)`. This prevents the viewport from extending beyond the content area.
- **Content is smaller than viewport (no scrolling needed).** Hide the minimap viewport rectangle entirely, or show it at full size with a "fit" indicator.

---

## What These Examples Demonstrate

| Aspect | ParallaxStarfield | MiniMapControl |
|---|---|---|
| **Visual type** | Multiple lightweight `Visual` subclasses | Single `Control` with `Render` override |
| **Update mechanism** | `RequestCompositionUpdate` frame callback | `InvalidateVisual()` on source content change |
| **Rendering** | Per-star `Render(DrawingContext)` | `RenderTargetBitmap` off-screen capture |
| **Performance focus** | Many (300+) independently moving elements | Infrequent bitmap re-render, stable after capture |
| **Input handling** | None | Pointer press/move/release for viewport drag |
| **UI thread impact** | Low (simple coordinate math per frame) | Low except during bitmap re-render (throttled) |

---

## See Also

- [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md)
- [025V — Compositor & Custom Visuals (verbose companion)](025-compositor-custom-visuals-verbose.md)
- [024 — Animation & Transitions](024-animation-transitions.md) — UI-thread animation alternative
- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md) — `Render(DrawingContext)` basics
- [061 — Rendering & Interop Boundaries](../../references/61-rendering-and-interop-boundaries-opengl-vulkan-framebuffer.md) (plugin ref)
- [Avalonia Docs: Compositor](https://docs.avaloniaui.net/docs/concepts/compositor)
