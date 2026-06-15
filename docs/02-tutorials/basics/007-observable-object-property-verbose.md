---
tier: basics
topic: mvvm
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 007-observable-object-property.md
---

# 007V — ObservableObject and ObservableProperty: An In-Depth Companion

**What you'll learn in this companion:** How `ObservableObject.SetProperty` works internally, what the `[ObservableProperty]` source generator actually emits, why the field must be `private` and underscore-prefixed, how change notification chaining resolves through the generated code, and the full lifecycle of property validation hooks.

**Prerequisites:** [002 — Command Binding](002-command-binding.md)

**You should already have read:** [007 — ObservableObject & ObservableProperty](007-observable-object-property.md) for the quick-start version. This file goes deeper on every section.

---

## 1. What `ObservableObject` Actually Provides

```csharp
public partial class MainViewModel : ObservableObject
```

`ObservableObject` is a base class from `CommunityToolkit.Mvvm.ComponentModel`. It implements:

- **`INotifyPropertyChanged`** — the standard .NET interface with a `PropertyChanged` event. The binding system in Avalonia subscribes to this event to know when to re-read a property value.
- **`INotifyPropertyChanging`** — a companion interface with a `PropertyChanging` event, fired **before** the value changes. This allows the binding system (or other subscribers) to capture the old value.
- **`SetProperty<T>(ref T, T, string)`** — a protected method that sets a backing field, raises `PropertyChanging`, sets the value, raises `PropertyChanged`, and returns `true` if the value actually changed (or `false` if the new value equals the old, avoiding unnecessary notifications).

```csharp
// ObservableObject.SetProperty<T> -- simplified
protected bool SetProperty<T>(ref T field, T newValue, string propertyName)
{
    if (EqualityComparer<T>.Default.Equals(field, newValue))
        return false; // no change, skip notification

    OnPropertyChanging(propertyName);
    field = newValue;
    OnPropertyChanged(propertyName);
    return true;
}
```

The `EqualityComparer<T>.Default` check is the reason `SetProperty` avoids unnecessary UI updates. If you set `Name = "Alice"` when `Name` is already `"Alice"`, the setter returns `false` without raising `PropertyChanged`. The UI does not re-evaluate bindings, the converter does not run, and the text block does not flicker.

### Why ObservableObject Instead of Manually Implementing INotifyPropertyChanged

Manual implementation requires:

```csharp
private string _name;
public string Name
{
    get => _name;
    set
    {
        if (_name == value) return;
        _name = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
    }
}
```

Every property repeats the equality check, the field assignment, and the event invocation. For a ViewModel with 15 properties, this is ~150 lines of boilerplate. `ObservableObject` + `[ObservableProperty]` collapses this to one line per property.

---

## 2. What the `[ObservableProperty]` Source Generator Actually Produces

```csharp
[ObservableProperty]
private string _name;
```

The CommunityToolkit.Mvvm source generator (`CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator`) processes this field and generates a partial class file (in the `obj/` directory) containing:

```csharp
// Generated -- simplified
public string Name
{
    get => _name;
    set
    {
        if (!EqualityComparer<string>.Default.Equals(_name, value))
        {
            OnPropertyChanging(nameof(Name));
            OnNameChanging(value);
            _name = value;
            OnNameChanged(value);
            OnPropertyChanged(nameof(Name));
        }
    }
}

partial void OnNameChanging(string value);
partial void OnNameChanged(string value);
```

The generated code:

1. **Public property** `Name` — capitalization is the field name with underscore stripped and first letter uppercased.
2. **`get` accessor** — returns the backing field directly.
3. **`set` accessor** — checks equality using `EqualityComparer<T>.Default`. If different:
   - Calls `OnPropertyChanging(nameof(Name))` (for `INotifyPropertyChanging` subscribers).
   - Calls `partial void OnNameChanging(string value)` — you can implement this in your ViewModel partial class to react before the value is stored.
   - Assigns the backing field.
   - Calls `partial void OnNameChanged(string value)` — you can implement this to react after the value is stored.
   - Calls `OnPropertyChanged(nameof(Name))` (for `INotifyPropertyChanged` subscribers — the Avalonia binding system).
4. **Partial method declarations** — `OnNameChanging` and `OnNameChanged` are declared as `partial void` with no body. If you don't implement them, the compiler removes the call sites entirely (zero overhead). If you do implement them, the generated setter calls your implementation.

### The Underscore Prefix Requirement

```csharp
[ObservableProperty]
private string _name;   // ✓ correct

[ObservableProperty]
private string name;    // ✗ compile error
```

The generator requires the backing field to have an underscore prefix (`_`). This is a design choice to:

1. **Distinguish fields from properties in source code.** The underscore is a common C# convention for private fields. Enforcing it avoids ambiguity when the generator strips the prefix to produce the property name.
2. **Enable the naming convention:** Strip `_`, then PascalCase the rest. `_firstName` → `FirstName`. `_isEnabled` → `IsEnabled`.
3. **Prevent naming collisions.** If the field were `name`, the generated property would also be `name`, which is a name collision with the field (the generated code would need `this.name` qualification). The underscore avoids this.

### What Happens If the Class Is Not `partial`

The source generator requires the declaring class to be `partial`. If it is not, the generator emits:

```
error: The type 'MainViewModel' is not partial. ObservableProperty attributes can only be used in partial types.
```

The code would not compile because the generated class file cannot merge with the hand-written class.

---

## 3. Property Change Notification Chaining: How Dependencies Propagate

```csharp
[ObservableProperty]
private string _firstName;

[ObservableProperty]
private string _lastName;

public string FullName => $"{FirstName} {LastName}";
```

`FullName` is a computed property — it has no setter, just a getter that combines `FirstName` and `LastName`. When `FirstName` changes, `FullName` changes too, but the binding system does not know that. Without notification, a `TextBlock` bound to `FullName` shows the old value until some other event triggers a refresh.

The fix is `[NotifyPropertyChangedFor]`:

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]
private string _firstName;

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]
private string _lastName;
```

This attribute instructs the generator to emit `OnPropertyChanged(nameof(FullName))` after `_firstName`'s `OnPropertyChanged(nameof(FirstName))` call. The generated setter becomes:

```csharp
set
{
    if (!EqualityComparer<string>.Default.Equals(_firstName, value))
    {
        OnPropertyChanging(nameof(FirstName));
        OnFirstNameChanging(value);
        _firstName = value;
        OnFirstNameChanged(value);
        OnPropertyChanged(nameof(FirstName));
        OnPropertyChanged(nameof(FullName));  // <-- injected by [NotifyPropertyChangedFor]
    }
}
```

### Chaining Multiple Levels

You can chain `[NotifyPropertyChangedFor]` across multiple levels:

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]
[NotifyPropertyChangedFor(nameof(Initials))]
private string _firstName;

public string FullName => $"{FirstName} {LastName}";
public string Initials => $"{FirstName?[0]}{LastName?[0]}";
```

When `FirstName` changes, both `FullName` and `Initials` are notified. This is more efficient than making `Initials` depend on `FullName` (which would trigger an extra chain) — each notification is explicit and direct.

### Why Not Use Automatic Dependency Tracking

Some MVVM frameworks (e.g., `ReactiveUI`, `Fody`) automatically detect dependencies by analyzing the getter body. `CommunityToolkit.Mvvm` does not do this because:

- Source generators cannot analyze method bodies in other partial class files (they only see the attribute and the field).
- Automatic detection is fragile — changes to the getter body can silently change the dependency graph.
- Explicit `[NotifyPropertyChangedFor]` is self-documenting and deterministic.

---

## 4. Validation Hooks: `OnNameChanging` vs `OnNameChanged`

```csharp
partial void OnEmailChanging(string value)
{
    // Before the value is stored
}

partial void OnEmailChanged(string value)
{
    // After the value is stored
}
```

### `OnEmailChanging(string value)`

- Called with the **new** value (not yet stored in the backing field).
- The backing field still holds the old value.
- Use cases:
  - Cancel the operation? No — throwing an exception here leaves the field in an inconsistent state (the setter has already called `OnPropertyChanging` but hasn't stored the value yet). Do not throw.
  - Log the old-to-new transition: `Debug.WriteLine($"Changing from {_email} to {value}")`.
  - Validate without storing: check format, but do not modify `value` (it's a copy for value types, a reference for reference types — modifying the object `value` points to would affect the caller, which is unexpected).

### `OnEmailChanged(string value)`

- Called **after** the backing field has been updated.
- `_email` now equals `value`.
- Use cases:
  - Validation: check `value.Contains('@')` and set an error flag.
  - Side effects: update derived state, fire a command's `NotifyCanExecuteChanged()`.
  - Logging: `Debug.WriteLine($"Email changed to {_email}")`.

Both partial methods are optional. If you do not implement them, the compiler removes the call sites with no performance cost.

### The Changing/Changed Pattern in the Binding System

The `OnPropertyChanging` + `OnPropertyChanged` pair mirrors Avalonia's own `AvaloniaPropertyChanging` / `AvaloniaPropertyChanged` pattern. When a binding receives `PropertyChanged`, it re-reads the source value. The `PropertyChanging` notification is used internally by the binding system to cache the old value for change-tracking scenarios. As a ViewModel author, you typically only need `OnChanged`, not `OnChanging`.

---

## 5. `[ObservableProperty]` with `INotifyDataErrorInfo` (ObservableValidator)

If your ViewModel extends `ObservableValidator` instead of `ObservableObject`, the generated setter also calls `ValidateProperty(value, propertyName)`:

```csharp
// Generated when class extends ObservableValidator
set
{
    if (!EqualityComparer<string>.Default.Equals(_email, value))
    {
        OnPropertyChanging(nameof(Email));
        _email = value;
        OnPropertyChanged(nameof(Email));
        ValidateProperty(value, nameof(Email)); // <-- injected for ObservableValidator
    }
}
```

`ObservableValidator` is covered in detail in [013 — Data Validation](../intermediate/013-data-validation.md). The key point: switching from `ObservableObject` to `ObservableValidator` changes the generated code, so you must choose your base class at design time.

---

## 6. Common Mistakes

1. **Field not `private`.** The source generator requires `private`. If the field is `public` or `protected`, the generator emits an error.
2. **Field missing underscore prefix.** `string name` instead of `string _name`. The generator emits an error expecting the `_` prefix.
3. **Using `[ObservableProperty]` on a static field.** `[ObservableProperty]` is only supported on instance fields. Static fields produce a generator error.
4. **Property name collision.** If the generated property name matches an existing member (e.g., you have a field `_name` and also a property `Name`), the generator emits an error about a duplicate member.
5. **Calling `SetProperty` manually on the generated property.** The generated setter already calls `SetProperty`. Calling it inside `OnNameChanged` creates a recursive loop.
6. **Forgetting to mark the class `partial`.** The most common error. The fix is to add `partial` to the class declaration.

---

## See Also

- [007 — ObservableObject & ObservableProperty (original tutorial)](007-observable-object-property.md)
- [007X — ObservableObject & ObservableProperty (examples)](007-observable-object-property-examples.md)
- [002 — Command Binding](002-command-binding.md)
- [002V — Command Binding (verbose companion)](002-command-binding-verbose.md)
- [008 — RelayCommand in Depth](008-relay-command.md)
- [013 — Data Validation with ObservableValidator](../intermediate/013-data-validation.md)
- [CommunityToolkit.Mvvm Docs: ObservableProperty](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observableproperty)
