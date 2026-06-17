---
tier: advanced
topic: extensibility
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 022-attached-properties-behaviors.md
---

# Quiz — Attached Properties & Behaviors

```quiz
Q: Which API registers an attached property that can be set on any AvaloniaObject?
A. AvaloniaProperty.Register<TSender, TValue>() || Register creates instance properties scoped to the owning class, not usable on arbitrary AvaloniaObject instances.
B. AvaloniaProperty.RegisterAttached<TOwner, THost, TValue>(name) (correct) || RegisterAttached with three type parameters (owner, host, value) creates a property that any AvaloniaObject can carry.
C. AvaloniaProperty.RegisterDirect<TOwner, TValue>() || RegisterDirect creates a direct (non-styled) instance property, not an attached one.
D. AttachedProperty.CreateGlobal<T>() || No such global registration API exists; attached properties always use RegisterAttached.
```

```quiz
Q: How does EnterKeyBehavior subscribe to changes on its attached Command property without polling?
A. By overriding TextBox.OnPropertyChanged in a derived class. || The behavior is static and cannot override instance methods on existing control types.
B. By hooking a class handler on CommandProperty.Changed via AddClassHandler<TextBox>() in the static constructor. (correct) || AddClassHandler<TextBox> subscribes to property changes on all TextBox instances efficiently through the property system.
C. By registering a global keyboard hook at the application level. || The behavior only reacts when the property is set on a TextBox, not on every key press globally.
D. By binding the property in XAML with a custom markup extension. || Binding does not trigger event subscriptions; the behavior needs imperative code to attach the KeyDown handler.
```

```quiz
Q: In FocusBehavior, why must OnAutoFocusChanged unsubscribe from AttachedToVisualTree when the property is set to false?
A. Because AttachedToVisualTree only fires once per control and should not be re-subscribed. || The event can fire multiple times; the key reason is to prevent memory leaks and unwanted focus calls.
B. To prevent memory leaks and ensure the behavior does not attempt to focus the control after the behavior is removed. (correct) || Unsubscribing cleans up the event handler so stale subscriptions do not accumulate and focus is not forced when the attached property is cleared.
C. To immediately remove focus from the control. || Unsubscribing only prevents future focus events; it does not programmatically unfocus the control.
D. Because subscribing twice causes an exception. || Attaching the same handler twice is safe (it still fires once per event); the cleanup is for correctness, not to avoid crashes.
```

```quiz
Q: When should you choose an attached behavior over a plain attached property?
A. When you only need to attach a data value to a control for binding. || A plain attached property suffices for data-only scenarios.
B. When you need to inject interaction logic such as keyboard handling, focus management, or event subscriptions. (correct) || Attached behaviors wrap the property with event handlers and conditional logic, as shown in the EnterKeyBehavior and FocusBehavior examples.
C. When the property should appear in the DevTools property panel. || Both attached properties and behaviors appear in DevTools; visibility is not the deciding factor.
D. When you want to set the value from a Style Setter. || Styles work with both; the choice depends on whether imperative logic is needed beyond setting a value.
```

```quiz
Q: According to the tutorial, what is the Avalonia equivalent of WPF's System.Windows.Interactivity.Behaviors?
A. The Microsoft.Xaml.Behaviors NuGet package. || Avalonia does not require a third-party behaviors library.
B. Code-behind event handlers in the control's own class. || Code-behind is not reusable across controls; behaviors are reusable attached components.
C. Attached behaviors built with RegisterAttached and class handlers. (correct) || The tutorial explicitly states: "Attached behaviors are the Avalonia equivalent of WPF's Interactivity.Behaviors — clean, reusable, XAML-composable logic extensions."
D. The Avalonia.Behaviors built-in namespace. || There is no built-in Avalonia.Behaviors namespace; the pattern uses the standard property system.
```
