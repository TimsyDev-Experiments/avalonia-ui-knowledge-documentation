---
tier: intermediate
topic: input
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 059E — TextBox & Text Input (examples)

## Example 1: Login form with validation

**View (AXAML):**

```xml
<StackPanel Spacing="12" Margin="20" Width="320">
  <TextBox PlaceholderText="Email"
           Text="{Binding Email, ValidatesOnNotifyDataErrors=True}"
           Watermark="user@example.com" />
  <TextBox PlaceholderText="Password"
           PasswordChar="●"
           Text="{Binding Password}" />
  <TextBox PlaceholderText="Phone"
           Watermark="+1 (555) 000-0000">
    <MaskedTextBox.Mask>(+1) 000 000-0000</MaskedTextBox.Mask>
  </TextBox>
  <Button Content="Login" Command="{Binding LoginCommand}"
          HorizontalAlignment="Right" />
</StackPanel>
```

**ViewModel:**

```csharp
public partial class LoginViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [EmailAddress(ErrorMessage = "Invalid email")]
    private string _email = "";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [MinLength(8, ErrorMessage = "Min 8 characters")]
    private string _password = "";

    [RelayCommand]
    private async Task Login()
    {
        ValidateAllProperties();
        if (HasErrors) return;
        // ...
    }
}
```

---

## Example 2: Search box with clear button

```xml
<TextBox PlaceholderText="Search..."
         Text="{Binding SearchText}"
         TextChanging="OnTextChanging"
         MaxLength="100">
  <TextBox.InnerRightContent>
    <Button Content="✕" Command="{Binding ClearSearchCommand}"
            Background="Transparent" BorderThickness="0"
            IsVisible="{Binding HasSearchText}" />
  </TextBox.InnerRightContent>
  <TextBox.InnerLeftContent>
    <PathIcon Data="{StaticResource SearchIcon}" />
  </TextBox.InnerLeftContent>
</TextBox>
```

```csharp
public partial class SearchViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = "";

    public bool HasSearchText => !string.IsNullOrEmpty(SearchText);

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasSearchText));
        // Debounced search:
        // await Task.Delay(200);
        // ApplyFilter(value);
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = "";
    }
}
```

---

## Example 3: Custom hex editor using TextInput

```csharp
public class HexEditor : TextBox
{
    private const string HexChars = "0123456789abcdefABCDEF";

    static HexEditor()
    {
        TextInputEvent.AddClassHandler<HexEditor>((e, args) =>
            e.OnCustomTextInput(args), RoutingStrategies.Tunnel);
    }

    private void OnCustomTextInput(TextInputRoutedEventArgs e)
    {
        if (e.Text is null) return;
        foreach (char c in e.Text)
        {
            if (!HexChars.Contains(c))
            {
                e.Handled = true;
                return;
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Allow navigation and clipboard, but block non-hex paste
        if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Ctrl)
        {
            var text = Application.Current?.Clipboard?.GetTextAsync()
                .GetAwaiter().GetResult();
            if (text is not null && !text.All(c => HexChars.Contains(c)))
            {
                e.Handled = true;
                return;
            }
        }
        base.OnKeyDown(e);
    }
}
```

---

## Example 4: AutoCompleteBox with custom object filtering

```xml
<AutoCompleteBox ItemsSource="{Binding Customers}"
                 SelectedItem="{Binding SelectedCustomer}"
                 FilterMode="Contains"
                 PlaceholderText="Type to search..." />
```

```csharp
public partial class CustomerEntryViewModel : ObservableObject
{
    public ObservableCollection<Customer> Customers { get; } = new()
    {
        new("Alice", "alice@example.com"),
        new("Bob", "bob@example.com"),
        // ...
    };

    [ObservableProperty]
    private Customer? _selectedCustomer;

    public CustomerEntryViewModel()
    {
        // Custom filter: match name OR email
        var box = /* get reference */;
        box.ItemFilter = (search, item) =>
        {
            if (item is not Customer c) return false;
            return c.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || c.Email.Contains(search, StringComparison.OrdinalIgnoreCase);
        };
    }
}

public record Customer(string Name, string Email);
```

---

## Example 5: Multi-line log viewer (append-only)

```xml
<TextBox Name="LogBox"
         AcceptsReturn="True"
         IsReadOnly="True"
         TextWrapping="Wrap"
         MinLines="10"
         MaxLines="30" />
```

```csharp
public class LogViewModel : ObservableObject
{
    private readonly StringBuilder _buffer = new();

    public void AppendLog(string line)
    {
        _buffer.AppendLine(line);
        OnPropertyChanged(nameof(LogText));
    }

    public string LogText => _buffer.ToString();
}
```

For high-frequency logging, append via dispatcher with debounce:

```csharp
private readonly Channel<string> _logChannel = Channel.CreateUnbounded<string>();

public LogViewModel()
{
    _ = ProcessLogQueue();
}

private async Task ProcessLogQueue()
{
    var reader = _logChannel.Reader;
    var batch = new List<string>();
    while (await reader.WaitToReadAsync())
    {
        while (reader.TryRead(out var line))
            batch.Add(line);

        var combined = string.Join(Environment.NewLine, batch);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            LogText += combined + Environment.NewLine;
            OnPropertyChanged(nameof(LogText));
        });
        batch.Clear();
    }
}

public void AppendLog(string line) => _logChannel.Writer.TryWrite(line);
```

---

## Example 6: IME composition tracking

```csharp
public class ImeAwareTextBox : TextBox
{
    private string _composingText = "";

    static ImeAwareTextBox()
    {
        TextInputEvent.AddClassHandler<ImeAwareTextBox>((w, e) =>
            w.OnCustomTextInput(e), RoutingStrategies.Tunnel);
    }

    private void OnCustomTextInput(TextInputRoutedEventArgs e)
    {
        // Track composing (IME) vs committed text
        if (string.IsNullOrEmpty(e.Text))
        {
            // Composition is in progress (no final text yet)
            _composingText = e.ControlText ?? "";
        }
        else
        {
            // Final composition committed
            _composingText = "";
        }
    }

    public string CompositionHint => _composingText;
}
```

---

## Example 7: Numeric-only TextBox

```csharp
public class NumericTextBox : TextBox
{
    static NumericTextBox()
    {
        TextInputEvent.AddClassHandler<NumericTextBox>((w, e) =>
        {
            if (e.Text is not null && !e.Text.All(c => char.IsDigit(c)
                || c == '.' || c == ','))
            {
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Space)
            e.Handled = true;

        base.OnKeyDown(e);
    }
}
```

---

## Example 8: Debounced AutoCompleteBox for large data sets

```csharp
public partial class DebouncedSearchViewModel : ObservableObject
{
    [ObservableProperty]
    private string _query = "";

    public ObservableCollection<Product> Results { get; } = new();

    private CancellationTokenSource? _debounceCts;

    partial void OnQueryChanged(string value)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Delay(300, token).ContinueWith(async _ =>
        {
            var filtered = await Task.Run(() =>
                AllProducts.Where(p =>
                    p.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                    .ToList(), token);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Results.Clear();
                foreach (var p in filtered) Results.Add(p);
            });
        }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }
}
```

---

## Example 9: Inline editing (double-click to edit)

```xml
<TextBlock Text="{Binding Name}" DoubleTapped="StartEdit" />
<TextBox Text="{Binding Name}" IsVisible="{Binding IsEditing}"
         LostFocus="EndEdit" KeyDown="EndEditOnEnter" />
```

```csharp
private void StartEdit(object? sender, DoubleTappedEventArgs e)
{
    if (sender is TextBlock tb && tb.DataContext is ItemViewModel vm)
    {
        vm.IsEditing = true;
        // Focus the TextBox in code-behind
        FindTextBox(vm)?.Focus();
    }
}

private void EndEdit(object? sender, EventArgs e)
{
    if (sender is TextBox tb && tb.DataContext is ItemViewModel vm)
        vm.IsEditing = false;
}

private void EndEditOnEnter(object? sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter)
        EndEdit(sender, EventArgs.Empty);
}
```

**ViewModel:**

```csharp
public partial class ItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private bool _isEditing;
}
```

---

## Example 10: Undo-redo status bar

```csharp
public partial class EditorViewModel : ObservableObject
{
    [ObservableProperty] private bool _canUndo;
    [ObservableProperty] private bool _canRedo;

    public void RegisterTextBox(TextBox tb)
    {
        tb.TextChanged += (_, _) =>
        {
            CanUndo = tb.CanUndo;
            CanRedo = tb.CanRedo;
        };
    }

    [RelayCommand]
    private void Undo(TextBox? tb) => tb?.Undo();

    [RelayCommand]
    private void Redo(TextBox? tb) => tb?.Redo();
}
```

---

## See Also

- [059 — TextBox & Text Input (core)](059-textbox-text-input.md)
- [059V — TextBox & Text Input (verbose)](059-textbox-text-input-verbose.md)
- [056 — Input Events](056-input-events.md)
