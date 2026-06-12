---
topic: migration
estimated: 10-20 min read
researched: YYYY-MM-DD
avalonia-version: 12.0.4
---

# Migration Guide: Source Platform -> Avalonia 12

**What you'll learn:** Step-by-step process for migrating from the source platform to Avalonia 12.0.4.

**Prerequisites:** A working project on the source platform; familiarity with Avalonia basics.

---

## Before You Start

- .NET 8+ required (10 recommended)
- NuGet package versions to target
- Tools you'll need

---

## Phase 1: Project Scaffolding

1. Create or update the project file
2. Add package references
3. Set up the build configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="12.0.4" />
    <!-- ... -->
  </ItemGroup>
</Project>
```

---

## Phase 2: Key Mapping Table

| Source Concept | Avalonia Equivalent | Notes |
|---------------|-------------------|-------|
| | | |

---

## Phase 3: Step-by-Step Migration

### 3.1 Area One

Before (source):
```xml
<!-- Source platform code -->
```

After (Avalonia):
```xml
<!-- Avalonia equivalent -->
```

Key differences: what changed and why.

### 3.2 Area Two

<!-- Continue for each migration area -->

---

## Common Issues

| Symptom | Cause | Fix |
|---------|-------|-----|
| | | |

---

## Verification Checklist

- [ ] Project builds without errors
- [ ] Main window renders correctly
- [ ] Data binding works (compiled bindings preferred)
- [ ] Theme/styling matches expected
- [ ] Platform services (file picker, clipboard) functional
- [ ] Keyboard navigation and focus order preserved

---

## See Also

- [Avalonia 11 -> 12 Migration Guide](../04-migration/avalonia-11-to-12.md)
- [Avalonia Docs: Breaking Changes](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)
- [Related Quick Reference](../01-quick-refs/some-ref.md)
