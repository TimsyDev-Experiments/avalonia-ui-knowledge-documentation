---
tier: intermediate
topic: controls
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 065V — Range & Toggle Controls (verbose companion)

**What this covers:** RangeBase internals, Slider ticks API, ProgressBar template parts, NumericUpDown validation edge cases, CheckBox select-all pattern, and ToggleSwitch knob transitions.

**Prerequisites:** 065 — Range & Toggle Controls core

---

## 1. RangeBase base class

`Slider` and `ProgressBar` inherit from `RangeBase`, which provides:

```csharp
public class RangeBase : TemplatedControl
{
    public static readonly StyledProperty<double> MinimumProperty;
    public static readonly StyledProperty<double> MaximumProperty;
    public static readonly StyledProperty<double> ValueProperty;

    public double Minimum { get; set; }
    public double Maximum { get; set; }
    public double Value { get; set; }

    public event EventHandler<RangeBaseValueChangedEventArgs>? ValueChanged;
}
```

When `Minimum` exceeds `Maximum`, the control clamps — always check your ranges in view-model code.

### Value coersion

The `CoerceValue` callback ensures `Value` stays between `Minimum` and `Maximum`. If you programmatically set `Value` outside the range, it is clamped silently.

---

## 2. Slider: Ticks API

The `Ticks` property accepts a collection of explicit tick positions:

```csharp
slider.Ticks = new DoubleCollection { 0, 25, 50, 75, 100 };
```

Combine with `IsSnapToTickEnabled="True"` to snap to your custom positions. When both `Ticks` and `TickFrequency` are set, `Ticks` takes precedence.

### TickPlacement values

| Value | Effect |
|-------|--------|
| `None` | No tick marks |
| `TopLeft` | Above horizontal / left of vertical |
| `BottomRight` | Below horizontal / right of vertical |
| `Outside` | Both sides |

---

## 3. ProgressBar template parts

| Part name | Type | Purpose |
|-----------|------|---------|
| `PART_Indicator` | `Border` | Filled area |
| `PART_Track` | `Border` | Background track |
| `PART_ProgressBarText` | `TextBlock` | Overlay caption |

Style the indicator:

```xml
<Style Selector="ProgressBar /template/ Border#PART_Indicator">
  <Setter Property="CornerRadius" Value="4" />
</Style>
```

### ProgressTextFormat

Uses `string.Format` with these indices:

| Index | Value |
|-------|-------|
| `{0}` | Current `Value` |
| `{1}` | Percentage (0–100) |
| `{2}` | `Minimum` |
| `{3}` | `Maximum` |

```xml
<ProgressBar Value="7" Maximum="10"
             ShowProgressText="True"
             ProgressTextFormat="{1:F0}% ({0}/{3})" />
```

---

## 4. NumericUpDown validation

The control clamps `Value` to `[Minimum, Maximum]` on loss of focus. Additional behaviors:

```csharp
// Non-numeric characters are silently ignored while typing
// Setting Value = null clears the input (use for "unset" state)
// FormatString accepts standard .NET numeric formats:
//   "C2" → $1,234.56
//   "P0" → 50%
//   "N3" → 1,234.568
```

### Binding exception on clear

Clearing all input may throw a binding exception. To avoid this, handle it with a fallback:

```xml
<NumericUpDown Value="{Binding Quantity, FallbackValue=0}" />
```

### Read-only display

```xml
<NumericUpDown Value="{Binding Total}"
               ShowButtonSpinner="False"
               AllowSpin="False"
               IsHitTestVisible="False" />
```

---

## 5. CheckBox select-all pattern

A three-state CheckBox as a "select all" with child items:

```csharp
private void UpdateSelectAllState()
{
    int selected = Items.Count(i => i.IsSelected);
    if (selected == 0)            SelectAllState = false;
    else if (selected == Items.Count) SelectAllState = true;
    else                          SelectAllState = null; // indeterminate
}

partial void OnSelectAllStateChanged(bool? value)
{
    if (value.HasValue)
        foreach (var item in Items)
            item.IsSelected = value.Value;
}
```

---

## 6. RadioButton: GroupName scope

When `GroupName` is not set, Avalonia groups by parent container. This means two `RadioButton` elements in the same `StackPanel` form a group. Use `GroupName` to create cross-container groups:

```xml
<StackPanel>
  <RadioButton GroupName="Theme" Content="Light" />
</StackPanel>
<StackPanel>
  <RadioButton GroupName="Theme" Content="Dark" />
</StackPanel>
```

### Programmatic clearance

```csharp
// Clear all radio buttons in a group
foreach (var rb in myPanel.GetRealizedContainers()
         .OfType<RadioButton>().Where(rb => rb.GroupName == "Theme"))
{
    rb.IsChecked = false;
}
```

---

## 7. ToggleSwitch knob transitions

Customize the knob animation:

```xml
<ToggleSwitch IsChecked="{Binding IsActive}">
  <ToggleSwitch.KnobTransitions>
    <Transitions>
      <ThicknessTransition Property="ToggleSwitch.KnobMargin"
                           Duration="0:0:0.3" />
    </Transitions>
  </ToggleSwitch.KnobTransitions>
</ToggleSwitch>
```

The knob moves via `KnobMargin` (a `Thickness`). You can also transition `RenderTransform` for custom slide effects.

### OnContent / OffContent with templates

```xml
<ToggleSwitch IsChecked="{Binding IsMuted}">
  <ToggleSwitch.OnContentTemplate>
    <DataTemplate>
      <PathIcon Data="{StaticResource speaker_regular}" />
    </DataTemplate>
  </ToggleSwitch.OnContentTemplate>
  <ToggleSwitch.OffContentTemplate>
    <DataTemplate>
      <PathIcon Data="{StaticResource speaker_off_regular}" />
    </DataTemplate>
  </ToggleSwitch.OffContentTemplate>
</ToggleSwitch>
```

---

## 8. Common patterns

### Slider as read-only indicator

```xml
<Slider Value="{Binding DownloadProgress}"
        IsHitTestVisible="False"
        IsSnapToTickEnabled="False" />
```

### Percentage slider with label

```xml
<StackPanel Orientation="Horizontal" Spacing="8">
  <Slider Width="200" Minimum="0" Maximum="1"
          TickFrequency="0.1" IsSnapToTickEnabled="True"
          Value="{Binding OpacityLevel}" />
  <TextBlock Text="{Binding OpacityLevel, StringFormat='{0:P0}'}"
             VerticalAlignment="Center" />
</StackPanel>
```

---

## See Also

- [065 — Range & Toggle Controls (core)](065-range-toggle-controls.md)
- [065E — Range & Toggle Controls (examples)](065-range-toggle-controls-examples.md)
- [Avalonia API: RangeBase](https://docs.avaloniaui.net/api/avalonia/controls/primitives/rangebase)
- [Avalonia API: Slider](https://docs.avaloniaui.net/api/avalonia/controls/slider)
- [058 — ScrollViewer & ScrollBar](058-scrollviewer-scrollbar.md)
