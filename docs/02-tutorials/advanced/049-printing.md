---
tier: advanced
topic: output
estimated: 15 min
researched: 2026-06-13
avalonia-version: 12.0.4
---

# 049 -- Printing

**What you'll learn:** Print visual content from an Avalonia app by rendering controls to bitmaps, exporting to PDF via Skia, and using the cross-platform PrintingTools library.

**Prerequisites:** [010 -- Window Basics & Simple Dialogs](../basics/010-window-dialog-basics.md)

---

Avalonia has no built-in `PrintDialog` (unlike WPF). Printing is handled through platform-specific APIs or third-party libraries. The three practical approaches are covered below.

## 1. Render a control to a bitmap

Use `Control.RenderTo` to capture any visual as a bitmap, then save or send it:

```csharp
using SkiaSharp;

public static class PrintExtensions
{
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
}
```

Usage:

```csharp
var bitmap = myControl.RenderToBitmap();
bitmap.Save("print-output.png");
```

`RenderToBitmap` captures the control's current visual state including any scrolling, selection, and theme — ideal for "print view" of a report panel or invoice.

## 2. Export to PDF via Skia

```shell
dotnet add package SkiaSharp
```

Render the control into a PDF document using `SKDocument`:

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
    using var skBitmap = bitmap.ToSKBitmap(); // from RenderTargetBitmap

    page.DrawBitmap(skBitmap, new SKPoint(0, 0));
    document.EndPage();
    document.Close();
}
```

This produces a vector-quality PDF from any Avalonia visual, suitable for printing or sharing.

## 3. PrintingTools library (cross-platform)

[PrintingTools](https://github.com/wieslawsoltes/PrintingTools) provides a unified `IPrintManager` API across Windows, macOS, and Linux.

```shell
dotnet add package PrintingTools
```

### 3a. Setup in Program.cs

```csharp
// Program.cs
builder = AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .UsePrintingTools()         // wires platform adapter
    .LogToTrace();
```

### 3b. Print from a ViewModel

```csharp
public partial class PrintViewModel : ObservableObject
{
    private readonly IPrintManager _printManager;
    private readonly Control _targetControl;   // injected or resolved

    public PrintViewModel(IPrintManager printManager, Control targetControl)
    {
        _printManager = printManager;
        _targetControl = targetControl;
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        var document = PrintDocument.FromVisual(_targetControl);
        var request = new PrintRequest(document)
        {
            Options = new PrintOptions { ShowPrintDialog = true },
            Description = "Invoice Report",
        };

        var session = await _printManager.RequestSessionAsync(request);
        if (session is not null)
            await _printManager.PrintAsync(session);
    }

    [RelayCommand]
    private async Task PreviewAsync()
    {
        var document = PrintDocument.FromVisual(_targetControl);
        var request = new PrintRequest(document);
        var session = await _printManager.RequestSessionAsync(request);

        if (session is not null)
        {
            var preview = await _printManager.CreatePreviewAsync(session);
            // Show in a PrintPreviewWindow or similar
        }
    }
}
```

### 3c. Silent printing (no dialog)

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

## 4. Print a multi-page document

For content that spans multiple pages, paginate manually and render each page:

```csharp
public List<SKBitmap> PaginateAndRender(Control template, IEnumerable<object> items, double scale = 2.0)
{
    var pages = new List<SKBitmap>();
    var pageSize = new Size(8.5 * 96, 11 * 96); // Letter size in pixels at 96 DPI

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

Render each page as a separate PDF page using `SKDocument`:

```csharp
using var doc = SKDocument.CreatePdf("multi-page.pdf");
foreach (var pageBitmap in pages)
{
    var skPage = doc.BeginPage(pageBitmap.Width, pageBitmap.Height);
    skPage.DrawBitmap(pageBitmap.ToSKBitmap(), new SKPoint(0, 0));
    doc.EndPage();
}
doc.Close();
```

## 5. Platform-specific notes

| Platform | Native print API | PrintingTools support | Notes |
|---|---|---|---|
| Windows | Win32 / XPS | Yes | Uses XPS print path; PDF viewers also work via shell print |
| macOS | AppKit | Yes | Native print dialog via `NSPrintOperation` |
| Linux | CUPS / GTK | Yes | Falls back to `lp` command; CUPS must be installed |
| Browser | N/A | No | Use PDF download + browser's built-in print |

## Key takeaways

- Avalonia has no built-in `PrintDialog` — use `RenderTargetBitmap` + Skia PDF export or the PrintingTools library
- `Control.RenderToBitmap()` captures any visible control at any DPI scale
- `SKDocument.CreatePdf()` produces vector PDFs from rendered bitmaps
- PrintingTools provides the closest experience to WPF's `PrintDialog` across all three desktop platforms
- For silent/automated printing, set `ShowPrintDialog = false` and specify a printer name
- Multi-page documents require manual pagination — measure, arrange, render per page

---

## See Also

- [028 -- Custom Drawing with Skia](028-custom-drawing-skia.md)
- [030 -- Parcel Packaging & Distribution](030-parcel-packaging.md)
- [PrintingTools GitHub](https://github.com/wieslawsoltes/PrintingTools)
- [049V -- Printing (verbose companion)](049-printing-verbose.md)
- [049X -- Printing (examples)](049-printing-examples.md)
