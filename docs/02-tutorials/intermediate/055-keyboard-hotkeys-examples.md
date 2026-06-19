---
tier: intermediate
topic: keyboard
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 055-keyboard-hotkeys.md
---

# 055E — Keyboard & Hotkeys: Real-World Examples

**What this is:** Two worked examples showing keyboard shortcut patterns in real app scenarios. Read [055 — Keyboard & Hotkeys](055-keyboard-hotkeys.md) and [055V — Verbose Companion](055-keyboard-hotkeys-verbose.md) first.

---

## Example 1: Document Editor with Full Keyboard Shortcuts

### Goal

Build a document editor window with:
- Application-wide shortcuts for file operations
- Scoped shortcuts for editing operations (only active when the editor has focus)
- Platform-portable Ctrl/Cmd handling
- Access keys in the menu bar

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:DocEditor.ViewModels"
        x:Class="DocEditor.Views.MainWindow"
        x:DataType="vm:MainViewModel"
        Title="Document Editor" Width="900" Height="600">

  <Window.Styles>
    <Style Selector="TextBox:focus /template/ Border">
      <Setter Property="BorderBrush" Value="{DynamicResource AccentColor}" />
      <Setter Property="BorderThickness" Value="2" />
    </Style>
  </Window.Styles>

  <Window.KeyBindings>
    <!-- File operations — always active -->
    <KeyBinding Gesture="Ctrl+N" Command="{Binding NewDocumentCommand}" />
    <KeyBinding Gesture="Ctrl+O" Command="{Binding OpenDocumentCommand}" />
    <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveDocumentCommand}" />
    <KeyBinding Gesture="Ctrl+Shift+S"
                Command="{Binding SaveAsCommand}" />

    <!-- Cross-platform: bind both Ctrl and Cmd -->
    <KeyBinding Gesture="Cmd+N" Command="{Binding NewDocumentCommand}" />
    <KeyBinding Gesture="Cmd+O" Command="{Binding OpenDocumentCommand}" />
    <KeyBinding Gesture="Cmd+S" Command="{Binding SaveDocumentCommand}" />
    <KeyBinding Gesture="Cmd+Shift+S"
                Command="{Binding SaveAsCommand}" />
  </Window.KeyBindings>

  <Grid RowDefinitions="Auto,*,Auto">
    <!-- Menu bar with access keys -->
    <Menu Grid.Row="0">
      <MenuItem Header="_File">
        <MenuItem Header="_New"   Command="{Binding NewDocumentCommand}"
                  HotKey="Ctrl+N" />
        <MenuItem Header="_Open"  Command="{Binding OpenDocumentCommand}"
                  HotKey="Ctrl+O" />
        <Separator />
        <MenuItem Header="_Save"  Command="{Binding SaveDocumentCommand}"
                  HotKey="Ctrl+S" />
        <MenuItem Header="Save _As..."
                  Command="{Binding SaveAsCommand}"
                  HotKey="Ctrl+Shift+S" />
        <Separator />
        <MenuItem Header="E_xit" Command="{Binding ExitCommand}" />
      </MenuItem>
      <MenuItem Header="_Edit">
        <MenuItem Header="_Undo"   Command="{Binding UndoCommand}"
                  HotKey="Ctrl+Z" />
        <MenuItem Header="_Redo"   Command="{Binding RedoCommand}"
                  HotKey="Ctrl+Y" />
        <Separator />
        <MenuItem Header="Cu_t"    Command="{Binding CutCommand}"
                  HotKey="Ctrl+X" />
        <MenuItem Header="_Copy"   Command="{Binding CopyCommand}"
                  HotKey="Ctrl+C" />
        <MenuItem Header="_Paste"  Command="{Binding PasteCommand}"
                  HotKey="Ctrl+V" />
      </MenuItem>
    </Menu>

    <!-- Editor area with scoped shortcuts -->
    <TextBox Name="EditorBox"
             Grid.Row="1"
             AcceptsReturn="True"
             Text="{Binding DocumentText}"
             Margin="8"
             FontFamily="Consolas"
             FontSize="14">
      <TextBox.KeyBindings>
        <!-- Ctrl+F: Find — only when editor has focus -->
        <KeyBinding Gesture="Ctrl+F"
                    Command="{Binding FindCommand}" />
        <KeyBinding Gesture="Cmd+F"
                    Command="{Binding FindCommand}" />

        <!-- Ctrl+G: Go to line — scoped to editor -->
        <KeyBinding Gesture="Ctrl+G"
                    Command="{Binding GoToLineCommand}" />
      </TextBox.KeyBindings>
    </TextBox>

    <!-- Status bar -->
    <Border Grid.Row="2" Padding="8"
            Background="{StaticResource SurfaceBrush}">
      <TextBlock Text="{Binding StatusText}" />
    </Border>
  </Grid>
</Window>
```

### ViewModel (excerpt)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace DocEditor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _documentText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    private int _documentCount;

    [RelayCommand]
    private void NewDocument()
    {
        DocumentText = string.Empty;
        _documentCount++;
        StatusText = $"Document {_documentCount} — new";
    }

    [RelayCommand]
    private void SaveDocument()
    {
        StatusText = $"Saved ({DateTime.Now:T})";
    }

    [RelayCommand]
    private void Find()
    {
        StatusText = "Find dialog (Ctrl+F) — scoped to editor";
    }
}
```

### Key points

- Window-level `KeyBindings` for file operations — active regardless of focus
- `TextBox.KeyBindings` for editing shortcuts — only active when the editor has focus
- Both `Ctrl+` and `Cmd+` variants for cross-platform support
- `HotKey` on `MenuItem` shows the shortcut label in the menu AND fires globally
- Access keys (`_File`, `_Save`) navigate the menu via Alt

---

## Example 2: Custom Shortcut Manager with Dynamic Commands

### Goal

Build a control that registers dynamic keyboard shortcuts at runtime and respects `CanExecute`.

### Custom control with dynamic KeyBindings

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Windows.Input;

namespace ShortcutApp.Controls;

public class ShortcutPanel : StackPanel
{
    public static readonly StyledProperty<ICommand?> SaveCommandProperty =
        AvaloniaProperty.Register<ShortcutPanel, ICommand?>(nameof(SaveCommand));

    public ICommand? SaveCommand
    {
        get => GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    private readonly KeyBinding _saveBinding;

    public ShortcutPanel()
    {
        _saveBinding = new KeyBinding
        {
            Gesture = new KeyGesture(Key.S, KeyModifiers.Control),
        };
        KeyBindings.Add(_saveBinding);

        // React to property changes to keep KeyBinding in sync
        SaveCommandProperty.Changed.AddClassHandler<ShortcutPanel>(
            (panel, args) =>
            {
                panel._saveBinding.Command = args.GetNewValue<ICommand?>();
            });
    }

    // Re-evaluate CanExecute when children change
    protected override void OnPropertyChanged(
        AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsEnabledProperty ||
            change.Property == IsVisibleProperty)
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
```

### Usage

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:ctl="using:ShortcutApp.Controls"
        x:Class="ShortcutApp.MainWindow">
  <ctl:ShortcutPanel SaveCommand="{Binding SaveDocumentCommand}">
    <TextBox Name="DocEditor" AcceptsReturn="True"
             Watermark="Type here... Ctrl+S to save" />
    <Button Content="Save (Ctrl+S)"
            Command="{Binding SaveDocumentCommand}" />
  </ctl:ShortcutPanel>
</Window>
```

### ViewModel

```csharp
public partial class DocumentViewModel : ObservableObject
{
    [ObservableProperty]
    private string _content = string.Empty;

    private RelayCommand? _saveCommand;
    public ICommand SaveDocumentCommand =>
        _saveCommand ??= new RelayCommand(
            execute: () => { /* save logic */ },
            canExecute: () => !string.IsNullOrWhiteSpace(Content)
        );

    partial void OnContentChanged(string value)
    {
        // RelayCommand with [NotifyCanExecuteChangedFor]
        // would handle this automatically, but for manual ICommand:
        CommandManager.InvalidateRequerySuggested();
    }
}
```

### Dynamic shortcut registration at runtime

```csharp
public void RegisterShortcut(KeyGesture gesture, ICommand command)
{
    // Avoid duplicate bindings for the same gesture
    var existing = KeyBindings.FirstOrDefault(
        kb => kb.Gesture?.Equals(gesture) == true);

    if (existing is not null)
    {
        existing.Command = command;
    }
    else
    {
        KeyBindings.Add(new KeyBinding
        {
            Gesture = gesture,
            Command = command,
        });
    }
}

public void UnregisterShortcut(KeyGesture gesture)
{
    var binding = KeyBindings.FirstOrDefault(
        kb => kb.Gesture?.Equals(gesture) == true);

    if (binding is not null)
        KeyBindings.Remove(binding);
}
```

### Key points

- `KeyBinding` created in code with explicit `KeyGesture` and `Command`
- `CommandManager.InvalidateRequerySuggested()` forces CanExecute re-evaluation
- Dynamic registration/unregistration at runtime via `KeyBindings` collection
- Custom control integrates `KeyBinding` as part of its API surface

---

## See Also

- [055 — Keyboard & Hotkeys (core tutorial)](055-keyboard-hotkeys.md)
- [055V — Keyboard & Hotkeys (verbose companion)](055-keyboard-hotkeys-verbose.md)
- [055Q — Keyboard & Hotkeys (quiz)](055-keyboard-hotkeys-quiz.md)
