---
tier: intermediate
topic: focus
estimated: 5-8 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 054-focus-management.md
---

# Quiz — Focus Management

```quiz
Q: How do you give keyboard focus to a Button in Avalonia?
A. button.Focus() (correct) || The Focus() method on any InputElement moves keyboard focus to that element. It returns false if the element is not visible or focusable.
B. FocusManager.SetFocus(button) || There is no static SetFocus method. Use button.Focus() or focusManager.Focus(button).
C. button.IsFocused = true || IsFocused is a read-only property — you cannot set it directly.
D. Keyboard.Focus(button) || Avalonia does not have a Keyboard.Focus method. Use control.Focus() instead.
Explanation: control.Focus() is the correct way to programmatically give focus to any InputElement.
```

```quiz
Q: You have three TextBoxes in a StackPanel. What determines the order in which Tab key moves focus between them?
A. Their XAML declaration order (the visual tree) (correct) || By default, Tab navigates elements in visual-tree order. TabIndex can override this.
B. Their Name property, sorted alphabetically || Names are not used for tab ordering.
C. The order of their Loaded events || Load order is not a factor.
D. Their ZIndex values || ZIndex controls rendering order, not focus navigation.
Explanation: Default tab order follows visual-tree order. Use TabIndex to specify a custom order.
```

```quiz
Q: Which event is fired before a control receives focus and can be cancelled?
A. GotFocusEvent || GotFocus fires after the focus has already moved — it cannot be cancelled.
B. LostFocusEvent || LostFocus fires when focus is lost, not before gaining focus.
C. GettingFocusEvent (correct) || GettingFocusEvent fires in the tunnel phase before the focus change occurs. Set e.Cancel = true to prevent the focus from moving to the new element.
D. PreviewGotFocusEvent || Avalonia does not have a PreviewGotFocusEvent. Use GettingFocusEvent with RoutingStrategies.Tunnel.
Explanation: GettingFocusEvent is the cancelable pre-focus event. GotFocusEvent fires after the focus change and cannot be cancelled.
```

```quiz
Q: What is a focus scope?
A. A visual effect that highlights the focused element. || Focus scopes are about tab-navigation boundaries, not visual effects.
B. A way to limit Tab navigation to elements within a specific container. (correct) || Focus scopes create boundaries for tab navigation. When the last element inside a scope is reached, Tab moves to the next scope rather than the next element in flat tree order.
C. The area of the screen where the pointer is located. || Focus is about keyboard input, not pointer location.
D. A method to disable focus on all child controls. || That would be setting Focusable=false on each child, not a scope.
Explanation: Focus scopes (TabControl, GroupBox, etc.) keep Tab navigation confined within the scope boundary for better keyboard navigation.
```

```quiz
Q: How can you style a parent container differently when any of its children has focus?
A. Use the :focus pseudo-class on the parent. || :focus applies only to the focused element itself.
B. Use the :focus-within pseudo-class on the parent. (correct) || :focus-within matches when any descendant element has focus, making it the correct selector for this scenario.
C. Subscribe to GotFocus on each child and update a style property. || That works but is more code than using :focus-within.
D. Both B and C are valid approaches. (correct) || :focus-within is the declarative approach. Subscribing to got-focus events per child is the imperative approach. Both work — :focus-within is cleaner.
Explanation: :focus-within is the declarative CSS-like approach. Handling GotFocusEvent per child is the imperative alternative. Both are valid.
```

```quiz
Q: What does FocusManager.TryMoveFocus(NavigationDirection.Next) do?
A. Returns the next focusable element without changing focus. || That is FindNextElement, not TryMoveFocus.
B. Moves focus to the next element in tab order. (correct) || TryMoveFocus attempts to move focus from the currently focused element to the next focusable element in the specified direction. It returns true if successful.
C. Removes focus from all controls. || That is ClearFocus.
D. Moves the focused control to a new position in the visual tree. || TryMoveFocus does not rearrange the tree.
Explanation: TryMoveFocus moves focus forward/backward in tab order. For querying without moving, use FindNextElement.
```

```quiz
Q: A TextBox has IsTabStop="False" but Focusable="True". How can the user interact with it?
A. The TextBox cannot receive focus at all. || IsTabStop only affects Tab navigation, not programmatic or click-based focus.
B. The user can click on it or it can be focused programmatically, but Tab skips over it. (correct) || IsTabStop controls whether the element is included in Tab navigation. The element can still receive focus via mouse click, pointer tap, or calling Focus() in code.
C. Both Tab and click focus are disabled. || Click focuses it since IsTabStop only affects Tab.
D. The TextBox is completely disabled. || IsEnabled controls whether a control is disabled, not IsTabStop.
Explanation: IsTabStop = false excludes the control from tab navigation only. The control remains focusable via mouse click or programmatic Focus().
```
