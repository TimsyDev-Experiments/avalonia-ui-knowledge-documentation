---
tier: intermediate
topic: focus
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 054-focus-management.md
---

# 054E — Focus Management: Real-World Examples

**What this is:** Two worked examples showing focus management in real app scenarios. Read [054 — Focus Management](054-focus-management.md) and [054V — Verbose Companion](054-focus-management-verbose.md) first.

---

## Example 1: Login Form with Auto-Focus and Validation Focus Trapping

### Goal

Build a login form that:
- Auto-focuses the username field on load
- Traps focus within the form fields while valid (Tab cycles User → Password → Login → back to User)
- Shows a focus indicator on the active field
- Validates and prevents leaving if fields are empty

### View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="LoginApp.Views.LoginForm">
  <Border Background="{StaticResource SurfaceBrush}"
          CornerRadius="8" Padding="24" Width="320">
    <StackPanel Spacing="12">
      <TextBlock Text="Sign In" FontSize="20" FontWeight="Bold" />

      <TextBlock Text="Username" />
      <TextBox Name="UserNameBox" Watermark="Enter your username"
               TabIndex="1" />

      <TextBlock Text="Password" />
      <TextBox Name="PasswordBox" Watermark="Enter your password"
               PasswordChar="*" TabIndex="2" />

      <Button Name="LoginButton" Content="Sign In"
              TabIndex="3" />

      <TextBlock Name="StatusText" Foreground="Red" />
    </StackPanel>
  </Border>
</UserControl>
```

### Code-behind

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Linq;

namespace LoginApp.Views;

public partial class LoginForm : UserControl
{
    private readonly IInputElement[] _fields;

    public LoginForm()
    {
        InitializeComponent();

        _fields = new IInputElement[]
        {
            UserNameBox,
            PasswordBox,
            LoginButton,
        };

        // Subscribe to GotFocus to update focus indicator
        AddHandler(InputElement.GotFocusEvent, OnAnyGotFocus);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        UserNameBox.Focus();
    }

    private void OnAnyGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (e.Source is Control c)
        {
            // Update status bar with focused control name
            StatusText.Text = $"Focused: {c.Name ?? c.GetType().Name}";
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Enter)
        {
            // Enter acts like Tab for the last field
            if (e.Source == LoginButton)
            {
                AttemptLogin();
                e.Handled = true;
            }
            else
            {
                // Move to next field
                var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
                focusManager?.TryMoveFocus(NavigationDirection.Next);
                e.Handled = true;
            }
        }
    }

    private void AttemptLogin()
    {
        if (string.IsNullOrWhiteSpace(UserNameBox.Text) ||
            string.IsNullOrWhiteSpace(PasswordBox.Text))
        {
            StatusText.Text = "Please fill in all fields";
            UserNameBox.Focus();
            return;
        }

        StatusText.Text = "Logging in...";
        // Perform login...
    }
}
```

### Key points

- `OnLoaded` auto-focuses the username field
- `GotFocusEvent` handler updates the status bar with the focused control name
- Enter key advances focus via `TryMoveFocus`
- The `TabIndex` values (1, 2, 3) ensure Tab cycles through fields in the correct order

---

## Example 2: Focus-Aware Search Panel with Escape-to-Clear

### Goal

Build a search panel that:
- Highlights when any child has focus (`:focus-within` style)
- Clears focus when Escape is pressed
- Shows the previous focused element when the panel loses focus
- Uses `GettingFocusEvent` to prevent focusing a disabled control

### View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="SearchApp.Controls.SearchPanel">
  <Border Name="PanelBorder" CornerRadius="6" Padding="12"
          BorderBrush="Transparent" BorderThickness="2">
    <StackPanel Spacing="8">
      <TextBlock Text="Search" FontSize="14" FontWeight="SemiBold" />
      <TextBox Name="SearchBox" Watermark="Type to search..."
               TabIndex="1" />
      <StackPanel Orientation="Horizontal" Spacing="8">
        <Button Name="SearchButton" Content="Go" TabIndex="2" />
        <Button Name="ClearButton" Content="Clear" TabIndex="3" />
      </StackPanel>
      <ListBox Name="ResultsList" Height="200"
               TabIndex="4" IsVisible="False" />
    </StackPanel>
  </Border>
</UserControl>
```

### Code-behind

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;

namespace SearchApp.Controls;

public partial class SearchPanel : UserControl
{
    private IInputElement? _previousFocus;

    public ObservableCollection<string> Results { get; } = new();

    public SearchPanel()
    {
        InitializeComponent();
        ResultsList.Items = Results;

        // Highlight panel when any child is focused
        AddHandler(InputElement.GotFocusEvent, OnChildGotFocus);
        AddHandler(InputElement.LostFocusEvent, OnChildLostFocus);

        // Prevent focusing the search button when disabled
        AddHandler(
            InputElement.GettingFocusEvent,
            OnGettingFocus,
            RoutingStrategies.Tunnel);
    }

    private void OnChildGotFocus(object? sender, GotFocusEventArgs e)
    {
        PanelBorder.BorderBrush = Avalonia.Media.Brushes.DodgerBlue;
        _previousFocus = e.Source as IInputElement;
    }

    private void OnChildLostFocus(object? sender, LostFocusEventArgs e)
    {
        // Check if focus moved to something outside the panel
        var topLevel = TopLevel.GetTopLevel(this);
        var focused = topLevel?.FocusManager?.GetFocusedElement();

        if (focused is null || !IsChildOfPanel(focused))
        {
            PanelBorder.BorderBrush = Avalonia.Media.Brushes.Transparent;
        }
    }

    private bool IsChildOfPanel(IInputElement element)
    {
        // Walk visual tree to see if element is inside this panel
        var current = element as StyledElement;
        while (current is not null)
        {
            if (current == this) return true;
            current = current.Parent;
        }
        return false;
    }

    private void OnGettingFocus(object? sender, GettingFocusEventArgs e)
    {
        // Block focus on search button when there's no query
        if (e.NewFocus == SearchButton &&
            string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            e.Cancel = true;
            // Redirect to search box instead
            SearchBox.Focus();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            // Clear search box and remove focus
            SearchBox.Text = "";
            Results.Clear();
            ResultsList.IsVisible = false;

            TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
            e.Handled = true;
        }
    }

    private void OnSearchButtonClick()
    {
        if (string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            // Redirect focus back to search box
            SearchBox.Focus();
            return;
        }

        Results.Add($"Result for: {SearchBox.Text}");
        ResultsList.IsVisible = true;
    }
}
```

### Key points

- `GotFocusEvent` / `LostFocusEvent` toggle the border highlight
- `LostFocusEvent` checks if focus moved outside the panel using `TopLevel.FocusManager.GetFocusedElement()` and a visual-tree walk
- `GettingFocusEvent` (tunnel) prevents focusing the Search button when the query is empty and redirects to `SearchBox`
- Escape clears focus via `FocusManager.ClearFocus()`

### Styling with focus pseudo-classes

In addition to the code-behind approach, you can style focus states declaratively:

```xml
<UserControl.Styles>
  <!-- Highlight the panel when any child is focused -->
  <Style Selector="^:focus-within /template/ Border#PanelBorder">
    <Setter Property="BorderBrush" Value="DodgerBlue" />
    <Setter Property="BorderThickness" Value="2" />
  </Style>

  <!-- Focus ring on text boxes -->
  <Style Selector="TextBox:focus /template/ Border">
    <Setter Property="BorderBrush" Value="{DynamicResource AccentColor}" />
    <Setter Property="BorderThickness" Value="2" />
  </Style>
</UserControl.Styles>
```

The `:focus-within` pseudo-class is a cleaner alternative to the code-behind `GotFocusEvent`/`LostFocusEvent` approach shown above — choose whichever fits your architecture.

---

## See Also

- [054 — Focus Management (core tutorial)](054-focus-management.md)
- [054V — Focus Management (verbose companion)](054-focus-management-verbose.md)
- [054Q — Focus Management (quiz)](054-focus-management-quiz.md)
