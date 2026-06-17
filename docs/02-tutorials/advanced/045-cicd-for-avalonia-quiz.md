---
tier: advanced
topic: cicd
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 045-cicd-for-avalonia.md
---

# Quiz — CI/CD for Avalonia Applications

```quiz
Q: What is the primary benefit of using a matrix strategy in a GitHub Actions publish workflow?
A. It allows running jobs in parallel on different operating systems without duplicating the YAML steps (correct) || A matrix strategy defines OS/RID/ext combinations in a compact include list, generating one job per combination without repeating the step definitions.
B. It reduces the total workflow execution time by caching NuGet packages || Caching is a separate optimization; the matrix strategy focuses on structural deduplication.
C. It enables multi-architecture publishing within a single runner || Each job runs on its own runner; the matrix does not combine architectures into one build.
D. It automatically signs the binaries for each platform || Code signing requires explicit action steps, not matrix configuration.
Explanation: A matrix strategy with include entries for each platform (os, rid, ext) generates separate build jobs without replicating the full step list, keeping the workflow DRY.
```

```quiz
Q: Which environment variable enables Avalonia headless testing in CI without a display server?
A. AVALONIA_HEADLESS_MODE=true || The correct variable name differs.
B. AVALONIA_TEST_HEADLESS=true (correct) || Setting this environment variable tells the Avalonia test framework to use the headless platform, which does not require a display server.
C. DISPLAY=:99 || This is the Xvfb display variable for Linux virtual framebuffers, not the Avalonia headless mode flag.
D. DOTNET_ENVIRONMENT=headless || The .NET environment concept is unrelated to Avalonia's rendering platform selection.
Explanation: AVALONIA_TEST_HEADLESS=true activates the headless test platform, allowing tests to run on any CI runner without Xvfb or a physical display.
```

```quiz
Q: What condition triggers a NuGet publish job to run only when a new version tag is pushed?
A. if: github.ref == 'refs/heads/main' || This triggers on every push to main, not just version tags.
B. if: startsWith(github.ref, 'refs/tags/v') (correct) || This restricts execution to tags starting with v (e.g., v1.2.3), matching the standard semantic versioning tag convention.
C. if: github.event_name == 'release' || This triggers on GitHub Release creation events, which would miss tag-only pushes.
D. if: contains(github.ref, 'nuget') || String matching on arbitrary text is fragile; the tag-based approach is the documented convention.
Explanation: The startsWith(github.ref, 'refs/tags/v') condition ensures NuGet publishing only happens for version tags, preventing accidental publishes on every commit.
```

```quiz
Q: Which Avallonia-specific step is required for code signing on Windows in CI?
A. Set the SignAssembly property in the .csproj || SignAssembly controls assembly strong-naming, not Authenticode signing.
B. Decode a base64-encoded PFX certificate from a GitHub secret and run signtool.exe (correct) || The PFX is stored as a base64 secret, decoded at runtime, and passed to signtool.exe with the SHA256 timestamp server URL.
C. Install the Windows SDK on the runner || The Windows GitHub runner already includes the Windows SDK; the certificate handling is the key step.
D. Use the apple-codesign action || That action is for macOS notarization, not Windows Authenticode signing.
Explanation: Windows code signing requires decoding the PFX certificate from secrets and invoking signtool.exe with the appropriate hash and timestamp parameters.
```

```quiz
Q: Which archive command creates a ZIP of the Windows publish output in a cross-platform release workflow?
A. tar czf MyApp-win-x64.zip -C release/win-x64 . || tar czf creates a .tar.gz, not a ZIP file.
B. Compress-Archive -Path release/win-x64/* -DestinationPath MyApp-win-x64.zip (correct) || Compress-Archive is PowerShell's built-in ZIP creation cmdlet, used in the tutorial for Windows artifacts.
C. zip -r MyApp-win-x64.zip release/win-x64 || The zip command is not available on Windows GitHub runners without additional tooling.
D. 7z a MyApp-win-x64.zip release/win-x64 || 7-Zip is not pre-installed on GitHub Windows runners.
Explanation: The tutorial uses PowerShell's Compress-Archive for Windows ZIP archives because it is available by default on windows-latest runners.
```
