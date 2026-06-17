---
tier: basics
topic: value converters
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 004-value-converters.md
---

```quiz
Q: A ViewModel exposes `[ObservableProperty] private bool _isReady;`. The view needs a `<Button>` that is hidden when `IsReady` is `false`. What is the minimal XAML binding?
A. `<Button IsVisible="{Binding IsReady}" />` (correct) || `IsVisible` in Avalonia accepts `bool` directly — no `Visibility` enum, no converter, and no `IValueConverter` wrapper needed for a simple boolean pass-through.
B. `<Button IsVisible="{Binding IsReady, Converter={StaticResource BoolToVis}}" />` with a converter that maps `true`→`true` and `false`→`false` || This works but is redundant; the identity transform adds no value when the source is already `bool` and the target expects `bool`.
C. `<Button Visibility="{Binding IsReady, Converter={StaticResource BoolToVisibility}}" />` using a converter that emits `Visibility.Visible` / `Visibility.Collapsed` || This is the WPF pattern. Avalonia does not have a `Visibility` enum on `Button`; `IsVisible` is the correct property and it is already `bool`.
D. `<Button IsVisible="{Binding IsReady, Mode=OneWayToSource}" />` || `OneWayToSource` pushes target → source, which would read the button's visibility state and write it to the ViewModel, the opposite of what is needed.
Explanation: Avalonia's `IsVisible` is a `bool` property. The binding `{Binding IsReady}` works directly — no converter, no enum mapping. This avoids the WPF boilerplate of `BoolToVisibilityConverter`.
```

```quiz
Q: In Avalonia 12, `FuncMultiValueConverter<TIn, TOut>` changed its input type. What was the change and why does it matter?
A. It now accepts `IReadOnlyList<TIn>` instead of `IEnumerable<TIn>`, allowing indexed access (`values[0]`) without materializing to a list. (correct) || `IReadOnlyList<TIn>` guarantees random access and a `Count` property, so a lambda like `values => values.All(v => v is true)` works without `.ToList()`.
B. It now accepts `IEnumerable<TIn>` instead of `IReadOnlyList<TIn>`, increasing flexibility for lazy evaluation. || The actual change was the opposite: the parameter was tightened to `IReadOnlyList<TIn>` for convenience.
C. It now accepts `Span<TIn>` instead of `IEnumerable<TIn>`, enabling zero-allocation converter lambdas. || `Span<T>` cannot be used in a lambda-captured generic delegate with the current converter infrastructure.
D. It now accepts `IList<TIn>` instead of `IReadOnlyList<TIn>`, allowing mutation of the input collection inside the converter. || Mutation of binding inputs inside a converter is an anti-pattern and not supported by the read-only contract.
Explanation: `FuncMultiValueConverter` in Avalonia 12 expects `IReadOnlyList<TIn>`, which provides indexer access and `Count`. Converters can write `values[0]` directly instead of calling `.ToList()` or `.ElementAt()`.
```

```quiz
Q: A developer writes `new FuncValueConverter<bool, bool>(value => !value)` to invert a boolean for `IsVisible`. What must be done to use this converter in XAML with compiled bindings?
A. Nothing beyond registering the converter instance as a XAML resource, e.g., `<converters:Inverse x:Key="Inverse" />` (correct) || `FuncValueConverter` works as a one-way converter; no `ConvertBack` is needed. With compiled bindings, the `x:DataType` on the control ensures type safety, and the converter is referenced via `{StaticResource Inverse}`.
B. Implement `ConvertBack` explicitly because compiled bindings enforce two-way by default. || Compiled bindings respect the target property's default mode; read-only bindings remain one-way. `FuncValueConverter` does not require `ConvertBack` for one-way use.
C. Add `[ObservableProperty]` to the converter field so the XAML compiler can resolve it. || `[ObservableProperty]` is a source-generator attribute for ViewModel fields, not for converter instances.
D. Expose the converter as a static property and reference it with `{x:Static}` because `FuncValueConverter` cannot be instantiated as a resource. || `FuncValueConverter` can be instantiated as a resource; `{x:Static}` is an alternative for static members but not a requirement.
Explanation: `FuncValueConverter<TIn, TOut>` is a ready-to-use one-way converter. Register it as a resource in XAML, then bind with `Converter={StaticResource Inverse}`. No `ConvertBack` implementation exists or is needed.
```

```quiz
Q: A ViewModel property `Price` is a `decimal`. The view must display `"Total: $12.50"`. Which approach avoids creating a custom `IValueConverter` class?
A. `<TextBlock Text="{Binding Price, StringFormat='Total: {0:C}'}" />` (correct) || `StringFormat` is a first-class binding property. The binding applies `string.Format` with the current culture before pushing the value to `TextBlock.Text` — no converter class required.
B. `<TextBlock Text="{Binding Price, Converter={StaticResource CurrencyFormatter}}" />` with a `CurrencyFormatter` converter || This works but introduces a converter class for what `StringFormat` handles natively; unnecessary code.
C. `<TextBlock Text="{Binding Price, Mode=OneWayToSource, ConverterParameter='Total: {0:C}'}" />` || `OneWayToSource` pushes target to source, and `ConverterParameter` is not evaluated as a format string by the binding engine.
D. `<TextBlock Text="{Binding Price, Converter={StaticResource StringFormat}, ConverterParameter='Total: {0:C}'}" />` || This works if the converter is defined, but the question asks which approach *avoids creating a custom converter*; `StringFormat` on the binding itself is the zero-code solution.
Explanation: `StringFormat` on a binding is processed by the binding engine directly — no converter needed. It supports standard `.NET` format strings and culture-aware formatting.
```
