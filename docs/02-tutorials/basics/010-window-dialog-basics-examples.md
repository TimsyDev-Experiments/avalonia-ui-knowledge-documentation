---
tier: basics
topic: windows
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 010-window-dialog-basics.md
---

# 010X — Window Basics and Simple Dialogs: Real-World Examples

**What you'll build:** A document editor with a "save changes" confirmation dialog and a file-picker-based import wizard — two scenarios that demonstrate modal dialog results, `TopLevel.GetTopLevel`, `StorageProvider` usage, and window lifecycle coordination.

**Prerequisites:** [010 — Window Basics and Simple Dialogs](010-window-dialog-basics.md). The [verbose companion](010-window-dialog-basics-verbose.md) covers the `TopLevel` class hierarchy, `ShowDialog` mechanics, and `Close(bool?)` result propagation in depth.

---

## Example 1: Document Editor with "Save Changes" Confirmation

**Goal:** Build a document editor where closing the window with unsaved changes shows a confirmation dialog. The dialog returns a tri-state result (Save, Discard, Cancel), and the editor window respects the user's choice.

This scenario demonstrates: intercepting `Window.Closing` to show a modal dialog, passing a result back from the dialog, and conditionally canceling the close operation.

### Main window ViewModel

```csharp
// ViewModels/DocumentEditorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class DocumentEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _documentText = string.Empty;

    [ObservableProperty]
    private string _fileName = "Untitled.txt";

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    public bool IsNewDocument => FileName == "Untitled.txt";

    // Track changes
    partial void OnDocumentTextChanged(string value)
    {
        HasUnsavedChanges = true;
    }

    partial void OnFileNameChanged(string value)
    {
        OnPropertyChanged(nameof(IsNewDocument));
    }

    [RelayCommand]
    private void Save()
    {
        // Persist document (simulated)
        HasUnsavedChanges = false;
        System.Diagnostics.Debug.WriteLine($"Saved: {FileName}");
    }

    /// <summary>
    /// Called from code-behind before closing.
    /// Returns true if the window should proceed closing, false to cancel.
    /// </summary>
    public async Task<bool> TryCloseAsync(Window owner)
    {
        if (!HasUnsavedChanges)
            return true;

        var dialog = new ConfirmSaveDialog();
        dialog.DataContext = new ConfirmSaveViewModel
        {
            FileName = FileName,
        };

        var result = await dialog.ShowDialog<ConfirmSaveDialog?>(owner);

        return result?.Result switch
        {
            ConfirmSaveAction.Save => true,   // proceed after save
            ConfirmSaveAction.Discard => true, // proceed without saving
            ConfirmSaveAction.Cancel => false, // cancel close
            _ => false,
        };
    }
}
```

### Confirmation dialog ViewModel

```csharp
// ViewModels/ConfirmSaveViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public enum ConfirmSaveAction { Save, Discard, Cancel }

public partial class ConfirmSaveViewModel : ObservableObject
{
    [ObservableProperty]
    private string _fileName = string.Empty;

    public ConfirmSaveAction Result { get; set; }
}
```

### Confirmation dialog window

```xml
<!-- Views/ConfirmSaveDialog.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:MyApp.ViewModels"
        x:Class="MyApp.Views.ConfirmSaveDialog"
        x:DataType="vm:ConfirmSaveViewModel"
        Title="Unsaved Changes"
        SizeToContent="WidthAndHeight"
        WindowStartupLocation="CenterOwner"
        CanResize="False">
  <StackPanel Margin="24" Spacing="16" MinWidth="300">
    <TextBlock TextWrapping="Wrap">
      <Run Text="Save changes to " />
      <Run Text="{Binding FileName}" FontWeight="Bold" />
      <Run Text="?" />
    </TextBlock>
    <TextBlock Text="If you don't save, your changes will be lost."
               FontSize="11"
               Foreground="Gray" />

    <StackPanel Orientation="Horizontal" Gap="8" HorizontalAlignment="Right">
      <Button Content="Save" Click="OnSave" />
      <Button Content="Discard" Click="OnDiscard" />
      <Button Content="Cancel" Click="OnCancel" />
    </StackPanel>
  </StackPanel>
</Window>
```

```csharp
// Views/ConfirmSaveDialog.axaml.cs
using MyApp.ViewModels;

namespace MyApp.Views;

public partial class ConfirmSaveDialog : Window
{
    public ConfirmSaveDialog()
    {
        InitializeComponent();
    }

    private void OnSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ConfirmSaveViewModel vm)
            vm.Result = ConfirmSaveAction.Save;
        Close();
    }

    private void OnDiscard(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ConfirmSaveViewModel vm)
            vm.Result = ConfirmSaveAction.Discard;
        Close();
    }

    private void OnCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ConfirmSaveViewModel vm)
            vm.Result = ConfirmSaveAction.Cancel;
        Close();
    }
}
```

### Document editor window

```xml
<!-- Views/DocumentEditorWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:MyApp.ViewModels"
        x:Class="MyApp.Views.DocumentEditorWindow"
        x:DataType="vm:DocumentEditorViewModel"
        Title="{Binding FileName, StringFormat='{0} — Editor'}"
        Width="800" Height="600">
  <DockPanel Margin="8">
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Gap="4" Margin="0,0,0,4">
      <Button Content="Save" Command="{Binding SaveCommand}" />
      <TextBlock Text="{Binding FileName}"
                 VerticalAlignment="Center"
                 Margin="8,0,0,0" />
      <TextBlock Text=" (modified)"
                 Foreground="#f59e0b"
                 VerticalAlignment="Center"
                 IsVisible="{Binding HasUnsavedChanges}" />
    </StackPanel>

    <TextBox Text="{Binding DocumentText, Mode=TwoWay}"
             AcceptsReturn="True"
             TextWrapping="Wrap"
             FontFamily="Consolas"
             FontSize="13" />
  </DockPanel>
</Window>
```

```csharp
// Views/DocumentEditorWindow.axaml.cs
using MyApp.ViewModels;

namespace MyApp.Views;

public partial class DocumentEditorWindow : Window
{
    public DocumentEditorWindow()
    {
        InitializeComponent();
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (DataContext is DocumentEditorViewModel vm)
        {
            // Prevent the window from closing — the dialog will decide
            e.Cancel = true;

            var shouldClose = await vm.TryCloseAsync(this);
            if (shouldClose)
            {
                // Remove the cancel to allow the close to proceed
                e.Cancel = false;
                Close();
            }
        }
    }
}
```

### How it works

1. When the user closes the window (click X or Alt+F4), `OnClosing` fires. The event is initially set to `e.Cancel = true` to prevent the window from closing immediately.
2. `TryCloseAsync` checks `HasUnsavedChanges`. If false, it returns `true` immediately and the window closes.
3. If there are unsaved changes, `TryCloseAsync` creates a `ConfirmSaveDialog`, sets its `DataContext`, and calls `ShowDialog`. The dialog is modal — the parent window cannot be interacted with until the dialog closes.
4. The user clicks Save, Discard, or Cancel in the dialog. Each button sets `vm.Result` and calls `Close()` on the dialog. `ShowDialog` returns the dialog instance.
5. Back in `TryCloseAsync`, the `result.Result` enum determines the action:
   - **Save:** The dialog was accepted — `TryCloseAsync` returns `true`, the window closes.
   - **Discard:** The user chose to discard — return `true`, window closes without saving.
   - **Cancel:** Return `false` — the `OnClosing` handler does not call `Close()` again, and the window stays open.
6. If the user chose Save, `TryCloseAsync` should call `Save()` before returning — the example omits this for brevity. In production, call `SaveCommand.Execute(null)` inside the `Save` case.

### Design decisions and edge cases

- **`e.Cancel = true` + manual re-close:** The `OnClosing` event sets `e.Cancel = true` immediately, *then* awaits the dialog. If `e.Cancel` were not set, the window would close while the dialog is open, orphaning the dialog. After the user chooses, the code sets `e.Cancel = false` and calls `Close()` again.
- **Dialog result via property, not `Close(bool?)`:** The dialog's result is stored in `ConfirmSaveViewModel.Result` before calling `Close()`. The `ShowDialog<ConfirmSaveDialog?>` call returns the window instance, and the caller reads the property. Alternatively, `Close(true)`/`Close(false)` gives a `bool?` — but the tri-state needs an enum.
- **`WindowStartupLocation="CenterOwner"`:** The dialog centers on the parent window. This requires that the parent window reference is correctly passed to `ShowDialog`. If the parent is null or not visible, the dialog appears at the screen center.
- **What if the user opens a new file while editing?** The `DocumentEditorViewModel` does not handle the "New" command in this example. In a full implementation, opening a new file would also call `TryCloseAsync` to check for unsaved changes before discarding the current document.

---

## Example 2: File Import Wizard with StorageProvider

**Goal:** Build a window that lets the user pick a file via `StorageProvider`, preview selected files, and confirm the import. The wizard uses `TopLevel.GetTopLevel` to access the storage API from a ViewModel command.

This scenario demonstrates: `StorageProvider.OpenFilePickerAsync` for platform-native file dialogs, previewing file metadata before import, and passing a visual reference from view to ViewModel.

### ViewModel

```csharp
// ViewModels/FileImportViewModel.cs
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class FileImportViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Select files to import";

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private double _importProgress;

    public ObservableCollection<ImportFileInfo> SelectedFiles { get; } = new();

    // Called from code-behind with a visual reference for TopLevel access
    [RelayCommand]
    private async Task PickFilesAsync(Visual? sourceVisual)
    {
        if (sourceVisual is null) return;

        var topLevel = TopLevel.GetTopLevel(sourceVisual);
        if (topLevel?.StorageProvider is not { } storage)
        {
            StatusMessage = "Storage provider not available";
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select files to import",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Data files")
                {
                    Patterns = new[] { "*.csv", "*.json", "*.xml" },
                },
                FilePickerFileTypes.All,
            },
        });

        if (files.Count == 0)
        {
            StatusMessage = "No files selected";
            return;
        }

        SelectedFiles.Clear();
        foreach (var file in files)
        {
            SelectedFiles.Add(new ImportFileInfo
            {
                Name = file.Name,
                Path = file.TryGetLocalPath(),
                SizeBytes = await GetFileSizeAsync(file),
            });
        }

        StatusMessage = $"{SelectedFiles.Count} file(s) selected";
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (SelectedFiles.Count == 0) return;

        IsImporting = true;
        ImportProgress = 0;

        try
        {
            var total = SelectedFiles.Count;
            for (var i = 0; i < total; i++)
            {
                // Simulate import for each file
                await Task.Delay(500);
                SelectedFiles[i].IsImported = true;
                ImportProgress = (i + 1.0) / total;
                StatusMessage = $"Importing {i + 1} of {total}...";
            }

            StatusMessage = $"Imported {total} file(s) successfully";
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedFiles.Clear();
        StatusMessage = "Selection cleared";
    }

    private static async Task<long> GetFileSizeAsync(IStorageFile file)
    {
        try
        {
            var properties = await file.GetBasicPropertiesAsync();
            return properties.Size;
        }
        catch
        {
            return 0;
        }
    }
}

public partial class ImportFileInfo : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _path;

    [ObservableProperty]
    private long _sizeBytes;

    [ObservableProperty]
    private bool _isImported;

    public string SizeDisplay => SizeBytes switch
    {
        0 => "Unknown",
        < 1024 => $"{SizeBytes} B",
        < 1_048_576 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / 1_048_576.0:F1} MB",
    };
}
```

### View

```xml
<!-- Views/FileImportView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.FileImportView"
             x:DataType="vm:FileImportViewModel"
             Name="ImportRootControl">

  <DockPanel Margin="24">
    <!-- Status bar -->
    <TextBlock DockPanel.Dock="Bottom"
               Text="{Binding StatusMessage}"
               FontSize="11"
               Foreground="Gray"
               Margin="0,8,0,0" />

    <!-- Toolbar -->
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Gap="8" Margin="0,0,0,12">
      <Button Content="Pick Files"
              Command="{Binding PickFilesCommand}"
              CommandParameter="{Binding #ImportRootControl}" />
      <Button Content="Import Selected"
              Command="{Binding ImportCommand}" />
      <Button Content="Clear"
              Command="{Binding ClearSelectionCommand}" />
    </StackPanel>

    <!-- Progress bar -->
    <ProgressBar Value="{Binding ImportProgress}"
                 IsVisible="{Binding IsImporting}"
                 Margin="0,0,0,8" />

    <!-- File list -->
    <ListBox ItemsSource="{Binding SelectedFiles}">
      <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:ImportFileInfo">
          <Border Margin="4,2" Padding="8,4"
                  CornerRadius="4"
                  BorderBrush="#e5e7eb"
                  BorderThickness="1">
            <Grid ColumnDefinitions="Auto,*,Auto" Gap="8">
              <TextBlock Text="{Binding Name}" FontWeight="SemiBold" />
              <TextBlock Grid.Column="1"
                         Text="{Binding SizeDisplay}"
                         FontSize="11"
                         Foreground="Gray"
                         VerticalAlignment="Center" />
              <TextBlock Grid.Column="2"
                         Text="Imported"
                         FontSize="11"
                         Foreground="Green"
                         IsVisible="{Binding IsImported}" />
            </Grid>
          </Border>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </DockPanel>
</UserControl>
```

### How it works

1. **`PickFilesCommand` receives a `Visual` parameter:** The XAML passes the `UserControl` itself as the `CommandParameter` via `{Binding #ImportRootControl}` (using `x:Name="ImportRootControl"`). This gives the ViewModel a visual reference to resolve the `TopLevel`.
2. **`TopLevel.GetTopLevel(sourceVisual)`** walks up the visual tree from the `UserControl` to find the containing `Window`. This is the recommended Avalonia 12 pattern — it works from any control in the tree and does not require a reference to the `Window` type.
3. **`StorageProvider.OpenFilePickerAsync`** opens the platform-native file picker. On Windows, this is the Win32 file dialog. On macOS, it is `NSOpenPanel`. On Linux, it uses the XDG Desktop Portal. The `FilePickerOpenOptions` configures title, multi-select, and file type filters.
4. **`IStorageFile.TryGetLocalPath()`** returns the local filesystem path as a `string?`. On some platforms (sandboxed apps, browser), this may return `null`. The `ImportFileInfo.Path` is nullable to reflect this.
5. **`GetBasicPropertiesAsync`** on `IStorageFile` returns file metadata. The `Size` property is a `long` byte count. The `ImportFileInfo.SizeDisplay` computed property formats it for display.
6. **Import progress** is tracked per file and via `ImportProgress` (0.0–1.0). The `ProgressBar.Value` binding reflects the overall progress across all selected files.

### Design decisions and edge cases

- **Visual reference via CommandParameter:** The ViewModel receives a `Visual` from the view. This is the simplest pattern for accessing `TopLevel` from a ViewModel command. Alternatives include injecting an `ITopLevelService` or using `ApplicationLifetime` for desktop-only apps.
- **`AllowMultiple = true`:** The user can select multiple files. The `SelectedFiles` collection clears before re-populating. If you want to add to the existing selection (not replace), use `AddRange` instead of `Clear` + loop.
- **Handling cancellation:** If the user cancels the file picker, `OpenFilePickerAsync` returns an empty list. The ViewModel checks `files.Count == 0` and sets a status message without clearing the existing selection.
- **`IsImporting` disables the import button:** The `ImportCommand` has no `CanExecute` guard — the button is always enabled while no import is running. In production, add `CanExecute = nameof(CanImport)` that checks `SelectedFiles.Count > 0 && !IsImporting`.
- **Error handling in `GetFileSizeAsync`:** The `try/catch` returns 0 if properties cannot be read. The `SizeDisplay` converter handles 0 as "Unknown". This prevents a single unreadable file from crashing the file selection flow.

---

## What These Examples Demonstrate

| Scenario | Dialog technique | What to learn |
|---|---|---|
| Save changes confirmation | `OnClosing` interception + modal dialog with tri-state result | `Window.Closing` event, conditional close cancellation, enum-based result passing |
| File import wizard | `StorageProvider.OpenFilePickerAsync` + `TopLevel.GetTopLevel` | Platform-native file picker, visual tree navigation from ViewModel, file metadata |

The document editor demonstrates intercepting the window lifecycle to show a custom dialog before closing. The file import wizard demonstrates the modern Avalonia 12 API for file picking — replacing the deprecated `OpenFileDialog` class with the async `IStorageProvider` interface.

## See Also

- [010 — Window Basics and Simple Dialogs](010-window-dialog-basics.md)
- [010V — Verbose Companion](010-window-dialog-basics-verbose.md)
- [002 — Command Binding](002-command-binding.md)
- [002V — Command Binding (verbose companion)](002-command-binding-verbose.md)
- [016 — Window & Dialog Management](../intermediate/016-window-dialog-management.md)
- [Avalonia API: TopLevel](https://reference.avaloniaui.net/api/Avalonia.Controls/TopLevel/)
- [Avalonia Docs: Storage Provider](https://docs.avaloniaui.net/docs/data-access/storage)
