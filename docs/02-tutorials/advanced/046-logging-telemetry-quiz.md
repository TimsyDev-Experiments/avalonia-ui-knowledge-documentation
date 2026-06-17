---
tier: advanced
topic: logging-telemetry
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 046-logging-telemetry.md
---

# Quiz — Logging & Telemetry

```quiz
Q: Why should structured logging placeholders be used instead of string interpolation in Serilog logging calls?
A. String interpolation is slower than structured placeholders || Performance is a secondary concern; the primary reason is different.
B. Structured placeholders make log values searchable in backends like Seq or Elastic, while interpolation produces unsearchable flat strings (correct) || With structured placeholders ({Count}) the value is stored as a separate field, enabling filtering and aggregation; string interpolation embeds the value in the message text.
C. String interpolation causes compile-time warnings in Avalonia projects || There are no such warnings — both syntaxes compile.
D. Structured placeholders are required by ILogger<T> in .NET || ILogger<T> accepts both syntaxes; the choice is about observability, not API requirements.
Explanation: Structured logging stores placeholder values as queryable fields, while string interpolation embeds them in opaque text, making analysis in log backends significantly harder.
```

```quiz
Q: How do you reduce log noise from the Microsoft namespace while keeping Debug-level logging for your own code?
A. Set Log.Logger.MinimumLevel to Error || This silences everything below Error, including your own Debug logs.
B. Use MinimumLevel.Override("Microsoft", LogEventLevel.Warning) while keeping MinimumLevel.Debug() as the default (correct) || .MinimumLevel.Debug() sets the global floor, .Override raises the threshold for specific namespaces, reducing framework noise while keeping app-level debug output.
C. Add a filtering rule that excludes Microsoft.* loggers || This approach works but MinimumLevel.Override is the explicit and more maintainable Serilog API for this purpose.
D. Set the ASPNETCORE_LOG_LEVELS environment variable || This affects ASP.NET Core logging, not Serilog configuration in a desktop app.
Explanation: MinimumLevel.Override raises the minimum level for a specific namespace prefix, allowing Debug-level logging for the app while suppressing verbose framework messages.
```

```quiz
Q: What must a LogEventSink do to safely add log entries to an ObservableCollection bound to the UI?
A. Use a lock statement around the Entries.Add call || Locking prevents race conditions but does not change thread affinity — the collection change notification still fires on the background thread.
B. Dispatch the Add operation to Dispatcher.UIThread.Post (correct) || The sink's Emit method runs on the thread that wrote the log; dispatching to the UI thread ensures the ObservableCollection's CollectionChanged event fires on the correct thread.
C. Set the collection's SyncRoot property || ObservableCollection does not have a SyncRoot property; this is a legacy ICollection pattern.
D. Call Application.Current.Dispatcher.InvokeAsync || The Avalonia API uses Dispatcher.UIThread, not Application.Current.Dispatcher.
Explanation: LogEventSink.Emit can be called from any thread. The tutorial dispatches Entries.Add to Dispatcher.UIThread.Post to ensure safe UI updates.
```

```quiz
Q: Which class provides the entry point for creating tracing spans in an OpenTelemetry-instrumented Avalonia service?
A. ILogger<T> || ILogger is for logging, not distributed tracing — it cannot create spans.
B. ActivitySource (correct) || ActivitySource creates Activity objects that represent spans in the OpenTelemetry tracing model, supporting tags, events, and child spans.
C. TracerProviderBuilder || This is used to configure and build the tracer provider, not to create individual spans.
D. DiagnosticSource || DiagnosticSource is a lower-level .NET mechanism; OpenTelemetry recommends ActivitySource for explicit instrumentation.
Explanation: ActivitySource is the recommended API for creating tracing spans. The tutorial shows a static ActivitySource instance that creates activities with StartActivity.
```

```quiz
Q: What is the correct approach to prevent sensitive data (passwords, tokens) from appearing in Serilog output?
A. Set MinimumLevel.Override for the security namespace || Namespace filtering avoids logging entire namespaces but does not filter individual sensitive properties.
B. Use Filter.ByExcluding to exclude log events containing Password or Token properties (correct) || Filter.ByExcluding evaluates each log event and drops it if the condition matches, preventing sensitive properties from reaching any sink.
C. Use Log.CloseAndFlush() after each sensitive operation || CloseAndFlush shuts down the logging system; it is not a filtering mechanism.
D. Store sensitive data in SecureString and Serilog automatically redacts it || Serilog does not automatically redact SecureString — explicit filtering or destructuring policies are required.
Explanation: The tutorial demonstrates Filter.ByExcluding to drop events with sensitive property keys, and an IDestructuringPolicy to redact Credential objects at the point of serialization.
```
