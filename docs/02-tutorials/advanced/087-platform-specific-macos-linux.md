---
tier: advanced
topic: platform
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 087 — Platform-Specific: macOS & Linux

**What you'll learn:** macOS and Linux platform specifics — native menus, app identity, keyboard mapping, URL handlers, file associations, X11, WSL 2, and accessibility on both platforms.

**Prerequisites:** [042 — Multi-Targeting](042-multi-targeting-desktop-browser-mobile.md)

---

## 1. How Avalonia runs on macOS

Avalonia uses its own native Objective-C++ backend via `libAvaloniaNative.dylib`. It does NOT use the .NET macOS workload — you target `net10.0` (not `net10.0-macos`) and can cross-compile from Windows or Linux.

The trade-off: only Avalonia-exposed APIs are available. For full macOS API access, change to `net10.0-macos` (requires building on macOS).

### Specifying a custom native library path

```csharp
.With(new AvaloniaNativePlatformOptions
{
    AvaloniaNativeLibraryPath = "/path/to/libAvaloniaNative.dylib"
})
```

---

## 2. macOS: Application name and identity

| Location | Source |
|---|---|
| Menu bar / "Quit" | `CFBundleName` (Info.plist, max 15 chars) or `Application.Name` (unbundled) |
| Dock tooltip | `CFBundleDisplayName` (Info.plist), falls back to `CFBundleName` |
| "About" item | Your `NativeMenuItem` header text |
| Window title bar | `Window.Title` property |

```xml
<Application xmlns="https://github.com/avaloniaui"
             x:Class="MyApp.App"
             Name="My Application">
```

```xml
<!-- Info.plist -->
<key>CFBundleName</key><string>My App</string>
<key>CFBundleDisplayName</key><string>My Application</string>
```

---

## 3. macOS: Native menu bar

### Application menu (App.axaml)

```xml
<Application.NativeMenu.Menu>
  <NativeMenu>
    <NativeMenuItem Header="About My Application..." Click="About_OnClick" />
    <NativeMenuItem Header="Preferences..." Click="Preferences_OnClick"
                    Gesture="Meta+Comma" />
  </NativeMenu>
</Application.NativeMenu.Menu>
```

Avalonia auto-appends "Quit App Name" (⌘Q) after your items.

### Window menus (Window.axaml)

```xml
<Window.NativeMenu.Menu>
  <NativeMenu>
    <NativeMenuItem Header="File">
      <NativeMenu>
        <NativeMenuItem Header="Open..." Gesture="Meta+O"
                        Command="{Binding OpenCommand}" />
        <NativeMenuItem Header="Save" Gesture="Meta+S"
                        Command="{Binding SaveCommand}" />
      </NativeMenu>
    </NativeMenuItem>
    <NativeMenuItem Header="Edit">
      <NativeMenu>
        <NativeMenuItem Header="Cut" Gesture="Meta+X"
                        Command="{Binding CutCommand}" />
      </NativeMenu>
    </NativeMenuItem>
  </NativeMenu>
</Window.NativeMenu.Menu>
```

A menu named "Edit" auto-receives standard macOS text editing features.

### Dock menu (App.axaml)

```xml
<Application.NativeDock.Menu>
  <NativeMenu>
    <NativeMenuItem Header="New Window" Click="NewWindow_OnClick" />
    <NativeMenuItemSeparator />
    <NativeMenuItem Header="Show Main Window" Click="ShowMainWindow_OnClick" />
  </NativeMenu>
</Application.NativeDock.Menu>
```

Only applies on macOS.

---

## 4. macOS: Keyboard mapping

| Avalonia modifier | macOS key | Symbol |
|---|---|---|
| `Meta` | Command | ⌘ |
| `Control` | Control | ⌃ |
| `Shift` | Shift | ⇧ |
| `Alt` | Option | ⌥ |

```xml
<NativeMenuItem Header="Preferences" Gesture="Meta+Comma" />
```

`PlatformHotkeyConfiguration` adapts common shortcuts (copy/paste/cut use ⌘ on macOS):

```csharp
var hotkeys = this.GetPlatformSettings()?.HotkeyConfiguration;
if (hotkeys?.Copy.Any(g => g.Matches(e)) == true)
    HandleCopy();
```

---

## 5. macOS: URL protocols and file associations

### URL handler

```xml
<!-- Info.plist -->
<key>CFBundleURLTypes</key>
<array>
  <dict>
    <key>CFBundleURLName</key><string>MyApp</string>
    <key>CFBundleTypeRole</key><string>Viewer</string>
    <key>CFBundleURLSchemes</key>
    <array><string>myapp</string></array>
  </dict>
</array>
```

### File type association

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

---

## 6. macOS: NativeControlHost

```csharp
public class MacNativeView : NativeControlHost
{
    protected override IPlatformHandle CreateNativeControlCore(
        IPlatformHandle parent)
    {
        if (OperatingSystem.IsMacOS())
        {
            // Create and return NSView handle
        }
        return base.CreateNativeControlCore(parent);
    }
}
```

Requires `net10.0-macos` target framework for access to macOS APIs.

---

## 7. How Avalonia runs on Linux

Avalonia targets **X11** directly on Linux. Wayland support arrives in Avalonia 12.0.

### WSL 2

```bash
sudo apt install libice6 libsm6 libfontconfig1
```

---

## 8. Linux: Accessibility (AT-SPI2)

Avalonia exposes the accessibility tree via **AT-SPI2** over D-Bus. Works automatically when a D-Bus session bus and accessibility service are present.

### Testing with Orca

```bash
sudo apt install orca
orca &
```

### Testing with Accerciser

```bash
sudo apt install accerciser
accerciser &
```

---

## 9. Platform services comparison

| Service | macOS | Linux |
|---|---|---|
| Clipboard | NSPasteboard | X11 selections + clipboard |
| File dialogs | Native NSSavePanel/NSOpenPanel | Native GTK dialogs (if available) or fallback |
| Drag & drop | NSDragOperation | XDND / XDnD |
| Launcher | `NSWorkspace` | `xdg-open` |
| System tray | NSStatusBar | X11 tray (freedesktop) |
| Transparency | `Transparent` only | Depends on compositor |
| Accessibility | NSAccessibility protocol | AT-SPI2 |

---

## Key Takeaways

- **macOS** uses a native Objective-C++ backend (`libAvaloniaNative.dylib`) — cross-compile from any OS using `net10.0`
- **App name** on macOS comes from `CFBundleName` (bundled) or `Application.Name` (unbundled)
- **NativeMenu** on macOS renders as the system menu bar; "Edit" menu auto-receives text features
- **Dock menu** is set via `NativeDock.Menu` in `App.axaml` (macOS only)
- **Keyboard**: `Meta` = ⌘ Command; use `PlatformHotkeyConfiguration` for platform-adaptive shortcuts
- **URL handlers** and **file associations** use standard `Info.plist` entries
- **Linux** targets X11; WSL 2 requires `libice6 libsm6 libfontconfig1`
- **Accessibility** on Linux uses AT-SPI2 — test with Orca (screen reader) or Accerciser (tree explorer)
- **Wayland** support is coming in Avalonia 12.0

---

## See Also

- [macOS deployment](https://docs.avaloniaui.net/docs/deployment/macos)
- [Linux deployment](https://docs.avaloniaui.net/docs/deployment/linux)
- [Embedded Linux](https://docs.avaloniaui.net/docs/platform-specific-guides/embedded-linux)
- [042 — Multi-Targeting](042-multi-targeting-desktop-browser-mobile.md)
- [NativeMenu](https://docs.avaloniaui.net/controls/menus/nativemenu)
