---
tier: basics|intermediate|advanced
topic: 
estimated: 5-10 min
researched: YYYY-MM-DD
avalonia-version: 12.0.4
---

# Tutorial Title

**What you'll learn:** One-sentence description of the skill this tutorial teaches.

**Prerequisites:** What the reader should already know (or links to prerequisite tutorials).

---

## Step 1: Brief Actionable Heading

1-3 sentences explaining what this step accomplishes.

```xml
<!-- File: Views/MyView.axaml -->
<Button Content="Click Me"
        Command="{Binding MyCommand}" />
```

Explain the key parts of the code — what each attribute does, why it's written this way.

> **Tip:** A helpful observation about the pattern.

---

## Step 2: Next Action

Continue with the same pattern — code first, then concise explanation.

```csharp
// File: ViewModels/MyViewModel.cs
public partial class MyViewModel : ObservableObject
{
    [RelayCommand]
    private void DoSomething()
    {
        // Implementation
    }
}
```

---

## Complete Example

A compact, copy-pasteable block showing everything working together.

---

## Key Takeaways

- Bullet 1: What the reader should remember
- Bullet 2: The main pattern to reuse

---

## See Also

- [Related Tutorial](link/to/tutorial)
- [Avalonia Docs: Official Topic](https://docs.avaloniaui.net/...)
