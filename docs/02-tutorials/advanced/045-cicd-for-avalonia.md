---
tier: advanced
topic: deployment and automation
estimated: 20 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 045 -- CI/CD for Avalonia Applications

**What you'll learn:** Set up GitHub Actions for building, testing, and packaging an Avalonia application on Windows, Linux, and macOS with NuGet publishing and cross-platform artifact distribution.

**Prerequisites:** [039 -- NativeAOT and Trimming](039-nativeaot-trimming.md), [042 -- Multi-targeting](042-multi-targeting-desktop-browser-mobile.md)

---

## 1. Workflow structure

A typical CI/CD pipeline for Avalonia has these jobs:

| Job | Runs on | Purpose |
|---|---|---|
| `build` | ubuntu-latest | Restore, build, lint, run headless tests |
| `package-win` | windows-latest | Publish Windows self-contained / MSI / ZIP |
| `package-linux` | ubuntu-latest | Publish Linux AppImage / tar.gz / deb |
| `package-macos` | macos-latest | Publish macOS .app bundle / DMG |
| `publish-nuget` | ubuntu-latest | Push NuGet packages (library projects only) |

## 2. Minimal build-and-test workflow

```yaml
name: Build and Test

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test (headless)
        run: dotnet test --configuration Release --no-build \
          --environment AVALONIA_TEST_HEADLESS=true
```

## 3. Cross-platform packaging

```yaml
jobs:
  package-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet publish src/MyApp.Desktop/MyApp.Desktop.csproj
          --configuration Release
          --runtime win-x64
          --self-contained true
          -p:PublishSingleFile=true
          -p:IncludeNativeLibrariesForSelfExtract=true
          -o publish/win-x64
      - uses: actions/upload-artifact@v4
        with:
          name: myapp-win-x64
          path: publish/win-x64/

  package-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet publish src/MyApp.Desktop/MyApp.Desktop.csproj
          --configuration Release
          --runtime linux-x64
          --self-contained true
          -p:PublishSingleFile=true
          -o publish/linux-x64
      - uses: actions/upload-artifact@v4
        with:
          name: myapp-linux-x64
          path: publish/linux-x64/

  package-macos:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet publish src/MyApp.Desktop/MyApp.Desktop.csproj
          --configuration Release
          --runtime osx-x64
          --self-contained true
          -p:PublishSingleFile=true
          -o publish/osx-x64
      - uses: actions/upload-artifact@v4
        with:
          name: myapp-macos-x64
          path: publish/osx-x64/
```

## 4. NativeAOT publish for smaller binaries

Add a publish profile or use `-p` flags:

```yaml
- run: dotnet publish src/MyApp.Desktop/MyApp.Desktop.csproj
    --configuration Release
    --runtime win-x64
    --self-contained true
    -p:PublishAot=true
    -p:IlcInstructionSet=avx2
    -o publish/win-x64-aot
```

NativeAOT produces a single `.exe` with no dependencies. See [tutorial 039](039-nativeaot-trimming.md) for required configuration.

## 5. NuGet publishing (libraries)

For reusable Avalonia controls or libraries:

```yaml
jobs:
  publish-nuget:
    if: startsWith(github.ref, 'refs/tags/v')
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet pack src/MyLibrary/MyLibrary.csproj
          --configuration Release
          -o nupkg
      - run: dotnet nuget push nupkg/*.nupkg
          --source https://api.nuget.org/v3/index.json
          --api-key ${{ secrets.NUGET_API_KEY }}
```

The `if` condition restricts publishing to tags matching `v*` (e.g., `v1.2.3`).

## 6. Headless testing in CI

Avalonia headless testing requires no display server. On Linux the GitHub runner already has a virtual framebuffer, so most tests work without Xvfb. If needed:

```yaml
- name: Start virtual framebuffer
  if: runner.os == 'Linux'
  run: |
    sudo apt-get install -y xvfb
    Xvfb :99 -screen 0 1920x1080x24 &
    echo "DISPLAY=:99" >> $GITHUB_ENV
```

See [tutorial 038](../intermediate/038-headless-testing.md) for the test project setup.

## 7. Code signing (Windows)

```yaml
- name: Sign executable
  shell: pwsh
  run: |
    & "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
      sign /fd SHA256
      /f ${{ secrets.CODE_SIGNING_PFX }}
      /p ${{ secrets.CODE_SIGNING_PASSWORD }}
      /tr http://timestamp.digicert.com
      /td SHA256
      publish/win-x64/MyApp.exe
```

Store the PFX as a base64-encoded GitHub secret and decode at runtime:

```yaml
- name: Decode certificate
  shell: pwsh
  run: |
    $bytes = [Convert]::FromBase64String("${{ secrets.PFX_BASE64 }}")
    [IO.File]::WriteAllBytes("cert.pfx", $bytes)
```

## 8. Release workflow with GitHub Releases

```yaml
name: Release

on:
  push:
    tags: ['v*']

jobs:
  build-and-release:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - run: dotnet restore
      - run: dotnet build --configuration Release --no-restore
      - run: dotnet test --configuration Release --no-build

      - run: dotnet publish --configuration Release
          --runtime win-x64 --self-contained true
          -p:PublishSingleFile=true -o release/win-x64
      - run: dotnet publish --configuration Release
          --runtime linux-x64 --self-contained true
          -p:PublishSingleFile=true -o release/linux-x64
      - run: dotnet publish --configuration Release
          --runtime osx-x64 --self-contained true
          -p:PublishSingleFile=true -o release/osx-x64

      - name: Create archives
        shell: pwsh
        run: |
          Compress-Archive -Path release/win-x64/* -DestinationPath MyApp-win-x64.zip
          tar czf MyApp-linux-x64.tar.gz -C release/linux-x64 .
          tar czf MyApp-macos-x64.tar.gz -C release/osx-x64 .

      - name: Create Release
        uses: softprops/action-gh-release@v2
        with:
          files: |
            MyApp-win-x64.zip
            MyApp-linux-x64.tar.gz
            MyApp-macos-x64.tar.gz
          generate_release_notes: true
```

## 9. Matrix strategy for DRY workflows

```yaml
jobs:
  publish:
    if: startsWith(github.ref, 'refs/tags/v')
    strategy:
      matrix:
        include:
          - os: windows-latest
            rid: win-x64
            ext: .zip
          - os: ubuntu-latest
            rid: linux-x64
            ext: .tar.gz
          - os: macos-latest
            rid: osx-x64
            ext: .tar.gz
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - run: dotnet publish --configuration Release
          --runtime ${{ matrix.rid }}
          --self-contained true -p:PublishSingleFile=true
          -o release/${{ matrix.rid }}

      - name: Create archive
        shell: pwsh
        run: |
          if ("${{ matrix.ext }}" -eq ".zip") {
            Compress-Archive -Path release/${{ matrix.rid }}/* -DestinationPath MyApp-${{ matrix.rid }}${{ matrix.ext }}
          } else {
            tar czf MyApp-${{ matrix.rid }}${{ matrix.ext }} -C release/${{ matrix.rid }} .
          }
```

## Key takeaways

- Use a matrix build strategy to avoid repeating publish steps per platform
- Headless tests (`--environment AVALONIA_TEST_HEADLESS=true`) run on any runner without Xvfb
- Trigger NuGet publish on version tags (`refs/tags/v*`) only
- NativeAOT publishing with `PublishAot=true` produces the smallest single-file output
- Store secrets (NuGet API key, signing cert) in GitHub Secrets, not in source
- Use `actions/upload-artifact` for per-commit build outputs and `action-gh-release` for tagged releases
- macOS signing / notarization requires additional steps via `apple-codesign` action

---

## See Also

- [039 -- NativeAOT and Trimming](039-nativeaot-trimming.md)
- [042 -- Multi-targeting](042-multi-targeting-desktop-browser-mobile.md)
- [038 -- Headless Testing](038-headless-testing.md)
- [032 -- Dependency Injection for MVVM](032-mvvm-di-wiring.md)
- [GitHub Actions docs](https://docs.github.com/en/actions)
- [045V -- CI/CD for Avalonia Applications (verbose companion)](045-cicd-for-avalonia-verbose.md)
- [045X -- CI/CD for Avalonia (examples)](045-cicd-for-avalonia-examples.md)
