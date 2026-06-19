---
title: Quiz
topic: 008-focus-keyboard-navigation
type: quiz
---

# Quiz: Focus & Keyboard Navigation

```quiz
Q: What does KeyboardNavigation.TabNavigation="Cycle" do?
A. After the last focusable child, Tab wraps back to the first child within the same scope (correct) || Cycle mode causes Tab to wrap from the last element back to the first, creating a circular tab order within the scope.
B. Tab cycles through all open windows in the application
C. Tab cycles between focus scopes in the window
D. Tab skips every other focusable element

Explanation: Cycle mode wraps tab focus from the last child back to the first, creating a circular navigation loop within the scope. This is useful for toolbars, palette windows, and grouped controls.
```

```quiz
Q: Which TabNavigation mode should you use to prevent Tab from leaving a group of controls (e.g., an editable list)?
A. Local
B. Once
C. Contained (correct) || Contained mode traps Tab within the scope — focus circulates among children and never leaves the group. This is ideal for modal sub-panels, editable lists, and grid cells.
D. Cycle

Explanation: Contained mode is specifically designed for situations where Tab should never leave the focus scope. The user can tab through all elements inside but cannot tab out — useful for list editors and modal panels.
```

```quiz
Q: In the Avalonia key binding priority system, which binding has the highest precedence?
A. Application.KeyBindings
B. Window.KeyBindings
C. The focused control's own KeyBindings (correct) || Key bindings are evaluated in order: focused control first, then parent chain, then Window, then Application. If a handler sets e.Handled = true, propagation stops.
D. System-level hotkeys

Explanation: The focused control's KeyBindings are evaluated first. If they handle the event (e.Handled = true), the event does not propagate to the Window or Application level. This allows focused controls to override global shortcuts.
```

```quiz
Q: What is the "roving tab stop" pattern used for in toolbars?
A. Making all toolbar buttons unfocusable
B. Making only the active tool button a tab stop, while arrow keys navigate between tools (correct) || In a toolbar, only the currently active tool has IsTabStop=true. When the user tabs into the toolbar, focus goes to the active tool. Arrow keys move between tools. Tab leaves the toolbar.
C. Making toolbar buttons focus in reverse tab order
D. Making toolbar buttons always focused in a circular pattern

Explanation: The roving tab stop pattern ensures that a toolbar appears as a single tab stop. Once focused, arrow keys navigate between tools. Only the active tool is a tab stop, keeping tab navigation efficient.
```

```quiz
Q: How does FocusNavigation.MoveFocusNext differ from simply calling Focus() on the next element?
A. MoveFocusNext respects the focus scope's TabNavigation mode and TabIndex ordering (correct) || MoveFocusNext uses the FocusManager to determine the next focusable element based on the current scope's navigation rules and TabIndex values, rather than hard-coding a specific target.
B. MoveFocusNext always focuses the window's first element
C. MoveFocusNext only works on TextBox controls
D. There is no difference — they are equivalent

Explanation: FocusNavigation.MoveFocusNext delegates to FocusManager.MoveFocus, which evaluates the scope's navigation mode and element TabIndex to determine the correct next element. This is more robust than calling Focus() on a hard-coded element.
```

```quiz
Q: What is the effect of setting KeyboardNavigation.IsTabStop="False" on a control?
A. The control can never receive focus, even programmatically
B. The control is skipped during Tab navigation but can still receive focus via code or mouse click (correct) || IsTabStop="False" removes the control from the Tab sequence but it remains focusable programmatically (Focus()) or via mouse interaction.
C. The control becomes invisible
D. The control's TabIndex is ignored but it still appears in the tab order

Explanation: IsTabStop controls participation in Tab-key navigation only. A control with IsTabStop="False" cannot be reached via Tab/Shift+Tab but can still be focused programmatically (Focus()) or by clicking on it with the mouse.
```
