---
tier: intermediate
topic: property-system
estimated: 5-8 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 052-property-system.md
---

# Quiz — Property System

```quiz
Q: Which Avalonia property type participates in styling, animation, and value inheritance?
A. DirectProperty || DirectProperty does not support styling, animation, or inheritance — it stores values in a CLR field for maximum performance.
B. StyledProperty (correct) || StyledProperty is the full-featured property type. It stores values in the property system dictionary and participates in the full value precedence chain including styling and animation.
C. AttachedProperty only || AttachedProperty does participate in styling and animation, but it is a variant of StyledProperty designed for extending foreign types.
D. CLR property || A plain CLR property has no binding, styling, or inheritance support in the Avalonia property system.
Explanation: StyledProperty is the default choice for UI properties that need styling, animation, or inheritance support.
```

```quiz
Q: A Button has a local Foreground="Red". A Style sets Foreground="Blue" via a type selector, and another Style setter triggers on :pointerover to set Foreground="Green". What color is the button when hovered?
A. Green — the :pointerover trigger has higher priority than the type selector. || StyleTrigger (priority 3) is higher than Style (priority 5), but LocalValue (priority 2) outranks both.
B. Blue — the type selector applies to all Button instances. || LocalValue beats Style.
C. Red (correct) || LocalValue (priority 2) outranks both StyleTrigger (priority 3) and Style (priority 5). The local value Foreground="Red" wins regardless of hover state.
D. The colors blend — Avalonia averages conflicting values. || The property system selects a single winner by priority; values do not blend.
Explanation: LocalValue has priority 2, StyleTrigger has priority 3. The local Foreground="Red" value wins over all style-sourced values.
```

```quiz
Q: You are writing a custom slider control that tracks an internal _isDragging field. The control needs to update a visual when IsDragging changes. Other parts of the app should be able to bind to IsDragging. Which property type should you use?
A. StyledProperty || StyledProperty is heavier than needed for internal control state that does not need styling.
B. DirectProperty (correct) || DirectProperty stores the value in a CLR backing field, does not participate in styling or inheritance, and is the recommended choice for internal control state. It still supports binding via RegisterDirect.
C. AttachedProperty || AttachedProperty extends foreign types; this state belongs to the control itself.
D. A plain CLR field || A plain field would not raise PropertyChanged, so bindings would not update.
Explanation: DirectProperty is designed for internal control state. It provides binding support without the overhead of the full styling/inheritance pipeline.
```

```quiz
Q: What does SetCurrentValue do differently from SetValue?
A. SetCurrentValue queues the value to be applied on the next layout pass. || Both apply immediately.
B. SetCurrentValue writes at the current effective priority rather than forcing LocalValue. (correct) || SetCurrentValue writes at whatever priority is currently active (Animation, LocalValue, StyleTrigger, etc.), allowing styles to override it. SetValue always writes at LocalValue priority.
C. SetCurrentValue is asynchronous and returns a Task. || Both are synchronous.
D. SetCurrentValue only works for DirectProperty instances. || SetCurrentValue works on any StyledProperty.
Explanation: SetCurrentValue is the correct method for control implementors to update their own properties without creating a LocalValue entry that blocks style overrides.
```

```quiz
Q: A derived control class needs to change the default value of a property inherited from its base. How should it do this?
A. Re-register the property with AvaloniaProperty.Register in the derived class. || Re-registering creates a separate property with a different identity.
B. Call OverrideDefaultValue<TDerived> or OverrideMetadata<TDerived> in the static constructor. (correct) || OverrideDefaultValue<TDerived> and OverrideMetadata<TDerived> let a derived type change the default value (and optionally other metadata) for instances of the derived type only.
C. Override the property's getter to return a different default. || The property getter delegates to GetValue, which checks metadata — you must change the metadata.
D. Set the value in the instance constructor instead. || Setting in the constructor applies LocalValue priority, which blocks styling. OverrideMetadata keeps the default at Unset priority so styles can still override.
Explanation: OverrideMetadata (or OverrideDefaultValue) in the static constructor changes the default for the derived type while keeping the value at Unset priority, allowing styles to override it normally.
```

```quiz
Q: A descendant type wants to read an inherited property that was defined on an unrelated ancestor type. What must it do?
A. Nothing — inheritance works automatically for any AvaloniaObject. || A type must register the property via AddOwner before GetValue will work.
B. Call AddOwner<T> on the original property in its static constructor. (correct) || AddOwner registers the property on the new type, enabling GetValue to resolve it. The descendant then participates in the same inheritance chain.
C. Re-declare the property with Register and inherits: true. || Re-registering creates a separate property that does not share the inheritance chain with the original.
D. Set the property value in the XAML of every child. || That defeats the purpose of inheritance and creates many local values.
Explanation: AddOwner is the correct way to make a property available on a type that did not originally register it. The descendant can then read the inherited value.
```

```quiz
Q: Which approach should you use to change the default value AND add a coercion callback for a property in a derived class?
A. OverrideDefaultValue for the default, then add a coerce callback separately. || OverrideDefaultValue only changes the default; overriding callback requires OverrideMetadata or AddOwner with metadata.
B. OverrideMetadata<TDerived> with a new StyledPropertyMetadata that includes both the new default and the coerce callback. (correct) || OverrideMetadata accepts a full StyledPropertyMetadata instance, which can set defaultValue and coerce together.
C. RegisterAttached on the derived class with the same name. || RegisterAttached creates a different property type.
D. Use AddOwner on the original property with new metadata. || AddOwner is for unrelated types; for a derived class in the same hierarchy, OverrideMetadata is the direct approach.
Explanation: OverrideMetadata<TDerived> accepts a complete StyledPropertyMetadata with default, coerce, and other settings, making it the unified approach for derived class property customization.
```
