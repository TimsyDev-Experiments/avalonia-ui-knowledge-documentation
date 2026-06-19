---
tier: advanced
topic: animation
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 081E — Animation System Deep Dive (examples)

## Example 1: Fade-in on load with bounce easing

```xml
<Window.Resources>
  <Animation x:Key="FadeIn"
             x:SetterTargetType="Border"
             Duration="0:0:0.5"
             FillMode="Forward"
             Easing="BounceEaseOut">
    <KeyFrame Cue="0%">
      <Setter Property="Opacity" Value="0" />
    </KeyFrame>
    <KeyFrame Cue="100%">
      <Setter Property="Opacity" Value="1" />
    </KeyFrame>
  </Animation>
</Window.Resources>
```

```csharp
protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnAttachedToVisualTree(e);
    var anim = (Animation)Resources["FadeIn"]!;
    await anim.RunAsync(AnimatedBorder);
}
```

## Example 2: Programmatic shake animation with cancellation

```csharp
public async Task ShakeAsync(Control target, CancellationToken ct = default)
{
    var originalX = target.RenderTransform is TranslateTransform t ? t.X : 0;

    var shake = new Animation
    {
        Duration = TimeSpan.FromMilliseconds(500),
        IterationCount = new IterationCount(3),
        PlaybackDirection = PlaybackDirection.Alternate
    };

    shake.Children.Add(new KeyFrame
    {
        Cue = new Cue(0.0),
        Setters = { new Setter(TranslateTransform.XProperty, originalX) }
    });
    shake.Children.Add(new KeyFrame
    {
        Cue = new Cue(1.0),
        Setters = { new Setter(TranslateTransform.XProperty, originalX + 10) }
    });

    await shake.RunAsync(target, ct);
}
```

## Example 3: Composition implicit animation for smooth list reorder

```csharp
public static class CompositionBehaviors
{
    public static readonly AttachedProperty<bool> SmoothOffsetProperty =
        AvaloniaProperty.RegisterAttached<CompositionBehaviors, Visual, bool>("SmoothOffset");

    public static bool GetSmoothOffset(Visual element) =>
        element.GetValue(SmoothOffsetProperty);

    public static void SetSmoothOffset(Visual element, bool value) =>
        element.SetValue(SmoothOffsetProperty, value);

    static CompositionBehaviors()
    {
        SmoothOffsetProperty.Changed.AddClassHandler<Visual>((element, e) =>
        {
            if (e.NewValue is true)
            {
                element.AttachedToVisualTree += (_, _) =>
                {
                    var visual = ElementComposition.GetElementVisual(element);
                    if (visual is null) return;

                    var compositor = visual.Compositor;
                    var anim = compositor.CreateVector3KeyFrameAnimation();
                    anim.Duration = TimeSpan.FromMilliseconds(300);
                    anim.Target = "Offset";
                    anim.InsertExpressionKeyFrame(1f, "this.FinalValue");

                    var implicitCol = compositor.CreateImplicitAnimationCollection();
                    implicitCol["Offset"] = anim;
                    visual.ImplicitAnimations = implicitCol;
                };
            }
        });
    }
}
```

```xml
<Style Selector="ListBoxItem">
  <Setter Property="local:CompositionBehaviors.SmoothOffset" Value="True" />
</Style>
```

## Example 4: Timeline with staggered child animations

```csharp
public async Task StaggerAsync(StackPanel panel, CancellationToken ct)
{
    var delay = TimeSpan.FromMilliseconds(50);
    var totalDelay = TimeSpan.Zero;

    foreach (Control child in panel.Children)
    {
        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Delay = totalDelay,
            FillMode = FillMode.Forward,
            Easing = new CubicEaseOut()
        };
        anim.Children.Add(new KeyFrame
        {
            Cue = new Cue(0.0),
            Setters = { new Setter(Visual.OpacityProperty, 0.0) }
        });
        anim.Children.Add(new KeyFrame
        {
            Cue = new Cue(1.0),
            Setters = { new Setter(Visual.OpacityProperty, 1.0) }
        });

        _ = anim.RunAsync(child, ct); // fire-and-forget staggered
        totalDelay += delay;
    }
}
```

## Example 5: Custom page transition — vertical flip

```csharp
public class FlipTransition : IPageTransition
{
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(0.5);

    public async Task Start(Visual? from, Visual? to, bool forward,
                            CancellationToken ct)
    {
        var tasks = new List<Task>();

        if (from is not null)
        {
            var outAnim = new Animation
            {
                Duration = Duration / 2,
                FillMode = FillMode.Forward,
                Easing = new CubicEaseIn()
            };
            outAnim.Children.Add(new KeyFrame
            {
                Cue = new Cue(0.0),
                Setters = { new Setter(ScaleTransform.ScaleYProperty, 1.0) }
            });
            outAnim.Children.Add(new KeyFrame
            {
                Cue = new Cue(1.0),
                Setters = { new Setter(ScaleTransform.ScaleYProperty, 0.0) }
            });
            tasks.Add(outAnim.RunAsync(from, ct));
        }

        if (to is not null)
        {
            to.IsVisible = true;
            to.Opacity = 0;
            var scale = 0.0;

            var inAnim = new Animation
            {
                Duration = Duration / 2,
                Delay = Duration / 2,
                FillMode = FillMode.Forward,
                Easing = new CubicEaseOut()
            };
            inAnim.Children.Add(new KeyFrame
            {
                Cue = new Cue(0.0),
                Setters = { new Setter(ScaleTransform.ScaleYProperty, 0.0) }
            });
            inAnim.Children.Add(new KeyFrame
            {
                Cue = new Cue(1.0),
                Setters = { new Setter(ScaleTransform.ScaleYProperty, 1.0) }
            });
            tasks.Add(inAnim.RunAsync(to, ct));
        }

        await Task.WhenAll(tasks);

        if (from is not null && !ct.IsCancellationRequested)
            from.IsVisible = false;
    }
}
```

## Example 6: Composite animation — three-property bounce

```xml
<Style Selector="Border.notification">
  <Setter Property="Background" Value="#6a33ff" />
  <Setter Property="CornerRadius" Value="8" />
  <Style.Animations>
    <Animation Duration="0:0:0.4"
               Easing="BackEaseOut"
               FillMode="Forward">
      <KeyFrame Cue="0%">
        <Setter Property="Opacity" Value="0" />
        <Setter Property="ScaleTransform.ScaleX" Value="0.5" />
        <Setter Property="ScaleTransform.ScaleY" Value="0.5" />
      </KeyFrame>
      <KeyFrame Cue="100%">
        <Setter Property="Opacity" Value="1" />
        <Setter Property="ScaleTransform.ScaleX" Value="1" />
        <Setter Property="ScaleTransform.ScaleY" Value="1" />
      </KeyFrame>
    </Animation>
  </Style.Animations>
</Style>
```

## Example 7: Infinite pulsing effect

```xml
<Ellipse Fill="#6a33ff" Width="80" Height="80">
  <Ellipse.Styles>
    <Style Selector="Ellipse">
      <Style.Animations>
        <Animation Duration="0:0:1.5"
                   IterationCount="Infinite"
                   PlaybackDirection="Alternate"
                   Easing="SineEaseInOut">
          <KeyFrame Cue="0%">
            <Setter Property="Opacity" Value="0.3" />
            <Setter Property="ScaleTransform.ScaleX" Value="0.8" />
            <Setter Property="ScaleTransform.ScaleY" Value="0.8" />
          </KeyFrame>
          <KeyFrame Cue="100%">
            <Setter Property="Opacity" Value="1" />
            <Setter Property="ScaleTransform.ScaleX" Value="1.2" />
            <Setter Property="ScaleTransform.ScaleY" Value="1.2" />
          </KeyFrame>
        </Animation>
      </Style.Animations>
    </Style>
  </Ellipse.Styles>
</Ellipse>
```

## Example 8: Composition explicit slide + fade

```csharp
var visual = ElementComposition.GetElementVisual(target);
var compositor = visual.Compositor;

var slide = compositor.CreateVector3KeyFrameAnimation();
slide.Duration = TimeSpan.FromMilliseconds(400);
slide.InsertKeyFrame(0f, new Vector3D(200, 0, 0));
slide.InsertKeyFrame(1f, new Vector3D(0, 0, 0));

var fade = compositor.CreateScalarKeyFrameAnimation();
fade.Duration = TimeSpan.FromMilliseconds(400);
fade.InsertKeyFrame(0f, 0f);
fade.InsertKeyFrame(1f, 1f);

visual.StartAnimation("Offset", slide);
visual.StartAnimation("Opacity", fade);
```
