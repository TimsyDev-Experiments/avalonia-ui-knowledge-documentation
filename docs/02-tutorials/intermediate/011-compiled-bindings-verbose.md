---
tier: intermediate
topic: bindings
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 011-compiled-bindings.md
---

# 011V — Compiled Bindings: An In-Depth Companion

**Why this exists:** The original tutorial shows you *what* compiled bindings are and *how* to use them. This companion explains *why* the architecture changed in Avalonia 12, *what happens inside* each binding type, and *when each pattern breaks or pays off*. Read this after the original.

**Cross-reference:** Original tutorial at [011-compiled-bindings.md](011-compiled-bindings.md).

---

## 1. Why compiled bindings became the default

Before Avalonia 12, every `{Binding}` resolved property paths at runtime via reflection. The binding engine walked the visual tree, looked up the `DataContext`, and used `TypeDescriptor` or direct reflection to find the named property. This approach has three problems:

- **Typo tolerance:** A misspelled property name produces no error at build time — the TextBlock just stays empty at runtime.
- **No IDE navigation:** You cannot "Go to Definition" from a binding path.
- **AOT incompatibility:** Reflection-based property access is trimmed away when you publish with Native AOT. The binding silently fails or throws.

Avalonia 12's `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` flips the default. The XAML compiler (Avalonia.NameGenerator) generates a method per binding that casts the DataContext to the declared `x:DataType` and accesses the property directly — no reflection, no string path, no runtime surprises.

The error `AVLN2000` is an XAML compiler error. It fires during `dotnet build`, not during app execution. This moves binding correctness into the same feedback loop as type errors in C#.

---

## 2. Explicit CompiledBinding vs ReflectionBinding — what each does internally

```xml
<TextBlock Text="{CompiledBinding UserName}" />
<TextBlock Text="{ReflectionBinding DynamicProperty}" />
```

`CompiledBinding` generates IL at compile time equivalent to:

```csharp
textBlock.Text = ((MainViewModel)textBlock.DataContext).UserName;
```

`ReflectionBinding` generates a call equivalent to:

```csharp
var prop = textBlock.DataContext.GetType().GetProperty("DynamicProperty");
textBlock.Text = prop?.GetValue(textBlock.DataContext);
```

**Key difference:** `ReflectionBinding` does **not** require `x:DataType` in scope. If no `x:DataType` is available up the tree, the binding engine silently falls back to reflection in v12 anyway (with a warning). But this fallback is only there for backward compatibility — it will not work under Native AOT.

**When to use ReflectionBinding:**

- The `DataContext` is `dynamic`, `ExpandoObject`, or `IDictionary<string, object>`.
- The bound property is computed from a path that only exists at runtime (e.g., `[0].Name` on a weakly-typed collection).
- You are migrating a large v11 codebase incrementally and cannot add `x:DataType` everywhere at once.

**When not to:** Any new code. Any AOT target. Any performance-sensitive list (reflection per item adds measurable overhead in virtualized lists).

---

## 3. x:DataType — scope, inheritance, and common mistakes

```xml
<Window x:DataType="vm:MainViewModel">
  <ListBox ItemsSource="{Binding Items}">
    <ListBox.ItemTemplate>
      <DataTemplate x:DataType="models:TodoItem">
        <TextBlock Text="{Binding Title}" />
      </DataTemplate>
    </ListBox.ItemTemplate>
  </ListBox>
</Window>
```

**What happens at each level:**

1. The `Window` declares `x:DataType="vm:MainViewModel"`. Every direct `{Binding}` child — including `ItemsSource="{Binding Items}"` — compiles against `MainViewModel`.
2. Inside the `DataTemplate`, `x:DataType` is overridden. The `TextBlock`'s `{Binding Title}` compiles against `TodoItem`.
3. The `ListBox` itself inherits `MainViewModel` as its DataType. Its own properties (`ItemsSource`, `SelectedItem`) must exist on `MainViewModel`.

**Common mistake:** Setting `x:DataType` on a `DataTemplate` that contains a `UserControl` whose `DataContext` is a different type. The binding inside the `UserControl` inherits the `DataTemplate`'s `x:DataType`, not the `UserControl`'s own `DataContext` type. Fix: redeclare `x:DataType` on the `UserControl` root element.

**Another mistake:** Forgetting `x:DataType` on a `DataTemplate` inside an `ItemsControl.ItemTemplate`. Without it, compiled bindings fail with `AVLN2000`. Add it even for trivial templates.

**Scope rule:** `x:DataType` propagates to all descendants until explicitly overridden. It is **not** overridden by `DataTemplate` unless you set it again. A `Grid` nested five levels deep still sees the grandparent's `x:DataType`.

---

## 4. x:CompileBindings="False" — the escape hatch

```xml
<StackPanel x:CompileBindings="False">
  <TextBlock Text="{Binding SomeDynamicProperty}" />
</StackPanel>
```

**What this does:** Instructs the XAML compiler to emit `ReflectionBinding` for every `{Binding}` in this subtree, even when `AvaloniaUseCompiledBindingsByDefault` is true. The parent's `x:DataType` is ignored.

**Use x:CompileBindings only when:**

- A specific section of the UI binds to a model object that has no fixed shape (e.g., JSON-deserialized `JObject`).
- You are wrapping a third-party control that swaps its `DataContext` type at runtime.
- You need incremental migration: disable on a subtree, fix bindings one at a time, re-enable.

**Warning:** `x:CompileBindings="False"` silently disables compile-time checking for that subtree. If someone later refactors the ViewModel and removes a property, no build error appears. Use it sparingly and document why.

---

## 5. C# binding construction — the v12 API change

```csharp
// Compiled (type-safe, AOT-friendly)
var binding = CompiledBinding.Create((Person p) => p.Name);

// Reflection (runtime-resolved)
var binding = new ReflectionBinding(nameof(Person.Name));

// Old Binding() constructor maps to ReflectionBinding
var binding = new ReflectionBinding("SomeProperty");
```

**Why `IBinding` and `InstancedBinding` were removed:** In v11, the binding pipeline produced `InstancedBinding` objects that wrapped both the binding expression and the target. v12 unified the architecture around `BindingBase` and `BindingExpressionBase`. The old interfaces added abstraction without value — every binding was essentially an expression tree or a string path anyway.

**When to use `CompiledBinding.Create`:**

- You are constructing bindings in code-behind (e.g., dynamic control generation).
- You need type safety at compile time.
- You want the binding to survive trimming.

**When to use `new ReflectionBinding`:**

- The property path is constructed dynamically at runtime (e.g., `"Item" + index`).
- You are binding to a `DataTable` column or a dictionary key.

**Performance note:** `CompiledBinding.Create` generates an expression tree that is compiled once and cached. Subsequent evaluations are direct property accesses — no reflection, no delegate invocation overhead. `ReflectionBinding` calls `Type.GetProperty` and `PropertyInfo.GetValue` on every evaluation (though it does cache the `PropertyInfo` per type).

---

## 6. Compiled bindings and Native AOT

**Why compiled bindings are required for Native AOT:** The .NET Native AOT publisher runs a linker that removes all metadata and code not statically reachable. Reflection-based property access requires the property's `MethodInfo` to survive trimming. The linker cannot know which strings you will pass to `ReflectionBinding` or `{Binding}` at runtime, so it removes the property accessors. The result: the binding silently fails (returns `UnsetValue`) or throws.

Compiled bindings generate direct property accesses at build time. The linker sees these as regular method calls and preserves them.

**Checklist for AOT readiness:**

- `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` — already the default in v12.
- Every `{Binding}` has a valid `x:DataType` in scope.
- Every `{CompiledBinding}` is explicit or via the default.
- Zero `{ReflectionBinding}` or `Binding` constructor calls in your codebase. If you must keep one, annotate the property with `[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(TargetType))]` on the consuming class.
- All converters are statically referenced (not loaded via `Activator.CreateInstance` from a string type name).

---

## 7. Common errors and debugging

| Error | Cause | Fix |
|---|---|---|
| `AVLN2000: Cannot resolve property 'X' on type 'Y'` | Typo, missing property, or wrong `x:DataType` | Check the property name and the nearest `x:DataType` ancestor |
| `AVLN2001: Binding path 'X.Y' is not valid on type 'Z'` | Nested path where intermediate property is null or wrong type | Use `x:DataType` on `DataTemplate` for each level |
| No binding error at build, but blank at runtime | Compiled binding disabled for the subtree, or DataContext is null at binding time | Check for `x:CompileBindings="False"`; ensure DataContext is set before layout |
| Binding works in debug but fails in release/Native AOT | `ReflectionBinding` used implicitly; trimmer removed the property | Switch to `CompiledBinding` or add `[DynamicDependency]` |

---

## Key Takeaways

- `{Binding}` = `{CompiledBinding}` when `AvaloniaUseCompiledBindingsByDefault` is true — the v12 default.
- `x:DataType` is mandatory for compiled bindings. Set it on every view root, every `DataTemplate`, and every `UserControl` top-level element.
- `ReflectionBinding` exists only for dynamic/legacy scenarios. It will not work under Native AOT without manual trimming annotations.
- In C#, use `CompiledBinding.Create(lambda)` for type-safe bindings; use `new ReflectionBinding(string)` only when paths are runtime-determined.
- Subtree opt-out: `x:CompileBindings="False"` converts all child `{Binding}` to reflection. Use sparingly.
- The removed `IBinding`/`InstancedBinding` types from v11 are replaced by `BindingBase`/`BindingExpressionBase`.

---

## See Also

- [011 — Compiled Bindings (original)](011-compiled-bindings.md)
- [002 — Command Binding](../basics/002-command-binding.md)
- [009 — Data Templates Basics](../basics/009-data-templates-basics.md)
- [018 — Navigation Patterns](018-navigation.md) (uses compiled bindings throughout shell views)
- [011E — Compiled Bindings (examples)](011-compiled-bindings-examples.md)
- [Avalonia Docs: Compiled Bindings](https://docs.avaloniaui.net/docs/data-binding/compiled-bindings)
