using System;

namespace DemoApp.Models;

public class TodoItem
{
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7);
}
