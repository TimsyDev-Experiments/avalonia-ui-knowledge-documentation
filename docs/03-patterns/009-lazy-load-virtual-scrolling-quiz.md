---
title: Quiz
topic: 009-lazy-load-virtual-scrolling
type: quiz
---

# Quiz: Lazy Load / Virtual Scrolling

```quiz
Q: What three problems does lazy loading solve when displaying large datasets?
A. Memory usage, initial load time, and UI responsiveness (correct) || Lazy loading avoids loading 100,000+ items into memory at once, reduces the initial load time by loading only the first page, and keeps the UI responsive by virtualizing containers.
B. Data validation, error handling, and network latency
C. Search performance, sorting accuracy, and filtering speed
D. Database connection pooling, query caching, and index optimization

Explanation: The three problems are: (1) memory — each item + UI container costs memory, (2) initial load time — loading everything upfront blocks the UI, and (3) UI responsiveness — rendering thousands of containers simultaneously freezes the application.
```

```quiz
Q: In AsyncLazyDataSource<T>, what is the purpose of the prefetchPages parameter?
A. It limits the total number of pages that can be loaded
B. It loads additional pages ahead of the current view to provide smoother scrolling (correct) || Prefetching loads the next 1-2 pages in advance so that when the user scrolls, the data is already available, eliminating loading delays.
C. It skips the first N pages and starts loading from page N+1
D. It caches previously loaded pages to avoid re-fetching

Explanation: The prefetchPages parameter tells the data source to proactively load additional pages beyond the current request. This ensures data is ready before the user scrolls to it, making the experience feel instantaneous.
```

```quiz
Q: What is the difference between VirtualizationMode="Simple" and VirtualizationMode="None" in a ListBox?
A. Simple virtualizes containers (only visible items have UI), None realizes all items immediately (correct) || Simple recycles item containers for off-screen items, keeping memory low. None creates containers for every item, which is appropriate for small lists or animated items.
B. Simple is faster but uses more memory; None is slower but uses less memory
C. Simple only works with ObservableCollection; None works with any collection
D. There is no difference — Simple and None are aliases

Explanation: VirtualizationMode.Simple creates UI containers only for visible items and recycles them as the user scrolls. VirtualizationMode.None creates containers for every item in the list, which is necessary when items have animations or non-uniform sizes.
```

```quiz
Q: In the infinite scroll pattern, why is a debounce mechanism needed in the ScrollChanged handler?
A. To batch multiple rapid scroll events into a single load request (correct) || When the user scrolls rapidly, ScrollChanged fires many times per second. Debouncing (e.g., 100ms delay) prevents firing multiple concurrent load requests for the same scroll position.
B. To ensure the progress bar animation completes before loading more data
C. To prevent the scroll position from resetting when new items are added
D. To synchronize the scroll position with the database query

Explanation: Without debouncing, each ScrollChanged event could trigger a page load. A debounce (via CancellationToken + Task.Delay) ensures that only the final scroll position triggers the load, preventing duplicate or concurrent requests.
```

```quiz
Q: When should you avoid virtualization (VirtualizationMode="None")?
A. When the list has animated item transitions or non-uniform item heights (correct) || VirtualizationMode.None is required when items have animations (because recycled containers break animations) or when items have variable heights (which cause layout jumps with virtualization).
B. When the list has more than 10,000 items
C. When the list uses data binding
D. When the list is inside a ScrollViewer

Explanation: Virtualization recycles containers, which breaks per-item animations and assumes uniform item sizes for stable layout. Non-uniform heights or animations require VirtualizationMode.None to keep all containers alive.
```

```quiz
Q: How does the SQLite-backed data source example implement pagination in the query?
A. It loads all rows into memory and skips the first N results
B. It uses the LIMIT and OFFSET SQL clauses to fetch only the requested page (correct) || The query uses "SELECT ... LIMIT @size OFFSET @skip" to retrieve only the rows for the requested page, minimizing database load and memory usage.
C. It queries one row at a time in a loop
D. It uses a stored procedure that returns all rows incrementally

Explanation: SQL pagination is done with LIMIT @size (max rows to return) and OFFSET @skip (rows to skip), where @skip = page * size. This is the standard and most efficient way to paginate SQL queries.
```
