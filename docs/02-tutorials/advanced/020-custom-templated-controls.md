---
tier: advanced
topic: custom controls
estimated: 12 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 020 — Custom Templated Controls

**What you'll learn:** Create a reusable templated control with `StyledProperty`, a `ControlTheme`, and designer-friendly defaults.

**Prerequisites:** [012 — Control Themes vs Styles](../intermediate/012-control-themes-vs-styles.md)

---

## 1. The control class

```csharp
// Controls/RatingControl.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace MyApp.Controls;

public class RatingControl : TemplatedControl
{
    public static readonly StyledProperty<int> ValueProperty =
        AvaloniaProperty.Register<RatingControl, int>(nameof(Value), 0);

    public static readonly StyledProperty<int> MaximumProperty =
        AvaloniaProperty.Register<RatingControl, int>(nameof(Maximum), 5);

    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Find named parts in the template
        if (e.NameScope.Get<Button>("PartIncrement") is { } increment)
        {
            // Wire up logic
        }
    }
}
```

---

## 2. The control theme (default template)

```xml
<!-- Themes/RatingControl.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:local="using:MyApp.Controls">
  <ControlTheme TargetType="local:RatingControl">
    <Setter Property="Template">
      <ControlTemplate TargetType="local:RatingControl">
        <Border Background="{TemplateBinding Background}"
                BorderBrush="{TemplateBinding BorderBrush}"
                BorderThickness="{TemplateBinding BorderThickness}"
                CornerRadius="8"
                Padding="12">
          <StackPanel Orientation="Horizontal" Spacing="4"
                      HorizontalAlignment="Center">
            <!-- Star display based on Value -->
            <TextBlock Text="★★★★★"
                       FontSize="24" />

            <!-- Buttons for +/- -->
            <Button Name="PartDecrement"
                    Content="−"
                    FontSize="18"
                    Width="32" Height="32"
                    Command="{TemplateBinding $parent[local:RatingControl].DecrementCommand}" />
            <Button Name="PartIncrement"
                    Content="+"
                    FontSize="18"
                    Width="32" Height="32"
                    Command="{TemplateBinding $parent[local:RatingControl].IncrementCommand}" />
          </StackPanel>
        </Border>
      </ControlTemplate>
    </Setter>
  </ControlTheme>
</ResourceDictionary>
```

---

## 3. Merge into App.axaml

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceInclude Source="/Themes/RatingControl.axaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

---

## 4. Usage

```xml
<controls:RatingControl Value="{Binding Rating}"
                        Maximum="5"
                        ValueChanged="OnRatingChanged" />
```

---

## 5. Adding commands to the control

```csharp
public static readonly DirectProperty<RatingControl, ICommand?> IncrementCommandProperty =
    AvaloniaProperty.RegisterDirect<RatingControl, ICommand?>(
        nameof(IncrementCommand),
        o => o.IncrementCommand);

private ICommand? _incrementCommand;

public ICommand? IncrementCommand =>
    _incrementCommand ??= new RelayCommand(Increment);

private void Increment()
{
    if (Value < Maximum)
        Value++;
}
```

---

## 6. Named parts convention

Name parts in your template with the `Part` prefix and document them:

```csharp
/// <summary>
/// The button that increments the rating.
/// Template part name: "PartIncrement"
/// </summary>
```

This follows the `TemplatedControl` convention — consumers know which named elements they can customize.

---

## Key Takeaways

- Extend `TemplatedControl` for lookless controls with replaceable templates
- Use `StyledProperty` for styleable properties, `DirectProperty` for performance-sensitive ones
- `OnApplyTemplate` is where you wire up named template parts
- Ship a default `ControlTheme` as a resource dictionary
- Add `Command` properties so users can bind to the control

---

## See Also

- [012 — Control Themes vs Styles](../intermediate/012-control-themes-vs-styles.md)
- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md)
- [Avalonia Docs: Templated Controls](https://docs.avaloniaui.net/docs/concepts/templated-controls)
