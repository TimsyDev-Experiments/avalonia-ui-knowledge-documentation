---
topic: text
estimated: 3 min read
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Font & Typography Reference

## FontFamily URI schemes

```xml
<!-- Embedded in assembly -->
<TextBlock FontFamily="avares://MyApp/Assets/#FontName" />

<!-- System font -->
<TextBlock FontFamily="Arial" />

<!-- Fallback chain -->
<TextBlock FontFamily="Segoe UI, Arial, sans-serif" />
```

## Font weight / style / stretch

```xml
<TextBlock FontWeight="Bold" FontStyle="Italic" FontStretch="Condensed" />
```

| Weight | Value |
|---|---|
| `Thin` | 100 |
| `Light` | 300 |
| `Normal` / `Regular` | 400 |
| `Medium` | 500 |
| `SemiBold` / `DemiBold` | 600 |
| `Bold` | 700 |
| `Black` / `Heavy` | 900 |

| Style | Value |
|---|---|
| `Normal` | 0 |
| `Italic` | 1 |

## TextElement attached properties

Apply text properties to an element hierarchy:

```xml
<StackPanel TextElement.FontSize="14"
            TextElement.FontWeight="SemiBold"
            TextElement.Foreground="Navy">
  <TextBlock Text="All children inherit these" />
  <TextBlock Text="Unless overridden" FontSize="12" />
</StackPanel>
```

## FormattedText

```csharp
var ft = new FormattedText(
    "Hello",
    Typeface.Default,
    TextAlignment.Left,
    TextWrapping.NoWrap,
    new Size(200, 100));
ft.SetFontSize(24, 0, 5);
```

## FontManager

```csharp
// List installed fonts
foreach (var name in FontManager.Current.GetInstalledFontFamilyNames())
    Console.WriteLine(name);
```

## Custom fonts — setup

```xml
<ItemGroup>
  <AvaloniaResource Include="Assets\Fonts\**" />
</ItemGroup>
```

```csharp
// Load at startup
FontManager.Current.AddFontCollection(
    new EmbeddedFontCollection(
        new Uri("fonts://myFonts", UriKind.Absolute),
        new Uri("avares://MyApp/Assets/Fonts", UriKind.Absolute)));
```

## GlyphRun (advanced)

```csharp
var glyphRun = new GlyphRun(
    Typeface.Default,
    48,
    "A",
    null!,
    new Point(0, 0));
// Pass to DrawingContext.DrawGlyphRun
```
