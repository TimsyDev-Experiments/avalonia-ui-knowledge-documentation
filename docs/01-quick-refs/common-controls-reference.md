---
tier: reference
topic: controls
estimated: 5 min
researched: 2026-06-13
avalonia-version: 12.0.4
---

# Q07 — Common Controls Reference

One-page reference for the 15 most frequently used Avalonia controls. Each entry lists key properties, the most common event or command, and a minimal usage example.

---

## Button

| Property | Type | Default |
|---|---|---|
| `Content` | `object` | — |
| `Command` | `ICommand` | — |
| `CommandParameter` | `object` | — |
| `ClickMode` | `ClickMode` | `Release` |

```xml
<Button Content="Save" Command="{Binding SaveCommand}" />
```

---

## TextBox

| Property | Type | Notes |
|---|---|---|
| `Text` | `string` | Two-way bind by default |
| `Watermark` | `string` | Placeholder when empty |
| `AcceptsReturn` | `bool` | Multi-line when `true` |
| `MaxLength` | `int` | Character limit |
| `TextWrapping` | `TextWrapping` | `NoWrap`, `Wrap` |

```xml
<TextBox Text="{Binding Name}" Watermark="Enter your name" />
```

Event: `TextChanged` (or bind `Text`).

---

## CheckBox

| Property | Type | Notes |
|---|---|---|
| `IsChecked` | `bool?` | Three-state when `IsThreeState="True"` |
| `Content` | `object` | Label |

```xml
<CheckBox IsChecked="{Binding IsComplete}" Content="Done" />
```

---

## ComboBox

| Property | Type | Notes |
|---|---|---|
| `ItemsSource` | `IEnumerable` | Data source |
| `SelectedItem` | `object` | Selected item (two-way) |
| `SelectedIndex` | `int` | Index selection |
| `PlaceholderText` | `string` | Shown when nothing selected |

```xml
<ComboBox ItemsSource="{Binding Options}"
          SelectedItem="{Binding SelectedOption}" />
```

---

## ListBox

| Property | Type | Notes |
|---|---|---|
| `ItemsSource` | `IEnumerable` | Data source |
| `SelectedItem` | `object` | Single selection |
| `SelectionMode` | `SelectionMode` | `Single`, `Multiple`, `Toggle` |
| `SelectedItems` | `IList` | Multi-select binding |

```xml
<ListBox ItemsSource="{Binding Items}"
         SelectedItem="{Binding SelectedItem}" />
```

---

## DataGrid

| Property | Type | Notes |
|---|---|---|
| `ItemsSource` | `IEnumerable` | Requires `DataGrid` NuGet package |
| `AutoGenerateColumns` | `bool` | Auto-columns from properties |
| `CanUserSortColumns` | `bool` | Column header click to sort |
| `CanUserResizeColumns` | `bool` | Drag column borders |

```xml
<DataGrid ItemsSource="{Binding Items}"
          AutoGenerateColumns="True" />
```

---

## Slider

| Property | Type | Notes |
|---|---|---|
| `Minimum` / `Maximum` | `double` | Range |
| `Value` | `double` | Current value (two-way) |
| `TickFrequency` | `double` | Snap interval |
| `IsSnapToTickEnabled` | `bool` | Snap to ticks |

```xml
<Slider Minimum="0" Maximum="100" Value="{Binding Volume}" />
```

---

## ProgressBar

| Property | Type | Notes |
|---|---|---|
| `Minimum` / `Maximum` | `double` | Default 0–100 |
| `Value` | `double` | Current progress |
| `IsIndeterminate` | `bool` | Animated marquee style |

```xml
<ProgressBar Value="{Binding Progress}" />
<ProgressBar IsIndeterminate="True" />
```

---

## Image

| Property | Type | Notes |
|---|---|---|
| `Source` | `IBitmap` | Bind or set in code-behind |

```xml
<Image Source="/Assets/icons/save.png" />
```

Load from resources:

```csharp
Image.Source = new Bitmap(AssetLoader.Open(new Uri("avares://MyApp/Assets/logo.png")));
```

---

## Border

| Property | Type | Notes |
|---|---|---|
| `Background` | `IBrush` | Fill color |
| `BorderBrush` | `IBrush` | Stroke color |
| `BorderThickness` | `Thickness` | Stroke width |
| `CornerRadius` | `CornerRadius` | Rounded corners |
| `BoxShadow` | `BoxShadows` | Drop shadow |

```xml
<Border Background="{DynamicResource SystemAccentColor}"
        CornerRadius="8" Padding="16">
  <TextBlock Text="Card" />
</Border>
```

---

## StackPanel

| Property | Type | Notes |
|---|---|---|
| `Orientation` | `Orientation` | `Vertical` (default) or `Horizontal` |
| `Spacing` | `double` | Gap between children |

```xml
<StackPanel Spacing="8" Orientation="Horizontal">
  <Button Content="A" />
  <Button Content="B" />
</StackPanel>
```

---

## Grid

| Property / Attached | Type | Notes |
|---|---|---|
| `ColumnDefinitions` | `string` | e.g. `"Auto,*"` |
| `RowDefinitions` | `string` | e.g. `"32,*,Auto"` |
| `Grid.Column` / `Grid.Row` | `int` | Attached placement |
| `Grid.ColumnSpan` / `Grid.RowSpan` | `int` | Spanning |

```xml
<Grid ColumnDefinitions="Auto,*,Auto" RowDefinitions="Auto">
  <TextBlock Grid.Column="0" Text="Name:" />
  <TextBox Grid.Column="1" Text="{Binding Name}" />
  <Button Grid.Column="2" Content="Browse" />
</Grid>
```

---

## ScrollViewer

| Property | Type | Notes |
|---|---|---|
| `HorizontalScrollBarVisibility` | `ScrollBarVisibility` | `Auto`, `Visible`, `Hidden`, `Disabled` |
| `VerticalScrollBarVisibility` | `ScrollBarVisibility` | Same options |
| `AllowAutoHide` | `bool` | Auto-hide scrollbars |

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto">
  <StackPanel Spacing="8">
    <!-- scrollable content -->
  </StackPanel>
</ScrollViewer>
```

---

## TabControl / TabItem

| Property | Type | Notes |
|---|---|---|
| `ItemsSource` | `IEnumerable` | Tab data source |
| `SelectedItem` / `SelectedIndex` | — | Selection |
| `TabStripPlacement` | `Dock` | `Top`, `Bottom`, `Left`, `Right` |

```xml
<TabControl ItemsSource="{Binding Panels}"
            SelectedIndex="{Binding SelectedPanelIndex}">
  <TabControl.ItemTemplate>
    <DataTemplate>
      <TextBlock Text="{Binding Header}" />
    </DataTemplate>
  </TabControl.ItemTemplate>
  <TabControl.ContentTemplate>
    <DataTemplate>
      <ContentControl Content="{Binding Content}" />
    </DataTemplate>
  </TabControl.ContentTemplate>
</TabControl>
```

---

## TextBlock

| Property | Type | Notes |
|---|---|---|
| `Text` | `string` | Display text |
| `TextWrapping` | `TextWrapping` | Wrap behavior |
| `FontSize` | `double` | Size in DIPs |
| `FontWeight` | `FontWeight` | `Normal`, `Bold`, `Light`, etc. |
| `Foreground` | `IBrush` | Text color |
| `TextAlignment` | `TextAlignment` | `Left`, `Center`, `Right`, `Justify` |

```xml
<TextBlock Text="{Binding Title}"
           FontSize="18" FontWeight="Bold"
           TextWrapping="Wrap" />
```

---

## See Also

- [Control catalog (Avalonia docs)](https://docs.avaloniaui.net/controls/)
- [001 -- Project Setup](../02-tutorials/basics/001-project-setup.md)
- [015 -- Item Lists](../02-tutorials/intermediate/015-item-lists.md)
- [040 -- DataGrid Deep Dive](../02-tutorials/intermediate/040-datagrid-deep-dive.md)
