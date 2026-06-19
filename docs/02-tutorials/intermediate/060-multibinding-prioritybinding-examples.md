---
tier: intermediate
topic: data
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 060E — MultiBinding & PriorityBinding (examples)

## Example 1: Full name formatter

**ViewModel:**

```csharp
public partial class PersonViewModel : ObservableObject
{
    [ObservableProperty] private string _firstName = "Jane";
    [ObservableProperty] private string _lastName = "Doe";
    [ObservableProperty] private string? _middleName;
}
```

**View:**

```xml
<TextBlock>
  <TextBlock.Text>
    <MultiBinding StringFormat="{}{0} {1} {2}">
      <Binding Path="FirstName" />
      <Binding Path="MiddleName" />
      <Binding Path="LastName" />
    </MultiBinding>
  </TextBlock.Text>
</TextBlock>
```

---

## Example 2: Login form validation (all conditions must be met)

**Converter:**

```csharp
public class AllTrueConverter : IMultiValueConverter
{
    public static readonly AllTrueConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType,
        object? parameter, CultureInfo culture)
    {
        foreach (var v in values)
            if (v is not true) return false;
        return true;
    }
}
```

**View:**

```xml
<Window.Resources>
  <local:AllTrueConverter x:Key="AllTrue" />
</Window.Resources>

<Button Content="Submit">
  <Button.IsEnabled>
    <MultiBinding Converter="{StaticResource AllTrue}">
      <Binding Path="IsFormValid" />
      <Binding Path="HasAcceptedTerms" />
      <Binding Path="IsNotBusy" />
    </MultiBinding>
  </Button.IsEnabled>
</Button>
```

---

## Example 3: Order summary with computed total

**Converter:**

```csharp
public class LineTotalConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType,
        object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is decimal price && values[1] is int qty)
            return price * qty;
        return 0m;
    }
}
```

**View:**

```xml
<StackPanel>
  <NumericUpDown x:Name="Qty" Value="1" Minimum="1" Maximum="99" />
  <TextBlock Text="{Binding UnitPrice, StringFormat='Unit: {0:C}'}" />

  <TextBlock FontWeight="Bold">
    <TextBlock.Text>
      <MultiBinding Converter="{StaticResource LineTotal}">
        <Binding Path="UnitPrice" />
        <Binding Path="#Qty.Value" />
      </MultiBinding>
    </TextBlock.Text>
  </TextBlock>
</StackPanel>
```

---

## Example 4: FuncMultiValueConverter (inline)

```csharp
public static class Converters
{
    // Concatenate non-empty strings with separator
    public static readonly FuncMultiValueConverter<string, string> JoinNonEmpty =
        new(parts => string.Join(" | ",
            parts.Where(p => !string.IsNullOrWhiteSpace(p))));

    // Average of numeric values
    public static readonly FuncMultiValueConverter<double, double> Average =
        new(vals => vals.Any() ? vals.Average() : 0.0);
}
```

```xml
<TextBlock.Text>
  <MultiBinding Converter="{x:Static local:Converters.JoinNonEmpty}">
    <Binding Path="Title" />
    <Binding Path="Subtitle" />
    <Binding Path="Tag" />
  </MultiBinding>
</TextBlock.Text>
```

---

## Example 5: PriorityBinding fallback chain

**ViewModel:**

```csharp
public partial class UserViewModel : ObservableObject
{
    [ObservableProperty] private string? _displayName;
    [ObservableProperty] private string? _userName;
    [ObservableProperty] private string _email = "unknown@example.com";
}
```

**View:**

```xml
<TextBlock>
  <TextBlock.Text>
    <PriorityBinding>
      <Binding Path="DisplayName" />
      <Binding Path="UserName" />
      <Binding Path="Email" FallbackValue="Unnamed User" />
    </PriorityBinding>
  </TextBlock.Text>
</TextBlock>
```

The TextBlock shows `DisplayName` if non-null, otherwise `UserName`, otherwise `Email`, or "Unnamed User" if all null.

---

## Example 6: Visibility from any-true condition

```csharp
public class AnyTrueConverter : IMultiValueConverter
{
    public static readonly AnyTrueConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType,
        object? parameter, CultureInfo culture)
    {
        // targetType is bool (IsVisible), return inverse for false
        return values.Any(v => v is true);
    }
}
```

```xml
<Border>
  <Border.IsVisible>
    <MultiBinding Converter="{x:Static local:AnyTrueConverter.Instance}">
      <Binding Path="HasErrors" />
      <Binding Path="HasWarnings" />
    </MultiBinding>
  </Border.IsVisible>
</Border>
```

---

## Example 7: Nested MultiBinding

```xml
<TextBlock>
  <TextBlock.Text>
    <MultiBinding StringFormat="{0} (age {1})">
      <MultiBinding StringFormat="{}{0} {1}">
        <Binding Path="FirstName" />
        <Binding Path="LastName" />
      </MultiBinding>
      <Binding Path="Age" />
    </MultiBinding>
  </TextBlock.Text>
</TextBlock>
```

---

## Example 8: Price range display with PriorityBinding

```xml
<TextBlock FontSize="16">
  <TextBlock.Text>
    <PriorityBinding>
      <Binding Path="SalePrice"
               StringFormat="Sale: {0:C}"
               FallbackValue="{x:Null}" />
      <Binding Path="ListPrice"
               StringFormat="List: {0:C}" />
      <Binding Path="Estimate"
               StringFormat="Est.: {0:C}" />
    </PriorityBinding>
  </TextBlock.Text>
</TextBlock>
```

---

## See Also

- [060 — MultiBinding & PriorityBinding (core)](060-multibinding-prioritybinding.md)
- [060V — MultiBinding & PriorityBinding (verbose)](060-multibinding-prioritybinding-verbose.md)
- [011 — Value Converters](../basics/011-value-converters.md)
