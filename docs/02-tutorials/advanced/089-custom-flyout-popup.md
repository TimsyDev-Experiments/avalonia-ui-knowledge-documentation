---
tier: advanced
topic: controls
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 089 — Custom Flyout & Popup

**What you'll learn:** Creating custom flyouts and using the Popup control for overlays, custom dropdowns, and interactive floating UI.

**Prerequisites:** [020 — Custom Templated Controls](020-custom-templated-controls.md), [061 — Collection Views](061-collection-views.md)

---

## 1. Flyout vs Popup vs ToolTip

| Control | Trigger | Light Dismiss | Keyboard | Best For |
|---|---|---|---|---|
| `Flyout` (attached) | `Button.Flyout`, `ShowAt()` | Built-in | Automatic | Menus, confirmations, pickers |
| `Popup` (primitive) | Manual `IsOpen` | Optional (`IsLightDismissEnabled`) | Manual | Custom overlays, precision positioning |
| `ToolTip` | Hover | Built-in | N/A | Hover hints |

For most cases, prefer `Flyout`. Use `Popup` when you need custom positioning behavior.

---

## 2. Built-in Flyout

The standard `Flyout` shows any `Content` in a `FlyoutPresenter`:

```xml
<Button Content="More Info">
  <Button.Flyout>
    <Flyout>
      <StackPanel Spacing="8" Padding="12">
        <TextBlock Text="Details" FontWeight="SemiBold" />
        <TextBlock Text="This is additional information." />
      </StackPanel>
    </Flyout>
  </Button.Flyout>
</Button>
```

Show programmatically:

```csharp
var flyout = new Flyout();
flyout.Content = new TextBlock { Text = "Hello" };
flyout.ShowAt(button);
```

---

## 3. Custom Flyout (FlyoutBase)

Derive from `FlyoutBase` and override `CreatePresenter()`:

```csharp
public class ConfirmFlyout : FlyoutBase
{
    public event EventHandler? Confirmed;

    protected override Control CreatePresenter()
    {
        var confirmBtn = new Button { Content = "Confirm" };
        confirmBtn.Click += (s, e) =>
        {
            Confirmed?.Invoke(this, EventArgs.Empty);
            Hide();
        };

        return new FlyoutPresenter
        {
            Content = new StackPanel
            {
                Spacing = 8,
                Padding = 12,
                Children =
                {
                    new TextBlock { Text = "Are you sure?" },
                    confirmBtn
                }
            }
        };
    }
}
```

```xml
<Button Content="Delete">
  <Button.Flyout>
    <local:ConfirmFlyout />
  </Button.Flyout>
</Button>
```

---

## 4. Flyout placement

```xml
<Flyout Placement="Bottom"
        HorizontalOffset="4"
        VerticalOffset="0">
```

| Placement | Behavior |
|---|---|
| `Bottom` | Below target, left-aligned |
| `Top` | Above target |
| `Left` / `Right` | Side-anchored |
| `Center` | Centered over target |
| `AnchorAndGravity` | Precision anchor + gravity |
| `Custom` | With `CustomPopupPlacementCallback` |

---

## 5. Popup control

```xml
<Popup x:Name="MyPopup"
       PlacementTarget="{Binding #ToggleButton}"
       Placement="Bottom"
       IsLightDismissEnabled="True">
  <Border Background="White" Padding="12" CornerRadius="4"
          BoxShadow="0 2 8 0 #40000000">
    <TextBlock Text="Popup content" />
  </Border>
</Popup>
```

```csharp
MyPopup.IsOpen = true;   // show
MyPopup.IsOpen = false;  // hide
```

### Bind IsOpen to ViewModel

```xml
<Popup IsOpen="{Binding IsDropdownOpen}"
       IsLightDismissEnabled="True">
```

---

## 6. Custom popup placement callback

```csharp
myPopup.CustomPopupPlacementCallback = placement =>
{
    placement.Anchor = PopupAnchor.TopRight;
    placement.Gravity = PopupGravity.BottomRight;
    placement.Offset = new Point(8, 0);
};
```

Also works on `Flyout`, `ContextMenu`, and `ToolTip`:

```csharp
ToolTip.SetCustomPopupPlacementCallback(control, placement =>
{
    placement.Offset = new Point(0, -10);
});
```

---

## 7. Events

| Event | FlyoutBase | Popup |
|---|---|---|
| `Opened` | Yes | Yes |
| `Closed` | Yes | Yes |
| `Opening` | Yes (PopupFlyoutBase) | No |
| `Closing` | Yes (PopupFlyoutBase) | No |

---

## See also

- [Custom controls — custom flyout](https://docs.avaloniaui.net/docs/custom-controls/custom-flyout)
- [Popup control reference](https://docs.avaloniaui.net/controls/feedback/popup)
- [Overview of feedback controls](https://docs.avaloniaui.net/controls/feedback)
