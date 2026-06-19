---
tier: intermediate
topic: property-system
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 052-property-system.md
---

# 052E — Property System: Real-World Examples

**What this is:** Two worked examples showing Avalonia property system patterns in real controls. Read [052 — Property System](052-property-system.md) and [052V — Verbose Companion](052-property-system-verbose.md) first.

---

## Example 1: ProgressSlider — StyledProperty with coercion + DirectProperty

### Goal

Build a custom slider control that:
- Exposes a `Value` property as a `StyledProperty` with coercion (clamps 0–100)
- Tracks an internal `IsDragging` state as a `DirectProperty`
- Updates a `ProgressBar` fill via a change callback

### View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="SliderApp.Controls.ProgressSlider">
  <Grid Name="layoutRoot" Height="32">
    <Border Name="trackBar" Background="{StaticResource TrackBrush}"
            CornerRadius="4" />
    <Border Name="fillBar" Background="{StaticResource FillBrush}"
            CornerRadius="4"
            HorizontalAlignment="Left" />
    <Thumb Name="thumb" Background="{StaticResource ThumbBrush}"
           Width="12" Height="24"
           Canvas.ZIndex="1" />
  </Grid>
</UserControl>
```

### Code-behind

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace SliderApp.Controls;

public partial class ProgressSlider : UserControl
{
    // ── StyledProperty with coercion ──────────────────────────────────

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ProgressSlider, double>(
            nameof(Value),
            defaultValue: 0.0,
            coerce: CoerceValue);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static double CoerceValue(AvaloniaObject sender, double value)
        => Math.Clamp(value, 0.0, 100.0);

    // ── DirectProperty for internal state ────────────────────────────

    public static readonly DirectProperty<ProgressSlider, bool> IsDraggingProperty =
        AvaloniaProperty.RegisterDirect<ProgressSlider, bool>(
            nameof(IsDragging),
            o => o.IsDragging);

    private bool _isDragging;
    public bool IsDragging
    {
        get => _isDragging;
        private set => SetAndRaise(IsDraggingProperty, ref _isDragging, value);
    }

    // ── Constructor ──────────────────────────────────────────────────

    public ProgressSlider()
    {
        InitializeComponent();

        layoutRoot.AddHandler(
            InputElement.PointerPressedEvent, OnPointerPressed);
        layoutRoot.AddHandler(
            InputElement.PointerReleasedEvent, OnPointerReleased);
        layoutRoot.AddHandler(
            InputElement.PointerMovedEvent, OnPointerMoved);
    }

    // ── Update fill when Value changes ────────────────────────────────

    protected override void OnPropertyChanged(
        AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            var value = change.GetNewValue<double>();
            var width = trackBar.Bounds.Width * (value / 100.0);
            fillBar.Width = width;
            Canvas.SetLeft(thumb, width - (thumb.Width / 2));
        }
    }

    // ── Pointer handlers ─────────────────────────────────────────────

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            IsDragging = true;
            UpdateValueFromPoint(e);
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (IsDragging)
        {
            IsDragging = false;
            UpdateValueFromPoint(e);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (IsDragging)
            UpdateValueFromPoint(e);
    }

    private void UpdateValueFromPoint(PointerEventArgs e)
    {
        var point = e.GetPosition(trackBar);
        var ratio = point.X / trackBar.Bounds.Width;
        ratio = Math.Clamp(ratio, 0.0, 1.0);

        // Use SetCurrentValue so styles can still override Value
        SetCurrentValue(ValueProperty, ratio * 100.0);
    }
}
```

### Key points

- `Value` is a `StyledProperty` with coercion — any value set outside 0–100 is silently clamped
- `IsDragging` is a `DirectProperty` — internal state that does not need styling
- `OnPropertyChanged` updates the visual when `Value` changes
- `SetCurrentValue` in pointer handlers avoids creating a `LocalValue` entry, letting styles remain authoritative

---

## Example 2: CompactLayout — Inherited property with AddOwner

### Goal

Create an inheritable `IsCompact` property on a workspace container that propagates to nested panels and controls, allowing child types to read the value via `AddOwner`.

### Inherited property definition

```csharp
// Layout/CompactScope.cs
using Avalonia;

namespace WorkspaceApp.Layout;

public static class CompactScope
{
    public static readonly StyledProperty<bool> IsCompactProperty =
        AvaloniaProperty.Register<CompactScope, bool>(
            "IsCompact",
            defaultValue: false,
            inherits: true);

    public static bool GetIsCompact(AvaloniaObject element) =>
        element.GetValue(IsCompactProperty);

    public static void SetIsCompact(AvaloniaObject element, bool value) =>
        element.SetValue(IsCompactProperty, value);
}
```

### Container that scopes compact mode

```csharp
// Controls/CompactPanel.cs
using Avalonia;
using Avalonia.Controls;

namespace WorkspaceApp.Controls;

public class CompactPanel : Panel
{
    // No property registration needed — we use CompactScope.IsCompactProperty
    // via the attached property setter in XAML
}
```

### Descendant that reads the inherited value

```csharp
// Controls/CompactToolbar.cs
using Avalonia;
using Avalonia.Controls;

namespace WorkspaceApp.Controls;

public class CompactToolbar : ContentControl
{
    public static readonly StyledProperty<bool> IsCompactProperty =
        CompactScope.IsCompactProperty.AddOwner<CompactToolbar>(
            new StyledPropertyMetadata<bool>(false));

    public bool IsCompact
    {
        get => GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    protected override void OnPropertyChanged(
        AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCompactProperty)
        {
            var isCompact = change.GetNewValue<bool>();
            Padding = isCompact ? new Thickness(2) : new Thickness(8);
            FontSize = isCompact ? 11 : 14;
        }
    }
}
```

### Usage

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:ctl="using:WorkspaceApp.Controls"
        xmlns:layout="using:WorkspaceApp.Layout"
        x:Class="WorkspaceApp.MainWindow">
  <StackPanel layout:CompactScope.IsCompact="True">
    <!-- Inherits IsCompact=True from StackPanel -->
    <ctl:CompactToolbar Text="File" />

    <!-- Overrides locally -->
    <ctl:CompactToolbar Text="Edit"
                        layout:CompactScope.IsCompact="False" />

    <!-- Also inherits True -->
    <ctl:CompactToolbar Text="View" />
  </StackPanel>
</Window>
```

### How it works

1. `CompactScope.IsCompact` is registered with `inherits: true`
2. `CompactToolbar` uses `CompactScope.IsCompactProperty.AddOwner<CompactToolbar>()` so it can read the property
3. The property system walks the visual tree from each `CompactToolbar` upward until it finds a `CompactScope.IsCompact` value
4. A local override (`layout:CompactScope.IsCompact="False"`) stops inheritance for that specific instance

### Key points

- `AddOwner` lets an unrelated type read and participate in an inherited property
- The `CompactToolbar` change callback updates padding and font size reactively
- Without `AddOwner`, `GetValue(CompactScope.IsCompactProperty)` would throw on `CompactToolbar`

---

## See Also

- [052 — Property System (core tutorial)](052-property-system.md)
- [052V — Property System (verbose companion)](052-property-system-verbose.md)
- [052Q — Property System (quiz)](052-property-system-quiz.md)
