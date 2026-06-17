---
tier: intermediate
topic: windowing
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 041-menus-context-menus-tray.md
---

# Quiz — Menus, Context Menus & System Tray

```quiz
Q: How are access keys (keyboard mnemonics) defined in a MenuItem header?
A. With an ampersand prefix: `&File` || Incorrect — ampersand is the WPF convention; Avalonia uses underscore.
B. With an underscore prefix: `_File` (correct) || Correct — the underscore before a character creates an access key; pressing Alt+F opens the menu.
C. With a colon prefix: `:File` || Incorrect — colon has no special meaning in MenuItem headers.
D. With curly braces: `{File}` || Incorrect — curly braces are used for markup extensions, not access keys.
Explanation: Underscore prefix marks the access key; Alt+<key> activates the menu item when the window is focused.
```

```quiz
Q: Which menu control renders as the global menu bar on macOS and as an in-window menu on Windows and Linux?
A. Menu || Incorrect — Menu always renders as an in-window control on all platforms; it does not produce the macOS global menu bar.
B. ContextMenu || Incorrect — ContextMenu is a right-click popup menu, not a top-level menu bar.
C. NativeMenu (correct) || Correct — NativeMenu attached to Window renders as the macOS global menu bar and as an in-window menu on Windows and Linux.
D. AppMenu || Incorrect — there is no AppMenu control in Avalonia.
Explanation: NativeMenu is the recommended approach for macOS targets because it produces the expected global menu bar experience.
```

```quiz
Q: What technique prevents a window from closing and instead hides it to the system tray when the user clicks the close button?
A. Set the WindowState to Minimized in the Closing event || Incorrect — setting WindowState minimizes the window but does not cancel the close; the app may still shut down.
B. Handle the OnClosing override, set e.Cancel = true, and call Hide() (correct) || Correct — setting e.Cancel = true prevents the close, and calling Hide() moves the window to the tray.
C. Override OnPointerPressed and suppress the close button hit test || Incorrect — intercepting pointer events on the title bar is unreliable and interferes with normal window chrome behavior.
D. Set TrayIcon.IsVisible = false in the Closing event || Incorrect — hiding the tray icon does not prevent the window from closing.
Explanation: The OnClosing override with e.Cancel = true cancels the close, then Hide() makes the window invisible while the app continues running in the tray.
```

```quiz
Q: Identify the bug in this dynamic ContextMenu construction:
    var menu = new ContextMenu();
    foreach (var item in AvailableActions)
    {
        menu.Items.Add(new MenuItem
        {
            Header = item.Name,
            Command = item.Action
        });
    }
    menu.Open(target);
A. ContextMenu.Items does not exist; use ContextMenu.ItemSource || Incorrect — ContextMenu inherits Items from ItemsControl; Items is valid.
B. ContextMenu.Open() is a void method that requires the control to be in the visual tree || Incorrect — Open(target) is the correct API and the control is already in the tree if target is a visual child.
C. The MenuItem.Command property cannot be bound to a raw ICommand reference in code-behind || Incorrect — setting Command directly to an ICommand instance works in code-behind.
D. The code is correct as shown — no bug (correct) || Correct — dynamic ContextMenu construction with MenuItem creation and Open(target) is the standard pattern shown in the tutorial.
Explanation: The code follows the documented pattern: create ContextMenu, populate Items with MenuItem instances, call Open(target) to show it.
```

```quiz
Q: Which platforms support TrayIcon in Avalonia?
A. Windows, macOS, Linux (correct) || Correct — TrayIcon is supported on Windows (full), macOS (menu bar), and Linux (DE-dependent via libappindicator/StatusNotifier).
B. Windows and macOS only || Incorrect — Linux also supports TrayIcon via libappindicator or StatusNotifier, though support is desktop-environment dependent.
C. Windows only || Incorrect — TrayIcon is not limited to Windows; macOS and Linux also support it.
D. Windows, macOS, Linux, and Browser || Incorrect — Browser does not support TrayIcon; web apps have no system tray concept.
Explanation: TrayIcon works on all desktop platforms but is not available on Browser, Android, or iOS.
```
