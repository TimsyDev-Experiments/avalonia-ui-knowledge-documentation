---
title: Quiz
topic: 006-logging-patterns
type: quiz
---

# Quiz: Logging Patterns

```quiz
Q: Why should you use structured placeholders ({Property}) instead of string interpolation ($"Message {value}") in log calls?
A. String interpolation is not supported by ILogger<T> || String interpolation is syntactically valid but defeats structured logging
B. Placeholders are faster because they avoid string concatenation || Performance is a secondary benefit, not the main reason
C. Structured placeholders create named properties that log backends like Seq and Elasticsearch can index and query independently (correct) || When Serilog captures `LogInformation("Loaded {Count} items", count)`, it stores `Count` as a numeric property. You can then query `Count > 100` in Seq. String interpolation embeds the value in the message text, making it only full-text-searchable, not queryable as a field.
D. Placeholders automatically encode special characters for security || Structured logging does not provide automatic encoding or injection prevention beyond what the sink implements

Explanation: Structured placeholders capture values as typed, named properties in the log event. This enables indexed queries (Count > 100), numeric aggregation (average Count), and property-based filtering (UserId = "abc") in dedicated log backends. String interpolation embeds the value into the message template string, losing all structured metadata.
```

```quiz
Q: What is the recommended log level for an unhandled exception that crashes the application?
A. Warning || Warnings are for recoverable issues
B. Error || Errors are for recoverable failures like network timeouts
C. Critical (correct) || Critical is reserved for unrecoverable failures: database corruption, out-of-memory, startup failure, or unhandled exceptions that terminate the process. These events trigger immediate alerting in production monitoring.
D. Information || Information is for normal operational events

Explanation: The Critical level indicates an unrecoverable failure that threatens application stability. Unhandled exceptions, process-level corruption, and startup failures should all be logged at Critical level to distinguish them from recoverable errors (Error) and handled edge cases (Warning).
```

```quiz
Q: What happens to LogContext.PushProperty scopes when the code enters a Task.Run callback?
A. The scope is automatically preserved because AsyncLocal flows with the execution context || AsyncLocal does flow with ExecutionContext, but Task.Run by default does not capture ExecutionContext in modern .NET
B. The scope is lost because AsyncLocal does not flow across thread boundaries (correct) || LogContext.PushProperty uses AsyncLocal<T> internally. Task.Run does not capture the current ExecutionContext by default in .NET Core 3.0+, so scoped properties set via LogContext are not available inside the Task.Run callback. The scope must be captured explicitly with LogContext.Clone() or by passing properties as parameters.
C. The scope is preserved only in Debug builds || Build configuration has no effect on AsyncLocal flow
D. LogContext automatically merges scopes from the calling thread || There is no automatic merging; the new thread starts with an empty context

Explanation: LogContext.PushProperty relies on AsyncLocal<T>, which flows with ExecutionContext. Task.Run creates a new task that does not capture the calling ExecutionContext by default in modern .NET. To preserve scoped properties in background tasks, capture the scope with LogContext.Clone() before Task.Run and restore it inside the callback with a using block.
```

```quiz
Q: Which pattern should you use for high-frequency logging (e.g., called every frame) to avoid allocation overhead?
A. Use Log.Debug() directly with string interpolation || String interpolation still allocates the formatted string
B. Use LoggerMessage.Define or the [LoggerMessage] source generator (correct) || LoggerMessage.Define and the [LoggerMessage] source generator (C# 10+) cache the message template and delegate creation, producing zero allocations per call. The message template is parsed once at startup, and values are captured into a pre-defined struct rather than boxing into object[].
C. Increase the log level to Fatal so fewer events are written || This suppresses the logs rather than optimizing them
D. Use Console.WriteLine instead of ILogger || Console.WriteLine blocks the thread and lacks structured logging, level filtering, and sink flexibility

Explanation: The [LoggerMessage] source generator (or manual LoggerMessage.Define) creates a static Action<ILogger, T1, T2, ...> delegate with the template pre-compiled. Each invocation reuses the same delegate, avoiding the per-call allocation of the object[] array and the LogEventProperties dictionary. This is critical for hot paths like layout passes, render loops, or streaming data processing.
```

```quiz
Q: A password is accidentally passed as a log property. How should you prevent it from appearing in log output?
A. Change the log level so the event is suppressed || The event will still be suppressed only if the minimum level is above the event's level — not a reliable redaction strategy
B. Remove the password before calling LogInformation || This works but relies on developer discipline; defensive redaction at the sink level is safer
C. Use a Serilog IDestructuringPolicy or Filter.ByExcluding to redact or exclude properties matching sensitive names (correct) || A destructuring policy (by type) or filter (by property name) catches sensitive data before it reaches the output sink. For example, Filter.ByExcluding(e => e.Properties.ContainsKey("Password")) prevents any log event with a Password property from being written. This is a defense-in-depth approach that protects against accidental leaks even if the developer forgets to scrub the value.
D. Encrypt the log file after writing || Encryption protects the file at rest but the unencrypted data is still written to the sink

Explanation: Sink-level redaction is the last line of defense before data leaves the process. A destructuring policy transforms specific types (SecureString, PasswordBox) into a redacted placeholder. A filter excludes entire log events that contain sensitive property names. These mechanisms protect against accidental exposure even when the logging code itself does not explicitly sanitize values.
```
