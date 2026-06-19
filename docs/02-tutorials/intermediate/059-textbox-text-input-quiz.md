---
tier: intermediate
topic: input
avalonia-version: 12.0.4
quiz-format: multiple-choice
---

# 059Q — TextBox & Text Input (quiz)

## Q1. Which event should you handle to intercept typed characters in an IME-aware way?

- [ ] A. KeyDown
- [ ] B. PreviewKeyDown
- [ ] C. TextInput
- [ ] D. TextChanged

**Answer:** C. TextInput receives the composed character string, including multi-character IME compositions. KeyDown/PreviewKeyDown only report physical keys.

---

## Q2. Which property enables multi-line input in a TextBox?

- [ ] A. Multiline="True"
- [ ] B. AcceptsReturn="True"
- [ ] C. AcceptsTab="True"
- [ ] D. TextWrapping="Wrap"

**Answer:** B. AcceptsReturn="True" lets Enter insert newlines. TextWrapping controls visual wrapping but does not enable multiline input.

---

## Q3. In a MaskedTextBox, what does the mask character `0` mean?

- [ ] A. Any character, optional
- [ ] B. ASCII letter, required
- [ ] C. Digit (0–9), required
- [ ] D. Digit or space, optional

**Answer:** C. `0` requires a digit (0–9). `9` is digit-or-space optional.

---

## Q4. How do you add a clear button inside a TextBox?

- [ ] A. Set ShowClearButton="True"
- [ ] B. Add a Button to InnerRightContent
- [ ] C. Use a ControlTemplate
- [ ] D. Add a Button to InnerLeftContent

**Answer:** B. TextBox.InnerRightContent allows placing a Button or any control inside the text area on the right side.

---

## Q5. Which FilterMode on AutoCompleteBox gives the most exact matching?

- [ ] A. Contains
- [ ] B. StartsWith
- [ ] C. StartsWithOrdinal
- [ ] D. ContainsCaseSensitive

**Answer:** C. StartsWithOrdinal performs ordinal (binary) comparison. ContainsCaseSensitive is case-sensitive but still uses culture rules.

---

## Q6. What happens when you set Text on a TextBox programmatically?

- [ ] A. The undo stack is preserved
- [ ] B. The undo stack is cleared
- [ ] C. The undo stack gets a new entry
- [ ] D. An exception is thrown if text is long

**Answer:** B. Programmatic assignment clears the undo stack. To preserve it, insert text through the input pipeline.

---

## Q7. True or False: TextInput fires for every hardware key press, including modifier keys.

- [ ] A. True
- [ ] B. False

**Answer:** B. False. TextInput only fires when a key press produces text characters. Modifier keys (Ctrl, Shift, Alt) do not trigger TextInput.

---

## Q8. Which of these is NOT a valid InputScope value?

- [ ] A. Number
- [ ] B. EmailSmtpAddress
- [ ] C. NumericPassword
- [ ] D. Url

**Answer:** C. NumericPassword is not a standard InputScope value. Use PinNumeric for numeric PIN entry.

---

## Q9. How do you select all text in a TextBox from code?

- [ ] A. myTextBox.SelectAll()
- [ ] B. myTextBox.Select(0, -1)
- [ ] C. myTextBox.SelectionLength = int.MaxValue
- [ ] D. myTextBox.Focus(); myTextBox.SelectAll()

**Answer:** A. myTextBox.SelectAll() selects all text. D would work too but is not the minimal method.

---

## Q10. In a MaskedTextBox, how do you include a literal `$` character in the mask?

- [ ] A. Mask="$$$999"
- [ ] B. Mask="\$999"
- [ ] C. Mask="&$999"
- [ ] D. Mask="[$$$]999"

**Answer:** B. The backslash escapes special mask characters. `\$` produces a literal dollar sign.

---

## Q11. Which event fires BEFORE the Text property is updated?

- [ ] A. TextChanged
- [ ] B. TextInput
- [ ] C. TextChanging
- [ ] D. PreviewKeyDown

**Answer:** C. TextChanging fires before the text is committed and can cancel the change. TextInput fires before TextChanging.

---

## Q12. What does the `:error` pseudo-class do on a TextBox?

- [ ] A. Shows an error tooltip
- [ ] B. Auto-focuses on error
- [ ] C. Activates when the validation system detects errors
- [ ] D. Logs the error to console

**Answer:** C. The `:error` pseudo-class is applied when validation (e.g., ObservableValidator) detects errors on the bound property.

---

## Scoring

| Score | Interpretation |
|-------|---------------|
| 12/12 | Expert |
| 10–11 | Strong understanding |
| 7–9 | Getting there |
| <7 | Review the core tutorial |

---

## See Also

- [059 — TextBox & Text Input (core)](059-textbox-text-input.md)
- [059V — TextBox & Text Input (verbose)](059-textbox-text-input-verbose.md)
- [059E — TextBox & Text Input (examples)](059-textbox-text-input-examples.md)
