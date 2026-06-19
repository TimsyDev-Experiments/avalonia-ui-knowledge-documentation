---
tier: intermediate
topic: keyboard
estimated: 5 min
researched: 2026-06-18
avalonia-version: 12.0.4
quiz-of: 055-keyboard-hotkeys.md
quiz-type: comprehension
---

# 055Q — Keyboard & Hotkeys Quiz

**Scenario:** You are adding keyboard shortcuts to an Avalonia document-editor app. Answer the following questions.

---

## Q1. What is the key behavioral difference between `HotKey` and `KeyBinding`?

**A1.** `HotKey` fires its command regardless of focus (as long as the hosting control is visible). `KeyBinding` only fires when the element that owns the `KeyBindings` collection (or one of its visual descendants) has focus.

---

## Q2. You want Ctrl+Delete to delete the current item, but only when a ListBox has focus. Which approach do you use?

**A2.** A `KeyBinding` on the `ListBox.KeyBindings` collection:

```xml
<ListBox.KeyBindings>
  <KeyBinding Gesture="Ctrl+Delete"
              Command="{Binding DeleteSelectedCommand}" />
</ListBox.KeyBindings>
```

---

## Q3. A `MenuItem` displays a shortcut label. How do you define the shortcut in XAML?

**A3.** Use the `HotKey` attached property:

```xml
<MenuItem Header="_Save" Command="{Binding SaveCommand}"
          HotKey="Ctrl+S" />
```

---

## Q4. When a window has both a Window-level `KeyBinding` for Ctrl+Delete and a ListBox-level `KeyBinding` for Ctrl+Delete, which one fires when the user presses Ctrl+Delete?

**A4.** The `ListBox.KeyBindings` entry fires first (because focus is on the ListBox or its child). The key event walks up the visual tree, checking `KeyBindings` at each level, so the most specific (deepest) binding wins.

---

## Q5. What is the effect of calling `CommandManager.InvalidateRequerySuggested()`?

**A5.** It forces all active `ICommand.CanExecute` methods to re-evaluate immediately, updating the enabled/disabled state of controls bound to those commands.

---

## Q6. How do you add a KeyGesture in code (C#) for the shortcut Ctrl+Shift+Z?

**A6.**

```csharp
var gesture = new KeyGesture(Key.Z,
    KeyModifiers.Control | KeyModifiers.Shift);

var binding = new KeyBinding
{
    Gesture = gesture,
    Command = myCommand,
};

myControl.KeyBindings.Add(binding);
```

---

## Q7. True or False: A `KeyBinding` added to a `StackPanel`'s `KeyBindings` collection will fire even when a `Button` inside the panel has focus.

**A7.** True. The `StackPanel` is an ancestor of the focused `Button`, so the key event walks up from the `Button` through the `StackPanel`, where the `KeyBinding` matches.

---

## Q8. On macOS, users expect Cmd+S for Save. Should you use `Gesture="Cmd+S"` and does it work without additional logic?

**A8.** Yes, use `Gesture="Cmd+S"` or `Gesture="Ctrl+S"` (or both). `Cmd` maps to `KeyModifiers.Meta`. On macOS, `Cmd+S` uses the native modifier key; `Ctrl+S` also works but deviates from the platform convention. For a polished app, include both variants.

---

## Q9. An access key (`_File`) requires which modifier key on Windows?

**A9.** Alt. The user presses Alt+F to activate the File menu.

---

## Q10. What happens when `CanExecute` returns `false` for a command bound to a `KeyBinding`?

**A10.** The `KeyBinding` does nothing — the command is not executed. The shortcut is effectively disabled until `CanExecute` returns `true` again.

---

## Q11. Describe the Rusty-kettle problem: You want to automatically save a document with Ctrl+S after a 5-minute idle timer, but `CanExecute` depends on `HasChanges` which is `false` right after a save.

**A11.** After `SaveCommand` executes, `CanExecute` should return `false` (no changes to save). The `KeyBinding` respects this — pressing Ctrl+S after a save does nothing until `HasChanges` becomes `true` again. The timer can call `CommandManager.InvalidateRequerySuggested()` after setting `HasChanges = true`.

---

## Q12. How would you scope a `KeyBinding` to fire only when the window is in a particular editing mode (e.g., "Insert Mode" vs "Command Mode")?

**A12.** Bind the `KeyBinding.Command` to a command whose `CanExecute` depends on a mode property. When the mode changes, call `CommandManager.InvalidateRequerySuggested()` to re-evaluate all bindings. Alternatively, remove and re-add `KeyBinding` entries dynamically when the mode changes.

---

## See Also

- [055 — Keyboard & Hotkeys (core tutorial)](055-keyboard-hotkeys.md)
- [055V — Keyboard & Hotkeys (verbose companion)](055-keyboard-hotkeys-verbose.md)
- [055E — Keyboard & Hotkeys (examples)](055-keyboard-hotkeys-examples.md)
