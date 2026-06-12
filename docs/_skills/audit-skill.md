# Audit Skill — Recursive Documentation Review

**Purpose:** Ensure all documentation is accurate, consistent in tone, properly styled, and technically correct. Run after every major addition or rewrite.

## Trigger Conditions

Run this skill when:
- A new tutorial document is completed
- A reference document is substantially rewritten (>30% changed)
- Before committing a batch of new docs
- When transitioning between tutorial tiers (basic → intermediate → advanced)

## Audit Checklist

### Tone & Style
- [ ] Does this read like Microsoft Learn / Avalonia docs — direct, instructional, jargon-aware?
- [ ] Are there any AI-typical phrases ("unlock the power", "delve into", "let's dive in", "in conclusion", "remember that", "it's worth noting")? Remove them.
- [ ] Is the voice imperative and active? ("Create a button" not "You can create a button")
- [ ] Are code blocks prefixed with clear context (file name, location)?
- [ ] Are explanations concise? Cut fluff, keep signal.

### Technical Accuracy
- [ ] Are all API names correct for Avalonia 12.0.4?
- [ ] Are all NuGet package versions current? (Check against NuGet if unsure.)
- [ ] Do all XAML examples use `x:DataType` and compiled bindings?
- [ ] Are CommunityToolkit.Mvvm source generators used where applicable?
- [ ] Do code examples compile? (Flag for sample project testing.)

### Structure
- [ ] Does the doc have a clear learning objective stated at the top?
- [ ] Does it follow the template: objective → code → explanation → key takeaway?
- [ ] Are related docs cross-referenced?
- [ ] Are "next steps" or "see also" links present?

### Screenshots & Visuals
- [ ] Are placeholder "[screenshot]" tags noted for later capture?
- [ ] Is the screenshot feasible? (Can it be captured from a running sample app?)
- [ ] For complex UIs, is a DevTools tree or property inspection helpful?

## Running an Audit

1. Read the document in full.
2. Walk each checklist section.
3. Fix issues inline.
4. If multiple docs were changed, cross-check for consistency.
5. Update this skill if new patterns emerge.

## Archiving Note

When a major rewrite occurs, move the old version to `_archive/` with a date prefix before writing the new version.
