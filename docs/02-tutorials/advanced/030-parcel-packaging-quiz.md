---
tier: advanced
topic: packaging
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 030-parcel-packaging.md
---

# Quiz — Packaging and Distribution with Parcel

```quiz
Q: What is the correct command to create a Parcel project file for an Avalonia application?
A. parcel init --project DemoApp.csproj || The flag is -p (short) not --project — the correct syntax is parcel init -p DemoApp.csproj.
B. parcel new DemoApp.parcel || There is no new command — use init with the -p flag pointing to the .csproj.
C. parcel init -p DemoApp.csproj (correct) || parcel init -p <csproj> creates a .parcel file in the output directory based on the project metadata.
D. dotnet new parcel || There is no dotnet new template for Parcel — it is a global tool command (parcel init).
Explanation: The parcel init -p command takes the .csproj path and generates a .parcel project file with default packaging configuration.
```

```quiz
Q: Which command packages an Avalonia app as a Windows NSIS installer?
A. parcel pack -p DemoApp.parcel -r win-x64 -t nsis (correct) || The -t nsis flag targets the NSIS installer format for Windows.
B. parcel pack -p DemoApp.parcel -r windows -t installer || There is no -r windows — use specific RIDs like win-x64. The -t value is nsis, not installer.
C. parcel build -p DemoApp.parcel -r win-x64 --installer || The command is parcel pack, not parcel build — and --installer is not the correct flag.
D. parcel pack -p DemoApp.parcel -r win-x64 -t exe || The correct target type for Windows installer is nsis, not exe.
Explanation: parcel pack with -r win-x64 -t nsis produces a MyApp-Setup-1.0.0.exe NSIS installer.
```

```quiz
Q: How do you configure Azure Trusted Signing for Windows code signing in a .parcel file?
A. Add <AzureSigning>true</AzureSigning> to the PropertyGroup || There is no AzureSigning property — the correct elements are TrustedSigningEndpoint, TrustedSigningCertificateProfile, and TrustedSigningAccountName.
B. Set the TrustedSigningEndpoint, TrustedSigningCertificateProfile, and TrustedSigningAccountName properties in the .parcel file (correct) || These three properties configure Azure Trusted Signing, and --sign enables it at pack time.
C. Provide the signing certificate via --cert and --password CLI flags || Azure Trusted Signing does not use raw certificates — it uses endpoint/profile/account configuration in the .parcel file.
D. Configure signing in the .csproj file with <PackageSigning> properties || Parcel signing config goes in the .parcel file, not the .csproj.
Explanation: The .parcel file holds TrustedSigningEndpoint, TrustedSigningCertificateProfile, and TrustedSigningAccountName; run parcel pack with --sign.
```

```quiz
Q: Which macOS packaging format does Parcel produce, and what is a key compatibility requirement?
A. .app bundle with -t app — set IncludeNativeLibrariesForSelfExtract to false || The format is dmg, not app, but the IncludeNativeLibrariesForSelfExtract advice is correct for macOS.
B. .dmg with -t dmg — set IncludeNativeLibrariesForSelfExtract to false (correct) || Parcel produces .dmg files for macOS, and IncludeNativeLibrariesForSelfExtract must be false for compatibility.
C. .pkg with -t pkg — set MinimumOSVersion to 11.0 || Parcel uses dmg, not pkg — though MinimumOSVersion is a valid macOS property in the .parcel file.
D. .appimage with -t appimage — no special config needed || AppImage is a Linux format, not macOS. macOS uses dmg.
Explanation: Parcel packages macOS apps as .dmg files, and IncludeNativeLibrariesForSelfExtract must be false for macOS compatibility.
```

```quiz
Q: In a CI pipeline, what is the recommended strategy for building cross-platform packages with Parcel?
A. Build each RID sequentially on a single Windows agent to ensure consistency || Building sequentially is slow — Parcel supports parallel builds across agents.
B. Use a matrix strategy with parallel jobs for each target RID (win-x64, osx-arm64, linux-x64) (correct) || A GitHub Actions matrix runs each RID in parallel, producing all platform packages efficiently.
C. Build on a macOS agent only and cross-compile for all platforms || Cross-compilation is not reliably supported — build each platform on matching agents via matrix.
D. Use a single multi-target command: parcel pack -p DemoApp.parcel --all-platforms || There is no --all-platforms flag — specify per-platform RIDs via matrix or separate commands.
Explanation: A CI matrix strategy (win-x64, osx-arm64, linux-x64) builds each platform in parallel on compatible agents.
```
