# Avalonia UI 12 — Study & Reference Documentation

> **Target:** Avalonia 12.0.4 · .NET 10 · CommunityToolkit.Mvvm 8.x  
> **Last indexed:** 2026-06-12  
> **View rendered:** Open with `_assets/templates/render.html`

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

---

## 02 — Mini-Tutorials

Step-by-step guides organized by difficulty.

### Basics
1. [001 — Project Setup](02-tutorials/basics/001-project-setup.md)
2. [002 — Command Binding](02-tutorials/basics/002-command-binding.md)
3. [003 — Basic Styling](02-tutorials/basics/003-basic-styling.md)
4. [004 — Value Converters](02-tutorials/basics/004-value-converters.md)
5. [005 — Binding Modes](02-tutorials/basics/005-binding-modes.md)
6. [006 — Resources (Static & Dynamic)](02-tutorials/basics/006-resources.md)
7. [007 — ObservableObject & ObservableProperty](02-tutorials/basics/007-observable-object-property.md)
8. [008 — RelayCommand (sync & async)](02-tutorials/basics/008-relay-command.md)
9. [009 — Data Templates Basics](02-tutorials/basics/009-data-templates-basics.md)
10. [010 — Window Basics & Simple Dialogs](02-tutorials/basics/010-window-dialog-basics.md)

### Intermediate
1. [011 — Compiled Bindings in Depth](02-tutorials/intermediate/011-compiled-bindings.md)
2. [012 — Control Themes vs Styles](02-tutorials/intermediate/012-control-themes-vs-styles.md)
3. [013 — Data Validation (ObservableValidator)](02-tutorials/intermediate/013-data-validation.md)
4. [014 — IMessenger Patterns](02-tutorials/intermediate/014-imessenger-patterns.md)
5. [015 — Item Lists (ListBox, ItemsRepeater, DataGrid)](02-tutorials/intermediate/015-item-lists.md)
6. [016 — Window & Dialog Management](02-tutorials/intermediate/016-window-dialog-management.md)
7. [017 — Theme Switching (Light/Dark/System)](02-tutorials/intermediate/017-theme-switching.md)
8. [018 — Navigation Patterns](02-tutorials/intermediate/018-navigation.md)
9. [019 — Drag & Drop](02-tutorials/intermediate/019-drag-drop.md)

### Advanced
1. [020 — Custom Templated Controls](02-tutorials/advanced/020-custom-templated-controls.md)
2. [021 — Custom Controls from Scratch](02-tutorials/advanced/021-custom-controls-from-scratch.md)
3. [022 — Attached Properties & Behaviors](02-tutorials/advanced/022-attached-properties-behaviors.md)
4. [023 — Custom Layout Panels](02-tutorials/advanced/023-custom-layout-panels.md)
5. [024 — Animation & Transitions](02-tutorials/advanced/024-animation-transitions.md)
6. [025 — Compositor & Custom Visuals](02-tutorials/advanced/025-compositor-custom-visuals.md)
7. [026 — Accessibility & Automation](02-tutorials/advanced/026-accessibility-automation.md)
8. [027 — Advanced Composite Bindings](02-tutorials/advanced/027-advanced-composite-bindings.md)
9. [028 — Custom Drawing with Skia](02-tutorials/advanced/028-custom-drawing-skia.md)
10. [029 — Using Avalonia DevTools](02-tutorials/advanced/029-avalonia-plus-devtools.md)
11. [030 — Parcel Packaging & Distribution](02-tutorials/advanced/030-parcel-packaging.md)
12. [031 — Custom Theme & Design System](02-tutorials/advanced/031-custom-theme-design-system.md)
13. [032 — MVVM with Dependency Injection](02-tutorials/advanced/032-mvvm-di-wiring.md)
14. [033 — Localization & i18n](02-tutorials/advanced/033-localization-i18n.md)
15. [034 — File Pickers & Platform Services](02-tutorials/advanced/034-file-pickers-platform-services.md)
16. [035 — Custom Dialogs & Window Management](02-tutorials/advanced/035-custom-dialogs-window-management.md)
17. [036 — Virtualization & Large List Performance](02-tutorials/advanced/036-virtualization-large-lists.md)
18. [037 — App Lifetimes & Splash Screen](02-tutorials/advanced/037-app-lifetimes-splash-screen.md)
19. [038 — Headless Testing](02-tutorials/advanced/038-headless-testing.md)
20. [039 — NativeAOT & Trimming](02-tutorials/advanced/039-nativeaot-trimming.md)

---

## 03 — Patterns

Reusable architectural and design patterns.

| Pattern | Covered In |
|---|---|---|
| MVVM with DI (Microsoft.Extensions.DI) | [032](02-tutorials/advanced/032-mvvm-di-wiring.md) |
| App lifetimes & startup patterns | [037](02-tutorials/advanced/037-app-lifetimes-splash-screen.md) |
| Service locator vs DI | Planned |
| Modular app with plugin-style views | Planned |

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
|---|---|---|
| [DevTools — setup & usage guide](02-tutorials/advanced/029-avalonia-plus-devtools.md) | Complete |
| [Parcel — packaging walkthrough](02-tutorials/advanced/030-parcel-packaging.md) | Complete |

---

## Reference Material

- [Avalonia Official Docs (12.x)](https://docs.avaloniaui.net/)
- [Plugin: Development Plugin for Avalonia](../references/compendium.md) — 69 specialized reference docs

---

*Maintained with the [audit-skill](../docs/_skills/audit-skill.md) workflow. Found an issue? Open a discussion.*
