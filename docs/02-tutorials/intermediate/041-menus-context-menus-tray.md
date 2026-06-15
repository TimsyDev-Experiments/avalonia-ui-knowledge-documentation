---
tier: intermediate
topic: windowing
estimated: 15 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 041 -- Menus, Context Menus, and System Tray

**What you'll learn:** How to build top-level menus, context menus, native OS menus, and system tray integration with icon and popup menu.

**Prerequisites:** [001 -- Project Setup](../basics/001-project-setup.md), [010 -- Window Basics](../basics/010-window-dialog-basics.md)

---

## 1. Top-level Menu control

```xml
<Window xmlns="https://github.com/avaloniaui"
        Title="Menu Demo" Width="600" Height="400">
  <DockPanel>
    <Menu DockPanel.Dock="Top">
      <MenuItem Header="_File">
        <MenuItem Header="_New" Command="{Binding NewCommand}" />
        <MenuItem Header="_Open..." Command="{Binding OpenCommand}" />
        <Separator />
        <MenuItem Header="_Save" Command="{Binding SaveCommand}"
                  InputGesture="Ctrl+S" />
        <MenuItem Header="Save _As..." Command="{Binding SaveAsCommand}" />
        <Separator />
        <MenuItem Header="E_xit" Command="{Binding ExitCommand}" />
      </MenuItem>
      <MenuItem Header="_Edit">
        <MenuItem Header="_Undo" Command="{Binding UndoCommand}"
                  InputGesture="Ctrl+Z" />
        <MenuItem Header="_Redo" Command="{Binding RedoCommand}"
                  InputGesture="Ctrl+Y" />
        <Separator />
        <MenuItem Header="Cu_t" Command="{Binding CutCommand}"
                  InputGesture="Ctrl+X" />
        <MenuItem Header="_Copy" Command="{Binding CopyCommand}"
                  InputGesture="Ctrl+C" />
        <MenuItem Header="_Paste" Command="{Binding PasteCommand}"
                  InputGesture="Ctrl+V" />
      </MenuItem>
      <MenuItem Header="_Help">
        <MenuItem Header="_About" Command="{Binding AboutCommand}" />
      </MenuItem>
    </Menu>

    <TextBox AcceptsReturn="True"
             Text="{Binding Document}" />
  </DockPanel>
</Window>
```

The underscore prefix (`_File`) creates an access key underlined in the UI. Pressing `Alt+F` opens the File menu.

### Menu with icons

```xml
<MenuItem Header="_Save">
  <MenuItem.Icon>
    <Image Source="avares://MyApp/Assets/save.png" Width="16" />
  </MenuItem.Icon>
</MenuItem>
```

### Checkable menu items

```xml
<MenuItem Header="Show _Status Bar"
          IsChecked="{Binding ShowStatusBar}"
          ToggleType="CheckBox" />
```

`ToggleType` values: `CheckBox` (default), `Radio`.

---

## 2. ContextMenu (right-click)

Attached to any control:

```xml
<TextBlock Text="Right-click me">
  <TextBlock.ContextMenu>
    <ContextMenu>
      <MenuItem Header="Cu_t" Command="{Binding CutCommand}" />
      <MenuItem Header="_Copy" Command="{Binding CopyCommand}" />
      <MenuItem Header="_Paste" Command="{Binding PasteCommand}" />
    </ContextMenu>
  </TextBlock.ContextMenu>
</TextBlock>
```

Open programmatically:

```csharp
myControl.ContextMenu?.Open(myControl);
```

Build dynamically:

```csharp
public partial class DynamicMenuViewModel : ObservableObject
{
    [RelayCommand]
    private void ShowContextMenu(Control target)
    {
        var menu = new ContextMenu();

        foreach (var item in AvailableActions)
        {
            var menuItem = new MenuItem
            {
                Header = item.Name,
                Command = item.Action
            };
            menu.Items.Add(menuItem);
        }

        menu.Open(target);
    }
}
```

---

## 3. NativeMenu (OS-native menus)

On macOS, `NativeMenu` renders as the global menu bar at the top of the screen. On Windows and Linux, it renders as an in-window menu similar to `Menu`.

```xml
<Window xmlns="https://github.com/avaloniaui">
  <NativeMenu.Menu>
    <NativeMenu>
      <NativeMenuItem Header="About MyApp"
                      Command="{Binding AboutCommand}" />
      <NativeMenuItemSeparator />
      <NativeMenuItem Header="Preferences..."
                      Command="{Binding PreferencesCommand}"
                      InputGesture="Ctrl+," />
    </NativeMenu>
  </NativeMenu.Menu>
</Window>
```

### Platform behavior

| Platform | Rendering | Keyboard |
|----------|-----------|----------|
| **macOS** | Global menu bar | Menu keyboard shortcuts work globally |
| **Windows** | In-window menu bar | Shortcuts work when window is focused |
| **Linux** | In-window menu bar | Shortcuts work when window is focused |

NativeMenu is the recommended approach for apps that target macOS because it produces the expected global menu bar experience.

---

## 4. System tray icon

```xml
<Window xmlns="https://github.com/avaloniaui">
  <TrayIcon.Icons>
    <TrayIcons>
      <TrayIcon Icon="/Assets/app-icon.ico"
                ToolTipText="My Application">
        <TrayIcon.Menu>
          <NativeMenu>
            <NativeMenuItem Header="Show Window"
                            Command="{Binding ShowWindowCommand}" />
            <NativeMenuItemSeparator />
            <NativeMenuItem Header="Exit"
                            Command="{Binding ExitCommand}" />
          </NativeMenu>
        </TrayIcon.Menu>
      </TrayIcon>
    </TrayIcons>
  </TrayIcon.Icons>
</Window>
```

### Code-behind tray management

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Minimize to tray instead of closing
        if (e.CloseReason == WindowCloseReason.User)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
```

ViewModel handlers:

```csharp
[RelayCommand]
private void ShowWindow()
{
    if (Application.Current?.ApplicationLifetime
        is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow?.Show();
        desktop.MainWindow?.Activate();
    }
}

[RelayCommand]
private void Exit()
{
    if (Application.Current?.ApplicationLifetime
        is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.Shutdown();
    }
}
```

### Platform support for TrayIcon

| Platform | Support | Icon format |
|----------|---------|-------------|
| Windows | Full | `.ico` |
| macOS | Menu bar | Template PNG (monochrome) |
| Linux | DE-dependent | PNG (libappindicator / StatusNotifier) |
| Browser | Not supported | — |
| Android/iOS | Not supported | — |

---

## 5. Keyboard navigation in menus

Access keys (`_F` for File menu):

```xml
<MenuItem Header="_Format">
  <MenuItem Header="_Word Wrap" />
  <MenuItem Header="_Font..." />
</MenuItem>
```

Pressing `Alt+F` opens the Format menu if there's no collision. On macOS, access keys are replaced by the global menu bar search.

Input gestures (keyboard shortcuts):

```xml
<MenuItem Header="_Save" Command="{Binding SaveCommand}"
          InputGesture="Ctrl+S" />
```

Standard shortcuts should follow platform conventions:

| Action | Windows/Linux | macOS |
|--------|--------------|-------|
| Copy | `Ctrl+C` | `Cmd+C` |
| Paste | `Ctrl+V` | `Cmd+V` |
| Save | `Ctrl+S` | `Cmd+S` |
| Open | `Ctrl+O` | `Cmd+O` |

---

## 6. Dynamic / data-bound menus

```xml
<Menu ItemsSource="{Binding MenuItems}">
  <Menu.ItemTemplate>
    <DataTemplate x:DataType="models:MenuItemModel">
      <MenuItem Header="{Binding Header}"
                Command="{Binding Command}"
                ItemsSource="{Binding Children}" />
    </DataTemplate>
  </Menu.ItemTemplate>
</Menu>
```

Model:

```csharp
public class MenuItemModel
{
    public string Header { get; set; } = "";
    public ICommand? Command { get; set; }
    public ObservableCollection<MenuItemModel> Children { get; set; } = new();
}
```

---

## 7. Menu styling

```xml
<Menu Background="#2D2D2D">
  <Menu.Styles>
    <Style Selector="MenuItem">
      <Setter Property="Foreground" Value="White" />
      <Setter Property="Background" Value="Transparent" />
    </Style>
    <Style Selector="MenuItem:pointerover /template/ Border#root">
      <Setter Property="Background" Value="#404040" />
    </Style>
    <Style Selector="MenuItem:selected /template/ Border#root">
      <Setter Property="Background" Value="#505050" />
    </Style>
  </Menu.Styles>
</Menu>
```

Popup submenu styling:

```xml
<Style Selector="MenuItem:open">
  <Setter Property="Foreground" Value="White" />
</Style>
```

## Key takeaways

- Use `Menu` + `MenuItem` for top-level menus with access keys (`_File`)
- `ContextMenu` attaches to any control for right-click menus
- `NativeMenu` is the recommended approach — renders as global menu bar on macOS
- `TrayIcon` with `NativeMenu` provides system tray integration (desktop only)
- `InputGesture` attaches keyboard shortcuts to menu items
- Bind menu items to data for dynamic menus
- Style menus with selectors targeting `MenuItem` and its named parts

---

## See Also

- [041V — Menus, Context Menus, and System Tray (verbose companion)](041-menus-context-menus-tray-verbose.md)
- [041E — Menus, Context Menus, and System Tray (examples)](041-menus-context-menus-tray-examples.md)
- [010 — Window Basics and Dialog Basics](../basics/010-window-dialog-basics.md)
- [035 — Custom Dialogs and Window Management](../advanced/035-custom-dialogs-window-management.md)
- [Avalonia Docs: TrayIcon](https://docs.avaloniaui.net/controls/navigation/trayicon)
- [Avalonia Docs: Menu](https://docs.avaloniaui.net/controls/navigation/menu)
