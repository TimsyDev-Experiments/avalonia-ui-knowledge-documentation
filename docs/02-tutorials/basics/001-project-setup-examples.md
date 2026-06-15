---
tier: basics
topic: project setup
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 001-project-setup.md
---

# 001X — Project Setup: Real-World Examples

**What you'll build:** A multi-window MVVM app with DI container setup and a headless-tested ViewModel pipeline — two project structures that go beyond the default template.

**Prerequisites:** [001 — Project Setup](001-project-setup.md). The [verbose companion](001-project-setup-verbose.md) covers `AppBuilder`, lifetime selection, and compiled binding mechanics in depth.

---

## Example 1: Multi-Window App with Dependency Injection

**Goal:** Structure a desktop app where windows are created through a DI container, ViewModels receive services via constructor injection, and the `MainWindow` is resolved — not instantiated directly.

The default template creates one window with `new MainWindow()`. A real app needs service lifetimes, constructor-injected ViewModels, and the ability to open secondary windows with their own resolved ViewModels.

### App entry point with DI host

```csharp
// Program.cs
using Avalonia;
using AvaloniaUI.DiagnosticsSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyApp;

public static class Program
{
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseSkia()
            .UseHarfBuzz();
}
```

### App.axaml.cs — host setup

```csharp
// App.axaml.cs
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyApp;

public partial class App : Application
{
    private IHost? _host;

    public override void OnFrameworkInitializationCompleted()
    {
        _host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddTransient<MainWindow>();
                services.AddTransient<SettingsWindow>();
            })
            .Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _host.Services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

### MainWindow code-behind

```csharp
// Views/MainWindow.axaml.cs
using MyApp.ViewModels;

namespace MyApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

The `DataContext` is set by the DI container — see the `MainWindow` constructor registration. In practice you can also inject the ViewModel directly:

```csharp
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

### MainWindow ViewModel

```csharp
// ViewModels/MainWindowViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = App.Current?.Services?.GetService<SettingsWindow>();
        if (settingsWindow is not null)
        {
            settingsWindow.Show();
        }
    }
}
```

### SettingsWindow ViewModel

```csharp
// ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private string _displayName = "User";
}
```

### View

```xml
<!-- Views/MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:MyApp.ViewModels"
        x:Class="MyApp.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="MyApp">
  <DockPanel Margin="16">
    <Button DockPanel.Dock="Top"
            Content="Open Settings"
            Command="{Binding OpenSettingsCommand}" />
    <TextBlock Text="{Binding StatusText}" />
  </DockPanel>
</Window>
```

### How it works

1. `Program.cs` follows the standard Avalonia 12 pattern with explicit `UseSkia()` + `UseHarfBuzz()`.
2. `App.OnFrameworkInitializationCompleted()` builds a `HostBuilder` and registers services. The `MainWindow` is resolved from the container, not instantiated with `new`.
3. `MainWindow` receives its ViewModel via constructor injection — no `DataContext = new ...()` in the constructor.
4. The `OpenSettings` command resolves a transient `SettingsWindow` from the same container. Each call creates a fresh window with a resolved `SettingsViewModel`.
5. The `x:DataType` on `MainWindow.axaml` enables compiled bindings. If `StatusText` or `OpenSettingsCommand` is missing from `MainWindowViewModel`, the build fails.
6. The DI pattern means ViewModels can accept services (file storage, API clients, navigation) without the View layer knowing about construction details.

### Design decisions and trade-offs

- **`AddSingleton` vs `AddTransient` for ViewModels:** Use singleton when the ViewModel lives as long as the app (main window). Use transient when the ViewModel is created per-window (settings). A singleton ViewModel bound to multiple windows shares state — that is sometimes desired, sometimes a bug.
- **HostBuilder lifecycle:** The `_host` field is not disposed in this example. In production, subscribe to `desktop.Exit` or `AppDomain.CurrentDomain.ProcessExit` and call `_host.Dispose()`.
- **Service locator anti-pattern:** The `OpenSettings` command calls `App.Current?.Services?.GetService<SettingsWindow>()`. This is a service locator — acceptable in the composition root (command code) but avoid in ViewModel logic proper. Inject a factory interface (`ISettingsWindowFactory`) for testability.

---

## Example 2: Headless-Validated ViewModel Pipeline

**Goal:** Verify that a ViewModel's properties and commands behave correctly without launching a real window, using Avalonia's headless platform.

This setup allows unit tests to exercise ViewModel logic, command execution, property change notifications, and validation — all without showing a pixel.

### Test project setup

```bash
dotnet new xunit -n MyApp.Tests
cd MyApp.Tests
dotnet add package Avalonia.Headless.NUnit
dotnet add reference ../MyApp/MyApp.csproj
```

### Headless app builder

```csharp
// Tests/AppBuilderFixture.cs
using Avalonia;
using Avalonia.Headless;

namespace MyApp.Tests;

public static class AppBuilderFixture
{
    public static AppBuilder Build() =>
        AppBuilder.Configure<App>()
            .UseHeadless();
}
```

### ViewModel under test

```csharp
// ViewModels/LoginViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

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

    public bool IsLoggedIn { get; private set; }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private void Login()
    {
        if (Username == "admin" && Password == "pass123")
        {
            IsLoggedIn = true;
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = "Invalid credentials";
        }
    }

    private bool CanLogin() =>
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);
}
```

### XAML view (for reference — not used in tests)

```xml
<!-- Views/LoginView.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:MyApp.ViewModels"
        x:Class="MyApp.Views.LoginView"
        x:DataType="vm:LoginViewModel"
        Title="Login" Width="320" Height="240">
  <StackPanel Spacing="8" Margin="16">
    <TextBlock Text="Username" />
    <TextBox Text="{Binding Username, Mode=TwoWay}" />
    <TextBlock Text="Password" />
    <TextBox Text="{Binding Password, Mode=TwoWay}"
             PasswordChar="*" />
    <Button Content="Login"
            Command="{Binding LoginCommand}" />
    <TextBlock Text="{Binding ErrorMessage}"
               Foreground="Red"
               IsVisible="{Binding ErrorMessage, Converter={StaticResource NotNullToBool}}" />
  </StackPanel>
</Window>
```

### Unit test

```csharp
// Tests/LoginViewModelTests.cs
using MyApp.ViewModels;

namespace MyApp.Tests;

public class LoginViewModelTests
{
    [Fact]
    public void Login_WithValidCredentials_SetsIsLoggedIn()
    {
        var vm = new LoginViewModel { Username = "admin", Password = "pass123" };
        vm.LoginCommand.Execute(null);
        Assert.True(vm.IsLoggedIn);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public void Login_WithInvalidCredentials_SetsErrorMessage()
    {
        var vm = new LoginViewModel { Username = "bad", Password = "bad" };
        vm.LoginCommand.Execute(null);
        Assert.False(vm.IsLoggedIn);
        Assert.Equal("Invalid credentials", vm.ErrorMessage);
    }

    [Fact]
    public void CanExecute_ReturnsFalse_WhenFieldsAreEmpty()
    {
        var vm = new LoginViewModel();
        Assert.False(vm.LoginCommand.CanExecute(null));
        vm.Username = "admin";
        Assert.False(vm.LoginCommand.CanExecute(null));
        vm.Password = "pass123";
        Assert.True(vm.LoginCommand.CanExecute(null));
    }

    [Fact]
    public void PropertyChanged_RaisesNotification()
    {
        var vm = new LoginViewModel();
        var changed = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.Username))
                changed = true;
        };
        vm.Username = "new value";
        Assert.True(changed);
    }
}
```

### Headless UI test (optional)

```csharp
// Tests/LoginViewUITests.cs
using Avalonia.Headless;
using MyApp.ViewModels;
using MyApp.Views;

namespace MyApp.Tests;

public class LoginViewUITests
{
    [AvaloniaFact]
    public void LoginButton_IsDisabled_WhenFieldsEmpty()
    {
        var window = new LoginView
        {
            DataContext = new LoginViewModel()
        };
        window.Show();

        var button = window.FindControl<Button>("loginButton");
        Assert.False(button.IsEnabled);
    }
}
```

### How it works

1. The `LoginViewModel` exposes observable properties (`Username`, `Password`) and a command (`LoginCommand`).
2. `[NotifyCanExecuteChangedFor(nameof(LoginCommand))]` on each property ensures that when `Username` or `Password` changes, the command re-evaluates `CanLogin()`.
3. Tests create the ViewModel directly — no window, no `Application` instance needed for property-level tests.
4. The `CanExecute` tests verify that the button is disabled when fields are empty, without a real UI.
5. Headless tests (using `[AvaloniaFact]`) can exercise view-layer logic: window creation, control lookup, visual state — but still render nothing to screen.
6. The XAML view uses compiled bindings — the `x:DataType` guarantees that all binding paths exist at compile time, so the tests can trust that the view won't fail at runtime for missing properties.

### Design decisions and trade-offs

- **ViewModel tests need no Avalonia headless platform.** Property changes, command execution, and `CanExecute` are pure .NET. Only tests that create controls need headless setup.
- **Headless tests are slower** than pure ViewModel tests because they initialize the Avalonia rendering pipeline (even though nothing is drawn). Reserve `[AvaloniaFact]` for tests that actually touch the visual tree.
- **The `[NotifyCanExecuteChangedFor]` attribute is the bridge** between property changes and command re-evaluation. Without it, the `CanExecute` tests above would pass (because calling `CanExecute` directly works), but the real UI would not update.

---

## What These Examples Demonstrate

| Scenario | Project structure | Key technique |
|---|---|---|
| DI-hosted multi-window app | `IHost` + container resolution | Constructor injection for ViewModels, service locator for secondary windows |
| Headless-tested ViewModel | Test project + xUnit | No-platform ViewModel testing vs headless UI testing |

The first example focuses on *composing* the project — wiring DI, resolving windows, managing lifetimes. The second focuses on *validating* the project — testing ViewModel logic without the UI layer and verifying compiled bindings work via `x:DataType`.

## See Also

- [001 — Project Setup](001-project-setup.md)
- [001V — Verbose Companion](001-project-setup-verbose.md)
- [011 — Compiled Bindings in Depth](../intermediate/011-compiled-bindings.md)
- [Avalonia Docs: Headless testing](https://docs.avaloniaui.net/docs/guides/testing)
- [Avalonia 12 Breaking Changes](../../04-migration/avalonia-11-to-12.md)
