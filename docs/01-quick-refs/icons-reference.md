---
topic: graphics
estimated: 3 min read
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Q13 — Icons Reference

## Three approaches

| Approach | When to use |
|---|---|
| Image file (PNG, JPG, BMP, SVG) | Complex logos, photos, multi-color icons |
| Icon font (TTF, OTF, WOFF2) | Monochrome vector icons, easy color/size control |
| Path icon (PathIcon) | Simple SVG paths, single color, theme-aware |

---

## Image files

```xml
<Image Width="16" Height="16"
       Source="avares://MyApp/Assets/icon.png" />
```

Recommended icon sizes: 16×16 for toolbars, 24×24 for buttons, 32×32 for panels.

### Asset setup

Place images in `Assets/` folder. Set build action:

```xml
<ItemGroup>
  <AvaloniaResource Include="Assets\**" />
</ItemGroup>
```

### Embedded vs loose files

| Source | Path |
|---|---|
| Embedded (avares://) | `avares://AssemblyName/Assets/file.png` |
| Loose file | `file:///C:/path/to/file.png` |
| URL | `https://example.com/icon.png` |

---

## Icon fonts

```xml
<TextBlock FontFamily="avares://MyApp/Assets/#FontAwesome"
           Text="&#xf030;" FontSize="16" />
```

### Setup steps

1. Add `.ttf` or `.otf` to `Assets/`.
2. Set build action to `AvaloniaResource`.
3. Reference with `avares://AssemblyName/Assets/#FontName`.
4. Use the Unicode glyph value in `Text`.

Popular icon fonts: FontAwesome (free), Material Design Icons, Fluent UI System Icons (open source).

---

## Path icons

```xml
<PathIcon Data="M 10,100 L 100,10 L 190,100 Z"
          Foreground="{DynamicResource SystemAccentColor}" />
```

`PathIcon` inherits `Foreground` from the parent, making it theme-aware. `PathIcon` is the recommended choice for built-in UI icons.

---

## Menu icons

```xml
<MenuItem Header="Open" Command="{Binding OpenCommand}">
  <MenuItem.Icon>
    <PathIcon Data="..." />
  </MenuItem.Icon>
</MenuItem>
```

---

## Best practices

- Use `PathIcon` for theme-consistent monochrome icons.
- Use icon fonts when you need a large icon set with minimal asset size.
- Use bitmap images only for multi-color or photographic icons.
- Always set `Width` and `Height` on icon images to avoid layout shifts.
- Test icons against both light and dark themes.
