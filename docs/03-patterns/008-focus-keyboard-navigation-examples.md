---
tier: advanced
topic: accessibility
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 008-focus-keyboard-navigation.md
---

# 008X — Focus & Keyboard Navigation: Real-World Examples

## Example 1: Data Entry Form with Tab Order and Validation Focus

A complex data entry form with logical tab order, focus trapping in a modal confirmation, and keyboard-driven validation.

### XAML Form

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MyApp.Views.PersonFormView">
  <StackPanel Spacing="8" Margin="16" KeyboardNavigation.TabNavigation="Local">
    <TextBlock Text="Personal Information" FontSize="18" FontWeight="SemiBold"
               KeyboardNavigation.IsTabStop="False" />

    <Label Target="FirstNameInput" Content="First Name:" />
    <TextBox Name="FirstNameInput"
             KeyboardNavigation.TabIndex="10"
             Watermark="Enter first name" />

    <Label Target="LastNameInput" Content="Last Name:" />
    <TextBox Name="LastNameInput"
             KeyboardNavigation.TabIndex="20"
             Watermark="Enter last name" />

    <Label Target="EmailInput" Content="Email:" />
    <TextBox Name="EmailInput"
             KeyboardNavigation.TabIndex="30"
             Watermark="email@example.com" />

    <Label Target="BirthDateInput" Content="Date of Birth:" />
    <DatePicker Name="BirthDateInput"
                KeyboardNavigation.TabIndex="40" />

    <Label Target="CountryInput" Content="Country:" />
    <ComboBox Name="CountryInput"
              KeyboardNavigation.TabIndex="50"
              Items="{Binding Countries}"
              SelectedItem="{Binding SelectedCountry}" />

    <StackPanel Orientation="Horizontal" Spacing="8"
                KeyboardNavigation.TabNavigation="Cycle"
                HorizontalAlignment="Right" Margin="0,16,0,0">
      <Button Content="Save" Command="{Binding SaveCommand}"
              KeyboardNavigation.TabIndex="60" />
      <Button Content="Cancel" Command="{Binding CancelCommand}"
              KeyboardNavigation.TabIndex="70" />
    </StackPanel>
  </StackPanel>
</UserControl>
```

### Code-Behind — Focus First Error

```csharp
public partial class PersonFormView : UserControl
{
    public PersonFormView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is PersonFormViewModel vm)
        {
            vm.FocusFirstError += OnFocusFirstError;
        }
    }

    private void OnFocusFirstError(string propertyName)
    {
        // Find the control bound to the errored property and focus it
        var control = propertyName switch
        {
            nameof(PersonFormViewModel.FirstName) => FirstNameInput,
            nameof(PersonFormViewModel.LastName) => LastNameInput,
            nameof(PersonFormViewModel.Email) => EmailInput,
            _ => null
        };
        control?.Focus();
    }
}

public sealed partial class PersonFormViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string _firstName = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string _lastName = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string _email = "";

    public event Action<string>? FocusFirstError;

    [RelayCommand]
    private void Save()
    {
        ValidateAll();
        if (HasErrors)
        {
            var firstError = GetErrors(null).Cast<object>().FirstOrDefault()?.ToString();
            FocusFirstError?.Invoke(firstError ?? "");
        }
    }

    private void ValidateAll()
    {
        // Validation logic
    }
}
```

---

## Example 2: Multi-Pane IDE with Arrow-Key Panel Navigation

A code editor split into three panels: Project Tree (left), Editor (center), Output (bottom). Arrow keys navigate between panels.

### Window Shell

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="MyApp.Views.IdeWindow"
        Title="Code Editor"
        Width="1200" Height="800">
  <Window.KeyBindings>
    <KeyBinding Gesture="Ctrl+N" Command="{Binding NewFileCommand}" />
    <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveFileCommand}" />
    <KeyBinding Gesture="Ctrl+Shift+F" Command="{Binding SearchInFilesCommand}" />
    <KeyBinding Gesture="F5" Command="{Binding RunCommand}" />
  </Window.KeyBindings>

  <DockPanel>
    <!-- Menu -->
    <Menu DockPanel.Dock="Top">
      <MenuItem Header="_File">
        <MenuItem Header="_New" Command="{Binding NewFileCommand}" InputGesture="Ctrl+N" />
        <MenuItem Header="_Open..." Command="{Binding OpenFileCommand}" InputGesture="Ctrl+O" />
        <Separator />
        <MenuItem Header="_Save" Command="{Binding SaveFileCommand}" InputGesture="Ctrl+S" />
      </MenuItem>
      <MenuItem Header="_Edit">
        <MenuItem Header="_Undo" Command="{Binding UndoCommand}" InputGesture="Ctrl+Z" />
        <MenuItem Header="_Redo" Command="{Binding RedoCommand}" InputGesture="Ctrl+Y" />
      </MenuItem>
      <MenuItem Header="_Search">
        <MenuItem Header="Search in Files" Command="{Binding SearchInFilesCommand}" InputGesture="Ctrl+Shift+F" />
      </MenuItem>
    </Menu>

    <!-- Left panel: Project Tree -->
    <GridSplitter DockPanel.Dock="Left" Width="3" />
    <ContentControl DockPanel.Dock="Left" Width="250"
                    Content="{Binding ProjectTreePanel}"
                    Focusable="True"
                    KeyDown="OnPanelKeyDown"
                    KeyboardNavigation.TabNavigation="Contained">
      <ContentControl.Styles>
        <Style Selector="ContentControl:focus">
          <Setter Property="BorderBrush" Value="{StaticResource SystemAccentColor}" />
          <Setter Property="BorderThickness" Value="1" />
        </Style>
      </ContentControl.Styles>
    </ContentControl>

    <!-- Center and bottom -->
    <DockPanel>
      <GridSplitter DockPanel.Dock="Bottom" Height="3" />
      <ContentControl DockPanel.Dock="Bottom" Height="200"
                      Content="{Binding OutputPanel}"
                      Focusable="True"
                      KeyDown="OnPanelKeyDown"
                      KeyboardNavigation.TabNavigation="Contained" />
      <ContentControl Content="{Binding EditorPanel}"
                      Focusable="True"
                      KeyDown="OnPanelKeyDown"
                      KeyboardNavigation.TabNavigation="Contained" />
    </DockPanel>
  </DockPanel>
</Window>
```

### Code-Behind — Arrow Key Panel Switching

```csharp
public partial class IdeWindow : Window
{
    private readonly List<Control> _panels = new();

    public IdeWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        // Collect panels in logical order
        _panels.Add(FindControl<ContentControl>("ProjectTreePanel"));
        _panels.Add(FindControl<ContentControl>("EditorPanel"));
        _panels.Add(FindControl<ContentControl>("OutputPanel"));
    }

    private void OnPanelKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not ContentControl current) return;

        int currentIndex = _panels.IndexOf(current);
        int targetIndex = -1;

        switch (e.Key)
        {
            case Key.Down:
                targetIndex = currentIndex + 1;
                break;
            case Key.Up:
                targetIndex = currentIndex - 1;
                break;
            case Key.Left when currentIndex == 0:
                // Already at leftmost — do nothing
                return;
            case Key.Right when currentIndex == _panels.Count - 1:
                // Already at rightmost — do nothing
                return;
        }

        if (targetIndex >= 0 && targetIndex < _panels.Count)
        {
            _panels[targetIndex].Focus();
            e.Handled = true;
        }
    }
}
```

---

## Example 3: Custom Toolbar with Roving Tab Stop

A drawing application toolbar where only the active tool receives tab focus. Arrow keys navigate between tools.

### ViewModel

```csharp
public sealed partial class DrawingToolBarViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ToolItem> _tools = new();

    [ObservableProperty]
    private int _activeIndex;

    [ObservableProperty]
    private string _activeToolName = "Pen";

    public DrawingToolBarViewModel()
    {
        Tools = new ObservableCollection<ToolItem>
        {
            new("Pen", "pen_icon.png"),
            new("Eraser", "eraser_icon.png"),
            new("Rectangle", "rect_icon.png"),
            new("Ellipse", "ellipse_icon.png"),
            new("Line", "line_icon.png"),
            new("Text", "text_icon.png"),
            new("Fill", "fill_icon.png"),
        };

        // Only the active tool is a tab stop
        UpdateTabStops();
    }

    private void UpdateTabStops()
    {
        for (int i = 0; i < Tools.Count; i++)
            Tools[i].IsTabStop = i == ActiveIndex;
    }

    [RelayCommand]
    private void ActivateTool(ToolItem tool)
    {
        var index = Tools.IndexOf(tool);
        if (index >= 0)
        {
            ActiveIndex = index;
            ActiveToolName = tool.Name;
            UpdateTabStops();
        }
    }

    [RelayCommand]
    private void NavigateTools(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Right:
                ActiveIndex = (ActiveIndex + 1) % Tools.Count;
                break;
            case Key.Left:
                ActiveIndex = (ActiveIndex - 1 + Tools.Count) % Tools.Count;
                break;
            case Key.Down:
                ActiveIndex = (ActiveIndex + 1) % Tools.Count;
                break;
            case Key.Up:
                ActiveIndex = (ActiveIndex - 1 + Tools.Count) % Tools.Count;
                break;
            default:
                return;
        }

        ActiveToolName = Tools[ActiveIndex].Name;
        UpdateTabStops();
        e.Handled = true;
    }
}

public sealed partial class ToolItem : ObservableObject
{
    public string Name { get; }
    public string IconPath { get; }

    [ObservableProperty]
    private bool _isTabStop;

    [ObservableProperty]
    private bool _isActive;

    public ToolItem(string name, string iconPath)
    {
        Name = name;
        IconPath = iconPath;
    }
}
```

### XAML Toolbar

```xml
<ToolBar KeyboardNavigation.TabNavigation="Once"
         KeyDown="OnToolBarKeyDown">
  <ItemsControl Items="{Binding Tools}"
                KeyboardNavigation.TabNavigation="Contained">
    <ItemsControl.ItemsPanel>
      <ItemsPanelTemplate>
        <StackPanel Orientation="Horizontal" />
      </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
      <DataTemplate DataType="vm:ToolItem">
        <ToggleButton Content="{Binding Name}"
                      IsChecked="{Binding IsActive}"
                      KeyboardNavigation.IsTabStop="{Binding IsTabStop}"
                      Command="{Binding $parent[ItemsControl].DataContext.ActivateToolCommand}"
                      CommandParameter="{Binding}"
                      ToolTip.Tip="{Binding Name}"
                      Margin="2" Padding="6,4" MinWidth="32" />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</ToolBar>
```

### Code-Behind

```csharp
public partial class DrawingToolBarView : UserControl
{
    public DrawingToolBarView()
    {
        InitializeComponent();
    }

    private void OnToolBarKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is DrawingToolBarViewModel vm)
            vm.NavigateToolsCommand.Execute(e);
    }
}
```

---

## Example 4: Modal Confirmation Dialog with Focus Trap

A custom modal dialog that traps focus within its boundary and closes on Escape.

### Dialog Content

```xml
<Window xmlns="https://github.com/avaloniaui"
        x:Class="MyApp.Views.ConfirmDialog"
        Title="{Binding Title}"
        SizeToContent="WidthAndHeight"
        WindowStartupLocation="CenterOwner"
        ShowInTaskbar="False"
        Topmost="True"
        PreviewKeyDown="OnPreviewKeyDown">
  <StackPanel Spacing="16" Margin="24" MinWidth="300"
              KeyboardNavigation.TabNavigation="Local">
    <TextBlock Text="{Binding Message}" TextWrapping="Wrap"
               FontSize="14" />
    <TextBlock Text="{Binding Detail}" TextWrapping="Wrap"
               FontSize="12" Opacity="0.7" />

    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8">
      <Button Name="YesButton" Content="Yes"
              KeyboardNavigation.TabIndex="10"
              Command="{Binding YesCommand}" />
      <Button Name="NoButton" Content="No"
              KeyboardNavigation.TabIndex="20"
              Command="{Binding NoCommand}" />
      <Button Name="CancelButton" Content="Cancel"
              KeyboardNavigation.TabIndex="30"
              Command="{Binding CancelCommand}" />
    </StackPanel>
  </StackPanel>
</Window>
```

### Code-Behind — Focus Trap

```csharp
public partial class ConfirmDialog : Window
{
    private readonly FocusTrap? _focusTrap;

    public ConfirmDialog()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        YesButton.Focus();
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(false);
            e.Handled = true;
        }

        // Trap Tab within the dialog
        if (e.Key == Key.Tab && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (FocusManager?.Current == CancelButton)
            {
                YesButton.Focus();
                e.Handled = true;
            }
        }

        if (e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (FocusManager?.Current == YesButton)
            {
                CancelButton.Focus();
                e.Handled = true;
            }
        }
    }
}
```

### Usage

```csharp
[RelayCommand]
private async Task DeleteProjectAsync()
{
    var dialog = new ConfirmDialog
    {
        DataContext = new ConfirmDialogViewModel
        {
            Title = "Delete Project",
            Message = "Are you sure you want to delete this project?",
            Detail = "This action cannot be undone.",
            YesCommand = new RelayCommand(() => dialog.Close(true)),
            NoCommand = new RelayCommand(() => dialog.Close(false)),
            CancelCommand = new RelayCommand(() => dialog.Close(false))
        }
    };

    var result = await dialog.ShowDialog<bool>(this);
    if (result)
        PerformDelete();
}
```
