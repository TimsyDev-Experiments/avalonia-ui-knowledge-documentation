---
tier: advanced
topic: tooling
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 088 — XAML Previewer & Hot Reload

**What you'll learn:** Using the Avalonia XAML Previewer and Hot Reload to iterate on UI design without restarting your app.

**Prerequisites:** [029 — DevTools](029-avalonia-plus-devtools.md), [085 — Performance & Profiling](085-performance-profiling.md)

---

## 1. XAML Previewer overview

The Avalonia XAML Previewer shows a live preview of your `.axaml` file alongside the source editor. It re-renders on save, letting you see layout and styling changes instantly without building or running the app.

### Supported editors

| Editor | Previewer | Notes |
|---|---|---|
| Visual Studio 2022+ | Built-in (split pane) | Requires Avalonia for Visual Studio extension |
| JetBrains Rider | Plugin-based | AvaloniaRider plugin |
| VS Code / Cursor | MCP-based | Via `attach-to-file` tool |
| CLI / any editor | Manual | Use `avdt mcp attach-to-file` |

---

## 2. Visual Studio Previewer setup

Install the **Avalonia for Visual Studio** extension (Tools → Extensions → Manage Extensions → search "Avalonia").

Once installed, open any `.axaml` file. The default split view shows code on the left and preview on the right.

### Configuration options

| Setting | Options |
|---|---|
| Default Document View | Split, Design, Source |
| Split Orientation | Horizontal (side-by-side), Vertical (top-bottom) |
| Default Zoom | 50%, 75%, 100%, 125%, 150%, 200%, Fit to Width, Fit All |

Access via **Tools → Options → Avalonia**.

### Design-time data context

Bind to a design-time view model without affecting runtime:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        d:DataContext="{d:DesignInstance vm:MainViewModel, IsDesignTimeCreatable=True}">
```

---

## 3. Rider Previewer setup

Install the **AvaloniaRider** plugin from JetBrains Marketplace. Open an `.axaml` file and click the Preview tab at the bottom of the editor pane.

Design-time data context works the same as in Visual Studio.

---

## 4. MCP-based previewer (VS Code, Cursor, CLI)

The DevTools MCP server provides an `attach-to-file` tool that connects to the XAML previewer for a given `.axaml` file.

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

Then in your AI assistant prompt:

```
Attach to the XAML file at src/MyApp/Views/MainWindow.axaml
and show me the current preview.
```

No rebuild needed between edits — just save the file and request another preview.

---

## 5. Hot Reload at runtime

Avalonia supports modifying XAML while the app is running. Changes are applied on save without restarting.

### Requirements

- `AvaloniaUI.DiagnosticsSupport` NuGet package installed
- `.WithDeveloperTools()` on `AppBuilder` or `this.AttachDeveloperTools()` in debug builds
- App launched with F5 (debug) from the IDE

### How it works

1. Edit an `.axaml` file while the app is running.
2. Save the file (Ctrl+S).
3. The running window updates automatically — no rebuild, no restart.

### Limitations

| Scenario | Supported |
|---|---|
| Changing colors, sizes, margins | Yes |
| Adding/removing controls | Yes |
| Changing bindings | Yes |
| Modifying C# code-behind | No (requires rebuild) |
| Adding new XAML files | No (requires rebuild) |
| Changing styles in merged dictionaries | Yes |

---

## 6. Custom preview data with sample data

Use sample data classes decorated with `[DesignTime]` to populate the previewer:

```csharp
[DesignTime]
public class SampleData
{
    public static SampleData Instance => new()
    {
        Items = new List<string> { "Alpha", "Beta", "Gamma" },
        SelectedItem = "Beta"
    };
}
```

```xml
<Window d:DataContext="{x:Static local:SampleData.Instance}"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008">
```

---

## 7. Troubleshooting

| Symptom | Fix |
|---|---|
| Previewer shows "Build required" | Build the project (Ctrl+Shift+B) |
| Preview not updating on save | Check file is saved to disk (auto-save delay in VS) |
| Hot Reload has no effect | Verify `AttachDeveloperTools()` is called in DEBUG only |
| "Avalonia.Metadata" not found | Add `<PackageReference Include="AvaloniaUI.DiagnosticsSupport" />` |
| Previewer shows default window instead of custom | Set `d:DataContext` or verify startup URI |

---

## See also

- [029 — DevTools](029-avalonia-plus-devtools.md) — MCP setup, visual tree inspection
- [085 — Performance & Profiling](085-performance-profiling.md)
- [Avalonia for Visual Studio extension — official docs](https://docs.avaloniaui.net/tools/visual-studio-extension)
- [DevTools MCP — attach-to-file](https://docs.avaloniaui.net/tools/developer-tools/mcp)
