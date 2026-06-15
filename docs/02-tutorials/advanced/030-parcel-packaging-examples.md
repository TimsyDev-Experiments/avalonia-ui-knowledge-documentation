---
tier: advanced
topic: packaging
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 030-parcel-packaging.md
---

# 030X — Packaging and Distribution with Parcel: Real-World Examples

## Scenario 1: Enterprise App with Multi-Platform CI/CD and Code Signing

### Goal

Package a line-of-business Avalonia application for enterprise deployment across Windows (NSIS installer + MSIX), macOS (DMG with notarization), and Linux (DEB + AppImage), signed and distributed through a corporate Artifactory feed.

### Parcel Project File

```xml
<?xml version="1.0" encoding="utf-8"?>
<PackageProject Sdk="Parcel.Sdk">
  <PropertyGroup>
    <DisplayName>Inventory Manager</DisplayName>
    <Description>Enterprise inventory management system</Description>
    <Company>Acme Corp</Company>
    <Version>3.2.1</Version>
    <Copyright>Copyright 2026 Acme Corp</Copyright>
    <Icon>Assets/app-icon.png</Icon>
  </PropertyGroup>

  <!-- Windows: NSIS + MSIX -->
  <PropertyGroup Condition="'$(RuntimeIdentifier)' == 'win-x64'">
    <AppxPackage>true</AppxPackage>
    <InstallPath>ProgramFiles</InstallPath>
    <!-- Azure Trusted Signing via env vars (no secrets in source) -->
    <TrustedSigningEndpoint>env:TRUSTED_SIGNING_ENDPOINT</TrustedSigningEndpoint>
    <TrustedSigningCertificateProfile>env:TRUSTED_SIGNING_PROFILE</TrustedSigningCertificateProfile>
    <TrustedSigningAccountName>env:TRUSTED_SIGNING_ACCOUNT</TrustedSigningAccountName>
  </PropertyGroup>

  <!-- macOS: universal binary -->
  <PropertyGroup Condition="'$(RuntimeIdentifier)' == 'osx-arm64' Or 
                            '$(RuntimeIdentifier)' == 'osx-x64'">
    <BundleName>InventoryManager</BundleName>
    <BundleIdentifier>com.acme.inventorymanager</BundleIdentifier>
    <MinimumOSVersion>12.0</MinimumOSVersion>
  </PropertyGroup>

  <!-- Linux: DEB for apt-based distros -->
  <PropertyGroup Condition="'$(RuntimeIdentifier)' == 'linux-x64'">
    <LinuxPackageName>inventory-manager</LinuxPackageName>
    <LinuxCategory>Office</LinuxCategory>
  </PropertyGroup>
</PackageProject>
```

### GitHub Actions CI Workflow

```yaml
name: Build & Sign

on:
  push:
    tags:
      - 'v*'

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet tool install --global Avalonia.Parcel.CLI
      - run: parcel pack -p InventoryManager.parcel -r win-x64 -t nsis --sign
        env:
          TRUSTED_SIGNING_ENDPOINT: ${{ secrets.TRUSTED_SIGNING_ENDPOINT }}
          TRUSTED_SIGNING_PROFILE: ${{ secrets.TRUSTED_SIGNING_PROFILE }}
          TRUSTED_SIGNING_ACCOUNT: ${{ secrets.TRUSTED_SIGNING_ACCOUNT }}
      - run: parcel pack -p InventoryManager.parcel -r win-x64 -t zip
      - uses: actions/upload-artifact@v4
        with:
          name: windows-installer
          path: bin/parcel/win-x64/*.exe
      - uses: actions/upload-artifact@v4
        with:
          name: windows-portable
          path: bin/parcel/win-x64/*.zip

  build-macos:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet tool install --global Avalonia.Parcel.CLI
      - run: |
          echo "${{ secrets.MAC_P12_CERT }}" | base64 --decode > cert.p12
          export P12_PASSWORD="${{ secrets.MAC_P12_PASSWORD }}"
          parcel setup-apple-sign -p InventoryManager.parcel \
            --p12-cert cert.p12 --p12-pass-env P12_PASSWORD
          parcel setup-apple-notary -p InventoryManager.parcel \
            --apple-id "${{ secrets.APPLE_ID }}" \
            --app-pass-env APP_SPECIFIC_PASSWORD \
            --team-id "${{ secrets.APPLE_TEAM_ID }}"
        env:
          APP_SPECIFIC_PASSWORD: ${{ secrets.APP_SPECIFIC_PASSWORD }}
      - run: parcel pack -p InventoryManager.parcel -r osx-arm64 -t dmg --sign
      - uses: actions/upload-artifact@v4
        with:
          name: macos-dmg
          path: bin/parcel/osx-arm64/*.dmg

  build-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet tool install --global Avalonia.Parcel.CLI
      - run: sudo apt-get install -y dpkg-dev fakeroot
      - run: parcel pack -p InventoryManager.parcel -r linux-x64 -t deb
      - run: parcel pack -p InventoryManager.parcel -r linux-x64 -t appimage
      - uses: actions/upload-artifact@v4
        with:
          name: linux-deb
          path: bin/parcel/linux-x64/*.deb
      - uses: actions/upload-artifact@v4
        with:
          name: linux-appimage
          path: bin/parcel/linux-x64/*.AppImage
```

### How It Works

1. **Tag-triggered release**: The workflow runs only on `git push` with a version tag (`v3.2.1`). This prevents accidental builds from feature branches.
2. **Three parallel matrix jobs**: Windows, macOS, and Linux build independently. Each job installs Parcel, configures its platform-specific signing, and runs `parcel pack`.
3. **Azure Trusted Signing** (Windows): The endpoint, profile, and account name are read from GitHub secrets via `env:` prefix in the `.parcel` file — no secrets in source code.
4. **Apple codesign + notarization**: The P12 certificate is base64-decoded from a secret, the app-specific password is set as an environment variable. The two `setup-*` commands write signing config into the `.parcel` file. `--sign` on the pack command triggers both signing and notarization submission.
5. **Linux prerequisites**: `dpkg-dev` and `fakeroot` are installed for DEB packaging. AppImage uses AppDirKit bundled with Parcel.

### Design Decisions & Edge Cases

- **Separate macOS notary setup per build**: Apple's notary service rejects submissions if you re-use a stapled ticket across builds. Running `setup-apple-notary` before each pack ensures fresh credentials.
- **Windows MSIX**: Set `AppxPackage=true` for Microsoft Store or sideloading. MSIX requires signing — Azure Trusted Signing handles this. For internal-only distribution, omit MSIX and use only NSIS.
- **Linux DEB arch naming**: Parcel automatically maps `linux-x64` to `amd64`. For `linux-arm64`, the DEB architecture becomes `arm64`.
- **Certificate storage**: The P12 file must be stored securely in CI. The example decodes from a base64 secret — never commit the binary to the repo.

---

## Scenario 2: Open-Source App Published via GitHub Releases

### Goal

Distribute an open-source Avalonia application (Markdown editor) to users on all three platforms using GitHub Releases with per-platform assets and update notifications.

### Parcel Project File

```xml
<?xml version="1.0" encoding="utf-8"?>
<PackageProject Sdk="Parcel.Sdk">
  <PropertyGroup>
    <DisplayName>MarkEdit</DisplayName>
    <Description>A fast cross-platform Markdown editor</Description>
    <Authors>Open Source Contributors</Authors>
    <Version>0.6.2</Version>
    <Icon>Assets/icon.png</Icon>
  </PropertyGroup>

  <!-- No signing for open-source build (community license) -->
  <PropertyGroup Condition="'$(RuntimeIdentifier)' == 'win-x64'">
    <InstallPath>AppData</InstallPath>  <!-- per-user, no admin -->
  </PropertyGroup>

  <PropertyGroup Condition="'$(RuntimeIdentifier)' == 'osx-arm64'">
    <BundleName>MarkEdit</BundleName>
    <BundleIdentifier>io.github.mymarkedit</BundleIdentifier>
    <MinimumOSVersion>11.0</MinimumOSVersion>
  </PropertyGroup>
</PackageProject>
```

### GitHub Actions Release Workflow

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    strategy:
      matrix:
        include:
          - os: windows-latest
            rid: win-x64
            packages: nsis zip
          - os: macos-latest
            rid: osx-arm64
            packages: dmg zip
          - os: ubuntu-latest
            rid: linux-x64
            packages: deb appimage
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet tool install --global Avalonia.Parcel.CLI
      - name: Install Linux packaging tools
        if: runner.os == 'Linux'
        run: sudo apt-get install -y dpkg-dev fakeroot
      - name: Build packages
        run: |
          $packages = "${{ matrix.packages }}" -split ' '
          foreach ($pkg in $packages) {
            parcel pack -p MarkEdit.parcel -r ${{ matrix.rid }} -t $pkg
          }
        shell: pwsh
      - uses: actions/upload-artifact@v4
        with:
          name: ${{ matrix.rid }}
          path: bin/parcel/${{ matrix.rid }}/*

  release:
    needs: build
    runs-on: ubuntu-latest
    permissions:
      contents: write
    steps:
      - uses: actions/download-artifact@v4
      - name: Create Release
        uses: softprops/action-gh-release@v2
        with:
          files: |
            win-x64/*.exe
            win-x64/*.zip
            osx-arm64/*.dmg
            osx-arm64/*.zip
            linux-x64/*.deb
            linux-x64/*.AppImage
          generate_release_notes: true
```

### ViewModel for Update Check

```csharp
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MarkEdit.ViewModels;

public partial class UpdateViewModel : ObservableObject
{
    private readonly HttpClient _http = new();

    [ObservableProperty]
    private string _currentVersion = "0.6.2";

    [ObservableProperty]
    private string? _latestVersion;

    [ObservableProperty]
    private bool _updateAvailable;

    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        try
        {
            var url = "https://api.github.com/repos/myorg/markedit/releases/latest";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("MarkEdit/0.6.2");

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using var json = await response.Content.ReadAsStreamAsync();
            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(json);

            if (release != null && release.TagName != $"v{CurrentVersion}")
            {
                LatestVersion = release.TagName.TrimStart('v');
                UpdateAvailable = true;
            }
        }
        catch
        {
            // Silently fail — update check is non-critical
        }
    }
}

public class GitHubRelease
{
    [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;
}
```

### How It Works

1. **Matrix with explicit include**: Each platform specifies its OS runner, RID, and package types. Windows gets NSIS + ZIP, macOS gets DMG + ZIP, Linux gets DEB + AppImage.
2. **Per-user Windows install**: `InstallPath=AppData` means the NSIS installer does not require administrator privileges — suitable for open-source tools users may install on corporate-locked machines.
3. **Release job**: Downloads all artifacts from the build jobs and attaches them to a GitHub Release created by `softprops/action-gh-release`. The `generate_release_notes` flag auto-generates release notes from commits.
4. **Update check**: `UpdateViewModel` queries the GitHub Releases API for the latest tag. If the tag differs from the local version, `UpdateAvailable` is set to `true`. The View binds to this to show a "Download Update" button.

### Design Decisions & Edge Cases

- **No code signing**: Open-source community builds typically skip code signing (Apple Developer account and Azure Trusted Signing require paid subscriptions). Users see "unverified developer" warnings on macOS and Windows SmartScreen. Mitigate by signing up for Apple's free developer account for macOS notarization eligibility.
- **ZIP as universal fallback**: Every platform produces a portable ZIP. This is the format for users who prefer not to use platform-specific installers.
- **Version tag convention**: Tags must start with `v` (`v0.6.2`) matching the `on.push.tags` filter. The version inside the `.parcel` file drops the `v` prefix (`0.6.2`).
- **Rate limiting**: The GitHub Releases API is rate-limited (60 requests/hour unauthenticated). The update check catches exceptions silently to avoid crashing on rate-limit errors.

### Comparison

| Aspect | Scenario 1: Enterprise | Scenario 2: Open Source |
|---|---|---|
| Installer types | NSIS, MSIX, DMG, DEB, AppImage | NSIS, DMG, DEB, AppImage, ZIP |
| Code signing | Azure Trusted Signing + Apple notarization | None (community license) |
| Windows install path | ProgramFiles (admin) | AppData (per-user, no admin) |
| CI trigger | Git tags only | Git tags only |
| Distribution target | Corporate Artifactory | GitHub Releases |
| Update mechanism | Internal update service | GitHub API release check |
| Certificate handling | Secrets in CI env vars | Not applicable |

## See Also

- [030 — Packaging and Distribution with Parcel](030-parcel-packaging.md)
- [030V — Packaging and Distribution with Parcel (verbose companion)](030-parcel-packaging-verbose.md)
- [001 — Project Setup](../basics/001-project-setup.md)
- [045 — CI/CD for Avalonia](045-cicd-for-avalonia.md)
- [050 — Auto Updater](050-auto-updater.md)
- [Avalonia Parcel Documentation](https://docs.avaloniaui.net/tools/parcel/overview)
- [Azure Trusted Signing documentation](https://learn.microsoft.com/en-us/azure/trusted-signing/)
