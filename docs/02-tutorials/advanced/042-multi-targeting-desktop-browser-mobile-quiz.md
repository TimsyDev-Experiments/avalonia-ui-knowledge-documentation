---
tier: advanced
topic: multi-targeting
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 042-multi-targeting-desktop-browser-mobile.md
---

# Quiz — Multi-Targeting: Desktop, Browser, and Mobile

```quiz
Q: How should the shared App.axaml.cs determine which platform it is running on?
A. Check the OperatingSystem.IsBrowser() or OperatingSystem.IsAndroid() methods at startup || These detect the OS but do not directly map to the Avalonia application lifetime.
B. Inspect the ApplicationLifetime property and branch on IClassicDesktopStyleApplicationLifetime vs ISingleViewApplicationLifetime (correct) || Desktop platforms use IClassicDesktopStyleApplicationLifetime while browser, Android, and iOS use ISingleViewApplicationLifetime.
C. Use conditional compilation with #if ANDROID / #elif IOS / #elif BROWSER || Conditional compilation works but requires separate builds; the lifetime check handles it uniformly at runtime.
D. Read a platform string from the app config file || There is no platform config mechanism — the lifetime is set by the platform entry point.
Explanation: The idiomatic approach is to check ApplicationLifetime — IClassicDesktopStyleApplicationLifetime for desktop and ISingleViewApplicationLifetime for mobile/browser.
```

```quiz
Q: Which method starts the Avalonia application lifecycle in the browser entry point?
A. StartWithClassicDesktopLifetime(args) || This is the desktop entry method, not available on WASM.
B. StartWithBrowserPlatform(args) (correct) || The browser entry point calls StartWithBrowserPlatform, provided by Avalonia.Web.
C. StartWithAndroidPlatform(args) || This is the Android-specific lifecycle starter.
D. RunBrowserApp(args) || No such method exists in the Avalonia API.
Explanation: The browser entry point uses StartWithBrowserPlatform from the Avalonia.Web package to initialize the WASM runtime.
```

```quiz
Q: What is the correct way to conditionally apply different XAML styles per platform?
A. Use a converter that checks OperatingSystem and returns a Style || There is a simpler built-in approach using the Platform selector.
B. Use the Platform selector in the Style definition, e.g. <Style Selector="Button" Platform="android"> (correct) || Avalonia supports the Platform attribute in style selectors to apply platform-specific setters.
C. Define platform-specific resource dictionaries and merge them conditionally || This works but is more complex than the built-in Platform selector.
D. Use x:DataType with platform-specific compiled bindings || Data types do not vary by platform — the selector addresses styling, not binding.
Explanation: The Platform selector attribute (e.g., Platform="android") directly applies styles only on the specified platform without requiring code-behind or conditional compilation.
```

```quiz
Q: Which of the following is a key limitation when targeting browser or mobile with Avalonia?
A. The app cannot use compiled bindings || Compiled bindings work on all platforms; they are an XAML compiler feature.
B. GPU rendering is unavailable on all non-desktop platforms || Each platform has a GPU backend (WebGL, OpenGL ES, Metal) — rendering works on all.
C. These platforms do not support multiple windows natively — they use a single-view model (correct) || Browser and mobile platforms are restricted to a single ISingleViewApplicationLifetime view; only desktop can open multiple windows.
D. The x:CompileBindings directive is ignored on mobile || Compiled bindings are compiled at build time and work identically on all targets.
Explanation: Single-view platforms (browser, Android, iOS) cannot open multiple windows natively because they use ISingleViewApplicationLifetime rather than IClassicDesktopStyleApplicationLifetime.
```

```quiz
Q: What should you always use instead of direct file system I/O on browser and mobile targets?
A. The OperatingSystem.IsBrowser() check with conditional code || This detects the platform but does not replace file I/O.
B. System.IO.Path and System.IO.File as usual || These APIs throw PlatformNotSupportedException on sandboxed platforms.
C. The StorageProvider API from the TopLevel or Application (correct) || StorageProvider is the cross-platform abstraction for file read/write that works on all targets including sandboxed environments.
D. HttpClient with a local file server || This adds network latency and an unnecessary server dependency.
Explanation: StorageProvider provides a uniform file picker and read/write experience across desktop, browser, and mobile, handling sandboxed file system access correctly on each platform.
```
