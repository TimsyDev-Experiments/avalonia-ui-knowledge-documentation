---
tier: intermediate
topic: data access
estimated: 20 min
researched: 2026-06-13
avalonia-version: 12.0.4
---

# 048 -- SQLite / Local Database

**What you'll learn:** Store and query local data with LiteDB (embedded NoSQL) and EF Core + SQLite, wire both into DI, and handle database file paths in a cross-platform way.

**Prerequisites:** [032 -- Dependency Injection for MVVM](../advanced/032-mvvm-di-wiring.md), [015 -- Item Lists](015-item-lists.md)

---

## 1. LiteDB (embedded NoSQL)

LiteDB is a lightweight, serverless NoSQL database stored as a single file. It requires zero configuration.

```shell
dotnet add package LiteDB
```

### 1a. Data model

```csharp
public class TodoItem
{
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public string Title { get; set; } = "";
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 1b. Database service

```csharp
public class LiteDbService
{
    private readonly string _connectionString;

    public LiteDbService()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyApp", "data.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = dbPath;
    }

    public List<TodoItem> GetAll() =>
        UsingDb(db => db.GetCollection<TodoItem>("todos")
                        .FindAll()
                        .ToList());

    public void Insert(TodoItem item) =>
        UsingDb(db => db.GetCollection<TodoItem>("todos").Insert(item));

    public void Update(TodoItem item) =>
        UsingDb(db => db.GetCollection<TodoItem>("todos").Update(item));

    public void Delete(ObjectId id) =>
        UsingDb(db => db.GetCollection<TodoItem>("todos").Delete(id));

    private void UsingDb(Action<LiteDatabase> action)
    {
        using var db = new LiteDatabase(_connectionString);
        action(db);
    }
}
```

### 1c. Register and use

```csharp
// Program.cs
builder.Services.AddSingleton<LiteDbService>();
```

```csharp
public partial class TodoViewModel : ObservableObject
{
    private readonly LiteDbService _db;

    public ObservableCollection<TodoItem> Items { get; } = new();

    public TodoViewModel(LiteDbService db)
    {
        _db = db;
        Items = new ObservableCollection<TodoItem>(_db.GetAll());
    }

    [RelayCommand]
    private void AddItem(string title)
    {
        var item = new TodoItem { Title = title };
        _db.Insert(item);
        Items.Add(item);
    }
}
```

## 2. EF Core + SQLite (relational)

For relational data, migrations, and LINQ queries.

```shell
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### 2a. DbContext

```csharp
public class AppDbContext : DbContext
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    public AppDbContext() { }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyApp", "ef-data.db");
            options.UseSqlite($"Data Source={dbPath}");
        }
    }
}
```

### 2b. Registration

```csharp
// Program.cs
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "MyApp", "ef-data.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
```

### 2c. Migrations

```shell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 2d. Usage in ViewModel

```csharp
public partial class EfTodoViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    [ObservableProperty]
    private ObservableCollection<TodoItem> _items = new();

    public EfTodoViewModel(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var items = await db.Todos.OrderBy(t => t.CreatedAt).ToListAsync();
        Items = new ObservableCollection<TodoItem>(items);
    }

    [RelayCommand]
    private async Task AddItemAsync(string title)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var item = new TodoItem { Title = title };
        db.Todos.Add(item);
        await db.SaveChangesAsync();
        Items.Add(item);
    }
}
```

Use `IDbContextFactory` over direct `DbContext` injection to avoid lifetime issues in long-running desktop apps.

## 3. Cross-platform data directory

| Platform | `SpecialFolder.LocalApplicationData` resolves to |
|---|---|
| Windows | `C:\Users\{user}\AppData\Local\MyApp\` |
| Linux   | `~/.local/share/MyApp/` |
| macOS   | `~/Library/Application Support/MyApp/` |

Always create the directory at startup:

```csharp
var appDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "MyApp");
Directory.CreateDirectory(appDir);
var dbPath = Path.Combine(appDir, "data.db");
```

## 4. LiteDB vs EF Core + SQLite

| Concern | LiteDB | EF Core + SQLite |
|---|---|---|
| Setup | One NuGet, no migrations | Multiple packages, migrations required |
| Data model | Document/NoSQL (BSON) | Relational (SQL) |
| Queries | Lambda expressions, indexes | Full LINQ, SQL |
| Concurrency | Single-writer file lock | Connection pooling, row-level |
| File size | Larger per document | Compact, normalized |
| Best for | Simple data, config, small apps | Complex queries, existing EF skills |

## Key takeaways

- LiteDB needs zero config — perfect for local-first desktop apps
- EF Core + SQLite needs `IDbContextFactory` to avoid stale tracking in long-lived ViewModels
- Use `Environment.SpecialFolder.LocalApplicationData` for cross-platform data paths
- Always `Directory.CreateDirectory` at startup to ensure the folder exists
- LiteDB uses `ObjectId`; EF Core can use `int` with auto-increment or `Guid`

---

## See Also

- [032 -- Dependency Injection for MVVM](../advanced/032-mvvm-di-wiring.md)
- [015 -- Item Lists](../intermediate/015-item-lists.md)
- [042 -- Multi-Targeting](../advanced/042-multi-targeting-desktop-browser-mobile.md)
- [LiteDB docs](https://litedb.org/)
- [EF Core docs](https://learn.microsoft.com/ef/core/)
- [048V -- SQLite / Local Database (verbose companion)](048-sqlite-local-database-verbose.md)
- [048X -- SQLite / Local Database (examples)](048-sqlite-local-database-examples.md)
