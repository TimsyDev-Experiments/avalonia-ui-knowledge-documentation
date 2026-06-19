---
tier: intermediate
topic: input
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 059V — TextBox & Text Input (verbose companion)

**What this covers:** A deeper look at how Avalonia handles text input — event routing, IME composition, programmatic control, custom input masking, and text-measurement internals.

**Prerequisites:** 059 — TextBox & Text Input core

---

## 1. TextBox event flow

Avalonia raises several events during a key-press → text-insert cycle:

1. `PreviewKeyDown` — tunnelling, can intercept
2. `KeyDown` — bubbling, the TextBox captures it
3. `TextInput` — receives the composed character(s)
4. `TextChanging` — fires *before* the Text property is updated; cancellable
5. `TextChanged` — fires *after* the Text property is updated

If a hardware key press produces no text (e.g. Ctrl, Shift), `TextInput` is not raised.

```csharp
// PreviewKeyDown for intercepting shortcuts before the TextBox sees them
myTextBox.AddHandler(InputElement.PreviewKeyDownEvent, (s, e) =>
{
    if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.Control)
    {
        // Insert a non-breaking space
        int pos = myTextBox.CaretIndex;
        myTextBox.Text = myTextBox.Text?.Insert(pos, "\u00A0") ?? "";
        myTextBox.CaretIndex = pos + 1;
        e.Handled = true;
    }
}, RoutingStrategies.Tunnel);
```

---

## 2. The TextInput event

`TextInputRoutedEventArgs` provides:

| Member | Description |
|--------|-------------|
| `Text` | The composed string (may be multiple characters) |
| `ControlText` | Pre-composed IME reading text |
| `Handled` | Set to true to prevent insertion |

IME composition sequence:

```
KeyDown(Key.ImeProcessed) → TextInput("") →
... repeated for each composing segment →
KeyDown(Key.ImeProcessed) → TextInput("あ") →  (final composition)
```

For a custom text input surface (e.g. hex editor), attach to `TextInput`:

```csharp
public class HexTextBox : TextBox
{
    static HexTextBox()
    {
        TextInputEvent.AddClassHandler<HexTextBox>((c, e) =>
            c.OnTextInput(e), RoutingStrategies.Tunnel);
    }

    protected override void OnTextInput(TextInputRoutedEventArgs e)
    {
        if (e.Text is not null && e.Text.Any(c => !IsHexChar(c)))
            e.Handled = true;
        else
            base.OnTextInput(e);
    }

    private static bool IsHexChar(char c) =>
        char.IsAsciiDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}
```

---

## 3. CaretIndex and selection programmatically

```csharp
// Move caret to end
myTextBox.CaretIndex = myTextBox.Text?.Length ?? 0;

// Insert at caret
int pos = myTextBox.CaretIndex;
myTextBox.Text = myTextBox.Text?.Insert(pos, " (edited)");
myTextBox.CaretIndex = pos + " (edited)".Length;

// Select a range
myTextBox.SelectionStart = 2;
myTextBox.SelectionEnd = 5;

// Clear selection
myTextBox.SelectionStart = myTextBox.SelectionEnd;
```

`CaretIndex` clamps to the text length automatically. Setting `SelectionStart` will move the caret; setting `SelectionEnd` expands the selection highlight.

---

## 4. Undo and redo

`TextBox` has built-in undo/redo via Ctrl+Z / Ctrl+Shift+Z. Control from code:

```csharp
// Clear the undo stack (useful after setting Text programmatically)
typeof(TextBox)
    .GetField("_undoRedoHelper", BindingFlags.NonPublic | BindingFlags.Instance)?
    .SetValue(myTextBox, null);
```

**Note:** Programmatic `Text` assignment clears the undo stack. To preserve it, insert characters via `TextInput` simulation or set `Text` and rebuild the undo state.

---

## 5. InputScope

`InputScope` hints the platform's on-screen keyboard or IME.

```xml
<TextBox InputScope="Number" />

<!-- Multiple scopes (first wins, others are fallback) -->
<TextBox>
  <TextBox.InputScope>
    <InputScope>
      <InputScopeName NameValue="Url" />
      <InputScopeName NameValue="Text" />
    </InputScope>
  </TextBox.InputScope>
</TextBox>
```

Available values: `Default`, `Text`, `Url`, `Number`, `TelephoneNumber`, `EmailSmtpAddress`, `Search`, `Chat`, `Digits`, `PinNumeric`, `CurrencyAmountAndSymbol`.

---

## 6. MaskedTextBox deep dive

Mask characters compose a validation pattern:

| Char | Meaning | Example |
|------|---------|---------|
| `0` | Digit required | `(000) 000-0000` → phone |
| `9` | Digit or space, optional | `999-99-9999` → SSN |
| `L` | ASCII letter required | `LLL 000` → license plate |
| `?` | ASCII letter, optional | `?-000` |
| `#` | Digit, space, +/−, optional | `###` → temperature |
| `>` | Shift up (uppercase following) | `>LLL` → all caps |
| `<` | Shift down (lowercase following) | `<LLL` → all lower |
| `\|` | Disable previous shift up/down | `>LLL\|LLL` |
| `\` | Escape special char | `\$999` → literal $ |

```xml
<MaskedTextBox Mask="(+99) 0000-0000" />
```

**Properties:**

| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `AsciiOnly` | bool | false | Only allow a-z, A-Z for letter chars |
| `Text` | string | "" | Includes literal mask characters |
| `MaskCompleted` | bool | (readonly) | All required positions filled |
| `MaskFull` | bool | (readonly) | All positions (incl. optional) filled |

---

## 7. AutoCompleteBox custom item templates

```xml
<AutoCompleteBox ItemsSource="{Binding Products}"
                 FilterMode="Contains"
                 PlaceholderText="Search products...">
  <AutoCompleteBox.ItemTemplate>
    <DataTemplate>
      <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="{Binding Name}" FontWeight="Bold" />
        <TextBlock Text="{Binding Price, StringFormat='${0:N2}'}"
                   Foreground="Gray" />
      </StackPanel>
    </DataTemplate>
  </AutoCompleteBox.ItemTemplate>
</AutoCompleteBox>
```

When using `ValueMemberBinding`:

```xml
<AutoCompleteBox ItemsSource="{Binding People}"
                 ValueMemberBinding="{Binding Name}"
                 SelectedItem="{Binding SelectedPerson}" />
```

`ValueMemberBinding` tells the box which property to use for text display while `SelectedItem` returns the full object.

---

## 8. TextWrapping modes

| Mode | Behavior |
|------|----------|
| `NoWrap` | Single line; horizontal scroll |
| `Wrap` | Wrap at word boundaries |
| `WrapWithOverflow` | Wrap at character boundary if no word-break |

---

## 9. Performance considerations

- `Text` property triggers `Measure` → `Arrange` on every set. For high-frequency updates (>10 Hz), consider Debounce.
- For logging output, use `TextBlock` (no input overhead) instead of `TextBox`.
- `AutoCompleteBox` filters on every keystroke by default. For large data sets (>10k items), debounce with a 200 ms delay.

---

## 10. Undo/redo internals

The `TextBox` stores up to 128 undo actions by default. Each action snapshots the text and caret position. When you call `TextBox.ClearUndoRedoHistory()` (Avalonia 12+), the internal stack is cleared. Batch operations (replacing a selection) are merged into a single undo action.

---

## See Also

- [059 — TextBox & Text Input (core)](059-textbox-text-input.md)
- [059E — TextBox & Text Input (examples)](059-textbox-text-input-examples.md)
- [056 — Input Events](056-input-events.md)
- [Avalonia Docs: Text Input](https://docs.avaloniaui.net/docs/input-interaction/text-input)
