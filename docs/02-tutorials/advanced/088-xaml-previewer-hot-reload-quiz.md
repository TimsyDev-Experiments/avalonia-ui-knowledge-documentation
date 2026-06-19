---
tier: advanced
topic: tooling
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 088 — XAML Previewer & Hot Reload — Quiz

**Prerequisites:** [088-core](088-xaml-previewer-hot-reload.md)

---

### Q1: What NuGet package is required for Hot Reload and MCP-based preview?

<details>
<summary>Answer</summary>

`AvaloniaUI.DiagnosticsSupport`. Also requires `.WithDeveloperTools()` on the AppBuilder or `this.AttachDeveloperTools()` in debug builds.

</details>

---

### Q2: A developer edits a merged dictionary file (`Colors.axaml`) while the app runs. Which resource references update live, and which ones require a window reload?

<details>
<summary>Answer</summary>

`DynamicResource` references update live. `StaticResource` references require closing and re-opening the window.

</details>

---

### Q3: What XAML namespace prefix and element are used to provide design-time data context for a ViewModel?

<details>
<summary>Answer</summary>

`xmlns:d="http://schemas.microsoft.com/expression/blend/2008"` with `d:DataContext="{d:DesignInstance vm:MyViewModel, IsDesignTimeCreatable=True}"`.

</details>

---

### Q4: True or False: The XAML Previewer executes C# code-behind and event handlers during design-time rendering.

<details>
<summary>Answer</summary>

False. The previewer only compiles and renders static XAML. It cannot execute C# code-behind, event handlers, or animations.

</details>

---

### Q5: A user reports Hot Reload stopped applying changes after several edits. What is the most likely cause and workaround?

<details>
<summary>Answer</summary>

The delta cache may have reached its limit. Restart the app to clear the cache and resume Hot Reload.

</details>

---

### Q6: Which MCP tool connects to the XAML previewer for a specific file without requiring a running app?

<details>
<summary>Answer</summary>

`attach-to-file`. It connects to the XAML previewer process for the specified `.axaml` file.

</details>

---

### Q7: Write XAML that provides design-time data for a `DashboardViewModel` with five sample list items.

<details>
<summary>Answer</summary>

```xml
<Window xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        d:DataContext="{d:DesignInstance vm:DashboardViewModel,
                         IsDesignTimeCreatable=True,
                         CreateList=True}">
```

</details>

---

### Q8: What VS Code / Cursor configuration enables the MCP-based previewer?

<details>
<summary>Answer</summary>

A `.vscode/mcp.json` (or `.cursor/mcp.json`) file with:

```json
{
    "servers": {
        "avalonia_devtools": {
            "type": "stdio",
            "command": "avdt",
            "args": ["mcp"]
        }
    }
}
```

</details>
