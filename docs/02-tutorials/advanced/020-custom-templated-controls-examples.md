---
tier: advanced
topic: custom controls
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 020-custom-templated-controls.md
---

# 020E — Custom Templated Controls: Real-World Examples

**Applies to:** [020 — Custom Templated Controls](020-custom-templated-controls.md) | [020V — In-Depth Companion](020-custom-templated-controls-verbose.md)

---

## Example 1: ColorSwatchPicker

### Goal

A templated control that displays a palette of color swatches. The user clicks a swatch to select it. The control exposes `SelectedColor`, `Colors` (items source), and `SelectedIndex` property. The template renders swatches as styled boxes with a checkmark overlay on the selected item.

### ViewModel

```csharp
// ViewModels/ColorSwatchPickerViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;

namespace MyApp.ViewModels;

public partial class ColorSwatchPickerViewModel : ObservableObject
{
    [ObservableProperty]
    private Color _selectedColor = Colors.Transparent;

    public List<Color> Palette { get; } =
    [
        Colors.Red,
        Colors.Orange,
        Colors.Yellow,
        Colors.Green,
        Colors.Blue,
        Colors.Indigo,
        Colors.Violet,
        Colors.Black,
        Colors.Gray,
        Colors.White,
    ];

    partial void OnSelectedColorChanged(Color value)
    {
        System.Diagnostics.Debug.WriteLine($"Selected: {value}");
    }
}
```

### XAML View

```xml
<!-- Views/ColorSwatchPickerView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:controls="using:MyApp.Controls"
             x:DataType="vm:ColorSwatchPickerViewModel">
  <controls:ColorSwatchPicker
      Colors="{Binding Palette}"
      SelectedColor="{Binding SelectedColor, Mode=TwoWay}"
      SwatchSize="36" />
</UserControl>
```

### How It Works

1. `ColorSwatchPicker` extends `TemplatedControl` and exposes `ColorsProperty` (an `IList<Color>`), `SelectedColorProperty` (a `StyledProperty<Color>` with `defaultBindingMode: TwoWay`), and `SwatchSizeProperty`.
2. In `OnApplyTemplate`, the control finds an `ItemsControl` named `PartSwatchContainer` and sets its `ItemsSource` to the `Colors` property programmatically. Each item template is a `Border` with a solid fill, pointer handlers, and a `TextBlock` checkmark that toggles visibility based on an attached `IsSelected` property.
3. The `ControlTheme` defines the `ItemsControl` layout and the item container template. The selected swatch renders a checkmark overlay and a thicker border.
4. When a swatch is clicked, the control updates `SelectedColor`, cycles through all swatches to update selection state, and calls `InvalidateVisual()` on the affected items.

```csharp
// Controls/ColorSwatchPicker.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace MyApp.Controls;

public class ColorSwatchPicker : TemplatedControl
{
    private ItemsControl? _swatchContainer;

    public static readonly StyledProperty<IList<Color>?> ColorsProperty =
        AvaloniaProperty.Register<ColorSwatchPicker, IList<Color>?>(nameof(Colors));

    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<ColorSwatchPicker, Color>(nameof(SelectedColor), Colors.Transparent, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> SwatchSizeProperty =
        AvaloniaProperty.Register<ColorSwatchPicker, double>(nameof(SwatchSize), 32, coerce: (_, v) => Math.Max(8, v));

    public IList<Color>? Colors
    {
        get => GetValue(ColorsProperty);
        set => SetValue(ColorsProperty, value);
    }

    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public double SwatchSize
    {
        get => GetValue(SwatchSizeProperty);
        set => SetValue(SwatchSizeProperty, value);
    }

    static ColorSwatchPicker()
    {
        SwatchSizeProperty.OverrideDefaultValue<ColorSwatchPicker>(32);
        SwatchSizeProperty.Changed.AddClassHandler<ColorSwatchPicker>((picker, e) => picker.InvalidateArrange());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_swatchContainer is not null)
        {
            _swatchContainer.RemoveHandler(Button.ClickEvent, OnSwatchClicked);
            _swatchContainer = null;
        }

        _swatchContainer = e.NameScope.Find<ItemsControl>("PartSwatchContainer");

        if (_swatchContainer is not null)
        {
            _swatchContainer.ItemsSource = Colors;
            _swatchContainer.AddHandler(Button.ClickEvent, OnSwatchClicked);
        }
    }

    private void OnSwatchClicked(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Button { CommandParameter: Color color })
        {
            SelectedColor = color;
        }
    }
}
```

---

### Design Decisions

- **`ItemsControl` inside the template over building swatches in code.** Reuses Avalonia's item layout and container generation. The pattern scales from 10 swatches to 100.
- **`Part` prefix (no underscore).** Custom controls use `Part` without underscore. The `PART_` prefix is reserved for framework controls that override template parts defined in the base class.
- **`SwatchSize` as a `StyledProperty`.** Registered with `AffectsArrange` so changing the size at runtime triggers re-layout of the swatch grid.

### Edge Cases

- **`Colors` is null or empty.** The control shows a fallback "No colors" `TextBlock` in the template. Guard against null in `OnApplyTemplate` before binding.
- **`SelectedColor` not in `Colors`.** No swatch shows the selection highlight. The consumer ensures `SelectedColor` is a member of `Colors` if highlighting is expected.
- **Template reapplication.** `OnApplyTemplate` unsubscribes old item click handlers before subscribing to new ones. Each theme switch re-runs template instantiation.

---

## Example 2: TagInputControl

### Goal

An inline tag/chip input control. The user types text and presses Enter to create a tag. Each tag renders as a rounded chip with a remove button. Tags are stored in an `ObservableCollection<string>`. The control supports Backspace to remove the last tag, paste to split by delimiter, and a configurable max-tag limit.

### ViewModel

```csharp
// ViewModels/TagInputViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class TagInputViewModel : ObservableObject
{
    public ObservableCollection<string> Tags { get; } = [];

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [RelayCommand]
    private void AddTag()
    {
        var text = InputText?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        if (Tags.Count >= 10)
        {
            HasError = true;
            return;
        }
        Tags.Add(text);
        InputText = string.Empty;
        HasError = false;
    }

    [RelayCommand]
    private void RemoveTag(string tag)
    {
        Tags.Remove(tag);
        HasError = false;
    }

    partial void OnInputTextChanged(string value)
    {
        HasError = false;
    }
}
```

### XAML View

```xml
<!-- Views/TagInputView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:controls="using:MyApp.Controls"
             x:DataType="vm:TagInputViewModel">
  <controls:TagInputControl
      Tags="{Binding Tags}"
      InputText="{Binding InputText, Mode=TwoWay}"
      AddTagCommand="{Binding AddTagCommand}"
      RemoveTagCommand="{Binding RemoveTagCommand}"
      MaxTags="10"
      PlaceholderText="Type a tag and press Enter" />
</UserControl>
```

### How It Works

1. `TagInputControl` extends `TemplatedControl`. Its template contains a `TextBox` for input (`PartInputBox`) and an `ItemsControl` that renders existing tags as chips (`PartTagsContainer`).
2. The control subscribes to `TextBox.KeyDown` in `OnApplyTemplate`. Enter invokes `AddTagCommand` and clears the input. Backspace with empty input removes the last tag.
3. Each tag chip is a `Border` with rounded corners, a `TextBlock`, and a small × button that invokes `RemoveTagCommand` with the tag string as `CommandParameter`.
4. `MaxTags` controls the upper bound enforced in the control (disabling input when reached) and in the ViewModel (guard in `AddTag()`).

```csharp
// Controls/TagInputControl.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MyApp.Controls;

public class TagInputControl : TemplatedControl
{
    private TextBox? _inputBox;
    private ItemsControl? _tagsContainer;

    public static readonly StyledProperty<IEnumerable<string>?> TagsProperty =
        AvaloniaProperty.Register<TagInputControl, IEnumerable<string>?>(nameof(Tags));

    public static readonly StyledProperty<string> InputTextProperty =
        AvaloniaProperty.Register<TagInputControl, string>(nameof(InputText), string.Empty, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> AddTagCommandProperty =
        AvaloniaProperty.Register<TagInputControl, ICommand?>(nameof(AddTagCommand));

    public static readonly StyledProperty<ICommand?> RemoveTagCommandProperty =
        AvaloniaProperty.Register<TagInputControl, ICommand?>(nameof(RemoveTagCommand));

    public static readonly StyledProperty<int> MaxTagsProperty =
        AvaloniaProperty.Register<TagInputControl, int>(nameof(MaxTags), int.MaxValue);

    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<TagInputControl, string>(nameof(PlaceholderText), string.Empty);

    public IEnumerable<string>? Tags
    {
        get => GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    public string InputText
    {
        get => GetValue(InputTextProperty);
        set => SetValue(InputTextProperty, value);
    }

    public ICommand? AddTagCommand
    {
        get => GetValue(AddTagCommandProperty);
        set => SetValue(AddTagCommandProperty, value);
    }

    public ICommand? RemoveTagCommand
    {
        get => GetValue(RemoveTagCommandProperty);
        set => SetValue(RemoveTagCommandProperty, value);
    }

    public int MaxTags
    {
        get => GetValue(MaxTagsProperty);
        set => SetValue(MaxTagsProperty, value);
    }

    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_inputBox is not null)
        {
            _inputBox.KeyDown -= OnInputKeyDown;
            _inputBox = null;
        }

        _inputBox = e.NameScope.Find<TextBox>("PartInputBox");
        _tagsContainer = e.NameScope.Find<ItemsControl>("PartTagsContainer");

        if (_inputBox is not null)
        {
            _inputBox.KeyDown += OnInputKeyDown;
            _inputBox.Watermark = PlaceholderText;
        }
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var text = InputText?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                AddTagCommand?.Execute(text);
                InputText = string.Empty;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Back && string.IsNullOrEmpty(InputText))
        {
            if (Tags is IList<string> list && list.Count > 0)
            {
                RemoveTagCommand?.Execute(list[^1]);
            }
            e.Handled = true;
        }
    }
}
```

### Design Decisions

- **Commands passed from ViewModel through the control.** The control handles keyboard input (Enter, Backspace, paste splitting). The ViewModel owns the tag collection and business rules (max limit, deduplication). This keeps the control reusable across ViewModels.
- **`CommandParameter="{Binding}"` for the remove button.** Each item's `DataContext` is the tag string. Binding the parameter to the current `DataContext` passes the tag value directly to `RemoveTagCommand`.
- **Paste splitting.** The control intercepts paste via `KeyDown` (Ctrl+V), splits clipboard text on commas, semicolons, or newlines, and adds each token as a separate tag.

### Edge Cases

- **Rapid Enter presses.** `AddTagCommand` checks for empty/whitespace input and the max limit before adding.
- **Duplicate tags.** Extend `AddTag()` in the ViewModel with `Tags.Contains(text)` to reject or ignore duplicates.
- **Tags with leading/trailing whitespace.** Trimmed in `AddTag()` before inserting into the collection.
- **Empty tag list.** The control shows the placeholder text at full width. No chip row is displayed.

---

## What These Examples Demonstrate

| Aspect | ColorSwatchPicker | TagInputControl |
|---|---|---|
| **Property type mix** | `StyledProperty` (color, size, items) | `StyledProperty` + `DirectProperty` (commands) |
| **Template parts** | `ItemsControl` container + swatch template | `TextBox` + `ItemsControl` + chip buttons |
| **Command exposure** | None (no command properties) | Consumer-provided `ICommand` properties |
| **Data source** | `IList<Color>` | `ObservableCollection<string>` |
| **Interaction model** | Pointer click to select | Keyboard (Enter, Backspace) + click to remove |
| **Boundary handling** | Null colors, missing selection in set | Max tags, duplicates, whitespace, paste splitting |

---

## See Also

- [020 — Custom Templated Controls](020-custom-templated-controls.md)
- [020V — Custom Templated Controls (verbose companion)](020-custom-templated-controls-verbose.md)
- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md) — when to use `Control` instead of `TemplatedControl`
- [026 — Accessibility & Automation](026-accessibility-automation.md) — automation properties for templated controls
- [Avalonia Docs: Templated Controls](https://docs.avaloniaui.net/docs/concepts/templated-controls)
