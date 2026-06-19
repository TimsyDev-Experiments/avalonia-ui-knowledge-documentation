---
topic: conventions
estimated: 2 min read
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Q08 — Naming Conventions

## XAML files

| Convention | Example |
|---|---|
| PascalCase for filenames | `MainWindow.axaml`, `LoginView.axaml` |
| Suffix Views with `View` | `DashboardView.axaml`, `SettingsView.axaml` |
| Suffix Windows with `Window` | `MainWindow.axaml`, `AboutWindow.axaml` |
| Suffix Styles with `Styles` | `ButtonStyles.axaml`, `ThemeStyles.axaml` |
| Suffix Dictionaries with `Resources` | `Colors.axaml`, `Icons.axaml` |

## Code-behind and classes

| Convention | Example |
|---|---|
| Partial class matches filename | `MainWindow.axaml` → `MainWindow` |
| ViewModels end with `ViewModel` | `MainViewModel`, `LoginViewModel` |
| Models are plain nouns | `User`, `Product`, `Order` |
| Services end with `Service` | `IDialogService`, `FileService` |
| Converters end with `Converter` | `BoolToVisibilityConverter` |
| Custom controls end with control type | `NumericUpDown`, `ColorPicker` |

## x:Name / x:Key

| Prefix | For | Example |
|---|---|---|
| `Part_` | Template parts (required) | `PART_ScrollBar`, `PART_ContentHost` |
| `_` | Private code-behind fields | `_myButton`, `_viewModel` |
| Descriptive suffix | Named XAML elements | `LoginButton`, `TitleText`, `MainPanel` |

## Resource keys

| Pattern | Example |
|---|---|
| `{Control}{Property}` | `ButtonBackground`, `TextBlockForeground` |
| `{Theme}Color{Index}` | `FluentColorBlue`, `FluentColorGray100` |
| `{Size}Spacing` | `SmallSpacing`, `LargeSpacing` |
