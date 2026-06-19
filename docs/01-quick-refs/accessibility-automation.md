---
topic: accessibility
estimated: 3 min read
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Q10 — Accessibility & Automation IDs

## AutomationProperties attached properties

Use the `AutomationProperties` class on any control:

```xml
<Button Content="Save"
        AutomationProperties.Name="Save document"
        AutomationProperties.HelpText="Persists all changes to disk"
        AutomationProperties.AcceleratorKey="Ctrl+S"
        AutomationProperties.AccessibilityView="Content" />
```

| Property | Purpose |
|---|---|
| `Name` | Primary identifier for screen readers |
| `HelpText` | Supplementary description |
| `AcceleratorKey` | Keyboard shortcut hint |
| `AccessibilityView` | `Content` (default), `Control`, `Raw` — controls tree visibility |
| `ItemType` | Custom control type string |
| `IsRequiredForForm` | Marks required fields |
| `LabeledBy` | Reference to a label element |
| `AutomationId` | Stable identifier for UI testing |

## AutomationId for testing

Set a stable, unique `AutomationId` on any element you test:

```xml
<Button Content="Login" AutomationProperties.AutomationId="LoginButton" />
<TextBox AutomationProperties.AutomationId="UsernameInput" />
```

Use in headless tests:

```csharp
var loginBtn = window.GetControl<Button>("LoginButton");
await loginBtn.SimulateClickAsync();
```

## Focus order

```xml
<StackPanel>
  <TextBox Name="Username" KeyboardNavigation.TabIndex="0" />
  <TextBox Name="Password" KeyboardNavigation.TabIndex="1" />
  <Button Content="Login" KeyboardNavigation.TabIndex="2" />
</StackPanel>
```

| Property | Purpose |
|---|---|
| `KeyboardNavigation.TabIndex` | Explicit tab order |
| `KeyboardNavigation.TabNavigation` | `Cycle`, `Once`, `Local`, `Contained` |
| `KeyboardNavigation.IsTabStop` | `false` to skip focus |
| `Focusable` | `false` to prevent focus entirely |

## Screen reader best practices

- Set `AutomationProperties.Name` on all interactive controls.
- Use `LabeledBy` to associate labels with inputs:

```xml
<TextBlock x:Name="EmailLabel" Text="Email" />
<TextBox AutomationProperties.LabeledBy="{Binding #EmailLabel}" />
```

- Mark decorative images with `AutomationProperties.AccessibilityView="Raw"`.
- Use `HelpText` for complex controls (sliders, custom pickers).
