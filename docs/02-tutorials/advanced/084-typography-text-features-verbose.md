---
tier: advanced
topic: layout
estimated: 30 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 084 — Typography & Text Features — Verbose

**Prerequisites:** [084-core](084-typography-text-features.md)

---

## 1. FontFamily resolution and fallback

`FontFamily` accepts a comma-separated list of families. Avalonia tries each in order:

```xml
<TextBlock FontFamily="Segoe UI, Inter, sans-serif"
           Text="Falls back through the list." />
```

The platform default is used if none of the specified families are found. On Windows this is typically Segoe UI; on macOS, San Francisco; on Linux, depending on the distro.

---

## 2. FontStretch in detail

`FontStretch` requires the font to have dedicated glyphs for the requested width. Most fonts only include `Normal` width.

| Value | Description |
|---|---|
| `UltraCondensed` | Narrowest |
| `ExtraCondensed` | |
| `Condensed` | |
| `SemiCondensed` | |
| `Normal` | Default |
| `SemiExpanded` | |
| `Expanded` | |
| `ExtraExpanded` | |
| `UltraExpanded` | Widest |

```xml
<TextBlock FontStretch="Condensed" Text="Narrow" />
<TextBlock FontStretch="Expanded" Text="Wide" />
```

If the font lacks the requested stretch, Avalonia silently falls back to `Normal`.

---

## 3. FontFeatures syntax

Features use HarfBuzz tag syntax: `+tag` to enable, `-tag` to disable. Multiple features are comma-separated:

```xml
<TextBlock FontFeatures="+tnum,-liga,+smcp"
           Text="Tabular nums, no ligs, small caps" />
```

Apply on containers:

```xml
<StackPanel TextElement.FontFeatures="+tnum">
  <TextBlock Text="12345" />
  <TextBlock Text="67890" />
</StackPanel>
```

---

## 4. Inlines detail

### Run

Binds to view model data with independent formatting:

```xml
<Run FontSize="24" FontWeight="Bold" Foreground="Orange"
     Text="{Binding Name}" />
```

### Span

Groups inlines and applies common formatting:

```xml
<Span Foreground="Green">
  <Bold>Bold green text</Bold> normal green text
</Span>
```

Derived formatting inlines: `Bold`, `Italic`, `Underline`.

### LineBreak

Forces a line break in the inline flow. Equivalent to `<br>` in HTML.

### InlineUIContainer

Embeds any `Control` (image, button, custom control) within text:

```xml
<InlineUIContainer BaselineAlignment="Baseline">
  <Image Width="32" Height="32" Source="/Assets/logo.png" />
</InlineUIContainer>
```

`BaselineAlignment` controls vertical alignment of the embedded element relative to surrounding text.

---

## 5. Custom decorations — full properties

```xml
<TextDecoration Location="Underline"
                Stroke="Red"
                StrokeThickness="2"
                StrokeThicknessUnit="Pixel"
                StrokeOffset="3"
                StrokeOffsetUnit="Pixel"
                StrokeDashArray="2,2"
                StrokeLineCap="Round" />
```

| Property | Values | Default |
|---|---|---|
| `Location` | `Underline`, `Strikethrough`, `Overline`, `Baseline` | — |
| `Stroke` | `IBrush` | — |
| `StrokeThickness` | `double` | 1 |
| `StrokeThicknessUnit` | `FontRecommended`, `FontRenderingEmSize`, `Pixel` | `FontRecommended` |
| `StrokeOffset` | `double` | 0 |
| `StrokeOffsetUnit` | Same as thickness unit | `FontRecommended` |
| `StrokeDashArray` | dash lengths (e.g. `2,2`) | empty (solid) |
| `StrokeLineCap` | `Flat`, `Round`, `Square` | `Flat` |

---

## 6. TextOptions rendering modes in practice

| Mode | Use case |
|---|---|
| `Alias` | Pixel fonts, very small sizes, terminal-style text |
| `Antialias` | General-purpose smooth text |
| `SubpixelAntialias` | LCD/OLED screens — sharpest horizontal resolution |
| `Auto` | Platform chooses (typically `SubpixelAntialias` on Windows) |

### Hinting modes

| Mode | Use case |
|---|---|
| `Full` | Small body text — sharpest |
| `None` | Large display text, animated text — preserves design shapes |
| `Slight` | Moderate hinting — vertical only |
| `Normal` | Between `Slight` and `Full` |

### Baseline alignment for animation

```xml
<TextBlock TextOptions.BaselinePixelAlignment="Unaligned">
  <TextBlock.RenderTransform>
    <TranslateTransform />
  </TextBlock.RenderTransform>
  Sliding text without pixel snapping
</TextBlock>
```

Without `Unaligned`, text "jumps" by 1 pixel during smooth scroll or animation as baselines snap to grid.

---

## 7. Custom fonts — troubleshooting

| Symptom | Cause |
|---|---|
| Font silently reverts to default | Missing `#FontFamilyName` suffix in URI |
| Font works on desktop, not WASM | Using system font name instead of full collection URI |
| Assembly size did not increase | Font files not included as `AvaloniaResource` |
| Font defined in `Application.Resources` fails | Wrap in `ResourceDictionary.MergedDictionaries` |

WASM fix:

```xml
<!-- Fails in WASM -->
<TextBlock FontFamily="Inter" />
<!-- Works everywhere -->
<TextBlock FontFamily="fonts:Inter#Inter" />
```

---

## 8. SelectableTextBlock

For text that users need to select and copy:

```xml
<SelectableTextBlock Text="Selectable content" />
```

Supports all the same formatting properties as `TextBlock` (`FontSize`, `FontWeight`, `TextWrapping`, inlines, etc.) but enables text selection.

---

## 9. Type scale — advanced

Combine type scale with responsive container queries:

```xml
<ContainerQuery Name="content" Query="max-width:400">
  <Style Selector="TextBlock.h1">
    <Setter Property="FontSize" Value="24" />
    <Setter Property="LineHeight" Value="32" />
  </Style>
</ContainerQuery>
<ContainerQuery Name="content" Query="min-width:800">
  <Style Selector="TextBlock.h1">
    <Setter Property="FontSize" Value="40" />
    <Setter Property="LineHeight" Value="48" />
  </Style>
</ContainerQuery>
```

---

## 10. Text Trimming — full modes

| Mode | Behavior |
|---|---|
| `None` | Clip (default) |
| `CharacterEllipsis` | `Hello Wor…` |
| `WordEllipsis` | `Hello …` |
| `PrefixEllipsis` | `…lorld` |
| `MiddleEllipsis` | `He…ld` |
| `LeadingEllipsis` | `…World` |
| `LeadingWordEllipsis` | `…World` |

```xml
<TextBlock TextTrimming="MiddleEllipsis" MaxWidth="80"
           Text="LongFileName.txt" />
<!-- Renders as "Lo…txt" -->
```

---

## Key Takeaways

- Font family supports fallback with comma-separated list
- FontStretch requires font-specific glyph variants; silently falls back to Normal
- FontFeatures uses HarfBuzz syntax — enable with `+`, disable with `-`
- Inlines (`Run`, `Span`, `LineBreak`, `InlineUIContainer`) provide rich mixed-content text blocks
- Custom `TextDecoration` supports stroke color, thickness, offset, dashes, and line cap
- `TextOptions.BaselinePixelAlignment="Unaligned"` prevents pixel jumping in animations
- SelectableTextBlock mirrors all TextBlock formatting but enables text selection
- Type scale can be combined with container queries for responsive font sizing
- Multiple ellipsis trimming modes available beyond basic `CharacterEllipsis` and `WordEllipsis`
