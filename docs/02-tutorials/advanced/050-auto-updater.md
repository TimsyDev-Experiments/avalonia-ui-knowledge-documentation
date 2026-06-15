---
tier: advanced
topic: deployment
estimated: 20 min
researched: 2026-06-13
avalonia-version: 12.0.4
---

# 050 -- App Update / Auto-Updater

**What you'll learn:** Integrate Velopack for automatic application updates, check for updates on startup, download in the background with progress reporting, and apply updates with restart.

**Prerequisites:** [039 -- NativeAOT and Trimming](039-nativeaot-trimming.md), [045 -- CI/CD](045-cicd-for-avalonia.md)

---

## 1. Velopack overview

[Velopack](https://velopack.io/) is a cross-platform update system for .NET desktop apps. It supports Windows, Linux, and macOS with delta updates, release channels, and silent background installation.

```shell
dotnet add package Velopack
```

## 2. Package your app with Velopack

Install the CLI tool:

```shell
dotnet tool install -g vpk
```

Create a release:

```shell
vpk pack --packId "MyApp" --packVersion "1.0.0"
    --packDir "publish/win-x64"
    --outputDir "releases"
    --mainExe "MyApp.exe"
```

Upload the contents of `releases/` to a web server, S3 bucket, or GitHub Releases.

### Release channels

```shell
vpk pack --packId "MyApp" --packVersion "1.0.0" --channel "stable"
vpk pack --packId "MyApp" --packVersion "1.1.0-beta" --channel "beta"
vpk pack --packId "MyApp" --packVersion "1.1.0-alpha" --channel "alpha"
```

## 3. Check for updates

```csharp
public partial class UpdateViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _updateStatus = "";

    [ObservableProperty]
    private int _downloadProgress;

    private UpdateManager? _updateManager;

    public async Task InitializeAsync()
    {
        // The URL where your releases are hosted
        _updateManager = new UpdateManager("https://example.com/releases");

        try
        {
            var result = await _updateManager.CheckForUpdatesAsync();
            IsUpdateAvailable = result is not null;
            if (result is not null)
            {
                UpdateStatus = $"Update {result.TargetFullRelease.Version} available " +
                               $"(current: {result.CurrentlyInstalledVersion})";
            }
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Update check failed: {ex.Message}";
        }
    }
}
```

## 4. Download with progress

```csharp
[RelayCommand(CanExecute = nameof(IsUpdateAvailable))]
private async Task DownloadUpdateAsync()
{
    if (_updateManager is null) return;

    try
    {
        UpdateStatus = "Downloading...";

        await _updateManager.DownloadUpdatesAsync(result!,
            progress => DownloadProgress = (int)progress);

        UpdateStatus = "Download complete. Restart to apply.";
    }
    catch (Exception ex)
    {
        UpdateStatus = $"Download failed: {ex.Message}";
    }
}
```

## 5. Apply and restart

```csharp
[RelayCommand]
private void ApplyAndRestart()
{
    _updateManager?.ApplyUpdatesAndRestart(result!);
}
```

This exits the current process, applies the update (on next launch for Windows, immediately for macOS/Linux), and restarts the app.

## 6. Full update flow on startup

```csharp
// App.axaml.cs or Program.cs
public static async Task CheckForUpdatesAndApplyAsync()
{
    using var mgr = new UpdateManager("https://example.com/releases");

    var result = await mgr.CheckForUpdatesAsync();
    if (result is null) return; // already up to date

    // Silently download in the background
    await mgr.DownloadUpdatesAsync(result);

    // Apply on next launch (Windows) or restart now (macOS/Linux)
    mgr.ApplyUpdatesAndRestart(result);
}
```

## 7. UI for update progress

```xml
<StackPanel Spacing="8" IsVisible="{Binding IsUpdateAvailable}">
  <TextBlock Text="{Binding UpdateStatus}" />

  <ProgressBar Value="{Binding DownloadProgress}"
               Minimum="0" Maximum="100"
               IsVisible="{Binding IsDownloading}" />

  <Button Content="Download Update"
          Command="{Binding DownloadUpdateCommand}"
          IsVisible="{Binding IsUpdateAvailable}" />

  <Button Content="Restart &amp; Apply"
          Command="{Binding ApplyAndRestartCommand}"
          IsVisible="{Binding IsDownloadComplete}" />
</StackPanel>
```

```csharp
[ObservableProperty]
private bool _isDownloading;

[ObservableProperty]
private bool _isDownloadComplete;

partial void OnDownloadProgressChanged(int value)
{
    IsDownloading = value > 0 && value < 100;
    IsDownloadComplete = value >= 100;
}
```

## 8. Delta updates

Velopack supports delta updates out of the box. When you pack a release, it automatically generates a delta from the previous release. The client downloads only the changed bytes, making updates significantly smaller.

To enable, ensure `vpk` has access to the previous release file when packing:

```shell
vpk pack --packId "MyApp" --packVersion "1.1.0"
    --packDir "publish/win-x64"
    --outputDir "releases"
    --mainExe "MyApp.exe"
    --delta "releases/MyApp-1.0.0-full.nupkg"
```

## 9. Update in CI/CD

Integrate with the publish workflow from [tutorial 045](045-cicd-for-avalonia.md):

```yaml
# .github/workflows/release.yml
- name: Pack with Velopack
  shell: pwsh
  run: |
    vpk pack --packId "MyApp" `
      --packVersion "${{ github.ref_name }}" `
      --packDir "publish/win-x64" `
      --outputDir "releases" `
      --mainExe "MyApp.exe"

- name: Upload releases
  uses: actions/upload-artifact@v4
  with:
    name: velopack-release
    path: releases/
```

## Key takeaways

- Velopack is the recommended update library for .NET desktop apps (cross-platform)
- `UpdateManager` handles check, download, and apply in three distinct phases
- Delta updates reduce download size automatically when the previous release is available
- Use release channels (`stable`, `beta`, `alpha`) for staged rollouts
- Call `ApplyUpdatesAndRestart` to exit, apply, and relaunch
- Hook the update check on startup with a silent download to minimize user disruption

---

## See Also

- [045 -- CI/CD for Avalonia](045-cicd-for-avalonia.md)
- [039 -- NativeAOT and Trimming](039-nativeaot-trimming.md)
- [Velopack docs](https://velopack.io/docs)
- [050V -- App Update / Auto-Updater (verbose companion)](050-auto-updater-verbose.md)
- [050X -- App Update / Auto-Updater (examples)](050-auto-updater-examples.md)
