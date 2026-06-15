---
tier: basics
topic: value converters
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 004-value-converters.md
---

# 004X — Value Converters: Real-World Examples

**What you'll build:** A file-size formatter and a priority-to-color converter for a task list — two scenarios that demonstrate `IValueConverter`, `FuncValueConverter`, and multi-binding in practical layouts.

**Prerequisites:** [004 — Value Converters](004-value-converters.md). The [verbose companion](004-value-converters-verbose.md) covers the `ConvertBack` contract, `FuncValueConverter` internals, and when to use `StringFormat` instead of a converter.

---

## Example 1: File Browser with Size Formatting and Date Grouping

**Goal:** Display a list of files with human-readable sizes (KB, MB, GB), relative timestamps ("2 hours ago"), and an icon based on file type — all via reusable converters.

The ViewModel stores raw bytes and `DateTime` values. Converters translate these to display strings without polluting the ViewModel with presentation logic.

### Converters

```csharp
// Converters/FileSizeConverter.cs
using System.Globalization;
using Avalonia.Data.Converters;

namespace MyApp.Converters;

public class FileSizeConverter : IValueConverter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long bytes)
            return AvaloniaProperty.UnsetValue;

        if (bytes == 0)
            return "0 B";

        var order = 0;
        double size = bytes;
        while (size >= 1024 && order < Units.Length - 1)
        {
            size /= 1024;
            order++;
        }

        return order == 0
            ? $"{size:F0} {Units[order]}"
            : $"{size:F1} {Units[order]}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

```csharp
// Converters/RelativeTimeConverter.cs
using System.Globalization;
using Avalonia.Data.Converters;

namespace MyApp.Converters;

public class RelativeTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime timestamp)
            return AvaloniaProperty.UnsetValue;

        var diff = DateTime.Now - timestamp;

        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return timestamp.ToString("MMM dd");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

```csharp
// Converters/FileTypeIconConverter.cs
using System.Globalization;
using Avalonia.Data.Converters;

namespace MyApp.Converters;

public class FileTypeIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string extension)
            return AvaloniaProperty.UnsetValue;

        return extension.ToLowerInvariant() switch
        {
            ".txt" or ".md" => "\uD83D\uDCC4",  // document
            ".jpg" or ".png" or ".gif" => "\uD83D\uDDBC", // image
            ".zip" or ".rar" => "\uD83D\uDCE6",  // archive
            ".exe" or ".dll" => "\u2699",        // gear
            _ => "\uD83D\uDCC1",                 // generic file
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

### ViewModel

```csharp
// ViewModels/FileBrowserViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class FileItem
{
    public string Name { get; set; } = string.Empty;
    public string Extension => System.IO.Path.GetExtension(Name);
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
}

public partial class FileBrowserViewModel : ObservableObject
{
    public ObservableCollection<FileItem> Files { get; } = new()
    {
        new() { Name = "readme.md", SizeBytes = 2048, LastModified = DateTime.Now.AddHours(-2) },
        new() { Name = "photo.jpg", SizeBytes = 2_456_000, LastModified = DateTime.Now.AddDays(-1) },
        new() { Name = "backup.zip", SizeBytes = 150_123_456, LastModified = DateTime.Now.AddDays(-5) },
        new() { Name = "app.exe", SizeBytes = 12_890_112, LastModified = DateTime.Now.AddMinutes(-15) },
    };
}
```

### View

```xml
<!-- Views/FileBrowserView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:conv="using:MyApp.Converters"
             x:Class="MyApp.Views.FileBrowserView"
             x:DataType="vm:FileBrowserViewModel">

  <UserControl.Resources>
    <conv:FileSizeConverter x:Key="FileSize" />
    <conv:RelativeTimeConverter x:Key="RelativeTime" />
    <conv:FileTypeIconConverter x:Key="FileTypeIcon" />
  </UserControl.Resources>

  <ListBox ItemsSource="{Binding Files}">
    <ListBox.ItemTemplate>
      <DataTemplate x:DataType="vm:FileItem">
        <Grid ColumnDefinitions="Auto,*,Auto" Margin="4,2" Gap="8">
          <TextBlock Text="{Binding Extension, Converter={StaticResource FileTypeIcon}}"
                     FontSize="22" VerticalAlignment="Center" />
          <StackPanel Grid.Column="1">
            <TextBlock Text="{Binding Name}" FontWeight="SemiBold" />
            <TextBlock Text="{Binding LastModified, Converter={StaticResource RelativeTime}}"
                       FontSize="11" Foreground="Gray" />
          </StackPanel>
          <TextBlock Grid.Column="2"
                     Text="{Binding SizeBytes, Converter={StaticResource FileSize}}"
                     FontSize="12" Foreground="Gray"
                     VerticalAlignment="Center" />
        </Grid>
      </DataTemplate>
    </ListBox.ItemTemplate>
  </ListBox>
</UserControl>
```

### How it works

1. `FileSizeConverter` takes a raw `long` byte count and returns a localized string like `"2.0 KB"`, `"23.4 MB"`, or `"1.2 GB"`. The conversion uses integer division for the unit traversal and formats with one decimal for units above KB.
2. `RelativeTimeConverter` takes a `DateTime` and returns a human-readable string. The thresholds (minutes, hours, days, weeks) are arbitrary — adjust based on your domain. For timestamps older than a week, it falls back to a date string.
3. `FileTypeIconConverter` maps file extensions to Unicode emoji characters. In a real app, this would return an `IImage` from embedded resources. The emoji approach avoids asset management at the cost of platform-dependent rendering.
4. Each converter returns `AvaloniaProperty.UnsetValue` for invalid input. This tells the binding system to use the target property's default value instead of crashing or showing "null".
5. All converters are registered as `StaticResource` in the `UserControl.Resources` — they are stateless singletons and never need `DynamicResource`.

### Design decisions and edge cases

- **`long` vs `int` for file sizes:** File sizes can exceed 2 GB. Using `long` avoids overflow. The converter checks for `long` specifically — if the ViewModel changes the property type to `int`, the converter returns `UnsetValue` (visible as empty text). Either widen the `is` check or add a compile-time test.
- **Relative time precision:** The converter rounds down (floor) rather than rounding to nearest. "2h ago" means "at least 2 hours, less than 3." Use `Math.Round` if your UX prefers "2h ago" for 1h35m.
- **Culture consideration:** `RelativeTimeConverter` uses `DateTime.Now` — it is not culture-aware. For localized relative times (French: "il y a 2 heures"), inject `CultureInfo` from the converter's `culture` parameter or use a resource file.
- **Edge case — zero bytes:** The converter explicitly handles `bytes == 0` returning `"0 B"`. Without this, the loop produces `"0.0 KB"` — misleading.

---

## Example 2: Task Priority Display with Multi-Binding

**Goal:** Render a task item with a priority indicator (color + label) and a due-date urgency badge, combining multiple source values through a single multi-value converter.

This scenario uses `FuncMultiValueConverter` to combine priority and due date into a single urgency level, and a regular `IValueConverter` to render that level as a color.

### Converters

```csharp
// Converters/PriorityConverters.cs
using Avalonia.Data.Converters;

namespace MyApp.Converters;

public static class PriorityConverters
{
    // Combines priority + days-until-due into an urgency score (0-4)
    public static readonly FuncMultiValueConverter<object, int> UrgencyScore =
        new(values =>
        {
            if (values.Count < 2) return 0;

            var priority = values[0] as int? ?? 0;
            var dueDate = values[1] as DateTime?;

            if (dueDate is null) return priority; // no due date, urgency = priority

            var daysUntilDue = (dueDate.Value.Date - DateTime.Now.Date).Days;

            if (daysUntilDue < 0) return 4;                 // overdue
            if (daysUntilDue == 0) return 3;                 // due today
            if (daysUntilDue <= 2) return Math.Max(priority, 2);
            return priority;
        });

    // Maps urgency score to a brush key name for DynamicResource lookup
    public static readonly FuncValueConverter<int, string> UrgencyToColorKey =
        new(score => score switch
        {
            4 => "UrgencyOverdueBrush",
            3 => "UrgencyTodayBrush",
            2 => "UrgencySoonBrush",
            1 => "UrgencyNormalBrush",
            _ => "UrgencyLowBrush",
        });
}
```

### ViewModel

```csharp
// ViewModels/TaskViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class TaskItem : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private int _priority; // 0=low, 1=normal, 2=high, 3=critical

    [ObservableProperty]
    private DateTime? _dueDate;

    [ObservableProperty]
    private bool _isComplete;
}

public partial class TaskListViewModel : ObservableObject
{
    public ObservableCollection<TaskItem> Tasks { get; } = new()
    {
        new() { Title = "Submit quarterly report", Priority = 3, DueDate = DateTime.Now.AddDays(-1) },
        new() { Title = "Review pull request", Priority = 2, DueDate = DateTime.Now.AddDays(1) },
        new() { Title = "Update dependencies", Priority = 1, DueDate = DateTime.Now.AddDays(5) },
        new() { Title = "Plan team offsite", Priority = 0, DueDate = null },
    };
}
```

### Theme resources (App.axaml)

```xml
<Application.Resources>
  <SolidColorBrush x:Key="UrgencyOverdueBrush" Color="#dc2626" />
  <SolidColorBrush x:Key="UrgencyTodayBrush" Color="#f59e0b" />
  <SolidColorBrush x:Key="UrgencySoonBrush" Color="#3b82f6" />
  <SolidColorBrush x:Key="UrgencyNormalBrush" Color="#6b7280" />
  <SolidColorBrush x:Key="UrgencyLowBrush" Color="#9ca3af" />
</Application.Resources>
```

### View

```xml
<!-- Views/TaskListView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:conv="using:MyApp.Converters"
             x:Class="MyApp.Views.TaskListView"
             x:DataType="vm:TaskListViewModel">

  <UserControl.Resources>
    <conv:PriorityConverters x:Key="PriorityConvertersInstance" />
  </UserControl.Resources>

  <ListBox ItemsSource="{Binding Tasks}">
    <ListBox.ItemTemplate>
      <DataTemplate x:DataType="vm:TaskItem">
        <Border Margin="4,2" Padding="8,6" CornerRadius="6"
                BorderBrush="{DynamicResource UrgencyNormalBrush}"
                BorderThickness="3,0,0,0">
          <Border.Styles>
            <!-- Override border brush via multi-binding -->
            <Style Selector="Border">
              <Setter Property="BorderBrush">
                <Setter.Value>
                  <MultiBinding Converter="{StaticResource PriorityConvertersInstance.UrgencyScore}">
                    <Binding Path="Priority" />
                    <Binding Path="DueDate" />
                  </MultiBinding>
                </Setter.Value>
              </Setter>
            </Style>
          </Border.Styles>

          <Grid ColumnDefinitions="Auto,*,Auto" Gap="8">
            <CheckBox IsChecked="{Binding IsComplete}" />

            <StackPanel Grid.Column="1" Gap="2">
              <TextBlock Text="{Binding Title}"
                         TextDecorations="{Binding IsComplete, Converter={StaticResource StrikeThrough}}"
                         Opacity="{Binding IsComplete, Converter={StaticResource BoolToOpacity}}" />
              <TextBlock Text="{Binding DueDate, StringFormat='Due: {0:MMM dd}'}"
                         FontSize="11"
                         Foreground="Gray"
                         IsVisible="{Binding DueDate, Converter={StaticResource NotNullToBool}}" />
            </StackPanel>

            <TextBlock Grid.Column="2" Text="{Binding Priority, Converter={StaticResource PriorityLabel}}" />
          </Grid>
        </Border>
      </DataTemplate>
    </ListBox.ItemTemplate>
  </ListBox>
</UserControl>
```

> `PriorityConvertersInstance.UrgencyScore` accesses the static field via the resource instance. In practice you would register each converter separately as a resource. The `MultiBinding` in the `Style` setter overrides the `BorderBrush` dynamically.

### How it works

1. `UrgencyScore` is a `FuncMultiValueConverter<object, int>`. It takes two bindings (priority and due date) and computes a composite urgency value. The `object` type parameter accepts mixed types in the `IReadOnlyList<object>`.
2. The `MultiBinding` inside `<Style Selector="Border">` updates the border brush whenever `Priority` or `DueDate` changes. The compiled bindings for `Priority` and `DueDate` are resolved against `TaskItem` (the `x:DataType` on the `DataTemplate`).
3. `UrgencyToColorKey` translates the score to a resource key string. In this example the `MultiBinding` directly calculates a brush reference. An alternative is to use `UrgencyToColorKey` with a second `DynamicResource` lookup — but `DynamicResource` cannot consume a dynamic key at runtime without a custom markup extension.
4. The `StrikeThrough` and `BoolToOpacity` converters handle the completed state: strikethrough text and reduced opacity when `IsComplete` is true.
5. The `PriorityLabel` converter translates the integer priority to a label string ("Low", "Normal", "High", "Critical").

### Design decisions and edge cases

- **MultiBinding in a Style setter:** The `<Setter Property="BorderBrush"><Setter.Value><MultiBinding ...>` pattern is verbose but works. An alternative is a custom `IValueConverter` that takes both values via `ConverterParameter` — but `ConverterParameter` is static (cannot bind). `MultiBinding` is the only way to feed dynamic values to a converter from a style.
- **`FuncMultiValueConverter<object, int>` with mixed types:** Using `object` as the input type avoids type constraints but requires casting inside the lambda. The `values[0] as int? ?? 0` pattern safely handles missing or type-mismatched values.
- **Overdue vs due-today logic:** The converter demotes high-priority items with a distant due date (priority is just one factor). This prevents a "Critical" task due in 3 months from showing the same urgency as a "Normal" task due today.
- **Null `DueDate`:** The converter treats `null` due dates as "no urgency from date" — the score equals the raw priority. The `IsVisible` binding on the due date label hides it when `DueDate` is null.

---

## What These Examples Demonstrate

| Scenario | Converter type | What to learn |
|---|---|---|
| File browser | `IValueConverter` (size, time, icon) | One-to-one value transformation, returning `AvaloniaProperty.UnsetValue` for invalid input, stateless converter singletons |
| Task priority | `FuncMultiValueConverter<TIn, TOut>` + `FuncValueConverter` | Multi-source aggregation, mixed-type value lists, computed urgency with business logic |

The file browser demonstrates classic one-to-one converters that transform raw data to display text. The task priority example shows how `MultiBinding` with a `FuncMultiValueConverter` combines multiple source values into a single visual output — something a single-value converter cannot do.

## See Also

- [004 — Value Converters](004-value-converters.md)
- [004V — Verbose Companion](004-value-converters-verbose.md)
- [005 — Binding Modes](005-binding-modes.md)
- [005V — Binding Modes (verbose companion)](005-binding-modes-verbose.md)
- [006 — Resources (Static & Dynamic)](006-resources.md)
- [Avalonia Docs: Value Converters](https://docs.avaloniaui.net/docs/data-binding/value-converters)
