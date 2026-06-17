---
tier: basics
topic: binding modes
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 005-binding-modes.md
---

```quiz
Q: A `TextBox` is bound to a `[ObservableProperty]` string `FullName`. The binding has no explicit `Mode` set. How does the binding behave in Avalonia 12?
A. Source-to-target only; updates to `FullName` in the ViewModel appear in the `TextBox`, but edits in the UI do not propagate back. || That describes `OneWay`, but `TextBox.Text` defaults to `TwoWay` per the control's binding metadata.
B. Bidirectional: edits in the `TextBox` update the ViewModel, and changes to `FullName` in the ViewModel update the `TextBox`. (correct) || `TextBox.Text` has its `DefaultBindingMode` set to `TwoWay`, so `Mode=Default` behaves as `TwoWay` without an explicit mode attribute.
C. Source-to-target once at load time; subsequent ViewModel changes do not refresh the UI. || That describes `OneTime`, which is not the default for any editing control.
D. The binding is silently ignored because `Mode=Default` is ambiguous for `TextBox`. || `Default` is a valid mode; the framework resolves it per-property using the owning control's `DefaultBindingMode` metadata.
Explanation: `TextBox.Text` declares `DefaultBindingMode=TwoWay` in its property metadata. `Mode=Default` resolves to `TwoWay`, enabling full source–target synchronization without an explicit `Mode=TwoWay` attribute.
```

```quiz
Q: You need to bind `PasswordBox.Password` so the ViewModel receives the user's typed password. No read-back from the ViewModel is required. Which binding mode achieves this without errors?
A. `Mode=TwoWay` || `PasswordBox.Password` does not expose a public `Set` via binding infrastructure for security reasons; `TwoWay` will compile but fail to push source changes back to the control, effectively breaking two-way sync.
B. `Mode=OneWay` || Source-to-target only; ViewModel changes would flow to the control, but the control's password input never reaches the ViewModel — the opposite of the required direction.
C. `Mode=OneWayToSource` (correct) || `OneWayToSource` pushes target → source only. The control's typed value writes to the ViewModel property, and no read-back from ViewModel to control occurs. This matches the write-only requirement.
D. `Mode=OneTime` || Source-to-target once at binding activation; even if the ViewModel later sets a value, the control never updates, and user input is not pushed to the ViewModel.
Explanation: `PasswordBox.Password` is intentionally write-only from the binding perspective. `OneWayToSource` sends user input to the ViewModel without attempting to set the control from the source — the correct fit for this scenario.
```

```quiz
Q: In Avalonia 12, how does `Mode=Default` differ from the 11.x behavior?
A. `Default` now behaves as `OneWayToSource` for all control properties, simplifying write-only scenarios. || No general change to `OneWayToSource`; the change was limited to `TwoWay` inference improvements.
B. `Default` now behaves as `TwoWay` for more properties because metadata inference was improved; being explicit with `Mode=TwoWay` is recommended for clarity. (correct) || Avalonia 12's binding system infers `TwoWay` for more property paths based on improved metadata, making `Default` resolve to `TwoWay` in cases where 11.x resolved to `OneWay`.
C. `Default` is deprecated in Avalonia 12; omitting `Mode` triggers a compile-time warning. || `Default` remains fully supported and is the implicit mode when `Mode` is omitted.
D. `Default` now requires `x:DataType` to be set on the binding expression or the nearest `{x:DataType}` scope. || `x:DataType` is a requirement for compiled bindings, but it is orthogonal to the `Mode` behavior; `Default` mode resolution does not depend on `x:DataType`.
Explanation: Avalonia 12 improved property-metadata inference so `Mode=Default` correctly identifies more properties as `TwoWay`-capable. When in doubt, write `Mode=TwoWay` explicitly — the behavior is identical but self-documenting.
```
