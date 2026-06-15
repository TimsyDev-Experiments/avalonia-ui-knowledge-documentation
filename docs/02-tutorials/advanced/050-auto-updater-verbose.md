---
tier: advanced
topic: deployment
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 050-auto-updater.md
---

# 050V — App Update / Auto-Updater: An In-Depth Companion

**What you'll learn in this companion:** Not just how to call `CheckForUpdatesAsync`, but how Velopack packages apps, what a "delta update" actually contains, how release channels work, why `ApplyUpdatesAndRestart` behaves differently on each OS, how to structure the startup update check without blocking the UI, and how to build a full update lifecycle with progress, error handling, and rollback.

**Prerequisites:** [039 — NativeAOT and Trimming](039-nativeaot-trimming.md), [045 — CI/CD for Avalonia](045-cicd-for-avalonia.md)

**You should already have read:** [050 — App Update / Auto-Updater](050-auto-updater.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Velopack — How It Differs from Other Update Systems

### What Velopack actually does

Velopack is an update system for .NET desktop apps. At its core, it handles three phases:

1. **Packaging** — Convert your published app into a Velopack release (a `.nupkg` file with metadata)
2. **Update detection** — Compare the app's current version against the latest on the server
3. **Download + apply** — Download the update package, replace the files, and restart the app

Unlike a simple "download a ZIP and unzip" approach, Velopack:
- Manages file replacement safely (no partially-updated app state)
- Supports delta updates (only download changed bytes)
- Handles rollback and uninstall via the Velopack manager
- Integrates with the OS for app registration (Start Menu entries on Windows, `.desktop` files on Linux)

### How it compares to alternatives

| Feature | Velopack | Squirrel.Windows | AutoUpdater.NET |
|---|---|---|---|
| Cross-platform | Windows, macOS, Linux | Windows only | Windows only |
| Delta updates | Yes (automatic) | No | No |
| Release channels | Yes (stable, beta, alpha) | No | No |
| Silent background install | Yes | Yes | Partial |
| NuGet-based packaging | Yes (`.nupkg`) | Yes (`.nupkg`) | No |
| Maintenance | Active (2024+) | Archived by GitHub | Maintained |

Velopack is the spiritual successor to Squirrel.Windows, with the same packaging model but full cross-platform support.

---

## 2. The VPK CLI — What `vpk pack` Does

```shell
vpk pack --packId "MyApp" --packVersion "1.0.0"
    --packDir "publish/win-x64"
    --outputDir "releases"
    --mainExe "MyApp.exe"
```

`vpk pack` processes the published output (`publish/win-x64/`) and produces:

1. **`MyApp-1.0.0-full.nupkg`** — The full release package containing all application files
2. **`MyApp-1.0.0-delta.nupkg`** — If a previous release exists, a delta containing only changed files
3. **`RELEASES`** — A manifest file listing all available packages, their sizes, and checksums

The `RELEASES` file is critical — it's the first file the client downloads when checking for updates. It looks like:

```
MyApp-1.0.0-full.nupkg SHA1:abc123 12345678
MyApp-1.1.0-full.nupkg SHA1:def456 87654321
MyApp-1.1.0-delta.nupkg SHA1:ghi789 1234567
```

The client reads the `RELEASES` file, finds the latest version newer than the current install, and downloads either the full or delta package based on availability.

### What `--mainExe` is for

`--mainExe` tells Velopack which executable is the app's entry point. Velopack uses this to:
- Create Start Menu shortcuts (Windows)
- Create `.desktop` entries (Linux)
- Set the restart target for `ApplyUpdatesAndRestart`

If you specify the wrong executable, `ApplyUpdatesAndRestart` launches the wrong binary.

---

## 3. The UpdateManager — What Constructor URL Does

```csharp
_updateManager = new UpdateManager("https://example.com/releases");
```

The URL points to a directory on a web server (or S3 bucket, or GitHub Releases) containing the `RELEASES` file and the `.nupkg` files. The client:

1. Downloads `{url}/RELEASES`
2. Parses the manifest
3. Downloads the appropriate package (full or delta)
4. Extracts and applies the update

### The URL must end with a trailing slash or not?

Both work. Velopack appends the file names relative to the URL. If the URL is `https://example.com/releases`, it fetches `https://example.com/releases/RELEASES`. If it's `https://example.com/releases/`, same result.

### Local file paths

For testing, you can use a local file path:

```csharp
var mgr = new UpdateManager(@"C:\releases");
```

This reads the RELEASES file from disk. Useful for integration testing without deploying to a server.

---

## 4. CheckForUpdatesAsync — The UpdateInfo Contract

```csharp
var result = await _updateManager.CheckForUpdatesAsync();
```

`CheckForUpdatesAsync` returns an `UpdateInfo?` object (null if no update is available). `UpdateInfo` contains:

- `CurrentlyInstalledVersion` — the version currently running (from the installed app's manifest)
- `TargetFullRelease` — the `ReleaseEntry` for the latest full package
- `TargetDeltaRelease` — the `ReleaseEntry` for the delta package (if available)
- `ReleasesToInstall` — ordered list of releases that need to be applied (supports skipping versions)

The method does NOT download anything — it only checks. It fetches the `RELEASES` file, parses it, and compares versions.

### What happens on first check

On first run after installation, there is no previous version in the local manifest. `CurrentlyInstalledVersion` returns the version that was packaged. If the server has the same version, no update is needed. If the server has a newer version, the update is identified.

### Error handling

```csharp
catch (Exception ex)
{
    UpdateStatus = $"Update check failed: {ex.Message}";
}
```

Network failures (no internet, server down, DNS resolution failure) throw exceptions. The catch block prevents the update failure from crashing the app. The app continues using the current version — updates are best-effort, not critical.

---

## 5. DownloadUpdatesAsync — Progress Reporting Mechanics

```csharp
await _updateManager.DownloadUpdatesAsync(result!,
    progress => DownloadProgress = (int)progress);
```

The progress callback receives a `double` from 0.0 to 1.0. Casting to `int` gives a 0-100 integer for binding to `ProgressBar`.

The download is background-friendly — `DownloadUpdatesAsync` is async and uses `HttpClient` internally. It does not block the UI thread if you await it properly.

### What happens during download

1. Velopack reads the `ReleasesToInstall` from the update info
2. For each release, it downloads the full or delta package
3. Downloads are verified against the SHA1 in the `RELEASES` file
4. The downloaded package is stored in a temporary directory
5. Multiple releases are downloaded in sequence (not parallel)

### Failure during download

If the network drops mid-download, `DownloadUpdatesAsync` throws. The partially-downloaded package is discarded (temp directory cleaned up). The next check will restart the download.

---

## 6. ApplyUpdatesAndRestart — Why Stop, Replace, Restart

```csharp
_updateManager?.ApplyUpdatesAndRestart(result!);
```

This method:

1. **Exits the current process** synchronously (it calls `Environment.Exit` internally)
2. **Starts a Velopack update process** (`Update.exe` on Windows, `Update` on macOS/Linux)
3. The update process waits for the main app to fully exit
4. It replaces the app files with the downloaded update package
5. It restarts the app using the main executable specified during `vpk pack`

On Windows, the process is:
```
App.exe → calls ApplyUpdatesAndRestart → App exits → Update.exe renames old files → copies new files → starts App.exe
```

### Why the method does not return

`ApplyUpdatesAndRestart` does not return. It terminates the process. Any code after the call will not execute. Treat it like `Environment.Exit` — it's the last thing your code does.

### OS differences

| OS | How update/restart works |
|---|---|
| Windows | Update.exe performs the replacement while the app is closed, then restarts |
| macOS | The app bundle is replaced atomically, then relaunched via `open` |
| Linux | Files are replaced in-place, the app is restarted via the shell |

On macOS, the .app bundle is a directory. Velopack replaces the bundle's contents. The `open` command re-launches the app via the LaunchServices database.

---

## 7. Startup Update Flow — Why Silent Download

```csharp
public static async Task CheckForUpdatesAndApplyAsync()
{
    using var mgr = new UpdateManager("https://example.com/releases");
    var result = await mgr.CheckForUpdatesAsync();
    if (result is null) return;

    await mgr.DownloadUpdatesAsync(result);
    mgr.ApplyUpdatesAndRestart(result);
}
```

This is the "automatic update" pattern — no user interaction needed. It blocks the startup by downloading the update before showing the main window.

### The problem with blocking startup

If the user has a slow connection, the download could take minutes before the app appears. Better pattern: check async, start the app, download in the background, prompt to restart:

```
App launches
  → Start update check in background (non-blocking)
  → Show main window immediately
  → When update check completes:
      → If update available: show "Update available" banner
      → If user clicks "Install": download + restart
      → If user clicks "Later": download silently, prompt on next launch
```

### The using statement

```csharp
using var mgr = new UpdateManager("...");
```

`UpdateManager` creates a temporary directory for downloads. Disposing it cleans up the temp directory and any partial downloads. Always dispose the manager.

---

## 8. Release Channels — How They Work

```shell
vpk pack --packId "MyApp" --packVersion "1.0.0" --channel "stable"
vpk pack --packId "MyApp" --packVersion "1.1.0-beta" --channel "beta"
vpk pack --packId "MyApp" --packVersion "1.1.0-alpha" --channel "alpha"
```

The `--channel` flag embeds the channel name in the release metadata. When the client checks for updates, it only sees releases for its own channel.

A client installed from the `stable` channel only sees `1.0.0`, not `1.1.0-beta`. To switch channels, the user must reinstall from a different channel URL.

### Preview channel pattern

For a "beta" program:
1. Release to `beta` channel first
2. Internal testers on `beta` channel verify
3. Pack the same version to `stable` channel
4. Stable users see the update

This requires rebuilding the package for each channel — not a tag change on the same package.

---

## 9. UI State Management — The Progress Properties Pattern

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

`OnDownloadProgressChanged` is a partial method generated by `[ObservableProperty]`. It fires whenever `DownloadProgress` is set. Using it to derive `IsDownloading` and `IsDownloadComplete` keeps the boolean properties synchronized with the progress value without manual assignment in multiple places.

### Why not compute IsDownloading in XAML

```xml
<!-- Computed in ViewModel instead -->
<ProgressBar Value="{Binding DownloadProgress}"
             IsVisible="{Binding IsDownloading}" />
```

Computing `IsDownloading` in the ViewModel (via the partial method) rather than in XAML as a multi-binding or converter keeps the XAML simple and the logic testable.

---

## 10. Delta Updates — What Gets Smaller

```shell
vpk pack --packId "MyApp" --packVersion "1.1.0"
    --delta "releases/MyApp-1.0.0-full.nupkg"
```

A delta update contains only the bytes that changed between two releases. Velopack uses a binary diff algorithm if the previous release's `.nupkg` is available during packing.

Delta sizes for a typical Avalonia app:
- Full package: 60-80 MB
- Delta after small code change: 1-5 MB
- Delta after major dependency update: 20-40 MB

Deltas are calculated file-by-file within the package. If only your `.dll` changed, only that file's bytes are in the delta.

### Why deltas require the previous .nupkg

The delta is computed by diffing the new release against the previous full release. Without the previous `.nupkg`, `vpk` creates only a full package. Store previous releases in your CI artifact store for delta generation.

---

## 11. CI/CD Integration — The Release Flow

```yaml
- name: Pack with Velopack
  shell: pwsh
  run: |
    vpk pack --packId "MyApp" `
      --packVersion "${{ github.ref_name }}" `
      --packDir "publish/win-x64" `
      --outputDir "releases" `
      --mainExe "MyApp.exe"
```

`${{ github.ref_name }}` is the tag name (e.g., `1.2.3`). This becomes the package version. Velopack versions follow NuGet versioning — `1.2.3`, `1.2.3-beta`, `1.2.3-beta.1` are all valid.

For delta updates in CI, download the previous release artifacts before packing:

```yaml
- name: Download previous release
  uses: actions/download-artifact@v4
  with:
    name: velopack-release
    path: previous-release/

- name: Pack with delta
  run: |
    vpk pack ... --delta "previous-release/*-full.nupkg"
```

---

## 12. Testing Updates Locally

To test the update flow without deploying to a server:

1. Pack version 1.0.0 and install it:
   ```shell
   vpk pack ... --packVersion "1.0.0" --outputDir "releases"
   vpk generate ... # creates installer
   ```

2. Install the app from the installer.

3. Pack version 1.1.0:
   ```shell
   vpk pack ... --packVersion "1.1.0" --outputDir "releases"
   ```

4. Start a local HTTP server in the `releases/` directory:
   ```shell
   dotnet tool install -g dotnet-serve
   dotnet serve --directory releases
   ```

5. Point `UpdateManager` to `http://localhost:port/`.

---

## Key Takeaways

- Velopack is cross-platform with delta updates, release channels, and silent background install
- `UpdateManager` constructor takes the URL hosting the RELEASES file and .nupkg packages
- `CheckForUpdatesAsync` only compares versions — no download
- `DownloadUpdatesAsync` reports progress from 0.0 to 1.0
- `ApplyUpdatesAndRestart` terminates the process — it is always the last line of code that runs
- Delta updates require the previous full `.nupkg` at pack time
- Release channels (`stable`, `beta`, `alpha`) are separate packaging runs — not tag-based
- On startup, download updates in the background and prompt for restart rather than blocking the UI
- Use `using` with `UpdateManager` to clean up temporary download files
- Always handle network exceptions in the update check — don't let a failed update crash the app

---

## See Also

- [050 — App Update / Auto-Updater (original)](050-auto-updater.md)
- [050X — App Update / Auto-Updater (examples)](050-auto-updater-examples.md)
- [045 — CI/CD for Avalonia](045-cicd-for-avalonia.md)
- [039 — NativeAOT and Trimming](039-nativeaot-trimming.md)
- [Velopack docs](https://velopack.io/docs)
- [Velopack GitHub](https://github.com/velopack/velopack)
