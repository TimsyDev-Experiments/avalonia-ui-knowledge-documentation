---
tier: advanced
topic: layout
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 084 — Typography & Text Features

**What you'll learn:** Master Avalonia's typography system — font management, text formatting, inlines, decorations, OpenType features, text rendering options, and creating a type scale.

**Prerequisites:** [083 — Container Queries & Responsive Layout](083-container-queries-responsive-layout.md)

---

## 1. TextElement attached properties

All typography properties are defined on `TextElement` as inherited attached properties. Set them on any control to affect all descendant text.

| Property | Type | Default | Description |
|---|---|---|---|
| `FontFamily` | `FontFamily` | Platform default | Typeface family |
| `FontSize` | `double` | `12` | Height in device-independent pixels |
| `FontWeight` | `FontWeight` | `Normal` | Stroke thickness (1–999) |
| `FontStyle` | `FontStyle` | `Normal` | Upright, italic, or oblique |
| `FontStretch` | `FontStretch` | `Normal` | Character width |
| `FontFeatures` | `FontFeatureCollection` | `null` | OpenType feature toggles |
| `Foreground` | `IBrush` | Inherited | Text brush |
| `LetterSpacing` | `double` | `0` | Extra space between characters |

```xml
<StackPanel TextElement.FontSize="16" TextElement.FontWeight="SemiBold">
  <TextBlock Text="This inherits both properties." />
  <TextBlock FontSize="24" Text="This overrides FontSize." />
</StackPanel>
```

---

## 2. Font weight values

| Name | Numeric |
|---|---|
| `Thin` | 100 |
| `ExtraLight` / `UltraLight` | 200 |
| `Light` | 300 |
| `SemiLight` | 350 |
| `Normal` / `Regular` | 400 |
| `Medium` | 500 |
| `SemiBold` / `DemiBold` | 600 |
| `Bold` | 700 |
| `ExtraBold` / `UltraBold` | 800 |
| `Black` / `Heavy` | 900 |
| `ExtraBlack` / `UltraBlack` | 950 |

```xml
<TextBlock FontWeight="550" Text="Custom weight 550" />
```

---

## 3. Font style

| Value | Description |
|---|---|
| `Normal` | Upright (default) |
| `Italic` | True italic variant |
| `Oblique` | Algorithmic slant |

---

## 4. Letter spacing

```xml
<TextBlock LetterSpacing="2" Text="Wider spacing" />
<TextBlock LetterSpacing="-1" Text="Tighter spacing" />
```

Positive values expand; negative values contract.

---

## 5. Line height and spacing

| Property | Type | Default | Description |
|---|---|---|---|
| `LineHeight` | `double` | `NaN` | Fixed line height |
| `LineSpacing` | `double` | `0` | Extra space between lines, on top of font metrics |

```xml
<TextBlock TextWrapping="Wrap" LineHeight="32"
           Text="Consistent line height." />
<TextBlock TextWrapping="Wrap" LineSpacing="6"
           Text="Extra breathing room between lines." />
```

---

## 6. Text alignment

| Value | Behavior |
|---|---|
| `Left` | Left edge |
| `Center` | Centered |
| `Right` | Right edge |
| `Start` | Respects `FlowDirection` |
| `End` | Respects `FlowDirection` |
| `Justify` | Stretched full width (requires `TextWrapping`) |
| `DetectFromContent` | Inferred from Unicode directionality |

---

## 7. Text wrapping and trimming

```xml
<TextBlock TextWrapping="Wrap" MaxLines="3"
           TextTrimming="WordEllipsis"
           Text="Long paragraph that wraps for up to three lines, then trims with ellipsis." />
```

| `TextWrapping` | Behavior |
|---|---|
| `NoWrap` | No wrapping (default) |
| `Wrap` | Wrap at nearest fitting character |
| `WrapWithOverflow` | Wrap but allow single-word overflow |

| `TextTrimming` | Behavior |
|---|---|
| `None` | Clip (default) |
| `CharacterEllipsis` | Ellipsis at character boundary |
| `WordEllipsis` | Ellipsis at word boundary |

---

## 8. Text decorations

### Preset decorations

```xml
<TextBlock TextDecorations="Underline" Text="Underlined" />
<TextBlock TextDecorations="Strikethrough" Text="Struck through" />
<TextBlock TextDecorations="Underline Strikethrough" Text="Both" />
```

Presets: `Underline`, `Strikethrough`, `Overline`, `Baseline`.

### Custom decorations

```xml
<TextBlock Text="Custom red dashed underline">
  <TextBlock.TextDecorations>
    <TextDecorationCollection>
      <TextDecoration Location="Underline"
                      Stroke="Red" StrokeThickness="2"
                      StrokeDashArray="2,2" />
    </TextDecorationCollection>
  </TextBlock.TextDecorations>
</TextBlock>
```

---

## 9. Inlines

Rich text formatting inside a `TextBlock`:

```xml
<TextBlock>
  <Run Text="Normal, " />
  <Run FontWeight="Bold" Foreground="Orange" Text="{Binding Name}" />
  <Run Text=" and " />
  <LineBreak />
  <Span Foreground="Green">
    <Italic>Formatted</Italic> text
  </Span>
  <InlineUIContainer>
    <Image Width="24" Height="24" Source="/Assets/icon.png" />
  </InlineUIContainer>
</TextBlock>
```

| Inline | Purpose |
|---|---|
| `Run` | Uniformly formatted text segment |
| `LineBreak` | Force line break |
| `Span` | Groups inlines, applies formatting; `Bold`, `Italic`, `Underline` derive from it |
| `InlineUIContainer` | Embeds any `Control` inline |

---

## 10. OpenType font features

```xml
<TextBlock Text="0123456789" FontFeatures="+tnum" />
<TextBlock Text="fi fl ffi" FontFeatures="-liga" />
<TextBlock Text="Small Caps" FontFeatures="+smcp" />
```

| Tag | Feature |
|---|---|
| `+tnum` / `-tnum` | Tabular (fixed-width) numbers |
| `+liga` / `-liga` | Standard ligatures |
| `+calt` / `-calt` | Contextual alternates |
| `+smcp` | Small capitals |
| `+onum` | Old-style numbers |

---

## 11. Text rendering options

```xml
<TextBlock Text="Sharp aliased text"
           TextOptions.TextRenderingMode="Alias" />
<TextBlock Text="Smooth large heading"
           TextOptions.TextHintingMode="None" />
<TextBlock Text="Animated text without jump"
           TextOptions.BaselinePixelAlignment="Unaligned" />
```

| Property | Values | Purpose |
|---|---|---|
| `TextRenderingMode` | `Auto`, `Alias`, `Antialias`, `SubpixelAntialias` | Anti-aliasing quality |
| `TextHintingMode` | `None`, `Slight`, `Normal`, `Full` | Pixel-grid alignment |
| `BaselinePixelAlignment` | `Unspecified`, `Aligned`, `Unaligned` | Sub-pixel positioning for animation |

These are inherited attached properties on `TextOptions`.

---

## 12. Custom fonts

### Static resource

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary>
        <FontFamily x:Key="NunitoFont">avares://MyApp/Assets/Fonts#Nunito</FontFamily>
      </ResourceDictionary>
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

```xml
<TextBlock FontFamily="{StaticResource NunitoFont}" Text="Hello" />
```

### Embedded font collection

```csharp
public sealed class MyFonts : EmbeddedFontCollection
{
    public MyFonts() : base(
        new Uri("fonts:MyFonts", UriKind.Absolute),
        new Uri("avares://MyApp/Assets/Fonts", UriKind.Absolute)) { }
}
```

```csharp
.ConfigureFonts(fm => fm.AddFontCollection(new MyFonts()))
```

```xml
<TextBlock FontFamily="fonts:MyFonts#Nunito" Text="Hello" />
```

### Pre-built package

```csharp
.WithInterFont()
```

```xml
<TextBlock FontFamily="fonts:Inter#Inter" Text="Hello" />
```

Font files must be included as `AvaloniaResource`:

```xml
<AvaloniaResource Include="Assets\Fonts\*" />
```

---

## 13. Type scale with style classes

```xml
<Application.Styles>
  <Style Selector="TextBlock.h1">
    <Setter Property="FontSize" Value="32" />
    <Setter Property="FontWeight" Value="Bold" />
    <Setter Property="LineHeight" Value="40" />
  </Style>
  <Style Selector="TextBlock.h2">
    <Setter Property="FontSize" Value="24" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="LineHeight" Value="32" />
  </Style>
  <Style Selector="TextBlock.body">
    <Setter Property="FontSize" Value="14" />
    <Setter Property="LineHeight" Value="20" />
  </Style>
  <Style Selector="TextBlock.caption">
    <Setter Property="FontSize" Value="12" />
    <Setter Property="Foreground" Value="{DynamicResource TextFillColorTertiaryBrush}" />
  </Style>
</Application.Styles>
```

```xml
<TextBlock Classes="h1" Text="Page Title" />
<TextBlock Classes="body" Text="Body content." />
<TextBlock Classes="caption" Text="Small note." />
```

---

## 14. Code-behind API

```csharp
TextElement.SetFontSize(panel, 18);
TextElement.SetFontWeight(panel, FontWeight.Bold);
TextElement.SetLetterSpacing(panel, 1.5);

myTextBlock.FontSize = 24;
myTextBlock.FontWeight = FontWeight.SemiBold;
myTextBlock.TextDecorations = TextDecorations.Underline;
```

---

## Key Takeaways

- **TextElement** attached properties inherit through the visual tree — set on a container to style all descendant text
- **Inlines** (`Run`, `Span`, `LineBreak`, `InlineUIContainer`) enable rich mixed-format text in a single `TextBlock`
- **Text decorations** include four presets and fully customizable lines with stroke, dash, and offset
- **OpenType features** use HarfBuzz tag syntax (`+tnum`, `-liga`, `+smcp`)
- **TextOptions** control rendering quality, hinting, and baseline alignment — important for animations
- **Custom fonts** use `avares://` URIs with `#FontFamilyName` suffix; `EmbeddedFontCollection` enables scheme-based lookups
- **Type scale** is created with `Style` selectors on `TextBlock` classes — no built-in heading styles
- **Font files** must be included as `AvaloniaResource` in the `.csproj`

---

## See Also

- [TextBlock control reference](https://docs.avaloniaui.net/controls/data-display/text-display/textblock)
- [SelectableTextBlock](https://docs.avaloniaui.net/controls/data-display/text-display/selectabletextblock)
- [Custom fonts](https://docs.avaloniaui.net/docs/styling/custom-fonts)
- [Text options](https://docs.avaloniaui.net/docs/graphics-animation/text-options)
- [Typing](https://docs.avaloniaui.net/docs/styling/typography)
