---
tier: basics|intermediate|advanced
topic:
estimated: 15-25 min
researched: YYYY-MM-DD
avalonia-version: 12.0.4
example-of: NNN-topic.md
---

# NNNX — Topic: Real-World Examples

**What you'll build:** One-sentence describing the two scenarios and what they demonstrate.

**Prerequisites:** [NNN — Original Topic](NNN-topic.md), or the [verbose companion](NNN-topic-verbose.md).

---

## Example 1: [Scenario Name]

**Goal:** One sentence describing what this example achieves.

### The ViewModel

What the ViewModel contains, why these properties, how the data flows.

```csharp
// File: ViewModels/ExampleOneViewModel.cs
public partial class ExampleOneViewModel : ObservableObject
{
    // Properties and commands
}
```

### The View

What the XAML does, how the template is resolved, key decisions.

```xml
<!-- File: Views/ExampleOneView.axaml -->
```

### How It Works

Walk through the mechanism step by step — what Avalonia does when it encounters each template, how it matches types, how bindings resolve.

### Key Points

- Design decision: why this approach over alternatives
- What happens at runtime (template resolution, recycling if applicable)
- Edge cases to watch for

---

## Example 2: [Scenario Name]

**Goal:** Different scenario that exercises a different aspect of the concept.

### The ViewModel

```csharp
```

### The View

```xml
```

### How It Works

---

## What These Examples Demonstrate

Compare the two scenarios — what aspect of the concept does each exercise? What patterns carry across?

## See Also

- [NNN — Original Topic](NNN-topic.md)
- [NNNV — Verbose Companion](NNN-topic-verbose.md)
