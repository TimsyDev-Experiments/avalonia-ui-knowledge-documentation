---
tier: intermediate
topic: services
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 062E — Clipboard & Launcher (examples)

## Example 1: Copy/paste text

```xml
<StackPanel Spacing="8" Margin="20">
  <TextBox x:Name="InputBox" PlaceholderText="Type something..." />
  <StackPanel Orientation="Horizontal" Spacing="8">
    <Button Content="Copy" Command="{Binding CopyCommand}"
            CommandParameter="{Binding #InputBox.Text}" />
    <Button Content="Paste" Command="{Binding PasteCommand}" />
  </StackPanel>
  <TextBlock Text="{Binding Status}" />
</StackPanel>
```

```csharp
public partial class ClipboardViewModel : ObservableObject
{
    [ObservableProperty] private string _status = "";

    [RelayCommand]
    private async Task Copy(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var top = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow!
            : null!);
        if (top?.Clipboard is { } cb)
        {
            await cb.SetTextAsync(text);
            Status = "Copied!";
        }
    }

    [RelayCommand]
    private async Task Paste()
    {
        var top = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow!
            : null!);
        if (top?.Clipboard is { } cb)
        {
            var text = await cb.TryGetTextAsync();
            Status = text is not null ? $"Pasted: {text}" : "Clipboard empty";
        }
    }
}
```

---

## Example 2: Copy image to clipboard

```csharp
[RelayCommand]
private async Task CopyImage()
{
    var bitmap = new Bitmap(@"C:\images\screenshot.png");
    var top = TopLevel.GetTopLevel(/* ... */);
    if (top?.Clipboard is { } cb)
        await cb.SetBitmapAsync(bitmap);
}

[RelayCommand]
private async Task PasteImage()
{
    var top = TopLevel.GetTopLevel(/* ... */);
    if (top?.Clipboard is { } cb)
    {
        var bitmap = await cb.TryGetBitmapAsync();
        if (bitmap is not null)
            PreviewImage = bitmap;
    }
}
```

---

## Example 3: Multi-format clipboard (text + HTML)

```csharp
[RelayCommand]
private async Task CopyRichText()
{
    var item = new DataTransferItem();
    item.Set(DataFormat.Text, "Avalonia is great");
    item.Set(DataFormat.CreateStringPlatformFormat("text/html"),
             "<b>Avalonia</b> is <i>great</i>");

    var data = new DataTransfer();
    data.Add(item);

    var top = TopLevel.GetTopLevel(/* ... */);
    if (top?.Clipboard is { } cb)
        await cb.SetDataAsync(data);
}
```

---

## Example 4: Open URL in browser

```csharp
[RelayCommand]
private async Task OpenWebsite(string? url)
{
    var top = TopLevel.GetTopLevel(/* ... */);
    if (top?.Launcher is { } launcher)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            bool success = await launcher.LaunchUriAsync(uri);
            Status = success ? "Opening..." : "Failed to open";
        }
    }
}
```

---

## Example 5: Open file with default app

```csharp
[RelayCommand]
private async Task OpenDocument()
{
    var top = TopLevel.GetTopLevel(/* ... */);
    if (top?.Launcher is { } launcher)
    {
        // From storage provider
        var files = await top.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.Pdf }
            });
        if (files.Count > 0)
            await launcher.LaunchFileAsync(files[0]);

        // Or from file system path (desktop only)
        var doc = new FileInfo(@"C:\readme.txt");
        await launcher.LaunchFileInfoAsync(doc);
    }
}
```

---

## Example 6: Clipboard flush for persistence

```csharp
[RelayCommand]
private async Task CopyAndFlush(string text)
{
    var top = TopLevel.GetTopLevel(/* ... */);
    if (top?.Clipboard is { } cb)
    {
        await cb.SetTextAsync(text);
        await cb.FlushAsync(); // Survive app shutdown
    }
}
```

---

## Example 7: Read all clipboard formats

```csharp
[RelayCommand]
private async Task InspectClipboard()
{
    var top = TopLevel.GetTopLevel(/* ... */);
    if (top?.Clipboard is not { } cb) return;

    using var data = await cb.TryGetDataAsync();
    if (data is null) { Status = "Clipboard empty"; return; }

    var sb = new StringBuilder();
    sb.AppendLine($"Formats ({data.Formats.Count}):");
    foreach (var f in data.Formats)
        sb.AppendLine($"  - {f.Name} ({f.Kind})");

    var text = await data.TryGetTextAsync();
    if (text is not null)
        sb.AppendLine($"Text: {text[..Math.Min(text.Length, 50)]}");

    var files = await data.TryGetFilesAsync();
    if (files.Length > 0)
    {
        sb.AppendLine($"Files ({files.Length}):");
        foreach (var f in files)
            sb.AppendLine($"  - {f.Name}");
    }

    Status = sb.ToString();
}
```

---

## See Also

- [062 — Clipboard & Launcher (core)](062-clipboard-launcher.md)
- [062V — Clipboard & Launcher (verbose)](062-clipboard-launcher-verbose.md)
- [063 — Storage Service](063-storage-service.md)
