---
tier: advanced
topic: nativeaot-trimming
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 039-nativeaot-trimming.md
---

# Quiz — NativeAOT & Trimming

```quiz
Q: Which property must be set to true in the .csproj to enable NativeAOT publishing?
A. <PublishTrimmed>true</PublishTrimmed> || Trims the assembly but still uses the JIT — it is not NativeAOT.
B. <PublishAot>true</PublishAot> (correct) || This instructs the .NET SDK to compile IL to a native binary with no JIT.
C. <IlcTrimMetadata>true</IlcTrimMetadata> || Controls metadata trimming only, not the full AOT compilation.
D. <AvaloniaNameGeneratorIsEnabled>true</AvaloniaNameGeneratorIsEnabled> || Enables the XAML name generator but does not trigger AOT compilation.
Explanation: NativeAOT is enabled by setting <PublishAot>true</PublishAot> in the project file, which compiles IL to native code without a JIT.
```

```quiz
Q: Why does `this.FindControl<Button>("myButton")` crash under NativeAOT + trimming?
A. The FindControl API is removed in Avalonia 12 || FindControl still exists but requires reflection which is stripped by trimming.
B. Trimming removes the x:Name-to-control mapping metadata and reflection-based lookup fails (correct) || Without AvaloniaNameGenerator, field access uses reflection at runtime; trimming removes the metadata needed for that reflection.
C. NativeAOT cannot run any XAML-based UI || NativeAOT supports XAML as long as compiled bindings and name generation are enabled.
D. The Button type is not preserved in the compiled binary || Types referenced in XAML are preserved by the XAML compiler; the issue is the lookup mechanism, not the type itself.
Explanation: FindControl relies on reflection to resolve x:Name fields, which fails when trimming strips reflection metadata. The fix is to enable AvaloniaNameGenerator.
```

```quiz
Q: Which binding style is required for trim safety in a NativeAOT-published Avalonia app?
A. ReflectionBinding with Mode=ReflectionBinding || Reflection bindings are stripped by the trimmer and crash at runtime.
B. Compiled bindings with x:DataType specified on each control or panel (correct) || Compiled bindings generate IL code at build time and avoid runtime reflection entirely.
C. Dynamic bindings with string property paths || String-based property paths use reflection internally and fail under trim.
D. OneWay bindings with no explicit data type || Without x:DataType the XAML compiler falls back to reflection-based resolution.
Explanation: Compiled bindings with x:DataType generate IL at compile time, bypassing reflection entirely and remaining safe under trimming and NativeAOT.
```

```quiz
Q: What is the purpose of an rd.xml file in a NativeAOT Avalonia project?
A. It configures the XAML compiler to generate compiled bindings || Compiled bindings are enabled through x:DataType and project properties, not rd.xml.
B. It lists assemblies and types that must be preserved at runtime despite trimming (correct) || Runtime directives tell the ILC linker which assemblies, types, and members to keep for dynamic use.
C. It defines the resource dictionaries used by the Fluent theme || Theme resources are defined in XAML files, not in rd.xml.
D. It specifies which NuGet packages to include in the native binary || NuGet resolution is handled by the SDK; rd.xml controls metadata preservation.
Explanation: rd.xml (runtime directives) preserves assemblies and types that the trimmer would otherwise remove, such as dynamically-loaded theme assemblies.
```

```quiz
Q: Which optimization flag saves approximately 30 MB by removing ICU data from the NativeAOT output?
A. <InvariantGlobalization>true</InvariantGlobalization> || This removes globalization data (~2 MB savings for culture data, but ICU is separate).
B. <OptimizationPreference>Size</OptimizationPreference> || Optimizes the native code for size rather than speed, but does not remove ICU.
C. <InvariantTimezone>true</InvariantTimezone> || This removes timezone data but has minimal effect on ICU data.
D. <InvariantGlobalization>true</InvariantGlobalization> combined with removing ICU data via <IlcInvariantGlobalization> or similar ICU-removal flags (correct) || InvariantGlobalization strips ICU data saving ~30 MB, but disables culture-aware formatting.
Explanation: Setting InvariantGlobalization to true removes ICU data saving ~30 MB, though this disables culture-sensitive formatting and should only be used when locale rendering is fully controlled by the app.
```

```quiz
Q: What happens if Avalonia.Themes.Fluent is not preserved via rd.xml or DynamicallyAccessedMembers during a NativeAOT publish?
A. The app falls back to a built-in default theme automatically || There is no built-in fallback — the theme data is simply missing.
B. The app launches with a blank window because no styles are loaded (correct) || The Fluent theme assembly is trimmed away, so no visual styles are applied, resulting in an empty window.
C. The app throws a compile-time error about the missing assembly || Trimming happens at publish time; the missing theme manifests as a runtime blank window.
D. The app still works but uses a software renderer || Rendering is unaffected; the issue is the absence of style definitions, not a rendering pipeline change.
Explanation: When the Fluent theme assembly is trimmed, no visual styles are applied and the window appears blank, which is the most common trimming issue listed in the tutorial.
```
