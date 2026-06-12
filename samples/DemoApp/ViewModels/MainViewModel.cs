using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemoApp.Models;

namespace DemoApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<TodoItem> Todos { get; } = new()
    {
        new() { Title = "Learn Avalonia", IsDone = true, DueDate = DateTime.Now.AddDays(-1) },
        new() { Title = "Write tutorials", IsDone = false, DueDate = DateTime.Now.AddDays(3) },
        new() { Title = "Build sample app", IsDone = false, DueDate = DateTime.Now.AddDays(7) },
        new() { Title = "Publish docs", IsDone = false, DueDate = DateTime.Now.AddDays(14) },
    };

    [ObservableProperty]
    private string _newItemTitle = string.Empty;

    [ObservableProperty]
    private TodoItem? _selectedTodo;

    [ObservableProperty]
    private int _counter;

    [ObservableProperty]
    private bool _isDarkMode;

    [RelayCommand]
    private void AddTodo()
    {
        if (string.IsNullOrWhiteSpace(NewItemTitle)) return;

        Todos.Add(new TodoItem
        {
            Title = NewItemTitle.Trim(),
            IsDone = false,
            DueDate = DateTime.Now.AddDays(7)
        });

        NewItemTitle = string.Empty;
    }

    [RelayCommand]
    private void RemoveSelectedTodo()
    {
        if (SelectedTodo is not null)
        {
            Todos.Remove(SelectedTodo);
            SelectedTodo = null;
        }
    }

    [RelayCommand]
    private void Increment()
    {
        Counter++;
    }

    [RelayCommand]
    private async Task SimulateWorkAsync()
    {
        Counter = -1;
        await Task.Delay(1500);
        Counter = 0;
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant =
                value ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;
        }
    }
}
