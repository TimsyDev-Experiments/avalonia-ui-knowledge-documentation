---
tier: basics
topic: binding modes
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 005-binding-modes.md
---

# 005X — Binding Modes: Real-World Examples

**What you'll build:** A live-preview Markdown editor and a configuration panel with read-only defaults — two scenarios that demonstrate when `TwoWay`, `OneWay`, `OneTime`, and `OneWayToSource` each belong.

**Prerequisites:** [005 — Binding Modes](005-binding-modes.md). The [verbose companion](005-binding-modes-verbose.md) covers the binding pipeline, `Default` mode resolution, and mode interaction with converters.

---

## Example 1: Live-Preview Markdown Editor

**Goal:** Build a split-pane editor where the left side is an editable TextBox (`TwoWay`), the right side shows a rendered preview (`OneWay`), and the initial document title shows once (`OneTime`). A word count display updates from the ViewModel but never pushes back (`OneWay`).

This scenario demonstrates three different binding modes in the same view, each chosen for its data-flow direction.

### ViewModel

```csharp
// ViewModels/MarkdownEditorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class MarkdownEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _documentTitle = "Untitled Document";

    [ObservableProperty]
    private string _markdownText = "# Hello\n\nThis is **bold** and *italic*.\n\n- Item 1\n- Item 2\n- Item 3";

    [ObservableProperty]
    private string _htmlPreview = string.Empty;

    [ObservableProperty]
    private int _wordCount;

    [ObservableProperty]
    private int _charCount;

    // Recalculate preview and counts when markdown changes
    partial void OnMarkdownTextChanged(string value)
    {
        HtmlPreview = RenderToHtml(value);
        WordCount = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        CharCount = value.Length;
    }

    private static string RenderToHtml(string markdown)
    {
        // Simplified renderer — in production use MarkdownSharp or Markdig
        var html = markdown
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("**", "<strong>", StringComparison.Ordinal)
            // Swap first occurrence only — a real parser handles nesting
            .Replace("*", "<em>", StringComparison.Ordinal);

        return $"<div>{html}</div>";
    }
}
```

### View

```xml
<!-- Views/MarkdownEditorView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.MarkdownEditorView"
             x:DataType="vm:MarkdownEditorViewModel">

  <DockPanel Margin="16">
    <!-- Document title — OneTime: set once, never re-read -->
    <TextBlock DockPanel.Dock="Top"
               Text="{Binding DocumentTitle, Mode=OneTime}"
               FontSize="18"
               FontWeight="Bold"
               Margin="0,0,0,12" />

    <!-- Status bar — OneWay: read-only counters -->
    <StackPanel DockPanel.Dock="Bottom"
                Orientation="Horizontal"
                Gap="16"
                Margin="0,8,0,0"
                FontSize="11"
                Foreground="Gray">
      <TextBlock Text="{Binding WordCount, Mode=OneWay, StringFormat='{0} words'}" />
      <TextBlock Text="{Binding CharCount, Mode=OneWay, StringFormat='{0} chars'}" />
    </StackPanel>

    <!-- Split pane: editor + preview -->
    <Grid ColumnDefinitions="*,*" Gap="8">
      <!-- Editor — TwoWay: user edits push to VM and VM changes pull to UI -->
      <TextBox Text="{Binding MarkdownText, Mode=TwoWay}"
               AcceptsReturn="True"
               TextWrapping="Wrap"
               FontFamily="Consolas"
               FontSize="13" />

      <!-- Preview — OneWay: VM renders HTML, UI never pushes back -->
      <Border Grid.Column="1"
              BorderBrush="#ddd"
              BorderThickness="1"
              CornerRadius="4"
              Padding="8">
        <WebView Source="{Binding HtmlPreview, Mode=OneWay}" />
      </Border>
    </Grid>
  </DockPanel>
</UserControl>
```

### How it works

1. **`DocumentTitle` with `Mode=OneTime`:** The title is read once when the ViewModel is assigned. If the ViewModel later changes `DocumentTitle`, the displayed title does not update. This is correct — the document title is set once per document load. The `OneTime` mode avoids an unnecessary `INotifyPropertyChanged` subscription.
2. **`MarkdownText` with `Mode=TwoWay`:** The `TextBox` is editable. When the user types, the ViewModel property updates. When the ViewModel changes the text (e.g., "Load File" command), the `TextBox` updates. This is the standard editable field pattern.
3. **`HtmlPreview` with `Mode=OneWay`:** The `WebView` source is computed from `MarkdownText`. The user never edits the preview directly — the data flows only from ViewModel to View. Using `OneWay` documents this intent and prevents accidental two-way binding.
4. **`WordCount` and `CharCount` with `Mode=OneWay`:** These are computed from `MarkdownText` inside the `OnMarkdownTextChanged` partial method. They are display-only. The `OneWay` mode matches the read-only semantics.
5. The `OnMarkdownTextChanged` partial method is called automatically by the `[ObservableProperty]` source generator after the `MarkdownText` setter stores the value. It updates `HtmlPreview`, `WordCount`, and `CharCount` — all of which raise their own `PropertyChanged` events.

### Design decisions and edge cases

- **`OneTime` for the title is a contract:** It tells future developers: "the title is not expected to change after initialization." If the app later adds a rename feature, `OneTime` must become `OneWay` (or `TwoWay` for inline editing). The binding mode choice is a design signal.
- **`WebView` with raw HTML source:** The `WebView.Source` property expects a `Uri` or `string`. Avalonia's `WebView` control (from `Avalonia.WebView`) supports binding HTML content via `Source`. In Avalonia 12, the `WebView` package is separate — add `Avalonia.WebView` NuGet reference.
- **Computed properties in the partial method:** `HtmlPreview`, `WordCount`, and `CharCount` are regular `[ObservableProperty]` fields. Their values are set inside `OnMarkdownTextChanged`, which runs after the `MarkdownText` setter. This is more direct than using `[NotifyPropertyChangedFor]` on computed getters because the computation (Markdown to HTML) is expensive — it should only run when `MarkdownText` changes, not when any property changes.
- **Edge case — rapid typing:** Each keystroke triggers `OnMarkdownTextChanged`, which runs the Markdown renderer synchronously. For very large documents, debounce the renderer (see 002X example 1 for the debounce pattern).

---

## Example 2: Configuration Panel with Live-Update and Write-Only Fields

**Goal:** Build a settings panel where some values are loaded once from config and never updated (`OneTime`), some push to the ViewModel but never read back (`OneWayToSource` for sensitive fields), and some are fully editable (`TwoWay`).

This mimics a real application settings dialog: the user edits preferences, but certain fields (API keys, secret tokens) are populated once and never displayed after initial load.

### ViewModel

```csharp
// ViewModels/ConfigurationViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class ConfigurationViewModel : ObservableObject
{
    // --- Loaded once from config file, never changes at runtime ---
    [ObservableProperty]
    private string _applicationVersion = "1.0.0";

    [ObservableProperty]
    private string _licenseType = "Community Edition";

    // --- TwoWay editable fields ---
    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private bool _autoSaveEnabled = true;

    [ObservableProperty]
    private int _autoSaveIntervalSeconds = 60;

    // --- OneWayToSource: written from view, not displayed ---
    private string? _apiKey;
    public string? ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    public bool HasApiKey => !string.IsNullOrEmpty(ApiKey);

    [RelayCommand]
    private void Save()
    {
        // Persist all settings
        System.Diagnostics.Debug.WriteLine(
            $"Saving: {DisplayName}, AutoSave={AutoSaveEnabled}, " +
            $"KeySet={!string.IsNullOrEmpty(ApiKey)}");
    }

    partial void OnApiKeyChanged(string? value)
    {
        OnPropertyChanged(nameof(HasApiKey));
    }
}
```

### View

```xml
<!-- Views/ConfigurationView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.ConfigurationView"
             x:DataType="vm:ConfigurationViewModel">

  <StackPanel Spacing="12" Margin="24" MaxWidth="400">

    <!-- Section: System Info — read once, never update -->
    <TextBlock Text="System Information"
               FontWeight="SemiBold" FontSize="14" />
    <Grid ColumnDefinitions="Auto,*" Gap="8" Margin="0,0,0,8">
      <TextBlock Text="Version:" />
      <TextBlock Grid.Column="1"
                 Text="{Binding ApplicationVersion, Mode=OneTime}" />
      <TextBlock Text="License:" />
      <TextBlock Grid.Column="1"
                 Text="{Binding LicenseType, Mode=OneTime}" />
    </Grid>

    <Separator />

    <!-- Section: User Preferences — fully editable -->
    <TextBlock Text="User Preferences"
               FontWeight="SemiBold" FontSize="14" />
    <StackPanel Spacing="8">
      <TextBox Text="{Binding DisplayName, Mode=TwoWay}"
               Watermark="Display name" />
      <CheckBox IsChecked="{Binding AutoSaveEnabled, Mode=TwoWay}"
                Content="Enable auto-save" />
      <StackPanel Orientation="Horizontal" Gap="8"
                  IsVisible="{Binding AutoSaveEnabled}">
        <TextBlock Text="Auto-save interval (seconds):" VerticalAlignment="Center" />
        <TextBox Text="{Binding AutoSaveIntervalSeconds, Mode=TwoWay}"
                 Width="80" />
      </StackPanel>
    </StackPanel>

    <Separator />

    <!-- Section: API Key — write-only, never displays value -->
    <TextBlock Text="API Key"
               FontWeight="SemiBold" FontSize="14" />
    <TextBlock Text="Enter your API key below. The key is sent to the server but not stored locally in plain text."
               FontSize="11"
               Foreground="Gray"
               TextWrapping="Wrap" />
    <PasswordBox Password="{Binding ApiKey, Mode=OneWayToSource}"
                 Watermark="Paste your API key" />
    <TextBlock Text="API key configured"
               Foreground="Green"
               FontSize="11"
               IsVisible="{Binding HasApiKey}" />

    <Separator />

    <Button Content="Save Settings"
            Command="{Binding SaveCommand}"
            HorizontalAlignment="Right" />
  </StackPanel>
</UserControl>
```

### How it works

1. **`ApplicationVersion` and `LicenseType` with `Mode=OneTime`:** These are loaded when the ViewModel is constructed. They never change during the app session. The `OneTime` binding reads them once and never subscribes to `PropertyChanged` — this is the most performant choice for truly static data.
2. **`DisplayName`, `AutoSaveEnabled`, `AutoSaveIntervalSeconds` with `Mode=TwoWay`:** The user edits these freely. Changes propagate from the `TextBox`/`CheckBox` to the ViewModel immediately, and the ViewModel can also update them programmatically (e.g., "Reset to defaults" command).
3. **`ApiKey` with `Mode=OneWayToSource`:** The `PasswordBox.Password` property is a write-only channel. The binding pushes the password to `ApiKey` when the user types, but never reads `ApiKey` back to pre-populate the `PasswordBox`. This is by design — the API key should not sit in a UI control's text buffer after load. The ViewModel's `HasApiKey` property (computed from `ApiKey`) drives the "API key configured" indicator.
4. **`AutoSaveIntervalSeconds` visibility:** The interval field is bound with `IsVisible="{Binding AutoSaveEnabled}"`. When the user unchecks "Enable auto-save", the interval field hides — but the binding mode on `AutoSaveIntervalSeconds` is still `TwoWay`. The value persists in the ViewModel even when the control is hidden.

### Design decisions and edge cases

- **`OneTime` for system info — what if the license changes?** This example assumes the license type does not change at runtime. If the app supports in-session license upgrades, change the mode to `OneWay` so the UI updates when the ViewModel raises `PropertyChanged`.
- **`PasswordBox.Password` is write-only by default.** The control's `Password` property has `DefaultBindingMode = OneWayToSource`. Setting `Mode=OneWayToSource` explicitly documents the intent, even though omitting it would produce the same behavior via the default.
- **`ApiKey` backing field is not `[ObservableProperty]`:** The `ApiKey` property is hand-written because `[ObservableProperty]` would generate a public getter that exposes the key to any code that reads the property. The hand-written version still raises `PropertyChanged` for `HasApiKey` via the `OnApiKeyChanged` partial method.
- **Security consideration:** `OneWayToSource` prevents the `PasswordBox` from displaying the existing key, but the ViewModel still holds the key in memory. For security-sensitive apps, clear `ApiKey` after use or use `SecureString` (with manual marshaling).

---

## What These Examples Demonstrate

| Scenario | Binding modes used | What to learn |
|---|---|---|
| Markdown editor | `TwoWay` (editor), `OneWay` (preview + stats), `OneTime` (title) | Choosing mode by data flow direction, computed properties in partial methods |
| Config panel | `OneTime` (static info), `TwoWay` (editable prefs), `OneWayToSource` (password) | Write-only binding for sensitive fields, mixing modes in one view |

The Markdown editor shows a clean separation: the user edits raw text (`TwoWay`), the app renders preview (`OneWay`), and static metadata loads once (`OneTime`). The config panel adds `OneWayToSource` to the mix for fields that should never be displayed back, demonstrating how binding modes enforce data-flow contracts at the XAML level.

## See Also

- [005 — Binding Modes](005-binding-modes.md)
- [005V — Verbose Companion](005-binding-modes-verbose.md)
- [004 — Value Converters](004-value-converters.md)
- [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)
- [011 — Compiled Bindings in Depth](../intermediate/011-compiled-bindings.md)
- [Avalonia Docs: Data Binding](https://docs.avaloniaui.net/docs/data-binding/data-binding-syntax)
