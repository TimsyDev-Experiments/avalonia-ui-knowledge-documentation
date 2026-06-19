---
tier: advanced
topic: controls
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 089 — Custom Flyout & Popup — Quiz

**Prerequisites:** [089-core](089-custom-flyout-popup.md)

---

### Q1: Which base class must you derive from to create a custom flyout, and which method must you override?

<details>
<summary>Answer</summary>

`FlyoutBase`. Override `CreatePresenter()` which returns the root `Control` to display in the flyout popup.

</details>

---

### Q2: What is the difference between `IsLightDismissEnabled` on a Popup versus the default behavior of a Flyout?

<details>
<summary>Answer</summary>

`Popup.IsLightDismissEnabled` is `false` by default and must be set to `true` for click-outside-to-close. Flyout has light dismiss built in and enabled by default.

</details>

---

### Q3: True or False: The `Flyout` class supports both `Content` and `ContentTemplate` properties.

<details>
<summary>Answer</summary>

True. `Flyout` (not `FlyoutBase`) provides `Content`, `ContentTemplate`, `FlyoutPresenterTheme`, and `FlyoutPresenterClasses`.

</details>

---

### Q4: You want a flyout to appear to the right of its target, aligned to the top. What `Placement` mode and property values achieve this?

<details>
<summary>Answer</summary>

```xml
<Flyout Placement="AnchorAndGravity"
        PlacementAnchor="TopRight"
        PlacementGravity="TopLeft" />
```

Or simply `Placement="Right"` for the basic right-aligned top position.

</details>

---

### Q5: Write a custom flyout class that displays a slider for numeric input and emits the selected value via an event.

<details>
<summary>Answer</summary>

```csharp
public class SliderFlyout : FlyoutBase
{
    public event EventHandler<double>? ValueSelected;

    protected override Control CreatePresenter()
    {
        var slider = new Slider { Minimum = 0, Maximum = 100, Width = 200 };
        var ok = new Button { Content = "OK" };
        ok.Click += (s, e) =>
        {
            ValueSelected?.Invoke(this, slider.Value);
            Hide();
        };
        return new FlyoutPresenter
        {
            Content = new StackPanel
            { Spacing = 8, Padding = 12, Children = { slider, ok } }
        };
    }
}
```

</details>

---

### Q6: How can you prevent clicks from passing through the overlay when a Popup is open?

<details>
<summary>Answer</summary>

By default, the overlay blocks clicks. To allow clicks through to a specific element, set `OverlayInputPassThroughElement`. To allow the dismiss click to pass through, set `OverlayDismissEventPassThrough = true`.

</details>

---

### Q7: What property on `Flyout` lets you apply a custom CSS-like class to the presenter for styling?

<details>
<summary>Answer</summary>

`FlyoutPresenterClasses` (an `Avalonia.Controls.Classes` collection). Add style classes via code, then target them with `Style Selector="FlyoutPresenter.myClass"` in XAML.

</details>

---

### Q8: How do you programmatically show a custom flyout anchored to a button?

<details>
<summary>Answer</summary>

```csharp
var flyout = new MyCustomFlyout();
flyout.ShowAt(myButton);
```

</details>
