---
tier: advanced
topic: platform
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 009-storage-file-io-pipeline.md
---

# 009X — Storage & File I/O Pipeline: Real-World Examples

## Example 1: Text Editor with Recent Files and Auto-Save

A minimal text editor that opens, saves, and tracks recently opened files with bookmarks.

### Service Registration

```csharp
// Program.cs
builder.Services.AddSingleton<IFileOperationService>(sp =>
{
    var mainWindow = Application.Current?.ApplicationLifetime switch
    {
        IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
        _ => throw new InvalidOperationException("Unsupported lifetime")
    };
    return new FileOperationService(mainWindow);
});

builder.Services.AddSingleton<RecentFilesManager>();
```

### ViewModel

```csharp
public sealed partial class TextEditorViewModel : ObservableObject
{
    private readonly IFileOperationService _fileOps;
    private readonly RecentFilesManager _recentFiles;
    private readonly DispatcherTimer? _autoSaveTimer;

    [ObservableProperty]
    private string _content = "";

    [ObservableProperty]
    private string _fileName = "Untitled";

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<string> RecentFiles => _recentFiles.Files;

    public TextEditorViewModel(IFileOperationService fileOps, RecentFilesManager recentFiles)
    {
        _fileOps = fileOps;
        _recentFiles = recentFiles;

        // Auto-save every 30 seconds if dirty
        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _autoSaveTimer.Tick += async (_, _) =>
        {
            if (IsDirty && CurrentFilePath is not null)
                await SaveAsync();
        };
        _autoSaveTimer.Start();
    }

    [RelayCommand]
    private async Task NewAsync()
    {
        if (IsDirty && !await ConfirmSaveAsync()) return;
        Content = "";
        FileName = "Untitled";
        CurrentFilePath = null;
        IsDirty = false;
        StatusText = "New document";
    }

    [RelayCommand]
    private async Task OpenAsync(CancellationToken ct)
    {
        if (IsDirty && !await ConfirmSaveAsync()) return;

        var text = await _fileOps.OpenTextFileAsync("Open Text File", "*.txt", ct);
        if (text is null) return;

        Content = text;
        IsDirty = false;
        StatusText = $"Opened: {CurrentFilePath ?? "unknown"}";
        if (CurrentFilePath is not null)
            _recentFiles.Add(CurrentFilePath);
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        if (CurrentFilePath is null)
        {
            await SaveAsAsync(ct);
            return;
        }

        IsSaving = true;
        try
        {
            var success = await _fileOps.SaveTextFileAsync(Content,
                "Save", FileName, "*.txt", ct);
            if (success)
            {
                IsDirty = false;
                StatusText = $"Saved: {FileName}";
            }
        }
        finally { IsSaving = false; }
    }

    [RelayCommand]
    private async Task SaveAsAsync(CancellationToken ct)
    {
        IsSaving = true;
        try
        {
            var success = await _fileOps.SaveTextFileAsync(Content,
                "Save As", "document.txt", "*.txt", ct);
            if (success)
            {
                IsDirty = false;
                FileName = Path.GetFileName(CurrentFilePath ?? "Untitled");
                StatusText = $"Saved: {FileName}";
                if (CurrentFilePath is not null)
                    _recentFiles.Add(CurrentFilePath);
            }
        }
        finally { IsSaving = false; }
    }

    [RelayCommand]
    private async Task OpenRecentAsync(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
        {
            _recentFiles.Remove(filePath);
            StatusText = $"File not found: {filePath}";
            return;
        }

        CurrentFilePath = filePath;
        Content = await File.ReadAllTextAsync(filePath, ct);
        FileName = Path.GetFileName(filePath);
        IsDirty = false;
        StatusText = $"Opened: {FileName}";
    }

    partial void OnContentChanged(string value) => IsDirty = true;

    private async Task<bool> ConfirmSaveAsync()
    {
        // In a real app, show a dialog
        return true;
    }
}

public sealed class RecentFilesManager
{
    private const int MaxRecent = 10;
    private readonly string _settingsPath;

    public ObservableCollection<string> Files { get; } = new();

    public RecentFilesManager()
    {
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MyTextEditor", "recent.json");
        Load();
    }

    public void Add(string path)
    {
        Files.Remove(path);
        Files.Insert(0, path);
        if (Files.Count > MaxRecent) Files.RemoveAt(Files.Count - 1);
        Save();
    }

    public void Remove(string path) { Files.Remove(path); Save(); }

    private void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var list = JsonSerializer.Deserialize<List<string>>(json);
                if (list is not null)
                    foreach (var item in list) Files.Add(item);
            }
        }
        catch { /* ignore corrupt file */ }
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(Files.ToList());
        File.WriteAllText(_settingsPath, json);
    }
}
```

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        x:Class="MyApp.Views.TextEditorWindow">
  <DockPanel>
    <Menu DockPanel.Dock="Top">
      <MenuItem Header="_File">
        <MenuItem Header="_New" Command="{Binding NewCommand}" InputGesture="Ctrl+N" />
        <MenuItem Header="_Open..." Command="{Binding OpenCommand}" InputGesture="Ctrl+O" />
        <Separator />
        <MenuItem Header="_Save" Command="{Binding SaveCommand}" InputGesture="Ctrl+S" />
        <MenuItem Header="Save _As..." Command="{Binding SaveAsCommand}" InputGesture="Ctrl+Shift+S" />
        <Separator />
        <MenuItem Header="Recent Files" Items="{Binding RecentFiles}"
                  Command="{Binding OpenRecentCommand}" CommandParameter="{Binding}" />
        <Separator />
        <MenuItem Header="E_xit" />
      </MenuItem>
    </Menu>

    <TextBox Text="{Binding Content}"
             AcceptsReturn="True"
             AcceptsTab="True"
             FontFamily="Consolas"
             FontSize="14"
             Padding="8"
             IsEnabled="{Binding IsSaving, Converter={StaticResource InvertBool}}" />

    <Border DockPanel.Dock="Bottom" Padding="4" Background="{StaticResource SystemChromeLowColor}">
      <TextBlock Text="{Binding StatusText}" FontSize="11" />
    </Border>
  </DockPanel>
</Window>
```

---

## Example 2: Image Gallery with Bulk Import and Progress

An image gallery that lets the user select multiple files, copy them to a local cache directory, and display thumbnails with a progress bar.

### Importer Service

```csharp
public sealed class ImageImportService
{
    private readonly IFileOperationService _fileOps;
    private readonly string _cacheDir;

    public ImageImportService(IFileOperationService fileOps)
    {
        _fileOps = fileOps;
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageGallery", "cache");
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task ImportFilesAsync(IEnumerable<IStorageFile> files,
        IProgress<ImportProgress> progress, CancellationToken ct)
    {
        var fileList = files.ToList();
        int total = fileList.Count;
        int completed = 0;

        foreach (var file in fileList)
        {
            ct.ThrowIfCancellationRequested();

            var destPath = Path.Combine(_cacheDir, file.Name);
            var destFile = await _fileOps.SaveFileAsync(file.Name, ct);
            if (destFile is null) continue;

            // Read all bytes from source
            await using var srcStream = await file.OpenReadAsync();
            await using var destStream = await destFile.OpenWriteAsync();
            await srcStream.CopyToAsync(destStream, ct);

            completed++;
            progress.Report(new ImportProgress(completed, total, file.Name));
        }
    }
}

public sealed record ImportProgress(int Completed, int Total, string CurrentFile)
{
    public double Percentage => (double)Completed / Total * 100;
}
```

### ViewModel

```csharp
public sealed partial class ImageGalleryViewModel : ObservableObject
{
    private readonly IFileOperationService _fileOps;
    private readonly ImageImportService _importer;

    [ObservableProperty]
    private ObservableCollection<ImageItem> _images = new();

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private double _importProgress;

    [ObservableProperty]
    private string _importStatus = "";

    public ImageGalleryViewModel(IFileOperationService fileOps, ImageImportService importer)
    {
        _fileOps = fileOps;
        _importer = importer;
    }

    [RelayCommand]
    private async Task ImportImagesAsync(CancellationToken ct)
    {
        var files = await _fileOps.OpenMultipleFilesAsync(
            new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp", "*.bmp" }
                }
            }, ct);

        if (files.Count == 0) return;

        IsImporting = true;
        var progress = new Progress<ImportProgress>(p =>
        {
            ImportProgress = p.Percentage;
            ImportStatus = $"Importing {p.Completed} of {p.Total}: {p.CurrentFile}";
        });

        try
        {
            await _importer.ImportFilesAsync(files, progress, ct);
            ImportStatus = $"Imported {files.Count} images";
            LoadImages();
        }
        catch (OperationCanceledException)
        {
            ImportStatus = "Import cancelled";
        }
        catch (Exception ex)
        {
            ImportStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
        }
    }

    private void LoadImages()
    {
        Images.Clear();
        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageGallery", "cache");
        if (Directory.Exists(cacheDir))
        {
            foreach (var file in Directory.GetFiles(cacheDir, "*.*"))
            {
                Images.Add(new ImageItem { FilePath = file, Name = Path.GetFileName(file) });
            }
        }
    }
}

public sealed class ImageItem
{
    public string FilePath { get; init; } = "";
    public string Name { get; init; } = "";
}
```

---

## Example 3: Project Manager with Folder Bookmarks

A project management tool that lets the user bookmark project folders and restore them across sessions.

### BookmarkService

```csharp
public sealed class ProjectBookmarkService
{
    private readonly IFileOperationService _fileOps;
    private readonly string _bookmarkFile;

    private Dictionary<string, string> _bookmarks = new();

    public IReadOnlyDictionary<string, string> Bookmarks => _bookmarks;

    public ProjectBookmarkService(IFileOperationService fileOps)
    {
        _fileOps = fileOps;
        _bookmarkFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MyProjectManager", "bookmarks.json");
        Load();
    }

    public async Task<string?> AddBookmarkAsync(string name, CancellationToken ct)
    {
        var folder = await _fileOps.OpenFolderAsync(ct);
        if (folder is null) return null;

        var bookmarkId = await _fileOps.SaveBookmarkAsync(folder, ct);
        if (bookmarkId is not null)
        {
            _bookmarks[name] = bookmarkId;
            Save();
        }
        return bookmarkId;
    }

    public async Task<IStorageFolder?> OpenBookmarkAsync(string name, CancellationToken ct)
    {
        if (_bookmarks.TryGetValue(name, out var bookmarkId))
            return await _fileOps.RestoreBookmarkAsync(bookmarkId, ct);
        return null;
    }

    public void RemoveBookmark(string name)
    {
        _bookmarks.Remove(name);
        Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_bookmarkFile))
            {
                var json = File.ReadAllText(_bookmarkFile);
                _bookmarks = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                    ?? new Dictionary<string, string>();
            }
        }
        catch { /* start fresh */ }
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(_bookmarkFile);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(_bookmarkFile, JsonSerializer.Serialize(_bookmarks));
    }
}
```

### ViewModel

```csharp
public sealed partial class ProjectManagerViewModel : ObservableObject
{
    private readonly ProjectBookmarkService _bookmarks;

    [ObservableProperty]
    private ObservableCollection<ProjectBookmark> _projects = new();

    [ObservableProperty]
    private string _statusText = "Ready";

    public ProjectManagerViewModel(ProjectBookmarkService bookmarks)
    {
        _bookmarks = bookmarks;
        RefreshProjects();
    }

    private void RefreshProjects()
    {
        Projects.Clear();
        foreach (var (name, _) in _bookmarks.Bookmarks)
            Projects.Add(new ProjectBookmark { Name = name });
    }

    [RelayCommand]
    private async Task AddProjectAsync(CancellationToken ct)
    {
        var name = await ShowInputDialogAsync("Project name:");
        if (string.IsNullOrEmpty(name)) return;

        var bookmarkId = await _bookmarks.AddBookmarkAsync(name, ct);
        if (bookmarkId is not null)
        {
            RefreshProjects();
            StatusText = $"Added project: {name}";
        }
    }

    [RelayCommand]
    private async Task OpenProjectAsync(ProjectBookmark project, CancellationToken ct)
    {
        var folder = await _bookmarks.OpenBookmarkAsync(project.Name, ct);
        if (folder is not null)
        {
            StatusText = $"Opened project: {project.Name} ({folder.Path})";
            // Navigate to project, load project file, etc.
        }
        else
        {
            _bookmarks.RemoveBookmark(project.Name);
            RefreshProjects();
            StatusText = $"Bookmark expired: {project.Name}";
        }
    }

    [RelayCommand]
    private void RemoveProject(ProjectBookmark project)
    {
        _bookmarks.RemoveBookmark(project.Name);
        RefreshProjects();
        StatusText = $"Removed project: {project.Name}";
    }
}

public sealed class ProjectBookmark
{
    public string Name { get; init; } = "";
}
```

### View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             x:Class="MyApp.Views.ProjectManagerView">
  <DockPanel>
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8" Margin="8">
      <Button Command="{Binding AddProjectCommand}" Content="+ Add Project" />
      <TextBlock Text="{Binding StatusText}" VerticalAlignment="Center" />
    </StackPanel>

    <ListBox Items="{Binding Projects}" SelectedItem="{Binding SelectedProject}"
             VirtualizationMode="Simple">
      <ListBox.ItemTemplate>
        <DataTemplate DataType="vm:ProjectBookmark">
          <Border Padding="12,8" Margin="4,2">
            <Grid ColumnDefinitions="*,Auto">
              <TextBlock Text="{Binding Name}" FontWeight="SemiBold" />
              <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="4">
                <Button Content="Open" Command="{Binding $parent[ListBox].DataContext.OpenProjectCommand}"
                        CommandParameter="{Binding}" />
                <Button Content="×" Command="{Binding $parent[ListBox].DataContext.RemoveProjectCommand}"
                        CommandParameter="{Binding}" />
              </StackPanel>
            </Grid>
          </Border>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </DockPanel>
</UserControl>
```
