---
tier: advanced
topic: accessibility
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 026-accessibility-automation.md
---

# Quiz — Accessibility & Automation

```quiz
Q: Which AutomationProperties member is the primary identifier announced by screen readers when a control receives focus?
A. AutomationProperties.HelpText || HelpText is a supplementary description, not the primary identifier.
B. AutomationProperties.Name (correct) || Name is the primary text that screen readers announce for a focused element.
C. AutomationProperties.ItemType || ItemType announces the kind of item (e.g., "menu item"), not the item's identity.
D. AutomationProperties.AccessibilityView || AccessibilityView controls visibility in the automation tree; it is not an announced string.
```

```quiz
Q: What does KeyboardNavigation.TabNavigation="Cycle" do on a container?
A. It restarts tab navigation from the first control after the last one is reached, keeping focus inside the container. (correct) || Cycle traps Tab focus within the container, wrapping from the last child back to the first and vice versa.
B. It disables all tab navigation inside the container. || Disabling is done with None, not Cycle.
C. It reverses the tab order of child controls. || Cycle does not change order; it only affects boundary wrapping.
D. It allows one tab stop per container, skipping child controls. || That describes Once mode, not Cycle.
```

```quiz
Q: In the RatingControl example, how does the control announce the current rating value to screen readers?
A. By binding AutomationProperties.Name to the Value property in XAML. || The tutorial uses code-behind to build a formatted string combining Value and Maximum.
B. By calling AutomationProperties.SetName(this, $"Rating: {Value} of {Maximum} stars") inside OnPropertyChanged when Value changes. (correct) || SetName updates the automation name dynamically, ensuring screen readers announce the correct rating.
C. By overriding ToString() and returning the rating string. || ToString does not affect automation properties.
D. By setting AutomationProperties.HelpText with a static resource. || HelpText is for tooltip-like descriptions, not dynamic value announcements.
```

```quiz
Q: What is the effect of AutomationProperties.LiveSetting = AutomationLiveSetting.Polite on a status text element?
A. The screen reader interrupts current speech and announces the change immediately. || That is Assertive, not Polite.
B. The screen reader waits until it is idle before announcing the content change. (correct) || Polite means the change is announced when the screen reader is not in the middle of other speech, avoiding abrupt interruptions.
C. The element is removed from the accessibility tree. || LiveSetting does not affect tree inclusion; it controls announcement timing.
D. The element is automatically focused when its text changes. || Live regions do not change focus; they only trigger announcements.
```

```quiz
Q: Which DevTools tab should you open to inspect automation properties at runtime?
A. Visual Tree tab || The Visual Tree shows the element hierarchy but not automation-specific metadata.
B. Layout tab || Layout tab shows measure/arrange details, not accessibility information.
C. Accessibility tab (correct) || The tutorial directs users to F12 -> Accessibility tab to "inspect automation properties, focus order, and tree structure."
D. Resources tab || Resources tab displays merged resource dictionaries, not accessibility data.
```
