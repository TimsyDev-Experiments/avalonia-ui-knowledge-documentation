---
tier: advanced
topic: build
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 039-nativeaot-trimming.md
---

# 039V — NativeAOT and Trimming: An In-Depth Companion

**Why this exists.** The original tutorial covers enabling `PublishAot`, using the XAML compiler, compiled bindings, and trimming-safe patterns. This companion explains what NativeAOT actually does to your IL, why the AOT compiler cannot resolve reflection calls statically, what the `ILLink` trimmer does differently from `ILC`, why `rd.xml` is the wrong format for NativeAOT, how `AvaloniaNameGenerator` avoids `FindControl`, and why size optimization flags like `InvariantGlobalization` carry real functional trade-offs.

**Read this alongside:** [039 — NativeAOT and Trimming](039-nativeaot-trimming.md)

---

## 1. What NativeAOT actually does

NativeAOT compiles .NET IL to a native binary in two phases:

1. **ILC (IL Compiler):** reads all IL assemblies, performs whole-program analysis, and compiles all `MethodBody` objects to native machine code. No JIT — every method is compiled ahead of time.
2. **The linker (ILLink):** removes types, methods, fields, and assemblies that the analysis determines are unreachable. This is the "trimming" step.

The result is a single native executable with no managed heap metadata for trimmed types, no IL interpreter, and no JIT compiler. The CLR is not required at runtime — the executable is a native PE/ELF/Mach-O binary.

**Why this breaks Avalonia:** Avalonia (and WPF, WinForms) uses reflection extensively:

- `FindControl<T>("name")` — resolves a control by string name.
- `DataTemplate` resolution — at runtime, the framework looks up types by name.
- `Binding` with mode `ReflectionBinding` — reads/writes properties via `PropertyInfo`.
- Converter activation — `IValueConverter` instances are created via `Activator.CreateInstance`.

If the trimmer cannot prove that a type is used at runtime, it is removed. The native binary then throws `MissingMethodException`, `NullReferenceException`, or silently fails (converter returns `UnsetValue`).

---

## 2. `PublishAot` configuration

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <IlcTrimMetadata>true</IlcTrimMetadata>
  <IlcGenerateCompleteTypeMetadata>false</IlcGenerateCompleteTypeMetadata>
  <EventSourceSupport>false</EventSourceSupport>
</PropertyGroup>
```

- **`PublishAot`**: enables NativeAOT publishing. Without this, `dotnet publish` produces a normal .NET IL binary.
- **`IlcTrimMetadata`**: removes metadata (reflection data) for unreferenced types. Reduces binary size but breaks reflection that targets those types. Set to `true` for maximum trimming.
- **`IlcGenerateCompleteTypeMetadata`**: when `false`, ILC does not generate metadata for types not accessed through reflection. Set to `false` for size; set to `true` if you have many reflection-based patterns that are hard to annotate.
- **`EventSourceSupport`**: setting to `false` removes EventSource infrastructure, saving ~1 MB. Avalonia does not use EventSource heavily.

---

## 3. XAML compiler — what `AvaloniaNameGenerator` does

```xml
<PropertyGroup>
  <AvaloniaNameGeneratorIsEnabled>true</AvaloniaNameGeneratorIsEnabled>
</PropertyGroup>
```

`AvaloniaNameGenerator` is a Roslyn source generator that reads the XAML in your project and generates a partial class for each `Window` or `UserControl`. For example, for `MainWindow.axaml` with `<Button x:Name="SubmitButton" />`:

```csharp
// Generated code
partial class MainWindow
{
    private global::Avalonia.Controls.Button SubmitButton
        => this.FindControl<global::Avalonia.Controls.Button>("SubmitButton");
}
```

Without this generator, `x:Name` fields are resolved via `FindControl<T>("name")` in `InitializeComponent()`, which uses reflection (`Type.GetElementType`, string matching). Under NativeAOT, the trimmer removes the string-to-control mapping metadata, and `FindControl` returns `null`.

**Enable `AvaloniaNameGenerator` for any project that targets NativeAOT.** It generates no runtime overhead — the `FindControl` calls are in the generated code, but the field references are compile-time.

---

## 4. Compiled bindings requirement

```xml
<TextBlock Text="{Binding Greeting}"
           x:DataType="viewModels:MainViewModel" />
```

Without `x:DataType`, Avalonia's binding engine evaluates bindings using reflection:

- `DataContext` is `MainViewModel`.
- `Greeting` is resolved by calling `Type.GetProperty("Greeting")`.
- Property change notifications are subscribed via `INotifyPropertyChanged` reflection.

With `x:DataType` (compiled bindings):

- The XAML compiler generates code that calls `MainViewModel.Greeting` directly.
- Property type is known at compile time — converters receive the correct type.
- The trimmer sees that `MainViewModel.Greeting` is accessed and preserves it.

**`ReflectionBinding` mode explicitly disables compiled bindings:**

```xml
<!-- Bad — crashes under NativeAOT -->
<TextBlock Text="{Binding Greeting, Mode=ReflectionBinding}" />
```

This bypasses the compiled binding path entirely. Never use `ReflectionBinding` in a NativeAOT build.

**Default in Avalonia 12:** compiled bindings are enabled project-wide when `Avalonia.NameGenerator` is active. You do not need `x:CompileBindings="True"` unless you explicitly disable it.

---

## 5. `rd.xml` vs `ILLink.Descriptors.xml` — which is correct

The original tutorial uses `rd.xml`:

```xml
<Directives xmlns="urn:standards:metadata:runtime-directives">
  <Application>
    <Assembly Name="Avalonia.Themes.Fluent" Dynamic="Required All" />
    <Assembly Name="DemoApp" Dynamic="Required All" />
    <Type Name="DemoApp.Views.MainWindow" Dynamic="Required All" />
  </Application>
</Directives>
```

**`rd.xml` is the .NET Native / UWP format.** It is **not** the correct format for NativeAOT trimming.

For NativeAOT, use one of:

- **`ILLink.Descriptors.xml`** — preserves types for the linker:
  ```xml
  <linker>
    <assembly fullname="Avalonia.Themes.Fluent" />
    <assembly fullname="DemoApp">
      <type fullname="DemoApp.Views.MainWindow" preserve="all" />
    </assembly>
  </linker>
  ```

- **`TrimmerRootAssembly` / `TrimmerRootDescriptor` MSBuild items**:
  ```xml
  <ItemGroup>
    <TrimmerRootAssembly Include="Avalonia.Themes.Fluent" />
    <TrimmerRootDescriptor Include="ILLink.Descriptors.xml" />
  </ItemGroup>
  ```

Alternatively, use C# attributes:

```csharp
[assembly: AssemblyMetadata("TrimmerRoot", "Avalonia.Themes.Fluent")]
[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MainWindow))]
```

**If you use `rd.xml`**, the NativeAOT linker (`ILLink`) ignores it silently. The assemblies will not be preserved, and trimming will remove them, causing blank windows and missing styles.

---

## 6. `[DynamicallyAccessedMembers]` and `[DynamicDependency]`

These attributes tell the trimmer to preserve members that would otherwise be removed:

```csharp
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class ViewModelBase { }

[DynamicDependency(DynamicallyAccessedMemberTypes.All, "OnPropertyChanged", typeof(MainViewModel))]
```

- `[DynamicallyAccessedMembers]`: applied to a type or parameter. The trimmer preserves all members of the specified kind (properties, methods, constructors) for that type.
- `[DynamicDependency]`: applied to a method or assembly. The trimmer preserves a specific member on a specific type, even if no direct reference exists.

Use these on:

- `IValueConverter` implementations (the trimmer does not see `Activator.CreateInstance` calls).
- `Type.GetType("...")` string references.
- Data types referenced only through `DataTemplate` in XAML.

---

## 7. Size optimization — trade-offs

```xml
<PropertyGroup>
  <InvariantGlobalization>true</InvariantGlobalization>
  <InvariantTimezone>false</InvariantTimezone>
  <DebugType>none</DebugType>
  <OptimizationPreference>Size</OptimizationPreference>
</PropertyGroup>
```

- **`InvariantGlobalization=true`**: removes ICU (International Components for Unicode) data. Saves ~30 MB. Breaks culture-aware formatting: `DateTime.ToString("C")` shows invariant format, `NumberFormatInfo.CurrentCulture.CurrencySymbol` is always `¤`. Only enable if you control all locale rendering in your app (e.g., a kiosk display).
- **`InvariantTimezone=false`**: does not embed timezone data. The app falls back to the OS timezone APIs. Saves ~1 MB. Acceptable for most desktop apps.
- **`DebugType=none`**: strips debug symbols. Saves ~5 MB. No stack traces in crash logs.
- **`OptimizationPreference=Size`**: ILC optimizes for binary size over speed. Can increase method call overhead by 5-10%. Use for distribution; switch to `Speed` for debugging.

---

## 8. Known trimming issues table

| Issue from original | Why it happens | Fix |
|---------------------|----------------|-----|
| Missing `Styles` | Theme assembly trimmed | Preserve with `TrimmerRootAssembly` |
| `FindControl` returns null | String-based control lookup trimmed | Enable `AvaloniaNameGenerator` |
| `IValueConverter` not found | `Activator.CreateInstance` call is invisible to the trimmer | Add `[DynamicDependency]` on the converter type |
| Data template not applied | Data type is removed | Use `x:DataType` with compiled bindings |
| Fluent theme missing | Assembly trimmed | Preserve `Avalonia.Themes.Fluent` assembly |

---

## 9. AOT-safe patterns summary

| Pattern | Safe under NativeAOT? | Alternative |
|---------|----------------------|-------------|
| `x:Name` — `FindControl<T>` | No (without generator) | `AvaloniaNameGenerator` |
| `{Binding Property}` without `x:DataType` | No | Always add `x:DataType` |
| `{Binding Property, Mode=ReflectionBinding}` | No | Remove explicit `Mode` (compiled is default) |
| `Activator.CreateInstance(type)` | No | Use DI registration or `[DynamicDependency]` |
| `Type.GetType("string")` | No | Use `typeof()` or DI mapping |
| `{StaticResource}` | Yes | Safe — resolved at XAML compile time |
| `{DynamicResource}` | Partial | Key must be statically referenced somewhere |
| `IValueConverter` from resource | No | `[DynamicDependency]` on the converter |

---

## Key differences from the original

| Concept | Original says | Why it matters |
|---------|---------------|----------------|
| `rd.xml` format | Presented as correct | `rd.xml` is .NET Native format, ignored by NativeAOT's ILLink. Use `ILLink.Descriptors.xml` or `TrimmerRootAssembly` |
| `AvaloniaNameGeneratorIsEnabled` | Must set explicitly | May be default in Avalonia 12 — check template output |
| `x:CompileBindings="True"` | XML namespace attribute | Not needed in Avalonia 12 — compiled bindings are default |
| Converter activation | Not addressed | `Activator.CreateInstance` is invisible to trimmer — must annotate |

---

## See Also

- [039 — NativeAOT and Trimming](039-nativeaot-trimming.md) — the original tutorial
- [039E — NativeAOT and Trimming (examples)](039-nativeaot-trimming-examples.md)
- [038 — Headless Testing](038-headless-testing.md) — testing AOT-safe bindings
- [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md) — NativeAOT compatibility per platform
- [Avalonia Docs: NativeAOT](https://docs.avaloniaui.net/docs/concepts/native-aot)
- [Microsoft Docs: NativeAOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
