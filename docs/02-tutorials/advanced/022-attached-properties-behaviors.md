---
tier: advanced
topic: extensibility
estimated: 10 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 022 — Attached Properties & Behaviors

**What you'll learn:** Create attached properties to extend any control, and build reusable attached behaviors for cross-cutting concerns.

**Prerequisites:** [003 — Basic Styling](/docs/02-tutorials/basics/003-basic-styling.md)

---

## 1. Simple attached property

```csharp
// Attached/ToolTipService.cs
using Avalonia;
using Avalonia.Controls;

namespace MyApp.Attached;

public static class ToolTipService
{
    public static readonly AttachedProperty<string?> ToolTipProperty =
        AvaloniaProperty.RegisterAttached<ToolTipService, AvaloniaObject, string?>(
            "ToolTip");

    public static void SetToolTip(AvaloniaObject element, string? value) =>
        element.SetValue(ToolTipProperty, value);

    public static string? GetToolTip(AvaloniaObject element) =>
        element.GetValue(ToolTipProperty);
}
```

Usage:

```xml
<Button Content="Delete"
        attached:ToolTipService.ToolTip="Deletes the selected item" />
```

---

## 2. Attached property with behavior

```csharp
// Attached/EnterKeyBehavior.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

public static class EnterKeyBehavior
{
    public static readonly AttachedProperty<ICommand?> CommandProperty =
        AvaloniaProperty.RegisterAttached<EnterKeyBehavior, AvaloniaObject, ICommand?>(
            "Command");

    static EnterKeyBehavior()
    {
        CommandProperty.Changed.AddClassHandler<TextBox>(HandleCommandChanged);
    }

    private static void HandleCommandChanged(TextBox sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is not null)
            sender.KeyDown -= OnTextBoxKeyDown;

        if (e.NewValue is ICommand)
            sender.KeyDown += OnTextBoxKeyDown;
    }

    private static void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            var command = GetCommand(textBox);
            if (command?.CanExecute(null) == true)
                command.Execute(null);
        }
    }

    public static void SetCommand(AvaloniaObject element, ICommand? value) =>
        element.SetValue(CommandProperty, value);

    public static ICommand? GetCommand(AvaloniaObject element) =>
        element.GetValue(CommandProperty);
}
```

Usage:

```xml
<TextBox Text="{Binding SearchText}"
         attached:EnterKeyBehavior.Command="{Binding SearchCommand}" />
```

---

## 3. Attached behavior with event subscription cleanup

```csharp
public static class FocusBehavior
{
    public static readonly AttachedProperty<bool> AutoFocusProperty =
        AvaloniaProperty.RegisterAttached<FocusBehavior, AvaloniaObject, bool>("AutoFocus");

    static FocusBehavior()
    {
        AutoFocusProperty.Changed.AddClassHandler<Control>(OnAutoFocusChanged);
    }

    private static void OnAutoFocusChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
            control.AttachedToVisualTree += OnAttached;
        else
            control.AttachedToVisualTree -= OnAttached;
    }

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control control)
            control.Focus();
    }

    public static void SetAutoFocus(AvaloniaObject element, bool value) =>
        element.SetValue(AutoFocusProperty, value);

    public static bool GetAutoFocus(AvaloniaObject element) =>
        element.GetValue(AutoFocusProperty);
}
```

Usage:

```xml
<TextBox Text="{Binding UserName}"
         attached:FocusBehavior.AutoFocus="True" />
```

---

## 4. Attached property for styling

```xml
<Style Selector="TextBox">

  <!-- Override the default Watermark based on an attached property -->
  <Setter Property="Watermark" Value="{Binding
    (attached:InputExtensions.Watermark),
    RelativeSource={RelativeSource Self}}" />
</Style>
```

```csharp
public static class InputExtensions
{
    public static readonly AttachedProperty<string?> WatermarkProperty =
        AvaloniaProperty.RegisterAttached<InputExtensions, AvaloniaObject, string?>("Watermark");
    // ... get/set
}
```

---

## 5. When to use attached properties vs behaviors

| Approach | Use for |
|---|---|
| Attached property | Extending a control with a new data value |
| Attached behavior | Adding interaction logic (keyboard, focus, gesture handling) |
| Built-in event handlers | Simple, one-off code-behind |
| Commands in ViewModel | Business logic that belongs in the VM |

Attached behaviors are the Avalonia equivalent of WPF's `Interactivity.Behaviors` — clean, reusable, XAML-composable logic extensions.

---

## Key Takeaways

- `RegisterAttached` creates properties usable on any `AvaloniaObject`
- Subscribe to property changes with `Property.Changed.AddClassHandler<T>()`
- Clean up event handlers when the attached property value changes
- Attached behaviors replace WPF's `System.Windows.Interactivity` — no extra library needed
- Use attached properties for both data extension and behavior injection

---

## See Also

- [016 — Property System & Attached Properties](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/16-property-system-attached-properties-behaviors-and-style-properties.md) (plugin ref)
- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md)
- [Avalonia Docs: Attached Properties](https://docs.avaloniaui.net/docs/data-binding/attached-properties)
