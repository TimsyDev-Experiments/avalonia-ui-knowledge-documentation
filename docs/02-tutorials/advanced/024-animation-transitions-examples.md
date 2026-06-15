---
tier: advanced
topic: animation
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 024-animation-transitions.md
---

# 024E — Animation & Transitions: Real-World Examples

**Applies to:** [024 — Animation & Transitions](024-animation-transitions.md) | [024V — In-Depth Companion](024-animation-transitions-verbose.md)

---

## Example 1: CardFlipControl

### Goal

A control that displays a card with a front and back face. Clicking the card triggers a 3D flip animation using `RenderTransform` and `TransformOperationsTransition`. The flip is driven by animating `RotationY` from 0 to 180 degrees. At the midpoint (90°), the faces swap.

### ViewModel

```csharp
// ViewModels/FlashCardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class FlashCardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _frontText = "Front";

    [ObservableProperty]
    private string _backText = "Back";

    [ObservableProperty]
    private bool _isFlipped;

    [RelayCommand]
    private void ToggleFlip()
    {
        IsFlipped = !IsFlipped;
    }
}
```

### XAML View

```xml
<!-- Views/FlashCardView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:DataType="vm:FlashCardViewModel">
  <Border Width="300" Height="400"
          CornerRadius="12"
          Background="White"
          PointerPressed="{Binding ToggleFlipCommand}">
    <Border.RenderTransform>
      <TransformGroup>
        <ScaleTransform ScaleX="1" ScaleY="1" />
      </TransformGroup>
    </Border.RenderTransform>

    <Border.Transitions>
      <Transitions>
        <TransformOperationsTransition Property="RenderTransform"
                                       Duration="0:0:0.6" />
      </Transitions>
    </Border.Transitions>

    <Grid>
      <!-- Front face -->
      <TextBlock Text="{Binding FrontText}"
                 FontSize="24"
                 HorizontalAlignment="Center"
                 VerticalAlignment="Center"
                 IsVisible="{Binding IsFlipped, Converter={x:Static BoolConverters.Not}}" />

      <!-- Back face -->
      <TextBlock Text="{Binding BackText}"
                 FontSize="24"
                 HorizontalAlignment="Center"
                 VerticalAlignment="Center"
                 IsVisible="{Binding IsFlipped}" />
    </Grid>
  </Border>
</UserControl>
```

### How It Works

1. The `Border` has a `TransformGroup` with a `ScaleTransform` as its `RenderTransform`. The `TransformOperationsTransition` animates the entire `RenderTransform` property.
2. The `ToggleFlipCommand` sets `IsFlipped` to the opposite value. This triggers two cascading changes:
   - The `TransformOperationsTransition` animates from `scale(1)` to `scale(-1, 1)` over 600ms, which creates a horizontal flip effect.
   - The `IsVisible` bindings switch the front/back `TextBlock` at the same time — but the back face only needs to be visible when `IsFlipped` is true.
3. At `RotationY = 90°` (or `ScaleX = 0`), both faces are edge-on and invisible. This is the transition point. The `IsVisible` swaps happen immediately, but the visual is hidden during the exact moment the face changes.
4. When `IsFlipped` is set back to false, the `TransformOperationsTransition` animates `RenderTransform` back to `scale(1)`, reversing the flip.

### Design Decisions

- **`ScaleTransform` with negative `ScaleX` instead of `PlaneProjection`.** Avalonia 12 does not have a `PlaneProjection` equivalent. Using `ScaleX = -1` creates a mirror effect that simulates a 2.5D flip. For a true 3D perspective effect, use a custom `RenderTransform` that combines `ScaleX` with a perspective matrix.
- **`BoolConverters.Not` for the front face visibility.** The front face should be hidden when flipped. The built-in `BoolConverters.Not` (from Avalonia) inverts the boolean binding directly without a custom converter.
- **`TransformOperationsTransition` over `DoubleTransition` on `ScaleTransform.ScaleX`.** `TransformOperationsTransition` animates the composed transform matrix, which is what the compositor consumes. Animating individual `ScaleX` via `DoubleTransition` works but can produce slight jitter due to property system overhead.

### Edge Cases

- **Rapid double-click during flip.** The animation is already running when `IsFlipped` changes again. The `TransformOperationsTransition` restarts from the current animated value, causing the card to stutter. Mitigate by disabling the command while the animation runs: add a `IsAnimating` flag that `ToggleFlip` checks before proceeding.
- **Card not visible at midpoint.** At `ScaleX ≈ 0`, the card disappears (it's edge-on). This is expected behavior for a flip animation. Ensure the background color of the container matches the card edge to avoid a visible gap.
- **Accessibility.** A flipped card is still in the focus order. Set `AutomationProperties.Name` to the current visible face text so screen readers announce the correct content.

---

## Example 2: StaggeredListEntrance

### Goal

An `ItemsControl` where items animate in sequentially when the view loads or the data source changes. Each item fades in and slides up from below with a staggered delay (item index × stagger interval). The animation is driven programmatically using the `Animation` class.

### ViewModel

```csharp
// ViewModels/TaskListViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class TaskItemViewModel : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private bool _isComplete;
}

public partial class TaskListViewModel : ObservableObject
{
    public ObservableCollection<TaskItemViewModel> Tasks { get; } = [];

    [ObservableProperty]
    private bool _hasLoaded;

    public async Task LoadTasksAsync()
    {
        // Simulate loading
        await Task.Delay(300);
        Tasks.Add(new TaskItemViewModel { Title = "Review PR" });
        Tasks.Add(new TaskItemViewModel { Title = "Update docs" });
        Tasks.Add(new TaskItemViewModel { Title = "Fix bug #42" });
        Tasks.Add(new TaskItemViewModel { Title = "Run tests" });
        Tasks.Add(new TaskItemViewModel { Title = "Deploy build" });
        HasLoaded = true;
    }
}
```

### XAML View

```xml
<!-- Views/TaskListView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:DataType="vm:TaskListViewModel"
             x:Name="Root">
  <ItemsControl ItemsSource="{Binding Tasks}">
    <ItemsControl.ItemTemplate>
      <DataTemplate x:DataType="vm:TaskItemViewModel">
        <Border Background="#f8f8f8" CornerRadius="6"
                Padding="12" Margin="0,4"
                Opacity="0"
                RenderTransform="{Binding $parent[ItemsControl].Tag}"
                x:Name="ItemHost">
          <Border.Transitions>
            <Transitions>
              <DoubleTransition Property="Opacity"
                                Duration="0:0:0.3" />
            </Transitions>
          </Border.Transitions>
          <CheckBox IsChecked="{Binding IsComplete}"
                    Content="{Binding Title}" />
        </Border>
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</UserControl>
```

### How It Works

1. When `HasLoaded` becomes true, the ViewModel triggers a method in the view's code-behind (via a messenger, event, or binding to a `LoadTrigger` property).
2. The code-behind iterates the generated item containers (`ItemContainerGenerator.Containers`). For each container at index `i`:
   - Sets `RenderTransform` to a `TranslateTransform` with `Y = 30` (start position below final).
   - Creates an `Animation` with two keyframes: `TranslateY = 30` at 0% and `TranslateY = 0` at 100%, duration 400ms.
   - Sets `Animation.Delay = TimeSpan.FromMilliseconds(i * 80)` for the stagger.
   - Sets `Opacity = 1` on the container (the `DoubleTransition` animates this over 300ms).
   - Runs the animation via `RunAsync(container, token)`.
3. The stagger delay creates a cascading effect: item 0 starts immediately, item 1 starts after 80ms, item 2 after 160ms, etc.

### Design Decisions

- **`TranslateTransform` for slide, `Opacity` via `DoubleTransition`.** The slide uses a programmatic `Animation` because it requires per-item configuration (delay, start offset). The opacity fade is simpler — a `DoubleTransition` on the `Opacity` property animates automatically from 0 to 1 when set in code.
- **80ms stagger interval.** Below 50ms, items appear to move as a group (no perceived stagger). Above 150ms, the animation feels sluggish for more than 10 items. 80ms is a good default for 5–15 items. Make it configurable via a `StaggerDelay` attached property.
- **Start offset of 30px.** Large enough to be noticeable, small enough that the item does not cross multiple item boundaries during animation. For taller items, scale the offset proportionally.

### Edge Cases

- **Items added after the initial animation.** New items should also animate in. The code-behind subscribes to `CollectionChanged` on the `ItemsSource`. When items are added, it animates only the new containers. Use a `HashSet<object>` to track which items have already been animated.
- **Items removed before animation completes.** Cancel the `CancellationTokenSource` associated with the removed item's animation. Without cancellation, the animation completes on a detached container, producing errors or ghost visuals.
- **Window minimized during animation.** The compositor pauses animations for hidden windows. When the window is restored, the remaining animations may jump to their final state. Set `Animation.PlaybackBehavior = PlaybackBehavior.Always` to force completion.

### Edge Cases (continued)

- **Empty list.** No containers to animate. The method exits immediately.
- **Scrolled list.** Items entering the viewport should also animate. Use `EffectiveViewportChanged` on each container. Only animate items within the viewport. Items outside the viewport start at full opacity and final position to avoid invisible animations.

---

## What These Examples Demonstrate

| Aspect | CardFlipControl | StaggeredListEntrance |
|---|---|---|
| **Animation system** | `TransformOperationsTransition` | Programmatic `Animation` class |
| **Trigger** | Command (user click) | Data load completion |
| **Animated property** | `RenderTransform` (ScaleX) | `Opacity` (transition) + `TranslateY` (keyframes) |
| **Performance** | GPU-accelerated (compositor) | CPU-bound (layout + property system) |
| **Reversibility** | Yes — toggle property flips back | No — entrance animations are one-shot |
| **Delay/stagger** | None (single element) | Staggered per item index |
| **Configuration** | Duration via XAML transition | Delay, offset, duration in code-behind |

---

## See Also

- [024 — Animation & Transitions](024-animation-transitions.md)
- [024V — Animation & Transitions (verbose companion)](024-animation-transitions-verbose.md)
- [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md) — GPU-only animation path
- [024 — Animation & Transitions](024-animation-transitions.md) — comprehensive coverage of animation primitives
- [Avalonia Docs: Animation](https://docs.avaloniaui.net/docs/animation/)
