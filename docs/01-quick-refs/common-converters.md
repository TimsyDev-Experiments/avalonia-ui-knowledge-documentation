---
topic: converters
estimated: 3 min read
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Q09 — Common Converters

## Built-in IValueConverter implementations

Avalonia does not ship a `BoolToVisibilityConverter` — write your own or use one from a toolkit.

## Essential converters (write-your-own)

### BoolToVisibilityConverter

```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is true;
        if (Invert) b = !b;
        return b ? true : false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true;
}
```

### StringNotEmptyConverter

```csharp
public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

### EnumToBoolConverter

```csharp
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter as string;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Enum.Parse(targetType, parameter as string!) : BindingOperations.DoNothing;
}
```

### MultiValueConverter (IMultiValueConverter)

```csharp
public class AllBoolConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        => values.OfType<bool>().All(b => b);
}
```

## Converter parameters

```xml
<Button IsVisible="{Binding IsActive, Converter={StaticResource BoolToVis}}"
        Command="{Binding SaveCommand}" />

<Button IsVisible="{Binding IsActive,
        Converter={StaticResource BoolToVis},
        ConverterParameter=Invert}" />
```

## Registering converters

```xml
<Window.Resources>
  <local:BoolToVisibilityConverter x:Key="BoolToVis" />
  <local:StringNotEmptyConverter x:Key="NotEmpty" />
</Window.Resources>
```

## CommunityToolkit converters

Available in `CommunityToolkit.Common`:

| Converter | Behavior |
|---|---|
| `BoolToObjectConverter` | Maps bool → any two objects |
| `StringToVisibilityConverter` | Empty/null → collapsed |
| `CollectionIsEmptyConverter` | True if collection is empty |
