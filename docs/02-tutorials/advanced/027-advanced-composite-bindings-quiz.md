---
tier: advanced
topic: bindings
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 027-advanced-composite-bindings.md
---

# Quiz — Advanced Composite Bindings

```quiz
Q: Which binding type should you use to combine FirstName and LastName into a single display string?
A. PriorityBinding (correct) || PriorityBinding tries bindings in order — it does not combine them.
B. MultiBinding || MultiBinding merges multiple sources through an IMultiValueConverter into one target.
C. ReflectionBinding || ReflectionBinding resolves a single path at runtime on dynamic objects — it does not combine sources.
D. CompiledBinding.Create() || CompiledBinding.Create builds a single-path typed binding — it does not combine sources.
Explanation: MultiBinding takes multiple source bindings and passes their values as a list to IMultiValueConverter.Convert().
```

```quiz
Q: What is the correct way to bind to an indexer property with compiled bindings in Avalonia 12?
A. <TextBlock Text="{Binding [Greeting]}" x:DataType="vm:MainViewModel" /> (correct) || Compiled bindings resolve indexer syntax when the indexer is typed and x:DataType points to the owning VM.
B. <TextBlock Text="{ReflectionBinding [Greeting]}" /> || ReflectionBinding is for dynamic/ExpandoObject data, not typed indexers.
C. <TextBlock Text="{Binding Item[Greeting]}" x:DataType="vm:MainViewModel" /> || Item is the default indexer name in some frameworks but Avalonia compiled binding uses bracket syntax directly.
D. <TextBlock Text="{Binding Greeting}" x:DataType="vm:MainViewModel" /> || This binds to a property named Greeting, not an indexer lookup.
Explanation: Indexer bindings use bracket syntax like [Greeting] and require x:DataType for compiled binding resolution in Avalonia 12.
```

```quiz
Q: What is the correct way to create a type-safe binding in C# code in Avalonia 12?
A. new ReflectionBinding { Path = "FullName", Mode = BindingMode.OneWay } || ReflectionBinding resolves the path at runtime and is not type-safe.
B. CompiledBinding.Create((Person p) => p.FullName, bindingMode: BindingMode.OneWay) (correct) || CompiledBinding.Create takes a lambda expression, giving compile-time type safety and refactoring support.
C. new InstancedBinding { Source = person, Mode = BindingMode.OneWay } || InstancedBinding was removed in Avalonia 12 — use BindingBase or CompiledBinding.Create.
D. new Binding("FullName") { Mode = BindingMode.OneWay } || The Binding class with string path is untyped — use CompiledBinding.Create for type safety.
Explanation: CompiledBinding.Create accepts a lambda expression and returns a type-safe IBinding implementation.
```

```quiz
Q: Which binding type is required for binding to an ExpandoObject or other dynamic data source?
A. PriorityBinding || PriorityBinding creates a fallback chain and does not handle dynamic property resolution.
B. MultiBinding || MultiBinding combines multiple static bindings — it does not handle dynamic dispatch.
C. CompiledBinding.Create() || CompiledBinding.Create requires a compile-time lambda and cannot resolve dynamic property names.
D. ReflectionBinding (correct) || ReflectionBinding resolves property paths at runtime, making it suitable for ExpandoObject and dynamic data.
Explanation: Dynamic property names are not known at compile time, so ReflectionBinding (or x:CompileBindings="False") is required.
```

```quiz
Q: When using PriorityBinding, what happens if the first binding's path resolves to null?
A. The target shows an empty string. || PriorityBinding does not stop at the first binding — it continues down the chain if the first value is null or unresolved.
B. An exception is thrown at runtime. || PriorityBinding handles fallback gracefully without exceptions.
C. PriorityBinding falls through to the next binding in the list. (correct) || PriorityBinding tries each binding in order and uses the first one whose value is not null/unset.
D. The app crashes with a binding error. || PriorityBinding is designed to prevent crashes by providing fallback alternatives.
Explanation: PriorityBinding evaluates bindings sequentially and uses the first one that resolves to a non-null value.
```
