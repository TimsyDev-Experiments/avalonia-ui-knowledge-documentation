---
tier: basics
topic: observable-object-property
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 007-observable-object-property.md
---

```quiz
Q: Which requirements must a class satisfy to use the [ObservableProperty] source-generator attribute?
A. The class must inherit from ObservableObject, be declared partial, and the field must use an underscore prefix. || (correct) || [ObservableProperty] requires the containing class to be partial (the generator emits code into a separate partial declaration), extend ObservableObject (or implement INotifyPropertyChanged yourself), and follow the `_fieldName` naming convention.
B. The class must be sealed and implement INotifyPropertyChanged manually; the generator only creates the property wrapper. || The generator does emit a property wrapper, but it also generates the notification call; the class must still be partial and inherit ObservableObject.
C. Any partial class will work regardless of base type; the generator adds INotifyPropertyChanged automatically. || The generator relies on ObservableObject's SetProperty and RaisePropertyChanged infrastructure; it does not synthesize those methods.
D. The class must use the `unsafe` modifier because the generator uses pointer arithmetic on the backing store. || There is no unsafe requirement; the generator works through ordinary IL and partial class composition.
Explanation: [ObservableProperty] is a source generator from CommunityToolkit.Mvvm. The containing class must be `partial` so the generated property code can be placed in a sibling partial file. It must inherit `ObservableObject` (or a type that implements INotifyPropertyChanged). The generator recognizes only fields named with a leading underscore — `_name`, not `name` or `m_name`.

Q: Given `[ObservableProperty] private int _count;`, what does the source generator produce?
A. A public `int Count { get; set; }` with no change notification. || The generator always wires PropertyChanged.
B. A public `int Count` property that calls `RaisePropertyChanged` in the setter. || The generated setter calls `ObservableObject.SetProperty(ref _count, value)` which raises PropertyChanged only when the value actually changes.
C. A public `int Count` where the setter invokes `SetProperty(ref _count, value)` and raises `PropertyChanged` for `Count`. || (correct) || The generator emits `SetProperty(ref _count, value)` inside the setter, which compares old and new values and raises `PropertyChanged` only when they differ.
D. A private `int Count` property with a protected `OnCountChanged` virtual method. || The property is public, and the hook method name is `OnCountChanging`/`OnCountChanged` (partial, not virtual).
Explanation: The generator translates field `_count` into property `Count`. The setter body calls `SetProperty(ref _count, value)`, which is inherited from `ObservableObject`. This method performs a reference-equality check (or value-equality for value types) and raises `PropertyChanged` only when the value actually changes. The field retains the current value; the property is the public API surface.

Q: How does [NotifyPropertyChangedFor] affect the generated code when placed on a field with [ObservableProperty]?
A. It replaces the normal PropertyChanged notification with a custom event name. || [NotifyPropertyChangedFor] adds an additional notification; it does not replace the one for the primary property.
B. It causes the generated setter to also raise PropertyChanged for the specified dependent property after the primary property changes. || (correct) || [NotifyPropertyChangedFor(nameof(FullName))] on `_firstName` makes the generated setter call `RaisePropertyChanged(nameof(FullName))` after updating `_firstName`, so the bound UI updates automatically.
C. It makes the dependent property read-only in the generated code. || The attribute does not change the access level or mutability of the dependent property.
D. It suppresses the primary property's PropertyChanged event and only raises it for the dependent properties listed. || The primary notification is always emitted; [NotifyPropertyChangedFor] adds extra notifications on top.
Explanation: [NotifyPropertyChangedFor] lists dependent computed properties that should be re-notified when the observed field changes. For example, if `FullName` is a read-only property derived from `_firstName` and `_lastName`, decorating both fields with `[NotifyPropertyChangedFor(nameof(FullName))]` ensures that `FullName`'s binding updates whenever either component changes.

Q: What is the signature and purpose of the partial hook methods generated alongside [ObservableProperty]?
A. `partial void OnNameChanging(int value)` runs before the field is updated; `partial void OnNameChanged(int value)` runs after. || (correct) || The generator creates `On<Name>Changing` (pre-set) and `On<Name>Changed` (post-set), both accepting the incoming value. They are partial methods that do nothing unless you provide a body.
B. `partial void BeforeNameSet()` and `partial void AfterNameSet()` — parameterless bookends around the setter. || The hooks receive the new value as a parameter; they are not parameterless.
C. `partial void ValidateName(ref int value)` runs inside the setter and can reject the change by setting a bool return. || The hooks cannot reject changes; they are fire-and-forget. Validation should be handled in the setter or via ObservableValidator.
D. The hooks are only generated when the field type implements IDisposable. || The hooks are generated unconditionally for every [ObservableProperty] field, regardless of type.
Explanation: For a field `_count` of type `int`, the generator emits `partial void OnCountChanging(int value)` and `partial void OnCountChanged(int value)`. Both are partial methods — they are no-ops unless you provide an implementation body. `OnCountChanging` fires before `SetProperty` writes the field; `OnCountChanged` fires after the write and notification complete. The parameter receives the new value being assigned.
```
