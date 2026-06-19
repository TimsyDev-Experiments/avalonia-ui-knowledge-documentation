---
tier: intermediate
topic: data
avalonia-version: 12.0.4
quiz-format: multiple-choice
---

# 060Q — MultiBinding & PriorityBinding (quiz)

## Q1. Which interface must a converter for MultiBinding implement?

- [ ] A. IValueConverter
- [ ] B. IMultiValueConverter
- [ ] C. IBindingConverter
- [ ] D. IMultiBindingConverter

**Answer:** B. MultiBinding uses `IMultiValueConverter`, which receives an `IList<object?>`.

---

## Q2. What binding modes does MultiBinding support?

- [ ] A. OneWay and TwoWay
- [ ] B. OneWay and OneTime
- [ ] C. OneWay, OneTime, and OneWayToSource
- [ ] D. All modes

**Answer:** B. MultiBinding is one-way only — `OneWay` and `OneTime`. There is no `ConvertBack`.

---

## Q3. In a PriorityBinding, which binding is used?

- [ ] A. The last successful binding
- [ ] B. The first binding that produces a valid value
- [ ] C. All bindings merged together
- [ ] D. The binding with the highest priority

**Answer:** B. PriorityBinding evaluates children in order and uses the first that returns a valid non-null value.

---

## Q4. What does `FuncMultiValueConverter<TIn, TOut>` do with values that are not of type `TIn`?

- [ ] A. Throws InvalidCastException
- [ ] B. Converts them automatically
- [ ] C. Skips them via `OfType<TIn>`
- [ ] D. Passes them as null

**Answer:** C. `FuncMultiValueConverter` filters the input list with `OfType<TIn>()`, silently skipping non-matching types.

---

## Q5. True or False: Nested MultiBinding is supported in Avalonia.

- [ ] A. True
- [ ] B. False

**Answer:** A. True. Avalonia supports nesting MultiBinding instances; each nested one resolves to a single value in the parent's array.

---

## Q6. When does a PriorityBinding fall through to the next binding?

- [ ] A. When the value is null or UnsetValue
- [ ] B. When the value is an empty string
- [ ] C. When the binding has a FallbackValue
- [ ] D. Always tries all bindings

**Answer:** A. PriorityBinding advances to the next binding when the current one returns null or `AvaloniaProperty.UnsetValue`.

---

## Q7. Which placeholder syntax is correct for escaping the first value in StringFormat?

- [ ] A. `StringFormat="\{0\}"`
- [ ] B. `StringFormat="{}{0}"`
- [ ] C. Both A and B
- [ ] D. `StringFormat="'{0}'"`

**Answer:** C. Both backslash escaping and prefix `{}` work. `StringFormat="\{0\}"` and `StringFormat="{}{0}"` are equivalent.

---

## Q8. What happens if an IMultiValueConverter throws an exception?

- [ ] A. The app crashes
- [ ] B. The exception is logged and FallbackValue is used
- [ ] C. The exception is silently swallowed
- [ ] D. The target property is not updated

**Answer:** B. Avalonia catches the exception, logs it to trace, and uses `FallbackValue` (or `AvaloniaProperty.UnsetValue`).

---

## Q9. How do you pass a ConverterParameter to a MultiBinding converter?

- [ ] A. `ConverterParameter="value"`
- [ ] B. It's not supported for MultiBinding
- [ ] C. Through the first child binding
- [ ] D. Via a separate element

**Answer:** A. `MultiBinding.ConverterParameter` works the same as on a regular `Binding`.

---

## Q10. Which scenario is NOT a good fit for PriorityBinding?

- [ ] A. Display name fallback chain
- [ ] B. Two-way binding with priority
- [ ] C. Price display (sale > list > estimate)
- [ ] D. Degrading gracefully when data is partial

**Answer:** B. PriorityBinding is one-way only and cannot be used for two-way scenarios.

---

## Scoring

| Score | Interpretation |
|-------|---------------|
| 10/10 | Expert |
| 8-9 | Strong understanding |
| 6-7 | Getting there |
| <6 | Review the core tutorial |

---

## See Also

- [060 — MultiBinding & PriorityBinding (core)](060-multibinding-prioritybinding.md)
- [060V — MultiBinding & PriorityBinding (verbose)](060-multibinding-prioritybinding-verbose.md)
- [060E — MultiBinding & PriorityBinding (examples)](060-multibinding-prioritybinding-examples.md)
