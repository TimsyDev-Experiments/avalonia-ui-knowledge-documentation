---
tier: intermediate
topic: messaging
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 014-imessenger-patterns.md
---

# 014V — IMessenger Patterns: An In-Depth Companion

**Why this exists:** The original tutorial shows the mechanics of sending and receiving messages. This companion explains *why the messenger pattern exists in MVVM*, *what each message type does internally*, *how weak references affect lifetime*, and *when to use messages vs. direct DI or events*.

**Cross-reference:** Original tutorial at [014-imessenger-patterns.md](014-imessenger-patterns.md).

---

## 1. Why a messenger — decoupling without ceremony

In a typical MVVM application, ViewModels often need to communicate without knowing about each other. Examples:

- A `ListViewModel` selects an item; a `DetailViewModel` needs to display its details.
- A `SettingsViewModel` changes a configuration; a `MainViewModel` needs to reload data.
- Any ViewModel needs to ask "is it safe to close?" without referencing the dirty-checker ViewModel directly.

**Three options for ViewModel-to-ViewModel communication:**

| Approach | Coupling | Testability | Complexity |
|---|---|---|---|
| Direct reference (VM knows VM) | Tight | Low | Low |
| Shared service via DI | Loose | High | Medium |
| Messenger | Loosest | High | Low |

The messenger is the loosest option: the sender does not know who receives, and the receiver does not know who sent. Both only know the message type.

**When NOT to use a messenger:**

- When one ViewModel logically owns another (e.g., a tab host tracks its active tab). Use direct reference injected via DI.
- When the communication is parent-to-child in a visual tree. Use bindings or `x:Reference`.
- When you need a return value from a specific recipient, not the first responder. Use a shared service or a `TaskCompletionSource` pattern instead.

**When to use a messenger:**

- The communication is broadcast (one sender, many receivers).
- The communication is event-like ("something happened, do what you need").
- The sender and receiver are in separate parts of the visual tree or separate windows.
- You want to add a new receiver without modifying the sender.

---

## 2. Message types — what each does internally

### ValueChangedMessage&lt;T&gt;

```csharp
public sealed class ItemSelectedMessage : ValueChangedMessage<string>
{
    public ItemSelectedMessage(string value) : base(value) { }
}
```

`ValueChangedMessage<T>` is the simplest message type. It carries a `Value` property of type `T`. The base class stores the value and makes it available to the receiver.

**Internal behavior:** When you call `Send(new ItemSelectedMessage(item))`, the messenger:

1. Looks up all registered recipients for `ItemSelectedMessage`.
2. For each recipient, invokes its registered handler (or `Receive` method).
3. The invocation happens synchronously on the caller's thread. If the handler blocks, the sender blocks.

**Why derive a named class instead of using `ValueChangedMessage<T>` directly:** Named message types serve as documentation. `ItemSelectedMessage` is self-explanatory; `ValueChangedMessage<string>` is not. Also, the type system ensures that different message types with the same payload type (`string`) do not conflict — a recipient that listens for `ItemSelectedMessage` will not receive `StatusMessage` even if both carry strings.

### RequestMessage&lt;T&gt;

```csharp
public sealed class SaveRequestMessage : RequestMessage<bool> { }
```

`RequestMessage<T>` is a special message that expects a reply. The sender blocks until a recipient calls `message.Reply(value)`. Internally, `RequestMessage<T>` uses a `TaskCompletionSource<T>`:

1. `Send` creates a `SaveRequestMessage` and calls `TaskCompletionSource<T>.Task.Wait()` (synchronously) or `await` (async version).
2. The recipient receives the message and calls `message.Reply(true)` or `message.Reply(false)`.
3. The `TaskCompletionSource` is signaled, the sender unblocks and receives the value.

**Important:** Only one recipient should reply to a `RequestMessage`. If multiple recipients reply, only the first reply is used; subsequent replies are ignored. If no one replies, the sender blocks forever — set a timeout or ensure at least one recipient registers.

**When to use `RequestMessage`:** For queries that need an answer before proceeding. Example: "Can I close the app?" — the app asks all dirty-checker ViewModels whether they have unsaved changes.

### Plain message (no payload)

```csharp
public sealed class StatusMessage { }
```

A message class with no data is a pure notification. It signals that something happened but carries no additional information. The receiver knows what to do based on the message type alone. Example: `UserLoggedOutMessage` — the receiver clears the current user data.

---

## 3. WeakReferenceMessenger — how weak references work

```csharp
WeakReferenceMessenger.Default.Register<ItemSelectedMessage>(this);
```

**What happens internally:**

1. The messenger stores a `WeakReference<object>` to `this` (your ViewModel), not a direct reference.
2. A `WeakReference` does not prevent garbage collection. If the ViewModel goes out of scope and has no other references, the GC can collect it.
3. Before dispatching a message, the messenger checks if the `WeakReference.Target` is still alive. If the target has been collected, the messenger skips that recipient.
4. Periodically (or on the next send), the messenger cleans up dead weak references from its internal dictionary.

**Why weak references are the default:** In large applications, ViewModels are created and destroyed frequently (e.g., tab close, navigation). If the messenger held strong references, every registered ViewModel would stay alive indefinitely — a memory leak. Weak references allow the GC to reclaim ViewModels even if they forgot to unregister.

**The tradeoff:** The delegate stored with the weak reference might capture additional objects. If you write:

```csharp
WeakReferenceMessenger.Default.Register<ItemSelectedMessage>(this, (r, m) =>
{
    ((DetailPanelViewModel)r).SelectedItem = m.Value;
});
```

The lambda captures the `(r, m)` parameters — no extra closure. But if you reference something from the enclosing scope:

```csharp
WeakReferenceMessenger.Default.Register<ItemSelectedMessage>(this, (r, m) =>
{
    _logger.Log("Received");  // captures `this` via the lambda
    ((DetailPanelViewModel)r).SelectedItem = m.Value;
});
```

The lambda might capture `this` from the enclosing scope as well as the weak reference. This creates a strong reference chain from the delegate to the closed-over objects. Be explicit: implement `IRecipient<T>` and call `Register`/`Unregister` manually, or use `ObservableRecipient`.

---

## 4. StrongReferenceMessenger — when to use it

```csharp
StrongReferenceMessenger.Default.Register<StatusMessage>(this);
```

`StrongReferenceMessenger` holds strong references to recipients. A registered ViewModel will **never** be garbage collected until it unregisters.

**Use StrongReferenceMessenger when:**

- The recipient has the same lifetime as the application (e.g., a logger, a shell ViewModel).
- You need deterministic cleanup and manage unregistration explicitly.
- The recipient is a short-lived object that must not be collected while registered (rare — usually a design smell).

**Never use StrongReferenceMessenger for:**

- Tab content ViewModels.
- Dialog ViewModels.
- Any ViewModel that can be created and discarded multiple times.

If you use `StrongReferenceMessenger`, you must call `Unregister` in `IDisposable.Dispose()` or `OnDeactivated()`. Forgetting causes memory leaks.

---

## 5. Tokens — channel filtering

```csharp
public static class Channels
{
    public const string Navigation = "Nav";
    public const string Data = "Data";
}

WeakReferenceMessenger.Default
    .Send(new ItemSelectedMessage(item), Channels.Navigation);

WeakReferenceMessenger.Default
    .Register<ItemSelectedMessage>(this, Channels.Navigation, (r, m) =>
    {
        ((DetailPanelViewModel)r).SelectedItem = m.Value;
    });
```

**What tokens do:** The messenger's internal dictionary is keyed by `(Type, Token)`. Without a token, the key is `(typeof(ItemSelectedMessage), null)`. With a token, it becomes `(typeof(ItemSelectedMessage), "Nav")`. A recipient registered with `Channels.Navigation` only receives messages sent with that same token.

**Why you need tokens:** Without tokens, every `ItemSelectedMessage` sent anywhere in the app reaches every recipient that listens for `ItemSelectedMessage`. In a large app, this causes unintended side effects. Tokens let you scope messages to a subsystem.

**Token types:** The token can be any object — string, enum, Guid, or a custom class. Use an enum or a `static class` with const strings for discoverability.

---

## 6. ObservableRecipient — lifecycle management

```csharp
public partial class MyViewModel : ObservableRecipient
{
}
```

`ObservableRecipient` extends `ObservableObject` and adds:

- A `Messenger` property (defaults to `WeakReferenceMessenger.Default`).
- `OnActivated()` — called when the ViewModel becomes active. Override to register messages.
- `OnDeactivated()` — calls `Messenger.UnregisterAll()`. Override to clean up other resources.
- `IsActive` — a bool property that you toggle to activate/deactivate.

**The recommended pattern:**

```csharp
public partial class DetailViewModel : ObservableRecipient,
    IRecipient<ItemSelectedMessage>
{
    protected override void OnActivated()
    {
        Messenger.Register<ItemSelectedMessage>(this);
    }

    public void Receive(ItemSelectedMessage message)
    {
        SelectedItem = message.Value;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();  // Unregisters all
    }
}
```

**Why `IsActive` matters:** When a ViewModel is not the active view (e.g., it is in a background tab), it should not process messages. Set `IsActive = false` when navigating away, and `IsActive = true` when navigating to. The messenger still receives messages but `ObservableRecipient` suppresses handler invocation when inactive.

---

## 7. Common mistakes

**Mistake 1: Registering in constructor, never unregistering.**

Fix: Use `ObservableRecipient` with `OnActivated`/`OnDeactivated`, or call `Unregister` explicitly in `Dispose`.

**Mistake 2: Sending from a background thread.**

Fix: The messenger dispatches synchronously on the caller's thread. If you send from a background thread, the handler runs on that thread and may update the UI directly. Use `Dispatcher.UIThread.Send()` or `Dispatcher.UIThread.Post()` to marshal to the UI thread before sending.

**Mistake 3: Using `RequestMessage` when no recipient is registered.**

Fix: The sender blocks forever. Always ensure a recipient is registered before sending, or wrap the send in a timeout:

```csharp
var tcs = new TaskCompletionSource<bool>();
var registration = WeakReferenceMessenger.Default.Register<SaveRequestMessage>(this, (r, m) =>
{
    m.Reply(true);
    tcs.TrySetResult(true);
});
var result = await Task.WhenAny(tcs.Task, Task.Delay(5000));
```

**Mistake 4: Message class is not sealed and has mutable state.**

Fix: Message types should be immutable (`sealed`, read-only properties). If the sender mutates the message after sending, the receiver may see inconsistent state.

---

## Key Takeaways

- `ValueChangedMessage<T>` for fire-and-forget with a payload. `RequestMessage<T>` for query-response. Plain classes for pure notifications.
- `WeakReferenceMessenger.Default` is the standard choice. Use `StrongReferenceMessenger` only when you manage lifetime deterministically.
- Tokens channel messages to specific subsystems — use an enum or const strings.
- `ObservableRecipient` handles registration lifecycle automatically via `OnActivated`/`OnDeactivated`.
- Messages are dispatched synchronously on the sender's thread. Marshal to UI thread if needed.
- Always unregister when done, or use weak references so GC can clean up.

---

## See Also

- [014 — IMessenger Patterns (original)](014-imessenger-patterns.md)
- [013 — Data Validation](013-data-validation.md) (uses messenger for dialog close signals)
- [016 — Window & Dialog Management](016-window-dialog-management.md) (shows messenger for dialog close)
- [CommunityToolkit.Mvvm Docs: Messenger](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/messenger)
- [014E — IMessenger Patterns (examples)](014-imessenger-patterns-examples.md)
- [MVVM Toolkit Messenger Skill](file:///C:/Users/tmher/.config/opencode/skills/mvvm-toolkit-messenger/SKILL.md)
