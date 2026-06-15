---
tier: intermediate
topic: data access
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 048-sqlite-local-database.md
---

# 048V — SQLite / Local Database: An In-Depth Companion

**What you'll learn in this companion:** Not just how to insert and query, but why LiteDB uses a different concurrency model than EF Core, what `IDbContextFactory` prevents, how `Environment.SpecialFolder.LocalApplicationData` resolves differently per OS, when to choose document vs relational storage for a desktop app, and how each decision affects your app's startup time, memory, and thread safety.

**Prerequisites:** [032 — Dependency Injection for MVVM](../advanced/032-mvvm-di-wiring.md), [015 — Item Lists](015-item-lists.md)

**You should already have read:** [048 — SQLite / Local Database](048-sqlite-local-database.md) for the quick-start version. This file goes deeper on every section.

---

## 1. LiteDB — Why Embedded NoSQL for Desktop Apps

### What LiteDB actually is

LiteDB is a **serverless, embedded, document-oriented NoSQL database** stored as a single file. "Serverless" means there is no separate database process — your app opens the file, reads/writes data, and closes the file. "Document-oriented" means it stores BSON documents (similar to JSON with typed fields), not relational rows.

The entire database is a single `.db` file. The maximum file size is 256 MB (the LiteDB v5 legacy limit; LiteDB v6+ has different limits). For a desktop app storing user data, this is more than sufficient.

### Why you would pick LiteDB over EF Core + SQLite

| Concern | LiteDB wins when |
|---|---|
| Setup time | You need one NuGet package and zero migrations |
| Schema changes | Your data model changes frequently — no migration scripts needed |
| Object shape | Your data is hierarchical (nested objects, arrays of sub-objects) |
| File inspection | You want to open the `.db` file with LiteDB Studio and see your data immediately |
| Threading | Your app is single-threaded or has light concurrency |

### The LiteDB concurrency model

```csharp
private void UsingDb(Action<LiteDatabase> action)
{
    using var db = new LiteDatabase(_connectionString);
    action(db);
}
```

Each call creates a **new** `LiteDatabase` instance, opens the file, performs the operation, and disposes the instance. LiteDB uses a single-writer file lock — only one process (or one `LiteDatabase` instance) can write at a time.

This pattern works because:
- A `LiteDatabase` instance is **not thread-safe** — you must not share it across threads
- Opening and closing LiteDB is fast (a few milliseconds)
- The file lock ensures write consistency

If you need concurrent reads from multiple threads, keep a single `LiteDatabase` instance (singleton) and use `LiteDatabase.BeginTrans()` / `Commit()` for write transactions. The singleton approach avoids the open/close overhead but requires you to manage thread safety yourself.

### ObjectId — why not auto-increment int

```csharp
public ObjectId Id { get; set; } = ObjectId.NewObjectId();
```

`ObjectId` is a 12-byte identifier (like MongoDB's ObjectId):
- 4 bytes: timestamp (seconds since Unix epoch)
- 5 bytes: random value
- 3 bytes: incrementing counter

It is **globally unique without a central sequence** — LiteDB can generate IDs on the client without querying the database for the next available number. This makes offline-write scenarios safe: two devices can create objects that will never collide.

Using `int` with auto-increment would require LiteDB to check the current max value + 1, adding a query round-trip per insert.

---

## 2. LiteDB Service Design — Why Action<T> Not Task<T>

```csharp
public List<TodoItem> GetAll() =>
    UsingDb(db => db.GetCollection<TodoItem>("todos").FindAll().ToList());
```

The `UsingDb` helper takes `Action<LiteDatabase>` (synchronous). LiteDB operations are synchronous because file I/O for a local database is fast enough that async overhead (state machine allocation, context switching) outweighs the benefit.

If you need async for a responsive UI (e.g., the database file is on a network share or OneDrive), wrap the entire `UsingDb` call in `Task.Run`:

```csharp
public Task<List<TodoItem>> GetAllAsync() =>
    Task.Run(() => UsingDb(db => db.GetCollection<TodoItem>("todos").FindAll().ToList()));
```

This moves the synchronous file I/O to a thread-pool thread, keeping the UI thread free.

### The collection name

```csharp
db.GetCollection<TodoItem>("todos")
```

LiteDB stores documents in named **collections** (analogous to SQL tables). The name (`"todos"`) is arbitrary but should be plural and lowercase by convention. LiteDB creates the collection lazily — the first insert creates it automatically.

---

## 3. EF Core + SQLite — Why It's Different from LiteDB

### The DbContext lifetime problem

```csharp
// Wrong for long-lived ViewModels
builder.Services.AddDbContext<AppDbContext>(options => ...);

// Correct for long-lived ViewModels
builder.Services.AddDbContextFactory<AppDbContext>(options => ...);
```

`AddDbContext` registers `AppDbContext` as scoped by default. In a desktop app, the DI container's default scope is the application lifetime (no HTTP request to scope to). A scoped DbContext in a desktop app effectively becomes singleton — same instance for the entire app lifetime.

The problem: DbContext tracks every entity it loads. After loading 1000 todo items and then loading 1000 more, the context holds 2000 tracked entities. Memory grows linearly. Queries slow down because the change tracker checks every tracked entity against query results.

`AddDbContextFactory` creates a **factory** that produces fresh, independent DbContext instances on demand. Each `CreateDbContextAsync()` call creates a new context that starts with zero tracked entities.

### Why not use AddDbContext with ServiceLifetime.Transient

```csharp
// Alternative — but not recommended
services.AddDbContext<AppDbContext>(options => ...,
    ServiceLifetime.Transient);
```

Transient lifetime means every injected instance is new. This works but couples your ViewModel to the lifetime management — the ViewModel must dispose each context. `IDbContextFactory` separates the responsibility: the factory owns creation, the consumer owns disposal.

### OnConfiguring fallback

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    if (!options.IsConfigured)
    {
        var dbPath = ...;
        options.UseSqlite($"Data Source={dbPath}");
    }
}
```

The `OnConfiguring` method is called by EF Core when no options have been configured externally (via `AddDbContext`). The `IsConfigured` check prevents overriding the external configuration.

This pattern is useful for:
- XAML designer support (designer creates DbContext without DI)
- Unit testing (test provides in-memory SQLite)
- Migration tooling (`dotnet ef` calls the parameterless constructor)

### The empty constructor

```csharp
public AppDbContext() { }
public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
```

EF Core's command-line tools (`dotnet ef migrations add`) need to create a `DbContext` instance at design time. If only the `DbContextOptions` constructor exists, the tools fail because they don't know how to resolve DI. The parameterless constructor is the fallback — the tools call it, then `OnConfiguring` configures the connection string.

---

## 4. Migrations — Why They Exist and When They Run

```shell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

`migrations add` generates a C# file in `Migrations/` that contains:
- `Up()` — SQL statements to apply the migration
- `Down()` — SQL statements to revert the migration

`database update` executes all pending migrations against the database.

For a desktop app, running `database update` on every startup is common:

```csharp
// Program.cs — after DI is built
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
db.Database.Migrate();
```

This ensures the database schema matches the current code, even if the user skipped an update. The `Migrate()` call is idempotent — it checks `__EFMigrationsHistory` table and applies only new migrations.

### Why not AutomaticMigration in desktop apps

EF Core does not support automatic migrations (the old EF6 feature). Manual migrations through `dotnet ef` are required. For desktop apps, this is fine — you generate migrations during development, and `Database.Migrate()` applies them at runtime.

---

## 5. Cross-Platform Data Directory — The OS Details

### Windows

`Environment.SpecialFolder.LocalApplicationData` → `C:\Users\{username}\AppData\Local`

The `AppData\Local` folder is per-user and per-machine (not roaming). Files here are not synced between machines by Windows roaming profiles. For an app named `MyApp`, the path becomes `C:\Users\{username}\AppData\Local\MyApp\data.db`.

### Linux

`Environment.SpecialFolder.LocalApplicationData` → `~/.local/share` (XDG_DATA_HOME)

Files here follow the XDG Base Directory specification. The app runs as the current user, so the path is `~/.local/share/MyApp/data.db`. This directory persists across logins but is not backed up by default.

### macOS

`Environment.SpecialFolder.LocalApplicationData` → `~/Library/Application Support`

Per-user app data goes in `~/Library/Application Support/MyApp/data.db`. This directory is backed up by Time Machine and iCloud (if the user enables it).

### Why Directory.CreateDirectory is required

```csharp
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
```

The parent directory (`MyApp/`) does not exist by default — the user has never launched this app before on this machine. `Directory.CreateDirectory` creates the entire path if missing. It is idempotent — calling it on every startup is harmless.

### Why not the app's own directory

```csharp
// Bad — saved next to the EXE
var dbPath = Path.Combine(AppContext.BaseDirectory, "data.db");
```

On Windows, installing to `Program Files` makes the directory read-only for standard users. Even for per-user installs, the OS may virtualize writes to a different location. Always use `LocalApplicationData`.

---

## 6. LiteDB vs EF Core + SQLite — The Deep Comparison

### File size and performance

LiteDB stores data in BSON format (Binary JSON), which includes field names with every document. A document like `{ "Title": "Buy milk", "IsComplete": false }` stores the strings `"Title"` and `"IsComplete"` for every document. EF Core + SQLite stores data in relational rows — column names appear once in the schema, not per row.

For 10,000 simple objects:
- LiteDB: ~3-5 MB
- SQLite: ~1-2 MB

For complex nested objects:
- LiteDB stores them naturally (BSON supports nesting)
- SQLite requires JOINs or JSON columns

### Query capabilities

LiteDB supports:
- Lambda expressions: `Query.EQ("Title", "Buy milk")`
- LINQ: `collection.Find(x => x.Title.Contains("milk"))`
- Indexes: `collection.EnsureIndex(x => x.Title)`

EF Core + SQLite supports:
- Full LINQ: `db.Todos.Where(t => t.Title.Contains("milk")).OrderBy(t => t.CreatedAt)`
- Raw SQL: `db.Database.SqlQueryRaw("SELECT * FROM Todos")`
- Complex joins, group by, aggregations, subqueries

If your queries involve joins across multiple tables or complex aggregations, EF Core + SQLite is more natural.

### Concurrency

LiteDB uses a **single-writer lock per file**. Two processes cannot write to the same file simultaneously. For a single-user desktop app, this is fine — there is only one process.

EF Core + SQLite uses SQLite's native locking (file-level, with WAL mode for concurrent readers). Multiple processes can read simultaneously; only one writer at a time. For a desktop app with background services, this is still fine.

### When EF Core is overkill

If your "database" is just a few settings or a small list of items, using EF Core is like hiring a data engineer to sort your desk drawer. LiteDB is simpler, has no migrations, and produces inspectable files.

---

## 7. Data Model Differences

### LiteDB model

```csharp
public class TodoItem
{
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public string Title { get; set; } = "";
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

Property initializers (`= ""`, `= DateTime.UtcNow`) prevent null reference issues in serialization and ensure default values.

### EF Core model for SQLite

For EF Core, you might use:

```csharp
public class TodoItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

EF Core with SQLite does not auto-increment `int Id` by default — use `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` or configure in `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<TodoItem>(entity =>
    {
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
    });
}
```

---

## Key Takeaways

- LiteDB is document-based with no schema migrations — use for simple data, config, or offline-first apps
- EF Core + SQLite is relational with full LINQ — use for complex queries, existing SQL skills, or when you need migrations
- `IDbContextFactory` prevents the "stale tracking" problem in long-lived desktop app ViewModels
- `Environment.SpecialFolder.LocalApplicationData` resolves to different paths on each OS — always use it for database files
- `Directory.CreateDirectory` is idempotent — call it on every startup to ensure the database directory exists
- `ObjectId` in LiteDB is globally unique without querying the database — safe for offline generation
- For async UI, wrap synchronous LiteDB calls in `Task.Run`; EF Core + SQLite operations are async natively
- EF Core's `Database.Migrate()` applies pending migrations at startup — call it once after DI is built

---

## See Also

- [048 — SQLite / Local Database (original)](048-sqlite-local-database.md)
- [048X — SQLite / Local Database (examples)](048-sqlite-local-database-examples.md)
- [032 — Dependency Injection for MVVM](../advanced/032-mvvm-di-wiring.md)
- [015 — Item Lists](015-item-lists.md)
- [042 — Multi-Targeting](../advanced/042-multi-targeting-desktop-browser-mobile.md)
- [LiteDB docs](https://litedb.org/)
- [EF Core docs](https://learn.microsoft.com/ef/core/)
