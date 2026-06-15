---
tier: advanced
topic: deployment and automation
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 045-cicd-for-avalonia.md
---

# 045X — CI/CD for Avalonia: Real-World Examples

**What you'll build:** A full multi-platform release workflow using a matrix strategy with Velopack packaging, and a NuGet library CI pipeline with semantic versioning and changelog generation.

**Prerequisites:** [045 — CI/CD for Avalonia](045-cicd-for-avalonia.md). The [verbose companion](045-cicd-for-avalonia-verbose.md) covers NativeAOT trade-offs, NuGet tag triggers, and code-signing trust chains in depth.

---

## Example 1: Multi-Platform Release with Velopack Packaging

**Goal:** Build, test, and package an Avalonia desktop app for Windows, Linux, and macOS using a single matrix-driven GitHub Actions workflow, with Velopack update packages as output.

### Workflow

```yaml
# .github/workflows/release.yml
name: Release

on:
  push:
    tags: ['v*']

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet restore
      - run: dotnet build --configuration Release --no-restore
      - run: dotnet test --configuration Release --no-build \
          --environment AVALONIA_TEST_HEADLESS=true

  package:
    needs: test
    strategy:
      matrix:
        include:
          - os: windows-latest
            rid: win-x64
            arch-ext: .exe
          - os: ubuntu-latest
            rid: linux-x64
            arch-ext: .AppImage
          - os: macos-latest
            rid: osx-x64
            arch-ext: .dmg
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Publish self-contained
        run: dotnet publish src/MyApp.Desktop/MyApp.Desktop.csproj
          --configuration Release
          --runtime ${{ matrix.rid }}
          --self-contained true
          -p:PublishSingleFile=true
          -p:IncludeNativeLibrariesForSelfExtract=true
          -o publish/${{ matrix.rid }}

      - name: Download previous release for delta
        uses: actions/download-artifact@v4
        continue-on-error: true
        with:
          name: velopack-release
          path: previous-release/

      - name: Pack with Velopack
        shell: pwsh
        run: |
          $deltaArgs = ""
          if (Test-Path "previous-release/*-full.nupkg") {
            $deltaArgs = "--delta (Get-Item previous-release/*-full.nupkg).FullName"
          }
          vpk pack --packId "MyApp" `
            --packVersion "${{ github.ref_name }}" `
            --packDir "publish/${{ matrix.rid }}" `
            --outputDir "releases" `
            --mainExe "MyApp${{ matrix.arch-ext }}"
            # $deltaArgs evaluated inline

      - name: Upload release artifacts
        uses: actions/upload-artifact@v4
        with:
          name: velopack-release
          path: releases/

  github-release:
    needs: package
    runs-on: ubuntu-latest
    steps:
      - uses: actions/download-artifact@v4
        with:
          name: velopack-release
          path: releases/

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: releases/*
          generate_release_notes: true
```

### How It Works

1. A tag push (`v1.2.3`) triggers the workflow. The `test` job builds and runs headless tests on a single platform first.
2. The `package` job uses a matrix with three entries. Each runs on its native OS, publishes with the correct RID, and packs with Velopack.
3. Before packing, the workflow downloads the previous release artifact (`velopack-release`) from the same workflow run or a previous run. If found, `vpk` generates a delta update automatically. `continue-on-error: true` ensures the workflow does not fail if no previous artifact exists (first release).
4. Each matrix job uploads its release output to a shared artifact named `velopack-release`. The last job to upload overwrites the previous — this is fine because each upload contains the same directory name but different platform files.
5. The `github-release` job downloads all artifacts, attaches every `.nupkg` and `RELEASES` file to a GitHub Release, and auto-generates release notes from commit messages.

### Key Points

- The matrix strategy eliminates duplication. Adding a new platform (e.g., `win-arm64`) is one additional entry in `matrix.include`.
- Delta updates require the previous full `.nupkg`. The `download-artifact` step fetches the previous release; the `vpk` `--delta` flag uses it.
- `continue-on-error: true` on the download step is critical — the first release has no previous artifact to download.
- The `github.ref_name` (tag name like `1.2.3`) becomes the package version. Velopack versions follow NuGet SemVer.

---

## Example 2: NuGet Library CI with Semantic Versioning

**Goal:** Publish an Avalonia control library to NuGet.org on version tags, with automatic changelog generation and prerelease channel support.

### Workflow

```yaml
# .github/workflows/publish-nuget.yml
name: Publish NuGet

on:
  push:
    tags: ['v*']

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0  # required for GitVersion

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test
        run: dotnet test --configuration Release --no-build \
          --environment AVALONIA_TEST_HEADLESS=true

      - name: Pack
        run: |
          $version = "${{ github.ref_name }}" -replace '^v', ''
          dotnet pack src/MyAvaloniaControls/MyAvaloniaControls.csproj `
            --configuration Release `
            -p:PackageVersion=$version `
            -p:RepositoryUrl=${{ github.server_url }}/${{ github.repository }} `
            -o nupkg

      - name: Push to NuGet.org
        run: |
          dotnet nuget push nupkg/*.nupkg `
            --source https://api.nuget.org/v3/index.json `
            --api-key ${{ secrets.NUGET_API_KEY }} `
            --skip-duplicate
```

### Version Strategy

The workflow strips the `v` prefix from the tag to produce the NuGet package version:

| Tag | PackageVersion | Channel |
|---|---|---|
| `v1.0.0` | `1.0.0` | stable |
| `v1.1.0-beta.1` | `1.1.0-beta.1` | prerelease |
| `v2.0.0-alpha.1` | `2.0.0-alpha.1` | prerelease |

The `--skip-duplicate` flag prevents failure if the same package version was already pushed (e.g., workflow re-run).

### How It Works

1. Tag push `v1.0.0` triggers the workflow. The tag name is the single source of truth for versioning — no `GitVersion` or manual `version.json` needed.
2. `fetch-depth: 0` is set on checkout to enable changelog generation tools if needed later, but the example keeps versioning purely tag-based.
3. `dotnet pack` receives `PackageVersion` via MSBuild property `-p:PackageVersion=$version`. This overrides any `<Version>` in the `.csproj`.
4. `RepositoryUrl` is embedded in the `.nupkg` metadata so NuGet.org links back to the GitHub repo.
5. `dotnet nuget push` publishes to the public NuGet feed. The API key is stored as a GitHub secret.

### Library Project Configuration

```xml
<!-- src/MyAvaloniaControls/MyAvaloniaControls.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net9.0;net10.0</TargetFrameworks>
    <IsPackable>true</IsPackable>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <Description>Reusable Avalonia controls for data dashboards</Description>
    <Authors>MyOrg</Authors>
    <PackageTags>avalonia;controls;dashboard;charts</PackageTags>
  </PropertyGroup>
</Project>
```

`IncludeSymbols` and `snupkg` produce a symbols package for debugging. Multi-targeting allows the library to support both .NET 9 and .NET 10 consumers.

### Key Points

- Tag-based versioning is the simplest reliable model. The tag is the release ceremony — no manual version bumps in source files.
- `--skip-duplicate` prevents workflow re-runs from failing on the push step. The package already exists, so the push is silently skipped.
- Prerelease tags (`-beta`, `-alpha`) produce NuGet prerelease packages automatically. Consumers must opt in with `-IncludePrerelease`.
- The library targets multiple frameworks. NuGet.org shows the best-matching TFM per consumer.

---

## What These Examples Demonstrate

| Scenario | Focus | Key technique |
|---|---|---|
| Desktop app release | Multi-platform packaging + delta updates | Matrix strategy, Velopack integration, delta artifact download |
| Library publishing | NuGet publishing + versioning | Tag-based version extraction, multi-targeting, `--skip-duplicate` |

The first example shows a complete desktop app delivery pipeline: build once, test everywhere, pack per-platform, distribute via GitHub Releases. The second shows a library-focused workflow where versioning and NuGet metadata are the primary concerns — a simpler matrix (single platform) but richer metadata.

## See Also

- [045 — CI/CD for Avalonia](045-cicd-for-avalonia.md)
- [045V — Verbose Companion](045-cicd-for-avalonia-verbose.md)
- [050 — Auto-Updater with Velopack](050-auto-updater.md)
- [039 — NativeAOT and Trimming](039-nativeaot-trimming.md)
- [042 — Multi-Targeting](042-multi-targeting-desktop-browser-mobile.md)
