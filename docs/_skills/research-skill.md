# Research Skill — API & Library Version Verification

**Purpose:** Ensure all documentation references correct, current APIs for Avalonia 12.0.4 and associated libraries.

## Sources (in priority order)

1. **Avalonia docs MCP** — primary for 12.0.4 API signatures, breaking changes, migration guidance
2. **Plugin reference corpus** — `references/compendium.md` for 11.3.12 fallback, `references/68-avalonia-12-migration-guide.md` for migration context, `references/69-avalonia-12-breaking-changes-and-new-api-catalog.md` for delta
3. **NuGet.org** — verify current package versions for:
   - `Avalonia` (12.0.x)
   - `Avalonia.Themes.Fluent` / `Avalonia.Themes.Simple`
   - `CommunityToolkit.Mvvm` (8.x)
   - `Microsoft.Extensions.DependencyInjection`
   - `AvaloniaUI.DiagnosticsSupport` (for DevTools)
4. **Avalonia GitHub releases** — for changelogs and patch notes
5. **Web search** — for community patterns if official docs are insufficient

## What to Verify

| Item | Check |
|---|---|
| API existence | Does the type/member exist in 12.0.4? |
| API signature | Are parameter types, counts, and names correct? |
| Namespace | Has it moved? (Common in 11→12 migration) |
| Package version | Is the version pinned in examples current? |
| Breaking change | Was this API modified or removed in 12? |

## When to Fall Back to 11.3.12

Only use 11.3.12 references when:
- The API was removed in 12 with no direct replacement
- A migration pattern is being documented (old → new)
- The user explicitly asks about Avalonia 11 behavior

## Output

Record research findings as a comment block at the top of the document being written:

```
Researched: 2026-06-11
Avalonia version confirmed: 12.0.4
CommunityToolkit.Mvvm: 8.4.0
DI package: Microsoft.Extensions.DependencyInjection 9.x
```
