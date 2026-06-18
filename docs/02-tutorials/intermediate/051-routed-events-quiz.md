---
tier: intermediate
topic: events
estimated: 5-8 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 051-routed-events.md
---

# Quiz — Routed Events

```quiz
Q: A Button is inside a StackPanel inside a Window. The user clicks the Button. In what order do bubble-phase handlers fire?
A. Window, StackPanel, Button || Bubble events start at the source and travel upward to the root.
B. Button, StackPanel, Window (correct) || Bubble events fire on the source element first, then travel up through each parent. The Button fires first, then StackPanel, then Window.
C. Window, Button, StackPanel || That would be tunnel order, not bubble order.
D. Button only — events do not bubble || Bubble is the default routing strategy for most input events; they do travel up the tree.
Explanation: Bubble routing fires on the source first (Button), then travels upward through each parent (StackPanel → Window).
```

```quiz
Q: How do you subscribe to the tunnel phase of PointerPressed in code?
A. AddHandler(InputElement.PointerPressedEvent, handler) || That subscribes to the bubble phase by default.
B. AddHandler(InputElement.PointerPressedEvent, handler, RoutingStrategies.Tunnel) (correct) || Pass RoutingStrategies.Tunnel as the third parameter to AddHandler. Avalonia does not have separate Preview* CLR events.
C. Control.PointerPressed += handler || This subscribes to the bubble phase only via the CLR event wrapper.
D. AddHandler(InputElement.PointerPressedEvent, handler, handledEventsToo: true) || handledEventsToo controls whether handled events are received, not which routing phase.
Explanation: Avalonia unifies tunnel and bubble on the same RoutedEvent. Subscribe to tunnel phase by passing RoutingStrategies.Tunnel to AddHandler.
```

```quiz
Q: A custom routed event is registered with RoutingStrategies.Bubble. A parent sets e.Handled = true in its handler for this event. Which statement is true?
A. The event stops routing immediately and no other handler fires. || The handled event still routes to the next element, but only handlers registered with handledEventsToo: true receive it.
B. The event continues routing but only handledEventsToo handlers fire farther up the tree. (correct) || Setting Handled = true does not physically stop the route — it signals that the event is considered handled. Handlers registered with handledEventsToo: true still receive it.
C. The event fires again in the tunnel phase. || Routing strategies are fixed at registration time; a Bubble event does not also tunnel.
D. The Handled flag is reset when the event reaches the root. || Handled persists through the entire route.
Explanation: Handled = true prevents normal handlers from being invoked, but handledEventsToo handlers still receive the event.
```

```quiz
Q: Which of these is true about Avalonia's custom routed event registration compared to WPF?
A. Avalonia uses EventManager.RegisterRoutedEvent with typeof() arguments. || That is the WPF pattern.
B. Avalonia uses RoutedEvent.Register<TOwner, TArgs>() with generic type parameters. (correct) || Avalonia's registration uses generic type parameters for both the owner type and the event args type, providing stronger typing than WPF's typeof() approach.
C. Avalonia does not support custom routed events. || Custom routed events are fully supported via RoutedEvent.Register.
D. Avalonia requires a separate RoutedEventHandler delegate type. || Avalonia uses EventHandler<TEventArgs> instead of a dedicated delegate type.
Explanation: Avalonia uses generic RoutedEvent<TEventArgs> and registers with RoutedEvent.Register<TOwner, TArgs>(name, strategy).
```

```quiz
Q: You are building a custom control that needs to intercept PointerPressed before any instance handler can mark it as handled. What mechanism should you use?
A. Override OnPointerPressed and set e.Handled = true. || Instance-level OnPointerPressed runs after class handlers and after instance handlers attached with +=.
B. Register a class handler via AddClassHandler in the static constructor. (correct) || Class handlers fire before any instance handler. They are the correct mechanism for implementing control-level input interception.
C. Subscribe in the constructor with AddHandler and RoutingStrategies.Tunnel. || Instance handlers in the constructor still fire after class handlers.
D. Use handledEventsToo: true. || handledEventsToo observes events that were already handled; it does not let you intercept before other handlers.
Explanation: Class handlers (AddClassHandler) fire before instance handlers. They are the recommended pattern for control implementors who need first access to input events.
```
