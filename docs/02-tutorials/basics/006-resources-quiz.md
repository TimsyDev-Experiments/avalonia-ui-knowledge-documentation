---
tier: basics
topic: resources
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 006-resources.md
---

```quiz
Q: When a StaticResource key is missing at element-load time, the runtime behavior is:
A. The element silently uses a fallback default value. || StaticResource never falls back; it either resolves or throws.
B. An exception is thrown immediately during element construction. || (correct) || StaticResource is resolved once during load; a missing key produces a runtime exception at that point.
C. The resource is resolved lazily on first access, and the application continues until that access occurs. || StaticResource resolution is eager, not lazy.
D. The runtime substitutes the nearest matching resource from the theme. || Theme resources are fallbacks only for DynamicResource lookups, not StaticResource.
Explanation: StaticResource is resolved once at element load time. If the key is absent from the lookup chain at that moment, a XAML parse exception is thrown. This is a fundamental difference from DynamicResource, which tolerates a missing key until the resource is actually provided later.

Q: Which of the following correctly describes the resource-lookup order for a control inside a nested Panel in Avalonia?
A. The control → its parent Panel → any Style setters → Application.Resources → Theme resources. || The correct chain starts with the element itself, then walks up the logical tree before reaching Application and theme.
B. The element itself → its parent → the Window or UserControl → Application.Resources → Theme dictionaries. || (correct) || Avalonia walks the logical tree upward: element, parent chain, owning window/control, then Application.Resources, and finally theme-level resources.
C. Application.Resources → Theme resources → the Window → the parent Panel → the element. || This is the reverse of the actual order; local resources take precedence over application resources.
D. Theme resources → Application.Resources → Styles → the element. || Theme resources are the lowest-priority layer, checked last, not first.
Explanation: The lookup proceeds from the most local scope outward: the element's own Resources, then each parent in the logical tree, then the containing Window or UserControl, then Application.Resources, and finally theme dictionaries. StaticResource stops at the first match; DynamicResource re-evaluates when any upstream dictionary changes.

Q: A developer defines a MergedDictionary in Window.Resources that includes a file via ResourceInclude. Which statement is true?
A. ResourceInclude merges the source file's resources at compile time, making them available as if defined inline. || ResourceInclude merges at runtime, not compile time.
B. ResourceInclude merges the source dictionary's resources into the parent dictionary at runtime, and resources in the merged dictionary are searched after resources defined directly in the parent. || (correct) || ResourceInclude loads the external .axaml dictionary and merges its entries into the containing dictionary. The parent's own resources take precedence over merged entries.
C. Merged dictionaries are only supported at the Application level, not on Window or UserControl. || Any ResourcedDictionary can merge other dictionaries, including those on Window and UserControl.
D. A resource defined in a merged dictionary always overrides a resource with the same key in the parent dictionary. || The parent dictionary's own entries win over merged entries when keys collide.
Explanation: MergedDictionaries with ResourceInclude let you compose resource dictionaries from separate files. The parent dictionary's locally-defined resources take priority over any entries pulled in via merge. ResourceInclude loads the file and adds its entries to the dictionary's lookup chain in the order they appear in the MergedDictionaries collection.

Q: Which declaration stores a string primitive as a reusable resource in Avalonia XAML?
A. `<s:String xmlns:sys="clr-namespace:System;assembly=mscorlib">Hello</s:String>` || This is a WPF-ism. Avalonia uses the `x:String` markup extension, not `s:String`.
B. `<x:String>Hello</x:String>` in a Resources section. || (correct) || Avalonia supports `x:String`, `x:Double`, `x:Int32`, and other XAML-language primitive elements directly in resource dictionaries.
C. `<ResourceDictionary><sys:String x:Key="greeting">Hello</sys:String></ResourceDictionary>` || The `sys:String` alias is not pre-mapped in Avalonia; the XAML language types use the `x:` prefix.
D. `x:Static` with a code-behind constant is the only way to share a string in resources. || Primitive type elements (`x:String`, `x:Double`, etc.) are fully supported in Avalonia resource dictionaries.
Explanation: Avalonia's XAML processor supports the XAML language primitives `x:String`, `x:Double`, `x:Int32`, and `x:Boolean` directly in resource dictionaries. These are the canonical way to store simple scalar values as resources. Theme-aware values should still use DynamicResource to support runtime theme switching.
```
