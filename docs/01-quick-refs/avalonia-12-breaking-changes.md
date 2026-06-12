---
topic: migration
estimated: 3 min read
researched: 2026-06-12
avalonia-version: 12.0.4
---

# Avalonia 11 → 12 Key Breaking Changes

## .NET Requirements

| v11 | v12 |
|---|---|
| .NET Framework, .NET Standard, .NET 6+ | **.NET 8+ only** (.NET 10 recommended) |
| `netstandard2.0` | Change to `net10.0` |

## Package Changes

| v11 | v12 |
|---|---|
| `Avalonia.Diagnostics` | **Removed** → use `AvaloniaUI.DiagnosticsSupport` |
| `Avalonia.Direct2D1` | **Removed** → use `Avalonia.Skia` |
| `Avalonia.Browser.Blazor` | **Removed** → use `Avalonia.Browser` |
| `Avalonia.Tizen` | **Removed** (no longer supported) |

## API Changes

| Area | v11 → v12 |
|---|---|
| **Bindings** | `{Binding}` = `CompiledBinding` by default. `IBinding` → `BindingBase`. `InstancedBinding` → `BindingExpressionBase`. |
| **Clipboard** | `IDataObject` removed. Use `DataTransfer` / `DataTransferItem`. `DoDragDrop` → `DoDragDropAsync`. |
| **DevTools** | `AttachDevTools()` → `AttachDeveloperTools()` |
| **Window state** | `Window.WindowState` is now a direct property (was styled). Can't set from styles. |
| **Focus** | `GotFocus` / `LostFocus` use `FocusChangedEventArgs` (was `GotFocusEventArgs` / `RoutedEventArgs`) |
| **Gestures** | Remove `Gestures.` prefix: `Gestures.Pinch` → `Pinch` |
| **TopLevel** | Casting to `TopLevel` directly doesn't work. Use `TopLevel.GetTopLevel(visual)`. |
| **Validation** | No more `UpdateDataValidation` override needed. Binding plugins removed. |
| **Animations** | Animations stop on hidden controls by default. Use `PlaybackBehavior="Always"` to override. |

## Startup Changes

```diff
- .UsePlatformDetect()
+ .UsePlatformDetect()
+ .UseSkia()          // required when not using UsePlatformDetect()
+ .UseHarfBuzz()      // required text shaper
```

```diff
- AttachDevTools();
+ AttachDeveloperTools();
```

## See Also

- [Migration guide](../04-migration/avalonia-11-to-12.md)
- [Full breaking changes list](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)
