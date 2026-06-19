---
tier: advanced
topic: platform
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 086 — Platform-Specific: Windows — Quiz

**Prerequisites:** [086-core](086-platform-specific-windows.md)

---

### Q1: Which two APIs does Avalonia use for rendering on Windows by default?

<details>
<summary>Answer</summary>

SkiaSharp backed by Direct3D 11. There is also an OpenGL (WGL) mode and a software fallback.

</details>

---

### Q2: What window property must be set for Mica or AcrylicBlur to take effect?

<details>
<summary>Answer</summary>

`Background="Transparent"`. Without it, the opaque background covers the transparency effect.

</details>

---

### Q3: How do you check which transparency level is actually active at runtime?

<details>
<summary>Answer</summary>

Read `window.ActualTransparencyLevel`. It returns the `WindowTransparencyLevel` enum value that is currently applied.

</details>

---

### Q4: Write the attached property used to mark a region as a draggable title bar in a custom-chrome window.

<details>
<summary>Answer</summary>

`WindowDecorationProperties.ElementRole="TitleBar"` on the element that should act as the drag region.

</details>

---

### Q5: True or False: On Windows 10, the native title bar automatically switches to dark when `RequestedThemeVariant` is `Dark`.

<details>
<summary>Answer</summary>

False. Windows 10 does not provide an official API for dark title bars. It works automatically on Windows 11. On Windows 10, you must use a custom title bar or the undocumented `DwmSetWindowAttribute` P/Invoke.

</details>

---

### Q6: What is the DWM attribute constant for immersive dark mode, and which Windows build supports it?

<details>
<summary>Answer</summary>

Attribute value `20` (`DWMWA_USE_IMMERSIVE_DARK_MODE`). Supported on Windows 10 build 18985 and later. It is undocumented and may change.

</details>

---

### Q7: Why might a ListBox inside a VirtualizingStackPanel show blurry text at 150% scaling?

<details>
<summary>Answer</summary>

At non-integer scaling factors, sub-pixel positioning can cause blur. Use `UseLayoutRounding="True"` on the element to snap to device-pixel boundaries.

</details>

---

### Q8: What package is required for WinForms interoperability, and what method replaces `StartWithClassicDesktopLifetime`?

<details>
<summary>Answer</summary>

The `Avalonia.Win32.Interoperability` package. Use `SetupWithoutStarting()` instead of `StartWithClassicDesktopLifetime` when hosting inside a WinForms application.

</details>

---

### Q9: Name two limitations of `NativeControlHost` for embedding Win32 controls.

<details>
<summary>Answer</summary>

1. Native views always render on top of Avalonia content (no interleaved z-order).
2. No transparency support — native views cannot have transparent backgrounds showing Avalonia content behind them.
3. No render transforms (rotation, scale) apply to native views.
4. Clipping is limited to rectangular host bounds only.

</details>

---

### Q10: Your signed application still triggers SmartScreen on Windows. What certificate type would bypass it immediately?

<details>
<summary>Answer</summary>

An EV (Extended Validation) certificate or Microsoft Trusted Signing. OV certificates require 3–6 months of reputation building.

</details>

---

### Q11: What is the correct approach to load a custom native library (`mylib.dll`) for Windows x64 in a cross-platform Avalonia app?

<details>
<summary>Answer</summary>

Place the library in `runtimes/win-x64/native/mylib.dll` and use `[LibraryImport]` or `[DllImport]` in the shared code. The .NET runtime resolves the correct platform library automatically.

```xml
<NativeLibrary Include="runtimes\win-x64\native\mylib.dll" />
```

</details>

---

### Q12: How do you ensure a standalone Avalonia window opened from a WinForms application receives keyboard input?

<details>
<summary>Answer</summary>

Register a `WinFormsAvaloniaMessageFilter` in `Program.cs` before `Application.Run()`:

```csharp
Application.AddMessageFilter(new WinFormsAvaloniaMessageFilter());
```

This dispatches keyboard messages to top-level Avalonia windows while leaving embedded hosts and native WinForms controls unaffected.

</details>
