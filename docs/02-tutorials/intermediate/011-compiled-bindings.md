---
tier: intermediate
topic: bindings
estimated: 8 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 011 — Compiled Bindings in Depth

**What you'll learn:** How compiled bindings work in Avalonia 12, explicit `CompiledBinding` vs `ReflectionBinding`, and patterns for mixed usage.

**Prerequisites:** [002 — Command Binding](/docs/02-tutorials/basics/002-command-binding.md)

---

## 1. How compiled bindings work

Avalonia 12 makes `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` the default. All `{Binding}` in XAML maps to `CompiledBinding` — the binding path is verified at build time.

If `UserName` doesn't exist on `MainViewModel`:

```xml
<TextBlock Text="{Binding UserNam}" />  <!-- typo -->
```

The build fails with: `error AVLN2000: Cannot resolve property 'UserNam' on type 'MainViewModel'`.

---

## 2. Explicit CompiledBinding and ReflectionBinding

You can be explicit in XAML:

```xml
<TextBlock Text="{CompiledBinding UserName}" />
<TextBlock Text="{ReflectionBinding DynamicProperty}" />
```

`CompiledBinding` requires `x:DataType` in scope. `ReflectionBinding` does not — it resolves at runtime, for dynamic or loosely-typed data.

---

## 3. x:DataType scope rules

`x:DataType` applies to all nested elements unless overridden:

```xml
<Window x:DataType="vm:MainViewModel">
  <!-- All compiled bindings here resolve against MainViewModel -->

  <ListBox ItemsSource="{Binding Items}">
    <ListBox.ItemTemplate>
      <DataTemplate x:DataType="models:TodoItem">
        <!-- Override: bindings here resolve against TodoItem -->
        <TextBlock Text="{Binding Title}" />
      </DataTemplate>
    </ListBox.ItemTemplate>
  </ListBox>
</Window>
```

You can also nest `x:DataType` on any container:

```xml
<StackPanel x:DataType="vm:DetailsViewModel">
  <TextBlock Text="{Binding Description}" />
</StackPanel>
```

---

## 4. x:CompileBindings="False"

To opt out of compiled bindings for a subtree:

```xml
<StackPanel x:CompileBindings="False">
  <!-- All bindings here are ReflectionBinding (runtime) -->
  <TextBlock Text="{Binding SomeDynamicProperty}" />
</StackPanel>
```

---

## 5. Creating bindings in C# code

Avalonia 12 changed the binding class hierarchy. Use `CompiledBinding.Create` or `ReflectionBinding`:

```csharp
// Compiled (type-safe, AOT-friendly)
var binding = CompiledBinding.Create((Person p) => p.Name);

// Reflection (runtime-resolved)
var binding = new ReflectionBinding(nameof(Person.Name));

// Old Binding() constructor maps to ReflectionBinding
var binding = new ReflectionBinding("SomeProperty");
```

The `IBinding` interface and `InstancedBinding` class were removed in v12. Use `BindingBase` or `BindingExpressionBase`.

---

## 6. Compiled bindings with Native AOT

When targeting Native AOT, compiled bindings are required. Reflection-based bindings may not work. Ensure:

- `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` is set
- All bindings have `x:DataType`
- No `ReflectionBinding` remains (or is guarded with `[DynamicDependency]`)

---

## Key Takeaways

- `{Binding}` = `{CompiledBinding}` when `AvaloniaUseCompiledBindingsByDefault` is true (default in v12)
- `x:DataType` is required for compiled bindings — set it on every view root and every DataTemplate
- Use `ReflectionBinding` only for dynamic/loosely-typed data
- In C#, use `CompiledBinding.Create()` or `new ReflectionBinding()`

---

## See Also

- [002 — Command Binding](/docs/02-tutorials/basics/002-command-binding.md)
- [009 — Data Templates Basics](/docs/02-tutorials/basics/009-data-templates-basics.md)
- [Avalonia Docs: Compiled Bindings](https://docs.avaloniaui.net/docs/data-binding/compiled-bindings)
