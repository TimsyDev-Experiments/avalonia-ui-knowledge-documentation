---
tier: intermediate
topic: input
estimated: 12 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 059 — TextBox & Text Input

**What you'll learn:** How to use `TextBox`, `MaskedTextBox`, and `AutoCompleteBox` for text entry, handle text input events, work with IME composition, and tie into validation.

**Prerequisites:** [002 — Command Binding](../basics/002-command-binding.md), [013 — Data Validation](013-data-validation.md)

---

## 1. TextBox basics

```xml
<TextBox Text="{Binding Username}"
         PlaceholderText="Enter username" />
```

`TextBox.Text` binds two-way by default. `PlaceholderText` shows hint text when empty.

### Password input

```xml
<TextBox PasswordChar="●"
         Text="{Binding Password}" />
```

Toggle visibility:

```xml
<TextBox Name="PwBox" PasswordChar="●" />
<ToggleButton IsChecked="{Binding #PwBox.RevealPassword}"
              Content="Show" />
```

### Read-only

```xml
<TextBox Text="{Binding DisplayValue}" IsReadOnly="True" />
```

---

## 2. Multi-line input

```xml
<TextBox AcceptsReturn="True"
         TextWrapping="Wrap"
         MinLines="4"
         PlaceholderText="Enter comments..." />
```

| Property | Effect |
|----------|--------|
| `AcceptsReturn="True"` | Enter inserts newline |
| `AcceptsTab="True"` | Tab inserts tab (instead of focus move) |
| `TextWrapping="Wrap"` | Wraps long lines |
| `MinLines` / `MaxLines` | Constrain visible height |
| `MaxLength` | Character limit (0 = no limit) |

---

## 3. Text selection

```csharp
myTextBox.SelectionStart = 5;
myTextBox.SelectionEnd = 10;

string selected = myTextBox.SelectedText;
myTextBox.SelectAll();

// Select all on focus
myTextBox.GotFocus += (s, e) => myTextBox.SelectAll();
```

---

## 4. TextInput event vs KeyDown

| Event | Use for |
|-------|---------|
| `TextInput` | Processing typed characters (IME-aware) |
| `KeyDown` | Modifier keys, arrow keys, Enter, Escape |

Always use `TextInput` for character input — it receives IME-composed text. `KeyDown` only reports physical keys.

```csharp
myControl.AddHandler(InputElement.TextInputEvent, (s, e) =>
{
    if (e.Text is not null)
        ProcessInput(e.Text);
});
```

### Restricting characters with TextChanging

```csharp
private void OnTextChanging(object? sender, TextChangingEventArgs e)
{
    if (sender is TextBox tb && tb.Text is not null
        && !tb.Text.All(char.IsDigit))
    {
        e.Cancel = true;
    }
}
```

---

## 5. Text change events

| Event | When |
|-------|------|
| `TextChanging` | Before each change (can cancel) |
| `TextChanged` | After each change (read-only) |

For MVVM, react to the bound property:

```csharp
[ObservableProperty]
private string _searchText = "";

partial void OnSearchTextChanged(string value)
{
    ApplyFilter(value);
}
```

---

## 6. Validation

```csharp
public partial class FormViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required]
    [EmailAddress]
    private string _email = "";
}
```

The `TextBox` shows a red error border automatically. Customize with the `:error` pseudo-class:

```xml
<Style Selector="TextBox:error">
  <Setter Property="BorderBrush" Value="Red" />
</Style>
```

---

## 7. Inner content (left/right)

Add icons or buttons inside the TextBox:

```xml
<TextBox PlaceholderText="Search...">
  <TextBox.InnerRightContent>
    <Button Content="✕" Command="{Binding ClearCommand}"
            Background="Transparent" BorderThickness="0" />
  </TextBox.InnerRightContent>
</TextBox>
```

---

## 8. MaskedTextBox

Constrains input by a mask pattern:

```xml
<MaskedTextBox Mask="(+09) 000 000 0000" />
<MaskedTextBox Mask="00/00/0000" />
```

Common mask characters: `0` (digit required), `9` (digit optional), `L` (letter required), `?` (letter optional), `>` (uppercase), `<` (lowercase), `\` (escape).

---

## 9. AutoCompleteBox

```xml
<AutoCompleteBox ItemsSource="{Binding Countries}"
                 FilterMode="StartsWith"
                 PlaceholderText="Search countries..." />
```

| FilterMode | Behavior |
|------------|----------|
| `StartsWith` / `Contains` | Culture-insensitive, case-insensitive |
| `StartsWithCaseSensitive` / `ContainsCaseSensitive` | Culture-insensitive, case-sensitive |
| `StartsWithOrdinal` / `ContainsOrdinal` | Ordinal, case-insensitive |

Custom filtering with objects:

```csharp
myBox.ValueMemberBinding = new Binding("Name");
myBox.ItemFilter = (search, item) =>
{
    if (item is Product p)
        return p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
            || p.Id.ToString().Contains(search);
    return true;
};
```

---

## 10. IME and on-screen keyboard

`TextBox` supports IME (Chinese, Japanese, Korean) out of the box. No configuration needed.

For mobile, `InputScope` hints the keyboard layout:

```xml
<TextBox InputScope="Number" />
<TextBox InputScope="EmailSmtpAddress" />
<TextBox InputScope="Url" />
<TextBox InputScope="TelephoneNumber" />
```

---

## Key Takeaways

- `TextBox.Text` binds two-way; use `PlaceholderText` for hints
- Multi-line: `AcceptsReturn="True"` + `TextWrapping="Wrap"`
- Use `TextInput` event for character input (IME-aware), not `KeyDown`
- Validation via `ObservableValidator` with `:error` pseudo-class styling
- `MaskedTextBox` constrains format; `AutoCompleteBox` provides suggestions
- IME and on-screen keyboards work automatically
- Built-in undo/redo with Ctrl+Z / Ctrl+Shift+Z

---

## See Also

- [059V — TextBox & Text Input (verbose companion)](059-textbox-text-input-verbose.md)
- [059E — TextBox & Text Input (examples)](059-textbox-text-input-examples.md)
- [Avalonia Docs: TextBox How-To](https://docs.avaloniaui.net/docs/how-to/textbox-how-to)
- [Avalonia Docs: Text Input and IME](https://docs.avaloniaui.net/docs/input-interaction/text-input)
