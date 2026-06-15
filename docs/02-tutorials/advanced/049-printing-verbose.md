---
tier: advanced
topic: output
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 049-printing.md
---

# 049V — Printing: An In-Depth Companion

**What you'll learn in this companion:** Not just how to call `RenderToBitmap`, but how Avalonia's rendering pipeline produces a bitmap from a control, why `RenderTargetBitmap` needs DPI and pixel size separately, how `SKDocument.CreatePdf` maps screen coordinates to PDF points, why the PrintingTools library exists, and when each approach (bitmap, PDF, native print) is the correct choice.

**Prerequisites:** [010 — Window Basics & Simple Dialogs](../basics/010-window-dialog-basics.md), [028 — Custom Drawing with Skia](028-custom-drawing-skia.md)

**You should already have read:** [049 — Printing](049-printing.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Why Avalonia Has No Built-in PrintDialog

WPF had `PrintDialog` because it ran only on Windows, where the Win32 print dialog API (`PrintDlgEx`) was always available. Avalonia runs on Windows, macOS, Linux, and WebAssembly — each with a completely different print subsystem:

- **Windows:** XPS print path, Win32 printing, or modern UWP print
- **macOS:** AppKit's `NSPrintOperation` with Quartz rendering
- **Linux:** CUPS via GTK's print dialog
- **Browser:** JavaScript `window.print()`

There is no cross-platform print dialog API in .NET. The third-party libraries fill this gap.

The tutorial covers three approaches because each solves a different problem:

| Approach | Use when |
|---|---|
| Bitmap render | You need a quick image export (PNG, JPG) for email or preview |
| PDF export | You need a distributable document (invoice, report) |
| PrintingTools | You need a native print dialog with page setup and printer selection |

---

## 2. RenderTargetBitmap — How It Captures a Control

```csharp
public static SKBitmap RenderToBitmap(this Control control, double scale = 2.0)
{
    var size = control.Bounds.Size;
    var pixelSize = new PixelSize(
        (int)(size.Width * scale),
        (int)(size.Height * scale));

    var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96 * scale, 96 * scale));
    bitmap.Render(control);
    return bitmap;
}
```

### The DPI parameter

`RenderTargetBitmap` takes two parameters:
1. `pixelSize` — how many pixels wide and tall the bitmap should be
2. `dpi` — the dots-per-inch for the bitmap metadata

The second parameter does not affect the rendered content — it sets the DPI metadata in the bitmap headers. The `Render` method draws the control at the pixel resolution of the render target, scaled by `dpi / 96`.

At scale = 2.0:
- A control that is 400×300 dips becomes an 800×600 pixel bitmap
- The DPI is 192 (2 × 96), so the physical size at 192 DPI is still 400×300 inches-equivalent

### Why Render might capture an empty or partially-rendered control

`Render` captures the control's current visual state. If the control has never been measured and arranged (i.e., it's not part of a visible window), its `Bounds` may be empty (width/height = 0) and nothing renders.

To render an off-screen control, you must manually call `Measure` and `Arrange`:

```csharp
control.Measure(new Size(desiredWidth, desiredHeight));
control.Arrange(new Rect(new Size(desiredWidth, desiredHeight)));
control.RenderToBitmap();
```

This is the pattern used in the multi-page document section: measure, arrange, then render per page.

### What Render does internally

`RenderTargetBitmap.Render(control)` triggers the following pipeline:

1. The compositor queues a render pass for the specified control and its visual subtree
2. Each control calls its `Render` method (if it has one) or uses the default visual rendering
3. SkiaSharp draws all visuals to the bitmap's Skia surface
4. The result is a fully-rendered bitmap including effects, opacity, transforms, and clipping

The render is synchronous — after `Render` returns, the bitmap is complete and can be saved.

---

## 3. PDF Export via SKDocument — The Coordinate Mapping

```csharp
public static void RenderToPdf(this Control control, string filePath, double scale = 2.0)
{
    var size = control.Bounds.Size;
    var pdfSize = new SKSize(
        size.Width * scale / 96f,
        size.Height * scale / 96f);

    using var document = SKDocument.CreatePdf(filePath);
    using var page = document.BeginPage(pdfSize.Width, pdfSize.Height);
    var bitmap = control.RenderToBitmap(scale);
    using var skBitmap = bitmap.ToSKBitmap();
    page.DrawBitmap(skBitmap, new SKPoint(0, 0));
    document.EndPage();
    document.Close();
}
```

### The division by 96

PDF uses **points** as the unit of measurement (1 point = 1/72 inch). Screen coordinates are in **device-independent pixels** (96 DPI by convention). Converting screen size to PDF size:

```
pdf_width = dips_width * scale / 96
pdf_height = dips_height * scale / 96
```

At scale = 2.0, a 400×300 dip control becomes a PDF box that is approximately 8.33×6.25 inches (400*2/96 = 8.33, 300*2/96 = 6.25).

### What SKDocument.CreatePdf produces

`SKDocument.CreatePdf` creates a PDF file using SkiaSharp's PDF backend. The PDF contains:

- A single page (or multiple pages if you call `BeginPage`/`EndPage` multiple times)
- The bitmap image embedded as a compressed image stream
- Metadata (PDF version, document info)

The output is a **flat image in a PDF wrapper** — not vector text. If you need searchable text or vector graphics, you must draw with SkiaSharp APIs directly instead of rendering a bitmap:

```csharp
using var document = SKDocument.CreatePdf(filePath);
using var page = document.BeginPage(width, height);
// Draw vector content directly with SkiaSharp
page.DrawText(...);  // vector text
page.DrawRect(...);  // vector shapes
document.EndPage();
document.Close();
```

The bitmap approach is simpler (capture any visual exactly as seen), but the vector approach produces smaller, zoomable PDFs.

---

## 4. PrintingTools — Why a Library Exists for Print

### What PrintingTools does

[PrintingTools](https://github.com/wieslawsoltes/PrintingTools) wraps the native print APIs of each platform behind a common `IPrintManager` interface. The `.UsePrintingTools()` extension method in `AppBuilder` registers the platform-specific implementation:

- **Windows:** Uses `System.Printing` (XPS print path)
- **macOS:** Uses `NSPrintOperation`
- **Linux:** Uses CUPS via `lp` command

### IPrintManager contract

```csharp
public interface IPrintManager
{
    Task<PrintSession?> RequestSessionAsync(PrintRequest request);
    Task PrintAsync(PrintSession session);
    Task<PrintPreview> CreatePreviewAsync(PrintSession session);
}
```

`RequestSessionAsync` shows the native print dialog if `ShowPrintDialog` is true. It returns `null` if the user cancels. When you get a valid session, `PrintAsync` submits the document to the printer.

### Why RequestSessionAsync exists separately from PrintAsync

The two-step flow lets you:
1. Show the dialog, let the user choose printer and settings
2. Preview or modify the document based on those settings
3. Submit the print job

This is the same model as WPF's `PrintDialog.PrintDocument` — show dialog, then print.

### Printing from a ViewModel — the control injection problem

```csharp
public PrintViewModel(IPrintManager printManager, Control targetControl)
```

Injecting a `Control` directly into a ViewModel violates MVVM (the ViewModel should have no reference to UI elements). The practical workaround:

1. Have the ViewModel expose a **request** object (data, not controls)
2. The View resolves the control and calls the print service
3. Or use an abstraction like `IReportRenderer : IPrintDocumentFactory`

For simple cases, the Direct control reference works fine — it just makes the ViewModel less testable.

---

## 5. PrintDocument.FromVisual — What It Needs

```csharp
var document = PrintDocument.FromVisual(_targetControl);
```

`FromVisual` creates a `PrintDocument` from any Avalonia `Visual`. The visual should be fully measured and arranged (visible or off-screen). The printed output uses the visual's current layout — if the control is collapsed or has zero size, nothing prints.

PrintingTools wraps the visual into a platform-specific print document. The document is not rendered until `PrintAsync` is called.

---

## 6. Silent Printing — When and Why

```csharp
var request = new PrintRequest(document)
{
    Options = new PrintOptions
    {
        ShowPrintDialog = false,
        PrinterName = "Microsoft Print to PDF",
    },
};
```

Silent printing bypasses the print dialog — the document goes directly to the specified printer. Use cases:

- **Auto-print reports:** Generate and print an invoice without user interaction
- **Print to PDF:** Save reports as PDF by routing to "Microsoft Print to PDF" or "Save as PDF"
- **Batch printing:** Print multiple documents in sequence without dialog for each

Setting `PrinterName` is optional. If omitted, the system default printer is used. If the printer is not found, `PrintAsync` throws.

---

## 7. Multi-Page Documents — Why Manual Pagination Is Required

```csharp
public List<SKBitmap> PaginateAndRender(Control template, IEnumerable<object> items, double scale = 2.0)
{
    var pages = new List<SKBitmap>();
    var pageSize = new Size(8.5 * 96, 11 * 96); // Letter

    foreach (var item in items)
    {
        template.DataContext = item;
        template.Measure(pageSize);
        template.Arrange(new Rect(pageSize));
        var bitmap = template.RenderToBitmap(scale);
        pages.Add(bitmap);
    }
    return pages;
}
```

### Why manual measure/arrange

Controls need to know their size to render. When a control is off-screen (not in a window), it has no layout pass. Calling `Measure` and `Arrange` manually tells the control what size it should occupy.

`Measure` determines the desired size. `Arrange` sets the actual bounds. Both are required before `Render` produces meaningful output.

### Setting DataContext per item

```csharp
template.DataContext = item;
```

Reusing a single template control and swapping its `DataContext` is more efficient than creating a new control per item. But it only works if the template's bindings are the same for all items — if different items need different templates, this pattern breaks.

### Page size constants

- Letter: 8.5 × 11 inches = 816 × 1056 pixels at 96 DPI
- A4: 8.27 × 11.69 inches = 794 × 1122 pixels at 96 DPI

The tutorial uses `8.5 * 96` for Letter with a 96 DPI page. At scale 2.0, the bitmap is 1632 × 2112 pixels (2× for print quality).

---

## 8. Platform-Specific Print Limitations

| Platform | Limitation |
|---|---|
| Windows | XPS print path ignores some SkiaSharp rendering features (gradients, masks) |
| macOS | Print dialog is native but async — the `PrintAsync` task completes when the dialog closes, not when printing finishes |
| Linux | CUPS must be installed. The `lp` fallback does not show a print dialog |
| Browser | PrintingTools does not support browser. Use `window.print()` via JavaScript interop |

### Testing printing on Linux

On a headless Linux system (CI runner, server), CUPS may not be installed. PrintingTools silently fails or throws if `lp` is not available. Test printing in a desktop environment with CUPS installed.

---

## 9. When to Use Each Approach

| Scenario | Approach |
|---|---|
| User clicks "Print" for an invoice | PrintingTools with ShowPrintDialog = true |
| Scheduled report auto-saves as PDF | SkiaSharp PDF export (no dialog needed) |
| Email a screenshot of a chart | `RenderToBitmap` + PNG save |
| Print multiple pages from a list | Measure/Arrange loop + SKDocument multi-page |
| Cross-platform with native dialog | PrintingTools |

---

## Key Takeaways

- `RenderTargetBitmap` captures any visual to a bitmap at configurable DPI — the DPI parameter affects metadata, not rendering
- PDF export via `SKDocument.CreatePdf` embeds a bitmap image; for vector PDF, draw directly with SkiaSharp
- PrintingTools provides `IPrintManager` as a cross-platform abstraction; use `.UsePrintingTools()` in AppBuilder
- Silent printing bypasses the dialog — specify `ShowPrintDialog = false` and optionally `PrinterName`
- Multi-page documents require manual `Measure`/`Arrange` per page before rendering
- Always render off-screen controls with explicit Measure/Arrange before calling `RenderToBitmap`
- Injecting UI controls into ViewModels violates MVVM — prefer a `PrintDocument` factory abstraction

---

## See Also

- [049 — Printing (original)](049-printing.md)
- [049X — Printing (examples)](049-printing-examples.md)
- [028 — Custom Drawing with Skia](028-custom-drawing-skia.md)
- [030 — Parcel Packaging & Distribution](030-parcel-packaging.md)
- [PrintingTools GitHub](https://github.com/wieslawsoltes/PrintingTools)
- [SkiaSharp PDF docs](https://docs.microsoft.com/en-us/dotnet/api/skiasharp.skdocument)
