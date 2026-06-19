---
tier: intermediate
topic: controls
estimated: 16 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 065 — Range & Toggle Controls

**What you'll learn:** How to use `Slider`, `ProgressBar`, `NumericUpDown`, `CheckBox`, `RadioButton`, and `ToggleSwitch` for numeric range input and boolean toggles.

**Prerequisites:** [001 — Project Setup](../basics/001-project-setup.md), [003 — Basic Controls](../basics/003-basic-controls.md)

---

## 1. Slider

A horizontal or vertical slider for picking a value from a range:

```xml
<Slider Minimum="0" Maximum="100" Value="{Binding Volume}"
        SmallChange="1" LargeChange="10"
        TickFrequency="10" IsSnapToTickEnabled="True" />
```

| Property | Description |
|----------|-------------|
| `Minimum` / `Maximum` | Range bounds (default 0–100) |
| `Value` | Current value (two-way bindable) |
| `SmallChange` | Arrow-key increment (default 1) |
| `LargeChange` | Track-click / Page-key increment (default 10) |
| `TickFrequency` | Interval between tick marks |
| `IsSnapToTickEnabled` | Snaps thumb to nearest tick |
| `TickPlacement` | `None`, `TopLeft`, `BottomRight`, `Outside` |
| `Orientation` | `Horizontal` (default) or `Vertical` |
| `IsDirectionReversed` | Reverses increasing-value direction |

### Vertical slider

```xml
<Slider Orientation="Vertical" Height="200"
        Minimum="0" Maximum="10" Value="{Binding Brightness}" />
```

---

## 2. ProgressBar

```xml
<ProgressBar Minimum="0" Maximum="100" Value="{Binding Progress}"
             ShowProgressText="True" />
```

### Indeterminate mode

```xml
<ProgressBar IsIndeterminate="True" />
```

| Property | Description |
|----------|-------------|
| `Minimum` / `Maximum` | Range bounds |
| `Value` | Current progress |
| `IsIndeterminate` | Animated loop when work amount is unknown |
| `ShowProgressText` | Overlays percentage text |
| `ProgressTextFormat` | Custom format string for the text |

### Bind to an async operation

```csharp
[ObservableProperty]
private int _progress;

[RelayCommand]
private async Task DownloadAsync()
{
    for (int i = 0; i <= 100; i++)
    {
        Progress = i;
        await Task.Delay(50);
    }
}
```

---

## 3. NumericUpDown

An editable numeric input with spinner buttons:

```xml
<NumericUpDown Value="{Binding Quantity}" Minimum="1"
               Maximum="100" Increment="1" />
```

| Property | Description |
|----------|-------------|
| `Value` | Current value (`decimal?`) |
| `Increment` | Step for spinner / arrows (default 1) |
| `Minimum` / `Maximum` | Allowed range |
| `FormatString` | .NET numeric format (e.g., `"0.00"`, `"C2"`) |
| `ShowButtonSpinner` | Show/hide spinner buttons (default `true`) |
| `AllowSpin` | Enable/disable spinner + keyboard increment |
| `ButtonSpinnerLocation` | `Left` or `Right` (default) |
| `InnerLeftContent` | Content on the left (e.g., `$`) |
| `InnerRightContent` | Content on the right (e.g., `kg`) |

### Decimal precision

```xml
<NumericUpDown Value="0.5" Increment="0.05" FormatString="0.00"
               Minimum="0" Maximum="1" />
```

---

## 4. CheckBox

```xml
<CheckBox IsChecked="{Binding AutoSave}" Content="Auto-save on exit" />
```

### Three-state mode

```xml
<CheckBox IsThreeState="True"
          IsChecked="{Binding SelectAllState}"
          Content="Select all" />
```

```csharp
[ObservableProperty]
private bool? _selectAllState = false;
```

The three states cycle: checked → unchecked → indeterminate (`null`) → checked.

---

## 5. RadioButton

Mutually exclusive options share a `GroupName`:

```xml
<StackPanel>
  <RadioButton GroupName="Size" Content="Small" />
  <RadioButton GroupName="Size" Content="Medium" IsChecked="True" />
  <RadioButton GroupName="Size" Content="Large" />
</StackPanel>
```

### Binding to an enum

```xml
<RadioButton Content="Standard"
             IsChecked="{Binding SelectedShipping,
                 Converter={StaticResource EnumToBoolConverter},
                 ConverterParameter={x:Static vm:ShippingMethod.Standard}}" />
```

Register the converter in `App.axaml`:

```xml
<converters:EnumToBoolConverter x:Key="EnumToBoolConverter" />
```

---

## 6. ToggleSwitch

An on/off sliding toggle for immediate settings:

```xml
<ToggleSwitch IsChecked="{Binding IsDarkMode}"
              OnContent="Dark" OffContent="Light" />
```

| Property | Description |
|----------|-------------|
| `IsChecked` | Current state (`bool`) |
| `OnContent` | Content when on (default "On") |
| `OffContent` | Content when off (default "Off") |
| `KnobTransitions` | Transitions for the knob animation |

Hide labels to show only the toggle thumb:

```xml
<ToggleSwitch IsChecked="{Binding IsActive}"
              OnContent="" OffContent="" />
```

---

## Key Takeaways

- `Slider` — bind `Value`, use `TickFrequency` + `IsSnapToTickEnabled` for discrete steps
- `ProgressBar` — `IsIndeterminate` for unknown duration; `ShowProgressText` for percentage
- `NumericUpDown` — use `FormatString` for decimal precision; `InnerLeftContent` for prefix labels
- `CheckBox` — `IsThreeState` enables the indeterminate (`null`) state
- `RadioButton` — group with `GroupName`; bind enums via `EnumToBoolConverter`
- `ToggleSwitch` — immediate on/off; hidden labels via empty `OnContent`/`OffContent`

---

## See Also

- [065V — Range & Toggle Controls (verbose)](065-range-toggle-controls-verbose.md)
- [065E — Range & Toggle Controls (examples)](065-range-toggle-controls-examples.md)
- [Avalonia Docs: Slider](https://docs.avaloniaui.net/controls/input/selectors/slider)
- [Avalonia Docs: ProgressBar](https://docs.avaloniaui.net/controls/feedback/progressbar)
- [Avalonia Docs: NumericUpDown](https://docs.avaloniaui.net/controls/input/selectors/numericupdown)
- [Avalonia Docs: CheckBox](https://docs.avaloniaui.net/controls/input/selectors/checkbox)
- [Avalonia Docs: RadioButton](https://docs.avaloniaui.net/controls/input/buttons/radiobutton)
- [Avalonia Docs: ToggleSwitch](https://docs.avaloniaui.net/controls/input/selectors/toggleswitch)
