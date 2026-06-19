---
tier: reference
topic: data access
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 005-repository-unit-of-work.md
---

# 005X — Repository / Unit of Work: Real-World Examples

## Example 1: EF Core Repository and Unit of Work for a Project Management App

A desktop application manages Projects and their associated TodoItems. Creating a project with default items must happen atomically.

### Domain models

```csharp
public sealed class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<TodoItem> Todos { get; set; } = new();
}

public sealed class TodoItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsComplete { get; set; }
    public Guid ProjectId { get; set; }
}
```

### DbContext

```csharp
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TodoItem> Todos => Set<TodoItem>();
}
```

### Repository (generic)

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

    public void Add(T entity) => _db.Set<T>().Add(entity);
    public void Update(T entity) => _db.Set<T>().Update(entity);
    public void Remove(T entity) => _db.Set<T>().Remove(entity);
}
```

### Unit of Work

```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<Project> Projects { get; }
    IRepository<TodoItem> Todos { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private IDbContextTransaction? _tx;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Projects = new Repository<Project>(db);
        Todos = new Repository<TodoItem>(db);
    }

    public IRepository<Project> Projects { get; }
    public IRepository<TodoItem> Todos { get; }

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

### ViewModel

```csharp
public partial class ProjectViewModel : ObservableObject
{
    private readonly IUnitOfWork _uow;

    [ObservableProperty]
    private IReadOnlyList<Project> _projects = Array.Empty<Project>();

    public ProjectViewModel(IUnitOfWork uow) => _uow = uow;

    [RelayCommand]
    private async Task CreateProjectWithDefaultsAsync(string name)
    {
        await _uow.BeginTransactionAsync();
        try
        {
            var project = new Project { Id = Guid.NewGuid(), Name = name };
            _uow.Projects.Add(project);

            var defaultTodo = new TodoItem
            {
                Id = Guid.NewGuid(),
                Title = "Initial task",
                ProjectId = project.Id
            };
            _uow.Todos.Add(defaultTodo);

            await _uow.SaveChangesAsync();
            await _uow.CommitAsync();
        }
        catch
        {
            await _uow.RollbackAsync();
            throw;
        }
    }

    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        var active = await _uow.Projects.FindAsync(p => p.IsActive);
        Projects = active;
    }
}
```

### Registration

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=projects.db"));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

### Key points

- `CreateProjectWithDefaultsAsync` atomically inserts a Project and a TodoItem
- On failure, `RollbackAsync` undoes both inserts
- The ViewModel never touches `AppDbContext` directly — only through `IRepository<T>` and `IUnitOfWork`

---

## Example 2: Desktop App with IDbContextFactory and Transient UoW

A long-lived dashboard ViewModel that periodically refreshes data. Using `IDbContextFactory` prevents stale tracking.

### Registration

```csharp
var connectionString = "Data Source=dashboard.db";

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<DashboardViewModel>();
```

### UnitOfWork with factory

```csharp
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private IDbContextTransaction? _tx;

    public UnitOfWork(IDbContextFactory<AppDbContext> factory)
    {
        _db = factory.CreateDbContext();
        Projects = new Repository<Project>(_db);
        Todos = new Repository<TodoItem>(_db);
    }

    public IRepository<Project> Projects { get; }
    public IRepository<TodoItem> Todos { get; }

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

    public void Dispose()
    {
        _tx?.Dispose();
        _db.Dispose();
    }
}
```

### ViewModel using DI factory

```csharp
public partial class DashboardViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<DashboardViewModel> _logger;

    [ObservableProperty]
    private IReadOnlyList<TodoItem> _recentItems = Array.Empty<TodoItem>();

    [ObservableProperty]
    private int _totalProjects;

    public DashboardViewModel(
        IDbContextFactory<AppDbContext> factory,
        ILogger<DashboardViewModel> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await using var uow = new UnitOfWork(_factory);
        try
        {
            var items = await uow.Todos.FindAsync(t => !t.IsComplete);
            var projects = await uow.Projects.GetAllAsync();
            RecentItems = items;
            TotalProjects = projects.Count;
            _logger.LogInformation("Dashboard refreshed: {ItemCount} items, {ProjectCount} projects",
                items.Count, projects.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard refresh failed");
        }
    }
}
```

### Key points

- Each `RefreshAsync` call creates a short-lived `DbContext`, avoiding stale tracked entities
- The `UnitOfWork` is created manually (not via DI) because it is scoped to a single operation
- `await using` ensures `Dispose` runs even on exception (calls `_db.Dispose()`)
- ViewModel is a singleton but data access contexts are transient

---

## Example 3: Switching from EF Core to LiteDB

The repository abstraction makes it possible to swap data sources without changing ViewModel code.

### LiteDB service

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
}
```

### LiteDB repository

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

### Registration (swap line)

```csharp
// EF Core version
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// LiteDB version — no UnitOfWork needed for single-collection ops
builder.Services.AddSingleton<LiteDbService>();
builder.Services.AddSingleton(typeof(IRepository<>), typeof(LiteRepository<>));
```

### ViewModel unchanged

```csharp
// No changes needed — the ViewModel still injects IRepository<TodoItem>
public partial class TodoViewModel : ObservableObject
{
    private readonly IRepository<TodoItem> _repo;

    public TodoViewModel(IRepository<TodoItem> repo) => _repo = repo;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var items = await _repo.GetAllAsync();
        // Bind to UI ...
    }
}
```

### Key points

- The ViewModel never imports `LiteDbService`, `AppDbContext`, or any data-source-specific type
- Swapping from EF Core to LiteDB is a single `Program.cs` change
- LiteDB's single-writer model makes `IUnitOfWork` unnecessary for most operations
- Complex LiteDB operations can use `LiteDatabase.BeginTrans()` directly within the service

---

## Example 4: Testing a ViewModel with In-Memory Repository

### Test repository

```csharp
public sealed class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly Dictionary<Guid, T> _items = new();
    private readonly Func<T, Guid> _getId;

    public InMemoryRepository(Func<T, Guid> getId) => _getId = getId;

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

    public void Add(T entity) => _items[_getId(entity)] = entity;
    public void Update(T entity) => _items[_getId(entity)] = entity;
    public void Remove(T entity) => _items.Remove(_getId(entity));
}
```

### xUnit test

```csharp
public class TodoViewModelTests
{
    [Fact]
    public async Task LoadAsync_populates_items()
    {
        var item = new TodoItem(Guid.NewGuid(), "Test", false, DateTime.UtcNow);
        var repo = new InMemoryRepository<TodoItem>(i => i.Id);
        repo.Add(item);

        var vm = new TodoViewModel(repo);
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Single(vm.Items);
        Assert.Equal("Test", vm.Items[0].Title);
    }

    [Fact]
    public async Task FindAsync_filters_by_predicate()
    {
        var repo = new InMemoryRepository<TodoItem>(i => i.Id);
        repo.Add(new TodoItem(Guid.NewGuid(), "A", false, DateTime.UtcNow));
        repo.Add(new TodoItem(Guid.NewGuid(), "B", true, DateTime.UtcNow));

        var result = await repo.FindAsync(i => i.IsComplete);
        Assert.Single(result);
        Assert.Equal("B", result[0].Title);
    }
}
```

### Key points

- No database, no mocking framework — just a `Dictionary<Guid, T>` in memory
- Tests run in milliseconds with no infrastructure setup
- The same `IRepository<T>` contract is used by production (EF Core, LiteDB) and test (in-memory) code
- `Expression<Func<T, bool>>` predicates are compiled with `Compile()` for in-memory evaluation
