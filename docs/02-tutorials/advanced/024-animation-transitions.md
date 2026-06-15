---
tier: advanced
topic: animation
estimated: 10 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 024 — Animation & Transitions

**What you'll learn:** Create XAML animations with `Style.Animations`, use transitions on controls, build keyframe animations, and use the animation API from code.

**Prerequisites:** [003 — Basic Styling](../basics/003-basic-styling.md)

---

## 1. Style trigger animation

```xml
<Style Selector="Button.primary">
  <Setter Property="Background" Value="#6a33ff" />

  <Style.Animations>
    <Animation Duration="0:0:0.2"
               FillMode="Forward">
      <KeyFrame Cue="0%">
        <Setter Property="ScaleTransform" Value="1.0" />
      </KeyFrame>
      <KeyFrame Cue="100%">
        <Setter Property="ScaleTransform" Value="0.95" />
      </KeyFrame>
    </Animation>
  </Style.Animations>
</Style>
```

In Avalonia, `ScaleTransform` is a `LayoutTransform` applied via the `RenderTransform` property. This animation scales the button to 0.95 over 200ms.

---

## 2. Trigger animations on pseudo-classes

```xml
<Style Selector="Button.primary">
  <Setter Property="Background" Value="#6a33ff" />
</Style>

<Style Selector="Button.primary /pressed/">
  <Style.Animations>
    <Animation Duration="0:0:0.15">
      <KeyFrame Cue="0%">
        <Setter Property="Background" Value="#6a33ff" />
      </KeyFrame>
      <KeyFrame Cue="100%">
        <Setter Property="Background" Value="#4a1ac0" />
      </KeyFrame>
    </Animation>
  </Style.Animations>
</Style>
```

The background animates from the default to a darker shade when pressed.

---

## 3. Transitions (declarative, per-property)

```xml
<Border Background="{Binding BackgroundColor}"
        CornerRadius="8"
        Padding="16">
  <Border.Transitions>
    <Transitions>
      <BrushTransition Property="Background"
                       Duration="0:0:0.3" />
      <DoubleTransition Property="CornerRadius"
                        Duration="0:0:0.2" />
    </Transitions>
  </Border.Transitions>
</Border>
```

Transitions animate property changes automatically — when `BackgroundColor` changes, the brush animates over 300ms.

Avalonia 12 transitions stop ticking when the control is hidden (improved CPU usage). Set `Animation.PlaybackBehavior="Always"` to override.

---

## 4. Available transition types

| Type | Property type |
|---|---|
| `DoubleTransition` | `double` |
| `FloatTransition` | `float` |
| `BrushTransition` | `IBrush` |
| `ColorTransition` | `Color` |
| `PointTransition` | `Point` |
| `SizeTransition` | `Size` |
| `VectorTransition` | `Vector` |
| `ThicknessTransition` | `Thickness` |
| `CornerRadiusTransition` | `CornerRadius` |
| `TransformOperationsTransition` | `TransformOperations` |
| `BoolTransition` | `bool` (Instant cross-fade style) |

---

## 5. Keyframe animation from code

```csharp
using Avalonia.Animation;
using Avalonia.Animations;
using Avalonia.Media;
using Avalonia.Styling;

public async Task FadeOut(Control target)
{
    var animation = new Animation
    {
        Duration = TimeSpan.FromSeconds(0.3),
        FillMode = FillMode.Forward
    };

    animation.Children.Add(new KeyFrame
    {
        Cue = new Cue(0.0),
        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 1.0 } }
    });

    animation.Children.Add(new KeyFrame
    {
        Cue = new Cue(1.0),
        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 0.0 } }
    });

    await animation.RunAsync(target, null);

    target.IsVisible = false;
}
```

---

## 6. RenderTransform for performant animation

```xml
<Button Content="Animate">
  <Button.RenderTransform>
    <ScaleTransform ScaleX="1" ScaleY="1" />
  </Button.RenderTransform>

  <Button.Styles>
    <Style Selector="^/pointerover/">
      <Style.Animations>
        <Animation Duration="0:0:0.2">
          <KeyFrame Cue="0%">
            <Setter Property="RenderTransform" Value="scale(1)" />
          </KeyFrame>
          <KeyFrame Cue="100%">
            <Setter Property="RenderTransform" Value="scale(1.05)" />
          </KeyFrame>
        </Animation>
      </Style.Animations>
    </Style>
  </Button.Styles>
</Button>
```

`RenderTransform` uses the GPU/compositor and doesn't trigger re-layout. Prefer it over `LayoutTransform` for animation.

---

## Key Takeaways

- **Transitions** animate property changes automatically — simplest approach
- **Style animations** run on triggers (pseudo-class, etc.)
- **Keyframe animations** for complex multi-step sequences
- **RenderTransform** animations are GPU-accelerated, avoid layout passes
- In v12, animations on hidden controls are stopped by default (use `PlaybackBehavior="Always"` to change)

---

## See Also

- [024 — Animation & Transitions](024-animation-transitions.md) — comprehensive coverage of animation primitives
- [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md)
- [024E — Animation & Transitions (examples)](024-animation-transitions-examples.md)
- [024V — Animation & Transitions (verbose companion)](024-animation-transitions-verbose.md)
- [Avalonia Docs: Animation](https://docs.avaloniaui.net/docs/animation/)
