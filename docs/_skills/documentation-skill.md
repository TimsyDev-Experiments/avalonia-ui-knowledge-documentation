# Documentation Skill — Standardized Creation Workflow

**Purpose:** Consistent, high-quality documentation creation with built-in style guidance.

## Workflow

1. **Research Phase**
   - Load `research-skill.md` if uncertain about APIs or versions.
   - Check Avalonia 12.0.4 docs first. Fall back to 11.3.12 references only if the API is absent in 12.
   - Verify NuGet package versions against the official feed.

2. **Draft Phase**
   - Follow the document template in `docs/_assets/templates/`.
   - Write objective-first: what will the reader learn?
   - Use concrete, runnable examples.
   - Prefer XAML with compiled bindings (`x:DataType`).
   - Use CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`).

3. **Tone Enforcement**
   - Read the draft aloud. If it sounds like generic AI prose, rewrite.
   - Target: technical, direct, neutral. Like the Avalonia official docs or Microsoft Learn.
   - Bad: "In this tutorial we will explore the exciting world of..."
   - Good: "This tutorial creates a searchable item list using compiled bindings."

4. **Review Phase**
   - Run `audit-skill.md` checks.
   - Cross-reference related docs for consistency.
   - Add "See also" links.

5. **Finalize Phase**
   - Commit with a descriptive message.
   - If screenshots are pending, note in commit message.

## Document Types

| Type | Template | Location |
|---|---|---|---|
| Mini-tutorial | `tutorial-template.md` | `docs/02-tutorials/{tier}/` |
| Quick reference | `quickref-template.md` | `docs/01-quick-refs/` |
| Pattern guide | `pattern-template.md` | `docs/03-patterns/` |
| Migration guide | `migration-template.md` | `docs/04-migration/` |
| Example walkthrough | `example-template.md` | `docs/02-tutorials/{tier}/` |

## Naming Convention

Files: `NNN-short-descriptive-name.md` (zero-padded 3-digit numbers for order).
