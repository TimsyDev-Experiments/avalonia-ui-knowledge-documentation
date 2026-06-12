---
tier: advanced
topic: accessibility
estimated: 8 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 026 — Accessibility & Automation

**What you'll learn:** Set automation properties for screen readers, manage focus order, and make custom controls accessible.

**Prerequisites:** [020 — Custom Templated Controls](020-custom-templated-controls.md)

---

## 1. AutomationProperties attached properties

```xml
<Button Content="Save"
        AutomationProperties.Name="Save the current document"
        AutomationProperties.HelpText="Saves all changes to the active file"
        AutomationProperties.AccessibilityView="Control" />
```

Common `AutomationProperties`:

| Property | Purpose |
|---|---|
| `Name` | Primary identifier read by screen readers |
| `HelpText` | Descriptive tooltip for assistive tech |
| `AccessibilityView` | `Control`, `Content`, or `Raw` |
| `ItemType` | Announces the type of item (e.g., "menu item") |
| `IsRequiredForForm` | Marks a field as required |

---

## 2. Focus order (TabIndex and TabNavigation)

```xml
<StackPanel>
  <TextBox Name="NameInput"
           TabIndex="0" />
  <TextBox Name="EmailInput"
           TabIndex="1" />
  <Button Content="Submit"
          TabIndex="2" />
</StackPanel>
```

For containers:

```xml
<StackPanel KeyboardNavigation.TabNavigation="Cycle">
  <!-- Tab cycles within the panel instead of exiting it -->
</StackPanel>
```

Tab navigation modes: `Continue`, `Once`, `Cycle`, `None`, `Local`.

---

## 3. Making a custom control accessible

```csharp
public class RatingControl : TemplatedControl
{
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Set automation properties on template parts
        if (e.NameScope.Find<Button>("PartIncrement") is { } inc)
        {
            AutomationProperties.SetName(inc, "Increase rating");
            AutomationProperties.SetHelpText(inc, "Add one star to the rating");
        }

        if (e.NameScope.Find<Button>("PartDecrement") is { } dec)
        {
            AutomationProperties.SetName(dec, "Decrease rating");
            AutomationProperties.SetHelpText(dec, "Remove one star from the rating");
        }
    }

    // Report current value for accessibility
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            AutomationProperties.SetName(this, $"Rating: {Value} of {Maximum} stars");
        }
    }
}
```

---

## 4. Keyboard handling for custom controls

```csharp
protected override void OnKeyDown(KeyEventArgs e)
{
    base.OnKeyDown(e);

    switch (e.Key)
    {
        case Key.Right:
        case Key.Up:
            Value = Math.Min(Value + 1, Maximum);
            e.Handled = true;
            break;

        case Key.Left:
        case Key.Down:
            Value = Math.Max(Value - 1, 0);
            e.Handled = true;
            break;

        case Key.Home:
            Value = 0;
            e.Handled = true;
            break;

        case Key.End:
            Value = Maximum;
            e.Handled = true;
            break;
    }
}
```

---

## 5. Testing accessibility

Avalonia DevTools (F12) includes an accessibility tree viewer:

1. Run the app and press F12
2. Navigate to the **Accessibility** tab
3. Inspect automation properties, focus order, and tree structure
4. Verify that all interactive elements have meaningful `AutomationProperties.Name`

For automated testing:

```csharp
var name = AutomationProperties.GetName(myButton);
Assert.Equal("Save the current document", name);
```

---

## 6. Live regions (dynamic content announcements)

```csharp
AutomationProperties.SetLiveSetting(myStatusText, AutomationLiveSetting.Polite);

// When the text changes, screen readers will announce it
myStatusText.Text = "Document saved successfully";
```

`AutomationLiveSetting` options:
- `Off` — No announcements
- `Polite` — Announce when idle
- `Assertive` — Announce immediately

---

## Key Takeaways

- Always set `AutomationProperties.Name` on interactive elements
- Set `AutomationProperties.HelpText` for complex interactions
- Control `TabIndex` and `KeyboardNavigation.TabNavigation` for focus order
- Custom controls must handle keyboard navigation and report state via automation
- Use DevTools (F12) → Accessibility tab to validate
- Live regions announce dynamic changes to screen readers

---

## See Also

- [023 — Accessibility & Automation](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/23-accessibility-and-automation.md) (plugin ref)
- [060 — Automation Properties & Attached Behavior Patterns](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/60-automation-properties-and-attached-behavior-patterns.md) (plugin ref)
- [Avalonia Docs: Accessibility](https://docs.avaloniaui.net/docs/accessibility/)
