---
tier: intermediate
topic: bindings
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 011-compiled-bindings.md
---

# 011E — Compiled Bindings: Real-World Examples

**What this is:** Two worked examples showing compiled bindings in real app scenarios. Read [011 — Compiled Bindings](011-compiled-bindings.md) and [011V — Verbose Companion](011-compiled-bindings-verbose.md) first.

---

## Example 1: Migrating a Legacy View from Reflection to Compiled Bindings

### Goal

Port an existing Avalonia 11 view that uses `{Binding}` without `x:DataType` to Avalonia 12 compiled bindings, using `x:CompileBindings="False"` as a migration bridge so the view stays functional during the transition.

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class CustomerViewModel : ObservableObject
{
    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private bool _isPreferred;

    [ObservableProperty]
    private ObservableCollection<OrderSummary> _recentOrders = new();

    public string StatusBadge => IsPreferred ? "⭐ Preferred" : "Standard";
}

public partial class OrderSummary : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _product = string.Empty;

    [ObservableProperty]
    private decimal _total;
}
```

### XAML View — Migration Phase (Mixed)

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:vm="using:MyApp.ViewModels"
        x:DataType="vm:CustomerViewModel">
  <Grid RowDefinitions="Auto,Auto,Auto,*" Margin="20" Spacing="12">

    <!-- Section 1: Already migrated — compiled bindings -->
    <StackPanel Grid.Row="0" Spacing="4">
      <TextBlock Text="{Binding FullName}"
                 FontSize="18" FontWeight="Bold" />
      <TextBlock Text="{Binding StatusBadge}"
                 Foreground="{Binding IsPreferred,
                   Converter={StaticResource BoolToGoldConverter}}" />
    </StackPanel>

    <!-- Section 2: Not yet migrated — reflection fallback -->
    <StackPanel Grid.Row="1" x:CompileBindings="False">
      <TextBlock Text="Legacy info panel (reflection)" FontStyle="Italic" />
      <TextBlock Text="{Binding LegacyPropertyThatIsDynamic}" />
      <controls:LegacyCustomerChart x:CompileBindings="False" />
    </StackPanel>

    <!-- Section 3: Nested DataTemplate with override -->
    <TextBlock Grid.Row="2" Text="Recent Orders" FontWeight="SemiBold" />
    <ListBox Grid.Row="3" ItemsSource="{Binding RecentOrders}">
      <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:OrderSummary">
          <Grid ColumnDefinitions="Auto,*,Auto" Spacing="8">
            <TextBlock Text="{Binding Id}" FontWeight="Bold" />
            <TextBlock Text="{Binding Product}" Grid.Column="1" />
            <TextBlock Text="{Binding Total, StringFormat='\{0:C\}'}"
                       Grid.Column="2" />
          </Grid>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </Grid>
</Window>
```

### How It Works

1. The `Window` root sets `x:DataType="vm:CustomerViewModel"`. All direct bindings (`FullName`, `StatusBadge`, `IsPreferred`, `RecentOrders`) are compiled — verified at build time.
2. The nested `DataTemplate` overrides `x:DataType` to `OrderSummary`. The `Id`, `Product`, and `Total` bindings compile against `OrderSummary`.
3. The legacy section uses `x:CompileBindings="False"`. Its bindings fall back to `ReflectionBinding` — no `x:DataType` needed, no build error for `LegacyPropertyThatIsDynamic`.
4. The `controls:LegacyCustomerChart` also sits in the reflection subtree. Its own internal bindings (which may target dynamic properties) continue to work without modification.

### Design Decisions & Trade-offs

- **Why `x:CompileBindings="False"` instead of `ReflectionBinding` per element:** Less markup to change. One attribute on the container covers all children. When the legacy section is ready, remove the attribute and add `x:DataType` on the container.
- **Why keep reflection at all:** The `LegacyCustomerChart` third-party control binds to properties resolved at runtime. Converting it would require changing the control's source code. The reflection escape hatch avoids that.
- **Edge case — nested DataTemplate inside reflection subtree:** If a `DataTemplate` inside `x:CompileBindings="False"` declares its own `x:DataType`, its bindings are still compiled. The opt-out is per-subtree, not absolute. This lets you incrementally add compiled bindings from the inside out.
- **Risk:** Engineers may forget to remove `x:CompileBindings="False"` after migration. Track with a code comment and a grep-for pattern in CI.

---

## Example 2: Dynamic Control Generation with CompiledBinding.Create

### Goal

Generate a list of labeled text fields at runtime from a data schema, where each field's binding is constructed in C# using `CompiledBinding.Create` for type safety.

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class DynamicFormViewModel : ObservableObject
{
    public List<FieldDefinition> Fields { get; } = new()
    {
        new("firstName", "First Name", 50),
        new("lastName", "Last Name", 50),
        new("email", "Email", 100),
        new("age", "Age", 3),
    };

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private int _age;
}

public record FieldDefinition(string PropertyName, string Label, int MaxLength);
```

### Code-Behind — Binding Construction

```csharp
using Avalonia.Data;
using Avalonia.Controls;
using Avalonia.Layout;

namespace MyApp.Views;

public partial class DynamicFormView : UserControl
{
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not DynamicFormViewModel vm)
            return;

        var stack = this.FindControl<StackPanel>("FormStack");
        stack.Children.Clear();

        foreach (var field in vm.Fields)
        {
            var label = new TextBlock { Text = field.Label };
            var textBox = new TextBox
            {
                Watermark = $"Enter {field.Label}",
                MaxLength = field.MaxLength,
                Margin = new Thickness(0, 0, 0, 8),
            };

            // Compiled binding — type-safe, AOT-friendly
            var prop = typeof(DynamicFormViewModel).GetProperty(field.PropertyName)
                ?? throw new InvalidOperationException($"Missing property {field.PropertyName}");

            var binding = CompiledBinding.Create(
                (DynamicFormViewModel vm) => prop.GetValue(vm),
                (DynamicFormViewModel vm, object? val) => prop.SetValue(vm, val),
                BindingMode.TwoWay);

            textBox.Bind(TextBox.TextProperty, binding);
            stack.Children.Add(label);
            stack.Children.Add(textBox);
        }
    }
}
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:vm="using:MyApp.ViewModels"
             x:DataType="vm:DynamicFormViewModel">
  <StackPanel Name="FormStack" Spacing="4" Margin="20" />
</UserControl>
```

### How It Works

1. The `DynamicFormView` declares `x:DataType` so its own properties (if any) compile. The `FormStack` is empty in XAML — children are added in code-behind.
2. `OnDataContextChanged` fires when the ViewModel is assigned. The code iterates `Fields`, creates a `Label` + `TextBox` pair for each.
3. `CompiledBinding.Create` takes two lambdas — a getter and a setter. The getter calls `prop.GetValue(vm)` (a `MethodInfo` call, not string-based reflection) and the setter calls `prop.SetValue(vm, val)`.
4. The binding is applied via `textBox.Bind(TextBox.TextProperty, binding)`. The TextBox now updates the ViewModel property when the user types.

### Design Decisions & Trade-offs

- **Why `CompiledBinding.Create` with lambdas instead of `new ReflectionBinding("firstName")`:** `CompiledBinding.Create` preserves AOT safety. The lambdas are compiled at build time and the property access is a direct method call through the delegate. `ReflectionBinding` would use `Type.GetProperty` + `PropertyInfo.GetValue` at runtime — trimmed under Native AOT.
- **Why not use a collection of primitive values instead of named properties:** A `List<string>` would avoid reflection entirely, but loses per-field validation, per-field error tracking, and compiled binding support. The named-property approach lets you use `[Required]` / `[EmailAddress]` attributes.
- **Edge case — property type mismatch:** If `prop.GetValue(vm)` returns `int` but the binding target expects `string`, `CompiledBinding.Create` uses the Avalonia value converter pipeline (the same as XAML bindings). Add a `StringConverter` parameter if needed.
- **Performance:** Creating bindings in a loop for 4–20 fields is negligible. For hundreds of dynamic fields, cache the `CompiledBinding` instances per property (e.g., a `static Dictionary<string, IBinding>`).

---

## Comparison

| Aspect | Example 1 — Migration Bridge | Example 2 — Dynamic Generation |
|---|---|---|
| **Core technique** | `x:CompileBindings="False"` subtree + `x:DataType` override | `CompiledBinding.Create` in C# code-behind |
| **Binding medium** | XAML markup | Programmatic construction |
| **Use case** | Incrementally porting v11 code to v12 | Generating controls from runtime schema |
| **AOT safe** | Partially (reflection subtree is not) | Yes |
| **Type safety** | Build-time for migrated sections | Delegate-based, verified at compile time |
| **When to use** | You have a large v11 codebase to port | You build dynamic forms, property grids, or inspection panels |
| **Key risk** | `x:CompileBindings="False"` left in production | Reflection in `prop.GetValue/SetValue` if properties are not compile-time known |

---

## See Also

- [011 — Compiled Bindings (original)](011-compiled-bindings.md)
- [011V — Compiled Bindings (verbose companion)](011-compiled-bindings-verbose.md)
- [002 — Command Binding](../basics/002-command-binding.md)
- [Avalonia Docs: Compiled Bindings](https://docs.avaloniaui.net/docs/data-binding/compiled-bindings)
