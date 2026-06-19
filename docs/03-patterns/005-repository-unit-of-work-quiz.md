---
title: Quiz
topic: 005-repository-unit-of-work
type: quiz
---

# Quiz: Repository / Unit of Work

```quiz
Q: What is the primary benefit of the IRepository<T> abstraction?
A. It improves query performance by optimizing SQL generation || Performance improvements are incidental; the abstraction does not inherently produce faster SQL
B. It allows switching the data source (EF Core, LiteDB, REST API) without changing ViewModel code (correct) || The repository interface decouples data access logic from ViewModels. When the data source changes (e.g., from EF Core to LiteDB), only the repository implementation changes — ViewModels that inject IRepository<T> remain untouched.
C. It automatically implements caching for all queries || Caching must be implemented explicitly as a decorator or within the repository
D. It eliminates the need for a database entirely || The abstraction abstracts the data source but does not remove the need for one

Explanation: IRepository<T> provides a stable boundary between business logic and data access. ViewModel code depends on the interface, not on EF Core's DbContext or LiteDB's API. Swapping implementations is a single registration change in Program.cs, with no ViewModel modifications.
```

```quiz
Q: For a desktop app with long-lived ViewModels, what is the recommended EF Core registration strategy?
A. AddScoped<IUnitOfWork, UnitOfWork> with a scoped DbContext || Scoped DbContexts accumulate tracked entities over the lifetime of a desktop session
B. AddSingleton<IUnitOfWork, UnitOfWork> with a singleton DbContext || Singleton DbContexts cause massive memory leaks and stale data
C. IDbContextFactory<IAppDbContext> with AddTransient<IUnitOfWork, UnitOfWork> (correct) || IDbContextFactory creates short-lived DbContext instances per operation. Transient UnitOfWork ensures each command gets a fresh context, avoiding stale entity tracking and memory growth.
D. AddDbContext<IAppDbContext> with no UnitOfWork || Without UnitOfWork, multi-repository operations cannot be coordinated atomically

Explanation: Desktop apps have long-lived process lifetimes. A scoped or singleton DbContext tracks every loaded entity, leading to memory bloat and stale data. IDbContextFactory creates a new DbContext on demand, and transient UnitOfWork ensures the context lives only as long as the operation. This is the recommended pattern for desktop Avalonia apps using EF Core.
```

```quiz
Q: Why is IUnitOfWork typically unnecessary when using LiteDB?
A. LiteDB does not support transactions at all || LiteDB does support transactions via BeginTrans/Commit
B. LiteDB uses a single-writer model where the database connection itself manages the transaction scope (correct) || LiteDB's single-writer model means only one write transaction can be active at a time. The LiteDatabase instance itself acts as the unit of work — inserts, updates, and deletes are automatically scoped within its internal transaction. A formal IUnitOfWork wrapper adds little value.
C. LiteDB repositories are always read-only || LiteDB supports full CRUD operations
D. LiteDB internally uses EF Core and inherits its transaction management || LiteDB is a separate embedded database with no dependency on EF Core

Explanation: LiteDB's single-writer design means write operations are serialized through a single connection. The LiteDatabase instance provides built-in transaction semantics: BeginTrans, Commit, and Rollback are available directly. For single-collection operations, the database handles atomicity automatically. A separate IUnitOfWork interface adds abstraction overhead without meaningful benefit.
```

```quiz
Q: Which scenario demands a Unit of Work with BeginTransaction/Commit/Rollback?
A. Reading a single record by ID || Reads do not need transaction coordination
B. Inserting a project and a related todo item in the same operation where both must succeed or neither should (correct) || When multiple repository writes must be atomic (all succeed or all roll back), a Unit of Work provides BeginTransactionAsync, CommitAsync, and RollbackAsync. If the project insert succeeds but the todo insert fails, the transaction rolls back both.
C. Querying data with a complex predicate expression || Predicate-based queries use FindAsync and do not involve transactions
D. Logging an audit entry after every read operation || Audit logging is a concern orthogonal to transactional atomicity

Explanation: Unit of Work coordinates multi-repository writes into a single database transaction. The canonical case is: add entity A, add entity B, SaveChanges, Commit. If either write fails, Rollback undoes the other. This guarantees atomicity, which is impossible when each repository calls SaveChanges independently.
```

```quiz
Q: What is the specification pattern and how does it relate to IRepository<T>?
A. It replaces IRepository<T> entirely with a single ISpecification<T> interface || The specification pattern complements, not replaces, the repository
B. It encapsulates query logic (criteria, ordering, includes) into reusable specification objects that can be passed to the repository (correct) || An ISpecification<T> bundles Criteria (Expression<Func<T, bool>>), OrderBy, and Include logic into a single object. The repository accepts an ISpecification<T> instead of inline Expression parameters, keeping queries reusable, testable, and consistent across ViewModels.
C. It is a LiteDB-specific feature for indexing || The specification pattern is data-source agnostic and works with any repository implementation
D. It generates SQL statements automatically from C# expressions || Expressions are already compiled by EF Core; the specification pattern organizes them, not generates them

Explanation: The specification pattern extracts query details from ViewModels into dedicated objects. For example, an ActiveItemsSpecification encapsulates the "is not complete" criteria plus ordering. The repository's FindAsync(ISpecification<T>) method applies the spec. This keeps ViewModels free of LINQ expressions and makes query logic reusable across the application.
```
