---
tier: advanced
topic: platform-services
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 034-file-pickers-platform-services.md
---

# 034E — File Pickers and Platform Services: Real-World Examples

**What this is:** Two complete, production-oriented examples that apply the file-picker and platform-service APIs from the tutorial to concrete scenarios.

**Prerequisites:** [034 — File Pickers and Platform Services](034-file-pickers-platform-services.md), [034V — Verbose Companion](034-file-pickers-platform-services-verbose.md)

---

## Example 1: Image Import and Metadata Extraction

### Goal

Let the user pick one or more image files via `StorageProvider`, read each file's stream, extract basic metadata (file name, size, dimensions via `SKBitmap`), and display the results in a list with thumbnail previews.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;

namespace DemoApp.ViewModels;

public partial class ImageImportViewModel : ObservableObject
{
    private readonly IStorageProvider _storage;

    public ImageImportViewModel(IStorageProvider storage)
    {
        _storage = storage;
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<ImageEntry> Entries { get; } = new();

    [RelayCommand]
    private async Task ImportImagesAsync()
    {
        if (!_storage.CanOpen)
        {
            ErrorMessage = "File picker is not supported on this platform.";
            return;
        }

        ErrorMessage = null;

        var files = await _storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select images to import",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                FilePickerFileTypes.ImageAll,
                FilePickerFileTypes.All
            }
        });

        if (files.Count == 0)
            return;

        IsLoading = true;

        try
        {
            foreach (var file in files)
            {
                var entry = await LoadImageEntryAsync(file);
                if (entry is not null)
                    Entries.Add(entry);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load one or more files: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static async Task<ImageEntry?> LoadImageEntryAsync(IStorageFile file)
    {
        await using var stream = await file.OpenReadAsync();
        if (stream is null) return null;

        using var skStream = new SKManagedStream(stream);
        using var codec = SKCodec.Create(skStream);
        if (codec is null) return null;

        var info = codec.Info;
        var name = file.Name;
        var size = stream.Length;

        return new ImageEntry(name, size, info.Width, info.Height, info.ColorType.ToString());
    }

    [RelayCommand]
    private void ClearEntries()
    {
        Entries.Clear();
        ErrorMessage = null;
    }
}

public partial class ImageEntry : ObservableObject
{
    [ObservableProperty]
    private string _fileName;

    [ObservableProperty]
    private long _fileSize;

    [ObservableProperty]
    private int _width;

    [ObservableProperty]
    private int _height;

    [ObservableProperty]
    private string _colorType;

    public ImageEntry(string fileName, long fileSize, int width, int height, string colorType)
    {
        _fileName = fileName;
        _fileSize = fileSize;
        _width = width;
        _height = height;
        _colorType = colorType;
    }

    public string FileSizeDisplay => FileSize switch
    {
        < 1024 => $"{FileSize} B",
        < 1048576 => $"{FileSize / 1024.0:F1} KB",
        _ => $"{FileSize / 1048576.0:F1} MB"
    };
}
```

### View (XAML)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DemoApp.ViewModels"
             x:Class="DemoApp.Views.ImageImportView"
             x:DataType="vm:ImageImportViewModel">
  <Grid RowDefinitions="Auto,*,Auto" Margin="16" Spacing="12">
    <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="8">
      <Button Content="Import Images"
              Command="{Binding ImportImagesCommand}" />
      <Button Content="Clear"
              Command="{Binding ClearEntriesCommand}" />
    </StackPanel>

    <Border Grid.Row="1"
            Background="{DynamicResource CardBrush}"
            CornerRadius="8" Padding="8">
      <ScrollViewer>
        <ItemsControl ItemsSource="{Binding Entries}"
                      x:DataType="vm:ImageImportViewModel">
          <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="vm:ImageEntry">
              <Grid ColumnDefinitions="Auto,*,Auto" Margin="0,4" Spacing="12">
                <Border Width="48" Height="48"
                        Background="{DynamicResource SurfaceBrush}"
                        CornerRadius="4" />
                <StackPanel Grid.Column="1" VerticalAlignment="Center">
                  <TextBlock Text="{Binding FileName}"
                             FontWeight="SemiBold" />
                  <TextBlock Text="{Binding FileSizeDisplay}"
                             FontSize="11"
                             Foreground="{DynamicResource SystemAccentColor}" />
                </StackPanel>
                <StackPanel Grid.Column="2"
                            VerticalAlignment="Center"
                            HorizontalAlignment="Right">
                  <TextBlock Text="{Binding Width}"
                             FontSize="11" />
                  <TextBlock Text="{Binding ColorType}"
                             FontSize="11" />
                </StackPanel>
              </Grid>
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
      </ScrollViewer>
    </Border>

    <TextBlock Grid.Row="2"
               Text="{Binding ErrorMessage}"
               Foreground="{DynamicResource ErrorBrush}"
               IsVisible="{Binding ErrorMessage, Converter={StaticResource IsNotNullConverter}}"
               TextWrapping="Wrap" />
  </Grid>
</UserControl>
```

### How It Works

1. **Guard with `CanOpen`** — The command checks `_storage.CanOpen` before showing the picker. On non-Chromium browsers this is `false`, and the ViewModel sets `ErrorMessage` instead of crashing with `PlatformNotSupportedException`.

2. **Multi-file selection** — `AllowMultiple = true` lets the user pick several files at once. The `foreach` loop processes each `IStorageFile` sequentially, so the UI stays responsive between files.

3. **Stream-based decoding** — `file.OpenReadAsync()` returns a `Stream`. On desktop, this is a `FileStream` backed by the real file. On browser/mobile, it is a sandboxed read-only stream. Passing it through `SKManagedStream` lets Skia decode image metadata without writing to disk.

4. **Format fallback for `FileTypeFilter`** — `FilePickerFileTypes.ImageAll` covers PNG, JPEG, GIF, BMP, WebP. `FilePickerFileTypes.All` acts as a catch-all fallback in case the platform does not support the combined filter.

5. **Error state** — If any file fails (corrupt image, denied access), the exception is caught and `ErrorMessage` is set. The successfully processed entries remain in the list — partial success is better than discarding everything.

### Design Decisions and Trade-offs

- **Sequential processing vs parallel:** Parallel processing (`Parallel.ForEach` or `Task.WhenAll`) would be faster but requires dispatching `Entries.Add` to the UI thread. Sequential keeps the code simple and is acceptable for a few dozen files.
- **SkiaSharp dependency:** This example adds a dependency on `SkiaSharp` for metadata extraction. Alternatives include `System.Drawing.Common` (Windows-only) or manual header parsing. For cross-platform, SkiaSharp is the safest choice.
- **No thumbnail rendering:** The example shows a placeholder `Border` instead of an actual thumbnail. A real implementation would decode a scaled-down bitmap via `SKBitmap.Resize` and display it in an `Image` control. This is omitted for brevity.

---

## Example 2: Batch Export with Folder Picker and Progress

### Goal

Let the user pick an output folder via `OpenFolderPickerAsync`, then export a set of generated reports (text files) to that folder with a progress indicator. Each file is created via `IStorageFolder.CreateFileAsync` and written with `OpenWriteAsync`. On platforms without folder picker support, fall back to a save-file picker per file.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DemoApp.ViewModels;

public partial class BatchExportViewModel : ObservableObject
{
    private readonly IStorageProvider _storage;

    public BatchExportViewModel(IStorageProvider storage)
    {
        _storage = storage;
    }

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private int _completedFiles;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _useFolderPicker = true;

    public ObservableCollection<string> ExportedFiles { get; } = new();

    public double Progress => TotalFiles > 0
        ? (double)CompletedFiles / TotalFiles
        : 0.0;

    partial void OnCompletedFilesChanged(int value) => OnPropertyChanged(nameof(Progress));

    [RelayCommand]
    private async Task ExportReportsAsync()
    {
        var reports = GenerateReports();
        TotalFiles = reports.Count;
        CompletedFiles = 0;
        ExportedFiles.Clear();

        if (TotalFiles == 0)
        {
            StatusMessage = "No reports to export.";
            return;
        }

        if (_storage.CanPickFolder && UseFolderPicker)
        {
            await ExportToFolderAsync(reports);
        }
        else
        {
            await ExportOneByOneAsync(reports);
        }
    }

    private async Task ExportToFolderAsync(List<ReportData> reports)
    {
        var folders = await _storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select export destination"
        });

        if (folders.Count == 0) return;

        var folder = folders[0];
        IsExporting = true;
        StatusMessage = $"Exporting to {folder.Name}...";

        try
        {
            foreach (var report in reports)
            {
                var file = await folder.CreateFileAsync($"{report.FileName}.txt");
                if (file is null) continue;

                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(report.Content);

                CompletedFiles++;
                ExportedFiles.Add(report.FileName);
            }

            StatusMessage = $"Exported {CompletedFiles} of {TotalFiles} files.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private async Task ExportOneByOneAsync(List<ReportData> reports)
    {
        IsExporting = true;

        try
        {
            foreach (var report in reports)
            {
                var file = await _storage.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = $"Save {report.FileName}",
                    SuggestedFileName = $"{report.FileName}.txt",
                    DefaultExtension = "txt",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Text Files")
                        {
                            Patterns = new[] { "*.txt" }
                        }
                    }
                });

                if (file is null) continue;

                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(report.Content);

                CompletedFiles++;
                ExportedFiles.Add(report.FileName);
            }

            StatusMessage = $"Exported {CompletedFiles} of {TotalFiles} files.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private static List<ReportData> GenerateReports()
    {
        return Enumerable.Range(1, 10).Select(i => new ReportData
        {
            FileName = $"Report_{i:000}",
            Content = $"Report {i}\nGenerated: {DateTime.Now:O}\nThis is sample content.\n"
        }).ToList();
    }

    private record ReportData
    {
        public required string FileName { get; init; }
        public required string Content { get; init; }
    }
}
```

### View (XAML)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DemoApp.ViewModels"
             x:Class="DemoApp.Views.BatchExportView"
             x:DataType="vm:BatchExportViewModel">
  <Grid RowDefinitions="Auto,Auto,*,Auto" Margin="16" Spacing="12">
    <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="8">
      <Button Content="Export Reports"
              Command="{Binding ExportReportsCommand}"
              IsEnabled="{Binding IsExporting, Converter={StaticResource InvertBool}}" />
      <CheckBox IsChecked="{Binding UseFolderPicker}"
                Content="Use folder picker (when available)" />
    </StackPanel>

    <ProgressBar Grid.Row="1"
                 Value="{Binding Progress}"
                 IsVisible="{Binding IsExporting}"
                 Height="20" />

    <Border Grid.Row="2"
            Background="{DynamicResource CardBrush}"
            CornerRadius="8" Padding="8">
      <ScrollViewer>
        <ItemsControl ItemsSource="{Binding ExportedFiles}"
                      x:DataType="vm:BatchExportViewModel">
          <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="x:String">
              <TextBlock Text="{Binding .}"
                         Margin="0,2" />
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
      </ScrollViewer>
    </Border>

    <TextBlock Grid.Row="3"
               Text="{Binding StatusMessage}"
               Foreground="{DynamicResource SystemAccentColor}"
               TextWrapping="Wrap" />
  </Grid>
</UserControl>
```

### How It Works

1. **Folder picker path** — When `CanPickFolder` is `true`, the user picks a destination folder once. `IStorageFolder.CreateFileAsync` creates each file inside that folder, and `OpenWriteAsync` writes the content. This is the fastest path because the user chooses once.

2. **Per-file save path** — When the folder picker is unavailable (browser, some Linux DEs), or the user unchecks "Use folder picker", the code falls back to `SaveFilePickerAsync` per file. The user confirms each save location. `SuggestedFileName` pre-fills the name so the user only needs to click Save.

3. **Progress tracking** — `CompletedFiles` increments after each successful write. The `Progress` property (derived from `TotalFiles` / `CompletedFiles`) drives the `ProgressBar`. The `partial void OnCompletedFilesChanged` triggers `OnPropertyChanged(nameof(Progress))` to keep the binding updated.

4. **Error handling** — If `CreateFileAsync` returns `null` (the user cancelled), or `OpenWriteAsync` fails, the loop continues to the next file. The exception is caught and displayed in `StatusMessage` without aborting the entire batch.

### Design Decisions and Trade-offs

- **`IStorageFolder.CreateFileAsync` vs local file path** — On desktop, `folder.TryGetLocalPath()` could give a native path, and `File.WriteAllText` would be simpler. But on browser/mobile, `TryGetLocalPath` returns `null`. The `IStorageFolder` API works everywhere.
- **Fallback strategy** — The fallback from folder picker to per-file save picker doubles the code paths but ensures the feature works on every platform. Without the fallback, browser users would see a disabled "Export" button.
- **Progress bar granularity** — The progress bar updates per file, not per byte. For large files, byte-level progress (reporting stream position as percentage of total) would be more accurate but adds complexity proportional to file size.

---

## Comparison: What the Two Examples Demonstrate

| Aspect | Example 1 — Image Import | Example 2 — Batch Export |
|--------|--------------------------|--------------------------|
| Picker type | `OpenFilePickerAsync` (multi) | `OpenFolderPickerAsync` + `SaveFilePickerAsync` |
| File operation | Read (stream decoding) | Write (stream creation) |
| `IStorageFile` source | From picker | From `CreateFileAsync` on folder |
| Platform fallback | `CanOpen` guard | Folder → per-file fallback |
| Progress | Loading spinner (`IsLoading`) | `ProgressBar` with `CompletedFiles` counter |
| Error handling | Per-file catch, partial success | Per-file catch, continue batch |
| Metadata dependency | SkiaSharp (`SKCodec`) | None (pure text) |
| Multiple selection | Yes (`AllowMultiple = true`) | No (one folder, many files) |
| Cancellation | None (simplified) | Not shown — user can close window |

## See Also

- [034 — File Pickers and Platform Services](034-file-pickers-platform-services.md) — the original tutorial
- [034V — File Pickers and Platform Services (verbose companion)](034-file-pickers-platform-services-verbose.md)
- [037 — App Lifetimes and Splash Screen](037-app-lifetimes-splash-screen.md) — wiring platform services at startup
- [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md) — per-platform capabilities
- [Avalonia Docs: StorageProvider](https://docs.avaloniaui.net/docs/concepts/storage-provider)
