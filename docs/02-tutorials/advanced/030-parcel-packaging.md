---
tier: advanced
topic: packaging
estimated: 30 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 030 -- Packaging and Distribution with Parcel

**What you'll learn:** How to use the Avalonia Parcel tool to package your application for Windows, macOS, and Linux distribution with platform-specific formats.

**Prerequisites:** [001 -- Project Setup](../basics/001-project-setup.md), a completed Avalonia 12 application, and the Avalonia Plus subscription for Parcel access.

---

## 1. Install Parcel

Parcel is included with Avalonia Plus. Install the global tool:

```bash
dotnet tool install --global Avalonia.Parcel.CLI
```

Verify the installation:

```bash
parcel --version
```

## 2. Create a Parcel project file

From your project directory, run:

```bash
parcel init -p DemoApp.csproj
```

This creates a `DemoApp.parcel` file in the output directory. The file defines packaging targets, metadata, and signing configuration.

## 3. Configure metadata

Edit the `.parcel` file to set application metadata:

```xml
<?xml version="1.0" encoding="utf-8"?>
<PackageProject Sdk="Parcel.Sdk">
  <PropertyGroup>
    <DisplayName>My Application</DisplayName>
    <Description>A cross-platform Avalonia application</Description>
    <Company>My Company</Company>
    <Authors>Developer Name</Authors>
    <Version>1.0.0</Version>
    <Copyright>Copyright 2026</Copyright>
    <Icon>Assets/app-icon.png</Icon>
  </PropertyGroup>

  <PropertyGroup Condition="'$(RuntimeIdentifier)' == 'win-x64'">
    <AppxPackage>false</AppxPackage>
    <InstallPath>ProgramFiles</InstallPath>
  </PropertyGroup>

  <PropertyGroup Condition="'$(RuntimeIdentifier)' == 'osx-x64' Or '$(RuntimeIdentifier)' == 'osx-arm64'">
    <BundleName>MyApp</BundleName>
    <BundleIdentifier>com.mycompany.myapp</BundleIdentifier>
    <MinimumOSVersion>11.0</MinimumOSVersion>
  </PropertyGroup>
</PackageProject>
```

## 4. Build platform packages

### Windows (installer)

```bash
parcel pack -p DemoApp.parcel -r win-x64 -t nsis
```

Output: `bin/parcel/win-x64/MyApp-Setup-1.0.0.exe`

### Windows (portable zip)

```bash
parcel pack -p DemoApp.parcel -r win-x64 -t zip
```

Output: `bin/parcel/win-x64/MyApp-1.0.0-win-x64.zip`

### macOS (.dmg)

```bash
parcel pack -p DemoApp.parcel -r osx-arm64 -t dmg
```

Output: `bin/parcel/osx-arm64/MyApp-1.0.0-arm64.dmg`

### Linux (.deb)

```bash
parcel pack -p DemoApp.parcel -r linux-x64 -t deb
```

Output: `bin/parcel/linux-x64/MyApp_1.0.0_amd64.deb`

### Linux (.AppImage)

```bash
parcel pack -p DemoApp.parcel -r linux-x64 -t appimage
```

Output: `bin/parcel/linux-x64/MyApp-1.0.0-x86_64.AppImage`

## 5. Code signing (Windows)

Configure Azure Trusted Signing in the `.parcel` file:

```xml
<PropertyGroup>
  <TrustedSigningEndpoint>https://eus.codesigning.azure.net</TrustedSigningEndpoint>
  <TrustedSigningCertificateProfile>MyProfile</TrustedSigningCertificateProfile>
  <TrustedSigningAccountName>MyAccount</TrustedSigningAccountName>
</PropertyGroup>
```

Run with signing:

```bash
parcel pack -p DemoApp.parcel -r win-x64 -t nsis --sign
```

## 6. Code signing (macOS)

Configure Apple codesign and notarization:

```bash
parcel setup-apple-sign -p DemoApp.parcel --p12-cert /path/to/cert.p12 --p12-pass-env P12_PASSWORD

parcel setup-apple-notary -p DemoApp.parcel --apple-id user@example.com --app-pass-env APP_SPECIFIC_PASSWORD --team-id ABC123
```

Run with signing and notarization:

```bash
parcel pack -p DemoApp.parcel -r osx-arm64 -t dmg --sign
```

## 7. Automation in CI

### GitHub Actions example

```yaml
jobs:
  build:
    strategy:
      matrix:
        rid: [win-x64, osx-arm64, linux-x64]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - run: dotnet tool install --global Avalonia.Parcel.CLI
      - run: parcel pack -p DemoApp.parcel -r ${{ matrix.rid }} -t zip
      - uses: actions/upload-artifact@v4
        with:
          name: DemoApp-${{ matrix.rid }}
          path: bin/parcel/${{ matrix.rid }}/*.zip
```

## Key takeaways

- Parcel produces platform-native packages (NSIS, DMG, DEB, AppImage, ZIP)
- Code signing is configured through the `.parcel` project file
- CI pipelines can build for all platforms in parallel
- The same `.parcel` file works across Windows, macOS, and Linux build agents
- Set `IncludeNativeLibrariesForSelfExtract` to `false` for macOS compatibility
