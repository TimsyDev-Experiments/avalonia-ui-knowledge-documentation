---
tier: intermediate
topic: windowing
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 041-menus-context-menus-tray.md
---

# 041V — Menus, Context Menus, and System Tray: An In-Depth Companion

**What you'll learn in this companion:** How Avalonia's menu system works across all three presentation layers — in-window `Menu`, attached `ContextMenu`, and OS-level `NativeMenu`/`TrayIcon`. Covers the event routing, platform differences, access key resolution, styling internals, and when each type is appropriate.

**Prerequisites:** [010 — Window Basics and Dialog Basics](../basics/010-window-dialog-basics.md), [011 — Compiled Bindings](011-compiled-bindings.md)

**You should already have read:** [041 — Menus, Context Menus, and System Tray](041-menus-context-menus-tray.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Avalonia's Three Menu Systems

Avalonia provides three distinct menu mechanisms, each with a different target and behavior:

| System | Control class | Target | Renders as | Best for |
|--------|---------------|--------|------------|----------|
| **Menu** | `Menu` + `MenuItem` | In-window toolbar | Styled XAML control | Cross-platform in-window menus |
| **ContextMenu** | `ContextMenu` + `MenuItem` | Attached to any control | Popup at cursor | Right-click context menus |
| **NativeMenu** | `NativeMenu` + `NativeMenuItem` | Window-level (.app-level on macOS) | OS-native menu bar | macOS global menu, system tray |

The `MenuItem` and `NativeMenuItem` classes are **not interchangeable**. `MenuItem` works inside `Menu` and `ContextMenu`. `NativeMenuItem` works inside `NativeMenu` and `TrayIcon.Menu`. Their APIs are similar but they inherit from different base classes and have different rendering pipelines.

---

## 2. Top-Level Menu Control: Structure and Behavior

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

### Why DockPanel with Dock="Top"

`Menu` inherits from `Control` and does not have its own layout logic for positioning. Placing it in a `DockPanel` with `DockPanel.Dock="Top"` makes it stretch horizontally across the window and occupy the top edge. The `TextBox` fills the remaining space because `DockPanel` gives the last child all leftover area.

Alternatives: `Grid` with `RowDefinitions`, or `DockPanel` with multiple docked children.

### Access keys (underscore prefix)

The underscore character `_` in `Header` marks the following character as an access key. `_File` marks `F` as the access key. On Windows/Linux, pressing `Alt+F` opens the File menu. The underlined character is rendered by the `Menu` control's template — it finds the character after `_` and applies `TextDecorations="Underline"` to it.

Rules:
- The `_` is removed from the displayed text
- Access keys are case-insensitive (`_File` and `_file` both match `Alt+F`)
- If two menu items at the same level have the same access key, the first one wins
- Access keys are **not** active when the menu is not focused — they require the menu to be in the focus scope

### Separator

`Separator` renders as a horizontal line. It is not a `MenuItem` subclass — it is a standalone `Control` that participates in the menu's layout. It has no command, no header, and cannot be focused or selected.

### InputGesture (keyboard shortcut)

```xml
<MenuItem Header="_Save" Command="{Binding SaveCommand}"
          InputGesture="Ctrl+S" />
```

`InputGesture` registers a `KeyBinding` with the window. When `Ctrl+S` is pressed, the command executes regardless of whether the menu is open. The gesture text is displayed right-aligned in the menu item's template (e.g., "Ctrl+S" appears at the right edge of the menu item).

The gesture works through the `KeyBinding` mechanism: the `Menu` creates a `KeyBinding` for each `MenuItem` that has an `InputGesture` and adds it to the window's `KeyBindings` collection.

---

## 3. Menu with Icons

```xml
<MenuItem Header="_Save">
  <MenuItem.Icon>
    <Image Source="avares://MyApp/Assets/save.png" Width="16" />
  </MenuItem.Icon>
</MenuItem>
```

The `Icon` property renders an element to the left of the header text. It is part of the default `MenuItem` template: a `ContentPresenter` named `PART_Icon` sits to the left of the header `ContentPresenter`. If `Icon` is null, the `PART_Icon` presenter is collapsed.

**Common mistake:** Forgetting the `avares://` URI scheme. Relative paths like `/Assets/save.png` work when the asset is in the same project, but `avares://MyApp/Assets/save.png` is the fully-qualified form. Use the `.csproj` asset configuration:

```xml
<ItemGroup>
  <AvaloniaResource Include="Assets\**" />
</ItemGroup>
```

---

## 4. Checkable Menu Items

```xml
<MenuItem Header="Show _Status Bar"
          IsChecked="{Binding ShowStatusBar}"
          ToggleType="CheckBox" />
```

`ToggleType` controls the visual representation:

| Value | Visual | Behavior |
|-------|--------|----------|
| `CheckBox` | Check mark (✓) next to the header | Toggle on click; remains checked until unchecked |
| `Radio` | Radio bullet (•) | Toggle on click; typically used in a group where only one can be active |

Radio behavior requires manual management — Avalonia does not auto-exclusive radio menu items. You must set `IsChecked` on the correct item and clear others via bindings or code.

`IsChecked` must be a `TwoWay` binding if you want the viewmodel to reflect the toggle state. `OneWay` shows the state but does not propagate changes.

---

## 5. ContextMenu: Attached vs Programmatic

### Attached in XAML

```xml
<TextBlock Text="Right-click me">
  <TextBlock.TextContextMenu>
    <ContextMenu>
      <MenuItem Header="Cu_t" Command="{Binding CutCommand}" />
      <MenuItem Header="_Copy" Command="{Binding CopyCommand}" />
      <MenuItem Header="_Paste" Command="{Binding PasteCommand}" />
    </ContextMenu>
  </TextBlock.TextContextMenu>
</TextBlock>
```

Note the property: `TextContextMenu` (not `ContextMenu`). The attached property is named `ContextMenu` on the `Control` class, but in XAML you access it as `TextBlock.TextContextMenu` because the property is registered by the `TextBlock` class via `ContextMenu` attached property.

Wait — actually, in Avalonia, `ContextMenu` is an attached property on `Control`. The correct XAML form is:

```xml
<TextBlock Text="Right-click me"
           ContextMenu="{Binding ...}" />
```

Or with inline definition:

```xml
<TextBlock Text="Right-click me">
  <TextBlock.ContextMenu>
    <ContextMenu>
      <MenuItem Header="_Copy" Command="{Binding CopyCommand}" />
    </ContextMenu>
  </TextBlock.ContextMenu>
</TextBlock>
```

The `ContextMenu` control appears as a popup at the mouse cursor position when the user right-clicks (or performs the platform equivalent gesture).

### Programmatic opening

```csharp
myControl.ContextMenu?.Open(myControl);
```

`ContextMenu.Open(Control)` takes the placement target — the control that the menu is positioned relative to. The menu opens at the cursor position, not at the control's location. If you want to open at a specific point, use the overload:

```csharp
menu.Open(myControl, new Point(x, y));
```

### Dynamic construction

```csharp
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
```

When building `ContextMenu` programmatically, remember:
- `MenuItem.Command` accepts an `ICommand` — use `RelayCommand` instances
- The menu's lifetime is managed by the popup; it closes when the user clicks elsewhere
- Each `MenuItem` gets `DataContext` from the `ContextMenu`'s `DataContext`, which defaults to the placement target's `DataContext`

### ContextMenu vs Popup

`ContextMenu` is a specialized `Popup` with menu behavior (click to dismiss, keyboard navigation, selection highlighting). For non-menu popups (tooltips, custom dropdowns, pickers), use `Popup` directly. `ContextMenu` adds: arrow key navigation, hover expand for submenus, automatic positioning at cursor, and dismiss on click outside.

---

## 6. NativeMenu: OS-Native Menus

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

### What makes NativeMenu "native"

`NativeMenu` does not render controls in the Avalonia visual tree. Instead, it:

1. **On macOS:** Translates the menu structure into Cocoa `NSMenu`/`NSMenuItem` objects that appear in the global menu bar at the top of the screen. Keyboard shortcuts are registered with the OS and work even when the window is in the background.
2. **On Windows/Linux:** Falls back to rendering an in-window menu bar using the same visual template as `Menu`, but driven by the `NativeMenu` API. It looks like a regular menu but the API surface is `NativeMenu`/`NativeMenuItem`.

### Why use NativeMenu

If your app targets macOS, **use NativeMenu**. macOS users expect the global menu bar. Without NativeMenu, you get an in-window menu (like a Windows app running on macOS), which feels foreign and breaks the platform convention.

On Windows/Linux, `NativeMenu` renders identically to `Menu`. The difference is only in the API. Choose based on whether you want platform-native behavior on macOS.

### NativeMenu vs Menu API differences

| Aspect | Menu | NativeMenu |
|--------|------|------------|
| Item class | `MenuItem` | `NativeMenuItem` |
| Separator | `Separator` | `NativeMenuItemSeparator` |
| Binding | `Command`, `ItemsSource` | `Command` only (no `ItemsSource`) |
| Submenus | `MenuItem.Items` | `NativeMenuItem.Menu` (another `NativeMenu`) |
| Checkable | `IsChecked` + `ToggleType` | `IsChecked` only |
| Icons | `MenuItem.Icon` | Supported via `NativeMenuItem.Icon` |

NativeMenuItem submenus:

```xml
<NativeMenu>
  <NativeMenuItem Header="Recent Files">
    <NativeMenuItem.Menu>
      <NativeMenu>
        <NativeMenuItem Header="file1.txt" Command="{Binding OpenRecentCommand}"
                        CommandParameter="file1.txt" />
      </NativeMenu>
    </NativeMenuItem.Menu>
  </NativeMenuItem>
</NativeMenu>
```

---

## 7. System Tray Icon

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

### What is TrayIcon

`TrayIcon` adds an icon to the operating system's notification area (system tray on Windows, menu bar extra on macOS, notification area on Linux DE). It is not a visual element rendered in the Avalonia window — it is an OS-level icon managed through platform-specific APIs (Shell_NotifyIcon on Windows, NSStatusBar on macOS, StatusNotifier on Linux).

Because `TrayIcon` lives outside the window, it cannot be declared inside the window's content area. It is attached to the window via the `TrayIcon.Icons` attached property, but the actual icon is registered with the OS when the window is shown.

### Platform-specific behavior

| Platform | Behavior |
|----------|----------|
| **Windows** | Icon appears in notification area (bottom-right). Left-click typically shows a context menu (not configurable in all shells). Right-click shows the NativeMenu. |
| **macOS** | Icon appears in the menu bar (top-right). Must be a template PNG (monochrome, 16x16 or 18x18). Single-click opens the NativeMenu. |
| **Linux** | Depends on DE. GNOME/KDE with StatusNotifier: icon in notification area. Without StatusNotifier: may not appear at all. libappindicator required on some distros. |

### Minimize to tray pattern

```csharp
protected override void OnClosing(WindowClosingEventArgs e)
{
    if (e.CloseReason == WindowCloseReason.User)
    {
        e.Cancel = true;
        Hide();
    }
    base.OnClosing(e);
}
```

This intercepts the close button (window close button, Alt+F4, Cmd+W). Instead of closing, it hides the window. The application stays running with the tray icon visible. The user can exit via the tray menu's "Exit" command, which calls `desktop.Shutdown()`.

**Important:** `WindowCloseReason.User` vs other reasons:
- `User` — close button, Alt+F4, Cmd+W (intercept for minimize-to-tray)
- `WindowManagerClosing` — OS shutdown, session end (do not block)
- `Other` — programmatic Close() call (check your app's logic)

### ViewModel tray commands

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

`ShowWindow` calls `Show()` (makes the window visible) and `Activate()` (brings it to the foreground). On macOS, `Activate()` also brings the app to the front via `NSApplication.ActivateIgnoringOtherApps`.

`Exit` calls `desktop.Shutdown()` which fires `Closing` on all windows, then exits the application. This is the only clean way to shut down an Avalonia application — calling `Environment.Exit` or `Application.Current.Shutdown()` skips the window closing lifecycle.

---

## 8. Keyboard Navigation in Menus

### Access keys

```xml
<MenuItem Header="_Format">
  <MenuItem Header="_Word Wrap" />
  <MenuItem Header="_Font..." />
</MenuItem>
```

Access keys work when the menu is in the focus scope. Pressing `Alt` highlights the menu bar (shows underline indicators). Then pressing the access key opens that menu. Once the menu is open, pressing another access key activates that item.

**macOS note:** Access keys are replaced by the global menu bar search (`Cmd+?` or Help menu search). The `_` prefix is ignored on macOS because the menu bar is rendered natively and uses Cocoa's keyboard shortcut system.

### Input gesture resolution order

When a key combination is pressed:

1. Check focused control's `KeyBindings` (e.g., `TextBox` with custom `KeyBinding`)
2. Check the focused window's `KeyBindings` (includes `Menu`-registered `InputGesture` bindings)
3. Check `Application`'s `KeyBindings`

If multiple bindings match the same gesture, the first one found wins. This means a `TextBox`-level `Ctrl+S` binding can override the menu's save command.

### Platform-appropriate shortcuts

```xml
<MenuItem Header="_Copy" Command="{Binding CopyCommand}"
          InputGesture="Ctrl+C" />
```

On macOS, `Cmd+C` is the standard copy shortcut. Avalonia translates `Ctrl` to `Cmd` on macOS automatically **in the InputGesture text display** but not in the `KeyBinding`. You must detect the platform or use the macOS-specific macro:

```xml
<!-- This works on all platforms, showing the correct modifier key -->
<MenuItem Header="_Copy" Command="{Binding CopyCommand}"
          InputGesture="^C" />  <!-- ^ maps to Ctrl on Win/Linux, Cmd on macOS -->
```

The `^` prefix for `InputGesture` is Avalonia's notation for the platform-standard command modifier (Ctrl on Windows/Linux, Cmd on macOS). Use it for standard shortcuts.

---

## 9. Dynamic / Data-Bound Menus

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

```csharp
public class MenuItemModel
{
    public string Header { get; set; } = "";
    public ICommand? Command { get; set; }
    public ObservableCollection<MenuItemModel> Children { get; set; } = new();
}
```

### How data-bound menus work

The `Menu` is an `ItemsControl` that generates `MenuItem` containers for each item in `ItemsSource`. The `ItemTemplate` tells it how to configure each `MenuItem`. The `ItemsSource` binding on the `MenuItem` itself creates child submenus recursively.

This pattern works for any depth of nesting — each `MenuItem` that has `Children` items will generate another level of submenu items using the same `ItemTemplate`.

**Limitations:**
- `CommandParameter` cannot be set in the template because the binding context is the item itself
- `InputGesture` is not data-bound in this pattern (it's not a bindable property on `MenuItem`)
- `IsChecked` for checkable items requires a model property

For a more flexible dynamic menu approach, build the `Menu` tree programmatically in code-behind or use a custom `IDataTemplate` selector.

---

## 10. Menu Styling: Selector Targeting

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

### Why /template/ selector

The `/template/` syntax traverses into the `MenuItem`'s control template to target a named part (`Border#root`). This is necessary because the hover/selected background is not applied to the `MenuItem` itself but to a `Border` inside its template. Setting `Background` on the `MenuItem` directly might not produce visible results depending on the template structure.

### Pseudo-classes used

| Pseudo-class | When it applies |
|--------------|-----------------|
| `:pointerover` | Mouse is hovering over the item |
| `:selected` | Item is highlighted via keyboard navigation (arrow keys) |
| `:open` | Submenu is open |
| `:disabled` | Command's `CanExecute` is false |

### Popup submenu styling

```xml
<Style Selector="MenuItem:open">
  <Setter Property="Foreground" Value="White" />
</Style>
```

The `:open` pseudo-class applies when the menu item's submenu (or its own popup) is visible. This lets you style the "parent" item while its child menu is open.

### Global vs scoped menu styling

Styles defined in `<Menu.Styles>` apply only to `MenuItem` elements inside that specific `Menu`. For application-wide menu styling, define styles in `Application.Styles` with a selector like `Menu MenuItem` or a named style.

```xml
<Application.Styles>
  <Style Selector="Menu MenuItem">
    <Setter Property="FontSize" Value="14" />
  </Style>
</Application.Styles>
```

This applies to all `MenuItem` elements that are descendants of a `Menu` anywhere in the app.

---

## 11. NativeMenu Limitations and Gotchas

### No ItemsSource binding

`NativeMenu` does not support `ItemsSource`. You cannot data-bind a list of viewmodels to a `NativeMenu`. If you need dynamic native menus, you must build them programmatically:

```csharp
var nativeMenu = new NativeMenu();
foreach (var item in recentFiles)
{
    nativeMenu.Add(new NativeMenuItem
    {
        Header = item.Name,
        Command = OpenRecentFileCommand,
        CommandParameter = item.Path
    });
}
```

### No template support

There is no `ItemTemplate` equivalent for `NativeMenu`. Each `NativeMenuItem` must be created explicitly.

### Different click behavior on macOS

On macOS, clicking a `NativeMenuItem` in the global menu bar does not trigger the `Click` event the same way as `MenuItem`. The command is invoked immediately when the item is selected (not on mouse release). This is standard macOS behavior.

---

## 12. TrayIcon: Multiple Icons and Dynamic Updates

### Multiple icons

`<TrayIcons>` can contain multiple `TrayIcon` elements. Each registers a separate icon in the system tray. Use this for multi-window apps where each window has its own tray presence, or for status indicators.

### Dynamic icon update

```csharp
var currentIcon = TrayIcon.GetIcons(this)?.FirstOrDefault();
if (currentIcon is not null)
{
    currentIcon.Icon = new WindowIcon(Assets.Open("online.png"));
}
```

Or via binding: the `Icon` property on `TrayIcon` is a direct property (not styled), so it can be bound to a viewmodel property.

### Click event

```csharp
trayIcon.Clicked += (_, args) =>
{
    // args is TrayIconClickedEventArgs
    // Check args.ClickType for Single, Double, or context menu clicks
};
```

The `Clicked` event fires on left-click (or single-click on macOS). The `ClickType` distinguishes between single-click, double-click, and platform-specific secondary-click.

---

## Key Takeaways

- Avalonia has three menu systems: `Menu` (in-window), `ContextMenu` (right-click popup), and `NativeMenu` (OS-native, global menu bar on macOS)
- Access keys (`_File`) work on Windows/Linux; on macOS they are replaced by the global menu bar search
- `InputGesture` registers keyboard shortcuts via the `KeyBinding` system — use `^` prefix for cross-platform modifier key mapping
- `NativeMenu` is required for macOS global menu bar; on Windows/Linux it renders like `Menu` but has a different API with no `ItemsSource` support
- `TrayIcon` is an OS-level element — it lives outside the window and requires platform-specific icon formats
- The minimize-to-tray pattern intercepts `OnClosing` for `WindowCloseReason.User` and hides the window instead
- Data-bound menus use `ItemsSource` + `ItemTemplate` with recursive `ItemsSource` binding on `MenuItem` for submenus
- Menu styling targets template parts via `/template/` selectors and uses pseudo-classes `:pointerover`, `:selected`, `:open`, `:disabled`

---

## See Also

- [041 — Menus, Context Menus, and System Tray (original quick-start)](041-menus-context-menus-tray.md)
- [041E — Menus, Context Menus, and System Tray (examples)](041-menus-context-menus-tray-examples.md)
- [010 — Window Basics and Dialog Basics](../basics/010-window-dialog-basics.md)
- [035 — Custom Dialogs and Window Management](../advanced/035-custom-dialogs-window-management.md)
- [019 — Drag and Drop](019-drag-drop.md)
- [Avalonia Docs: TrayIcon](https://docs.avaloniaui.net/controls/navigation/trayicon)
- [Avalonia Docs: Menu](https://docs.avaloniaui.net/controls/navigation/menu)
- [Avalonia Docs: NativeMenu](https://docs.avaloniaui.net/controls/navigation/nativemenu)
