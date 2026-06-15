---
tier: advanced
topic: accessibility
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 026-accessibility-automation.md
---

# 026V — Accessibility & Automation: An In-Depth Companion

**Why this exists.** The original tutorial covers `AutomationProperties`, focus order, custom control accessibility, keyboard handling, live regions, and DevTools validation. This companion explains how the automation tree is built, what screen readers actually consume on each platform, why `Name` takes priority over `Content`, and how to avoid the accessibility regressions that commonly appear during control customization.

**Read this alongside:** [026 — Accessibility & Automation](026-accessibility-automation.md)

---

## 1. The automation tree

### What the automation tree is

Avalonia builds a parallel tree to the visual tree called the **automation tree**. Each `Control` in the visual tree maps to an `AutomationNode` that exposes:
- A control type (Button, TextBox, Slider, etc.)
- The `AutomationProperties` values (Name, HelpText, etc.)
- The control's bounding rectangle
- The control's children (nested automation nodes)

Screen readers (NVDA, JAWS, Narrator on Windows; VoiceOver on macOS; TalkBack on Android) consume this tree. They do NOT read the visual tree directly.

### How the automation tree is populated

Avalonia's platform-specific automation providers walk the visual tree and create `AutomationNode` entries for every element that has `AutomationProperties.AccessibilityView` set to `Control` (the default for interactive controls) or `Content`.

| `AccessibilityView` value | Included in automation tree |
|---|---|
| `Control` | Yes — the element is an interactive control |
| `Content` | Yes — the element contains readable content |
| `Raw` | Yes — included but not typically presented |

If `AccessibilityView` is unset, the default is `Control` for `InputElement` subclasses (buttons, text boxes) and `Content` for `TextBlock`, `Image`, etc.

### Why invisible elements still appear

An element with `IsVisible = false` still exists in the automation tree but has an `IsOffscreen` property set to `true`. Screen readers may or may not skip it depending on their settings. Call `AutomationProperties.SetAccessibilityView(element, AccessibilityView.Raw)` to hide an element entirely.

---

## 2. `AutomationProperties.Name` — the most important property

```xml
<Button Content="Save"
        AutomationProperties.Name="Save the current document" />
```

### What the screen reader does with `Name`

On Windows (UIA), the screen reader reads the `Name` property of the automation element. If `Name` is not set, the provider falls back to:
1. `Content` (the button's text content, if any)
2. `HelpText` (if `Name` is also empty)
3. The control's type name ("Button")

Always set `Name` explicitly for interactive controls. The fallback chain produces generic announcements like "Button Button" or "Save Button" — functional but not helpful.

### Why `Name` differs from `Content`

- `Content` is what the control **visually displays** (e.g., a floppy-disk icon + "Save").
- `Name` is what the screen reader **announces** (e.g., "Save the current document").

They can differ. An icon-only button (no text) must have `Name` set because there is no `Content` fallback.

### The `Name` should be localized

```csharp
AutomationProperties.SetName(saveButton, LocalizedStrings.SaveButton_A11yName);
```

If your app supports multiple languages, `AutomationProperties.Name` must be in the user's language, just like visual text. Static unlocalized strings in XAML create an accessibility barrier for non-English users.

---

## 3. Focus order and `TabIndex`

```xml
<TextBox Name="NameInput" TabIndex="0" />
<TextBox Name="EmailInput" TabIndex="1" />
<Button Content="Submit" TabIndex="2" />
```

### How `TabIndex` is evaluated

Avalonia sorts focusable elements by:
1. `TabIndex` (ascending)
2. Z-order (declaration order in XAML, or actual visual Z-order)

Elements with `TabIndex = int.MaxValue` (the default) are visited after all explicitly-indexed elements. This follows the W3C ARIA authoring practices for keyboard navigation.

### Negative `TabIndex`

Setting `TabIndex = -1` makes the element programmatically focusable (via `Focus()`) but **skips** it in tab navigation. Use this for elements that should be reachable only via other keyboard interactions (arrow keys within a list, for example).

### `IsTabStop = false`

Setting `IsTabStop = false` on a `TextBox` prevents it from receiving keyboard focus via Tab, even if it has a `TabIndex`. The element can still receive focus programmatically or by clicking.

### `KeyboardNavigation.TabNavigation` modes

| Mode | Behavior |
|---|---|
| `Continue` (default) | Tab moves out of the container to the next element in the window |
| `Once` | Tab enters the container, then moves to the first item. Next tab exits the container |
| `Cycle` | Tab cycles within the container and never exits |
| `Local` | Tab moves inside the container only. If at the last item, Tab wraps to the first |
| `None` | Tab skips all elements in the container |

Use `Cycle` for toolbars, tab controls, and list boxes where the user should navigate within the control before moving on.

### Focus visual

Avalonia renders a focus rectangle (usually a dashed or colored border) on the focused element. If you override the control theme, ensure the `:focus` pseudo-class triggers a visual focus indicator. Without it, keyboard users cannot tell which element is active.

---

## 4. Making custom controls accessible

```csharp
AutomationProperties.SetName(this, $"Rating: {Value} of {Maximum} stars");
```

### Updating `Name` dynamically

The `OnPropertyChanged` handler updates the automation name whenever `Value` changes. This is essential because screen readers cache the automation node's properties. Without the update, the reader announces "Rating: 0 of 5 stars" even after the user changes the rating to 3.

### The `AutomationPeer` pattern

Avalonia 12 supports a `AutomationPeer` class for advanced automation scenarios. Instead of setting `AutomationProperties` via attached properties, you can override the peer:

```csharp
public class RatingControlAutomationPeer : ControlAutomationPeer
{
    private readonly RatingControl _owner;
    public RatingControlAutomationPeer(RatingControl owner) : base(owner) => _owner = owner;

    protected override string GetNameCore() =>
        $"Rating: {_owner.Value} of {_owner.Maximum} stars";

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Slider;
}
```

Override `OnCreateAutomationPeer()` in the control to return your peer. This gives you full control over what the automation tree exposes.

### Required vs. optional control patterns

| Pattern | When to implement |
|---|---|
| `IInvokeProvider` | Button-like controls (click to act) |
| `ISelectionProvider` | Controls that maintain a selection |
| `IRangeValueProvider` | Sliders, progress bars, numeric steppers |
| `IToggleProvider` | Checkboxes, toggle switches |

Avalonia maps common controls to these patterns automatically. For custom controls, implement the interface on your `AutomationPeer`.

---

## 5. Keyboard handling contracts

```csharp
case Key.Right:
case Key.Up:
    Value = Math.Min(Value + 1, Maximum);
    e.Handled = true;
    break;
```

### Why mark `e.Handled = true`

Setting `Handled = true` prevents the event from bubbling further up the visual tree. Without it, the `KeyDown` event would also reach the parent container, which might respond to arrow keys for scrolling or focus navigation.

### The expected keyboard contract

| Control type | Expected keys |
|---|---|
| Button / Clickable | `Enter` or `Space` |
| Slider / Range | `Left`/`Right`, `Up`/`Down`, `Home`/`End`, `PageUp`/`PageDown` |
| List / Tree | `Up`/`Down`, `Home`/`End`, character search |
| Editable text | All text input keys plus `Enter`, `Tab`, arrows |

Deviating from these expectations confuses users. If your `RatingControl` uses `Up`/`Down` to change value, it matches the slider contract, which is appropriate.

### Focusability

A control that handles keyboard input must be focusable. Set `Focusable = true` in the constructor (it is true by default for `TemplatedControl` but false for `Control`).

---

## 6. Live regions

```csharp
AutomationProperties.SetLiveSetting(myStatusText, AutomationLiveSetting.Polite);
```

### What live regions do

A live region tells the screen reader to monitor an element's `Name` or `Text` for changes and announce them automatically. Without live regions, the user must re-navigate to the element to hear new content.

### `Polite` vs `Assertive`

| `AutomationLiveSetting` | Behavior |
|---|---|
| `Off` | No automatic announcement |
| `Polite` | Announce the change when the screen reader is idle (after finishing current speech). Use for status messages, progress updates, and non-critical notifications. |
| `Assertive` | Announce immediately, interrupting current speech. Use for errors, warnings, and time-sensitive information. |

### What triggers an announcement

Changing the element's `Text` (for `TextBlock`/`Label`) or `AutomationProperties.Name` (for any element) triggers the live region. The screen reader compares the new value to the previously announced value and speaks the difference.

### Live region on a `TextBlock`

```xml
<TextBlock Name="StatusText"
           AutomationProperties.LiveSetting="Assertive"
           AutomationProperties.Name="{Binding #StatusText.Text}" />
```

The binding to `Name` ensures that when `Text` changes, the automation name also changes, which triggers the live region. Without this binding, the screen reader sees no change in the automation properties and stays silent.

---

## 7. Testing accessibility with code

```csharp
var name = AutomationProperties.GetName(myButton);
Assert.Equal("Save the current document", name);
```

### What this test verifies

This test verifies that the `AutomationProperties.Name` attached property was set to the expected value. It does **not** verify that the automation tree is correct or that a screen reader would announce the right thing. For full coverage:

```csharp
// Verify the automation peer produces the correct name
var peer = ControlAutomationPeer.CreatePeerForElement(myButton);
Assert.Equal("Save the current document", peer.GetName());
```

### DevTools inspection (F12 → Accessibility tab)

The DevTools accessibility tab shows:
- The full automation tree (not just the visual tree)
- Each node's `Name`, `HelpText`, `ControlType`, and bounding rect
- Focus order as it would appear to a screen reader

Verify these in the DevTools for every page/view in your app.

---

## 8. Common accessibility regressions

### Regression 1: Custom template loses automation

When you replace the `ControlTemplate` of a `Button`, the template parts (the `ContentPresenter`, the border) change. If your template does not include a `ContentPresenter` with the `PART_ContentPresenter` name, the `ButtonAutomationPeer` cannot find the content, and the automation name falls back to empty.

Fix: Keep `PART_ContentPresenter` in custom button templates, or override the automation peer.

### Regression 2: Invisible focus indicator

Custom controls often skip the focus visual in their theme:

```xml
<Style Selector="^:focus">
  <Setter Property="Background" Value="{DynamicResource SystemControlHighlightAccentBrush}" />
</Style>
```

Always define a visible focus state in your `ControlTheme`.

### Regression 3: Icon-only controls without names

An icon-only `Button` or `MenuItem` with no `AutomationProperties.Name` is invisible to screen readers. Always add a name:

```xml
<Button>
  <Image Source="/Icons/delete.png" />
  <ToolTip.Tip>Delete</ToolTip.Tip>
  <!-- AutomationProperties.Name is the ONLY way screen readers see this -->
  <AutomationProperties.Name>Delete selected item</AutomationProperties.Name>
</Button>
```

### Regression 4: Dynamic content without live regions

A `TextBlock` that shows "Uploading..." → "Complete" has no live region. Screen reader users hear nothing. Add `AutomationProperties.LiveSetting="Polite"` to status text blocks.

---

## Cross-links

- [020 — Custom Templated Controls](020-custom-templated-controls.md) — templated control patterns that affect automation
- [026 — Accessibility & Automation](026-accessibility-automation.md) — complete walkthrough of automation properties and patterns
- [026E — Accessibility & Automation (examples)](026-accessibility-automation-examples.md)
- [Avalonia Docs: Accessibility](https://docs.avaloniaui.net/docs/accessibility/)
