---
tier: basics
topic: commands
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 008-relay-command.md
---

# 008X — RelayCommand in Depth: Real-World Examples

**What you'll build:** A file-upload manager with cancellation and progress, and a retry-enabled data-loading command — two scenarios that demonstrate async command patterns, `CancellationToken` injection, `IProgress<T>` binding, and fallback execution.

**Prerequisites:** [008 — RelayCommand in Depth](008-relay-command.md). The [verbose companion](008-relay-command-verbose.md) covers the `AsyncRelayCommand` internal implementation, `CancellationToken` injection mechanics, and manual `ICommand` alternatives.

---

## Example 1: File Upload Manager with Cancellation and Progress

**Goal:** Create an upload panel where each file upload is an async command with a cancellable progress bar and status text. Multiple uploads can run concurrently, each with its own progress tracking.

This scenario demonstrates `CancellationToken` injection for cancellation, `IProgress<T>` for per-item progress, and binding to `IsRunning` and `Progress` on individual command instances.

### Upload job model

```csharp
// Models/UploadJob.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.Models;

public partial class UploadJob : ObservableObject
{
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private long _fileSize;

    [ObservableProperty]
    private string _status = "Pending";

    [ObservableProperty]
    private double _progress; // 0.0 to 1.0

    [ObservableProperty]
    private bool _isUploading;
}
```

### ViewModel

```csharp
// ViewModels/UploadViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyApp.Models;

namespace MyApp.ViewModels;

public partial class UploadViewModel : ObservableObject
{
    public ObservableCollection<UploadJob> Jobs { get; } = new();

    [ObservableProperty]
    private UploadJob? _selectedJob;

    [RelayCommand]
    private void AddFile()
    {
        Jobs.Add(new UploadJob
        {
            FileName = $"file_{Jobs.Count + 1}.bin",
            FileSize = new Random().Next(1024, 10_485_760), // 1 KB - 10 MB
            Status = "Pending",
        });
    }

    [RelayCommand]
    private async Task UploadAsync(UploadJob? job, CancellationToken token)
    {
        if (job is null) return;

        job.Status = "Uploading...";
        job.IsUploading = true;
        job.Progress = 0;

        var progress = new Progress<double>(p =>
        {
            job.Progress = p;
        });

        try
        {
            // Simulate chunked upload with progress
            var totalChunks = 20;
            for (var i = 0; i < totalChunks; i++)
            {
                token.ThrowIfCancellationRequested();

                // Simulate network send
                await Task.Delay(200, token);

                var pct = (i + 1.0) / totalChunks;
                (progress as IProgress<double>).Report(pct);
            }

            job.Status = "Completed";
            job.Progress = 1.0;
        }
        catch (OperationCanceledException)
        {
            job.Status = "Cancelled";
            job.Progress = 0;
        }
        finally
        {
            job.IsUploading = false;
        }
    }

    [RelayCommand]
    private void CancelSelected()
    {
        if (SelectedJob is not null)
        {
            SelectedJob.Status = "Cancelled";
            SelectedJob.IsUploading = false;
            // Real cancellation requires the CancellationTokenSource —
            // see the "How It Works" section below
        }
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        var completed = Jobs.Where(j => j.Status is "Completed" or "Cancelled").ToList();
        foreach (var job in completed)
            Jobs.Remove(job);
    }
}
```

### Upload command with external cancellation

The `UploadAsync` method above uses `CancellationToken` from the command's internal source. To cancel from outside, the ViewModel must hold the token source:

```csharp
// Alternative: ViewModel with external cancellation support
private CancellationTokenSource? _uploadCts;

[RelayCommand]
private async Task UploadWithCancelAsync(UploadJob? job)
{
    if (job is null) return;

    _uploadCts?.Cancel();
    _uploadCts = new CancellationTokenSource();
    var token = _uploadCts.Token;

    job.Status = "Uploading...";
    job.IsUploading = true;

    try
    {
        for (var i = 0; i < 100; i += 5)
        {
            token.ThrowIfCancellationRequested();
            await Task.Delay(100, token);
            job.Progress = i / 100.0;
        }
        job.Status = "Completed";
    }
    catch (OperationCanceledException)
    {
        job.Status = "Cancelled";
    }
    finally
    {
        job.IsUploading = false;
    }
}

[RelayCommand]
private void CancelUpload()
{
    _uploadCts?.Cancel();
}
```

### View

```xml
<!-- Views/UploadView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:models="using:MyApp.Models"
             x:Class="MyApp.Views.UploadView"
             x:DataType="vm:UploadViewModel">

  <DockPanel Margin="16">

    <!-- Toolbar -->
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Gap="8" Margin="0,0,0,8">
      <Button Content="Add File" Command="{Binding AddFileCommand}" />
      <Button Content="Cancel Selected" Command="{Binding CancelSelectedCommand}" />
      <Button Content="Clear Completed" Command="{Binding ClearCompletedCommand}" />
    </StackPanel>

    <!-- Job list -->
    <ListBox ItemsSource="{Binding Jobs}"
             SelectedItem="{Binding SelectedJob, Mode=TwoWay}">
      <ListBox.ItemTemplate>
        <DataTemplate x:DataType="models:UploadJob">
          <Border Margin="4,2" Padding="8" CornerRadius="6"
                  BorderBrush="#ddd" BorderThickness="1">
            <Grid ColumnDefinitions="*,Auto,120" Gap="8">
              <StackPanel>
                <TextBlock Text="{Binding FileName}" FontWeight="SemiBold" />
                <TextBlock Text="{Binding Status}" FontSize="11" Foreground="Gray" />
              </StackPanel>

              <Button Grid.Column="1"
                      Content="Upload"
                      Command="{Binding $parent[Window].DataContext.UploadCommand}"
                      CommandParameter="{Binding .}"
                      IsVisible="{Binding IsUploading, Converter={StaticResource InverseBool}}" />

              <ProgressBar Grid.Column="2"
                           Value="{Binding Progress}"
                           IsVisible="{Binding IsUploading}"
                           VerticalAlignment="Center" />
            </Grid>
          </Border>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </DockPanel>
</UserControl>
```

### How it works

1. **`UploadAsync(UploadJob? job, CancellationToken token)`** is an async command with a typed parameter and injected `CancellationToken`. The source generator detects `CancellationToken` and removes it from the external parameter list — the command exposes `IAsyncRelayCommand<UploadJob?>`.
2. **`token.ThrowIfCancellationRequested()`** is called in each chunk loop. When the token is cancelled, `OperationCanceledException` is thrown. The catch block sets the job status to "Cancelled".
3. **Progress reporting through `IProgress<double>`:** The `progress` local is created in the command body. Progress is reported after each chunk. In this example, progress is written to `job.Progress` directly — the `IProgress<double>` pattern lets the progress callback marshal to the UI thread automatically.
4. **External cancellation via `CancellationTokenSource`:** The alternative `UploadWithCancelAsync` pattern stores the token source in a field. The `CancelUpload` command cancels it, which cancels the running upload. The `_uploadCts?.Cancel()` call is safe — if no upload is running, `_uploadCts` is null.
5. **Command binding from DataTemplate:** The Upload button in the `DataTemplate` uses `$parent[Window].DataContext.UploadCommand` to reach the parent ViewModel's command (since the `DataTemplate`'s `x:DataType` is `UploadJob`, not `UploadViewModel`). The `CommandParameter` passes the current job.

### Design decisions and edge cases

- **Token injection vs manual token source:** The injected `CancellationToken` is managed by `AsyncRelayCommand` internally — it creates and disposes the token source per execution. This works for self-contained cancellation (cancelling only that command). For cross-command cancellation (Cancel button), store the token source in a field and create it in the command body.
- **Multiple concurrent uploads:** Each `UploadAsync` call gets its own `CancellationToken`. Uploads run concurrently via `Task.WhenAll` if the calling code starts them together. The `UploadCommand.IsRunning` binds to the last-executed instance — per-job `IsRunning` is tracked in `UploadJob.IsUploading`.
- **`OperationCanceledException` handling:** Always catch `OperationCanceledException` in async commands that accept `CancellationToken`. If unhandled, the exception propagates to `ExecutionTask` and becomes an unobserved task exception.
- **Progress granularity:** 20 chunks with 200 ms delay gives a 4-second simulated upload. Adjust the chunk count and delay to match real transfer characteristics. For real file uploads, report progress from the `HttpClient`'s `HttpContent` progress stream.

---

## Example 2: Retry-Enabled Data Loader

**Goal:** Load data from a service with automatic retry on failure. The command exposes a retry count, error state, and loading status. Each retry attempt updates the status message.

The built-in `AsyncRelayCommand` does not support retry. This example uses a combination of `[RelayCommand]` with manual retry logic in the method body, exposing retry state through observable properties.

### ViewModel

```csharp
// ViewModels/DataLoaderViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class DataLoaderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private int _retryCount;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isLoaded;

    public ObservableCollection<string> Items { get; } = new();

    private const int MaxRetries = 3;

    [RelayCommand]
    private async Task LoadDataAsync(CancellationToken token)
    {
        IsLoaded = false;
        HasError = false;
        RetryCount = 0;

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            token.ThrowIfCancellationRequested();

            StatusMessage = attempt == 1
                ? "Loading..."
                : $"Retrying ({attempt}/{MaxRetries})...";

            try
            {
                var data = await FetchDataAsync(token);

                // Success — populate items
                Items.Clear();
                foreach (var item in data)
                    Items.Add(item);

                IsLoaded = true;
                StatusMessage = $"Loaded {Items.Count} items";
                HasError = false;
                return;
            }
            catch (HttpRequestException ex)
            {
                RetryCount = attempt;
                StatusMessage = $"Attempt {attempt} failed: {ex.Message}";

                if (attempt >= MaxRetries)
                {
                    HasError = true;
                    StatusMessage = $"Failed after {MaxRetries} attempts";
                }
                else if (!token.IsCancellationRequested)
                {
                    // Wait before retrying with exponential back-off
                    var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1));
                    await Task.Delay(delay, token);
                }
            }
        }
    }

    private static async Task<List<string>> FetchDataAsync(CancellationToken token)
    {
        // Simulate an unreliable service
        await Task.Delay(300, token);

        // Simulate intermittent failure (60% chance of failure)
        if (Random.Shared.NextDouble() < 0.6)
            throw new HttpRequestException("Service temporarily unavailable");

        return new List<string>
        {
            "Item A", "Item B", "Item C",
            "Item D", "Item E",
        };
    }

    [RelayCommand]
    private async Task LoadDataWithCustomRetryAsync()
    {
        // Reset state
        IsLoaded = false;
        HasError = false;

        // Use a custom retry policy with exponential back-off
        var policy = new RetryPolicy
        {
            MaxRetries = 5,
            BaseDelayMs = 200,
            OnRetry = (attempt, ex) =>
            {
                RetryCount = attempt;
                StatusMessage = $"Retry {attempt}: {ex.Message}";
            },
        };

        try
        {
            var data = await policy.ExecuteAsync(ct => FetchDataAsync(ct));
            Items.Clear();
            foreach (var item in data)
                Items.Add(item);
            IsLoaded = true;
            StatusMessage = "Loaded successfully";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed: {ex.Message}";
        }
    }
}

// Simple retry policy helper (not part of CommunityToolkit.Mvvm)
public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public int BaseDelayMs { get; set; } = 200;
    public Action<int, Exception>? OnRetry { get; set; }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        var attempts = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return await operation(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                attempts++;
                if (attempts >= MaxRetries)
                    throw;

                OnRetry?.Invoke(attempts, ex);
                var delay = TimeSpan.FromMilliseconds(BaseDelayMs * Math.Pow(2, attempts - 1));
                await Task.Delay(delay, ct);
            }
        }
    }
}
```

### View

```xml
<!-- Views/DataLoaderView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.DataLoaderView"
             x:DataType="vm:DataLoaderViewModel">

  <StackPanel Margin="24" Spacing="12" MaxWidth="400">

    <!-- Status bar -->
    <TextBlock Text="{Binding StatusMessage, Mode=OneWay}"
               Foreground="{Binding HasError, Converter={StaticResource BoolToColor}}"
               FontSize="12" />

    <!-- Retry indicator -->
    <ProgressBar IsIndeterminate="{Binding LoadDataCommand.IsRunning}"
                 IsVisible="{Binding LoadDataCommand.IsRunning}" />

    <!-- Action buttons -->
    <StackPanel Orientation="Horizontal" Gap="8">
      <Button Content="Load Data"
              Command="{Binding LoadDataCommand}" />
      <Button Content="Load (Custom Retry)"
              Command="{Binding LoadDataWithCustomRetryCommand}" />
    </StackPanel>

    <!-- Retry count -->
    <TextBlock Text="{Binding RetryCount, StringFormat='Retry attempts: {0}'}"
               FontSize="11"
               Foreground="Gray"
               IsVisible="{Binding RetryCount, Converter={StaticResource PositiveToBool}}" />

    <!-- Loaded items -->
    <ListBox ItemsSource="{Binding Items}"
             IsVisible="{Binding IsLoaded}">
      <ListBox.ItemTemplate>
        <DataTemplate x:DataType="x:String">
          <TextBlock Text="{Binding .}" Margin="4,2" />
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>

    <!-- Error state -->
    <Border IsVisible="{Binding HasError}"
            Background="#fef2f2"
            BorderBrush="#dc2626"
            BorderThickness="1"
            CornerRadius="6"
            Padding="12">
      <StackPanel Gap="4">
        <TextBlock Text="Loading failed"
                   FontWeight="Bold"
                   Foreground="#dc2626" />
        <TextBlock Text="Check your connection and try again."
                   FontSize="11" />
        <Button Content="Retry"
                Command="{Binding LoadDataCommand}" />
      </StackPanel>
    </Border>
  </StackPanel>
</UserControl>
```

### How it works

1. **`LoadDataAsync(CancellationToken token)`** retries up to `MaxRetries` times inside a `for` loop. Each attempt calls `FetchDataAsync(token)`. On success, items are loaded and the loop returns. On `HttpRequestException`, the loop increments `RetryCount` and waits with exponential back-off before the next attempt.
2. **`StatusMessage` updates** on each attempt — "Loading...", "Retrying (2/3)...", "Attempt 2 failed: ...". These observable properties drive the status `TextBlock` in the view.
3. **`HasError` indicates persistent failure** after exhausting all retries. The view shows an error banner with a Retry button when `HasError` is true.
4. **`LoadDataCommand.IsRunning`** is bound to the `ProgressBar`. The async command manages the `IsRunning` state — the progress bar shows automatically while the command is executing and hides when it completes (success or failure).
5. **The `RetryPolicy` helper class** in the alternative `LoadDataWithCustomRetryAsync` method separates retry logic from the command method. This is a cleaner separation — the command only deals with success/failure, and the policy handles the retry mechanics.
6. **Exponential back-off** (`500ms × 2^(attempt-1)`): 500ms, 1000ms, 2000ms. This prevents hammering the failing service. The `Task.Delay` respects the `CancellationToken`, so the retry wait is cancelled if the user cancels the command.

### Design decisions and edge cases

- **Retry loop vs `RetryPolicy` class:** The inline retry loop in `LoadDataAsync` is self-contained and easy to read. The `RetryPolicy` class is reusable across commands. Choose based on whether retry logic is a one-off requirement or a cross-cutting concern.
- **What happens on the last failed attempt?** The `for` loop exits after `MaxRetries` without `return`. `HasError` becomes `true`. The `StatusMessage` is set to "Failed after N attempts." The error banner appears with a Retry button that re-runs the command from scratch.
- **`HttpRequestException` is the retry trigger:** Real code should distinguish between retriable (HTTP 500, timeout, network error) and non-retriable (HTTP 400, 404) failures. Catch only retriable exceptions in the retry loop; let non-retriable exceptions propagate to the outer catch.
- **Cancellation during retry delay:** The `Task.Delay(delay, token)` accepts the cancellation token. If the user cancels during the back-off wait, `OperationCanceledException` is thrown and bubbles up. The `LoadDataCommand`'s `ExecutionTask` then contains the cancelled exception — the view can bind to it if needed.
- **`Items.Clear()` before repopulating:** If a previous load succeeded, the items list is cleared on the next load attempt. This prevents stale data from showing during the next load. An alternative is to keep the old items visible until the new data arrives (replace at the end).

---

## What These Examples Demonstrate

| Scenario | Command technique | What to learn |
|---|---|---|
| File upload manager | `CancellationToken` injection + `IProgress<double>` | External cancellation, progress reporting, per-item command execution |
| Retry-enabled loader | Manual retry loop + `RetryPolicy` helper | Retry patterns with `AsyncRelayCommand`, exponential back-off, error state management |

The upload manager focuses on *cancellable progress* — a common pattern for any long-running operation. The data loader focuses on *resilience* — retrying on failure with user-visibile status. Together they cover async command patterns beyond the trivial button click.

## See Also

- [008 — RelayCommand in Depth](008-relay-command.md)
- [008V — Verbose Companion](008-relay-command-verbose.md)
- [002 — Command Binding](002-command-binding.md)
- [002V — Command Binding (verbose companion)](002-command-binding-verbose.md)
- [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)
- [CommunityToolkit.Mvvm Docs: RelayCommand](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/relaycommand)
