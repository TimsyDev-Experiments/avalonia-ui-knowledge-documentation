---
tier: intermediate
topic: bindings
estimated: 5-8 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 011-compiled-bindings.md
---

# Quiz — Compiled Bindings

```quiz
Q: What happens at build time if you bind to a property that doesn't exist on the type declared in the nearest x:DataType?
A. The binding silently returns null at runtime || With compiled bindings enabled, there is no silent fallback — the build fails.
B. The build fails with AVLN2000: Cannot resolve property 'X' on type 'Y' (correct) || The XAML compiler validates every {Binding} path against the declared x:DataType at build time.
C. A warning is emitted but the app still compiles || Compiled binding errors are build-breaking errors, not warnings.
D. Avalonia falls back to ReflectionBinding automatically || Automatic fallback to reflection only happens when x:DataType is absent in the tree.
Explanation: With <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault> (the v12 default), {Binding} maps to {CompiledBinding}. The compiler checks every binding path against the x:DataType scope at build time and emits AVLN2000 on mismatch.
```

```quiz
Q: You have a Window with x:DataType="vm:MainViewModel" and a ListBox whose ItemsSource binds to Items (a collection of TodoItem). Where must you set x:DataType="models:TodoItem"?
A. On the ListBox element || The ListBox inherits MainViewModel from the Window — bindings on ListBox properties (ItemsSource, SelectedItem) compile against MainViewModel.
B. On the DataTemplate inside ListBox.ItemTemplate (correct) || Bindings inside the DataTemplate target each item, so x:DataType must be overridden at the DataTemplate level. All children of the template inherit this override.
C. On each TextBlock inside the DataTemplate individually || The DataTemplate's x:DataType propagates to all descendant elements — no need to repeat it.
D. On the Window level || MainViewModel does not have TodoItem properties, so that would cause AVLN2000 errors on every binding inside the template.
Explanation: x:DataType propagates to all descendants until explicitly overridden. Inside a DataTemplate, bindings target the item type (TodoItem), so x:DataType must be set on the DataTemplate element. All children of the template (Grid, TextBlock, etc.) inherit this override automatically.
```

```quiz
Q: When is x:CompileBindings="False" the appropriate choice?
A. To disable all bindings in a subtree || It only disables compiled bindings — {Binding} still works via reflection.
B. When a specific subtree binds to dynamic or loosely-typed data (e.g., ExpandoObject, JObject, or a third-party control with runtime DataContext types) (correct) || x:CompileBindings="False" reverts every {Binding} in that subtree to ReflectionBinding, bypassing compile-time path checking.
C. To suppress build warnings || It disables compile-time checking entirely, which can hide real errors.
D. To improve rendering performance || Reflection is measurably slower than compiled access — this would hurt performance, not help it.
Explanation: x:CompileBindings="False" is the escape hatch for subtrees that cannot declare a fixed x:DataType. Typical use cases: legacy migration, dynamic data, or third-party controls. Use it sparingly and document why — it silences compile-time validation for that subtree.
```

```quiz
Q: What is the correct way to create a type-safe binding in C# that survives Native AOT trimming?
A. new Binding("PropertyName") || The old Binding constructor maps to ReflectionBinding, which uses runtime reflection and gets trimmed.
B. CompiledBinding.Create((Person p) => p.Name) (correct) || Takes a lambda expression compiled to direct property access at build time — no reflection, no trimming risk.
C. new ReflectionBinding("PropertyName") || ReflectionBinding uses Type.GetProperty + PropertyInfo.GetValue at runtime, which the trimmer removes.
D. BindingOperations.CreateBinding("PropertyName") || No such API exists in Avalonia v12 — IBinding and InstancedBinding were removed.
Explanation: CompiledBinding.Create accepts an expression lambda that the compiler converts to direct property access. new ReflectionBinding(string) uses runtime type metadata lookup — the Native AOT linker cannot preserve metadata for property names only known as strings at runtime.
```

```quiz
Q: Why are compiled bindings required for Native AOT publishing but not for desktop publishing?
A. Desktop targets include the full .NET runtime where reflection always works || Even on desktop, trimming can remove unused metadata — but the full runtime can JIT fallbacks. Native AOT cannot.
B. The Native AOT linker removes IL metadata not statically reachable — ReflectionBinding's string-based property lookup references members the linker cannot see, so their accessors are trimmed away (correct) || Compiled bindings generate statically-referenced method calls that the linker preserves.
C. Compiled bindings are faster and Native AOT requires maximum performance || Performance is a benefit, not the fundamental compatibility requirement.
D. Native AOT uses a different binding engine || Same binding engine; the difference is which APIs survive linking.
Explanation: Native AOT runs the IL linker which removes all code not statically reachable. ReflectionBinding calls Type.GetProperty("PropertyName") — the linker sees a string, not a member reference, so it removes PropertyName's getter. Compiled bindings emit direct calls like `((Person)source).Name`, which the linker preserves.
```
