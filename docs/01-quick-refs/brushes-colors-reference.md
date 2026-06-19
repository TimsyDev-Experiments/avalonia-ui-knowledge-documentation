---
topic: graphics
estimated: 3 min read
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Brushes & Colors Reference

## Brush types

| Brush | Description |
|---|---|
| `SolidColorBrush` | Single flat color |
| `LinearGradientBrush` | Gradient along a line |
| `RadialGradientBrush` | Gradient from a center point |
| `ImageBrush` | Tiled or stretched image |
| `VisualBrush` | Content from another visual |
| `AcrylicBrush` | Frosted-glass blur (platform-dependent) |

## SolidColorBrush

```xml
<Border Background="Red" />
<Border Background="#FF3366" />
<Border Background="{StaticResource SystemAccentColor}" />
```

```csharp
new SolidColorBrush(Colors.Navy);
new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));  // semi-transparent red
```

## LinearGradientBrush

```xml
<Rectangle Fill="White">
  <Rectangle.Fill>
    <LinearGradientBrush StartPoint="0,0" EndPoint="1,0">
      <GradientStop Color="Blue" Offset="0.0" />
      <GradientStop Color="White" Offset="0.5" />
      <GradientStop Color="Red" Offset="1.0" />
    </LinearGradientBrush>
  </Rectangle.Fill>
</Rectangle>
```

## RadialGradientBrush

```xml
<RadialGradientBrush Center="0.5,0.5" GradientOrigin="0.5,0.5" Radius="0.5">
  <GradientStop Color="Yellow" Offset="0.0" />
  <GradientStop Color="OrangeRed" Offset="1.0" />
</RadialGradientBrush>
```

## AcrylicBrush (Windows 10+)

```xml
<ExperimentalAcrylicBorder>
  <ExperimentalAcrylicBorder.Material>
    <ExperimentalAcrylicMaterial BackgroundSource="AcrylicBackgroundSource"
                                  TintColor="White" TintOpacity="0.6"
                                  FallbackColor="White" />
  </ExperimentalAcrylicBorder.Material>
</ExperimentalAcrylicBorder>
```

## Common named colors

| Color | Hex |
|---|---|
| `Transparent` | `#00FFFFFF` |
| `White` | `#FFFFFF` |
| `Black` | `#000000` |
| `Gray` | `#808080` |
| `Silver` | `#C0C0C0` |
| `Red` | `#FF0000` |
| `Green` | `#008000` |
| `Blue` | `#0000FF` |
| `Yellow` | `#FFFF00` |
| `Orange` | `#FFA500` |

All `System.Windows.Media.Colors` static properties available in Avalonia via `Colors.*`.

## Immutable variants

```csharp
// Use for shared/cached brushes (thread-safe, no property change notifications)
new ImmutableSolidColorBrush(Colors.Navy);
new ImmutableLinearGradientBrush(...);
```
