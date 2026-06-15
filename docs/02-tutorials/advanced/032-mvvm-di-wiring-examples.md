---
tier: advanced
topic: di
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 032-mvvm-di-wiring.md
---

# 032X — MVVM Dependency Injection: Real-World Examples

## Scenario 1: Multi-Window Document Editor with Scoped Services

### Goal

Build a document editor where each document window has its own scoped `DocumentViewModel` with isolated undo history, file state, and a shared spell-check service. Use `IServiceScopeFactory` to create per-window scopes.

### Service Registration

```csharp
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DocumentApp.Services;
using DocumentApp.ViewModels;

namespace DocumentApp;

class Program
{
    public static IHost AppHost { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        AppHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServices)
            .Build();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void ConfigureServices(HostBuilderContext context,
        IServiceCollection services)
    {
        // Singleton — shared across all windows
        services.AddSingleton<ISpellCheckService, SpellCheckService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        // Transient — new instance per each document window
        services.AddTransient<DocumentViewModel>();
        services.AddTransient<DocumentWindow>();

        // Factory registration
        services.AddSingleton<Func<DocumentViewModel>>(
            sp => sp.GetRequiredService<DocumentViewModel>);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

### App.axaml.cs — Open the first document window

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DocumentApp.ViewModels;
using DocumentApp.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentApp;

public partial class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Program.AppHost.Services.GetRequiredService<DocumentViewModel>();
            desktop.MainWindow = new DocumentWindow { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

### DocumentViewModel (per-window)

```csharp
using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DocumentApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentApp.ViewModels;

public partial class DocumentViewModel : ObservableObject, IRecipient<FileOpenedMessage>
{
    private readonly ISpellCheckService _spellCheck;
    private readonly IFileService _fileService;
    private readonly IMessenger _messenger;
    private readonly IServiceScope _scope;
    private readonly Stack<IUndoCommand> _undoStack = new();

    [ObservableProperty]
    private string _documentText = string.Empty;

    [ObservableProperty]
    private string _fileName = "Untitled";

    [ObservableProperty]
    private bool _hasChanges;

    public DocumentViewModel(
        ISpellCheckService spellCheck,
        IFileService fileService,
        IMessenger messenger,
        IServiceScopeFactory scopeFactory)
    {
        _spellCheck = spellCheck;
        _fileService = fileService;
        _messenger = messenger;
        _scope = scopeFactory.CreateScope();
        _messenger.RegisterAll(this);
    }

    [RelayCommand]
    private void Save()
    {
        _fileService.Save(FileName, DocumentText);
        HasChanges = false;
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var text = await _fileService.OpenAsync();
        if (text != null)
        {
            DocumentText = text;
            HasChanges = false;
        }
    }

    public void Receive(FileOpenedMessage message)
    {
        // Handles file-open requests from other windows
    }

    // Called by the window close handler
    public void DisposeScope()
    {
        _scope.Dispose();
    }
}
```

### DocumentWindow code-behind

```csharp
using Avalonia.Controls;
using DocumentApp.ViewModels;

namespace DocumentApp.Views;

public partial class DocumentWindow : Window
{
    public DocumentWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is DocumentViewModel vm)
            vm.DisposeScope();

        base.OnClosing(e);
    }
}
```

### How It Works

1. **`DocumentViewModel` is registered as Transient**: Each `IServiceProvider.GetRequiredService<DocumentViewModel>()` creates a new instance with its own undo stack and document state.
2. **`IServiceScopeFactory.CreateScope()`** inside the ViewModel constructor creates a scope. The ViewModel holds the scope reference and disposes it on window close, releasing any scoped resources.
3. **Singleton services** (`ISpellCheckService`, `IFileService`) are shared across all windows. The spell checker maintains a shared dictionary; the file service handles OS file dialogs.
4. **`IMessenger`** (WeakReferenceMessenger) is registered as singleton so all ViewModels share the same message bus. A "File → Open in New Window" command sends a `FileOpenedMessage` that all document ViewModels receive.
5. **`Func<DocumentViewModel>` factory**: Registered for programmatic creation of new document ViewModels (e.g., in a "New Window" menu handler).

### Design Decisions & Edge Cases

- **Scope disposal on window close**: The `OnClosing` override calls `DisposeScope()`, which disposes the DI scope. Important for releasing transient dependencies that implement `IDisposable` (e.g., file handles, network connections). Without this, transients leak until app shutdown.
- **Captive dependency warning**: A singleton service holding a reference to a scoped/transient `DocumentViewModel` would leak the ViewModel. The `IMessenger` uses weak references to avoid this.
- **Multiple windows, same ViewModel type**: Each `DocumentWindow` gets its own `DocumentViewModel`. The `DataContext` is assigned in App.axaml.cs for the first window; subsequent windows create their own via the factory.
- **Testing**: Unit tests create `DocumentViewModel` directly with mock services — no DI container needed. The scope factory is mocked to return a simple `IServiceScope`.

---

## Scenario 2: Plugin Architecture with Keyed Service Registration

### Goal

Design a reporting application where multiple export plugins (PDF, CSV, HTML) are registered as keyed services and resolved at runtime based on user selection. Use `IMessenger` to notify the export ViewModel of plugin availability changes.

### Service Interface and Implementations

```csharp
using System.Threading.Tasks;

namespace ReportApp.Services;

public interface IExportPlugin
{
    string DisplayName { get; }
    string FileExtension { get; }
    Task ExportAsync(string reportContent, string outputPath);
}

public class PdfExportPlugin : IExportPlugin
{
    public string DisplayName => "PDF Document";
    public string FileExtension => ".pdf";

    public async Task ExportAsync(string reportContent, string outputPath)
    {
        // PDF generation logic
        await Task.Delay(200);
    }
}

public class CsvExportPlugin : IExportPlugin
{
    public string DisplayName => "CSV Spreadsheet";
    public string FileExtension => ".csv";

    public async Task ExportAsync(string reportContent, string outputPath)
    {
        await File.WriteAllTextAsync(outputPath, reportContent);
    }
}

public class HtmlExportPlugin : IExportPlugin
{
    public string DisplayName => "HTML Page";
    public string FileExtension => ".html";

    public async Task ExportAsync(string reportContent, string outputPath)
    {
        var html = $"<html><body>{reportContent}</body></html>";
        await File.WriteAllTextAsync(outputPath, html);
    }
}
```

### Service Registration with Keyed Services

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReportApp.Services;
using ReportApp.ViewModels;

namespace ReportApp;

class Program
{
    public static IHost AppHost { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        AppHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServices)
            .Build();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void ConfigureServices(HostBuilderContext context,
        IServiceCollection services)
    {
        // Register export plugins as keyed services (available in .NET 8+)
        services.AddKeyedSingleton<IExportPlugin>("pdf", new PdfExportPlugin());
        services.AddKeyedSingleton<IExportPlugin>("csv", new CsvExportPlugin());
        services.AddKeyedSingleton<IExportPlugin>("html", new HtmlExportPlugin());

        // Keyed service resolver factory
        services.AddSingleton<IExportPluginFactory, ExportPluginFactory>();

        // ViewModels
        services.AddTransient<ReportViewModel>();
        services.AddSingleton<MainViewModel>();

        // Messenger
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

### Factory Service

```csharp
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace ReportApp.Services;

public interface IExportPluginFactory
{
    IExportPlugin GetPlugin(string key);
    IEnumerable<(string Key, string DisplayName)> ListAvailable();
}

public class ExportPluginFactory : IExportPluginFactory
{
    private readonly IServiceProvider _provider;

    public ExportPluginFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IExportPlugin GetPlugin(string key)
    {
        return _provider.GetRequiredKeyedService<IExportPlugin>(key);
    }

    public IEnumerable<(string Key, string DisplayName)> ListAvailable()
    {
        // Keyed services cannot be enumerated from DI directly in M.E.DI.
        // For a full plugin system, register a collection explicitly.
        yield return ("pdf", "PDF Document");
        yield return ("csv", "CSV Spreadsheet");
        yield return ("html", "HTML Page");
    }
}
```

### ReportViewModel

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ReportApp.Services;

namespace ReportApp.ViewModels;

public partial class ReportViewModel : ObservableObject
{
    private readonly IExportPluginFactory _pluginFactory;

    [ObservableProperty]
    private string _reportContent = string.Empty;

    [ObservableProperty]
    private string _selectedFormat = "pdf";

    [ObservableProperty]
    private bool _isExporting;

    public ObservableCollection<ExportFormatItem> AvailableFormats { get; } = new();

    public ReportViewModel(IExportPluginFactory pluginFactory)
    {
        _pluginFactory = pluginFactory;

        foreach (var (key, name) in pluginFactory.ListAvailable())
            AvailableFormats.Add(new ExportFormatItem(key, name));

        if (AvailableFormats.Count > 0)
            SelectedFormat = AvailableFormats[0].Key;
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedFormat))
            return;

        IsExporting = true;

        try
        {
            var plugin = _pluginFactory.GetPlugin(SelectedFormat);
            var path = $"report_{DateTime.Now:yyyyMMdd}{plugin.FileExtension}";
            await plugin.ExportAsync(ReportContent, path);
        }
        finally
        {
            IsExporting = false;
        }
    }
}

public record ExportFormatItem(string Key, string DisplayName);
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ReportApp.ViewModels"
             x:Class="ReportApp.Views.ReportView"
             x:DataType="vm:ReportViewModel">

  <Grid RowDefinitions="*,Auto,Auto" Spacing="{StaticResource Space4}"
        Margin="{StaticResource Space4}">

    <!-- Report content area -->
    <TextBox Grid.Row="0"
             Text="{Binding ReportContent}"
             AcceptsReturn="True"
             Watermark="Enter report content..." />

    <!-- Format selector -->
    <Grid Grid.Row="1" ColumnDefinitions="*,Auto" Spacing="8">
      <ComboBox ItemsSource="{Binding AvailableFormats}"
                SelectedValue="{Binding SelectedFormat}"
                SelectedValuePath="Key"
                DisplayMemberBinding="{Binding DisplayName}" />
    </Grid>

    <!-- Export button with busy indicator -->
    <Button Grid.Row="2"
            Content="{Binding IsExporting,
                     Converter={StaticResource BoolToExportLabel}}"
            Command="{Binding ExportCommand}"
            IsEnabled="{Binding IsExporting, Converter={StaticResource InvertBool}}"
            HorizontalAlignment="Stretch" />
  </Grid>
</UserControl>
```

### How It Works

1. **Keyed services** (`AddKeyedSingleton<IExportPlugin>("pdf", ...)`) register each plugin implementation with a string key. In .NET 8+, `Microsoft.Extensions.DependencyInjection` natively supports keyed DI.
2. **`ExportPluginFactory`** wraps `IServiceProvider.GetRequiredKeyedService<T>(key)` to resolve the correct plugin at runtime. The factory also implements `ListAvailable()` to populate the format ComboBox.
3. **`ReportViewModel`** selects a format via the ComboBox (bound to `SelectedFormat`), then resolves the matching `IExportPlugin` and calls `ExportAsync`. The `IsExporting` flag disables the button during export and shows progress text via a converter.
4. **`IMessenger`** (not shown in this ViewModel but available) enables cross-ViewModel communication. For example, a configuration ViewModel could send `ExportPluginReconfiguredMessage` when a plugin's settings change, and `ReportViewModel` would re-resolve the plugin.

### Design Decisions & Edge Cases

- **Keyed services vs. manual dictionary**: M.E.DI's keyed services provide container-managed lifecycle and disposal. A manual `Dictionary<string, IExportPlugin>` in a factory would work but requires manual singleton management.
- **Unregistered key**: If `SelectedFormat` is set to a key without a registration, `GetRequiredKeyedService` throws `InvalidOperationException`. The `Export` command guard returns early if `SelectedFormat` is empty. Production code should use `GetKeyedService` (returns null) and show a user-friendly error.
- **No enumeration from container**: M.E.DI does not support enumerating keyed services. The factory hardcodes the list in `ListAvailable()`. For a dynamic plugin system (scanning assemblies), register a `IEnumerable<IExportPlugin>` collection explicitly.
- **Export progress**: The `IsExporting` pattern is minimal. For real progress reporting, use `IProgress<double>` injected into the plugin and bind to a `ProgressBar`.

### Comparison

| Aspect | Scenario 1: Document Editor | Scenario 2: Plugin Architecture |
|---|---|---|
| Registration pattern | Transient + Scope | Keyed Singleton + Factory |
| Lifetime per instance | Per-window (scope) | App-wide singleton per key |
| Resolution strategy | `GetRequiredService<T>()` | `GetRequiredKeyedService<T>(key)` |
| Cleanup | Per-window scope disposal | Container-managed (singletons) |
| Service discovery | Implicit (single impl per type) | Explicit via factory enumeration |
| Use case | Multi-window app with isolated state | Runtime polymorphic selection |
| Messaging role | Cross-window coordination | Plugin reconfiguration notifications |

## See Also

- [032 — MVVM Dependency Injection](032-mvvm-di-wiring.md)
- [032V — MVVM Dependency Injection (verbose companion)](032-mvvm-di-wiring-verbose.md)
- [001 — Project Setup](../basics/001-project-setup.md)
- [007 — Observable Object and Property](../basics/007-observable-object-property.md)
- [033 — Localization i18n](033-localization-i18n.md)
- [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md)
- [Avalonia Docs: Dependency Injection](https://docs.avaloniaui.net/docs/guides/implementation-guides/dependency-injection)
- [CommunityToolkit.Mvvm: Messenger docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/messenger)
