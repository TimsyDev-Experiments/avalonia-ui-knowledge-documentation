---
tier: advanced
topic: printing
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 049-printing.md
---

# Quiz — Printing

```quiz
Q: What is the primary method for capturing an Avalonia control's visual content as a bitmap for printing?
A. Control.CaptureAsync() || No such method exists on the Control class.
B. Control.RenderTo() or RenderTargetBitmap.Render() (correct) || RenderTargetBitmap's Render method captures the visual state of a control into a bitmap, which can then be saved or used for PDF generation.
C. Control.Screenshot() || Screenshot is not a Control method; screenshot APIs exist at the window level.
D. VisualTreeHelper.Draw() || There is no VisualTreeHelper.Draw method in Avalonia — WPF has a similar concept but the API differs.
Explanation: The tutorial uses RenderTargetBitmap.Render(control) inside a custom RenderToBitmap extension method to capture the control's visual at a given DPI scale.
```

```quiz
Q: Which SkiaSharp class is used to create a PDF document from rendered Avalonia control bitmaps?
A. SKPdfDocument || No such class exists; SkiaSharp uses a different type for PDF creation.
B. SKDocument (correct) || SKDocument.CreatePdf creates a new PDF document. Pages are started with BeginPage, drawn onto, and ended with EndPage before closing the document.
C. SKCanvas || SKCanvas is the drawing surface for a single page, not the document container.
D. SKFileStream || SKFileStream provides file I/O but does not understand PDF document structure.
Explanation: SKDocument.CreatePdf(filePath) creates a PDF document, SKDocument.BeginPage opens a page for drawing, and EndPage/Close finalizes it.
```

```quiz
Q: What does the .UsePrintingTools() call add to the AppBuilder pipeline?
A. It registers the IPrintManager interface and its platform-specific implementation in the DI container (correct) || UsePrintingTools is an extension method from the PrintingTools library that wires the cross-platform printing adapter and registers IPrintManager for injection.
B. It enables the built-in Avalonia PrintDialog || Avalonia has no built-in PrintDialog — PrintingTools is a third-party library.
C. It installs the SkiaSharp NuGet package automatically || NuGet packages are declared in the project file; UsePrintingTools is purely a runtime registration.
D. It creates a default print queue for the application || The print queue is managed by the OS; PrintingTools abstracts the platform adapter but does not create queues.
Explanation: UsePrintingTools registers IPrintManager with the DI container, enabling ViewModels and services to inject the printer adapter for cross-platform printing.
```

```quiz
Q: How do you print multiple pages with PrintingTools or Skia PDF generation?
A. Use PrintDocument.FromVisual with a PageCount property || PrintDocument.FromVisual captures a single visual — pagination must be done manually.
B. Call SKDocument.CreatePdf for each page || A single SKDocument can hold multiple pages; creating separate documents would produce separate PDF files.
C. Manually paginate by measuring, arranging, and rendering each page separately, then adding each as a page in SKDocument (correct) || The tutorial shows a paginate-and-render loop: for each item, set the DataContext, measure/arrange, render to bitmap, then add as a page in SKDocument.
D. Use the MultiPagePrintCommand from PrintingTools || PrintingTools does not provide an automatic pagination command — pagination is the developer's responsibility.
Explanation: Multi-page documents require manual pagination: iterate over data items, set DataContext on a template, measure/arrange, render each page as a bitmap, and add each bitmap as a separate SKDocument page.
```

```quiz
Q: How do you perform silent printing (no dialog) with the PrintingTools library?
A. Set ShowPrintDialog = false on the PrintOptions and specify a printer name (correct) || This bypasses the native print dialog and sends the document directly to the specified printer.
B. Call _printManager.PrintAsync without a session || PrintAsync requires a session obtained from RequestSessionAsync, which shows the dialog by default.
C. Pass a null PrintRequest to RequestSessionAsync || A null request causes a NullReferenceException; a valid PrintRequest is always required.
D. Set the environment variable AVALONIA_SILENT_PRINT=true || No such environment variable exists in Avalonia or PrintingTools.
Explanation: The tutorial shows that creating a PrintRequest with Options.ShowPrintDialog = false and Options.PrinterName set performs silent/automated printing.
```
