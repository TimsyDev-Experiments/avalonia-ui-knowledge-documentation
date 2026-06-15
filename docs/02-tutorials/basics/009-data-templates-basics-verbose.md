---
tier: basics
topic: data templates
estimated: 20 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 009-data-templates-basics.md
---

# 009V — Data Templates: An In-Depth Companion

**What you'll learn in this companion:** Not just how to write a DataTemplate, but how Avalonia's template system actually works — resolution order, the IDataTemplate contract, compiled bindings, and why templates live in DataTemplates collections instead of resources.

**Prerequisites:** [002 — Command Binding](002-command-binding.md), [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)

**You should already have read:** [009 — Data Templates Basics](009-data-templates-basics.md) for the quick-start version. This file goes deeper on every section.

---

## 1. What a DataTemplate Actually Is

A `DataTemplate` is a **factory**. When Avalonia needs to display an object that isn't a UI control (like a `TodoItem` — a plain C# class with no visual representation), it looks for a factory that can produce a control tree from that object. The `DataTemplate` is that factory: it takes data in, produces controls out.

```csharp
// Conceptually, this is what a DataTemplate does:
public interface IDataTemplate
{
    bool Match(object data);        // "Can I handle this type of data?"
    Control? Build(object? data);   // "Give me a control tree for this specific data"
}
```

The XAML `<DataTemplate>` you write is the declarative version of implementing `Build`. Avalonia's XAML compiler translates your markup into the equivalent `Control` tree construction code.

The `Match` method — Avalonia's way of deciding *which* template to use — is determined by the `DataType` attribute on the `DataTemplate`:

```xml
<DataTemplate DataType="models:TodoItem">
  <!-- Match returns true when the data object is a TodoItem (or a subclass) -->
  <CheckBox Content="{Binding Title}" IsChecked="{Binding IsDone}" />
</DataTemplate>
```

This is the core concept: **data in, controls out**. The DataTemplate sits between your data objects and your visual tree, acting as the bridge. Every list, every content area, every dynamic view in your app ultimately relies on this mechanism.

---

## 2. Where Templates Live (and Why)

In WPF, templates go in `Resources`. In Avalonia, they go in `DataTemplates` — a dedicated collection on every `Control` and on `Application` itself.

```xml
<!-- WPF way (doesn't work in Avalonia) -->
<Window.Resources>
  <DataTemplate DataType="{x:Type models:TodoItem}"> ... </DataTemplate>
</Window.Resources>

<!-- Avalonia way -->
<Window.DataTemplates>
  <DataTemplate DataType="models:TodoItem"> ... </DataTemplate>
</Window.DataTemplates>
```

**Why?** Because separating templates into their own collection gives Avalonia a simpler, faster resolution path. Instead of searching through merged resource dictionaries (which have their own complex lookup rules for keys, themes, and variants), Avalonia walks a flat list of `IDataTemplate` objects up the logical tree. This makes template resolution predictable and performant.

Every control that can host a data template has a `DataTemplates` property:

| Host | Scope |
|---|---|
| `Application.DataTemplates` | Global — every control in the app |
| `Window.DataTemplates` | That window and everything inside it |
| `UserControl.DataTemplates` | That control and its children |
| `ItemsControl.DataTemplates` | Items inside this specific list control |
| `ContentControl.DataTemplates` | Content inside this single-content control |
| `DataTemplates` collection on *any* `Control` | That element's subtree |

You choose the level based on how widely you need the template available. Put shared templates on `Application` (or a resource dictionary merged into it), put page-specific templates on the `Window` or `UserControl`, and put narrowly-scoped templates directly on the list control.

---

## 3. Template Resolution Order (The Critical Detail)

When a `ContentPresenter` or `ItemsControl` needs to find a template for an item, it does not just check its own `DataTemplates`. It walks **up** the logical tree:

```
1. Check the control's own ItemTemplate / ContentTemplate property (if set)
   │
2. Walk up the logical tree, checking each ancestor's DataTemplates collection
   │  (stops at the first IDataTemplateHost that has a matching template)
   │
3. Check Application.DataTemplates
   │
4. If nothing matched:
   └─ Fall back to default rendering (ToString())
```

Step 2 is the one that surprises most developers. When you put a `DataTemplate` on a `Window`, every `ContentControl` and `ItemsControl` inside that window can find it. You don't need to repeat the template on each individual control.

**Practical implications:**

- Put broadly-reusable templates as high as makes sense (Application or Window level)
- Put overrides as low as possible (on the specific control, where they shadow higher templates)
- If a template seems to not apply, the resolution might be finding a different match first

---

## 4. How Type Matching Works

Avalonia's `DataType` matching is more flexible than WPF's:

| Capability | WPF | Avalonia |
|---|---|---|
| Exact type match | Yes | Yes |
| Derived class match | Yes | Yes |
| Interface match | No | Yes |
| Abstract class match | No | Yes |

**Because interface matching is supported, template order matters.** If you have:

```xml
<Window.DataTemplates>
  <DataTemplate DataType="models:IEditableItem"> ... </DataTemplate>
  <DataTemplate DataType="models:TodoItem"> ... </DataTemplate>
</Window.DataTemplates>
```

And `TodoItem` implements `IEditableItem`, the first template (IEditableItem) will match all `TodoItem` instances because it's declared first. Either `TodoItem` gets the IEditableItem template (if ordered first), or it gets its own template (if ordered first). Place more-specific templates before more-general ones.

This is the reverse of how you might think about it — the first match wins, not the best match.

---

## 5. ItemTemplate vs DataTemplates: What's the Difference?

This is one of the most common sources of confusion. Two properties with similar names, different purposes:

### ItemTemplate (on ItemsControl / ListBox / ComboBox)

`ItemTemplate` is a **single** `IDataTemplate` applied directly to the control. It is set inline:

```xml
<ListBox ItemsSource="{Binding Items}">
  <ListBox.ItemTemplate>
    <DataTemplate x:DataType="models:TodoItem">
      <TextBlock Text="{Binding Title}" />
    </DataTemplate>
  </ListBox.ItemTemplate>
</ListBox>
```

This template applies only to this specific `ListBox` and only to its items. It is the most specific possible template location — it takes highest priority during resolution.

### DataTemplates (collection on any control)

`DataTemplates` is a **collection** of `IDataTemplate` objects that can be matched by type. It participates in the tree-walking resolution:

```xml
<Window.DataTemplates>
  <DataTemplate DataType="models:TodoItem">
    <CheckBox Content="{Binding Title}" IsChecked="{Binding IsDone}" />
  </DataTemplate>
</Window.DataTemplates>
```

This template applies to every `ContentControl`, `ItemsControl`, `ListBox`, etc. inside the window where a `TodoItem` needs to be displayed.

### Why both exist

Use `ItemTemplate` when you want a one-off template for a specific list. Use `DataTemplates` when you want the same template for every occurrence of a data type across a scope.

You can also use both together: define the template in `DataTemplates` for reuse, then override in `ItemTemplate` for a specific list:

```xml
<Window.DataTemplates>
  <!-- Default: inline checkbox -->
  <DataTemplate DataType="models:TodoItem">
    <CheckBox Content="{Binding Title}" IsChecked="{Binding IsDone}" />
  </DataTemplate>
</Window.DataTemplates>

<!-- Override in this specific ListBox: just text -->
<ListBox ItemsSource="{Binding Items}">
  <ListBox.ItemTemplate>
    <DataTemplate x:DataType="models:TodoItem">
      <TextBlock Text="{Binding Title}" />
    </DataTemplate>
  </ListBox.ItemTemplate>
</ListBox>
```

The `ItemTemplate` takes priority because it's checked first.

---

## 6. x:DataType and Compiled Bindings

Every code example in this tutorial uses `x:DataType`. This attribute tells the Avalonia XAML compiler what type the binding source will be, enabling **compiled bindings**:

```xml
<DataTemplate x:DataType="models:TodoItem">
  <CheckBox Content="{Binding Title}"
            IsChecked="{Binding IsDone}" />
</DataTemplate>
```

Without `x:DataType`:

- Bindings use **reflection** at runtime to find `Title` and `IsDone`
- A typo (`Titel`) silently fails at runtime — the binding resolves to nothing
- Performance is slightly worse because of the reflection overhead

With `x:DataType`:

- The compiler checks at **build time** that `Title` and `IsDone` exist on `TodoItem`
- A typo (`Titel`) produces a **compile error** — you catch it before running
- Bindings are resolved at compile time, making them faster

If your project has `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` in the `.csproj`, then `x:DataType` is required on every `DataTemplate` — the compiler will warn or error if it's missing.

Note that `x:DataType` and `DataType` are different attributes:
- `DataType` — used by the template matching system (`Match()`)
- `x:DataType` — used by the XAML compiler for compiled binding validation

You can (and should) set both to the same value:

```xml
<DataTemplate DataType="models:TodoItem"
              x:DataType="models:TodoItem">
```

Avalonia 12's XAML compiler also supports `{Binding}` (compiled when `x:DataType` is set), `{CompiledBinding}`, and `{ReflectionBinding}` (forces runtime reflection even when `x:DataType` is present).

---

## 7. The Full IDataTemplate Contract (Why Custom Selectors Work)

The `IDataTemplate` interface has exactly two methods:

```csharp
public interface IDataTemplate
{
    bool Match(object? data);
    Control? Build(object? data);
}
```

**`Match`** is called first. It answers: *"Should this template be used for this data object?"* Avalonia iterates through available templates (checking `ItemTemplate` first, then walking up `DataTemplates` collections) and uses the first one whose `Match` returns `true`.

For a `<DataTemplate DataType="models:TodoItem">`, the XAML compiler generates a `Match` method equivalent to:

```csharp
bool Match(object? data) => data is TodoItem;
```

**`Build`** is called second. It creates the control tree. For a XAML `DataTemplate`, the compiler generates code that instantiates the XAML element tree and sets up bindings.

A custom template selector works by implementing this interface yourself:

```csharp
public class ConditionalTemplateSelector : IDataTemplate
{
    public IDataTemplate DefaultTemplate { get; set; }
    public IDataTemplate AlternateTemplate { get; set; }
    public Func<object, bool> Condition { get; set; }

    public bool Match(object? data) => true;  // Match everything

    public Control? Build(object? data)
    {
        // Choose which template to delegate to at runtime
        if (data is not null && Condition(data))
            return AlternateTemplate.Build(data);
        return DefaultTemplate.Build(data);
    }
}
```

The key insight: `Match` decides *whether* the template applies. `Build` decides *what to create*. In the basic `DataTemplate`, both are derived from `DataType`. In a custom `IDataTemplate`, you control both independently.

---

## 8. Template Resolution Walkthrough (Step by Step)

Consider this scenario:

```xml
<Window DataContext="{Binding ...}" xmlns:models="using:MyApp.Models">
  <Window.DataTemplates>
    <DataTemplate DataType="models:TodoItem">
      <CheckBox Content="{Binding Title}" IsChecked="{Binding IsDone}" />
    </DataTemplate>
  </Window.DataTemplates>

  <ListBox ItemsSource="{Binding Items}">
    <!-- No ItemTemplate set — falls through to DataTemplates lookup -->
  </ListBox>
</Window>
```

When Avalonia renders the first item in `Items`:

1. `ListBox` asks its generator to create a container for the first item (a `TodoItem`)
2. It checks `ListBox.ItemTemplate` → **null** (not set), continue
3. It walks to `ListBox`'s parent, the `Window`, and checks `Window.DataTemplates`
4. First template in `Window.DataTemplates`: `DataTemplate DataType="TodoItem"`
5. Calls `Match(new TodoItem())` → `true` (type matches)
6. Calls `Build(new TodoItem())` → creates `CheckBox`, sets `DataContext = TodoItem`, and bindings resolve `Title` and `IsDone`
7. The `CheckBox` becomes the visual for that list item

Now change the `Window.DataTemplates` to have two templates where one is an interface:

```xml
<Window.DataTemplates>
  <DataTemplate DataType="models:ICompletable"> <!-- interface -->
    <TextBlock Text="{Binding Summary}" Foreground="Gray" />
  </DataTemplate>
  <DataTemplate DataType="models:TodoItem">
    <CheckBox Content="{Binding Title}" IsChecked="{Binding IsDone}" />
  </DataTemplate>
</Window.DataTemplates>
```

If `TodoItem : ICompletable`, then the first template matches. Your `TodoItem` renders as a `TextBlock`, not a `CheckBox`. This is why template order matters: the first `Match` that returns `true` wins, regardless of whether a more-specific template appears later.

---

## 9. Common Mistakes and Why They Happen

### Mistake 1: Putting DataTemplate in Resources

```xml
<Window.Resources>
  <DataTemplate DataType="models:TodoItem"> <!-- won't be found -->
    <CheckBox ... />
  </DataTemplate>
</Window.Resources>
```

**Why:** `DataTemplates` and `Resources` are separate collections. Template resolution only checks `DataTemplates`, never `Resources`. Move it to `<Window.DataTemplates>`.

### Mistake 2: Setting both ItemTemplate and DisplayMemberBinding

```xml
<ListBox ItemsSource="{Binding Items}"
         DisplayMemberBinding="{Binding Title}">
  <ListBox.ItemTemplate>
    <DataTemplate> ... </DataTemplate>
  </ListBox.ItemTemplate>
</ListBox>
```

**Why:** They are mutually exclusive. Setting both causes an exception at runtime. `DisplayMemberBinding` is a shortcut that internally creates a `DataTemplate` with a single `TextBlock` — you can't have both that and an explicit `ItemTemplate`.

### Mistake 3: Forgetting x:DataType and getting silent binding failures

```xml
<DataTemplate>
  <TextBlock Text="{Binding Titel}" /> <!-- typo: no x:DataType, no error -->
</DataTemplate>
```

**Why:** Without `x:DataType`, bindings use runtime reflection. The typo `Titel` silently resolves to nothing. With `x:DataType`, this would be a compile error.

### Mistake 4: Template appears to not apply

```xml
<Window.DataTemplates>
  <DataTemplate DataType="models:TodoItem">...</DataTemplate>
</Window.DataTemplates>
```

But the `ListBox` still shows `MyApp.Models.TodoItem` as plain text.

**Why:** The `ListBox` might have an `ItemTemplate` set in a style, or a control higher in the tree has an `ItemTemplate` set, or the `DataTemplates` collection is on a different branch of the logical tree that doesn't include this `ListBox`. Check each ancestor's `DataTemplates` and any applied styles.

### Mistake 5: Interface template matches when you don't want it to

You add a template for `ISelectable` expecting it to only apply to certain types, but it matches everything that implements that interface.

```xml
<DataTemplate DataType="models:ISelectable">
  <TextBlock Text="{Binding Name}" />
</DataTemplate>
```

**Why:** Interface matching is a feature. Every class implementing `ISelectable` will match this template unless a more-specific template (concrete type) is declared before it. Put concrete-type templates first, interface templates last.

---

## 10. Performance Considerations

### Recycling

When `ListBox` virtualizes items (reuses containers instead of creating new ones for every item), it can also reuse the control tree *inside* the container if the template supports it. This is controlled by `IRecyclingDataTemplate`. The built-in `DataTemplate` does support recycling, but custom `IDataTemplate` implementations must opt in:

```csharp
public class MyTemplate : IDataTemplate, IRecyclingDataTemplate
{
    public Control? Build(object? data, Control? existing)
    {
        if (existing is MyControl existingControl)
        {
            existingControl.DataContext = data;
            return existingControl;
        }
        return Build(data);
    }
}
```

### x:DataType performance

Compiled bindings avoid reflection at runtime. For lists with hundreds or thousands of items, this matters — reflection-based binding resolution adds measurable overhead per item per property. Always use `x:DataType` in templates that will be instantiated many times.

### Template lookup cost

Template resolution walks the logical tree. For a deeply nested control with many `DataTemplates` collections at each level, resolution can be non-trivial. If you're seeing performance issues with template resolution, consider:
- Moving frequently-matched templates to `Application.DataTemplates` (shorter walk)
- Using `ItemTemplate` directly on the control (skips the tree walk entirely)
- Using `ContentTemplate` on `ContentControl` explicitly rather than relying on type matching

---

## 11. How DataTemplate Differs from ContentTemplate

`ItemTemplate` is for items in a list. `ContentTemplate` is for a single piece of content:

```xml
<!-- Single item: ContentTemplate -->
<ContentControl Content="{Binding SelectedItem}">
  <ContentControl.ContentTemplate>
    <DataTemplate x:DataType="models:TodoItem">
      <Border BorderBrush="Gray" BorderThickness="1" Padding="8">
        <TextBlock Text="{Binding Title}" FontSize="18" />
      </Border>
    </DataTemplate>
  </ContentControl.ContentTemplate>
</ContentControl>
```

The resolution order is the same: `ContentTemplate` first, then `DataTemplates` tree walk, then `Application.DataTemplates`. The difference is only semantic — `ContentTemplate` applies to the single content object, `ItemTemplate` applies to each item in a collection.

---

## Key Takeaways

- `DataTemplate` is a **factory** that converts data objects to control trees — it's not a visual element itself
- Templates live in `DataTemplates` collections, not `Resources` — a deliberate design choice for cleaner resolution
- Resolution walks up the tree: `ItemTemplate` → ancestor `DataTemplates` → `Application.DataTemplates`
- `DataType` determines type matching (supports interfaces and abstract classes)
- `x:DataType` enables compiled bindings with build-time validation — always set it
- Template order matters because all types that match an interface will hit the interface template first
- Custom template selectors implement `IDataTemplate.Match` and `IDataTemplate.Build` directly
- Use `ItemTemplate` for one-off list templates, `DataTemplates` collection for reusable type-based templates
- `ContentTemplate` on `ContentControl` follows the same resolution rules as `ItemTemplate`

---

## See Also

- [009 — Data Templates Basics (original quick-start)](009-data-templates-basics.md)
- [009X — Data Templates: Real-World Examples](009-data-templates-examples.md)
- [015 — Item Lists in Depth](../intermediate/015-item-lists.md)
- [043 — TreeView with Hierarchical Data](../intermediate/043-treeview-hierarchical-data.md)
- [Avalonia Docs: Data Templates](https://docs.avaloniaui.net/docs/data-binding/data-templates)
- [Avalonia Docs: IDataTemplate](https://docs.avaloniaui.net/docs/data-templates/creating-data-templates-in-code)
