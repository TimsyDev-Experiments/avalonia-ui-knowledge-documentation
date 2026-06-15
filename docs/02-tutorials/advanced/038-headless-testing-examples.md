---
tier: advanced
topic: testing
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 038-headless-testing.md
---

# 038E — Headless Testing with Avalonia.Headless: Real-World Examples

**What this is:** Two complete test scenarios that apply headless testing patterns — form validation, custom control interaction, pixel assertions, keyboard/focus testing, and mocking platform services.

**Prerequisites:** [038 — Headless Testing](038-headless-testing.md), [038V — Verbose Companion](038-headless-testing-verbose.md)

---

## Example 1: Form Validation — End-to-End Behavior Test

### Goal

Test a login form: verify that the submit button is disabled when fields are empty, enabled when both fields are filled, shows validation errors on incorrect input, and displays a success message after a mock login completes.

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DemoApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private bool _isLoading;

    public bool CanLogin => !string.IsNullOrWhiteSpace(Username)
                            && !string.IsNullOrWhiteSpace(Password)
                            && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Simulate authentication
            await Task.Delay(500);

            if (Username is "admin" && Password is "pass123")
            {
                IsSuccess = true;
            }
            else
            {
                ErrorMessage = "Invalid credentials.";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### View (XAML)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DemoApp.ViewModels"
             x:Class="DemoApp.Views.LoginView"
             x:DataType="vm:LoginViewModel">
  <Grid RowDefinitions="Auto,Auto,Auto,Auto,Auto"
        Margin="20" Spacing="12" Width="320">
    <TextBlock Grid.Row="0" Text="Log In"
               FontSize="18" FontWeight="Bold" />

    <TextBox Grid.Row="1"
             Text="{Binding Username}"
             Watermark="Username" />

    <TextBox Grid.Row="2"
             Text="{Binding Password}"
             Watermark="Password"
             PasswordChar="*" />

    <Button Grid.Row="3"
            Content="{Binding IsLoading, Converter={StaticResource BoolToLoginText}}"
            Command="{Binding LoginCommand}" />

    <TextBlock Grid.Row="4"
               Text="{Binding ErrorMessage}"
               Foreground="{DynamicResource ErrorBrush}"
               IsVisible="{Binding ErrorMessage, Converter={StaticResource IsNotNullConverter}}"
               TextWrapping="Wrap" />
  </Grid>
</UserControl>
```

### Headless Tests

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.Controls;
using DemoApp.ViewModels;
using FluentAssertions;

namespace DemoApp.Tests;

public class LoginFormTests
{
    [AvaloniaFact]
    public void LoginButton_Disabled_WhenFieldsEmpty()
    {
        var vm = new LoginViewModel();
        var button = new Button
        {
            Command = vm.LoginCommand,
            DataContext = vm
        };

        // Initial state
        vm.CanLogin.Should().BeFalse();
        button.Command.CanExecute(null).Should().BeFalse();
    }

    [AvaloniaFact]
    public void LoginButton_Enabled_WhenBothFieldsFilled()
    {
        var vm = new LoginViewModel
        {
            Username = "user",
            Password = "pass"
        };

        vm.CanLogin.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task Login_Fails_WithWrongCredentials()
    {
        var vm = new LoginViewModel
        {
            Username = "user",
            Password = "wrongpass"
        };

        await vm.LoginCommand.ExecuteAsync(null);

        vm.IsSuccess.Should().BeFalse();
        vm.ErrorMessage.Should().Be("Invalid credentials.");
    }

    [AvaloniaFact]
    public async Task Login_Succeeds_WithCorrectCredentials()
    {
        var vm = new LoginViewModel
        {
            Username = "admin",
            Password = "pass123"
        };

        await vm.LoginCommand.ExecuteAsync(null);

        vm.IsSuccess.Should().BeTrue();
        vm.ErrorMessage.Should().BeNull();
    }

    [AvaloniaFact]
    public void Button_IsDisabled_DuringLoading()
    {
        var vm = new LoginViewModel
        {
            Username = "admin",
            Password = "pass123"
        };

        // Before CanLogin checks IsLoading
        vm.IsLoading = true;
        vm.CanLogin.Should().BeFalse();

        vm.IsLoading = false;
        vm.CanLogin.Should().BeTrue();
    }

    [AvaloniaTheory]
    [InlineData("", "")]
    [InlineData("user", "")]
    [InlineData("", "pass")]
    [InlineData("   ", "pass")]
    public void CanLogin_ReturnsFalse_ForInvalidInputs(string username, string password)
    {
        var vm = new LoginViewModel
        {
            Username = username,
            Password = password
        };

        vm.CanLogin.Should().BeFalse();
    }
}
```

### How It Works

1. **Direct ViewModel testing** — Most tests exercise the ViewModel directly without creating any UI. `CanLogin` is a pure computed property. `LoginCommand.ExecuteAsync` runs the async login logic. This is the fastest and most reliable testing approach.

2. **`[AvaloniaTheory]` for parameterized edge cases** — `CanLogin_ReturnsFalse_ForInvalidInputs` covers five input combinations in a single test method. `InlineData` supplies username/password pairs that should all result in `CanLogin = false`.

3. **UI-enabled test via `Button.Command` binding** — `LoginButton_Disabled_WhenFieldsEmpty` creates a `Button` with the ViewModel's command. The test verifies both the VM-level `CanLogin` and the button-level `CanExecute` to confirm the binding works.

4. **Loading state test** — `Button_IsDisabled_DuringLoading` sets `IsLoading = true` directly. This tests the guard clause in `CanLogin` without needing to execute the async login method.

5. **No `Measure`/`Arrange` needed** — These tests do not need layout because they test data properties, not visual output. Only tests that check rendered pixels or visual state require `Measure`/`Arrange`.

### Design Decisions and Trade-offs

- **View-level tests omitted** — A more thorough test suite would also create the `LoginView` `UserControl` in a headless `Window` and verify that the `TextBox` bindings propagate correctly. This example focuses on the ViewModel because that is where login logic lives.
- **`[AvaloniaTheory]` vs loop in a test** — `[AvaloniaTheory]` runs each `InlineData` as a separate test, giving per-case failure reporting. A `foreach` loop inside a single test would stop at the first failure.
- **No mocking** — The `LoginViewModel` does not depend on external services, so mocking is unnecessary. A real version would inject `IAuthService` and mock it with Moq.

---

## Example 2: Custom Control Rendering and Interaction Test

### Goal

Test a custom `StarRating` control: verify that clicking on the third star sets the rating to 3, that hovering over a star changes its color, and that `CaptureRenderedImage` returns a bitmap with the correct number of filled stars.

### Custom Control Code

```csharp
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;

namespace DemoApp.Controls;

public class StarRating : Control
{
    public static readonly StyledProperty<int> RatingProperty =
        AvaloniaProperty.Register<StarRating, int>(nameof(Rating));

    public static readonly StyledProperty<int> HoverRatingProperty =
        AvaloniaProperty.Register<StarRating, int>(nameof(HoverRating));

    public static readonly StyledProperty<int> MaxStarsProperty =
        AvaloniaProperty.Register<StarRating, int>(nameof(MaxStars), 5);

    public static readonly DirectProperty<StarRating, IBrush?> FillBrushProperty =
        AvaloniaProperty.RegisterDirect<StarRating, IBrush?>(
            nameof(FillBrush), o => o.FillBrush, (o, v) => o.FillBrush = v);

    public int Rating
    {
        get => GetValue(RatingProperty);
        set => SetValue(RatingProperty, value);
    }

    public int HoverRating
    {
        get => GetValue(HoverRatingProperty);
        set => SetValue(HoverRatingProperty, value);
    }

    public int MaxStars
    {
        get => GetValue(MaxStarsProperty);
        set => SetValue(MaxStarsProperty, value);
    }

    private IBrush? _fillBrush = Brushes.Gold;
    public IBrush? FillBrush
    {
        get => _fillBrush;
        set => SetAndRaise(FillBrushProperty, ref _fillBrush, value);
    }

    public override void Render(DrawingContext context)
    {
        var fill = FillBrush ?? Brushes.Gold;
        var empty = Brushes.Gray;
        var starSize = 18.0;
        var spacing = 4.0;
        var y = (Bounds.Height - starSize) / 2;

        for (int i = 0; i < MaxStars; i++)
        {
            var x = i * (starSize + spacing);
            var isFilled = i < (HoverRating > 0 ? HoverRating : Rating);
            var rect = new Rect(x, y, starSize, starSize);
            context.FillRectangle(isFilled ? fill : empty, rect, 2);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var starIndex = GetStarIndex(e.GetPosition(this).X);
        HoverRating = Math.Clamp(starIndex + 1, 0, MaxStars);
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        HoverRating = 0;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var starIndex = GetStarIndex(e.GetPosition(this).X);
        Rating = Math.Clamp(starIndex + 1, 1, MaxStars);
        e.Handled = true;
    }

    private int GetStarIndex(double x)
    {
        var starSize = 18.0;
        var spacing = 4.0;
        var index = (int)(x / (starSize + spacing));
        return Math.Clamp(index, 0, MaxStars - 1);
    }
}
```

### Headless Tests

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using DemoApp.Controls;
using FluentAssertions;

namespace DemoApp.Tests;

public class StarRatingTests
{
    [AvaloniaFact]
    public void Rating_StartsAtZero()
    {
        var star = new StarRating();
        star.Rating.Should().Be(0);
    }

    [AvaloniaFact]
    public void Click_ThirdStar_SetsRatingToThree()
    {
        var star = new StarRating();
        var window = new Window { Content = star };
        window.Show();

        star.Measure(new Size(200, 30));
        star.Arrange(new Rect(0, 0, 200, 30));

        // Click the third star: x = 2 * (18 + 4) + 9 = 53
        star.RaiseEvent(new PointerPressedEventArgs(
            star, new Pointer(Pointer.GetNextFreeId()),
            star, new Point(53, 15), 0,
            new PointerPointProperties(), KeyModifiers.None));

        star.Rating.Should().Be(3);
    }

    [AvaloniaFact]
    public void Hover_FourthStar_SetsHoverRating()
    {
        var star = new StarRating();
        var window = new Window { Content = star };
        window.Show();

        star.Measure(new Size(200, 30));
        star.Arrange(new Rect(0, 0, 200, 30));

        star.RaiseEvent(new PointerEventArgs(
            PointerMovedEvent, star, new Pointer(Pointer.GetNextFreeId()),
            star, new Point(75, 15), 0,
            new PointerPointProperties(), KeyModifiers.None));

        star.HoverRating.Should().Be(4);
    }

    [AvaloniaFact]
    public void Hover_ThenExit_ResetsHoverRating()
    {
        var star = new StarRating();
        var window = new Window { Content = star };
        window.Show();

        star.Measure(new Size(200, 30));
        star.Arrange(new Rect(0, 0, 200, 30));

        star.RaiseEvent(new PointerEventArgs(
            PointerMovedEvent, star, new Pointer(Pointer.GetNextFreeId()),
            star, new Point(75, 15), 0,
            new PointerPointProperties(), KeyModifiers.None));

        star.RaiseEvent(new PointerEventArgs(
            PointerExitedEvent, star, new Pointer(Pointer.GetNextFreeId()),
            star, new Point(), 0,
            new PointerPointProperties(), KeyModifiers.None));

        star.HoverRating.Should().Be(0);
    }

    [AvaloniaFact]
    public void RenderedImage_HasCorrectPixels_ForThreeStars()
    {
        var star = new StarRating
        {
            Rating = 3,
            FillBrush = Brushes.Gold
        };

        var window = new Window { Content = star };
        window.Show();

        star.Measure(new Size(200, 30));
        star.Arrange(new Rect(0, 0, 200, 30));

        var bitmap = star.CaptureRenderedImage();
        bitmap.Should().NotBeNull();

        // Pixel at center of first star should be gold
        var pixel1 = bitmap!.GetPixel(9, 15).ToAvaloniaColor();
        pixel1.Should().Be(Colors.Gold);

        // Pixel at center of fourth star (not filled, Rating=3) should be gray
        var pixel4 = bitmap.GetPixel(70, 15).ToAvaloniaColor();
        pixel4.Should().Be(Colors.Gray);
    }

    [AvaloniaFact]
    public void Rating_DoesNotExceed_MaxStars()
    {
        var star = new StarRating
        {
            MaxStars = 3
        };

        // Try clicking outside the star area (beyond third star)
        var window = new Window { Content = star };
        window.Show();

        star.Measure(new Size(200, 30));
        star.Arrange(new Rect(0, 0, 200, 30));

        // Click at x=200 (far past the last star)
        star.RaiseEvent(new PointerPressedEventArgs(
            star, new Pointer(Pointer.GetNextFreeId()),
            star, new Point(200, 15), 0,
            new PointerPointProperties(), KeyModifiers.None));

        star.Rating.Should().Be(3); // Clamped to MaxStars
    }
}
```

### How It Works

1. **Layout for interaction** — Every interaction test calls `Measure` and `Arrange` after `window.Show()`. Without layout, `Bounds` is `Rect.Empty`, `GetPosition(this)` returns `(0,0)`, and all pointer events resolve to star index 0.

2. **Coordinate calculation** — Stars are drawn at `x = i * (18 + 4) = i * 22`. The third star (`i = 2`) is at `x = 44`, and the test clicks at `x = 53` (the center of the third star). Hover over the fourth star uses `x = 75` (3 * 22 + 9).

3. **`Pointer.GetNextFreeId()`** — The headless platform requires a valid `Pointer` with a unique ID. `Pointer.GetNextFreeId()` generates one. Reusing the same pointer in multiple tests is fine because the headless platform has no real pointer state.

4. **`RoutedEvent` must be set for pointer events** — `PointerMovedEvent` and `PointerExitedEvent` are passed as the first argument to `PointerEventArgs`. Without setting `RoutedEvent`, the event does not route to the control's handlers.

5. **Pixel assertion** — `RenderedImage_HasCorrectPixels_ForThreeStars` captures the rendered output and checks individual pixels. The fill color (`Gold`) confirms the rendering produced the expected visual. The empty star pixel (`Gray`) confirms the rendering boundary between filled and unfilled.

### Design Decisions and Trade-offs

- **`CaptureRenderedImage` variability** — The headless software renderer may produce slightly different pixel values on different platforms or renderer versions. Tests that assert exact colors should use a tolerance or compare against a reference render done on the same platform.
- **`Pointer.Pointer.GetNextFreeId()` vs specific ID** — The headless platform does not enforce pointer identity. Any unique positive integer works. `GetNextFreeId()` avoids accidental collisions.
- **Explicit layout vs template** — The `StarRating` control draws itself in `Render` and has no template. Controls that rely on templates (`Button`, `TextBox`) require more complex setup with theme application.

---

## Comparison: What the Two Examples Demonstrate

| Aspect | Example 1 — Form Validation | Example 2 — Custom Control |
|--------|------------------------------|----------------------------|
| Test target | ViewModel (`LoginViewModel`) | Custom `StarRating` control |
| UI creation | None (direct VM testing) | Yes (`Window`, `Measure`, `Arrange`) |
| `[AvaloniaFact]` usage | Yes | Yes |
| `[AvaloniaTheory]` usage | Yes (5 `InlineData` cases) | No |
| Pointer events | Not needed | `PointerPressedEventArgs`, `PointerMovedEventArgs` |
| Pixel assertions | Not needed | `CaptureRenderedImage()` + `GetPixel()` |
| Layout required | No | Yes (`Measure`/`Arrange`) |
| Command testing | `CanExecute` + `ExecuteAsync` | Property-based |
| Mocking | Not needed | Not needed |
| Theme dependency | No | No (custom `Render` override) |

## See Also

- [038 — Headless Testing](038-headless-testing.md) — the original tutorial
- [038V — Headless Testing (verbose companion)](038-headless-testing-verbose.md)
- [039 — NativeAOT and Trimming](039-nativeaot-trimming.md) — why compiled bindings matter in tests
- [007 — Observable Object and Property](../basics/007-observable-object-property.md) — ViewModel property changes
- [Avalonia Docs: Headless Testing](https://docs.avaloniaui.net/docs/concepts/headless-testing)
