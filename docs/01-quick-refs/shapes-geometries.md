---
topic: graphics
estimated: 3 min read
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Q12 — Shapes & Geometries

## Shape controls

All shapes derive from `Shape` and support `Fill`, `Stroke`, `StrokeThickness`, `Stretch`.

| Control | Description | Key Properties |
|---|---|---|
| `Rectangle` | Rectangle with optional rounded corners | `CornerRadius` (TL/TR/BR/BL) |
| `Ellipse` | Circle or ellipse | `Width`, `Height` |
| `Line` | Straight line | `X1`, `Y1`, `X2`, `Y2` |
| `Polyline` | Connected line segments | `Points` (space/comma-separated) |
| `Polygon` | Closed shape from points | `Points` |
| `Path` | Arbitrary geometry | `Data` (Geometry) |

```xml
<Rectangle Width="100" Height="50" Fill="Blue" CornerRadius="8" />
<Ellipse Width="80" Height="80" Stroke="Red" StrokeThickness="2" />
<Line X1="0" Y1="0" X2="100" Y2="100" Stroke="Black" />
```

## Geometry types

Used in `Path.Data` and `Drawing` objects.

| Geometry | Description |
|---|---|
| `RectangleGeometry` | Rectangular region | `Rect` |
| `EllipseGeometry` | Elliptical region | `Rect`, `RadiusX`, `RadiusY` |
| `LineGeometry` | Single line segment | `StartPoint`, `EndPoint` |
| `PathGeometry` | Arbitrary path from figures | `Figures` |
| `CombinedGeometry` | Boolean combination of two geometries | `Geometry1`, `Geometry2`, `GeometryCombineMode` |
| `StreamGeometry` | Lightweight read-only path (mini-language) | Path string |

## Path mini-language

```
M x,y   Move to (absolute)
m x,y   Move to (relative)
L x,y   Line to
l x,y   Line to (relative)
H x     Horizontal line
V y     Vertical line
C x1 y1 x2 y2 x y  Cubic bezier
Q x1 y1 x y  Quadratic bezier
A rx ry x-axis-rotation large-arc sweep x y  Arc
Z       Close path
```

```xml
<Path Data="M 10,100 L 100,10 L 190,100 Z"
      Fill="Green" Stroke="DarkGreen" StrokeThickness="2" />
```

## CombinedGeometry

```xml
<Path Fill="Purple">
  <Path.Data>
    <CombinedGeometry GeometryCombineMode="Exclude">
      <CombinedGeometry.Geometry1>
        <EllipseGeometry Rect="0,0,100,100" />
      </CombinedGeometry.Geometry1>
      <CombinedGeometry.Geometry2>
        <EllipseGeometry Rect="25,25,50,50" />
      </CombinedGeometry.Geometry2>
    </CombinedGeometry>
  </Path.Data>
</Path>
```

| GeometryCombineMode | Effect |
|---|---|
| `Union` | All area from both |
| `Intersect` | Only overlapping area |
| `Exclude` | Geometry1 minus overlap |
| `Xor` | Non-overlapping area |

## Transform shortcuts

```xml
<Rectangle RenderTransform="rotate(45)" />
<Rectangle RenderTransform="scale(1.5,0.5)" />
<Rectangle RenderTransform="translate(10,20)" />
```

Full syntax: `<RotateTransform Angle="45" />`, `<ScaleTransform ScaleX="1.5" />`, etc.
