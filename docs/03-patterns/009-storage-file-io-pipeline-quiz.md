---
title: Quiz
topic: 009-storage-file-io-pipeline
type: quiz
---

# Quiz: Storage & File I/O Pipeline

```quiz
Q: How does the FileOperationService obtain the platform-specific IStorageProvider?
A. It creates a new instance of IStorageProvider directly
B. It receives a TopLevel instance in its constructor and accesses TopLevel.StorageProvider (correct) || FileOperationService takes a TopLevel in its constructor. IStorageProvider is available via the StorageProvider property on any TopLevel (Window, Popup, etc.).
C. It uses a static method IStorageProvider.GetDefault()
D. It reads the storage provider from a configuration file

Explanation: IStorageProvider is a property on TopLevel (the base class for Window, Popup, etc.). The FileOperationService receives a TopLevel and uses it to access the platform-specific storage implementation for file pickers and folder pickers.
```

```quiz
Q: What is the purpose of storage bookmarks in the file I/O pipeline?
A. To mark files as read-only so they cannot be modified
B. To persist access to a folder across application restarts, so the user doesn't have to navigate to it again (correct) || Bookmarks save a platform-specific token that allows the application to re-access a previously opened folder without forcing the user to navigate to it manually.
C. To bookmark a specific line within a text file
D. To create a shortcut to the file on the desktop

Explanation: Storage bookmarks persist folder access across sessions. The application saves a bookmark ID (token) and later uses it to restore access to the same folder, which is critical for project management and recent-folder workflows.
```

```quiz
Q: What are the three DI registration strategies for FileOperationService described in the verbose companion?
A. Singleton from MainWindow, Scoped per Window, Singleton with fallback TopLevel (correct) || Strategy 1 resolves TopLevel from MainWindow at registration time. Strategy 2 scopes the service per window. Strategy 3 uses a fallback that searches active windows.
B. Transient, Singleton, and Scoped from the same TopLevel
C. Manual instantiation, factory pattern, and service locator
D. Static singleton, thread-local singleton, and pooled singleton

Explanation: The three strategies are: (1) singleton resolved from MainWindow, (2) scoped per window (each window gets its own service), and (3) singleton with fallback that checks all open windows for an active TopLevel.
```

```quiz
Q: In the progress reporting example, how does the ViewModel receive and display copy progress?
A. The CopyFileWithProgressAsync method updates a global static variable that the ViewModel polls
B. The ViewModel passes an IProgress<FileCopyProgress> to the copy method, which reports bytes copied, percentage, speed, and ETA (correct) || The ViewModel creates a Progress<FileCopyProgress> that updates observable properties (ProgressPercentage, TransferSpeed, EstimatedTime, StatusText) on each progress notification.
C. The copy method writes progress to a text file that the UI reads periodically
D. The progress is reported via a message box after the copy completes

Explanation: The ViewModel creates a Progress<FileCopyProgress> instance and passes it to the copy service. The service reports progress via IProgress<T>, and the Progress<T> wrapper invokes the callback on the UI thread, updating bindable properties.
```

```quiz
Q: What happens when you call IStorageProvider.OpenFilePickerAsync with AllowMultiple = false?
A. The user can select multiple files, but only the first one is returned
B. The picker only allows selecting a single file, and the result contains zero or one file (correct) || With AllowMultiple = false, the platform file picker restricts the user to selecting a single file. The result is an IReadOnlyList<IStorageFile> with at most one element.
C. An exception is thrown if the user selects more than one file
D. The picker opens in save mode instead of open mode

Explanation: FilePickerOpenOptions.AllowMultiple controls multi-selection. When false, the native picker only allows one file to be selected. The method returns an IReadOnlyList that will contain either zero files (user cancelled) or one file.
```

```quiz
Q: Why should FileTypeFilters include an "All Files" fallback in addition to specific patterns?
A. "All Files" is required by the Avalonia API and throws without it
B. It ensures the user can still select files even if their file doesn't match the specific filter patterns (correct) || Including "All Files" (*.*) as a fallback gives users the option to select any file, which is important when the file they need doesn't match the primary filters.
C. "All Files" improves the performance of the file picker dialog
D. "All Files" is automatically added by IStorageProvider

Explanation: Best practice is to include a "All Files" (*.*) filter as the last option in the filter list. This ensures users can always select any file, even if their file doesn't match the application's expected patterns. The FileOperationService.SaveFileAsync method adds this fallback automatically if no filters are provided.
```
