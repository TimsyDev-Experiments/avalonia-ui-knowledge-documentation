---
tier: intermediate
topic: data access
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 048-sqlite-local-database.md
---

# Quiz — SQLite / Local Database

```quiz
Q: What is the primary advantage of LiteDB over EF Core + SQLite for simple local-first desktop applications?
A. LiteDB supports full SQL querying with JOINs || Incorrect — LiteDB is a NoSQL document store with lambda queries, not SQL; EF Core + SQLite excels at complex SQL queries.
B. LiteDB requires zero configuration and no migrations — one NuGet package and a file path is all you need (correct) || Correct — LiteDB is embeddable with no setup beyond installing the NuGet package and specifying a file path; no migrations or schema definitions are required.
C. LiteDB supports concurrent multi-user access with connection pooling || Incorrect — LiteDB uses a single-writer file lock; EF Core + SQLite handles concurrency better.
D. LiteDB automatically generates migration scripts for schema changes || Incorrect — LiteDB is schemaless (NoSQL document store) and does not use migration scripts.
Explanation: LiteDB's document model and zero-configuration setup make it ideal for simple local storage where full relational modeling is unnecessary.
```

```quiz
Q: Why is IDbContextFactory<AppDbContext> recommended over injecting AppDbContext directly into ViewModels?
A. IDbContextFactory creates a singleton DbContext shared across the app || Incorrect — IDbContextFactory creates short-lived DbContext instances, not a singleton.
B. Direct DbContext injection causes stale entity tracking and lifetime issues in long-lived ViewModels (correct) || Correct — ViewModels live longer than a single operation; a long-lived DbContext accumulates tracked entities and can return stale data. IDbContextFactory creates fresh contexts per operation.
C. IDbContextFactory is the only way to configure the SQLite connection string || Incorrect — connection strings can be configured in DI registration; IDbContextFactory does not affect that.
D. Direct DbContext injection does not support async queries || Incorrect — direct injection supports async queries; the issue is lifetime management, not async support.
Explanation: IDbContextFactory ensures a fresh DbContext per operation, avoiding stale tracked entities and cross-operation state pollution in long-running ViewModels.
```

```quiz
Q: Which API provides the correct cross-platform base directory for storing a local database file?
A. AppDomain.CurrentDomain.BaseDirectory || Incorrect — this points to the application's executable directory, which may not be writable on all platforms (especially macOS sandboxed apps).
B. Directory.GetCurrentDirectory() || Incorrect — the current directory is not guaranteed to be a stable, writable location for application data.
C. Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) (correct) || Correct — this resolves to the platform-appropriate app data directory: %LOCALAPPDATA% on Windows, ~/.local/share on Linux, and ~/Library/Application Support on macOS.
D. Path.GetTempPath() || Incorrect — the temp directory can be cleared by the OS at any time; it is not suitable for persistent database storage.
Explanation: LocalApplicationData provides the OS-designated per-user application data directory, which is the correct location for persistent local databases.
```

```quiz
Q: Identify the bug in this LiteDB service method:
    public void Insert(TodoItem item) =>
        UsingDb(db => db.GetCollection<TodoItem>("todos").Insert(item));
A. LiteDB requires the collection name to be the exact class name || Incorrect — collection names are arbitrary strings; "todos" is valid.
B. The Insert method does not exist on LiteCollection<T> || Incorrect — LiteCollection<T> does have an Insert method.
C. The `item` parameter will not have its Id populated after Insert because ObjectId is assigned in the constructor but the method signature does not return the new Id || Incorrect — ObjectId.NewObjectId() in the constructor assigns the Id before Insert; no return value issue.
D. The code is correct and follows the documented pattern (correct) || Correct — the UsingDb helper opens a LiteDatabase, calls Insert on the collection, and disposes the connection, following the tutorial's recommended pattern.
Explanation: The Insert method on LiteCollection<T> accepts a typed item and persists it; ObjectId.NewObjectId() in the constructor ensures the Id is set before insert.
```

```quiz
Q: Which concern favors choosing EF Core + SQLite over LiteDB?
A. The application stores simple key-value configuration pairs || Incorrect — LiteDB is better suited for simple config storage with its document model.
B. The application needs complex relational queries with JOINs, LINQ, and migrations (correct) || Correct — EF Core provides full LINQ support, SQL queries, relational modeling, and schema migrations, which are not available in LiteDB's NoSQL document model.
C. The application targets mobile platforms like Android and iOS || Incorrect — both LiteDB and EF Core + SQLite run on mobile platforms; this does not favor one over the other.
D. The application requires zero configuration with no schema definition || Incorrect — zero configuration is a strength of LiteDB, not EF Core + SQLite.
Explanation: EF Core + SQLite excels at relational data modeling, complex queries, and schema evolution through migrations — scenarios where LiteDB's document model falls short.
```
