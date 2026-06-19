---
tier: reference
topic: data access
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 005-repository-unit-of-work.md
---

# 005V — Repository / Unit of Work: An In-Depth Companion

You should already have read: [005 — Repository / Unit of Work](005-repository-unit-of-work.md) for the quick-start version. This file goes deeper on every section.

## Prerequisites

- EF Core familiarity (`DbContext`, `DbSet<T>`, migrations)
- DI registration lifetimes (`AddScoped`, `AddTransient`, `AddSingleton`)
- Understanding of `Expression<Func<T, bool>>` for specification patterns

---

## 1. IRepository<T> — Deep Dive

### Why not IQueryable?

Many generic repository implementations expose `IQueryable<T>` to allow callers to build queries:

```csharp
public interface IRepository<T>
{
    IQueryable<T> Query();
    // ...
}
```

This defeats much of the abstraction benefit — callers become coupled to `IQueryable` semantics (LINQ-to-Entities vs. LINQ-to-Objects), and switching from EF Core to LiteDB (which does not implement `IQueryable`) becomes a rewrite. The explicit method signatures (`GetAllAsync`, `FindAsync`) in the core file keep the abstraction clean.

### Specification pattern as an alternative to `FindAsync`

When predicate expressions grow complex, extract them into specification objects:

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; }
    Func<IQueryable<T>, IQueryable<T>>? Include { get; }
}

public sealed class ActiveItemsSpecification : ISpecification<TodoItem>
{
    public Expression<Func<TodoItem, bool>> Criteria =>
        item => !item.IsComplete;

    public Func<IQueryable<TodoItem>, IOrderedQueryable<TodoItem>>? OrderBy =>
        q => q.OrderByDescending(i => i.CreatedAt);

    public Func<IQueryable<TodoItem>, IQueryable<TodoItem>>? Include => null;
}
```

Extend the repository:

```csharp
public interface IRepository<T>
{
    Task<IReadOnlyList<T>> FindAsync(ISpecification<T> spec);
}

public async Task<IReadOnlyList<T>> FindAsync(ISpecification<T> spec)
{
    IQueryable<T> query = _db.Set<T>();
    query = query.Where(spec.Criteria);
    if (spec.OrderBy is not null) query = spec.OrderBy(query);
    if (spec.Include is not null) query = spec.Include(query);
    return await query.ToListAsync();
}
```

### Read-only vs. writable repositories

Some architectures split the interface into read and write sides:

```csharp
public interface IReadRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
}

public interface IWriteRepository<T>
{
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}
```

ViewModels that only query inject `IReadRepository<T>`; ViewModels that mutate inject `IWriteRepository<T>`. This follows the CQRS principle at a micro scale.

### Handling navigation properties

EF Core navigation properties are not loaded by default. The repository must support eager loading:

```csharp
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(Guid id, Func<IQueryable<T>, IQueryable<T>>? include = null);
}

public async Task<T?> GetByIdAsync(Guid id, Func<IQueryable<T>, IQueryable<T>>? include = null)
{
    IQueryable<T> query = _db.Set<T>();
    if (include is not null) query = include(query);
    return await query.SingleOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
}

// Usage
var project = await repo.GetByIdAsync(id, q => q.Include(p => p.Todos));
```

---

## 2. IUnitOfWork — Deep Dive

### Why not just call SaveChangesAsync directly?

Without a `UnitOfWork` abstraction, each ViewModel or service calls `_db.SaveChangesAsync()` independently. If two repository operations must succeed or fail together, you need a shared coordinator:

```csharp
// Without UoW — fragile
await _todos.AddAsync(item);
await _projects.UpdateAsync(project);
await _db.SaveChangesAsync(); // Which _db? Who manages the transaction?

// With UoW — coordinated
_uow.Todos.Add(item);
_uow.Projects.Update(project);
await _uow.SaveChangesAsync(); // Single commit point
```

### UnitOfWork with multiple DbContexts

In larger applications, a unit of work may span multiple databases:

```csharp
public sealed class MultiDbUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _appDb;
    private readonly AuditDbContext _auditDb;
    private IDbContextTransaction? _appTx;
    private IDbContextTransaction? _auditTx;

    public MultiDbUnitOfWork(AppDbContext appDb, AuditDbContext auditDb)
    {
        _appDb = appDb;
        _auditDb = auditDb;
        Todos = new Repository<TodoItem>(appDb);
        AuditLogs = new Repository<AuditEntry>(auditDb);
    }

    public IRepository<TodoItem> Todos { get; }
    public IRepository<AuditEntry> AuditLogs { get; }

    public async Task<int> SaveChangesAsync()
    {
        var app = await _appDb.SaveChangesAsync();
        var audit = await _auditDb.SaveChangesAsync();
        return app + audit;
    }

    public async Task BeginTransactionAsync()
    {
        _appTx = await _appDb.Database.BeginTransactionAsync();
        _auditTx = await _auditDb.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_appTx is not null) await _appTx.CommitAsync();
        if (_auditTx is not null) await _auditTx.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        if (_appTx is not null) await _appTx.RollbackAsync();
        if (_auditTx is not null) await _auditTx.RollbackAsync();
    }

    public void Dispose()
    {
        _appTx?.Dispose();
        _auditTx?.Dispose();
    }
}
```

### IDbContextFactory and transient UoW

The core file recommends `IDbContextFactory` with transient `IUnitOfWork` for desktop apps. Here is why:

EF Core's `DbContext` tracks entities. In a long-lived desktop ViewModel, a scoped or singleton `DbContext` accumulates tracked entities indefinitely, causing memory growth and stale data. `IDbContextFactory` creates short-lived contexts per operation:

```csharp
// Registration
builder.Services.AddSingleton<IDbContextFactory<AppDbContext>>(provider =>
    new DbContextFactory<AppDbContext>(
        provider.GetRequiredService<DbContextOptions<AppDbContext>>()
    ));
builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();

// UnitOfWork creates a new context each time
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly AppDbContext _db;

    public UnitOfWork(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
        _db = factory.CreateDbContext();
        Todos = new Repository<TodoItem>(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
```

Each command gets a fresh context, avoiding stale tracking.

---

## 3. LiteDB — Single Writer Semantics

LiteDB uses a single-writer model — only one write transaction can be active at a time. This makes a formal `IUnitOfWork` unnecessary:

```csharp
public sealed class LiteDbService
{
    private readonly LiteDatabase _db;

    public LiteDbService(string path)
    {
        _db = new LiteDatabase($"Filename={path};Connection=direct");
    }

    public ILiteCollection<T> GetCollection<T>() where T : class =>
        _db.GetCollection<T>();

    // Transaction-like behavior via the database itself
    public void BulkInsert<T>(IEnumerable<T> items) where T : class
    {
        var col = _db.GetCollection<T>();
        col.InsertBulk(items);
        // No explicit commit — LiteDB auto-commits
    }
}
```

The `LiteDatabase` instance manages its own internal transaction. If you need rollback semantics, use `LiteDatabase.BeginTrans()` / `Commit()` / `Rollback()` directly on the service.

### Repository implementation for LiteDB

```csharp
public sealed class LiteRepository<T> : IRepository<T> where T : class
{
    private readonly LiteDbService _db;

    public LiteRepository(LiteDbService db) => _db = db;

    public Task<T?> GetByIdAsync(Guid id)
    {
        var result = _db.GetCollection<T>().FindById(new BsonValue(id));
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<T>> GetAllAsync()
    {
        var results = _db.GetCollection<T>().FindAll().ToList();
        return Task.FromResult<IReadOnlyList<T>>(results);
    }

    public Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        var results = _db.GetCollection<T>().Find(predicate).ToList();
        return Task.FromResult<IReadOnlyList<T>>(results);
    }

    public void Add(T entity) => _db.GetCollection<T>().Insert(entity);
    public void Update(T entity) => _db.GetCollection<T>().Update(entity);
    public void Remove(T entity) => _db.GetCollection<T>().Delete(entity);
}
```

---

## 4. Testing with Repository Abstractions

The primary benefit of the repository pattern is testability. An in-memory test double makes ViewModel tests fast and deterministic:

```csharp
// Test double
public sealed class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly Dictionary<Guid, T> _items = new();

    public Task<T?> GetByIdAsync(Guid id) =>
        Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

    public Task<IReadOnlyList<T>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<T>>(_items.Values.ToList());

    public Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        return Task.FromResult<IReadOnlyList<T>>(
            _items.Values.Where(compiled).ToList()
        );
    }

    public void Add(T entity) => _items.Add(GetId(entity), entity);
    public void Update(T entity) => _items[GetId(entity)] = entity;
    public void Remove(T entity) => _items.Remove(GetId(entity));
}
```

For EF Core, use the in-memory provider:

```csharp
[Fact]
public async Task SaveTodoItem_persists_to_database()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase("TestDb")
        .Options;

    await using var db = new AppDbContext(options);
    var repo = new Repository<TodoItem>(db);
    repo.Add(new TodoItem(Guid.NewGuid(), "Test", false, DateTime.UtcNow));
    await db.SaveChangesAsync();

    var items = await repo.GetAllAsync();
    Assert.Single(items);
}
```

---

## 5. Out-of-Scope: REST API Backends

When the data source is a REST API, the repository maps to HTTP calls rather than a database:

```csharp
public sealed class ApiRepository<T> : IRepository<T> where T : class
{
    private readonly HttpClient _http;
    private readonly string _endpoint;

    public ApiRepository(HttpClient http, string endpoint)
    {
        _http = http;
        _endpoint = endpoint;
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        var response = await _http.GetAsync($"{_endpoint}/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
        var items = await _http.GetFromJsonAsync<List<T>>(_endpoint);
        return items?.AsReadOnly() ?? Array.Empty<T>();
    }

    // Add/Update/Remove map to POST/PUT/DELETE
}
```

Note: Unit of Work has no meaning over HTTP — each request is stateless. In this scenario, only `IRepository<T>` is needed.

---

## Key Takeaways (Expanded)

- `IRepository<T>` hides the data source; switch implementations without touching ViewModels
- `IUnitOfWork` coordinates multi-repository transactions; essential for EF Core, unnecessary for LiteDB or REST APIs
- **Prefer transient UoW in desktop apps** — `IDbContextFactory` + transient `IUnitOfWork` avoids stale entity tracking in long-lived ViewModels
- **Test doubles are the real payoff** — in-memory or in-memory-database repositories make data-access tests fast, deterministic, and infrastructure-free
- **Specification pattern** keeps complex queries reusable and testable without leaking `IQueryable`
- **LiteDB is single-writer** — its database instance acts as its own unit of work; use `BeginTrans` explicitly only when needed
