---
tier: basics
topic: mvvm
estimated: 6 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 007 — ObservableObject and ObservableProperty

**What you'll learn:** Create ViewModel properties that automatically notify the UI of changes using source generators.

**Prerequisites:** [002 — Command Binding](002-command-binding.md)

---

## 1. The manual way (before generators)

```csharp
private string _name;
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);
}
```

Works, but verbose. Every property needs the same pattern.

---

## 2. The source-generated way

```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;
}
```

This generates:

```csharp
public string Name
{
    get => _name;
    set
    {
        if (SetProperty(ref _name, value))
            OnPropertyChanged();
    }
}
```

The class **must** be `partial`. The field **must** have an underscore prefix (`_name`, `_count`, etc.).

---

## 3. Property change notification chaining

```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _firstName;

    [ObservableProperty]
    private string _lastName;

    public string FullName => $"{FirstName} {LastName}";
}
```

No manual property changed notification for `FullName` here — but if you need it:

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]
private string _firstName;
```

---

## 4. Property validation in setter

```csharp
[ObservableProperty]
private string _email;

partial void OnEmailChanged(string value)
{
    // Called automatically after the property is set
    if (!value.Contains('@'))
    {
        // handle validation...
    }
}
```

The partial method `On<PropertyName>Changed` is called by the generated setter.

---

## 5. Before-change notification

```csharp
partial void OnEmailChanging(string value)
{
    // Called before the property is set
}
```

---

## Key Takeaways

- `[ObservableProperty]` generates the public property and `INotifyPropertyChanged` notification
- The backing field must be `private` with underscore prefix
- Use `[NotifyPropertyChangedFor]` to chain dependent properties
- `partial void On<Name>Changing/Changed` hooks into the generated setter

---

## See Also

- [002 — Command Binding](002-command-binding.md)
- [008 — RelayCommand in Depth](008-relay-command.md)
- [012 — Data Validation with ObservableValidator](../intermediate/013-data-validation.md)
- [CommunityToolkit.Mvvm Docs: ObservableProperty](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observableproperty)
