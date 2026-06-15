---
tier: basics
topic: data templates
estimated: 20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 009-data-templates-basics.md
---

# 009X — Data Templates: Real-World Examples

**What you'll build:** A multi-type message feed (chat, notifications) and a dynamic settings panel — two real scenarios that demonstrate DataTemplate matching, IDataTemplate selectors, and composite template patterns.

**Prerequisites:** [009 — Data Templates Basics](009-data-templates-basics.md). The [verbose companion](009-data-templates-basics-verbose.md) covers template resolution order, the IDataTemplate contract, and common mistakes in depth — useful context before these examples.

---

## Example 1: Multi-Type Conversation Feed

**Goal:** Render a chat-style message feed where different message types produce different visual output, selected automatically by data type.

A conversation contains a mix of text messages, image shares, and system announcements. Each type needs a distinct visual: a text bubble, a media card, or a centered status line.

### Data model

```csharp
// Models/Messages.cs
namespace MyApp.Models;

public abstract class Message
{
    public DateTime Timestamp { get; set; }
    public bool IsOwnMessage { get; set; }
}

public class TextMessage : Message
{
    public string Sender { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ImageMessage : Message
{
    public string Sender { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
}

public class SystemMessage : Message
{
    public string SystemText { get; set; } = string.Empty;
}
```

`Message` is abstract — no instance of it will ever appear. Each concrete subclass carries the data its template needs.

### ViewModel

```csharp
// ViewModels/ChatViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MyApp.Models;

namespace MyApp.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    [ObservableProperty]
    private string _newMessageText = string.Empty;

    public ObservableCollection<Message> Messages { get; } = new();

    [RelayCommand]
    private void Send()
    {
        if (string.IsNullOrWhiteSpace(NewMessageText))
            return;

        Messages.Add(new TextMessage
        {
            Sender = "You",
            Text = NewMessageText,
            Timestamp = DateTime.Now,
            IsOwnMessage = true,
        });

        NewMessageText = string.Empty;

        // Simulate a reply
        Messages.Add(new TextMessage
        {
            Sender = "Support",
            Text = "Got it, working on it.",
            Timestamp = DateTime.Now,
        });
    }

    [RelayCommand]
    private void AddSystemMessage()
    {
        Messages.Add(new SystemMessage
        {
            SystemText = "Support joined the conversation",
            Timestamp = DateTime.Now,
        });
    }
}
```

### View — the template definitions

Place three `DataTemplate` entries in `ChatView.DataTemplates`, ordered from most- to least-specific:

```xml
<!-- File: Views/ChatView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:models="using:MyApp.Models"
             x:Class="MyApp.Views.ChatView"
             x:DataType="vm:ChatViewModel">

  <UserControl.DataTemplates>
    <!-- Template 1: Text messages (most specific) -->
    <DataTemplate DataType="models:TextMessage"
                  x:DataType="models:TextMessage">
      <Border Margin="4,2" MaxWidth="400"
              HorizontalAlignment="{Binding IsOwnMessage, Converter={StaticResource BoolToAlign}}">
        <Border BorderBrush="#ddd" BorderThickness="1"
                CornerRadius="8" Padding="10,6"
                Background="{Binding IsOwnMessage, Converter={StaticResource BoolToBubbleBg}}">
          <StackPanel Gap="3">
            <TextBlock Text="{Binding Sender}"
                       FontWeight="SemiBold" FontSize="11"
                       Foreground="{Binding IsOwnMessage, Converter={StaticResource BoolToLabel}}"/>
            <TextBlock Text="{Binding Text}" TextWrapping="Wrap" />
            <TextBlock Text="{Binding Timestamp, StringFormat='{}{0:HH:mm}'}"
                       FontSize="10" Foreground="Gray"
                       HorizontalAlignment="Right" />
          </StackPanel>
        </Border>
      </Border>
    </DataTemplate>

    <!-- Template 2: Image messages -->
    <DataTemplate DataType="models:ImageMessage"
                  x:DataType="models:ImageMessage">
      <Border Margin="4,2" MaxWidth="400"
              HorizontalAlignment="{Binding IsOwnMessage, Converter={StaticResource BoolToAlign}}">
        <Border BorderBrush="#ddd" BorderThickness="1"
                CornerRadius="8" Padding="10,6"
                Background="{Binding IsOwnMessage, Converter={StaticResource BoolToBubbleBg}}">
          <StackPanel Gap="3">
            <TextBlock Text="{Binding Sender}"
                       FontWeight="SemiBold" FontSize="11" />
            <Border CornerRadius="6" ClipToBounds="True">
              <Image Source="{Binding ImageUrl}"
                     MaxHeight="240"
                     Stretch="Uniform" />
            </Border>
            <TextBlock Text="{Binding Caption}"
                       FontStyle="Italic"
                       TextWrapping="Wrap"
                       IsVisible="{Binding Caption, Converter={StaticResource NotNullToBool}}" />
          </StackPanel>
        </Border>
      </Border>
    </DataTemplate>

    <!-- Template 3: System messages (most general, matches SystemMessage) -->
    <DataTemplate DataType="models:SystemMessage"
                  x:DataType="models:SystemMessage">
      <Border Margin="4,2" HorizontalAlignment="Center">
        <TextBlock Text="{Binding SystemText}"
                   FontSize="11" Foreground="Gray"
                   FontStyle="Italic" />
      </Border>
    </DataTemplate>
  </UserControl.DataTemplates>

  <!-- Layout -->
  <DockPanel>
    <TextBox DockPanel.Dock="Bottom"
             Text="{Binding NewMessageText}"
             Watermark="Type a message..."
             AcceptsReturn="False">
      <TextBox.InnerRightContent>
        <Button Content="Send" Command="{Binding SendCommand}" />
      </TextBox.InnerRightContent>
    </TextBox>

    <Button DockPanel.Dock="Bottom"
            Content="Add System Message"
            Command="{Binding AddSystemMessageCommand}"
            Margin="0,4" />

    <ScrollViewer>
      <ItemsControl ItemsSource="{Binding Messages}" />
    </ScrollViewer>
  </DockPanel>
</UserControl>
```

### How it works

Avalonia's `ItemsControl` iterates `Messages`. For each item, it starts at the `ListBox` (or `ItemsControl`), finds no `ItemTemplate`, then walks up to `UserControl.DataTemplates`. It calls `Match` on each template in declaration order:

1. `DataTemplate DataType="TextMessage"` → `Match(new TextMessage(...))` → `true`. Build produces a chat bubble.

2. `DataTemplate DataType="ImageMessage"` → `Match(new ImageMessage(...))` → `true`. Build produces an image card.

3. `DataTemplate DataType="SystemMessage"` → `Match(new SystemMessage(...))` → `true`. Build produces a centered status line.

Because `TextMessage`, `ImageMessage`, and `SystemMessage` are unrelated concrete classes (they share the abstract `Message` base but never appear as `Message` directly), ordering among them is arbitrary — no type is a subclass of another. The abstract `Message` has no template, so it will never match anything.

### What the converters do

The `BoolToAlign`, `BoolToBubbleBg`, and `BoolToLabel` converters translate `IsOwnMessage` to visual choices — right/left alignment, blue/gray bubble, white/dark text. These are classic `IValueConverter` implementations:

```csharp
// Converters/ChatConverters.cs
public class BoolToAlignConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? LayoutDirection.Right : LayoutDirection.Left;
    // ConvertBack not needed
}
```

---

## Example 2: Dynamic Settings Panel with Type-Based Editors

**Goal:** Render a settings page where each setting definition carries a `SettingType` discriminator, and the UI selects the appropriate editor control (toggle, text box, dropdown) automatically.

This scenario is common in real apps: settings are loaded from a file or API as a heterogeneous collection, and each one needs a different editor. This example uses a custom `IDataTemplate` selector to route by a property value (not by CLR type).

### Data model

```csharp
// Models/SettingDefinition.cs
namespace MyApp.Models;

public enum SettingType { Boolean, String, Choice }

public class SettingDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SettingType Type { get; set; }
    public object? CurrentValue { get; set; }
    public IList<string>? Options { get; set; } // For Choice type
}
```

### ViewModel

```csharp
// ViewModels/SettingsViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MyApp.Models;

namespace MyApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public ObservableCollection<SettingDefinition> Settings { get; } = new()
    {
        new()
        {
            Key = "notifications_enabled",
            DisplayName = "Enable notifications",
            Description = "Receive push notifications for updates",
            Type = SettingType.Boolean,
            CurrentValue = true,
        },
        new()
        {
            Key = "display_name",
            DisplayName = "Display name",
            Description = "Your public display name",
            Type = SettingType.String,
            CurrentValue = "Developer",
        },
        new()
        {
            Key = "theme",
            DisplayName = "Theme",
            Description = "Color scheme",
            Type = SettingType.Choice,
            CurrentValue = "System",
            Options = new[] { "System", "Light", "Dark" },
        },
        new()
        {
            Key = "auto_save",
            DisplayName = "Auto-save",
            Description = "Automatically save changes",
            Type = SettingType.Boolean,
            CurrentValue = false,
        },
    };
}
```

### Template selector (custom IDataTemplate)

Because the template should be chosen based on `SettingDefinition.Type` (a property value), not the CLR type, the built-in `DataType` matching won't work — every item is a `SettingDefinition`. Instead, implement `IDataTemplate` directly:

```csharp
// Selectors/SettingTemplateSelector.cs
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MyApp.Models;

namespace MyApp.Selectors;

public class SettingTemplateSelector : IDataTemplate
{
    // Sub-templates assigned from XAML
    public IDataTemplate? BooleanTemplate { get; set; }
    public IDataTemplate? StringTemplate { get; set; }
    public IDataTemplate? ChoiceTemplate { get; set; }

    public bool Match(object? data) => data is SettingDefinition;

    public Control? Build(object? data)
    {
        if (data is not SettingDefinition setting)
            return null;

        return setting.Type switch
        {
            SettingType.Boolean => BooleanTemplate?.Build(data),
            SettingType.String  => StringTemplate?.Build(data),
            SettingType.Choice  => ChoiceTemplate?.Build(data),
            _                   => null,
        };
    }
}
```

### View — wiring it together

The sub-templates are defined as keyed resources so the selector can reference them, and the `SettingTemplateSelector` instance is assigned to `ItemsControl.ItemTemplate`:

```xml
<!-- File: Views/SettingsView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:models="using:MyApp.Models"
             xmlns:selectors="using:MyApp.Selectors"
             x:Class="MyApp.Views.SettingsView"
             x:DataType="vm:SettingsViewModel">

  <UserControl.DataTemplates>
    <DataTemplate x:Key="BooleanEditor" x:DataType="models:SettingDefinition">
      <DockPanel>
        <ToggleSwitch DockPanel.Dock="Right"
                      IsChecked="{Binding CurrentValue, Mode=TwoWay}" />
        <TextBlock Text="{Binding DisplayName}"
                   VerticalAlignment="Center" />
      </DockPanel>
    </DataTemplate>

    <DataTemplate x:Key="StringEditor" x:DataType="models:SettingDefinition">
      <StackPanel Gap="2">
        <TextBlock Text="{Binding DisplayName}" FontSize="12" />
        <TextBox Text="{Binding CurrentValue, Mode=TwoWay}"
                 Watermark="Enter value..." />
      </StackPanel>
    </DataTemplate>

    <DataTemplate x:Key="ChoiceEditor" x:DataType="models:SettingDefinition">
      <StackPanel Gap="2">
        <TextBlock Text="{Binding DisplayName}" FontSize="12" />
        <ComboBox ItemsSource="{Binding Options}"
                  SelectedItem="{Binding CurrentValue, Mode=TwoWay}" />
      </StackPanel>
    </DataTemplate>
  </UserControl.DataTemplates>

  <UserControl.Resources>
    <selectors:SettingTemplateSelector x:Key="SettingSelector"
                                       BooleanTemplate="{StaticResource BooleanEditor}"
                                       StringTemplate="{StaticResource StringEditor}"
                                       ChoiceTemplate="{StaticResource ChoiceEditor}" />
  </UserControl.Resources>

  <ScrollViewer>
    <ItemsControl ItemsSource="{Binding Settings}"
                  ItemTemplate="{StaticResource SettingSelector}">
      <ItemsControl.ItemContainerTheme>
        <ControlTheme TargetType="ContentPresenter">
          <Setter Property="Padding" Value="8" />
        </ControlTheme>
      </ItemsControl.ItemContainerTheme>
    </ItemsControl>
  </ScrollViewer>
</UserControl>
```

### How it works

1. The `ItemsControl` iterates `Settings`. Each item is a `SettingDefinition`.
2. `ItemTemplate` is set to `SettingTemplateSelector`.
3. Avalonia calls `Match(new SettingDefinition(...))` → `true`.
4. Avalonia calls `Build(new SettingDefinition(...))`.
5. Inside `Build`, the selector checks `setting.Type` and delegates to the appropriate sub-template:
   - `Boolean` → `BooleanEditor` (ToggleSwitch)
   - `String` → `StringEditor` (TextBox)
   - `Choice` → `ChoiceEditor` (ComboBox)
6. The sub-template's `Build` creates the actual control tree and sets `DataContext = SettingDefinition`, so bindings resolve.

The three sub-templates are defined as keyed `DataTemplate` instances in `DataTemplates` (not `Resources`), and referenced via `StaticResource`. This works because keyed `DataTemplate` instances in the `DataTemplates` collection are also findable from the resource scope.

### Two-way binding to CurrentValue

Each editor binds `CurrentValue` with `Mode=TwoWay`. The `SettingDefinition.CurrentValue` is `object?`, so a boolean toggle writes `true`/`false`, a text box writes a `string`, and a combo box writes the selected string. The ViewModel doesn't need separate properties for each setting — a single `ObservableCollection<SettingDefinition>` drives the entire panel. When the user saves, the ViewModel iterates `Settings` and reads the current value per key.

---

## What These Examples Demonstrate

| Scenario | Template technique | What to learn |
|---|---|---|
| Chat feed | Multiple `DataTemplate` entries with `DataType` matching concrete types | Type-based selection, template ordering, rendering choice by data shape |
| Settings panel | Custom `IDataTemplate` that delegates to sub-templates by property value | When type matching isn't enough — routing by runtime state rather than CLR type |

The chat example uses Avalonia's built-in type-based template matching — you just declare `DataTemplate`s and they are selected automatically. The settings example needs a custom selector because all items share the same CLR type but differ by a property value.

## See Also

- [009 — Data Templates Basics](009-data-templates-basics.md)
- [009V — Verbose Companion](009-data-templates-basics-verbose.md)
- [015 — Item Lists in Depth](../intermediate/015-item-lists.md)
- [004 — Value Converters](../basics/004-value-converters.md) — essential for DataTemplate binding expressions
- [Avalonia Docs: IDataTemplate](https://docs.avaloniaui.net/docs/data-templates/creating-data-templates-in-code)
