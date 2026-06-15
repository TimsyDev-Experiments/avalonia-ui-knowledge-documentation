---
tier: basics
topic: command binding
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 002-command-binding.md
---

# 002X — Command Binding: Real-World Examples

**What you'll build:** A search-as-you-type filter and a multi-step wizard with async navigation — two scenarios that demonstrate parameterized commands, `CanExecute` dependencies, and async command patterns beyond the basic button click.

**Prerequisites:** [002 — Command Binding](002-command-binding.md). The [verbose companion](002-command-binding-verbose.md) covers the `IRelayCommand` type hierarchy and `CanExecute` re-evaluation mechanics in depth.

---

## Example 1: Search-as-You-Type with Debounce

**Goal:** Filter a list of items as the user types in a search box, with a debounce delay to avoid filtering on every keystroke.

The naive approach binds the `TextBox.Text` directly to a collection filter, which re-filters on every character. For large lists or expensive filters (API calls, regex), this causes visible lag. A command-based approach with a timer gives control over timing.

### ViewModel

```csharp
// ViewModels/SearchViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _statusMessage;

    private CancellationTokenSource? _debounceCts;

    public ObservableCollection<string> AllItems { get; } = new()
    {
        "Apple", "Apricot", "Banana", "Blueberry",
        "Cherry", "Date", "Fig", "Grape",
        "Kiwi", "Lemon", "Mango", "Orange",
    };

    public ObservableCollection<string> FilteredItems { get; } = new();

    public SearchViewModel()
    {
        foreach (var item in AllItems)
            FilteredItems.Add(item);

        SearchCommand.Execute(null);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(300, token);

            if (token.IsCancellationRequested)
                return;

            FilteredItems.Clear();
            var query = SearchText?.Trim() ?? string.Empty;

            if (query.Length == 0)
            {
                foreach (var item in AllItems)
                    FilteredItems.Add(item);
                StatusMessage = $"Showing all {AllItems.Count} items";
            }
            else
            {
                var matches = AllItems
                    .Where(x => x.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (var item in matches)
                    FilteredItems.Add(item);
                StatusMessage = $"{matches.Count} of {AllItems.Count} items match";
            }
        }
        catch (TaskCanceledException)
        {
            // Debounce cancelled — next keystroke restarted the timer
        }
    }
}
```

### View with property-triggered command

```xml
<!-- Views/SearchView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.SearchView"
             x:DataType="vm:SearchViewModel">
  <DockPanel Margin="16">
    <TextBox DockPanel.Dock="Top"
             Text="{Binding SearchText, Mode=TwoWay}"
             Watermark="Type to search..."
             Margin="0,0,0,8">
      <TextBox.Styles>
        <Style Selector="TextBox">
          <Setter Property="(Interaction.Behaviors)">
            <Setter.Value>
              <CommunityToolkitBehaviors:PropertyChangedTrigger
                  Command="{Binding SearchCommand}" />
            </Setter.Value>
          </Setter>
        </Style>
      </TextBox.Styles>
    </TextBox>

    <TextBlock DockPanel.Dock="Top"
               Text="{Binding StatusMessage}"
               FontSize="11"
               Foreground="Gray"
               Margin="0,0,0,4" />

    <ListBox ItemsSource="{Binding FilteredItems}" />
  </DockPanel>
</UserControl>
```

> The `PropertyChangedTrigger` behavior fires the command whenever `SearchText` changes. If you prefer no external dependency, wire the command in code-behind using `TextBox.TextChanged` event and call the ViewModel command directly.

### How it works

1. The `SearchText` property is `[ObservableProperty]` — changing it raises `PropertyChanged`.
2. A `PropertyChangedTrigger` behavior (or code-behind handler) calls `SearchCommand.Execute(null)` on every text change.
3. `SearchAsync` starts a 300 ms delay. If the text changes again before 300 ms elapses, the previous `CancellationTokenSource` is cancelled, and the previous `Task.Delay` throws `TaskCanceledException`. The new keystroke starts a fresh delay.
4. After 300 ms of no typing, `FilteredItems` is rebuilt with the matching items.
5. The `ListBox` is bound to `FilteredItems` — changes propagate automatically because `ObservableCollection<T>` raises collection-changed events.
6. The `StatusMessage` gives the user feedback on how many items match.

### Design decisions and edge cases

- **Debounce at 300 ms:** Short enough to feel responsive, long enough to avoid filtering on every keystroke. Adjust based on your filter cost.
- **`CancellationTokenSource` per invocation:** Each search attempt gets its own token. The previous token is cancelled when a new search starts. This avoids a stale search overwriting results from a newer search (race condition).
- **Case-insensitive search:** `StringComparison.OrdinalIgnoreCase` avoids culture-specific quirks. For non-Latin scripts, consider `StringComparison.CurrentCultureIgnoreCase`.
- **Empty query restores all items:** Clearing the search box shows the full list. The `trim()` call ensures whitespace-only input also restores the list.

---

## Example 2: Multi-Step Wizard with Async Navigation

**Goal:** Build a three-step wizard (personal info → preferences → confirmation) where each step validates before proceeding, and the "Next" command is async and context-aware.

Wizards require: forward/back navigation, per-step validation, and the ability to cancel the entire flow. This example demonstrates parameterized commands and `CanExecute` dependent on the current step.

### Step model

```csharp
// Models/WizardStep.cs
namespace MyApp.Models;

public enum WizardStep
{
    PersonalInfo,
    Preferences,
    Confirmation
}
```

### ViewModel

```csharp
// ViewModels/WizardViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyApp.Models;

namespace MyApp.ViewModels;

public partial class WizardViewModel : ObservableObject
{
    [ObservableProperty]
    private WizardStep _currentStep;

    // Step 1 fields
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    // Step 2 fields
    [ObservableProperty]
    private bool _receiveNewsletter = true;

    [ObservableProperty]
    private string _theme = "System";

    public IReadOnlyList<string> ThemeOptions { get; } =
        new[] { "System", "Light", "Dark" };

    public bool IsOnFirstStep => CurrentStep == WizardStep.PersonalInfo;
    public bool IsOnLastStep => CurrentStep == WizardStep.Confirmation;

    partial void OnCurrentStepChanged(WizardStep value)
    {
        OnPropertyChanged(nameof(IsOnFirstStep));
        OnPropertyChanged(nameof(IsOnLastStep));
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task GoNextAsync()
    {
        if (CurrentStep == WizardStep.PersonalInfo)
        {
            await Task.Delay(200); // simulate async validation call
            if (string.IsNullOrWhiteSpace(Name) || !Email.Contains('@'))
                return;
            CurrentStep = WizardStep.Preferences;
        }
        else if (CurrentStep == WizardStep.Preferences)
        {
            CurrentStep = WizardStep.Confirmation;
        }
    }

    private bool CanGoNext()
    {
        return CurrentStep switch
        {
            WizardStep.PersonalInfo => !string.IsNullOrWhiteSpace(Name) && Email.Contains('@'),
            WizardStep.Preferences => true,
            WizardStep.Confirmation => false,
            _ => false,
        };
    }

    [RelayCommand]
    private void GoBack()
    {
        CurrentStep = CurrentStep switch
        {
            WizardStep.Preferences => WizardStep.PersonalInfo,
            WizardStep.Confirmation => WizardStep.Preferences,
            _ => CurrentStep,
        };
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        await Task.Delay(500); // submit data
        CurrentStep = WizardStep.PersonalInfo; // reset
        Name = string.Empty;
        Email = string.Empty;
    }
}
```

### View with step-conditional visibility

```xml
<!-- Views/WizardView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:models="using:MyApp.Models"
             x:Class="MyApp.Views.WizardView"
             x:DataType="vm:WizardViewModel">
  <StackPanel Margin="24" Spacing="16" MinWidth="360">

    <!-- Step indicator -->
    <StackPanel Orientation="Horizontal" Gap="8" HorizontalAlignment="Center">
      <Border CornerRadius="12" Width="24" Height="24"
              Background="{Binding CurrentStep, Converter={StaticResource StepToColor}, ConverterParameter=PersonalInfo}">
        <TextBlock Text="1" HorizontalAlignment="Center" VerticalAlignment="Center" />
      </Border>
      <Line Stroke="Gray" StrokeThickness="2" Width="32" VerticalAlignment="Center" />
      <Border CornerRadius="12" Width="24" Height="24"
              Background="{Binding CurrentStep, Converter={StaticResource StepToColor}, ConverterParameter=Preferences}">
        <TextBlock Text="2" HorizontalAlignment="Center" VerticalAlignment="Center" />
      </Border>
      <Line Stroke="Gray" StrokeThickness="2" Width="32" VerticalAlignment="Center" />
      <Border CornerRadius="12" Width="24" Height="24"
              Background="{Binding CurrentStep, Converter={StaticResource StepToColor}, ConverterParameter=Confirmation}">
        <TextBlock Text="3" HorizontalAlignment="Center" VerticalAlignment="Center" />
      </Border>
    </StackPanel>

    <!-- Step 1: Personal Info -->
    <StackPanel IsVisible="{Binding IsOnFirstStep}" Spacing="8">
      <TextBlock Text="Name" />
      <TextBox Text="{Binding Name, Mode=TwoWay}" Watermark="Your name" />
      <TextBlock Text="Email" />
      <TextBox Text="{Binding Email, Mode=TwoWay}" Watermark="email@example.com" />
    </StackPanel>

    <!-- Step 2: Preferences -->
    <StackPanel IsVisible="{Binding CurrentStep, Converter={StaticResource StepToVisibility}, ConverterParameter=Preferences}" Spacing="8">
      <CheckBox IsChecked="{Binding ReceiveNewsletter, Mode=TwoWay}"
                Content="Receive newsletter" />
      <TextBlock Text="Theme" />
      <ComboBox ItemsSource="{Binding ThemeOptions}"
                SelectedItem="{Binding Theme, Mode=TwoWay}" />
    </StackPanel>

    <!-- Step 3: Confirmation -->
    <StackPanel IsVisible="{Binding IsOnLastStep}" Spacing="8">
      <TextBlock TextWrapping="Wrap">
        <Run Text="Name: " FontWeight="Bold" />
        <Run Text="{Binding Name}" />
      </TextBlock>
      <TextBlock TextWrapping="Wrap">
        <Run Text="Email: " FontWeight="Bold" />
        <Run Text="{Binding Email}" />
      </TextBlock>
      <TextBlock TextWrapping="Wrap">
        <Run Text="Newsletter: " FontWeight="Bold" />
        <Run Text="{Binding ReceiveNewsletter}" />
      </TextBlock>
    </StackPanel>

    <!-- Navigation buttons -->
    <StackPanel Orientation="Horizontal" Gap="8" HorizontalAlignment="Right">
      <Button Content="Back"
              Command="{Binding GoBackCommand}"
              IsVisible="{Binding IsOnFirstStep, Converter={StaticResource InverseBool}}" />
      <Button Content="Next"
              Command="{Binding GoNextCommand}" />
      <Button Content="Finish"
              Command="{Binding FinishCommand}"
              IsVisible="{Binding IsOnLastStep}" />
    </StackPanel>
  </StackPanel>
</UserControl>
```

### How it works

1. `CurrentStep` is an `[ObservableProperty]` of enum type `WizardStep`. The `partial void OnCurrentStepChanged` raises notifications for `IsOnFirstStep` and `IsOnLastStep` computed properties.
2. `GoNextAsync` is async with a `CanExecute` guard. The `CanGoNext()` method inspects the current step and validates field content — it returns `false` on step 1 if name or email are invalid, `true` on step 2, and `false` on step 3 (where "Finish" takes over).
3. `CanExecute` is re-evaluated automatically when properties change because `[NotifyCanExecuteChangedFor(nameof(GoNextCommand))]` is applied to `Name` and `Email`.
4. `GoBackCommand` has no `CanExecute` — it is always available unless on the first step, where its visibility is toggled via `IsVisible="{Binding IsOnFirstStep, Converter={StaticResource InverseBool}}"`.
5. The step indicator circles use a converter (`StepToColor`) to highlight the active step. This keeps the ViewModel free of UI-only state.

### Design decisions and edge cases

- **Async "Next" with validation:** The `GoNextAsync` method simulates a server-side validation call. The `CanExecute` guard provides *instant* feedback (button disabled), while the async body provides *server-side* validation. Both layers serve different purposes.
- **Step visibility via converters:** Example 1 uses `IsVisible` bound to `IsOnFirstStep` (a bool property). Example 2 also shows the alternative: a converter that takes the step enum and returns visibility. Choose based on whether you want the ViewModel to expose UI-specific bools or keep it clean.
- **Reset on finish:** `FinishCommand` resets all fields and returns to step 1. In a real app, this would navigate away or close the window.
- **The `return` on validation failure in `GoNextAsync`:** If the server-side validation fails, the method returns without changing `CurrentStep`. The async `Task.Delay` simulates a network round-trip — the button remains disabled during the call because `IAsyncRelayCommand` sets `IsRunning = true`.

---

## What These Examples Demonstrate

| Scenario | Command technique | What to learn |
|---|---|---|
| Search with debounce | Async command + `CancellationToken` for debounce | Cancelling in-flight work, timing control, async collection update |
| Multi-step wizard | `CanExecute` driven by enum state + async step transitions | Command parameterization by step, computed `CanExecute` from multiple properties, async validation |

The search example uses an async command to debounce keystrokes, cancelling previous work with `CancellationToken`. The wizard example uses `CanExecute` to gate navigation based on the current step and field validity, with async validation inside the command body.

## See Also

- [002 — Command Binding](002-command-binding.md)
- [002V — Verbose Companion](002-command-binding-verbose.md)
- [008 — RelayCommand in Depth](008-relay-command.md)
- [008V — RelayCommand in Depth (verbose companion)](008-relay-command-verbose.md)
- [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)
- [011 — Compiled Bindings in Depth](../intermediate/011-compiled-bindings.md)
- [CommunityToolkit.Mvvm Docs: RelayCommand](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/relaycommand)
