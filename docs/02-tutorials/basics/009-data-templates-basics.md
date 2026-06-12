---
tier: basics
topic: data templates
estimated: 8 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 009 — Data Templates Basics

**What you'll learn:** Display a list of items using `ItemsControl` and `DataTemplate`, and understand how template selection works.

**Prerequisites:** [002 — Command Binding](002-command-binding.md), [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)

---

## 1. The data model

```csharp
// Models/TodoItem.cs
namespace MyApp.Models;

public class TodoItem
{
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}
```

---

## 2. The ViewModel with an observable collection

```csharp
// ViewModels/MainViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MyApp.Models;

namespace MyApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<TodoItem> Items { get; } = new()
    {
        new() { Title = "Learn Avalonia", IsDone = true },
        new() { Title = "Write a tutorial", IsDone = false },
        new() { Title = "Build an app", IsDone = false },
    };
}
```

---

## 3. The simplest list

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:MyApp.ViewModels"
        xmlns:models="using:MyApp.Models"
        x:Class="MyApp.Views.MainWindow"
        x:DataType="vm:MainViewModel">
  <ItemsControl ItemsSource="{Binding Items}">
    <ItemsControl.ItemTemplate>
      <DataTemplate x:DataType="models:TodoItem">
        <CheckBox Content="{Binding Title}"
                  IsChecked="{Binding IsDone}" />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</Window>
```

Each item renders as a `CheckBox`. `x:DataType` on the `DataTemplate` enables compiled bindings within the template.

---

## 4. ListBox with selection

```xml
<ListBox ItemsSource="{Binding Items}"
         SelectedItem="{Binding SelectedItem, Mode=TwoWay}"
         x:DataType="vm:MainViewModel">
  <ListBox.ItemTemplate>
    <DataTemplate x:DataType="models:TodoItem">
      <TextBlock Text="{Binding Title}" />
    </DataTemplate>
  </ListBox.ItemTemplate>
</ListBox>
```

`SelectedItem` is two-way by default. Add to the ViewModel:

```csharp
[ObservableProperty]
private TodoItem? _selectedItem;

partial void OnSelectedItemChanged(TodoItem? value)
{
    // React to selection change
}
```

---

## 5. Template selector (different templates per item type)

For heterogeneous lists, create a selector:

```csharp
// Selectors/TodoTemplateSelector.cs
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MyApp.Models;

namespace MyApp.Selectors;

public class TodoTemplateSelector : IDataTemplate
{
    public IDataTemplate? DefaultTemplate { get; set; }
    public IDataTemplate? DoneTemplate { get; set; }

    public Control? Build(object? param)
    {
        if (param is TodoItem item)
            return (item.IsDone ? DoneTemplate : DefaultTemplate)?.Build(param);
        return DefaultTemplate?.Build(param);
    }

    public bool Match(object? data) => data is TodoItem;
}
```

```xml
<UserControl.Resources>
  <DataTemplate x:Key="DefaultTodo" x:DataType="models:TodoItem">
    <CheckBox Content="{Binding Title}"
              IsChecked="{Binding IsDone}" />
  </DataTemplate>
  <DataTemplate x:Key="DoneTodo" x:DataType="models:TodoItem">
    <TextBlock Text="{Binding Title}"
               TextDecorations="Underline"
               Foreground="Gray" />
  </DataTemplate>
  <selectors:TodoTemplateSelector x:Key="TodoSelector"
                                   DefaultTemplate="{StaticResource DefaultTodo}"
                                   DoneTemplate="{StaticResource DoneTodo}" />
</UserControl.Resources>

<ItemsControl ItemsSource="{Binding Items}"
              ItemTemplate="{StaticResource TodoSelector}" />
```

---

## 6. Common item controls comparison

| Control | Use for |
|---|---|
| `ItemsControl` | Simple read-only lists, no selection |
| `ListBox` | Single/multi selection from a list |
| `ComboBox` | Dropdown selection |
| `DataGrid` | Tabular data with columns, sorting, editing |
| `TreeView` | Hierarchical data |
| `ItemsRepeater` | Virtualized, high-performance custom layouts |

---

## Key Takeaways

- `ItemsControl` + `DataTemplate` is the foundation for all list rendering
- Always set `x:DataType` on `DataTemplate` for compiled bindings
- `ListBox` adds selection; `ComboBox` is a dropdown `ListBox`
- `IDataTemplate` selectors let you vary templates by item type/state

---

## See Also

- [014 — Item Lists in Depth](../intermediate/015-item-lists.md)
- [038 — Data Templates and IDataTemplate Selector Patterns](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/38-data-templates-and-idatatemplate-selector-patterns.md) (plugin ref)
- [Avalonia Docs: Data Templates](https://docs.avaloniaui.net/docs/data-binding/data-templates)
