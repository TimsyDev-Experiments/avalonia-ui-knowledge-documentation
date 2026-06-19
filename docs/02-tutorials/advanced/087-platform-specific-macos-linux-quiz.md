---
tier: advanced
topic: platform
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 087 — Platform-Specific: macOS & Linux — Quiz

**Prerequisites:** [087-core](087-platform-specific-macos-linux.md)

---

### Q1: What native technology does the Avalonia macOS backend use, and what target framework is required by default?

<details>
<summary>Answer</summary>

Avalonia uses its own native Objective-C++ backend (`libAvaloniaNative.dylib`) via MicroCom interop. The default target framework is `net10.0` (not `net10.0-macos`), allowing cross-compilation from Windows or Linux.

</details>

---

### Q2: A macOS user reports "About Avalonia" appears in the application menu. How do you replace it with your own About dialog?

<details>
<summary>Answer</summary>

Define a `NativeMenu` on the Application in `App.axaml` with your own `NativeMenuItem`, and handle the `Click` event to show your About window. Avalonia auto-generates a default menu with "About Avalonia" only if you don't define your own.

</details>

---

### Q3: Which modifier should you use in `Gesture` strings to represent the Command (⌘) key on macOS?

<details>
<summary>Answer</summary>

`Meta`. Example: `Gesture="Meta+S"` maps to ⌘S on macOS.

</details>

---

### Q4: True or False: Setting `NativeDock.Menu` on the Application has an effect on all platforms.

<details>
<summary>Answer</summary>

False. `NativeDock.Menu` only affects macOS. On other platforms, the property is silently ignored.

</details>

---

### Q5: Write `Info.plist` entries to register your app as a handler for `.sketch` files.

<details>
<summary>Answer</summary>

```xml
<key>CFBundleDocumentTypes</key>
<array>
  <dict>
    <key>CFBundleTypeName</key><string>Sketch</string>
    <key>CFBundleTypeExtensions</key><array><string>sketch</string></array>
    <key>CFBundleTypeRole</key><string>Viewer</string>
    <key>LSHandlerRank</key><string>Default</string>
  </dict>
</array>
```

</details>

---

### Q6: What is the maximum length of `CFBundleName` in `Info.plist`, and which platform UI element uses it?

<details>
<summary>Answer</summary>

15 characters. It controls the application name in the menu bar and the "Quit" menu item. Use `CFBundleDisplayName` (no limit) for the Dock tooltip and Finder.

</details>

---

### Q7: Your macOS app uses a custom `NativeControlHost` that returns an NSView. What target framework change is required?

<details>
<summary>Answer</summary>

Change to `net10.0-macos` (macOS-specific TFM), which requires building on a Mac. The default `net10.0` target does not expose macOS APIs needed to create NSView instances.

</details>

---

### Q8: What window system does Avalonia use on Linux by default, and what is coming in 12.0?

<details>
<summary>Answer</summary>

X11 is the default and current window system. Wayland support is coming in Avalonia 12.0.

</details>

---

### Q9: What three libraries must be installed on WSL 2 for Avalonia to run?

<details>
<summary>Answer</summary>

`libice6`, `libsm6`, and `libfontconfig1`.

```bash
sudo apt install libice6 libsm6 libfontconfig1
```

</details>

---

### Q10: Which accessibility protocol does Avalonia use on Linux, and what tools can test it?

<details>
<summary>Answer</summary>

AT-SPI2 (Assistive Technology Service Provider Interface) over D-Bus. Test with **Orca** (screen reader) and **Accerciser** (interactive accessibility tree explorer).

</details>

---

### Q11: How do you make a `NativeMenuItem` appear enabled vs greyed out?

<details>
<summary>Answer</summary>

Each `NativeMenuItem` requires either a `Click` event handler or a `Command` binding. Without either, the item is greyed out. With a `Command`, the item's enabled state follows `ICommand.CanExecute`.

</details>

---

### Q12: Your Linux app targets a framebuffer device without X11. What `X11PlatformOptions` setting enables this?

<details>
<summary>Answer</summary>

```csharp
.With(new X11PlatformOptions
{
    UseFb = true
})
```

For DRM/KMS, use `RenderingMode = new[] { X11RenderingMode.Drm }`.

</details>
