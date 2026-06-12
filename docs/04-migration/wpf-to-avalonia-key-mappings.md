---
topic: migration
estimated: 5 min read
researched: 2026-06-12
avalonia-version: 12.0.4
---

# WPF → Avalonia Key Mappings

**What you'll learn:** Quick mapping table for common WPF concepts to their Avalonia equivalents.

---

## Control Mappings

| WPF | Avalonia | Notes |
|---|---|---|
| `Window` | `Window` | Similar API, but `ShowDialog` returns `Task<T?>`. No `this.Owner` — use `TopLevel.GetTopLevel()`. |
| `UserControl` | `UserControl` | Same concept |
| `TextBlock` | `TextBlock` | Same |
| `TextBox` | `TextBox` | Same |
| `Button` | `Button` | Same |
| `ListBox` | `ListBox` | Same |
| `ListView` | `ListBox` | No `ListView` — use `ListBox` or `DataGrid` |
| `GridView` | `DataGrid` | Avalonia has a rich `DataGrid` |
| `TreeView` | `TreeView` | Similar |
| `ComboBox` | `ComboBox` | Same |
| `Expander` | `Expander` | Same |
| `ScrollViewer` | `ScrollViewer` | Same |
| `Border` | `Border` | Same |
| `StackPanel` | `StackPanel` | Same |
| `WrapPanel` | `WrapPanel` | Built-in (not in an assembly) |
| `DockPanel` | `DockPanel` | Built-in |
| `Grid` | `Grid` | Same |
| `Canvas` | `Canvas` | Same |
| `Frame` / `Page` | `ContentControl` + `NavigationService` | No `Frame`/`Page`; use content switching |
| `Menu` | `Menu` | Same |
| `ContextMenu` | `ContextMenu` | Same |
| `ToolTip` | `ToolTip` | Same |
| `Image` | `Image` | Same |
| `DataGrid` | `DataGrid` | Avalonia's DataGrid is similar |
| `ProgressBar` | `ProgressBar` | Same |
| `Slider` | `Slider` | Same |
| `PasswordBox` | `PasswordBox` | Same |
| `Calendar` / `DatePicker` | `DatePicker` / `Calendar` | Same |
| `WebBrowser` | `WebView` / `NativeWebView` | `WebView` (CEF) or `NativeWebView` (Pro) |

---

## Property & API Mappings

| WPF | Avalonia |
|---|---|
| `DependencyProperty` | `StyledProperty` (styleable) or `DirectProperty` (performant) |
| `DependencyProperty.Register` | `AvaloniaProperty.Register` |
| `PropertyChangedCallback` | `AffectsRender`, `AffectsMeasure`, `Property.Changed.AddClassHandler` |
| `ICommand` | `ICommand` (same interface) |
| `Visibility.Visible/Collapsed/Hidden` | `IsVisible` (bool) — no enum |
| `IsEnabled` | `IsEnabled` (same) |
| `Tag` | `Tag` (same, `object?`) |
| `ToolTip` attached property | `ToolTip.Tip` attached property |
| `ContextMenu` | `ContextMenu` (same) |
| `Focusable` | `Focusable` (same) |
| `TabIndex` | `TabIndex` (same) |
| `Style.Triggers` | `Style.Animations` (XAML animations) or pseudo-classes |
| `DataTrigger` | Style classes + `:true` / `:false` pseudo-classes |
| `MultiBinding` | `MultiBinding` (same concept) |
| `PriorityBinding` | `PriorityBinding` (same concept) |

---

## Binding Mappings

| WPF | Avalonia |
|---|---|
| `{Binding ElementName=foo, Path=Text}` | `{Binding #foo.Text}` |
| `{Binding RelativeSource={RelativeSource Self}}` | `{Binding $self.Property}` |
| `{Binding RelativeSource={RelativeSource AncestorType=Grid}}` | `{Binding $parent[Grid].Property}` |
| `{Binding RelativeSource={RelativeSource TemplatedParent}}` | `{TemplateBinding Property}` (OneWay only) |
| `UpdateSourceTrigger=PropertyChanged` | Default behavior (not needed) |
| `Mode=TwoWay` on `TextBlock.Text` | `Mode=OneWay` — TextBlock is read-only in Avalonia |
| `Visibility="{Binding IsActive, Converter=...}"` | `IsVisible="{Binding IsActive}"` (bool, no converter) |
| `ValidatesOnDataErrors=True` | `DataValidationErrors` — automatic with ObservableValidator |
| `x:Static` | `{x:Static}` (same) |

---

## Styling Mappings

| WPF | Avalonia |
|---|---|
| `<Style TargetType="Button">` | `<Style Selector="Button">` |
| `<Setter Property="..."/>` | `<Setter Property="..." Value="..."/>` (same) |
| `BasedOn="{StaticResource ...}"` | `BasedOn="{StaticResource ...}"` (same) |
| `Trigger` on properties | Style `Selector` + pseudo-classes |
| `DataTrigger` | `:true` / `:false` pseudo-classes for bool bindings |
| `ControlTemplate` | `ControlTemplate` (same) |
| `TemplateBinding` | `TemplateBinding` (same, OneWay only) |
| `ControlTheme` | **New in Avalonia** — separates theme from style |
| `VisualStateManager` | Style pseudo-classes in `ControlTheme` |
| `SystemColors`, `SystemFonts` | Theme resources (`{DynamicResource ...}`) |

---

## Key Differences to Watch

1. **No XAML reader/writer** — Avalonia builds the visual tree at compile time via XamlX
2. **No triggers** — use pseudo-class selectors instead
3. **No `IValueConverter` culture** — uses `CultureInfo.CurrentCulture` by default
4. **Layout rounding** — `UseLayoutRounding` is enabled by default
5. **No `Application.Current.Resources` in code-behind** — use `Application.Current!.TryFindResource()`
6. **Routed events** — similar but use `AddHandler` instead of `+=` for handling in parent
7. **Cross-platform** — P/Invoke calls must be platform-guarded or abstracted

---

## See Also

- [Avalonia official: WPF Migration Guide](https://docs.avaloniaui.net/docs/migration/wpf)
- [Plugin: WPF to Avalonia Modern UI Conversion Index](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/64-wpf-to-avalonia-modern-ui-conversion-index.md)
