---
tier: advanced
topic: layout
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 084 — Typography & Text Features — Quiz

**Prerequisites:** [084-core](084-typography-text-features.md)

---

### Q1: Which class defines typography properties that are inherited by descendant controls?

<details>
<summary>Answer</summary>

`TextElement`. Properties like `FontSize`, `FontWeight`, `LetterSpacing`, and `FontFeatures` are attached properties on `TextElement` that inherit through the visual tree.

</details>

---

### Q2: What is the numeric value for `FontWeight.SemiBold`?

<details>
<summary>Answer</summary>

600. Named aliases include `DemiBold`.

</details>

---

### Q3: Write XAML for a `TextBlock` that wraps text, limits to 3 lines, and shows an ellipsis on overflow.

<details>
<summary>Answer</summary>

```xml
<TextBlock TextWrapping="Wrap" MaxLines="3"
           TextTrimming="WordEllipsis"
           Text="Long text that wraps..." />
```

</details>

---

### Q4: Which inline element embeds a `Button` or `Image` inside a `TextBlock`?

<details>
<summary>Answer</summary>

`InlineUIContainer`. It accepts any `Control` as its content and aligns it within the text flow using `BaselineAlignment`.

</details>

---

### Q5: True or False: The `FontStretch` property works with any font out of the box.

<details>
<summary>Answer</summary>

False. `FontStretch` only has a visible effect if the font includes dedicated glyphs for the requested stretch value (e.g., `Condensed` or `Expanded`). Most fonts only include `Normal` width glyphs.

</details>

---

### Q6: Write a `TextDecoration` that draws a 3px-wide red dashed line below the text.

<details>
<summary>Answer</summary>

```xml
<TextDecoration Location="Underline"
                Stroke="Red"
                StrokeThickness="3"
                StrokeDashArray="4,2" />
```

</details>

---

### Q7: What does the OpenType feature tag `+tnum` do, and when would you use it?

<details>
<summary>Answer</summary>

`+tnum` enables tabular (fixed-width) numbers where each digit occupies the same horizontal space. Use it in tables, financial displays, stopwatches, or any layout where numeric columns should align vertically.

</details>

---

### Q8: A custom font defined in `Application.Resources` silently falls back to the default. What are two likely causes?

<details>
<summary>Answer</summary>

1. The URI is missing the `#FontFamilyName` suffix (e.g., `#Nunito`).
2. The resource is not wrapped in `ResourceDictionary.MergedDictionaries`.

Also check that font files are included as `AvaloniaResource` in the `.csproj`.

</details>

---

### Q9: Which `TextOptions` property prevents text from "jumping" during a smooth scrolling animation?

<details>
<summary>Answer</summary>

`BaselinePixelAlignment` set to `Unaligned`. This disables baseline snapping to pixel boundaries, allowing sub-pixel positioning during animations.

</details>

---

### Q10: Write XAML showing how to apply `FontFeatures` to a container so all child `TextBlock` controls use tabular numbers.

<details>
<summary>Answer</summary>

```xml
<StackPanel TextElement.FontFeatures="+tnum">
  <TextBlock Text="12345" />
  <TextBlock Text="67890" />
</StackPanel>
```

</details>

---

### Q11: Name all four derived formatting inlines that inherit from `Span`.

<details>
<summary>Answer</summary>

`Bold`, `Italic`, and `Underline`. (These are the three predefined; you can also derive custom inlines from `Span`.)

</details>

---

### Q12: What is the difference between `LineHeight` and `LineSpacing`?

<details>
<summary>Answer</summary>

- `LineHeight` sets a fixed total height for each line. When set to `NaN` (the default), font metrics determine line height.
- `LineSpacing` adds extra space between lines **on top of** the font's natural line height.

Use `LineHeight` for precise control; use `LineSpacing` for adding breathing room without overriding font metrics.

</details>
