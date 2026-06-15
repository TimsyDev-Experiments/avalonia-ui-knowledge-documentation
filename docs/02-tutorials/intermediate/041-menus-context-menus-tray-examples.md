---
tier: intermediate
topic: windowing
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 041-menus-context-menus-tray.md
---

# 041E — Menus, Context Menus, and System Tray: Real-World Examples

**What you'll learn:** Two complete scenarios covering a dynamic document editor menu system and a background service app with system tray and context menus.

**Prerequisites:** [041 — Menus, Context Menus, and System Tray](041-menus-context-menus-tray.md), [041V — Menus, Context Menus, and System Tray (verbose companion)](041-menus-context-menus-tray-verbose.md)

---

## Example 1: Document Editor with Dynamic Menu and Recent Files

### Goal

Build a document editor with a top-level menu that dynamically populates a "Recent Files" submenu from user settings, supports checkable toggles for view options, and registers platform-appropriate keyboard shortcuts.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class RecentFile
{
    public string Path { get; set; } = "";
    public string DisplayName => System.IO.Path.GetFileName(Path);
}

public partial class DocumentEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _documentText = "";

    [ObservableProperty]
    private string _currentFile = "";

    [ObservableProperty]
    private bool _showStatusBar = true;

    [ObservableProperty]
    private bool _wordWrap = true;

    [ObservableProperty]
    private ObservableCollection<RecentFile> _recentFiles = new();

    public string Title => string.IsNullOrEmpty(CurrentFile)
        ? "Untitled - Document Editor"
        : $"{System.IO.Path.GetFileName(CurrentFile)} - Document Editor";

    public DocumentEditorViewModel()
    {
        // Simulate loading recent files from settings
        RecentFiles = new ObservableCollection<RecentFile>
        {
            new() { Path = @"C:\docs\report.txt" },
            new() { Path = @"C:\docs\notes.txt" },
            new() { Path = @"C:\docs\README.md" },
        };
    }

    [RelayCommand]
    private void NewDocument()
    {
        DocumentText = "";
        CurrentFile = "";
        OnPropertyChanged(nameof(Title));
    }

    [RelayCommand]
    private async Task OpenDocument()
    {
        // In a real app, use StorageProvider.OpenFilePickerAsync
        CurrentFile = @"C:\docs\sample.txt";
        DocumentText = "Sample content...";
        OnPropertyChanged(nameof(Title));
    }

    [RelayCommand]
    private async Task SaveDocument()
    {
        if (string.IsNullOrEmpty(CurrentFile))
        {
            await SaveAsDocument();
            return;
        }
        // Save logic
    }

    [RelayCommand]
    private async Task SaveAsDocument()
    {
        // In a real app, use StorageProvider.SaveFilePickerAsync
    }

    [RelayCommand]
    private void OpenRecentFile(RecentFile? file)
    {
        if (file is null) return;
        CurrentFile = file.Path;
        DocumentText = "Loaded content...";
        OnPropertyChanged(nameof(Title));
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

    [RelayCommand]
    private void ShowAbout()
    {
        // Show about dialog
    }
}
```

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        xmlns:models="clr-namespace:MyApp.Models"
        x:DataType="vm:DocumentEditorViewModel"
        Title="{Binding Title}" Width="800" Height="500">
  <DockPanel>
    <Menu DockPanel.Dock="Top">
      <MenuItem Header="_File">
        <MenuItem Header="_New" Command="{Binding NewDocumentCommand}"
                  InputGesture="^N" />
        <MenuItem Header="_Open..." Command="{Binding OpenDocumentCommand}"
                  InputGesture="^O" />
        <MenuItem Header="_Save" Command="{Binding SaveDocumentCommand}"
                  InputGesture="^S" />
        <MenuItem Header="Save _As..." Command="{Binding SaveAsDocumentCommand}" />

        <!-- Dynamic recent files submenu -->
        <MenuItem Header="Recent _Files"
                  ItemsSource="{Binding RecentFiles}">
          <MenuItem.Icon>
            <PathIcon Data="{StaticResource ClockIcon}" />
          </MenuItem.Icon>
          <MenuItem.ItemTemplate>
            <DataTemplate x:DataType="models:RecentFile">
              <MenuItem Header="{Binding DisplayName}"
                        Command="{Binding $parent[Menu].DataContext.OpenRecentFileCommand}"
                        CommandParameter="{Binding}" />
            </DataTemplate>
          </MenuItem.ItemTemplate>
        </MenuItem>

        <Separator />
        <MenuItem Header="E_xit" Command="{Binding ExitCommand}"
                  InputGesture="Alt+F4" />
      </MenuItem>

      <MenuItem Header="_Edit">
        <MenuItem Header="_Undo" InputGesture="^Z" />
        <MenuItem Header="_Redo" InputGesture="^Y" />
        <Separator />
        <MenuItem Header="Cu_t" InputGesture="^X" />
        <MenuItem Header="_Copy" InputGesture="^C" />
        <MenuItem Header="_Paste" InputGesture="^V" />
      </MenuItem>

      <MenuItem Header="_View">
        <MenuItem Header="_Status Bar"
                  IsChecked="{Binding ShowStatusBar}"
                  ToggleType="CheckBox" />
        <MenuItem Header="_Word Wrap"
                  IsChecked="{Binding WordWrap}"
                  ToggleType="CheckBox" />
      </MenuItem>

      <MenuItem Header="_Help">
        <MenuItem Header="_About" Command="{Binding ShowAboutCommand}" />
      </MenuItem>
    </Menu>

    <TextBox AcceptsReturn="True"
             Text="{Binding DocumentText}"
             Margin="8" />

    <StatusBar DockPanel.Dock="Bottom"
               IsVisible="{Binding ShowStatusBar}">
      <TextBlock Text="Ready" />
    </StatusBar>
  </DockPanel>
</Window>
```

### How It Works

1. **Dynamic submenu with data binding** — The "Recent Files" `MenuItem` uses `ItemsSource="{Binding RecentFiles}"` and a `MenuItem.ItemTemplate` to render each `RecentFile` as a child `MenuItem`. The command binding uses `$parent[Menu].DataContext.OpenRecentFileCommand` to reach the window-level ViewModel because each item's `DataContext` is the `RecentFile` model, not the ViewModel.

2. **Checkable items** — "Status Bar" and "Word Wrap" use `IsChecked="{Binding ...}"` with `ToggleType="CheckBox"`. The bindings are `TwoWay` by default, so toggling the menu item updates the ViewModel property and the `StatusBar` visibility responds via `IsVisible="{Binding ShowStatusBar}"`.

3. **Cross-platform input gestures** — The `^` prefix maps to `Ctrl` on Windows/Linux and `Cmd` on macOS. `^S` becomes Ctrl+S on Windows, Cmd+S on macOS. The `Alt+F4` for Exit is Windows-specific; on macOS the Quit menu item is provided by the system's application menu.

4. **Dynamic title** — The window title binds to a computed `Title` property that updates when `CurrentFile` changes via `OnPropertyChanged(nameof(Title))` in the file commands.

5. **Icon on submenu header** — The "Recent Files" parent menu item has a `PathIcon` in its `Icon` slot, showing a clock icon next to the text.

### Design Decisions and Trade-offs

- **MenuItem.ItemTemplate vs building MenuItems in code**: Data-bound submenus are simpler for read-only lists. The trade-off is that `InputGesture` cannot be set inside a `DataTemplate` (it's not a bindable property). For recent files this is fine — keyboard shortcuts for recent files are uncommon.
- **$parent binding for command**: `$parent[Menu].DataContext` walks up the visual tree to find a `Menu` ancestor and accesses its `DataContext`. This is fragile if the menu structure changes (e.g., wrapping in another container). An alternative is to use `x:Reference` to name the window.
- **Separator placement**: The `Separator` between Save As and Recent Files is part of the static menu markup. If Recent Files is empty, the separator still shows. To hide it, bind `IsVisible` on the separator or build the menu programmatically.

---

## Example 2: Background Service App with System Tray and Context Menus

### Goal

Create a utility app that runs in the system tray, provides a context menu on log entries to copy or delete, and minimizes to tray instead of closing.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class LogEntry : ObservableObject
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "Info"; // Info, Warning, Error
    public string Message { get; set; } = "";
    public string Source { get; set; } = "";
}

public partial class BackgroundServiceViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LogEntry> _logEntries = new();

    [ObservableProperty]
    private LogEntry? _selectedLogEntry;

    [ObservableProperty]
    private bool _isRunning = true;

    [ObservableProperty]
    private string _statusText = "Service Running";

    private int _entryCounter;

    public BackgroundServiceViewModel()
    {
        // Seed with sample entries
        AddEntry("Info", "Service started", "BackgroundWorker");
        AddEntry("Info", "Monitoring directory: C:\logs\incoming", "FileWatcher");
    }

    private void AddEntry(string level, string message, string source)
    {
        _entryCounter++;
        LogEntries.Insert(0, new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            Source = source,
        });

        // Keep only last 1000 entries
        while (LogEntries.Count > 1000)
            LogEntries.RemoveAt(LogEntries.Count - 1);
    }

    [RelayCommand]
    private void CopyLogEntry(LogEntry? entry)
    {
        if (entry is null) return;
        var text = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message} ({entry.Source})";
        _ = CopyToClipboardAsync(text);
    }

    private async Task CopyToClipboardAsync(string text)
    {
        var topLevel = TopLevel.GetTopLevel(
            Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null);
        if (topLevel?.Clipboard is not null)
            await topLevel.Clipboard.SetTextAsync(text);
    }

    [RelayCommand]
    private void DeleteLogEntry(LogEntry? entry)
    {
        if (entry is not null)
            LogEntries.Remove(entry);
    }

    [RelayCommand]
    private void ClearAllLogs()
    {
        LogEntries.Clear();
    }

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
    private void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    private void StopService()
    {
        IsRunning = false;
        StatusText = "Service Stopped";
    }

    [RelayCommand]
    private void StartService()
    {
        IsRunning = true;
        StatusText = "Service Running";
    }
}
```

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        xmlns:models="clr-namespace:MyApp.Models"
        x:DataType="vm:BackgroundServiceViewModel"
        Title="Background Service Monitor" Width="700" Height="450">

  <!-- System tray icon -->
  <TrayIcon.Icons>
    <TrayIcons>
      <TrayIcon Icon="/Assets/app-icon.ico"
                ToolTipText="Background Service Monitor">
        <TrayIcon.Menu>
          <NativeMenu>
            <NativeMenuItem Header="Show Window"
                            Command="{Binding ShowWindowCommand}" />
            <NativeMenuItem Header="Start Service"
                            Command="{Binding StartServiceCommand}" />
            <NativeMenuItem Header="Stop Service"
                            Command="{Binding StopServiceCommand}" />
            <NativeMenuItemSeparator />
            <NativeMenuItem Header="Exit"
                            Command="{Binding ExitApplicationCommand}" />
          </NativeMenu>
        </TrayIcon.Menu>
      </TrayIcon>
    </TrayIcons>
  </TrayIcon.Icons>

  <DockPanel>
    <!-- Toolbar -->
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8" Margin="8">
      <Button Content="Start" Command="{Binding StartServiceCommand}" />
      <Button Content="Stop" Command="{Binding StopServiceCommand}" />
      <Button Content="Clear All" Command="{Binding ClearAllLogsCommand}" />
      <TextBlock Text="{Binding StatusText}"
                 VerticalAlignment="Center" Margin="16,0,0,0"
                 FontWeight="SemiBold" />
    </StackPanel>

    <!-- Log entries list with context menu -->
    <ListBox ItemsSource="{Binding LogEntries}"
             SelectedItem="{Binding SelectedLogEntry}">
      <ListBox.ItemTemplate>
        <DataTemplate x:DataType="models:LogEntry">
          <Grid ColumnDefinitions="Auto,Auto,*" Margin="2,0">
            <TextBlock Text="{Binding Timestamp, StringFormat='{0:HH:mm:ss}'}"
                       FontFamily="Consolas" Margin="0,0,8,0" />
            <TextBlock Grid.Column="1"
                       Text="{Binding Level}"
                       FontWeight="Bold"
                       Foreground="{Binding Level, Converter={StaticResource LogLevelToColorConverter}}"
                       Width="60" />
            <TextBlock Grid.Column="2"
                       Text="{Binding Message}"
                       TextTrimming="CharacterEllipsis" />
          </Grid>
          <DataTemplate.DataType>
            <x:Type TypeName="models:LogEntry" />
          </DataTemplate.DataType>
        </DataTemplate>
      </ListBox.ItemTemplate>

      <!-- Context menu on each log entry -->
      <ListBox.Styles>
        <Style Selector="ListBoxItem">
          <Setter Property="ContextMenu">
            <Setter.Value>
              <ContextMenu>
                <MenuItem Header="_Copy"
                          Command="{Binding $parent[ListBox].DataContext.CopyLogEntryCommand}"
                          CommandParameter="{Binding}" />
                <MenuItem Header="_Delete"
                          Command="{Binding $parent[ListBox].DataContext.DeleteLogEntryCommand}"
                          CommandParameter="{Binding}" />
                <Separator />
                <MenuItem Header="Clear _All"
                          Command="{Binding $parent[ListBox].DataContext.ClearAllLogsCommand}" />
              </ContextMenu>
            </Setter.Value>
          </Setter>
        </Style>
      </ListBox.Styles>
    </ListBox>

    <!-- Status bar -->
    <StatusBar DockPanel.Dock="Bottom">
      <TextBlock Text="{Binding StatusText}" />
      <Separator />
      <TextBlock Text="{Binding LogEntries.Count, StringFormat='{0} entries'}" />
    </StatusBar>
  </DockPanel>
</Window>
```

### Code-Behind (Minimize to Tray)

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (e.CloseReason == WindowCloseReason.User)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
```

### How It Works

1. **System tray with NativeMenu** — The `TrayIcon` is declared via the `TrayIcon.Icons` attached property on the Window. Its `Menu` is a `NativeMenu` with four items: Show Window, Start Service, Stop Service, and Exit. These commands reach the ViewModel via compiled bindings. The icon registers with the OS when the window is first shown.

2. **Minimize to tray** — `OnClosing` intercepts the close button (`WindowCloseReason.User`) and hides the window instead of closing. The application keeps running with the tray icon active. `ShowWindowCommand` calls `MainWindow.Show()` and `Activate()` to restore. `ExitApplicationCommand` calls `desktop.Shutdown()` for clean termination.

3. **Context menu on ListBox items** — A style setter on `ListBoxItem` assigns a `ContextMenu` with Copy, Delete, and Clear All options. The commands use `$parent[ListBox].DataContext` to reach the ViewModel because the `ContextMenu`'s DataContext would otherwise be the `LogEntry` item. The `CommandParameter="{Binding}"` passes the clicked log entry.

4. **Clipboard copy** — `CopyLogEntryCommand` formats the entry as a structured text line and writes it to the clipboard via `TopLevel.Clipboard.SetTextAsync`. The `TopLevel.GetTopLevel` helper retrieves the current window's top-level object to access platform services.

5. **Dynamic entry trimming** — `AddEntry` inserts new logs at index 0 (newest first) and removes entries beyond 1000 to cap memory usage. This prevents unbounded growth in a long-running background service.

### Design Decisions and Trade-offs

- **ContextMenu via style setter vs attached in DataTemplate**: A style setter applies the context menu uniformly to all `ListBoxItem` containers. If you need different menus per log level (e.g., Error entries get a "Show Details" option), use an `ItemTemplate` with conditional content or a `DataTemplateSelector`.
- **NativeMenu vs Menu for tray**: `TrayIcon` only accepts `NativeMenu`, not `Menu`. This is enforced at the API level. `NativeMenu` does not support `ItemsSource` binding, so all tray menu items must be declared statically in XAML or built programmatically in code-behind.
- **$parent[ListBox] binding**: The context menu items use `$parent[ListBox]` to escape the `ListBoxItem`'s data context. If you restructure the layout (e.g., wrapping in a `ScrollViewer`), the `$parent` path breaks. An alternative is to use `x:Reference` with a named `ListBox`.
- **WindowCloseReason filtering**: Only intercepting `WindowCloseReason.User` ensures that OS shutdown or application quit does not get blocked. The `WindowManagerClosing` reason should always proceed to close.

---

## Comparison: What Each Example Demonstrates

| Aspect | Example 1: Document Editor | Example 2: Background Service |
|--------|---------------------------|------------------------------|
| **Menu type** | Top-level `Menu` | `NativeMenu` in system tray, `ContextMenu` on list items |
| **Dynamic content** | Data-bound "Recent Files" submenu | Not needed (static tray menu) |
| **Checkable items** | Yes — ShowStatusBar, WordWrap | No |
| **Input gestures** | `^N`, `^O`, `^S` etc. | None |
| **Context menu** | Not used | Right-click on log entries |
| **System tray** | Not used | Full tray icon with menu |
| **Minimize to tray** | Not used | OnClosing interception |
| **Clipboard integration** | Not used | Copy log entry to clipboard |
| **Platform awareness** | `^` prefix for cross-platform modifiers | WindowCloseReason filtering for OS shutdown |
| **Key edge case** | Empty recent files shows orphan separator | 1000-entry cap prevents memory growth |

---

## See Also

- [041 — Menus, Context Menus, and System Tray](041-menus-context-menus-tray.md)
- [041V — Menus, Context Menus, and System Tray (verbose companion)](041-menus-context-menus-tray-verbose.md)
- [010 — Window Basics and Dialog Basics](../basics/010-window-dialog-basics.md)
- [035 — Custom Dialogs and Window Management](../advanced/035-custom-dialogs-window-management.md)
- [Avalonia Docs: TrayIcon](https://docs.avaloniaui.net/controls/navigation/trayicon)
- [Avalonia Docs: Menu](https://docs.avaloniaui.net/controls/navigation/menu)
- [Avalonia Docs: NativeMenu](https://docs.avaloniaui.net/controls/navigation/nativemenu)
