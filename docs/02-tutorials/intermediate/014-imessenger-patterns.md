---
tier: intermediate
topic: messaging
estimated: 10 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 014 — IMessenger Patterns

**What you'll learn:** Decouple ViewModels using `WeakReferenceMessenger`, send and receive messages, and use request messages for ViewModel-to-ViewModel queries.

**Prerequisites:** [007 — ObservableObject & ObservableProperty](/docs/02-tutorials/basics/007-observable-object-property.md)

---

## 1. The message types

Define message classes for each communication channel:

```csharp
// Messages/ItemSelectedMessage.cs
using CommunityToolkit.Mvvm.Messaging;

public sealed class ItemSelectedMessage : ValueChangedMessage<string>
{
    public ItemSelectedMessage(string value) : base(value) { }
}

// Messages/SaveRequestMessage.cs
public sealed class SaveRequestMessage : RequestMessage<bool> { }

// Messages/StatusMessage.cs (simple notification, no payload)
public sealed class StatusMessage { }
```

---

## 2. Sending a message

```csharp
public partial class ItemListViewModel : ObservableObject
{
    [RelayCommand]
    private void SelectItem(string item)
    {
        WeakReferenceMessenger.Default
            .Send(new ItemSelectedMessage(item));
    }
}
```

---

## 3. Receiving a message (via IRecipient)

```csharp
public partial class DetailPanelViewModel : ObservableObject,
    IRecipient<ItemSelectedMessage>
{
    public DetailPanelViewModel()
    {
        WeakReferenceMessenger.Default
            .Register<ItemSelectedMessage>(this);
    }

    public void Receive(ItemSelectedMessage message)
    {
        SelectedItem = message.Value;
    }

    [ObservableProperty]
    private string _selectedItem = string.Empty;
}
```

---

## 4. Request messages (request-response pattern)

```csharp
// Sender (needs an answer)
public partial class MainViewModel : ObservableObject
{
    [RelayCommand]
    private async Task CheckSaveAsync()
    {
        var canSave = await WeakReferenceMessenger.Default
            .Send<SaveRequestMessage>();

        if (canSave)
        {
            // Proceed with save
        }
    }
}

// Responder
public partial class DirtyCheckerViewModel : ObservableObject,
    IRecipient<SaveRequestMessage>
{
    public void Receive(SaveRequestMessage message)
    {
        message.Reply(HasUnsavedChanges is false);
    }

    public bool HasUnsavedChanges { get; set; }
}
```

Request messages unblock the sender until a reply is received.

---

## 5. StrongReferenceMessenger (no WeakReference)

```csharp
using CommunityToolkit.Mvvm.Messaging;

// Use StrongReferenceMessenger if you want deterministic cleanup
// Register manually, unregister explicitly
WeakReferenceMessenger.Default.Register<StatusMessage>(this);

// Always unregister when done
WeakReferenceMessenger.Default.Unregister<StatusMessage>(this);
```

`WeakReferenceMessenger` uses weak references — subscribers can be garbage collected without explicit unregistration. Use `StrongReferenceMessenger` only when you need deterministic lifetime management.

---

## 6. Message tokens (channel filtering)

```csharp
// Different channels for different scopes
public static class Channels
{
    public const string Navigation = "Nav";
    public const string Data = "Data";
}

// Send on a specific channel
WeakReferenceMessenger.Default
    .Send(new ItemSelectedMessage(item), Channels.Navigation);

// Receive from that channel
WeakReferenceMessenger.Default
    .Register<ItemSelectedMessage>(this, Channels.Navigation, (r, m) =>
    {
        ((DetailPanelViewModel)r).SelectedItem = m.Value;
    });
```

---

## 7. Cleanup with ObservableRecipient

```csharp
public partial class MyViewModel : ObservableRecipient
{
    // ObservableRecipient:
    // - Owns an IMessenger instance (or uses Default)
    // - Calls UnregisterAll() in OnDeactivated()
    // - Register messages in OnActivated()
}
```

---

## Key Takeaways

- `ValueChangedMessage<T>` for fire-and-forget with payload
- `RequestMessage<T>` for query-response patterns
- Use tokens for channel separation
- `ObservableRecipient` manages messenger lifecycle automatically
- Prefer `WeakReferenceMessenger.Default` unless you need strong references

---

## See Also

- [CommunityToolkit.Mvvm Docs: Messenger](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/messenger)
- [MVVM Toolkit Messenger Skill](file:///C:/Users/tmher/.config/opencode/skills/mvvm-toolkit-messenger/SKILL.md)
