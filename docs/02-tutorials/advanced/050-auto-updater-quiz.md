---
tier: advanced
topic: auto-updater
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 050-auto-updater.md
---

# Quiz — App Update / Auto-Updater

```quiz
Q: What are the three distinct phases of the Velopack update lifecycle?
A. Download, Install, Restart || The first phase should be checking; download comes second.
B. CheckForUpdatesAsync, DownloadUpdatesAsync, ApplyUpdatesAndRestart (correct) || The tutorial documents these three distinct phases: check for a new version, download it with optional progress reporting, then apply and restart.
C. Pack, Upload, Release || These are the CI/CD packaging phases, not the client-side update lifecycle.
D. Initialize, Update, Cleanup || Velopack's UpdateManager separates the operation into check, download, and apply steps.
Explanation: The three-phase lifecycle is CheckForUpdatesAsync → DownloadUpdatesAsync → ApplyUpdatesAndRestart, each with its own method on UpdateManager.
```

```quiz
Q: How does the vpk tool generate delta updates to reduce download size?
A. It computes a binary diff between the current and previous release when --delta is provided (correct) || Passing --delta with the path to the previous .nupkg file generates a delta package containing only the changed bytes between releases.
B. It compresses each release package using a higher compression level || Delta updates are based on binary diffs, not compression level — full packages are always the same size regardless of compression.
C. It splits the release into multiple smaller chunks that download in parallel || Parallel chunking is a download optimization, not a delta strategy.
D. It downloads only the updated .dll files and skips unchanged assemblies || Velopack works at the package level with full and delta .nupkg files, not individual assemblies.
Explanation: The vpk pack --delta flag points to the previous full release, and Velopack generates a delta (binary diff) package containing only the bytes that changed.
```

```quiz
Q: How should an Avalonia app display update progress in the UI when using Velopack?
A. The DownloadUpdatesAsync method accepts an Action<int> progress callback that updates an observable property bound to a ProgressBar (correct) || The progress callback receives an integer (0-100) which can be assigned to an [ObservableProperty] bound to a ProgressBar in XAML.
B. Velopack raises a ProgressChanged event on the UI thread automatically || The callback runs on a background thread; the developer must ensure UI-safe property updates.
C. The UpdateManager exposes a DownloadProgress observable collection || UpdateManager provides a callback-based API, not an observable collection.
D. Progress reporting requires an additional NuGet package Velopack.Progress || No additional package is needed — the callback parameter is built into DownloadUpdatesAsync.
Explanation: DownloadUpdatesAsync accepts a progress Action<int> that the ViewModel uses to set an observable DownloadProgress property bound to a ProgressBar in the XAML view.
```

```quiz
Q: What does ApplyUpdatesAndRestart do on the Windows platform?
A. It immediately applies the update and restarts the application in one step || On Windows, the update is staged for the next launch; the restart triggers the stage.
B. It exits the current process, schedules the update to apply on next launch, and restarts the app (correct) || On Windows, the update is staged by the Velopack installer and applied on the next process start. On macOS and Linux the update is applied immediately before restart.
C. It downloads the update again to verify integrity before applying || ApplyUpdatesAndRestart does not download — the update must already be downloaded via DownloadUpdatesAsync.
D. It opens the Velopack update wizard UI || ApplyUpdatesAndRestart performs a silent apply and restart without showing a wizard.
Explanation: ApplyUpdatesAndRestart terminates the process, stages the update (on Windows it applies on next launch; on macOS/Linux it applies immediately), and relaunches the app.
```

```quiz
Q: What is the recommended approach for checking updates on application startup?
A. Show a dialog asking the user if they want to check for updates || This interrupts the user; the recommended approach is silent.
B. Call UpdateManager.CheckForUpdatesAsync on a background thread and silently download any available update (correct) || The tutorial recommends checking on startup with a background download, minimizing disruption — the user is only notified when the update is ready to apply.
C. Register a BackgroundService that checks every hour || Periodic checking is useful for long-running apps, but the primary check should happen on startup.
D. Embed the latest version number in App.axaml and compare at runtime || Hardcoding version numbers requires recompilation; Velopack's UpdateManager fetches the latest version from the release server.
Explanation: The tutorial shows a silent startup check that downloads updates in the background, then presents a restart prompt only when the download is complete.
```
