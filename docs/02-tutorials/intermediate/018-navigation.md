---
tier: intermediate
topic: navigation
estimated: 10 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 018 — Navigation Patterns

**What you'll learn:** Implement View-first navigation with a ViewModel-driven approach using `ContentControl` and DI.

**Prerequisites:** [007 — ObservableObject & ObservableProperty](../basics/007-observable-object-property.md)

---

## 1. The navigation service

```csharp
// Services/INavigationService.cs
public interface INavigationService
{
    ViewModelBase? CurrentView { get; }
    void NavigateTo<T>() where T : ViewModelBase;
    void NavigateTo(ViewModelBase viewModel);
    void GoBack();
}
```

```csharp
// Services/NavigationService.cs
public class NavigationService : INavigationService
{
    private readonly Stack<ViewModelBase> _history = new();
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public ViewModelBase? CurrentView { get; private set; }

    public void NavigateTo<T>() where T : ViewModelBase
    {
        var vm = _services.GetRequiredService<T>();
        NavigateTo(vm);
    }

    public void NavigateTo(ViewModelBase viewModel)
    {
        if (CurrentView is not null)
            _history.Push(CurrentView);

        CurrentView = viewModel;
    }

    public void GoBack()
    {
        if (_history.Count > 0)
            CurrentView = _history.Pop();
    }
}
```

---

## 2. The shell ViewModel

```csharp
public partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;

    public ShellViewModel(INavigationService navigation)
    {
        _navigation = navigation;

        // Track navigation changes
        _navigation.CurrentViewChanged += (vm) =>
            OnPropertyChanged(nameof(CurrentView));
    }

    public ViewModelBase? CurrentView =>
        _navigation.CurrentView;

    [RelayCommand]
    private void GoToSettings() =>
        _navigation.NavigateTo<SettingsViewModel>();

    [RelayCommand]
    private void GoBack() =>
        _navigation.GoBack();
}
```

---

## 3. The shell view

```xml
<!-- Views/ShellView.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:vm="using:MyApp.ViewModels"
        xmlns:views="using:MyApp.Views"
        x:DataType="vm:ShellViewModel">
  <Grid>
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto" />
      <RowDefinition Height="*" />
    </Grid.RowDefinitions>

    <!-- Navigation bar -->
    <StackPanel Orientation="Horizontal" Spacing="8" Margin="12">
      <Button Content="←" Command="{Binding GoBackCommand}" />
      <Button Content="Settings" Command="{Binding GoToSettingsCommand}" />
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

---

## 4. View locator (matches ViewModel to View)

```csharp
// Views/ViewLocator.cs
using Avalonia.Controls;
using Avalonia.Controls.Templates;

public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Type> _mappings = new()
    {
        { typeof(HomeViewModel), typeof(HomeView) },
        { typeof(SettingsViewModel), typeof(SettingsView) },
        { typeof(AboutViewModel), typeof(AboutView) },
    };

    public Control? Build(object? param)
    {
        if (param is null) return null;

        var viewModelType = param.GetType();
        if (!_mappings.TryGetValue(viewModelType, out var viewType))
            throw new InvalidOperationException($"No view for {viewModelType}");

        var view = (Control)Activator.CreateInstance(viewType)!;
        view.DataContext = param;
        return view;
    }

    public bool Match(object? data) => data is ViewModelBase;
}
```

---

## 5. Convention-based view locator

```csharp
public Control? Build(object? param)
{
    if (param is null) return null;

    var viewModelType = param.GetType();

    // Convention: "MyApp.ViewModels.FooViewModel" → "MyApp.Views.FooView"
    var viewName = viewModelType.FullName!
        .Replace("ViewModels", "Views")
        .Replace("ViewModel", "View");

    var viewType = Type.GetType(viewName);
    if (viewType is null)
        throw new InvalidOperationException($"No view found for {viewModelType}");

    var view = (Control)Activator.CreateInstance(viewType)!;
    view.DataContext = param;
    return view;
}
```

---

## 6. Navigation with parameters

```csharp
public void NavigateTo<T>(Action<T> configure) where T : ViewModelBase
{
    var vm = _services.GetRequiredService<T>();
    configure(vm);
    NavigateTo(vm);
}

// Usage
_navigation.NavigateTo<DetailViewModel>(vm =>
{
    vm.ItemId = selectedItem.Id;
});
```

---

## Key Takeaways

- Use `ContentControl` + `DataTemplate` as the navigation surface
- A `ViewLocator` maps ViewModel types to View types
- Register views as transient in DI (new instance per navigation)
- Use `INavigationService` to keep navigation logic out of ViewModels
- Convention-based locators reduce registration boilerplate

---

## See Also

- [015 — Item Lists](015-item-lists.md)
- [016 — Window & Dialog Management](016-window-dialog-management.md)
- [018V — Navigation Patterns (verbose companion)](018-navigation-verbose.md)
- [018E — Navigation Patterns (examples)](018-navigation-examples.md)
- [Avalonia Docs: View Locator](https://docs.avaloniaui.net/docs/concepts/view-locator)
