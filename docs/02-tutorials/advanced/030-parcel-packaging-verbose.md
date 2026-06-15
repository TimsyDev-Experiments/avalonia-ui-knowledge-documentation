---
tier: advanced
topic: packaging
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 030-parcel-packaging.md
---

# 030V — Packaging and Distribution with Parcel: An In-Depth Companion

This companion explains each packaging target, signing mechanism, and CI integration pattern in depth. Read it alongside [030 — Packaging and Distribution with Parcel](030-parcel-packaging.md).

---

## 1. Install Parcel — What the CLI Does

```bash
dotnet tool install --global Avalonia.Parcel.CLI
```

Parcel.CLI is a .NET global tool. Installing it globally makes `parcel` available system-wide. The tool wraps the `Avalonia.Parcel` NuGet SDK (referenced by the `.parcel` project file). It orchestrates:
1. Restoring NuGet packages for the Parcel SDK.
2. Running `dotnet publish` with the specified runtime identifier.
3. Post-processing the published output into the requested package format (NSIS, DMG, DEB, etc.).

### Why a global tool instead of a NuGet package

Parcel operates on the published output of your application — it needs to run after `dotnet publish`. A global tool can be invoked from any directory and integrated into any CI pipeline. The alternative (a build-time NuGet package) would couple packaging to the build process, making it harder to build on one machine and package on another.

### Version matching

The Parcel CLI version should match the Parcel SDK version referenced by the `.parcel` project file. Mismatches may produce errors about unrecognized SDK elements. Run `parcel --version` to check the CLI version, and inspect the `.parcel` file's `Sdk` attribute for the SDK version.

---

## 2. Create a Parcel Project File — Understanding the SDK Format

```bash
parcel init -p DemoApp.csproj
```

### What `parcel init` does

1. Reads the `.csproj` to extract the assembly name, version, and output type.
2. Creates a `.parcel` file with default metadata (in the same directory as the `.csproj` by default).
3. The output directory can be specified with `--out-dir`.

### The `.parcel` format

A `.parcel` file is a `PackageProject` in the `Parcel.Sdk` MSBuild project system. MSBuild processes it using targets defined in the `Parcel.Sdk` NuGet package. This means:
- You can use MSBuild conditionals (`Condition="'$(RuntimeIdentifier)' == 'win-x64'"`).
- You can reference MSBuild properties from the build environment.
- The SDK is resolved via NuGet, so the `.parcel` file alone is portable — no additional tools needed on the build agent beyond the CLI.

---

## 3. Configure Metadata — What Each Property Controls

### Cross-platform properties

```xml
<DisplayName>My Application</DisplayName>
<Description>A cross-platform Avalonia application</Description>
<Company>My Company</Company>
<Authors>Developer Name</Authors>
<Version>1.0.0</Version>
<Copyright>Copyright 2026</Copyright>
<Icon>Assets/app-icon.png</Icon>
```

| Property | Where it appears |
|---|---|
| `DisplayName` | Start menu (Windows), .app bundle name (macOS), desktop entry (Linux) |
| `Description` | NSIS installer description, DEB control file, DMG metadata |
| `Company` | NSIS publisher, DEB maintainer |
| `Authors` | DEB maintainer, assembly metadata |
| `Version` | All package types; must be a valid semver (e.g., 1.0.0, not 1.0) |
| `Copyright` | File metadata, about dialog |
| `Icon` | Converted to .ico (Windows), .icns (macOS), .png (Linux) automatically |

### Windows-specific properties

```xml
<PropertyGroup Condition="'$(RuntimeIdentifier)' == 'win-x64'">
  <AppxPackage>false</AppxPackage>
  <InstallPath>ProgramFiles</InstallPath>
</PropertyGroup>
```

- `AppxPackage` — set to `true` to produce a `.msix` package for the Microsoft Store. Requires signing. Default: `false`.
- `InstallPath` — `ProgramFiles` installs to `%ProgramFiles%\YourApp`. `AppData` installs to `%LocalAppData%\YourApp` (no admin required). `Custom` lets you specify a path.

### macOS-specific properties

```xml
<PropertyGroup Condition="'$(RuntimeIdentifier)' == 'osx-x64' Or '$(RuntimeIdentifier)' == 'osx-arm64'">
  <BundleName>MyApp</BundleName>
  <BundleIdentifier>com.mycompany.myapp</BundleIdentifier>
  <MinimumOSVersion>11.0</MinimumOSVersion>
</PropertyGroup>
```

- `BundleName` — the `.app` bundle's display name (appears in the menu bar and Dock).
- `BundleIdentifier` — a reverse-DNS name used for code signing, keychain access, and iCloud. Must be unique across all apps on the system.
- `MinimumOSVersion` — the earliest macOS version the app supports. Setting 11.0 targets Big Sur and later (including Apple Silicon).

---

## 4. Build Platform Packages — What Each Format Is

### Windows: NSIS Installer

```bash
parcel pack -p DemoApp.parcel -r win-x64 -t nsis
```

**NSIS** (Nullsoft Scriptable Install System) produces a `.exe` installer. It:
- Extracts files to the target directory (`ProgramFiles` or `AppData`).
- Creates Start Menu shortcuts.
- Adds an entry to **Add/Remove Programs**.
- Can run as admin (for `ProgramFiles` installs) or per-user (for `AppData` installs).

Output: `bin/parcel/win-x64/MyApp-Setup-1.0.0.exe`

### Windows: Portable ZIP

```bash
parcel pack -p DemoApp.parcel -r win-x64 -t zip
```

A self-contained `.zip` archive with no installer. The user unzips and runs the `.exe`. No registry entries, no uninstaller. Suitable for portable app scenarios (USB drives, CI artifacts, internal distribution).

Output: `bin/parcel/win-x64/MyApp-1.0.0-win-x64.zip`

### macOS: DMG

```bash
parcel pack -p DemoApp.parcel -r osx-arm64 -t dmg
```

A `.dmg` disk image containing the `.app` bundle. When opened, it presents the app to the user who drags it to `/Applications`. Contains:
- The `.app` bundle (which includes the executable, libraries, and assets).
- A symbolic link to `/Applications`.
- Optional background image and icon positioning.

Output: `bin/parcel/osx-arm64/MyApp-1.0.0-arm64.dmg`

### Linux: DEB

```bash
parcel pack -p DemoApp.parcel -r linux-x64 -t deb
```

Debian package format (also used by Ubuntu, Mint, and derivatives). The `.deb` package:
- Follows the Debian directory structure (`/usr/bin`, `/usr/lib`, `/usr/share/applications`).
- Generates a `.desktop` file for the application menu.
- Adds the app to the system package database.

Output: `bin/parcel/linux-x64/MyApp_1.0.0_amd64.deb`

### Linux: AppImage

```bash
parcel pack -p DemoApp.parcel -r linux-x64 -t appimage
```

AppImage is a portable, distribution-agnostic format. The `.AppImage` file:
- Contains the entire application filesystem (AppDir).
- Is a self-extracting executable — no install needed.
- Works on any Linux distribution from 2012 onward.
- Integrates with the desktop via `.desktop` file extraction.

Output: `bin/parcel/linux-x64/MyApp-1.0.0-x86_64.AppImage`

---

## 5. Code Signing (Windows) — Azure Trusted Signing

```xml
<PropertyGroup>
  <TrustedSigningEndpoint>https://eus.codesigning.azure.net</TrustedSigningEndpoint>
  <TrustedSigningCertificateProfile>MyProfile</TrustedSigningCertificateProfile>
  <TrustedSigningAccountName>MyAccount</TrustedSigningAccountName>
</PropertyGroup>
```

### Azure Trusted Signing vs. traditional certs

Traditional code signing requires a hardware token or a PFX file stored on disk — a security risk in CI environments. Azure Trusted Signing is a cloud-based signing service:
- The private key never leaves Microsoft'sHSM.
- Signing is performed via a REST API call.
- You control access via Azure IAM roles.
- The certificate is automatically timestamped.

### What gets signed

With `--sign`, Parcel signs:
- The application `.exe` and `.dll` files (Authenticode).
- The MSI/NSIS installer (if applicable).
- Any bundled native DLLs.

### Environment variables

The `TrustedSigningEndpoint`, `TrustedSigningCertificateProfile`, and `TrustedSigningAccountName` values can be set with `env:` prefix to read from environment variables instead of hardcoding in the `.parcel` file:

```xml
<TrustedSigningEndpoint>env:TRUSTED_SIGNING_ENDPOINT</TrustedSigningEndpoint>
```

---

## 6. Code Signing (macOS) — Apple Codesign and Notarization

### Setup commands

```bash
parcel setup-apple-sign -p DemoApp.parcel --p12-cert /path/to/cert.p12 --p12-pass-env P12_PASSWORD

parcel setup-apple-notary -p DemoApp.parcel --apple-id user@example.com --app-pass-env APP_SPECIFIC_PASSWORD --team-id ABC123
```

### `setup-apple-sign` — what it does

1. Reads your `.p12` certificate file (exported from Apple Developer account → Certificates → Developer ID Application).
2. Stores the certificate path and the environment variable name for the password in the `.parcel` file (encrypted at rest).
3. The certificate must be for "Developer ID Application" (not "Apple Development" or "Apple Distribution") for distribution outside the App Store.

### `setup-apple-notary` — what it does

1. Configures the Apple ID, app-specific password, and team ID in the `.parcel` file.
2. During `parcel pack --sign`, it:
   a. Signs all binaries with `codesign`.
   b. Packages the `.app` into a `.dmg`.
   c. Submits the `.dmg` to Apple's notary service.
   d. Staples the notarization ticket to the `.dmg`.
   e. If notarization fails, the build step fails with the notary log.

### App-specific password

Apple requires an **app-specific password** (generated at appleid.apple.com under Security → App-Specific Passwords) — your regular Apple ID password does not work with the notary API.

---

## 7. Automation in CI — How the Matrix Build Works

```yaml
jobs:
  build:
    strategy:
      matrix:
        rid: [win-x64, osx-arm64, linux-x64]
```

### Why a matrix strategy

Each platform's package must be built on that platform (or cross-compiled). The matrix strategy runs three parallel jobs:
- **win-x64** — runs on a Windows runner (`windows-latest`).
- **osx-arm64** — runs on a macOS runner (`macos-latest`).
- **linux-x64** — runs on a Linux runner (`ubuntu-latest`).

### Step order matters

1. `actions/checkout@v4` — checks out the source.
2. `actions/setup-dotnet@v4` — installs the .NET SDK.
3. `dotnet tool install --global Avalonia.Parcel.CLI` — installs Parcel. Run this in the CI script (the tool is not pre-installed on GitHub runners).
4. `parcel pack -p DemoApp.parcel -r ${{ matrix.rid }} -t zip` — builds and packages.
5. `actions/upload-artifact@v4` — uploads the output for later download or release.

### Self-hosted runner considerations

If using self-hosted runners:
- macOS runners must have Xcode command line tools installed (for `codesign`).
- Windows runners must have the Windows SDK (for `signtool` if not using Azure Trusted Signing).
- Linux runners must have `dpkg-deb` and `fakeroot` for DEB packaging, and `appimagetool` for AppImage.

### Cross-platform build caveat

Building for `osx-arm64` on a non-macOS runner is not supported (Apple's linker and codesign tooling are macOS-only). Cross-compilation requires a macOS build agent.

---

## Key Takeaways — Why Each Choice Matters

- **NSIS**: Full Windows integration (Start Menu, Add/Remove Programs). Choose for end-user distribution.
- **ZIP**: Portable, no install. Choose for internal tools, CI artifacts, or USB distribution.
- **DMG**: Standard macOS distribution format. Users expect it.
- **DEB**: Required for Debian/Ubuntu apt-based distribution.
- **AppImage**: Portable, distribution-agnostic Linux. Best for users who don't want to add PPAs.
- **Code signing**: Required for trust (Windows SmartScreen, macOS Gatekeeper).
- **CI matrix**: Ensures each platform is built natively with correct architecture-specific dependencies.

---

## See Also

- [030 — Packaging and Distribution with Parcel (original)](030-parcel-packaging.md)
- [001 — Project Setup](../basics/001-project-setup.md)
- [029 — Using Avalonia DevTools](029-avalonia-plus-devtools.md)
- [Avalonia Parcel Documentation](https://docs.avaloniaui.net/tools/parcel/overview)
- [Azure Trusted Signing documentation](https://learn.microsoft.com/en-us/azure/trusted-signing/)
- [030X — Packaging and Distribution with Parcel (examples)](030-parcel-packaging-examples.md)
