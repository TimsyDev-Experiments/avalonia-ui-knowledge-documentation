---
tier: advanced
topic: controls
estimated: 30 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 089 — Custom Flyout & Popup — Verbose

**Prerequisites:** [089-core](089-custom-flyout-popup.md)

---

## 1. FlyoutBase architecture

```
FlyoutBase (abstract)
├── CreatePresenter() : Control       ← override this
├── ShowAt(Control)                   ← show anchored to a control
├── Hide()
├── Opened / Closed events
├── IsOpen property
└── Target property (the anchor)
    │
    └── PopupFlyoutBase
        ├── Placement, HorizontalOffset, VerticalOffset
        ├── OverlayDismissEventPassThrough
        ├── OverlayInputPassThroughElement
        ├── CustomPopupPlacementCallback
        └── ShowMode
            │
            ├── Flyout (concrete)
            │   ├── Content
            │   ├── ContentTemplate
            │   ├── FlyoutPresenterTheme
            │   └── FlyoutPresenterClasses
            │
            └── MenuFlyout (concrete)
```

`FlyoutBase` gives you a lightweight overlay. `PopupFlyoutBase` adds the popup placement system. `Flyout` adds `Content`/`ContentTemplate`.

---

## 2. Custom flyout with StyledProperty

```csharp
public class ImageFlyout : FlyoutBase
{
    public static readonly StyledProperty<IImage> ImageProperty =
        AvaloniaProperty.Register<ImageFlyout, IImage>(nameof(Image));

    [Content]
    public IImage Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    protected override Control CreatePresenter()
    {
        return new FlyoutPresenter
        {
            Content = new Image
            {
                [!Image.SourceProperty] = this[!ImageProperty]
            }
        };
    }
}
```

Using the indexer `this[!ImageProperty]` creates a one-way binding from the flyout property to the `Image.SourceProperty`. This keeps the presenter in sync when the property changes.

---

## 3. Flyout presenter styling

Override the presenter theme:

```csharp
var flyout = new Flyout
{
    Content = new TextBlock { Text = "Hello" },
    FlyoutPresenterTheme = new ControlTheme(typeof(FlyoutPresenter))
    {
        Setters =
        {
            new Setter(FlyoutPresenter.MinWidthProperty, 300.0),
            new Setter(FlyoutPresenter.MaxWidthProperty, 500.0),
            new Setter(FlyoutPresenter.PaddingProperty, new Thickness(16)),
        }
    }
};
```

Or in XAML styles:

```xml
<Style Selector="FlyoutPresenter.myCustom">
  <Setter Property="Background" Value="LightYellow" />
  <Setter Property="CornerRadius" Value="12" />
  <Setter Property="Padding" Value="16" />
</Style>
```

Apply via `FlyoutPresenterClasses`:

```csharp
flyout.FlyoutPresenterClasses.Add("myCustom");
```

---

## 4. Popup overlay behavior

The overlay is a semi-transparent layer behind the popup that intercepts pointer events. Controls behind the overlay cannot be clicked. Use `OverlayInputPassThroughElement` to allow clicks through:

```csharp
popup.OverlayInputPassThroughElement = someButton;
```

When `OverlayDismissEventPassThrough` is `true`, the click that dismisses the popup also passes through to the element underneath.

---

## 5. ShowMode options

| ShowMode | Behavior |
|---|---|
| `Standard` (default) | Opens immediately, stays open until dismissed |
| `Transient` | Opens on pointer press, closes on release |
| `TransientWithDismissOnPointerMoveAway` | Like Transient but also closes when pointer moves far enough |

Useful for in-place dropdowns and tooltips.

---

## 6. Popup placement details

`AnchorAndGravity` placement uses two separate enums:

```xml
<Popup Placement="AnchorAndGravity"
       PlacementAnchor="BottomRight"
       PlacementGravity="TopRight" />
```

| Enum | Values |
|---|---|
| `PopupAnchor` | `TopLeft`, `Top`, `TopRight`, `Left`, `Center`, `Right`, `BottomLeft`, `Bottom`, `BottomRight` |
| `PopupGravity` | `TopLeft`, `Top`, `TopRight`, `Left`, `Center`, `Right`, `BottomLeft`, `Bottom`, `BottomRight` |

Anchor is where on the target the popup attaches. Gravity is which direction the popup expands from the anchor.

---

## 7. PlacementConstraintAdjustment

Controls how the popup repositions when it would extend beyond the screen edge:

| Value | Behavior |
|---|---|
| `None` | No adjustment |
| `SlideX` | Slide horizontally to stay on screen |
| `SlideY` | Slide vertically |
| `FlipX` | Flip to the opposite horizontal side |
| `FlipY` | Flip to the opposite vertical side |
| `All` (default) | Apply all adjustments |

---

## 8. Complete custom dropdown example

```csharp
public class ColorPickerFlyout : FlyoutBase
{
    public event EventHandler<Color>? ColorSelected;

    protected override Control CreatePresenter()
    {
        var listBox = new ListBox
        {
            Items = new List<string> { "Red", "Green", "Blue", "Yellow", "Purple" },
            SelectedIndex = 0
        };

        listBox.SelectionChanged += (s, e) =>
        {
            if (listBox.SelectedItem is string color)
            {
                ColorSelected?.Invoke(this, color.ToLower() switch
                {
                    "red" => Colors.Red,
                    "green" => Colors.Green,
                    "blue" => Colors.Blue,
                    "yellow" => Colors.Yellow,
                    "purple" => Colors.Purple,
                    _ => Colors.Black
                });
                Hide();
            }
        };

        return new FlyoutPresenter { Content = listBox };
    }
}
```
