---
tier: advanced
topic: deployment and automation
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 045-cicd-for-avalonia.md
---

# 045V — CI/CD for Avalonia Applications: An In-Depth Companion

**What you'll learn in this companion:** Not just which YAML blocks to copy, but why each CI/CD stage exists for an Avalonia desktop app — why Linux runners can build Windows targets, why headless testing works without a display, how code signing integrates with CI, and how to structure a matrix strategy that doesn't duplicate publish logic.

**Prerequisites:** [039 — NativeAOT and Trimming](039-nativeaot-trimming.md), [042 — Multi-targeting](042-multi-targeting-desktop-browser-mobile.md)

**You should already have read:** [045 — CI/CD for Avalonia Applications](045-cicd-for-avalonia.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Workflow Structure — Why Three Package Jobs Instead of One Matrix

The tutorial shows separate jobs for each packaging platform even though GitHub Actions supports `matrix` strategies. The reason: **each runner OS can only build for its own target platform by default**, and cross-compilation for native dependencies is fragile.

### What happens inside a build job

When you run `dotnet publish --runtime win-x64` on an `ubuntu-latest` runner:

1. The .NET SDK compiles your C# to IL (this is cross-platform)
2. It resolves managed NuGet dependencies (cross-platform)
3. It tries to acquire native dependencies for `win-x64` — this works for pure managed code
4. For Avalonia with SkiaSharp, the native SkiaSharp runtime for `win-x64` may not be available on the Linux runner

**The practical rule:** Build on the same OS as your target. Windows → windows-latest, Linux → ubuntu-latest, macOS → macos-latest. The tutorial's three separate jobs follow this rule.

### When a single matrix works

A single matrix is safe when your app has **no native dependencies** or when you're publishing **framework-dependent** (not self-contained):

```yaml
strategy:
  matrix:
    os: [windows-latest, ubuntu-latest, macos-latest]
```

But as soon as you use `--self-contained true` with a runtime like `win-x64`, you must restrict the matrix or you'll get runtime-specific errors.

---

## 2. The Build-and-Test Job — What Actually Happens

### dotnet restore vs dotnet build --no-restore

```yaml
- name: Restore
  run: dotnet restore

- name: Build
  run: dotnet build --configuration Release --no-restore
```

`dotnet restore` downloads NuGet packages and caches them on the runner. The `--no-restore` flag on `dotnet build` prevents a redundant restore, saving about 10-15 seconds per job.

**Common mistake:** Omitting `--no-restore` causes `dotnet build` to check NuGet sources even when packages are already cached, adding unnecessary network calls and potential failures if NuGet.org is unreachable.

### Why Release configuration

`--configuration Release` means the compiler optimizes for speed and omits debug information. It also triggers different library behavior — some packages (like Serilog) log differently in Debug vs Release. Always test in Release mode to match production.

### The headless test environment variable

```yaml
--environment AVALONIA_TEST_HEADLESS=true
```

This environment variable tells Avalonia's headless platform integration to activate. It replaces the normal platform initialization (which would fail without a display server or window manager) with a dummy render surface. The tests run the same way — they set up controls, simulate input, assert on state — but nothing renders on screen.

---

## 3. Cross-Platform Packaging — Why Each Flag Exists

### --self-contained true

```yaml
--self-contained true
```

By default, `dotnet publish` produces a **framework-dependent** deployment — it assumes the target machine has the .NET runtime installed. `--self-contained true` bundles the runtime, runtime libraries, and your app into the output.

Why use self-contained for desktop apps: you don't control whether users have .NET installed. A self-contained deployment is larger but eliminates the runtime dependency. The tradeoff is file size (typically 50-80 MB for a basic Avalonia app).

### -p:PublishSingleFile=true

```yaml
-p:PublishSingleFile=true
```

This flag bundles all managed assemblies into a single .exe (or binary) by extracting IL from the assemblies and re-embedding them. Native dependencies (like SkiaSharp's .dll/.so/.dylib) remain external unless you also set:

```yaml
-p:IncludeNativeLibrariesForSelfExtract=true
```

With `IncludeNativeLibrariesForSelfExtract`, even native libraries are embedded. On first run, the app extracts them to a temporary directory. The extraction adds startup time but produces a truly single-file distribution.

### --runtime and -o

`--runtime win-x64` specifies the **portable runtime identifier** (RID). The RID determines which native assets are included. Common RIDs for Avalonia:

| RID | Target |
|---|---|
| `win-x64` | Windows 64-bit |
| `win-arm64` | Windows ARM (Surface Pro X, etc.) |
| `linux-x64` | Linux 64-bit (most distributions) |
| `linux-arm64` | Linux ARM (Raspberry Pi) |
| `osx-x64` | macOS Intel |
| `osx-arm64` | macOS Apple Silicon |

`-o publish/win-x64` sets the output directory. The name convention (`publish/{rid}`) is not required but makes it easy to identify which artifact came from which build.

---

## 4. NativeAOT Publishing — Why It's Different

```yaml
-p:PublishAot=true
-p:IlcInstructionSet=avx2
```

NativeAOT compiles your entire application — including the .NET runtime and GC — into a native executable at build time. There is no JIT, no IL, and no runtime to install.

### The tradeoffs

| Concern | Normal publish | NativeAOT publish |
|---|---|---|
| File size | ~60 MB (self-contained) | ~10-15 MB |
| Startup time | ~500 ms (JIT warmup) | ~50 ms (no JIT) |
| Compatibility | All .NET APIs | Subset (no runtime code gen) |
| Build time | ~30 seconds | ~5-10 minutes |

NativeAOT requires the AOT compiler to be installed (part of the .NET SDK). It also requires the IL linker (Ionica) to be able to prune all code paths that are not statically reachable. See [tutorial 039](039-nativeaot-trimming.md) for the full configuration.

### IlcInstructionSet

`IlcInstructionSet=avx2` tells the AOT compiler to generate code optimized for AVX2 (Intel Haswell and later, AMD Excavator and later). The default is `base` (generic x86-64). Using `avx2` produces faster code but crashes on older CPUs. Choose based on your minimum supported hardware.

---

## 5. NuGet Publishing — The Tag Trigger Pattern

```yaml
if: startsWith(github.ref, 'refs/tags/v')
```

This condition checks whether the git ref (the commit or tag that triggered the workflow) starts with `refs/tags/v`. Typical tag names might be `v1.0.0`, `v2.3.4-beta`, or `v2024.1.0`.

### Why not just push on every commit

Publishing on every commit floods NuGet.org with experimental packages. The tag trigger creates an explicit "ship" action — you create a tag, the CI builds and publishes. This matches the mental model of a release: tagging is the release ceremony.

### The NuGet API key secret

```yaml
--api-key ${{ secrets.NUGET_API_KEY }}
```

Store the API key in GitHub repository secrets (Settings → Secrets and variables → Actions). Never hardcode the key. The key itself is generated from your NuGet.org account and should have push permissions for the package ID.

### dotnet pack -o

```yaml
dotnet pack src/MyLibrary/MyLibrary.csproj --configuration Release -o nupkg
```

`dotnet pack` produces a `.nupkg` file from the project. The `-o nupkg` directory is temporary — it exists only for the duration of the job and is uploaded or published. The `.nupkg` file contains your compiled assemblies, NuGet metadata, and embedded resources (like `.axaml` files from your Avalonia library).

---

## 6. Headless Testing — What the Runner Needs

### The `AVALONIA_TEST_HEADLESS=true` environment variable

Avalonia's test framework checks for this environment variable during startup. When set:

1. `AppBuilder` uses `UseHeadless()` internally
2. The headless platform creates a framebuffer in memory (not on screen)
3. Tests interact with the framework normally — `Window.Show()`, button clicks, text input all work
4. `RenderTargetBitmap` can still capture renders, but nothing appears on a display

### When Xvfb is needed

```yaml
- name: Start virtual framebuffer
  if: runner.os == 'Linux'
  run: |
    sudo apt-get install -y xvfb
    Xvfb :99 -screen 0 1920x1080x24 &
    echo "DISPLAY=:99" >> $GITHUB_ENV
```

On Linux GitHub runners, `AVALONIA_TEST_HEADLESS=true` usually works without Xvfb because Avalonia's headless mode does not require a display server. Xvfb is only needed if:

- You run non-headless integration tests that create actual windows
- You use a test framework that relies on screenshot comparison (which may need SkiaSharp to initialize with a display device)
- You are testing OpenGL/Vulkan interop (headless mode cannot initialize GPU rendering)

As a rule, try without Xvfb first. Add it only if headless tests fail with display-related errors.

---

## 7. Code Signing — The Full Trust Chain

### signtool.exe

```powershell
& "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe"
    sign /fd SHA256
    /f ${{ secrets.CODE_SIGNING_PFX }}
    /p ${{ secrets.CODE_SIGNING_PASSWORD }}
    /tr http://timestamp.digicert.com
    /td SHA256
    publish/win-x64/MyApp.exe
```

`/fd SHA256` sets the file digest algorithm to SHA-256 (required since Windows 10, SHA-1 is deprecated). `/td SHA256` sets the timestamp digest algorithm. Both must match.

`/tr` specifies an RFC 3161 timestamp server. The timestamp proves the signature existed at a specific time, so the certificate can expire and the signature remains valid.

The URL (`http://timestamp.digicert.com`) is DigiCert's public timestamp server. If your certificate is from a different CA, use their timestamp URL.

### Why the PFX needs base64 encoding

```yaml
- name: Decode certificate
  shell: pwsh
  run: |
    $bytes = [Convert]::FromBase64String("${{ secrets.PFX_BASE64 }}")
    [IO.File]::WriteAllBytes("cert.pfx", $bytes)
```

GitHub Secrets cannot store binary content directly — they are text fields. Base64 encoding converts the binary PFX file to an ASCII string that fits in a secret. The decode step reverses this to a temp file that `signtool.exe` can read.

---

## 8. Release Workflow — Why Compress-Archive and tar

```powershell
Compress-Archive -Path release/win-x64/* -DestinationPath MyApp-win-x64.zip
tar czf MyApp-linux-x64.tar.gz -C release/linux-x64 .
tar czf MyApp-macos-x64.tar.gz -C release/osx-x64 .
```

Windows users expect `.zip` files (native support in File Explorer). Linux users expect `.tar.gz`. macOS supports both but `.tar.gz` preserves Unix file permissions.

`Compress-Archive` is PowerShell-native and produces standard ZIP files. `tar czf` is the Unix-standard compression. Both are available on their respective runners without additional tools.

---

## 9. Matrix Strategy — Why the Tutorial Uses include

```yaml
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
```

`matrix.include` is an explicit definition — it creates exactly three jobs with the specified OS, RID, and extension. This is different from `matrix.os: [windows-latest, ubuntu-latest, macos-latest]` which creates a cartesian product (if you also have multiple RIDs or extensions, you get more combinations than you want).

Using `include` keeps control: each job knows its OS and its RID, and the archive command branch only depends on `ext`.

The PowerShell conditional for archive type:

```powershell
if ("${{ matrix.ext }}" -eq ".zip") {
    Compress-Archive -Path release/${{ matrix.rid }}/* -DestinationPath MyApp-${{ matrix.rid }}${{ matrix.ext }}
} else {
    tar czf MyApp-${{ matrix.rid }}${{ matrix.ext }} -C release/${{ matrix.rid }} .
}
```

This single branching point handles all three platforms without duplicating the publish step.

---

## 10. GitHub Actions Caching — Not in the Tutorial but You Need It

For Faster Builds:
```yaml
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

Add this after `actions/checkout` and before `dotnet restore`. It caches downloaded NuGet packages across workflow runs, cutting restore time from ~30s to ~2s.

For NativeAOT builds (tens of minutes) you may also want to cache the AOT compilation artifacts.

---

## 11. macOS Signing and Notarization — What the Tutorial Mentions Briefly

The tutorial notes that macOS signing/notarization requires additional steps. The `apple-codesign` action handles:

1. Importing the Apple Developer certificate into the temporary keychain
2. Running `codesign` with entitlements
3. Notarizing via `xcrun notarytool`
4. Stapling the notarization ticket

A minimal setup:

```yaml
- name: Codesign and notarize
  uses: apple-actions/import-codesign-certs@v3
  with:
    p12-file-base64: ${{ secrets.MACOS_CERT_P12_BASE64 }}
    p12-password: ${{ secrets.MACOS_CERT_PASSWORD }}
```

Then a separate step runs `codesign` on the .app bundle.

---

## Key Takeaways

- Build on the same OS as the target runtime to avoid native-dependency issues
- `--self-contained true` bundles the runtime; `PublishSingleFile=true` bundles assemblies; `IncludeNativeLibrariesForSelfExtract` bundles native libs
- NativeAOT produces smaller, faster binaries at the cost of longer build time and API compatibility constraints
- NuGet publishing should be tag-triggered (`refs/tags/v*`), not commit-triggered
- Headless testing with `AVALONIA_TEST_HEADLESS=true` works on any runner without a display server
- Always base64-encode binary certificates before storing as GitHub secrets
- A matrix strategy with `include` is cleaner than duplicating publish steps per platform
- Cache NuGet packages to reduce restore time; cache AOT artifacts for NativeAOT builds

---

## See Also

- [045 — CI/CD for Avalonia Applications (original)](045-cicd-for-avalonia.md)
- [045X — CI/CD for Avalonia (examples)](045-cicd-for-avalonia-examples.md)
- [039 — NativeAOT and Trimming](039-nativeaot-trimming.md)
- [042 — Multi-targeting](042-multi-targeting-desktop-browser-mobile.md)
- [038 — Headless Testing](038-headless-testing.md)
- [032 — Dependency Injection for MVVM](032-mvvm-di-wiring.md)
- [GitHub Actions docs](https://docs.github.com/en/actions)
- [Velopack CI/CD integration](050-auto-updater.md#9-update-in-cicd)
