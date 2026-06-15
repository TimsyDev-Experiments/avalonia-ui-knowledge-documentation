---
tier: intermediate
topic: navigation
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 018-navigation.md
---

# 018E — Navigation Patterns: Real-World Examples

**What this is:** Two worked examples of ViewModel-first navigation with `ContentControl`, view locator, and lifecycle management. Read [018 — Navigation Patterns](018-navigation.md) and [018V — Verbose Companion](018-navigation-verbose.md) first.

---

## Example 1: Tab-Based Navigation with ViewModel Activation/Deactivation

### Goal

A primary navigation shell with three tabs (Home, Dashboard, Settings). Each tab is a separate ViewModel that receives activation/deactivation lifecycle events. Tab state is preserved when switching away and back.

### ViewModel Interface and Base Class

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public interface INavigationAware
{
    void OnNavigatedTo(NavigationContext context);
    void OnNavigatedFrom(NavigationContext context);
}

public record NavigationContext(string? Source, object? Parameter);

public abstract partial class TabViewModelBase : ObservableObject, INavigationAware
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isActive;

    public virtual void OnNavigatedTo(NavigationContext context) { }
    public virtual void OnNavigatedFrom(NavigationContext context) { }
}
```

### NavigationService with Lifecycle

```csharp
using MyApp.ViewModels;

namespace MyApp.Services;

public interface ITabNavigationService
{
    TabViewModelBase? CurrentTab { get; }
    void NavigateTo(TabViewModelBase tab, object? parameter = null);
}

public class TabNavigationService : ITabNavigationService
{
    private readonly Dictionary<string, TabViewModelBase> _tabs = new();

    public TabViewModelBase? CurrentTab { get; private set; }

    public void NavigateTo(TabViewModelBase tab, object? parameter = null)
    {
        var key = tab.GetType().Name;

        if (CurrentTab is INavigationAware from)
            from.OnNavigatedFrom(new NavigationContext(key, null));

        if (!_tabs.ContainsKey(key))
            _tabs[key] = tab;

        CurrentTab = _tabs[key];
        CurrentTab.IsActive = true;

        if (CurrentTab is INavigationAware to)
            to.OnNavigatedTo(new NavigationContext(null, parameter));
    }
}
```

### ViewModels

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class HomeTabViewModel : TabViewModelBase
{
    public HomeTabViewModel()
    {
        Title = "Home";
    }

    public override void OnNavigatedTo(NavigationContext context)
    {
        // Refresh dashboard data
        LoadDataCommand.Execute(null);
    }

    [ObservableProperty]
    private string _welcomeMessage = "Welcome back!";

    [RelayCommand]
    private void LoadData() { /* load from service */ }
}

public partial class DashboardTabViewModel : TabViewModelBase
{
    public DashboardTabViewModel()
    {
        Title = "Dashboard";
    }

    public override void OnNavigatedFrom(NavigationContext context)
    {
        // Save scroll position
        ScrollPosition = _currentScroll;
    }

    [ObservableProperty]
    private double _scrollPosition;

    private double _currentScroll;
}

public partial class SettingsTabViewModel : TabViewModelBase
{
    public SettingsTabViewModel()
    {
        Title = "Settings";
    }

    public override void OnNavigatedTo(NavigationContext context)
    {
        if (context.Parameter is string section)
            ActiveSection = section;
    }

    [ObservableProperty]
    private string _activeSection = "general";

    [RelayCommand]
    private void NavigateToSection(string section) => ActiveSection = section;
}
```

### Shell ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ITabNavigationService _navigation;
    private readonly IServiceProvider _services;

    public ShellViewModel(ITabNavigationService navigation, IServiceProvider services)
    {
        _navigation = navigation;
        _services = services;
    }

    public TabViewModelBase? CurrentTab => _navigation.CurrentTab;

    [RelayCommand]
    private void GoToHome()
    {
        var tab = _services.GetRequiredService<HomeTabViewModel>();
        _navigation.NavigateTo(tab);
        OnPropertyChanged(nameof(CurrentTab));
    }

    [RelayCommand]
    private void GoToDashboard()
    {
        var tab = _services.GetRequiredService<DashboardTabViewModel>();
        _navigation.NavigateTo(tab);
        OnPropertyChanged(nameof(CurrentTab));
    }

    [RelayCommand]
    private void GoToSettings()
    {
        var tab = _services.GetRequiredService<SettingsTabViewModel>();
        _navigation.NavigateTo(tab, "appearance");
        OnPropertyChanged(nameof(CurrentTab));
    }
}
```

### XAML — Shell View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:vm="using:MyApp.ViewModels"
        x:DataType="vm:ShellViewModel"
        Title="MyApp" Width="900" Height="600">

  <Grid ColumnDefinitions="200,*">
    <!-- Sidebar navigation -->
    <Border Background="{DynamicResource Surface}" Padding="12">
      <StackPanel Spacing="8">
        <TextBlock Text="MyApp" FontSize="20" FontWeight="Bold" />
        <Button Content="Home"
                Command="{Binding GoToHomeCommand}"
                HorizontalAlignment="Stretch" />
        <Button Content="Dashboard"
                Command="{Binding GoToDashboardCommand}"
                HorizontalAlignment="Stretch" />
        <Button Content="Settings"
                Command="{Binding GoToSettingsCommand}"
                HorizontalAlignment="Stretch" />
      </StackPanel>
    </Border>

    <!-- Active tab content -->
    <ContentControl Grid.Column="1"
                    Content="{Binding CurrentTab}">
      <ContentControl.ContentTemplate>
        <DataTemplate x:DataType="vm:TabViewModelBase">
          <views:ViewLocator />
        </DataTemplate>
      </ContentControl.ContentTemplate>
    </ContentControl>
  </Grid>
</Window>
```

### How It Works

1. Clicking "Dashboard" calls `GoToDashboardCommand`. The Shell resolves a `DashboardTabViewModel` from DI and passes it to `TabNavigationService.NavigateTo`.
2. `NavigateTo` calls `OnNavigatedFrom` on the current tab, then stores/preserves the new tab in the dictionary.
3. `CurrentTab` property change triggers the `ContentControl` to re-evaluate. The `ViewLocator` builds the correct view for the active ViewModel.
4. `HomeTabViewModel.OnNavigatedTo` refreshes data. `DashboardTabViewModel.OnNavigatedFrom` saves scroll position. Tab state is preserved in the dictionary (tabs are singleton per type, not transient).

### Design Decisions & Edge Cases

- **Why singleton tabs (preserved in dictionary) instead of transient:** Each tab's state (scroll position, form inputs) is preserved when switching away. Transient tabs would lose state. Use transient only for "wizard step" or "one-shot" views.
- **Why `INavigationAware` instead of `ObservableRecipient.IsActive`:** `INavigationAware` is navigation-specific — it carries the source tab and parameter. `ObservableRecipient.IsActive` is broader (messenger lifecycle). Both can coexist.
- **Edge case — navigating to the same tab twice:** `NavigationService` returns the existing instance. `OnNavigatedFrom` is called on the current tab, then `OnNavigatedTo` immediately after. If this is undesirable, guard with `if (key == CurrentTab?.GetType().Name) return;`.
- **Edge case — tab destruction:** If the app needs to close a tab and free its memory, add a `CloseTab(string key)` method that removes it from the dictionary and calls `IDisposable.Dispose()`.

---

## Example 2: Deep Linking with Route Parameters

### Goal

Navigate to a detail view using a route-like path (e.g., `product/42/reviews`) parsed from a string, with typed parameters passed to the target ViewModel.

### NavigationService with Route Parsing

```csharp
using MyApp.ViewModels;

namespace MyApp.Services;

public interface IRouteNavigationService
{
    void NavigateTo(string route);
    void GoBack();
}

public class RouteNavigationService : IRouteNavigationService
{
    private readonly Stack<ViewModelBase> _history = new();
    private readonly IServiceProvider _services;
    private readonly Dictionary<string, Func<string[], ViewModelBase>> _routes = new();

    public RouteNavigationService(IServiceProvider services)
    {
        _services = services;

        _routes["home"] = _ => _services.GetRequiredService<HomeViewModel>();
        _routes["product"] = parts => new ProductDetailViewModel(
            _services.GetRequiredService<IProductService>(),
            int.Parse(parts[1]));
        _routes["product/reviews"] = parts => new ProductReviewsViewModel(
            _services.GetRequiredService<IProductService>(),
            int.Parse(parts[1]));
        _routes["settings"] = _ => _services.GetRequiredService<SettingsViewModel>();
        _routes["settings/security"] = _ => new SecuritySettingsViewModel();
    }

    public ViewModelBase? CurrentView { get; private set; }

    public void NavigateTo(string route)
    {
        // Route is "product/42/reviews" → segments = ["product", "42", "reviews"]
        var segments = route.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Try longest path first
        for (var len = segments.Length; len > 0; len--)
        {
            var path = string.Join("/", segments.Take(len));
            if (_routes.TryGetValue(path, out var factory))
            {
                var remaining = segments.Skip(len).ToArray();
                var vm = factory(remaining);

                if (CurrentView is not null)
                    _history.Push(CurrentView);

                CurrentView = vm;
                return;
            }
        }

        throw new InvalidOperationException($"No route registered for '{route}'");
    }

    public void GoBack()
    {
        if (_history.Count > 0)
            CurrentView = _history.Pop();
    }
}
```

### ViewModels

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class ProductDetailViewModel : ViewModelBase
{
    private readonly IProductService _products;

    public ProductDetailViewModel(IProductService products, int productId)
    {
        _products = products;
        ProductId = productId;
    }

    public int ProductId { get; }

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private string _description = string.Empty;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var product = await _products.GetByIdAsync(ProductId);
        if (product is not null)
        {
            ProductName = product.Name;
            Price = product.Price;
            Description = product.Description;
        }
    }
}

public partial class ProductReviewsViewModel : ViewModelBase
{
    private readonly IProductService _products;

    public ProductReviewsViewModel(IProductService products, int productId)
    {
        _products = products;
        ProductId = productId;
    }

    public int ProductId { get; }

    public ObservableCollection<Review> Reviews { get; } = new();

    [RelayCommand]
    private async Task LoadAsync()
    {
        var reviews = await _products.GetReviewsAsync(ProductId);
        Reviews.Clear();
        foreach (var r in reviews)
            Reviews.Add(r);
    }
}

public partial class HomeViewModel : ViewModelBase { }
public partial class SettingsViewModel : ViewModelBase { }
public partial class SecuritySettingsViewModel : ViewModelBase { }

public record Review(string Author, int Rating, string Text);
```

### Shell ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IRouteNavigationService _navigation;

    public ShellViewModel(IRouteNavigationService navigation)
    {
        _navigation = navigation;
    }

    public ViewModelBase? CurrentView => _navigation.CurrentView;

    [RelayCommand]
    private void Navigate(string route)
    {
        _navigation.NavigateTo(route);
        OnPropertyChanged(nameof(CurrentView));
    }

    [RelayCommand]
    private void Back()
    {
        _navigation.GoBack();
        OnPropertyChanged(nameof(CurrentView));
    }
}
```

### XAML — Shell View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:vm="using:MyApp.ViewModels"
        x:DataType="vm:ShellViewModel"
        Title="Store" Width="800" Height="600">

  <Grid RowDefinitions="Auto,*">
    <!-- Breadcrumb / navigation bar -->
    <StackPanel Orientation="Horizontal" Spacing="8" Margin="12">
      <Button Content="← Back"
              Command="{Binding BackCommand}" />
      <Button Content="Home"
              Command="{Binding NavigateCommand}"
              CommandParameter="home" />
      <Button Content="Settings"
              Command="{Binding NavigateCommand}"
              CommandParameter="settings" />
    </StackPanel>

    <!-- Active view -->
    <ContentControl Grid.Row="1"
                    Content="{Binding CurrentView}">
      <ContentControl.ContentTemplate>
        <DataTemplate x:DataType="vm:ViewModelBase">
          <views:ViewLocator />
        </DataTemplate>
      </ContentControl.ContentTemplate>
    </ContentControl>
  </Grid>
</Window>
```

### Usage — Navigating from Another ViewModel

```csharp
// In a product list ViewModel
[RelayCommand]
private void OpenProduct(int productId)
{
    var shell = _services.GetRequiredService<ShellViewModel>();
    shell.NavigateCommand.Execute($"product/{productId}");
}

[RelayCommand]
private void OpenProductReviews(int productId)
{
    var shell = _services.GetRequiredService<ShellViewModel>();
    shell.NavigateCommand.Execute($"product/{productId}/reviews");
}
```

### How It Works

1. `Navigate("product/42")` splits into `["product", "42"]`. The route table matches `"product"` and passes `["42"]` to the factory. The factory parses `int.Parse("42")` and creates `ProductDetailViewModel`.
2. `Navigate("product/42/reviews")` matches `"product/reviews"` (longest match), passes `["42"]`, creates `ProductReviewsViewModel`.
3. The current view is pushed onto the history stack. `GoBack` pops the previous view.
4. The `ViewLocator` maps each ViewModel to its corresponding view using convention or dictionary.

### Design Decisions & Edge Cases

- **Why string routes instead of typed navigation methods:** String routes can come from external sources (deep links, notification clicks, saved bookmarks). They serialize easily to settings files or URLs.
- **Why longest-path matching:** Without it, `"product/42/reviews"` would match `"product"` with remaining `["42", "reviews"]`, and the factory would not know the sub-route. Longest-path ensures `"product/reviews"` is tried first.
- **Edge case — invalid route segment:** `"product/abc"` would throw `FormatException` from `int.Parse`. Add a try/catch in the factory and return an error ViewModel instead of crashing.
- **Edge case — history overflow:** Each navigation pushes to the stack. For long sessions, limit stack depth (e.g., 50 entries) to cap memory usage.
- **Trade-off:** Route-based navigation requires a central route table that knows about all ViewModels. For small apps (5–10 views) this is manageable. For large apps, use a convention-based route discovery (scan assemblies for `[Route]` attributes).

---

## Comparison

| Aspect | Example 1 — Tab Navigation | Example 2 — Route Navigation |
|---|---|---|
| **Navigation trigger** | ViewModel command (`GoToHome`) | String route (`"product/42"`) |
| **State preservation** | Singleton tabs (state kept in memory) | Transient views (new instance per navigation) |
| **Parameters** | Object via `NavigationContext` | Parsed from route segments |
| **Lifecycle hooks** | `INavigationAware.OnNavigatedTo/From` | Manual (data loading in command) |
| **History** | Single active tab (no back stack) | `Stack<ViewModelBase>` with `GoBack` |
| **When to use** | Dashboard, settings, primary navigation | Deep linking, product drill-down, wizards |
| **Key risk** | Tab singleton memory growth | Route string typos at runtime (no compile check) |

---

## See Also

- [018 — Navigation Patterns (original)](018-navigation.md)
- [018V — Navigation Patterns (verbose companion)](018-navigation-verbose.md)
- [015 — Item Lists](015-item-lists.md)
- [016 — Window & Dialog Management](016-window-dialog-management.md)
- [Avalonia Docs: View Locator](https://docs.avaloniaui.net/docs/concepts/view-locator)
