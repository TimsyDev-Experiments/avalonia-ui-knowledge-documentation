---
tier: advanced
topic: custom controls
estimated: 5-8 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 020-custom-templated-controls.md
---

# Quiz — Custom Templated Controls

```quiz
Q: Which base class should you extend for a reusable control whose visual appearance can be entirely replaced by consumers via a theme?
A. UserControl || UserControl has no Template property or ControlTheme support. Use it for pages/views in an MVVM app, not for library controls.
B. TemplatedControl (correct) || Lookless controls define logic in C# and appearance in a replaceable ControlTheme. OnApplyTemplate hooks up named template parts.
C. ContentControl || ContentControl extends TemplatedControl with ContentPresenter wiring — use it only when you need content hosting with template selection.
D. Control || Control is for Render-override drawing — no template, no theme support.
Explanation: TemplatedControl provides the Template property, OnApplyTemplate lifecycle hook, pseudo-class state management, and ControlTheme integration. This is the standard pattern for reusable controls where consumers may want to restyle the appearance without modifying the control's code.
```

```quiz
Q: In the ColorSwatchPicker example from 020X, OnApplyTemplate unsubscribes old event handlers from _swatchContainer before subscribing new ones. Why is this necessary?
A. To avoid duplicate event subscriptions when the template is reapplied (correct) || OnApplyTemplate fires each time the template is instantiated — initially, after theme switches, and after template reload. Without unsubscription, every call adds another handler, causing duplicate invocations and memory leaks.
B. To clear the ItemsSource binding || Handler unsubscription is separate from data binding setup.
C. To avoid null reference exceptions || The `if (_swatchContainer is not null)` guard already handles the null case on first call.
D. Because the old container element is destroyed by the framework || The old container may still be referenced; explicit unsubscription prevents leaks.
Explanation: OnApplyTemplate can be called multiple times during a control's lifetime. Each call must tear down previous template state (unsubscribe events, clear references) before setting up new state. Without this, each theme switch adds another event handler subscription.
```

```quiz
Q: When should you use DirectProperty instead of StyledProperty?
A. For properties that will be set in a style setter or animated || Those scenarios need StyledProperty, which participates in the value precedence system.
B. For properties like ICommand that are set only via code or binding and never come from a style (correct) || DirectProperty reads/writes a backing field directly with no precedence computation — faster, but no style or animation support.
C. DirectProperty is always preferred because it avoids the value precedence overhead || Choose by requirement, not by speed — if the value could ever be styled, StyledProperty is correct.
D. For read-only properties only || Read-only properties use DirectProperty, but read-write properties can use either depending on whether style participation is needed.
Explanation: DirectProperty bypasses Avalonia's multi-layered value precedence system. Use it for ICommand, selected items, or any property that is always set from code or binding and never from styles or animations. StyledProperty is the default for any property that might be styled or animated.
```

```quiz
Q: In a ControlTemplate, what does {TemplateBinding Value} resolve to?
A. A compiled binding to the templated parent's Value property (correct) || TemplateBinding is equivalent to {Binding RelativeSource={RelativeSource TemplatedParent}, Path=Value} but is shorter and compiled.
B. A binding to the current element's own Value property || TemplateBinding specifically targets the control the template is applied to, not the element it's written on.
C. A binding to the DataContext's Value property || The templated parent is not the same as the DataContext — they serve different roles.
D. A resource reference keyed to "Value" || TemplateBinding is not a resource lookup.
Explanation: TemplateBinding is a compiled-binding shorthand that only works inside a ControlTemplate with a matching TargetType. It binds to properties of the templated parent (the control instance), enabling the template to read the control's property values for display and layout.
```

```quiz
Q: The RatingControl lazy-initializes its command backing field: _incrementCommand ??= new RelayCommand(Increment). Why this pattern over constructor initialization?
A. Lazy allocation avoids creating RelayCommand objects for command properties the consumer never binds to (correct) || If the template doesn't reference IncrementCommand, the RelayCommand is never allocated, saving memory per control instance.
B. Constructors cannot instantiate RelayCommand || They can — there is no restriction.
C. It fixes a cross-thread access issue || Threading is not a concern here; the getter is called on the UI thread during template application.
D. It makes IncrementCommand read-only || The ??= pattern controls initialization timing, not mutability — the property has no setter either way.
Explanation: The lazy ??= pattern defers RelayCommand allocation until the getter is first called. For controls with several command properties, this avoids allocating command objects that may never be used (if the consumer does not bind to them). The DirectProperty getter runs during template application — lazy init keeps that path cheap.
```
