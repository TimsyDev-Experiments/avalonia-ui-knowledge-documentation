# Verbose Companion Creation Skill

**Purpose:** Create in-depth companion versions of existing tutorials that explain the "why" and "what does what" — without modifying or replacing the original concise version.

## Trigger Conditions

Run this skill when:
- A reader reports that an existing tutorial has too little explanation
- A tutorial is code-block-heavy with minimal prose between sections
- The original targets quick reference but a deeper treatment is warranted
- Cross-references in the original are broken or point to the wrong number

## File Convention

### Naming

`NNN-topic-verbose.md` — same number prefix as the original, append `-verbose`.

Example: `009-data-templates-basics.md` → `009-data-templates-basics-verbose.md`

### Location

Same directory as the original. Companion lives alongside, not in a separate folder.

### Front-matter

```yaml
---
tier: basics|intermediate|advanced
topic: <same as original>
estimated: 20-30 min         # verbose versions are longer
researched: YYYY-MM-DD
avalonia-version: 12.0.4
companion-to: NNN-topic.md   # points to the original
---
```

Add the `companion-to` field so tooling and readers can navigate between the two.

### Title

`NNNV — Original Title: An In-Depth Companion`

Example: `009V — Data Templates: An In-Depth Companion`

---

## Workflow

### Phase 1: Read and Analyze the Original

1. Read the original tutorial in full.
2. Identify sections where code is presented with minimal or no explanation.
3. Note any cross-references that are broken (wrong numbers, dead links, `file:///` paths).
4. Identify what the reader would need to know to truly *understand* rather than just *copy*.
5. Run `research-skill.md` if more API context is needed.

Output: a list of gaps — "section X has code with no explanation of what DataTemplate actually is", "reference Y links to a file:// path that won't work for anyone else", "link says 014 but targets 015".

### Phase 2: Research via Available Tools

Use every tool at your disposal before writing:

1. **Avalonia skills** — load the relevant specialist skill(s) for the topic:
   - `avalonia-bindings-and-xaml` for template, binding, and XAML topics
   - `avalonia-views-and-templating` for template, view-locator, and tree topics
   - `avalonia-styling-and-resources` for styling and resource topics
   - `avalonia-controls-and-windowing` for control-specific topics
   - etc.

2. **Plugin reference docs** — read the relevant reference from `development-plugin-for-avalonia/references/` via the skills' start-with list.

3. **Avalonia official docs** — use `avalonia-docs_search_avalonia_docs` and `avalonia-docs_lookup_avalonia_api` to verify API names, signatures, and behavior for Avalonia 12.0.4.

4. **Community context** — search the web for community discussions about common mistakes, pitfalls, or patterns for this topic. Use `websearch` with targeted queries.

5. **Existing code** — check the DemoApp or sample code for real usage examples.

### Phase 3: Draft the Companion

Structure each section of the companion to cover:

| Element | Purpose |
|---|---|
| **What this is** | Define the concept in plain terms before showing code |
| **Why it exists** | Explain the design decision or problem it solves |
| **What each part does** | Walk through the code attribute by attribute, line by line |
| **How it works internally** | Describe the mechanism (not just the syntax) |
| **Common mistakes** | What goes wrong and why |
| **When to use this vs. alternatives** | Decision guidance |

Tone rules (from `documentation-skill.md`):
- Technical, direct, neutral
- No AI-typical phrases ("unlock the power", "delve into", "let's dive in")
- Imperative voice ("Set x:DataType" not "You should set x:DataType")
- Every paragraph should answer either "why" or "what does this do"

### Phase 4: Fix Issues Found in the Original

Any broken links, wrong cross-reference numbers, or outdated API usage found in the original must be fixed. The companion does not replace the original — both should be correct.

Common fixes:
- `file:///C:/Users/...` paths → proper relative paths or official docs URLs
- Wrong tutorial numbers in "See Also" links → correct numbers
- Deprecated API usage → current Avalonia 12 API
- Missing `x:DataType` → add compiled binding annotations

### Phase 5: Cross-Link Both Directions

Add a `companion-to` field in the companion's front-matter. Add a link from the original's "See Also" section pointing to the companion:

```markdown
- [NNNV — Original Topic (verbose companion)](NNN-topic-verbose.md)
```

### Phase 6: Register in Index and Viewer

1. Add the companion to `docs/00-index.md` indented under the original entry:

```markdown
N. [NNN — Original Title](02-tutorials/.../NNN-topic.md)
   ⌞ [NNNV — Original Title (verbose companion)](02-tutorials/.../NNN-topic-verbose.md)
```

2. Add an entry to `docs/viewer.html` in the `DOC_INDEX` array near the original entry.

### Phase 7: Audit the Companion

Use `audit-skill.md` plus these additional checks:

#### Explanation Density
- [ ] Does every code block have preceding explanatory text?
- [ ] Is the "why" explained for each major code element?
- [ ] Are there at least 2–3 sentences of prose between code blocks?
- [ ] Does the companion cover what the original omitted, not restate it?

#### Technical Accuracy
- [ ] Are all API names verified against Avalonia 12.0.4 docs?
- [ ] Is `x:DataType` used throughout all XAML examples?
- [ ] Are NuGet package names accurate?
- [ ] Do the explanations match how the framework actually works (not how it might be guessed to work)?

#### Cross-Reference Integrity
- [ ] Do all internal links resolve?
- [ ] Are any `file:///` paths present? (They must not be — replace with relative paths.)
- [ ] Do "See Also" links use the correct tutorial numbers?

#### Original Fixes
- [ ] Was the original checked for broken links?
- [ ] Were any reference number mismatches corrected?
- [ ] Was the original's "See Also" updated to link to the companion?

---

## Example

Given an original with:

```xml
<ItemsControl ItemsSource="{Binding Items}">
  <ItemsControl.ItemTemplate>
    <DataTemplate x:DataType="models:TodoItem">
      <CheckBox Content="{Binding Title}" IsChecked="{Binding IsDone}" />
    </DataTemplate>
  </ItemsControl.ItemTemplate>
</ItemsControl>
```

The companion would explain:

- `ItemsControl` vs `ListBox` — when to use each and why `ItemsControl` has no selection
- `ItemTemplate` vs `DataTemplates` collection — two different mechanisms, different resolution order
- What `x:DataType` actually does (compiled binding validation, not runtime type matching)
- What happens when Avalonia resolves the template (the tree walk, Match/Build contract)
- Why the `CheckBox` has two-way binding on `IsChecked` but one-way on `Content`
- What goes wrong if `x:DataType` is missing (silent reflection fallback)

---

## Relationship to Other Skills

| Skill | Role |
|---|---|
| `documentation-skill.md` | Base creation workflow and tone enforcement |
| `audit-skill.md` | Post-creation audit checks |
| `research-skill.md` | API and version research |
| This skill | Verbose companion specifics |
