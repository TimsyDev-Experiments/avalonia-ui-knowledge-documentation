---
tier: intermediate
topic: data access
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 048-sqlite-local-database.md
---

# 048X — SQLite / Local Database: Real-World Examples

**What you'll build:** An offline-first notes app backed by LiteDB with full-text search, and a customer management screen using EF Core + SQLite with relational data and migrations.

**Prerequisites:** [048 — SQLite / Local Database](048-sqlite-local-database.md). The [verbose companion](048-sqlite-local-database-verbose.md) covers LiteDB concurrency, EF Core lifetime pitfalls, and cross-platform path resolution in depth.

---

## Example 1: Offline-First Notes App with LiteDB

**Goal:** A note-taking app that stores notes locally in LiteDB, supports create/read/update/delete, searches by title, and wraps synchronous LiteDB calls in async for UI responsiveness.

### Data Model

```csharp
// Models/Note.cs
using LiteDB;

namespace MyApp.Models;

public class Note
{
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }
}
```

### Database Service

```csharp
// Services/NoteService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using MyApp.Models;

namespace MyApp.Services;

public class NoteService
{
    private readonly string _connectionString;

    public NoteService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NotesApp");
        Directory.CreateDirectory(dir);
        _connectionString = Path.Combine(dir, "notes.db");
    }

    public Task<List<Note>> GetAllAsync()
    {
        return Task.Run(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            return db.GetCollection<Note>("notes")
                     .FindAll()
                     .OrderByDescending(n => n.ModifiedAt)
                     .ToList();
        });
    }

    public Task<List<Note>> SearchAsync(string query)
    {
        return Task.Run(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            return db.GetCollection<Note>("notes")
                     .Find(n =>
                         n.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         n.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(n => n.ModifiedAt)
                     .ToList();
        });
    }

    public Task SaveAsync(Note note)
    {
        return Task.Run(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<Note>("notes");
            note.ModifiedAt = DateTime.UtcNow;

            if (col.FindById(note.Id) is not null)
                col.Update(note);
            else
                col.Insert(note);
        });
    }

    public Task DeleteAsync(ObjectId id)
    {
        return Task.Run(() =>
        {
            using var db = new LiteDatabase(_connectionString);
            db.GetCollection<Note>("notes").Delete(id);
        });
    }
}
```

### ViewModel

```csharp
// ViewModels/NotesViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.ViewModels;

public partial class NotesViewModel : ObservableObject
{
    private readonly NoteService _noteService;

    [ObservableProperty]
    private ObservableCollection<Note> _notes = new();

    [ObservableProperty]
    private Note? _selectedNote;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _editorTitle = "";

    [ObservableProperty]
    private string _editorContent = "";

    [ObservableProperty]
    private bool _isLoading;

    public NotesViewModel(NoteService noteService)
    {
        _noteService = noteService;
    }

    [RelayCommand]
    private async Task LoadNotesAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _noteService.GetAllAsync();
            Notes = new ObservableCollection<Note>(items);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = SearchAsync(value);
    }

    private async Task SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            await LoadNotesAsync();
            return;
        }

        var results = await _noteService.SearchAsync(query);
        Notes = new ObservableCollection<Note>(results);
    }

    [RelayCommand]
    private void SelectNote(Note note)
    {
        SelectedNote = note;
        EditorTitle = note.Title;
        EditorContent = note.Content;
    }

    [RelayCommand]
    private async Task SaveNoteAsync()
    {
        if (SelectedNote is null) return;

        SelectedNote.Title = EditorTitle;
        SelectedNote.Content = EditorContent;
        await _noteService.SaveAsync(SelectedNote);
        await LoadNotesAsync();
    }

    [RelayCommand]
    private async Task CreateNoteAsync()
    {
        var note = new Note
        {
            Title = "New Note",
            Content = "",
        };
        await _noteService.SaveAsync(note);
        await LoadNotesAsync();
    }

    [RelayCommand]
    private async Task DeleteNoteAsync()
    {
        if (SelectedNote is null) return;

        await _noteService.DeleteAsync(SelectedNote.Id);
        SelectedNote = null;
        EditorTitle = "";
        EditorContent = "";
        await LoadNotesAsync();
    }
}
```

### View

```xml
<!-- File: Views/NotesView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:models="using:MyApp.Models"
             x:Class="MyApp.Views.NotesView"
             x:DataType="vm:NotesViewModel">

  <DockPanel Margin="8">
    <StackPanel DockPanel.Dock="Top" Spacing="6" Margin="0,0,0,8">
      <TextBox Text="{Binding SearchText}"
               Watermark="Search notes..." />
      <StackPanel Orientation="Horizontal" Spacing="6">
        <Button Content="New Note" Command="{Binding CreateNoteCommand}" />
        <Button Content="Delete" Command="{Binding DeleteNoteCommand}" />
        <Button Content="Save" Command="{Binding SaveNoteCommand}" />
      </StackPanel>
    </StackPanel>

    <Grid ColumnDefinitions="250,4,*">
      <!-- Note list -->
      <ListBox ItemsSource="{Binding Notes}"
               SelectedItem="{Binding SelectedNote}">
        <ListBox.ItemTemplate>
          <DataTemplate x:DataType="models:Note">
            <StackPanel>
              <TextBlock Text="{Binding Title}" FontWeight="Bold" />
              <TextBlock Text="{Binding ModifiedAt, StringFormat='{0:g}'}"
                         FontSize="11" Foreground="Gray" />
            </StackPanel>
          </DataTemplate>
        </ListBox.ItemTemplate>
      </ListBox>

      <GridSplitter Grid.Column="1" Width="4" />

      <!-- Editor -->
      <StackPanel Grid.Column="2" Spacing="6" Margin="8,0,0,0">
        <TextBox Text="{Binding EditorTitle}"
                 Watermark="Title" FontSize="18" FontWeight="Bold" />
        <TextBox Text="{Binding EditorContent}"
                 Watermark="Start writing..."
                 AcceptsReturn="True"
                 Height="300" />
      </StackPanel>
    </Grid>
  </DockPanel>
</UserControl>
```

### How It Works

1. `NoteService` wraps all LiteDB operations in `Task.Run`. LiteDB's API is synchronous file I/O — `Task.Run` moves it off the UI thread, keeping the interface responsive.
2. `SaveAsync` uses an upsert pattern: if the note exists (`FindById` returns non-null), it calls `Update`; otherwise `Insert`. This avoids a separate `Exists` call.
3. `SearchAsync` performs a client-side `string.Contains` filter. LiteDB does not have native full-text search, so the search loads matching documents into memory. For a notes app with hundreds of notes, this is fast enough. For millions, you would add an external index (e.g., Lucene.NET).
4. The ViewModel calls `LoadNotesAsync` after every mutation (save, delete, create) to refresh the list. This is simple and correct for single-user offline data; for larger datasets you would mutate the `ObservableCollection` in-place instead.
5. `OnSearchTextChanged` debounces implicitly — each keystroke triggers a new search. The previous search does not cancel, so rapid typing could produce stale results. A production version would add cancellation via `CancellationTokenSource`.

### Key Points

- `Task.Run` wrapping synchronous LiteDB calls keeps the UI thread free. The overhead of thread-pool dispatch is negligible compared to the file I/O.
- The upsert pattern (`FindById` then `Insert`/`Update`) avoids a code branch for create vs. edit. The cost is one extra query per save.
- `ObjectId` is generated client-side — no round-trip needed for ID assignment. This makes `CreateNoteAsync` instant.
- Edge case: if two instances of the app share the same file (notebook on a network share), LiteDB's single-writer lock prevents corruption but `SaveAsync` may throw if the file is locked. Wrap in try/catch for a "save failed" message.

---

## Example 2: Customer Management with EF Core + SQLite

**Goal:** A customer management screen with related orders, using EF Core with SQLite, `IDbContextFactory`, and runtime migrations.

### Data Model

```csharp
// Models/Customer.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Models;

public class Customer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
}
```

### DbContext

```csharp
// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using MyApp.Models;

namespace MyApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(e => e.CustomerId);
        });
    }
}
```

### Registration with Migrations

```csharp
// Program.cs
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "CrmApp", "crm.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Build app, then apply migrations
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.Migrate();
}
```

### ViewModel

```csharp
// ViewModels/CustomerListViewModel.cs
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;

namespace MyApp.ViewModels;

public partial class CustomerListViewModel : ObservableObject
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private string _newCustomerName = "";

    [ObservableProperty]
    private string _newCustomerEmail = "";

    public CustomerListViewModel(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var items = await db.Customers
            .Include(c => c.Orders)
            .OrderBy(c => c.Name)
            .ToListAsync();
        Customers = new ObservableCollection<Customer>(items);
    }

    [RelayCommand]
    private async Task AddCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCustomerName)) return;

        await using var db = await _contextFactory.CreateDbContextAsync();
        var customer = new Customer
        {
            Name = NewCustomerName,
            Email = NewCustomerEmail,
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        NewCustomerName = "";
        NewCustomerEmail = "";
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer is null) return;

        await using var db = await _contextFactory.CreateDbContextAsync();
        var customer = await db.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == SelectedCustomer.Id);

        if (customer is not null)
        {
            db.Customers.Remove(customer);
            await db.SaveChangesAsync();
        }

        await LoadCustomersAsync();
    }
}
```

### View

```xml
<!-- File: Views/CustomerListView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:models="using:MyApp.Models"
             x:Class="MyApp.Views.CustomerListView"
             x:DataType="vm:CustomerListViewModel">

  <DockPanel Margin="12">
    <StackPanel DockPanel.Dock="Top" Spacing="8" Margin="0,0,0,12">
      <StackPanel Orientation="Horizontal" Spacing="6">
        <TextBox Text="{Binding NewCustomerName}"
                 Watermark="Customer name" Width="200" />
        <TextBox Text="{Binding NewCustomerEmail}"
                 Watermark="Email" Width="200" />
        <Button Content="Add Customer"
                Command="{Binding AddCustomerCommand}" />
      </StackPanel>
      <Button Content="Delete Selected"
              Command="{Binding DeleteCustomerCommand}" />
    </StackPanel>

    <DataGrid ItemsSource="{Binding Customers}"
              AutoGenerateColumns="False"
              IsReadOnly="True"
              SelectedItem="{Binding SelectedCustomer}">
      <DataGrid.Columns>
        <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*" />
        <DataGridTextColumn Header="Email" Binding="{Binding Email}" Width="2*" />
        <DataGridTextColumn Header="Orders"
                            Binding="{Binding Orders.Count}" Width="80" />
        <DataGridTextColumn Header="Created"
                            Binding="{Binding CreatedAt, StringFormat='{0:g}'}"
                            Width="120" />
      </DataGrid.Columns>
    </DataGrid>
  </DockPanel>
</UserControl>
```

### How It Works

1. `IDbContextFactory<AppDbContext>` is registered in DI. Every command creates a fresh DbContext via `CreateDbContextAsync()` — no stale tracking, no memory bloat from accumulated tracked entities.
2. The `Database.Migrate()` call in `Program.cs` runs on startup. It checks `__EFMigrationsHistory` and applies any pending migrations. This ensures the database schema matches the code even if the user skipped an update.
3. `LoadCustomersAsync` eagerly loads orders via `.Include(c => c.Orders)` so the `Orders.Count` binding works without lazy loading (which SQLite does not support efficiently).
4. `AddCustomerAsync` creates a new `Customer`, saves it, and reloads the full list. The list reload ensures the `ObservableCollection` matches the database state, catching any side effects (e.g., triggers, default values).
5. `DeleteCustomerAsync` loads the customer with orders before deleting. The `Include` ensures related orders are loaded and can be cascade-deleted (configured in the DbContext or database).
6. The `DataGrid` columns bind directly to `Customer` properties. `Orders.Count` displays the count of related orders without a separate query — it reads from the in-memory collection loaded by `Include`.

### Key Points

- `IDbContextFactory` is non-negotiable for desktop EF Core apps. Without it, the single DbContext instance accumulates tracked entities for the entire app lifetime, degrading performance and causing stale data reads.
- `Database.Migrate()` on startup handles the "user skips versions" scenario. The migration is idempotent — it only applies pending migrations.
- `.Include(c => c.Orders)` is required for the `Orders.Count` binding. Without it, EF Core would not load the navigation property, and the count would always be 0.
- Edge case: if the database file is deleted manually, `Database.Migrate()` re-creates it from scratch. All data is lost — consider a backup strategy for production apps.
- Edge case: `AddCustomerCommand` does not validate email format. A production app would add validation before `SaveChangesAsync`.

---

## What These Examples Demonstrate

| Scenario | Database | Key technique |
|---|---|---|
| Notes app | LiteDB (NoSQL) | `Task.Run` for async, upsert pattern, client-side search |
| Customer management | EF Core + SQLite (relational) | `IDbContextFactory`, `Include` for navigation, runtime migrations |

The LiteDB example shows a document-oriented approach where schema changes are invisible — just add a property to the model and it serializes. The EF Core example shows relational modeling with foreign keys, unique indexes, and migration-managed schema evolution. Choose LiteDB when data is simple and you want zero config; choose EF Core when you need relational integrity and complex queries.

## See Also

- [048 — SQLite / Local Database](048-sqlite-local-database.md)
- [048V — Verbose Companion](048-sqlite-local-database-verbose.md)
- [032 — Dependency Injection for MVVM](../advanced/032-mvvm-di-wiring.md)
- [015 — Item Lists](015-item-lists.md)
- [LiteDB docs](https://litedb.org/)
- [EF Core docs](https://learn.microsoft.com/ef/core/)
