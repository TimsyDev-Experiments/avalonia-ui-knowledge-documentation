---
tier: intermediate
topic: imessenger patterns
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 014-imessenger-patterns.md
---

# Quiz — IMessenger Patterns

```quiz
Q: Which message base class should you use for a fire-and-forget notification that carries a string payload?
A. RequestMessage<string> || Incorrect — RequestMessage is for request-response patterns that block until a reply is received.
B. ValueChangedMessage<string> (correct) || Correct. ValueChangedMessage<T> is designed for fire-and-forget messages with a typed payload.
C. AsyncRequestMessage<string> || Incorrect — AsyncRequestMessage is for async request-response, not simple fire-and-forget.
D. MessageBase || Incorrect — there is no MessageBase in CommunityToolkit.Mvvm; messages inherit from the generic types.
Explanation: ValueChangedMessage<T> extends the base message class with a Value property of type T, suitable for fire-and-forget notification messages.
```

```quiz
Q: How does a ViewModel send a SaveRequestMessage and wait for a boolean response?
A. WeakReferenceMessenger.Default.Send(new SaveRequestMessage()) and check the return value (correct) || Correct. Send<T>() on a RequestMessage<T> returns the reply synchronously.
B. WeakReferenceMessenger.Default.Publish(new SaveRequestMessage()) and subscribe to a callback || Incorrect — there is no Publish method; the messenger uses Send/Register.
C. Use Task.Run to send the message and poll for a response || Incorrect — RequestMessage blocks the sender and returns the reply directly.
D. WeakReferenceMessenger.Default.SendAsync(new SaveRequestMessage()) || Incorrect — Send is synchronous for RequestMessage; no SendAsync overload exists.
Explanation: RequestMessage<T>.Send() blocks until a recipient calls message.Reply(value), returning the reply value to the sender.
```

```quiz
Q: What is the purpose of a message token when sending a message?
A. It authenticates the sender to prevent unauthorized messages || Incorrect — tokens are not a security feature.
B. It creates a named channel so recipients can filter which messages they receive (correct) || Correct. Tokens act as channel identifiers, allowing selective registration.
C. It assigns a priority level to the message || Incorrect — the messenger does not support priority queuing.
D. It enables the message to be serialized to JSON || Incorrect — tokens are runtime routing identifiers only.
Explanation: Pass a token to Send/Register to scope messages to a specific channel, enabling the same message type to be used in different contexts without interference.
```

```quiz
Q: Which base class automatically manages messenger registration lifecycle — registering in OnActivated and unregistering in OnDeactivated?
A. ObservableObject || Incorrect — ObservableObject has no messenger integration.
B. ObservableValidator || Incorrect — ObservableValidator adds validation, not messenger lifecycle.
C. ObservableRecipient (correct) || Correct. ObservableRecipient owns an IMessenger instance and automatically calls Register/UnregisterAll during activation/deactivation.
D. MessengerDefault || Incorrect — this class does not exist.
Explanation: ObservableRecipient is designed for ViewModels that need messaging — it handles messenger lifecycle so you don't need to manually unregister.
```

```quiz
Q: When should StrongReferenceMessenger be used instead of WeakReferenceMessenger?
A. Always — strong references are faster || Incorrect — WeakReferenceMessenger is the recommended default; strong references add cleanup burden.
B. When deterministic cleanup and explicit unregistration is required (correct) || Correct. StrongReferenceMessenger prevents GC collection of subscribers, so you must explicitly unregister when done.
C. When sending messages across AppDomain boundaries || Incorrect — neither messenger supports cross-AppDomain communication.
D. When the message payload is a value type || Incorrect — message payload type does not affect the choice between weak and strong references.
Explanation: WeakReferenceMessenger (the default) allows subscribers to be GC-collected without explicit unregistration. StrongReferenceMessenger should only be used when you need precise control over subscriber lifetimes.
```
