---
tier: reference
topic: data access
estimated: 10 min
researched: 2026-06-13
avalonia-version: 12.0.4
---

# Pattern 005 -- Repository / Unit of Work

## Problem

ViewModel code mixes data access logic (queries, filters, sorting) with presentation logic. When the data source changes (e.g., from LiteDB to a REST API), every ViewModel must be updated.

## Solution

Abstract data access behind `IRepository<T>` and coordinate writes through `IUnitOfWork`.

---

## 1. Repository interface

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}
```

### Generic implementation (EF Core)

```csharp
public sealed class Repository<T> : IRepository<T> where T : class
{
    private readonly AppDbContext _db;

    public Repository(AppDbContext db) => _db = db;

    public async Task<T?> GetByIdAsync(Guid id) =>
        await _db.Set<T>().FindAsync(id);

    public async Task<IReadOnlyList<T>> GetAllAsync() =>
        await _db.Set<T>().ToListAsync();

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await _db.Set<T>().Where(predicate).ToListAsync();

    public void Add(T entity)    => _db.Set<T>().Add(entity);
    public void Update(T entity) => _db.Set<T>().Update(entity);
    public void Remove(T entity) => _db.Set<T>().Remove(entity);
}
```

## 2. Unit of Work

Coordinates multiple repository operations into a single transaction.

```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<TodoItem> Todos { get; }
    IRepository<Project> Projects { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
```

### Implementation

```csharp
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private IDbContextTransaction? _tx;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Todos    = new Repository<TodoItem>(db);
        Projects = new Repository<Project>(db);
    }

    public IRepository<TodoItem> Todos    { get; }
    public IRepository<Project> Projects  { get; }

    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();

    public async Task BeginTransactionAsync() =>
        _tx = await _db.Database.BeginTransactionAsync();

    public async Task CommitAsync()
    {
        if (_tx is not null) await _tx.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        if (_tx is not null) await _tx.RollbackAsync();
    }

    public void Dispose() => _tx?.Dispose();
}
```

## 3. Registration

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

Desktop apps with long-lived ViewModels should use `IDbContextFactory` and create scoped `IUnitOfWork` instances per operation:

```csharp
builder.Services.AddSingleton<IDbContextFactory<AppDbContext>>();
builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();
```

## 4. ViewModel usage

```csharp
public partial class DashboardViewModel : ObservableObject
{
    private readonly IUnitOfWork _uow;

    public DashboardViewModel(IUnitOfWork uow) => _uow = uow;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        var todos    = await _uow.Todos.GetAllAsync();
        var projects = await _uow.Projects.FindAsync(p => p.IsActive);
        // Bind to properties...
    }

    [RelayCommand]
    private async Task TransferItemAsync(Guid itemId, Guid targetProjectId)
    {
        await _uow.BeginTransactionAsync();
        try
        {
            var item = await _uow.Todos.GetByIdAsync(itemId);
            if (item is null) return;

            item.ProjectId = targetProjectId;
            _uow.Todos.Update(item);
            await _uow.SaveChangesAsync();
            await _uow.CommitAsync();
        }
        catch
        {
            await _uow.RollbackAsync();
            throw;
        }
    }
}
```

## 5. Repository without EF Core (LiteDB)

```csharp
public sealed class LiteRepository<T> : IRepository<T> where T : class
{
    private readonly LiteDbService _db;

    public LiteRepository(LiteDbService db) => _db = db;

    public Task<T?> GetByIdAsync(Guid id) =>
        Task.FromResult(_db.GetCollection<T>().FindById(id));

    public Task<IReadOnlyList<T>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<T>>(_db.GetCollection<T>().FindAll().ToList());

    // ... etc
}
```

Because LiteDB is single-writer, a formal `IUnitOfWork` is usually unnecessary — the `LiteDatabase` instance itself acts as the unit of work.

## 6. When to use

| Scenario | Repository | Unit of Work |
|---|---|---|
| CRUD over EF Core | Yes | Yes, for multi-table transactions |
| LiteDB single-collection ops | Optional | Not needed |
| REST API backend | Yes (maps to HTTP calls) | No (stateless) |
| In-memory test data | Yes (test double) | No |

## Key takeaways

- `IRepository<T>` decouples query logic from ViewModels — switch data sources without touching VM code
- `IUnitOfWork` coordinates multi-repository writes in a single transaction
- For desktop apps, prefer `IDbContextFactory` + transient `IUnitOfWork` to avoid stale tracking
- LiteDB doesn't need a formal UoW — the database connection itself fills that role
- Test repositories with in-memory collections or `DbContextOptions.InMemory`

---

## See Also

- [048 -- SQLite / Local Database](../02-tutorials/intermediate/048-sqlite-local-database.md)
- [032 -- Dependency Injection for MVVM](../02-tutorials/advanced/032-mvvm-di-wiring.md)
- [Pattern 003 -- Async Initialization](003-async-initialization.md)
