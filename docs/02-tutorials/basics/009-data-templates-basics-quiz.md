---
tier: basics
topic: data templates
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 009-data-templates-basics.md
---

# Quiz — Data Templates Basics

```quiz
Q: Why must a DataTemplate inside an ItemsControl include x:DataType when compiled bindings are enabled?
A. Without x:DataType the XAML compiler cannot resolve binding paths at compile time — the binding falls back to reflection-based runtime resolution, losing type safety and performance (correct) || Compiled bindings require the XAML compiler to know the data type at compile time so it can verify property paths and emit optimized code. x:DataType on the DataTemplate provides that type for all bindings within the template.
B. x:DataType registers the DataTemplate in the visual tree || DataTemplate registration is unrelated to the x:DataType attribute; templates are resolved by the item control's ItemTemplate property.
C. x:DataType enables two-way binding on CheckBox.IsChecked || Two-way binding works with or without x:DataType. The attribute controls compile-time binding resolution, not binding direction.
D. x:DataType is required by the ItemsControl itself, not the DataTemplate || The ItemsControl uses x:DataType for its own bindings (ItemsSource, SelectedItem), but the DataTemplate's x:DataType is independent and applies only to bindings within the template.
Explanation: Compiled bindings in Avalonia use the XAML compiler to generate binding code at build time. The compiler needs x:DataType to know the type of the templated item so it can resolve property paths (Title, IsDone) and generate type-safe, performant binding accessors. Without it, bindings degrade to runtime reflection.
```

```quiz
Q: What is the default binding mode of ListBox.SelectedItem?
A. TwoWay — changes in the UI selection update the ViewModel property automatically without explicit Mode=TwoWay (correct) || ListBox.SelectedItem defaults to TwoWay so the ViewModel property tracks the user's selection without extra markup. The ViewModel needs an [ObservableProperty] matching the bound property name.
B. OneWay — the ListBox reads the value but never writes back || If the mode were OneWay, selection changes would not update the bound source, making SelectedItem useless for data-driven workflows.
C. OneTime — the value is set once at load and never updated || OneTime is used for static or rarely-changing data, not for interactive selection.
D. OneWayToSource — the ViewModel can set the selection but the ListBox cannot || This would prevent the user from ever selecting an item, which defeats the purpose of a selection control.
Explanation: ListBox.SelectedItem defaults to TwoWay binding. When a user selects an item, the ListBox writes the selected data object back to the bound property on the ViewModel. The corresponding ViewModel property uses [ObservableProperty] to raise PropertyChanged and optionally react via the partial OnChanged method.
```

```quiz
Q: What does the Match method on IDataTemplate control in a custom template selector?
A. It returns true when the selector can build a template for the given data object — all items of that type use the same selector instance (correct) || The Match method gates whether a data item is eligible for this IDataTemplate. In a selector, implementing Match() to check the item type or state ensures the selector is only applied to items it knows how to handle.
B. It matches a string pattern against the item's ToString() output || Match receives the raw data object, not a string. Pattern matching against ToString() would be fragile and is not the intended use.
C. It compares the data object's hash code against a dictionary of templates || Hash-code matching is not part of the IDataTemplate contract. The interface defines Build() and Match() for content-based template selection.
D. It determines whether the DataTemplate has been loaded from resources || Resource resolution is handled by the framework. Match is purely about whether this IDataTemplate applies to the given data object.
Explanation: IDataTemplate defines two methods: Build(object? param) creates the control tree, and Match(object? data) returns true if this template can handle the given data item. The ItemsControl iterates items and applies the first IDataTemplate whose Match returns true, calling Build to produce the visual.
```

```quiz
Q: When should ItemsControl be preferred over ListBox for rendering a collection?
A. When the list is read-only and no selection or click interaction is needed — ItemsControl has no selection concept and produces no selection overhead (correct) || ItemsControl is the lightest item-based control. It renders items via ItemTemplate but provides no selection, focus, or input handling, making it ideal for static display surfaces like dashboards or read-only lists.
B. When the list contains thousands of items that need virtualization || ItemsControl does not virtualize by default; ListBox supports virtualization. For large data sets ListBox is the better choice.
C. When items need multi-select with Ctrl+click || Multi-select requires ListBox's SelectionMode. ItemsControl has no selection infrastructure at all.
D. When each item must be editable inline with TextBox controls || Inline editing works equally with ItemsControl or ListBox. The choice depends on whether selection tracking is needed, not on editing capability.
Explanation: ItemsControl is a pure presentation control with no selection state, no focus management for items, and no keyboard navigation for item picking. It is the right choice when you want to render items without any interactive selection behavior. For selection, use ListBox; for dropdowns, use ComboBox; for tabular data, use DataGrid.
```
