---
tier: advanced
topic: output
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 049-printing.md
---

# 049X — Printing: Real-World Examples

**What you'll build:** An invoice print flow using PrintingTools with a native print dialog and PDF fallback, and a multi-page product catalog exporter using SkiaSharp vector PDF with manual pagination.

**Prerequisites:** [049 — Printing](049-printing.md). The [verbose companion](049-printing-verbose.md) covers `RenderTargetBitmap` internals, PDF coordinate mapping, and the `PrintDocument.FromVisual` contract in depth.

---

## Example 1: Invoice Print with Native Dialog and PDF Fallback

**Goal:** Print an invoice preview to a physical printer using the native dialog, with a fallback that saves the same content as a PDF file.

### Print Service

```csharp
// Services/InvoicePrintService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using PrintingTools;
using SkiaSharp;

namespace MyApp.Services;

public class InvoicePrintService
{
    private readonly IPrintManager _printManager;

    public InvoicePrintService(IPrintManager printManager)
    {
        _printManager = printManager;
    }

    public async Task<bool> PrintToPrinterAsync(Control invoiceVisual)
    {
        var document = PrintDocument.FromVisual(invoiceVisual);
        var request = new PrintRequest(document)
        {
            Options = new PrintOptions { ShowPrintDialog = true },
            Description = "Invoice",
        };

        var session = await _printManager.RequestSessionAsync(request);
        if (session is null)
            return false; // user cancelled

        await _printManager.PrintAsync(session);
        return true;
    }

    public void ExportToPdf(Control invoiceVisual, string filePath)
    {
        var size = invoiceVisual.Bounds.Size;
        const double scale = 2.0;

        var pdfWidth = size.Width * scale / 96f;
        var pdfHeight = size.Height * scale / 96f;

        using var document = SKDocument.CreatePdf(filePath);
        using var pdfPage = document.BeginPage(pdfWidth, pdfHeight);

        var pixelSize = new PixelSize(
            (int)(size.Width * scale),
            (int)(size.Height * scale));
        var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96 * scale, 96 * scale));
        bitmap.Render(invoiceVisual);

        using var skBitmap = bitmap.ToSKBitmap();
        pdfPage.DrawBitmap(skBitmap, new SKPoint(0, 0));
        document.EndPage();
        document.Close();
    }
}
```

### ViewModel

```csharp
// ViewModels/InvoiceViewModel.cs
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyApp.Services;

namespace MyApp.ViewModels;

public partial class InvoiceViewModel : ObservableObject
{
    private readonly InvoicePrintService _printService;
    private readonly Func<Control> _invoiceVisualFactory;

    [ObservableProperty]
    private string _customerName = "Acme Corp";

    [ObservableProperty]
    private decimal _totalAmount = 1249.99m;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isBusy;

    public InvoiceViewModel(
        InvoicePrintService printService,
        Func<Control> invoiceVisualFactory)
    {
        _printService = printService;
        _invoiceVisualFactory = invoiceVisualFactory;
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        IsBusy = true;
        StatusMessage = "Opening print dialog...";

        try
        {
            var visual = _invoiceVisualFactory();
            var result = await _printService.PrintToPrinterAsync(visual);
            StatusMessage = result ? "Print job submitted." : "Print cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Print failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SavePdfAsync()
    {
        IsBusy = true;
        StatusMessage = "Saving PDF...";

        try
        {
            // In a real app, use a SaveFileDialog to pick the path
            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Invoice_{DateTime.Now:yyyyMMdd}.pdf");

            var visual = _invoiceVisualFactory();
            _printService.ExportToPdf(visual, filePath);
            StatusMessage = $"PDF saved to {filePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"PDF export failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### View (Invoice Visual)

```xml
<!-- File: Views/InvoiceView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.InvoiceView"
             x:DataType="vm:InvoiceViewModel">

  <DockPanel Margin="16">
    <!-- Print controls -->
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8"
                Margin="0,0,0,16">
      <Button Content="Print..."
              Command="{Binding PrintCommand}" />
      <Button Content="Save PDF..."
              Command="{Binding SavePdfCommand}" />
      <TextBlock Text="{Binding StatusMessage}"
                 VerticalAlignment="Center"
                 Foreground="Gray" />
    </StackPanel>

    <!-- Invoice content (rendered for print) -->
    <Border BorderBrush="#333" BorderThickness="2" Padding="24"
            Background="White" x:Name="InvoiceContent">
      <StackPanel Spacing="12">
        <TextBlock Text="INVOICE" FontSize="28" FontWeight="Bold" />
        <TextBlock Text="{Binding CustomerName, StringFormat='Customer: {0}'}"
                   FontSize="16" />
        <Separator />
        <TextBlock Text="{Binding TotalAmount, StringFormat='Total: {0:C}'}"
                   FontSize="24" FontWeight="Bold" />
        <TextBlock Text="{Binding StatusMessage}"
                   FontSize="11" Foreground="Gray" />
      </StackPanel>
    </Border>
  </DockPanel>
</UserControl>
```

### Registration and Visual Factory

```csharp
// Program.cs
builder.Services
    .UsePrintingTools()
    .AddSingleton<InvoicePrintService>();

// View resolution — factory provides the visual to the ViewModel
builder.Services.AddTransient<Func<Control>>(sp =>
{
    var window = sp.GetRequiredService<Window>();
    return () => window.FindControl<Border>("InvoiceContent");
});
```

### How It Works

1. The user clicks **Print**. The ViewModel invokes `PrintAsync`.
2. `_invoiceVisualFactory()` resolves the `Border` control named `InvoiceContent` from the current window. The factory pattern keeps the ViewModel free of direct control references.
3. `InvoicePrintService.PrintToPrinterAsync` creates a `PrintDocument.FromVisual(invoiceVisual)`, requests a session (shows native print dialog), and prints if the user accepts.
4. If the user clicks **Save PDF**, `ExportToPdf` renders the same visual to a `RenderTargetBitmap` at 2× scale (192 DPI), then wraps it in an `SKDocument` PDF.
5. The `SaveFileDialog` is omitted for brevity — in a real app, use the storage provider API to let the user pick the save location.

### Key Points

- The factory function (`Func<Control>`) is the MVVM-safe way to pass a control reference. The ViewModel never stores the control — it requests it fresh on each operation.
- `PrintDocument.FromVisual` requires the visual to be measured and arranged. Since the `Border` is in the visible window tree, it is already laid out.
- The PDF export uses `RenderTargetBitmap` at 2× scale for print-quality output (192 DPI). Higher scale factors produce better quality at the cost of larger file sizes and slower rendering.
- Edge case: if the print dialog is cancelled, `RequestSessionAsync` returns null. The ViewModel shows "Print cancelled" — no exception is thrown.
- Edge case: if the visual has zero bounds (collapsed, not in visual tree), `RenderToBitmap` produces an empty bitmap and the PDF is blank. Always verify the visual is measured before printing.

---

## Example 2: Multi-Page Product Catalog PDF Export

**Goal:** Export a list of products as a multi-page PDF catalog, with one product per page using a reusable template control and manual pagination.

### Data Model

```csharp
// Models/Product.cs
namespace MyApp.Models;

public class Product
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public string Description { get; set; } = "";
}
```

### Pagination Service

```csharp
// Services/CatalogExportService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using MyApp.Models;
using SkiaSharp;

namespace MyApp.Services;

public class CatalogExportService
{
    public void ExportToPdf(
        IEnumerable<Product> products,
        Func<Product, Control> pageTemplateFactory,
        string outputPath)
    {
        const double scale = 2.0;
        var pageSize = new Size(8.5 * 96, 11 * 96); // Letter

        using var document = SKDocument.CreatePdf(outputPath);

        foreach (var product in products)
        {
            var page = pageTemplateFactory(product);

            // Measure and arrange the template to fill a letter page
            page.Measure(pageSize);
            page.Arrange(new Rect(new Point(0, 0), pageSize));

            // Render to bitmap
            var pixelSize = new PixelSize(
                (int)(pageSize.Width * scale),
                (int)(pageSize.Height * scale));
            var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96 * scale, 96 * scale));
            bitmap.Render(page);

            // Write to PDF page
            var pdfWidth = pageSize.Width * scale / 96f;
            var pdfHeight = pageSize.Height * scale / 96f;
            using var pdfPage = document.BeginPage(pdfWidth, pdfHeight);
            using var skBitmap = bitmap.ToSKBitmap();
            pdfPage.DrawBitmap(skBitmap, new SKPoint(0, 0));
            document.EndPage();
        }

        document.Close();
    }
}
```

### ViewModel

```csharp
// ViewModels/CatalogViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
    private readonly CatalogExportService _catalogService;

    [ObservableProperty]
    private string _statusMessage = "";

    public ObservableCollection<Product> Products { get; } = new()
    {
        new Product { Name = "Widget Alpha", Category = "Hardware", Price = 29.99m, Description = "A versatile widget for general use." },
        new Product { Name = "Gadget Beta", Category = "Electronics", Price = 149.99m, Description = "Advanced gadget with Bluetooth." },
        new Product { Name = "Tool Gamma", Category = "Hardware", Price = 89.99m, Description = "Heavy-duty tool for professionals." },
        new Product { Name = "Service Delta", Category = "Software", Price = 19.99m, Description = "Monthly subscription service." },
    };

    public CatalogViewModel(CatalogExportService catalogService)
    {
        _catalogService = catalogService;
    }

    [RelayCommand]
    private async Task ExportCatalogAsync()
    {
        StatusMessage = "Generating PDF catalog...";

        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "ProductCatalog.pdf");

            // In production, inject a factory or resolve via visual tree
            Product product = null!;
            _catalogService.ExportToPdf(
                Products,
                p =>
                {
                    product = p;
                    return BuildPageControl(p);
                },
                outputPath);

            StatusMessage = $"Catalog saved to {outputPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private static Control BuildPageControl(Product product)
    {
        return new Border
        {
            Padding = new Thickness(40),
            Background = Avalonia.Media.Brushes.White,
            Child = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = product.Name,
                        FontSize = 28,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                    },
                    new TextBlock
                    {
                        Text = product.Category,
                        FontSize = 14,
                        Foreground = Avalonia.Media.Brushes.Gray,
                    },
                    new Separator(),
                    new TextBlock
                    {
                        Text = product.Price.ToString("C"),
                        FontSize = 22,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                    },
                    new TextBlock
                    {
                        Text = product.Description,
                        FontSize = 14,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    },
                },
            },
        };
    }
}
```

### View

```xml
<!-- File: Views/CatalogView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:models="using:MyApp.Models"
             x:Class="MyApp.Views.CatalogView"
             x:DataType="vm:CatalogViewModel">

  <DockPanel Margin="16">
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8"
                Margin="0,0,0,12">
      <Button Content="Export Catalog PDF"
              Command="{Binding ExportCatalogCommand}" />
      <TextBlock Text="{Binding StatusMessage}"
                 VerticalAlignment="Center" Foreground="Gray" />
    </StackPanel>

    <DataGrid ItemsSource="{Binding Products}"
              AutoGenerateColumns="True"
              IsReadOnly="True" />
  </DockPanel>
</UserControl>
```

### How It Works

1. The user clicks **Export Catalog PDF**. The ViewModel calls `CatalogExportService.ExportToPdf`.
2. `ExportToPdf` iterates the product list. For each product, it calls `pageTemplateFactory(product)`, which creates a new `Border`-based control tree configured for that product's data.
3. Each template control is measured at Letter size (8.5×11 inches) and arranged to fill that rectangle. `Measure` calculates desired sizes; `Arrange` sets the final bounds.
4. The control is rendered to a `RenderTargetBitmap` at 2× scale (192 DPI). The bitmap captures the full visual tree including text, colors, and layout.
5. Each bitmap is written to a separate PDF page via `SKDocument.BeginPage`/`EndPage`.
6. The resulting PDF contains one page per product, each page showing the product name, category, price, and description in a clean layout.

### Key Points

- Manual `Measure`/`Arrange` is required because the template controls are not in the visual tree. Without it, their `Bounds` are empty and rendering produces a blank page.
- Creating a new control per product is simple but allocates UI objects for every item. For catalogs with hundreds of products, reuse a single template control and swap its `DataContext` instead (see the verbose companion pattern).
- The page size (8.5×11 inches at 96 DPI = 816×1056 pixels) is Letter format. For A4, use 794×1122 pixels.
- The `SKDocument` creates a PDF with one page per `BeginPage`/`EndPage` pair. The PDF will have exactly as many pages as products.
- Edge case: if a product's content is longer than the page, it is clipped. The current example does not handle content overflow — a production version would measure text height and paginate within a single product's content.
- Edge case: the `BuildPageControl` method creates controls in code rather than loading an `.axaml` template. This avoids the complexity of loading a `DataTemplate` outside the visual tree. For complex templates, use `AvaloniaRuntimeXamlLoader` or predefine a `UserControl` and instantiate it.

---

## What These Examples Demonstrate

| Scenario | Approach | Key technique |
|---|---|---|
| Invoice print | PrintingTools (native dialog) + Skia PDF fallback | `PrintDocument.FromVisual`, `IPrintManager`, factory pattern for control access |
| Product catalog | SkiaSharp multi-page PDF with manual layout | `Measure`/`Arrange` loop, per-page `SKDocument`, code-built controls |

The invoice example is interactive — the user triggers print or PDF save and sees a dialog. The catalog example is batch-oriented — generate a complete document from a list with no per-page user interaction. Both use `RenderTargetBitmap` to capture visual state, but the catalog example shows the off-screen rendering path with explicit layout.

## See Also

- [049 — Printing](049-printing.md)
- [049V — Verbose Companion](049-printing-verbose.md)
- [028 — Custom Drawing with Skia](028-custom-drawing-skia.md)
- [PrintingTools GitHub](https://github.com/wieslawsoltes/PrintingTools)
