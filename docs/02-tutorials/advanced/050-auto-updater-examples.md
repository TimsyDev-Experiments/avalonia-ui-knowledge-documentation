---
tier: advanced
topic: deployment
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 050-auto-updater.md
---

# 050X — App Update / Auto-Updater: Real-World Examples

**What you'll build:** A user-consent update flow with download progress and restart prompt, and a silent forced-update startup flow that downloads in the background and applies on next launch.

**Prerequisites:** [050 — App Update / Auto-Updater](050-auto-updater.md). The [verbose companion](050-auto-updater-verbose.md) covers Velopack internals, delta update mechanics, and release channel strategies in depth.

---

## Example 1: Manual Update with User Consent and Progress

**Goal:** Check for updates on startup, show a banner if one is available, let the user click to download with a progress bar, and prompt to restart when ready.

### ViewModel

```csharp
// ViewModels/UpdateViewModel.cs
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Velopack;

namespace MyApp.ViewModels;

public partial class UpdateViewModel : ObservableObject
{
    private readonly Uri _updateUrl;
    private UpdateManager? _manager;
    private UpdateInfo? _updateInfo;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _updateStatus = "";

    [ObservableProperty]
    private int _downloadProgress;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private bool _isDownloadComplete;

    [ObservableProperty]
    private bool _isChecking;

    public UpdateViewModel()
    {
        _updateUrl = new Uri("https://releases.example.com/myapp");
    }

    public async Task CheckAsync()
    {
        IsChecking = true;
        UpdateStatus = "Checking for updates...";

        try
        {
            _manager = new UpdateManager(_updateUrl.ToString());
            _updateInfo = await _manager.CheckForUpdatesAsync();

            IsUpdateAvailable = _updateInfo is not null;
            UpdateStatus = _updateInfo is not null
                ? $"Version {_updateInfo.TargetFullRelease.Version} available"
                : "You have the latest version.";
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Update check failed: {ex.Message}";
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadUpdateAsync()
    {
        if (_manager is null || _updateInfo is null) return;

        IsDownloading = true;
        UpdateStatus = "Downloading...";

        try
        {
            await _manager.DownloadUpdatesAsync(_updateInfo,
                progress => DownloadProgress = (int)(progress * 100));

            IsDownloadComplete = true;
            UpdateStatus = "Download complete. Restart to apply.";
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private void ApplyAndRestart()
    {
        _manager?.ApplyUpdatesAndRestart(_updateInfo!);
        // This method does not return — process exits here.
    }

    [RelayCommand]
    private void Dismiss()
    {
        IsUpdateAvailable = false;
        UpdateStatus = "";
    }

    private bool CanDownload() => IsUpdateAvailable && !IsDownloading && !IsDownloadComplete;

    public void Dispose()
    {
        _manager?.Dispose();
    }
}
```

### App Integration

```csharp
// App.axaml.cs
public override async void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var updateVm = new UpdateViewModel();
        _ = updateVm.CheckAsync(); // fire-and-forget, non-blocking

        var mainVm = new MainViewModel(updateVm);
        desktop.MainWindow = new MainWindow
        {
            DataContext = mainVm,
        };
    }

    base.OnFrameworkInitializationCompleted();
}
```

### View

```xml
<!-- File: Views/UpdateBannerView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.UpdateBannerView"
             x:DataType="vm:UpdateViewModel">

  <Border Background="#2d2d5e" CornerRadius="8" Padding="12"
          IsVisible="{Binding IsUpdateAvailable}">
    <StackPanel Spacing="8">
      <TextBlock Text="{Binding UpdateStatus}"
                 FontWeight="Bold"
                 Foreground="White" />

      <ProgressBar Value="{Binding DownloadProgress}"
                   Minimum="0" Maximum="100"
                   IsVisible="{Binding IsDownloading}" />

      <StackPanel Orientation="Horizontal" Spacing="8">
        <Button Content="Download &amp; Install"
                Command="{Binding DownloadUpdateCommand}"
                IsVisible="{Binding IsUpdateAvailable}" />
        <Button Content="Restart Now"
                Command="{Binding ApplyAndRestartCommand}"
                IsVisible="{Binding IsDownloadComplete}" />
        <Button Content="Later"
                Command="{Binding DismissCommand}" />
      </StackPanel>
    </StackPanel>
  </Border>
</UserControl>
```

### How It Works

1. In `App.axaml.cs`, `OnFrameworkInitializationCompleted` creates the `UpdateViewModel` and kicks off `CheckAsync` without awaiting it. The main window appears immediately — the update check runs in the background.
2. `CheckAsync` creates an `UpdateManager`, fetches the `RELEASES` file from the server, and compares versions. If a newer version exists, `IsUpdateAvailable` becomes `true` and the banner becomes visible.
3. The user clicks **Download & Install**. `DownloadUpdatesAsync` streams the update package, reporting progress from 0.0 to 1.0. The ViewModel multiplies by 100 for the 0-100 `ProgressBar` binding.
4. When the download reaches 100%, `IsDownloadComplete` becomes `true`. The button switches from "Download & Install" to "Restart Now".
5. The user clicks **Restart Now**. `ApplyUpdatesAndRestart` terminates the process, replaces the app files, and restarts.
6. The **Later** button calls `DismissCommand`, which hides the banner but does not cancel the download. On next launch, the check runs again.

### Key Points

- The update check is fire-and-forget from `App.axaml.cs`. The main window loads without waiting for the network round-trip. This is critical for startup performance — a slow update check should not delay the app appearing.
- The `CanDownload` getter prevents the download command from being re-triggered while already downloading or after completion.
- `ApplyUpdatesAndRestart` does not return. The method is the last line of code that executes in the current process.
- Edge case: if the user clicks **Later** while a download is in progress, the download continues. `UpdateManager` disposes the temp directory and partial files when the app exits. The next check restarts from scratch.
- Edge case: if the network is unavailable, `CheckAsync` throws and `UpdateStatus` shows the error. The banner does not appear. The app continues normally.

---

## Example 2: Silent Forced Update on Startup

**Goal:** On every launch, silently check for updates, download in the background, and apply the update before showing the main window — no user interaction required.

### Update Service

```csharp
// Services/SilentUpdateService.cs
using System;
using System.Threading.Tasks;
using Velopack;

namespace MyApp.Services;

public class SilentUpdateService
{
    private readonly string _releasesUrl;

    public SilentUpdateService(string releasesUrl)
    {
        _releasesUrl = releasesUrl;
    }

    public async Task CheckAndApplyAsync()
    {
        try
        {
            using var mgr = new UpdateManager(_releasesUrl);

            var updateInfo = await mgr.CheckForUpdatesAsync();
            if (updateInfo is null)
                return; // already up to date

            // Silently download the full update
            await mgr.DownloadUpdatesAsync(updateInfo);

            // On Windows: apply on next launch (Update.exe replaces files)
            // On macOS/Linux: apply immediately and restart
            mgr.ApplyUpdatesAndRestart(updateInfo);

            // The process exits here on macOS/Linux.
            // On Windows, the process exits and Update.exe replaces files,
            // then the app is restarted.
        }
        catch (Exception)
        {
            // Silently fail — the app continues on the current version.
            // Log the failure for diagnostics.
        }
    }
}
```

### Program.cs Integration

```csharp
// Program.cs
public static async Task<int> Main(string[] args)
{
    // Silent update check before building the app
    var updateService = new SilentUpdateService("https://releases.example.com/myapp");
    await updateService.CheckAndApplyAsync();

    // Normal app startup — this only runs if no update was applied
    var builder = AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();

    return await builder.StartWithClassicDesktopLifetime(args);
}
```

### ViewModel (Status)

```csharp
// ViewModels/StartupViewModel.cs
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class StartupViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Checking for updates...";

    public void SetComplete()
    {
        StatusMessage = "Ready";
    }

    public void SetError(string message)
    {
        StatusMessage = $"Update skipped: {message}";
    }
}
```

### Alternative: Non-Blocking Silent Download with Post-Launch Apply

A less aggressive variant that does not block startup but still applies automatically:

```csharp
// Services/BackgroundUpdateService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Velopack;

namespace MyApp.Services;

public class BackgroundUpdateService : BackgroundService
{
    private readonly string _releasesUrl;

    public BackgroundUpdateService(string releasesUrl)
    {
        _releasesUrl = releasesUrl;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a few seconds to let the app start first
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            using var mgr = new UpdateManager(_releasesUrl);

            var updateInfo = await mgr.CheckForUpdatesAsync();
            if (updateInfo is null)
                return;

            await mgr.DownloadUpdatesAsync(updateInfo);

            // On next launch, the update is applied automatically.
            // On macOS/Linux, ApplyUpdatesAndRestart exits immediately.
            // On Windows, the downloaded package is staged for next launch.
            mgr.ApplyUpdatesAndRestart(updateInfo);
        }
        catch (OperationCanceledException)
        {
            // App is shutting down
        }
        catch (Exception)
        {
            // Log and continue on current version
        }
    }
}
```

This variant runs as a `BackgroundService`, waits 5 seconds for the UI to initialize, then does the check/download/apply cycle. If the user is on Windows, the update is staged and applied on next launch. On macOS/Linux, the app restarts immediately after the 5-second delay.

### How It Works

1. **Blocking variant** (`Program.cs`): `Main` calls `CheckAndApplyAsync()` synchronously (but with `await`). The app does not start until the update check completes. If an update is found, it is downloaded and applied. On macOS/Linux, the process restarts and `Main` runs again with the updated version. On Windows, `ApplyUpdatesAndRestart` exits the process; `Update.exe` replaces files and restarts the app.
2. **Non-blocking variant** (`BackgroundService`): The app starts immediately. After a 5-second delay, the service checks and downloads the update. On Windows, the update is applied on next launch (the `BackgroundService` calls `ApplyUpdatesAndRestart` which exits the process). On macOS/Linux, the app restarts mid-session.

### Key Points

- The silent flow has zero UI — no progress bars, no buttons. The user sees only their normal app. If an update downloads, it is applied silently.
- The blocking variant is simpler but delays startup. The non-blocking variant is better UX but requires the `BackgroundService` pattern and handles process exit as part of normal operation.
- `ApplyUpdatesAndRestart` on Windows stages the update for next launch and then exits. The `Update.exe` process replaces the files while the app is closed. On macOS/Linux, the replacement happens immediately.
- Edge case: if the download fails mid-way, the `catch` block silently swallows the exception. The app continues on the current version. The next launch retries the check.
- Edge case: if the user closes the app while the download is in progress (non-blocking variant), `stoppingToken` is signalled and `DownloadUpdatesAsync` throws `OperationCanceledException`. The catch block handles this gracefully without crashing.
- Edge case: the blocking variant adds ~2-10 seconds to startup time depending on network speed. For apps where startup time is critical, use the non-blocking variant or the user-consent flow from Example 1.

---

## What These Examples Demonstrate

| Scenario | User interaction | Update timing | Best for |
|---|---|---|---|
| User consent update | Banner with download/restart buttons | Background check, explicit download and restart | Apps where users control when to update |
| Silent forced update | None — fully automatic | Startup blocking or deferred background | Managed deployments, kiosk apps, non-interactive scenarios |

The consent flow gives the user control and shows progress — appropriate for productivity apps where interrupting work with a restart is disruptive. The silent flow is appropriate for background services, kiosk apps, or enterprise-managed installations where the app should always be on the latest version without user involvement.

## See Also

- [050 — App Update / Auto-Updater](050-auto-updater.md)
- [050V — Verbose Companion](050-auto-updater-verbose.md)
- [045 — CI/CD for Avalonia](045-cicd-for-avalonia.md)
- [044 — Background Services & Progress Reporting](044-background-services-and-progress.md)
- [Velopack docs](https://velopack.io/docs)
