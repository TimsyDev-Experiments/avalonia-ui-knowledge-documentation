---
tier: advanced
topic: platform
estimated: 30 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 087 — Platform-Specific: macOS & Linux — Verbose

**Prerequisites:** [087-core](087-platform-specific-macos-linux.md)

---

## 1. macOS: Native backend architecture

Avalonia's macOS backend is written in Objective-C++ (`.mm` files) at `native/Avalonia.Native/src/OSX/`. The .NET side communicates through MicroCom, a lightweight COM-style interop layer, using an IDL file at `src/Avalonia.Native/avn.idl`.

**Compiling the native layer:**

```bash
# Open the Xcode project
open native/Avalonia.Native.OSX.xcodeproj
# Build with ⌘B, then use the output dylib:
```
```csharp
.With(new AvaloniaNativePlatformOptions
{
    AvaloniaNativeLibraryPath = "/path/to/libAvaloniaNative.dylib"
})
```

---

## 2. macOS: Running as an app bundle during development

Some features (Xcode Accessibility Inspector, URL handlers) require your app to run as a `.app` bundle:

```xml
<!-- .csproj -->
<OutputPath>bin\$(Configuration)\$(Platform)\MyApp.app/Contents/MacOS</OutputPath>
<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
<UseAppHost>true</UseAppHost>
```

Place a valid `Info.plist` in the `Contents` directory.

---

## 3. macOS: Native menu details

### Application menu auto-generation

If you don't define a `NativeMenu` on the Application, Avalonia creates a default with "About Avalonia". Always define your own to replace it.

### Menu item enabled state

Each `NativeMenuItem` needs either a `Click` handler or a `Command` binding. Without either, the item appears greyed out.

### About dialog

The About item is a regular `NativeMenuItem` with no special behavior:

```csharp
private void About_OnClick(object? sender, EventArgs e)
{
    var about = new AboutWindow();
    about.ShowDialog(this.GetMainWindow());
}
```

Convention: first item, text "About My Application..." with ellipsis.

### Dock menu at runtime

```csharp
var dockMenu = NativeDock.GetMenu(this);
dockMenu?.Items.Insert(0, new NativeMenuItem("Dynamic Item"));
```

---

## 4. macOS: Standard shortcuts

| Action | Shortcut | Auto? |
|---|---|---|
| Quit | ⌘Q | Yes |
| Minimize | ⌘M | Yes |
| Hide | ⌘H | Yes |
| Full Screen | ⌘⌃F | Yes |
| Close Window | ⌘W | Bind manually |
| Preferences | ⌘, | Bind manually |
| Find | ⌘F | Bind manually |

---

## 5. macOS: Mac Catalyst alternative

Mac Catalyst runs iOS apps on macOS. It requires a Mac to build and depends on the `maccatalyst` .NET workload — no cross-compilation from Windows/Linux.

Use it when your app depends heavily on UIKit APIs or when embedding Avalonia in a MAUI hybrid app. For most Avalonia apps, the default macOS backend is recommended.

---

## 6. macOS: NSWindow handle

```csharp
var handle = TopLevel.GetTopLevel(myControl)?.TryGetPlatformHandle();
// handle.Handle: NSWindow pointer (IntPtr)
// handle.HandleDescriptor: "NSWindow"
```

Useful for calling native AppKit APIs directly.

---

## 7. Linux: X11 vs Wayland

| Feature | X11 | Wayland (12.0) |
|---|---|---|
| Availability | Now | Avalonia 12.0 |
| Window management | Full | Full |
| Transparency | Compositor-dependent | Compositor-dependent |
| Drag & drop | XDND | wl_data_device |
| Screens | XRandR | wl_output |
| Global hotkeys | XGrabKey | Protocol extension needed |

---

## 8. Linux: Embedded Linux (framebuffer)

For devices without X11 or Wayland, use the framebuffer backend:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .With(new X11PlatformOptions
    {
        UseFb = true,
        // Or specify DRM:
        // RenderingMode = new[] { X11RenderingMode.Drm }
    });
```

Required libraries:

```bash
sudo apt install libfontconfig1 libice6 libsm6 libx11-dev libxrandr-dev \
                 libxcursor-dev libxinerama-dev libxfixes-dev
```

---

## 9. Linux: Accessibility details

AT-SPI2 is exposed over D-Bus. To verify D-Bus is available:

```bash
echo $DBUS_SESSION_BUS_ADDRESS
```

If empty, start a session bus:

```bash
dbus-launch --autolaunch $(uuidgen)
```

Orca may need `spiel` speech dispatcher:

```bash
sudo apt install speech-dispatcher
```

---

## 10. macOS: Optional entitlements

For sandboxed macOS apps (Mac App Store), add an `Entitlements.plist`:

```xml
<key>com.apple.security.app-sandbox</key>
<true/>
<key>com.apple.security.files.user-selected.read-only</key>
<true/>
```

Referenced in `.csproj`:

```xml
<PropertyGroup>
  <CodesignEntitlements>Entitlements.plist</CodesignEntitlements>
</PropertyGroup>
```

---

## Key Takeaways

- macOS native backend is compiled from Xcode; point to custom dylib via `AvaloniaNativeLibraryPath`
- App bundle structure during dev: set `OutputPath` to `*.app/Contents/MacOS` and provide `Info.plist`
- NativeMenu on Application replaces default "About Avalonia" — auto-appends Quit
- Dock menu is macOS-only, set via `NativeDock.Menu`, modifiable at runtime
- Mac Catalyst is an alternative for UIKit-heavy apps but cannot cross-compile
- Linux uses X11; add `libice6 libsm6 libfontconfig1` for WSL 2
- Embedded Linux uses framebuffer/DRM via `X11PlatformOptions.UseFb`
- AT-SPI2 on Linux requires D-Bus; test with Orca (screen reader) or Accerciser (tree inspector)
