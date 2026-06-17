---
tier: intermediate
topic: window and dialog management
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 016-window-dialog-management.md
---

# Quiz — Window & Dialog Management

```quiz
Q: What is the primary benefit of the IDialogService pattern described in the tutorial?
A. It reduces memory usage by pooling window instances || Incorrect — the pattern does not pool windows.
B. It keeps ViewModels decoupled from specific Window types (correct) || Correct. ViewModels depend on the IDialogService interface, not concrete Window classes, enabling testability and separation of concerns.
C. It enables XAML-only dialog definitions || Incorrect — the pattern uses code-behind for window instantiation.
D. It automatically validates dialog input || Incorrect — validation is handled separately via ObservableValidator.
Explanation: IDialogService maps ViewModel types to Window types, so ViewModels can open dialogs without referencing Avalonia.Controls.Window directly.
```

```quiz
Q: In the DialogService implementation, how are ViewModel types mapped to their corresponding Window types?
A. Via a ResourceDictionary in App.axaml || Incorrect — resource dictionaries are for resources, not type mappings.
B. Using a Dictionary<Type, Type> that maps ViewModel types to Window types (correct) || Correct. The DialogService maintains a private dictionary mapping ViewModel types to Window types for instantiation.
C. Through naming convention: every ViewModel named XViewModel maps to XWindow || Incorrect — the tutorial uses explicit dictionary registration, not convention.
D. Via a [ViewFor(typeof(XViewModel))] attribute on the Window class || Incorrect — the tutorial does not use attributes for this mapping.
Explanation: The DialogService uses a Dictionary<Type, Type> populated in the constructor to associate each ViewModel type with the Window type that renders it.
```

```quiz
Q: How does a dialog ViewModel communicate a close signal (with a result) back to its Window?
A. The ViewModel directly calls Window.Close(result) || Incorrect — the ViewModel has no reference to the Window in a decoupled design.
B. It sends a message via WeakReferenceMessenger that the Window code-behind receives and then calls Close (correct) || Correct. The ViewModel sends a DialogResultMessage; the Window code-behind receives it and calls Close(m.Value).
C. It sets a bindable DialogResult property that the Window polls || Incorrect — there is no polling mechanism; the messenger pattern handles this.
D. It throws a DialogResultException that the Window catches || Incorrect — exceptions should not be used for control flow.
Explanation: The tutorial pattern uses WeakReferenceMessenger to send a result message from the VM to the window code-behind, which then closes the window with the result value.
```

```quiz
Q: What is the purpose of a WindowManager in multi-window applications?
A. It styles all windows consistently || Incorrect — styling is handled by themes and styles, not WindowManager.
B. It tracks open windows and prevents duplicate instances by activating existing ones (correct) || Correct. WindowManager maintains a dictionary of open windows and calls Activate() if a window with the same key already exists.
C. It serializes window state to disk automatically || Incorrect — window state persistence is a separate concern handled by explicit save/load code.
D. It manages the Application.Current.MainWindow lifecycle || Incorrect — WindowManager is for secondary windows, not MainWindow.
Explanation: WindowManager stores references to open windows by key, allowing the app to activate an existing window instead of creating a duplicate, and cleans up references on close.
```

```quiz
Q: Why can Window.WindowState no longer be set from a style in Avalonia 12?
A. WindowState is now a direct property, not a styled property (correct) || Correct. In v12, WindowState changed from a StyledProperty to a direct property, meaning it cannot be set via styles.
B. WindowState was removed entirely from the Window class || Incorrect — WindowState still exists and is functional.
C. WindowState is now read-only at runtime || Incorrect — WindowState can still be set programmatically.
D. WindowState was moved to a platform-specific API || Incorrect — it remains on the Window class.
Explanation: Avalonia 12 changed WindowState from a StyledProperty to a direct property. Styles can only set styled properties, so WindowState must now be set in code-behind, not in XAML styles.
```
