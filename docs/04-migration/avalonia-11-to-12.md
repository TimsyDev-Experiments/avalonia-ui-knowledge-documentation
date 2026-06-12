---
topic: migration
estimated: 15 min read
researched: 2026-06-11
avalonia-version: 12.0.4
---

# Avalonia 11 → 12 Migration Guide

**What you'll learn:** Step-by-step migration of an Avalonia 11.3.x project to 12.0.4, covering packages, API changes, bindings, and project file updates.

**Prerequisites:** [001 — Project Setup](/docs/02-tutorials/basics/001-project-setup.md)

---

## Phase 1: Project File

### Target framework

```diff
-<TargetFramework>netstandard2.0</TargetFramework>
-<TargetFramework>net6.0</TargetFramework>
-<TargetFramework>net8.0</TargetFramework>
+<TargetFramework>net10.0</TargetFramework>  <!-- or net8.0+ -->
```

Avalonia 12 dropped .NET Framework and .NET Standard support.

### Package versions

```diff
-<PackageReference Include="Avalonia" Version="11.3.12" />
+<PackageReference Include="Avalonia" Version="12.0.4" />

-<PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.12" />
+<PackageReference Include="Avalonia.Themes.Fluent" Version="12.0.4" />

-<PackageReference Include="Avalonia.Diagnostics" Version="11.3.12" />
+<PackageReference Include="AvaloniaUI.DiagnosticsSupport" Version="2.2.1" />
```

### Removed packages

```diff
-<PackageReference Include="Avalonia.Direct2D1" />
+<!-- Remove entirely; use Skia -->

-<PackageReference Include="Avalonia.Browser.Blazor" />
+<!-- Remove; use Avalonia.Browser -->

-<PackageReference Include="Avalonia.Tizen" />
+<!-- Remove; no longer supported -->
```

---

## Phase 2: Startup Code

### Program.cs

```diff
 public static AppBuilder BuildAvaloniaApp() =>
     AppBuilder.Configure<App>()
         .UsePlatformDetect()
-        .WithInterFont()
-        .LogToTrace();
+        .WithInterFont()
+        .LogToTrace()
+        .UseSkia()
+        .UseHarfBuzz();
```

If you're using `UsePlatformDetect()`, Skia and HarfBuzz are configured automatically. Only add them explicitly if you were previously using `UseSkia()` alone.

### DevTools

```diff
-using Avalonia.Diagnostics;
+using AvaloniaUI.DiagnosticsSupport;

-this.AttachDevTools();
+this.AttachDeveloperTools();
```

---

## Phase 3: Bindings

### Compiled bindings are now default

```diff
 <!-- Not needed in v12 — it's the default -->
-<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
```

If you need runtime-resolved bindings, use `ReflectionBinding` explicitly:

```diff
-<TextBlock Text="{Binding DynamicProperty}" />
+<TextBlock Text="{ReflectionBinding DynamicProperty}" />
```

### Binding API in C#

```diff
-var binding = new Binding("SomeProperty");
+var binding = new ReflectionBinding("SomeProperty");

-var compiled = CompiledBinding.Create<Person, string>(p => p.Name);
+var compiled = CompiledBinding.Create((Person p) => p.Name);
```

The `IBinding` interface and `InstancedBinding` class were removed. Use `BindingBase` or `BindingExpressionBase`.

---

## Phase 4: Clipboard & Drag-Drop

### Clipboard

```diff
-var data = new DataObject();
-data.Set(DataFormats.Text, "text");
+var item = new DataTransferItem();
+item.Set(DataFormat.Text, "text");
+var data = new DataTransfer();
+data.Add(item);

-await clipboard.SetDataObjectAsync(data);
+await clipboard.SetDataAsync(data);

-var text = await clipboard.GetTextAsync();
+var text = await clipboard.TryGetTextAsync();
```

### Drag-drop

```diff
-var result = await DragDrop.DoDragDrop(...);
+var result = await DragDrop.DoDragDropAsync(...);

-DragEventArgs.Data
+DragEventArgs.DataTransfer
```

---

## Phase 5: Gesture Events

```diff
-<Button Gestures.Pinch="OnPinch" />
+<Button Pinch="OnPinch" />
```

All gesture events moved from the `Gestures` class to `InputElement`. The `Gestures` class is no longer public.

---

## Phase 6: Focus Events

```diff
-private void OnGotFocus(object? sender, GotFocusEventArgs e)
+private void OnGotFocus(object? sender, FocusChangedEventArgs e)

-private void OnLostFocus(object? sender, RoutedEventArgs e)
+private void OnLostFocus(object? sender, FocusChangedEventArgs e)
```

---

## Phase 7: Window Decoration

```diff
 <!-- v11 custom chrome approach: TitleBar, CaptionButtons, ChromeOverlayLayer -->
 <!-- v12: use WindowDrawnDecorations instead -->
+<Window xmlns:chrome="using:Avalonia.Controls.Chrome">
+    <chrome:WindowDrawnDecorations />
+</Window>
```

`Window.ExtendClientAreaChromeHints` was removed. Use `WindowDecorations` + `ExtendClientAreaToDecorationsHint`.

---

## Phase 8: TopLevel Access

```diff
 // Broken in v12
-TopLevel topLevel = (TopLevel)visual;
+TopLevel? topLevel = TopLevel.GetTopLevel(visual);
```

---

## Phase 9: Validation

```diff
 // No longer needed in v12 — happens automatically
-protected override void UpdateDataValidation(...)
-{
-    if (property == MyProperty)
-        DataValidationErrors.SetError(this, error);
-}
+// Remove entire override
```

---

## Phase 10: Animations on Hidden Controls

If you have animations that must run while the control is hidden:

```diff
 <Animation Duration="0:0:1"
-            FillMode="Forward">
+            FillMode="Forward"
+            PlaybackBehavior="Always">
```

---

## Verification Checklist

- [ ] App builds without errors
- [ ] All `{Binding}` have `x:DataType` in scope
- [ ] No `Avalonia.Diagnostics` package references remain
- [ ] DevTools open with F12
- [ ] Clipboard operations work
- [ ] Drag-drop between controls works
- [ ] Theme switching still functions
- [ ] No `IDataObject` or `DataFormats` references in code
- [ ] No `Gestures.` prefix in XAML
- [ ] No direct `(TopLevel)` casts in code

---

## See Also

- [Quick reference: Key breaking changes](../01-quick-refs/avalonia-12-breaking-changes.md)
- [Avalonia Official: Breaking Changes in 12](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)
- [Plugin: Avalonia 12 migration guide](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/68-avalonia-12-migration-guide.md)
