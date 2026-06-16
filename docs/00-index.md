# Avalonia UI 12 — Study & Reference Documentation

> **Target:** Avalonia 12.0.4 · .NET 10 · CommunityToolkit.Mvvm 8.x  
> **Last indexed:** 2026-06-14  
> **View rendered:** Open `viewer.html` via a local HTTP server (see below)

An organized collection of mini-tutorials, reference guides, patterns, and migration notes for Avalonia UI development. Written for developers who know the basics but need quick refreshers, structured learning, and deep-dive references.

---

## Quick Start

| If you want to... | Start here |
|---|---|
| Set up a new Avalonia 12 project | [001-project-setup](02-tutorials/basics/001-project-setup.md) |
| Wire a button to a command | [002-command-binding](02-tutorials/basics/002-command-binding.md) |
| Style a button with themes | [003-basic-styling](02-tutorials/basics/003-basic-styling.md) |
| Display a list of items | [009-data-templates-basics](02-tutorials/basics/009-data-templates-basics.md) |
| Create a reusable custom control | [advanced/ ... ](02-tutorials/advanced/) |

---

## 01 — Quick References

Cheat sheets and one-page summaries for common tasks.

| Ref | Topic |
|---|---|
| [Q01](01-quick-refs/avalonia-12-breaking-changes.md) | Avalonia 11 → 12 key breaking changes |
| [Q02](01-quick-refs/mvvm-source-generators.md) | CommunityToolkit.Mvvm source generator reference |
| [Q03](01-quick-refs/xaml-selectors.md) | XAML style selectors cheat sheet |
| [Q04](01-quick-refs/binding-modes.md) | Binding modes cheat sheet |
| [Q05](01-quick-refs/resource-dictionaries.md) | Resource Dictionaries |
| [Q06](01-quick-refs/control-themes.md) | Control Themes reference |
| [Q07](01-quick-refs/common-controls-reference.md) | Common Controls reference (15 controls) |

---

## 02 — Mini-Tutorials

Step-by-step guides organized by difficulty.

### Basics
1. [001 — Project Setup](02-tutorials/basics/001-project-setup.md)
   ⌞ [001V — Project Setup (verbose companion)](02-tutorials/basics/001-project-setup-verbose.md)
   ⌞ [001X — Project Setup (examples)](02-tutorials/basics/001-project-setup-examples.md)
   ⌞ [001Q — Project Setup (quiz)](02-tutorials/basics/001-project-setup-quiz.md)
2. [002 — Command Binding](02-tutorials/basics/002-command-binding.md)
   ⌞ [002V — Command Binding (verbose companion)](02-tutorials/basics/002-command-binding-verbose.md)
   ⌞ [002X — Command Binding (examples)](02-tutorials/basics/002-command-binding-examples.md)
3. [003 — Basic Styling](02-tutorials/basics/003-basic-styling.md)
   ⌞ [003V — Basic Styling (verbose companion)](02-tutorials/basics/003-basic-styling-verbose.md)
   ⌞ [003X — Basic Styling (examples)](02-tutorials/basics/003-basic-styling-examples.md)
4. [004 — Value Converters](02-tutorials/basics/004-value-converters.md)
   ⌞ [004V — Value Converters (verbose companion)](02-tutorials/basics/004-value-converters-verbose.md)
   ⌞ [004X — Value Converters (examples)](02-tutorials/basics/004-value-converters-examples.md)
5. [005 — Binding Modes](02-tutorials/basics/005-binding-modes.md)
   ⌞ [005V — Binding Modes (verbose companion)](02-tutorials/basics/005-binding-modes-verbose.md)
   ⌞ [005X — Binding Modes (examples)](02-tutorials/basics/005-binding-modes-examples.md)
6. [006 — Resources (Static & Dynamic)](02-tutorials/basics/006-resources.md)
   ⌞ [006V — Resources (verbose companion)](02-tutorials/basics/006-resources-verbose.md)
   ⌞ [006X — Resources (examples)](02-tutorials/basics/006-resources-examples.md)
7. [007 — ObservableObject & ObservableProperty](02-tutorials/basics/007-observable-object-property.md)
   ⌞ [007V — ObservableObject & ObservableProperty (verbose companion)](02-tutorials/basics/007-observable-object-property-verbose.md)
   ⌞ [007X — ObservableObject & ObservableProperty (examples)](02-tutorials/basics/007-observable-object-property-examples.md)
8. [008 — RelayCommand (sync & async)](02-tutorials/basics/008-relay-command.md)
   ⌞ [008V — RelayCommand (verbose companion)](02-tutorials/basics/008-relay-command-verbose.md)
   ⌞ [008X — RelayCommand (examples)](02-tutorials/basics/008-relay-command-examples.md)
9. [009 — Data Templates Basics](02-tutorials/basics/009-data-templates-basics.md)
   ⌞ [009V — Data Templates (verbose companion)](02-tutorials/basics/009-data-templates-basics-verbose.md)
   ⌞ [009X — Data Templates (real-world examples)](02-tutorials/basics/009-data-templates-examples.md)
10. [010 — Window Basics & Simple Dialogs](02-tutorials/basics/010-window-dialog-basics.md)
    ⌞ [010V — Window & Dialogs Basics (verbose companion)](02-tutorials/basics/010-window-dialog-basics-verbose.md)
    ⌞ [010X — Window & Dialogs (examples)](02-tutorials/basics/010-window-dialog-basics-examples.md)

### Intermediate
1. [011 — Compiled Bindings in Depth](02-tutorials/intermediate/011-compiled-bindings.md)
   ⌞ [011V — Compiled Bindings (verbose companion)](02-tutorials/intermediate/011-compiled-bindings-verbose.md)
   ⌞ [011X — Compiled Bindings (examples)](02-tutorials/intermediate/011-compiled-bindings-examples.md)
2. [012 — Control Themes vs Styles](02-tutorials/intermediate/012-control-themes-vs-styles.md)
   ⌞ [012V — Control Themes vs Styles (verbose companion)](02-tutorials/intermediate/012-control-themes-vs-styles-verbose.md)
   ⌞ [012X — Control Themes vs Styles (examples)](02-tutorials/intermediate/012-control-themes-vs-styles-examples.md)
3. [013 — Data Validation (ObservableValidator)](02-tutorials/intermediate/013-data-validation.md)
   ⌞ [013V — Data Validation (verbose companion)](02-tutorials/intermediate/013-data-validation-verbose.md)
   ⌞ [013X — Data Validation (examples)](02-tutorials/intermediate/013-data-validation-examples.md)
4. [014 — IMessenger Patterns](02-tutorials/intermediate/014-imessenger-patterns.md)
   ⌞ [014V — IMessenger Patterns (verbose companion)](02-tutorials/intermediate/014-imessenger-patterns-verbose.md)
   ⌞ [014X — IMessenger Patterns (examples)](02-tutorials/intermediate/014-imessenger-patterns-examples.md)
5. [015 — Item Lists (ListBox, ItemsRepeater, DataGrid)](02-tutorials/intermediate/015-item-lists.md)
   ⌞ [015V — Item Lists (verbose companion)](02-tutorials/intermediate/015-item-lists-verbose.md)
   ⌞ [015X — Item Lists (examples)](02-tutorials/intermediate/015-item-lists-examples.md)
6. [016 — Window & Dialog Management](02-tutorials/intermediate/016-window-dialog-management.md)
   ⌞ [016V — Window & Dialog Mgmt (verbose companion)](02-tutorials/intermediate/016-window-dialog-management-verbose.md)
   ⌞ [016X — Window & Dialog Mgmt (examples)](02-tutorials/intermediate/016-window-dialog-management-examples.md)
7. [017 — Theme Switching (Light/Dark/System)](02-tutorials/intermediate/017-theme-switching.md)
   ⌞ [017V — Theme Switching (verbose companion)](02-tutorials/intermediate/017-theme-switching-verbose.md)
   ⌞ [017X — Theme Switching (examples)](02-tutorials/intermediate/017-theme-switching-examples.md)
8. [018 — Navigation Patterns](02-tutorials/intermediate/018-navigation.md)
   ⌞ [018V — Navigation (verbose companion)](02-tutorials/intermediate/018-navigation-verbose.md)
   ⌞ [018X — Navigation (examples)](02-tutorials/intermediate/018-navigation-examples.md)
9. [019 — Drag & Drop](02-tutorials/intermediate/019-drag-drop.md)
   ⌞ [019V — Drag & Drop (verbose companion)](02-tutorials/intermediate/019-drag-drop-verbose.md)
   ⌞ [019X — Drag & Drop (examples)](02-tutorials/intermediate/019-drag-drop-examples.md)
10. [040 — DataGrid Deep Dive](02-tutorials/intermediate/040-datagrid-deep-dive.md)
    ⌞ [040V — DataGrid Deep Dive (verbose companion)](02-tutorials/intermediate/040-datagrid-deep-dive-verbose.md)
    ⌞ [040X — DataGrid Deep Dive (examples)](02-tutorials/intermediate/040-datagrid-deep-dive-examples.md)
11. [041 — Menus, Context Menus, and System Tray](02-tutorials/intermediate/041-menus-context-menus-tray.md)
    ⌞ [041V — Menus & Tray (verbose companion)](02-tutorials/intermediate/041-menus-context-menus-tray-verbose.md)
    ⌞ [041X — Menus & Tray (examples)](02-tutorials/intermediate/041-menus-context-menus-tray-examples.md)
12. [043 — TreeView with Hierarchical Data](02-tutorials/intermediate/043-treeview-hierarchical-data.md)
    ⌞ [043V — TreeView (verbose companion)](02-tutorials/intermediate/043-treeview-hierarchical-data-verbose.md)
    ⌞ [043X — TreeView (examples)](02-tutorials/intermediate/043-treeview-hierarchical-data-examples.md)

### Advanced
1. [020 — Custom Templated Controls](02-tutorials/advanced/020-custom-templated-controls.md)
   ⌞ [020V — Custom Templated Controls (verbose companion)](02-tutorials/advanced/020-custom-templated-controls-verbose.md)
   ⌞ [020X — Custom Templated Controls (examples)](02-tutorials/advanced/020-custom-templated-controls-examples.md)
2. [021 — Custom Controls from Scratch](02-tutorials/advanced/021-custom-controls-from-scratch.md)
   ⌞ [021V — Custom Controls from Scratch (verbose companion)](02-tutorials/advanced/021-custom-controls-from-scratch-verbose.md)
   ⌞ [021X — Custom Controls from Scratch (examples)](02-tutorials/advanced/021-custom-controls-from-scratch-examples.md)
3. [022 — Attached Properties & Behaviors](02-tutorials/advanced/022-attached-properties-behaviors.md)
   ⌞ [022V — Attached Properties & Behaviors (verbose companion)](02-tutorials/advanced/022-attached-properties-behaviors-verbose.md)
   ⌞ [022X — Attached Properties & Behaviors (examples)](02-tutorials/advanced/022-attached-properties-behaviors-examples.md)
4. [023 — Custom Layout Panels](02-tutorials/advanced/023-custom-layout-panels.md)
   ⌞ [023V — Custom Layout Panels (verbose companion)](02-tutorials/advanced/023-custom-layout-panels-verbose.md)
   ⌞ [023X — Custom Layout Panels (examples)](02-tutorials/advanced/023-custom-layout-panels-examples.md)
5. [024 — Animation & Transitions](02-tutorials/advanced/024-animation-transitions.md)
   ⌞ [024V — Animation & Transitions (verbose companion)](02-tutorials/advanced/024-animation-transitions-verbose.md)
   ⌞ [024X — Animation & Transitions (examples)](02-tutorials/advanced/024-animation-transitions-examples.md)
6. [025 — Compositor & Custom Visuals](02-tutorials/advanced/025-compositor-custom-visuals.md)
   ⌞ [025V — Compositor & Custom Visuals (verbose companion)](02-tutorials/advanced/025-compositor-custom-visuals-verbose.md)
   ⌞ [025X — Compositor & Custom Visuals (examples)](02-tutorials/advanced/025-compositor-custom-visuals-examples.md)
7. [026 — Accessibility & Automation](02-tutorials/advanced/026-accessibility-automation.md)
   ⌞ [026V — Accessibility & Automation (verbose companion)](02-tutorials/advanced/026-accessibility-automation-verbose.md)
   ⌞ [026X — Accessibility & Automation (examples)](02-tutorials/advanced/026-accessibility-automation-examples.md)
8. [027 — Advanced Composite Bindings](02-tutorials/advanced/027-advanced-composite-bindings.md)
   ⌞ [027V — Advanced Composite Bindings (verbose companion)](02-tutorials/advanced/027-advanced-composite-bindings-verbose.md)
   ⌞ [027X — Advanced Composite Bindings (examples)](02-tutorials/advanced/027-advanced-composite-bindings-examples.md)
9. [028 — Custom Drawing with Skia](02-tutorials/advanced/028-custom-drawing-skia.md)
   ⌞ [028V — Custom Drawing with Skia (verbose companion)](02-tutorials/advanced/028-custom-drawing-skia-verbose.md)
   ⌞ [028X — Custom Drawing with Skia (examples)](02-tutorials/advanced/028-custom-drawing-skia-examples.md)
10. [029 — Using Avalonia DevTools](02-tutorials/advanced/029-avalonia-plus-devtools.md)
    ⌞ [029V — DevTools (verbose companion)](02-tutorials/advanced/029-avalonia-plus-devtools-verbose.md)
    ⌞ [029X — DevTools (examples)](02-tutorials/advanced/029-avalonia-plus-devtools-examples.md)
11. [030 — Parcel Packaging & Distribution](02-tutorials/advanced/030-parcel-packaging.md)
    ⌞ [030V — Parcel Packaging (verbose companion)](02-tutorials/advanced/030-parcel-packaging-verbose.md)
    ⌞ [030X — Parcel Packaging (examples)](02-tutorials/advanced/030-parcel-packaging-examples.md)
12. [031 — Custom Theme & Design System](02-tutorials/advanced/031-custom-theme-design-system.md)
    ⌞ [031V — Custom Theme & Design System (verbose companion)](02-tutorials/advanced/031-custom-theme-design-system-verbose.md)
    ⌞ [031X — Custom Theme & Design System (examples)](02-tutorials/advanced/031-custom-theme-design-system-examples.md)
13. [032 — MVVM with Dependency Injection](02-tutorials/advanced/032-mvvm-di-wiring.md)
    ⌞ [032V — MVVM DI Wiring (verbose companion)](02-tutorials/advanced/032-mvvm-di-wiring-verbose.md)
    ⌞ [032X — MVVM DI Wiring (examples)](02-tutorials/advanced/032-mvvm-di-wiring-examples.md)
14. [033 — Localization & i18n](02-tutorials/advanced/033-localization-i18n.md)
    ⌞ [033V — Localization & i18n (verbose companion)](02-tutorials/advanced/033-localization-i18n-verbose.md)
    ⌞ [033X — Localization & i18n (examples)](02-tutorials/advanced/033-localization-i18n-examples.md)
15. [034 — File Pickers & Platform Services](02-tutorials/advanced/034-file-pickers-platform-services.md)
    ⌞ [034V — File Pickers & Platform (verbose companion)](02-tutorials/advanced/034-file-pickers-platform-services-verbose.md)
    ⌞ [034X — File Pickers & Platform (examples)](02-tutorials/advanced/034-file-pickers-platform-services-examples.md)
16. [035 — Custom Dialogs & Window Management](02-tutorials/advanced/035-custom-dialogs-window-management.md)
    ⌞ [035V — Custom Dialogs (verbose companion)](02-tutorials/advanced/035-custom-dialogs-window-management-verbose.md)
    ⌞ [035X — Custom Dialogs (examples)](02-tutorials/advanced/035-custom-dialogs-window-management-examples.md)
17. [036 — Virtualization & Large List Performance](02-tutorials/advanced/036-virtualization-large-lists.md)
    ⌞ [036V — Virtualization & Large Lists (verbose companion)](02-tutorials/advanced/036-virtualization-large-lists-verbose.md)
    ⌞ [036X — Virtualization & Large Lists (examples)](02-tutorials/advanced/036-virtualization-large-lists-examples.md)
18. [037 — App Lifetimes & Splash Screen](02-tutorials/advanced/037-app-lifetimes-splash-screen.md)
    ⌞ [037V — App Lifetimes & Splash Screen (verbose companion)](02-tutorials/advanced/037-app-lifetimes-splash-screen-verbose.md)
    ⌞ [037X — App Lifetimes & Splash Screen (examples)](02-tutorials/advanced/037-app-lifetimes-splash-screen-examples.md)
19. [038 — Headless Testing](02-tutorials/advanced/038-headless-testing.md)
    ⌞ [038V — Headless Testing (verbose companion)](02-tutorials/advanced/038-headless-testing-verbose.md)
    ⌞ [038X — Headless Testing (examples)](02-tutorials/advanced/038-headless-testing-examples.md)
20. [039 — NativeAOT & Trimming](02-tutorials/advanced/039-nativeaot-trimming.md)
    ⌞ [039V — NativeAOT & Trimming (verbose companion)](02-tutorials/advanced/039-nativeaot-trimming-verbose.md)
    ⌞ [039X — NativeAOT & Trimming (examples)](02-tutorials/advanced/039-nativeaot-trimming-examples.md)
21. [042 — Multi-Targeting: Desktop, Browser, Mobile](02-tutorials/advanced/042-multi-targeting-desktop-browser-mobile.md)
    ⌞ [042V — Multi-Targeting (verbose companion)](02-tutorials/advanced/042-multi-targeting-desktop-browser-mobile-verbose.md)
    ⌞ [042X — Multi-Targeting (examples)](02-tutorials/advanced/042-multi-targeting-desktop-browser-mobile-examples.md)
22. [044 — Background Services & Progress Reporting](02-tutorials/advanced/044-background-services-and-progress.md)
    ⌞ [044V — Background Services & Progress (verbose companion)](02-tutorials/advanced/044-background-services-and-progress-verbose.md)
    ⌞ [044X — Background Services & Progress (examples)](02-tutorials/advanced/044-background-services-and-progress-examples.md)
23. [045 — CI/CD for Avalonia Applications](02-tutorials/advanced/045-cicd-for-avalonia.md)
    ⌞ [045V — CI/CD for Avalonia (verbose companion)](02-tutorials/advanced/045-cicd-for-avalonia-verbose.md)
    ⌞ [045X — CI/CD for Avalonia (examples)](02-tutorials/advanced/045-cicd-for-avalonia-examples.md)
24. [046 — Logging & Telemetry](02-tutorials/advanced/046-logging-telemetry.md)
    ⌞ [046V — Logging & Telemetry (verbose companion)](02-tutorials/advanced/046-logging-telemetry-verbose.md)
    ⌞ [046X — Logging & Telemetry (examples)](02-tutorials/advanced/046-logging-telemetry-examples.md)
25. [047 — Charts & Data Visualization](02-tutorials/intermediate/047-charts-data-visualization.md)
    ⌞ [047V — Charts & Data Viz (verbose companion)](02-tutorials/intermediate/047-charts-data-visualization-verbose.md)
    ⌞ [047X — Charts & Data Viz (examples)](02-tutorials/intermediate/047-charts-data-visualization-examples.md)
26. [048 — SQLite / Local Database](02-tutorials/intermediate/048-sqlite-local-database.md)
    ⌞ [048V — SQLite / Local DB (verbose companion)](02-tutorials/intermediate/048-sqlite-local-database-verbose.md)
    ⌞ [048X — SQLite / Local DB (examples)](02-tutorials/intermediate/048-sqlite-local-database-examples.md)
27. [049 — Printing](02-tutorials/advanced/049-printing.md)
    ⌞ [049V — Printing (verbose companion)](02-tutorials/advanced/049-printing-verbose.md)
    ⌞ [049X — Printing (examples)](02-tutorials/advanced/049-printing-examples.md)
28. [050 — App Update / Auto-Updater](02-tutorials/advanced/050-auto-updater.md)
    ⌞ [050V — App Update / Auto-Updater (verbose companion)](02-tutorials/advanced/050-auto-updater-verbose.md)
    ⌞ [050X — App Update / Auto-Updater (examples)](02-tutorials/advanced/050-auto-updater-examples.md)

---

## 03 — Patterns

Reusable architectural and design patterns.

| Pattern | Covered In |
|---|---|
| MVVM with DI (Microsoft.Extensions.DI) | [032](02-tutorials/advanced/032-mvvm-di-wiring.md) |
| App lifetimes & startup patterns | [037](02-tutorials/advanced/037-app-lifetimes-splash-screen.md) |
| Service locator vs DI | [Pattern 001](03-patterns/001-service-locator-vs-di.md) |
| Modular app with plugin-style views | [Pattern 002](03-patterns/002-plugin-architecture.md) |
| Async initialization patterns | [Pattern 003](03-patterns/003-async-initialization.md) |
| State management (Flux / singleton / IMessenger) | [Pattern 004](03-patterns/004-state-management.md) |
| Repository / Unit of Work | [Pattern 005](03-patterns/005-repository-unit-of-work.md) |
| Logging patterns | [Pattern 006](03-patterns/006-logging-patterns.md) |
| Editor application architecture | [Pattern 007](03-patterns/007-editor-application-architecture.md) |

---

## 04 — Migration

Moving between versions and platforms.

| Guide | Status |
|---|---|
| [Avalonia 11 → 12 Migration](04-migration/avalonia-11-to-12.md) | Complete — 10-phase guide |
| [WPF → Avalonia Key Mappings](04-migration/wpf-to-avalonia-key-mappings.md) | Complete — control/property/binding/style tables |
| Key breaking changes in 12.0.4 | See [Q01 quick-ref](01-quick-refs/avalonia-12-breaking-changes.md) |

---

## 05 — Avalonia Plus

Coverage of the commercial tool suite.

| Tool | Status |
|---|---|
| [DevTools — setup & usage guide](02-tutorials/advanced/029-avalonia-plus-devtools.md) | Complete |
| [Parcel — packaging walkthrough](02-tutorials/advanced/030-parcel-packaging.md) | Complete |

---

## Reference Material

- [Avalonia Official Docs (12.x)](https://docs.avaloniaui.net/)
- Plugin: Development Plugin for Avalonia (external — 69 specialized reference docs available in the plugin repo)

---

---

## Viewing the Docs

Open `viewer.html` in this directory via a local HTTP server:

```powershell
# Quick start (auto-detects available tool):
.\serve-docs.ps1

# Or manually with Python:
python -m http.server 8080
# then open http://localhost:8080/viewer.html
```

```bash
# Or with .NET (install once):
dotnet tool install -g dotnet-serve
dotnet serve --directory . --port 8080
```

The viewer provides styled rendering, a table of contents sidebar, syntax-highlighted code blocks, dark mode (system preference), and a dropdown to navigate between all documents.

*Maintained with the [audit-skill](../docs/_skills/audit-skill.md) workflow. Found an issue? Open a discussion.*
