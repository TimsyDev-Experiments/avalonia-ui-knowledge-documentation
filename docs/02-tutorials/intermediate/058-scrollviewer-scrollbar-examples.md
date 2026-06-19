---
tier: intermediate
topic: layout
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 058-scrollviewer-scrollbar.md
---

# 058E — ScrollViewer & ScrollBar: Real-World Examples

**What this is:** Two worked examples showing `ScrollViewer` and custom scrollbar patterns. Read [058 — ScrollViewer & ScrollBar](058-scrollviewer-scrollbar.md) and [058V — Verbose Companion](058-scrollviewer-scrollbar-verbose.md) first.

---

## Example 1: Chat Window with Infinite Scroll + Scroll Lock

### Goal

Build a chat window with:
- Messages loading from history (scroll up to load more)
- Auto-scroll to bottom on new messages
- Scroll lock: if the user has scrolled up, do NOT auto-scroll on new messages
- Scroll anchoring when older messages are prepended

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ChatApp.ViewModels"
        x:Class="ChatApp.Views.MainWindow"
        x:DataType="vm:ChatViewModel"
        Title="Chat" Width="400" Height="600">

  <Grid RowDefinitions="*,Auto">
    <!-- Message list -->
    <ScrollViewer Name="MessageScrollViewer"
                  Grid.Row="0"
                  ScrollChanged="OnScrollChanged">
      <ItemsControl ItemsSource="{Binding Messages}">
        <ItemsControl.ItemTemplate>
          <DataTemplate>
            <Border Margin="4" Padding="8" CornerRadius="6"
                    Background="{StaticResource SurfaceBrush}">
              <StackPanel>
                <TextBlock Text="{Binding Author}"
                           FontWeight="Bold" FontSize="12" />
                <TextBlock Text="{Binding Text}"
                           TextWrapping="Wrap" />
              </StackPanel>
            </Border>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </ScrollViewer>

    <!-- Input area -->
    <Border Grid.Row="1" Padding="8"
            Background="{StaticResource SurfaceBrush}">
      <Grid ColumnDefinitions="*,Auto" Spacing="8">
        <TextBox Name="MessageInput"
                 Watermark="Type a message..."
                 KeyDown="OnInputKeyDown" />
        <Button Content="Send" Command="{Binding SendCommand}"
                CommandParameter="{Binding #MessageInput.Text}" />
      </Grid>
    </Border>
  </Grid>
</Window>
```

### Code-behind

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using ChatApp.ViewModels;
using System;

namespace ChatApp.Views;

public partial class MainWindow : Window
{
    private bool _isUserAtBottom = true;
    private double _lastExtentHeight;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var sv = MessageScrollViewer;

        // Are we at the bottom (within 5px tolerance)?
        double distanceFromBottom =
            sv.Extent.Height - sv.Viewport.Height - sv.Offset.Y;
        _isUserAtBottom = distanceFromBottom < 5;

        // Detect "scroll up to load history"
        // When extent grows upward (content prepended),
        // anchor the view so user doesn't jump
        if (e.ExtentDelta.Y > 0 && !_isUserAtBottom)
        {
            // Content was added above; keep view stable
            sv.Offset = new Vector(
                sv.Offset.X,
                sv.Offset.Y + e.ExtentDelta.Y);
        }

        _lastExtentHeight = sv.Extent.Height;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is ChatViewModel vm && vm.SendCommand.CanExecute(null))
            {
                vm.SendCommand.Execute(MessageInput.Text);
                MessageInput.Text = string.Empty;

                // Auto-scroll after sending
                ScrollToBottom();
            }
            e.Handled = true;
        }
    }

    public void ScrollToBottom()
    {
        var sv = MessageScrollViewer;
        sv.Offset = new Vector(
            sv.Offset.X,
            sv.Extent.Height - sv.Viewport.Height);
    }

    public void OnNewMessageReceived()
    {
        // Only auto-scroll if user is at bottom
        if (_isUserAtBottom)
            ScrollToBottom();
    }
}
```

### ViewModel (excerpt)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ChatApp.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    [RelayCommand]
    private void Send(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Messages.Add(new ChatMessage("You", text));
        // Signal the view to scroll to bottom
    }
}

public record ChatMessage(string Author, string Text);
```

### Key points

- `_isUserAtBottom` tracking with tolerance — auto-scroll only when user is at bottom
- Extent delta compensation — when history loads above current view, offset adjusts to keep content stable
- Enter key sends and scrolls to bottom
- `ScrollChanged` event drives both history loading and scroll lock

---

## Example 2: Custom Scrollable Panel with Standalone ScrollBar

### Goal

Build a custom image strip with a standalone `ScrollBar` for scroll control, bypassing `ScrollViewer`.

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="ImageStrip.Views.MainWindow"
        Title="Image Strip" Width="700" Height="250">

  <Grid ColumnDefinitions="*,Auto" RowDefinitions="*">
    <!-- Scrollable content area (no built-in scrollbar) -->
    <Border Name="ContentArea" Grid.Column="0" ClipToBounds="True"
            Background="Black">
      <StackPanel Name="ImagePanel" Orientation="Horizontal"
                  VerticalAlignment="Center">
        <Border Width="200" Height="150" Margin="4"
                Background="CornflowerBlue" />
        <Border Width="200" Height="150" Margin="4"
                Background="Coral" />
        <Border Width="200" Height="150" Margin="4"
                Background="MediumSeaGreen" />
        <Border Width="200" Height="150" Margin="4"
                Background="Gold" />
        <Border Width="200" Height="150" Margin="4"
                Background="DarkOrchid" />
        <Border Width="200" Height="150" Margin="4"
                Background="Tomato" />
      </StackPanel>
    </Border>

    <!-- Standalone vertical scrollbar -->
    <!-- (We actually want horizontal here, using ScrollBar as a slider) -->
  </Grid>
</Window>
```

### Code-behind with custom ScrollBar

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using System;

namespace ImageStrip.Views;

public partial class MainWindow : Window
{
    private ScrollBar? _scrollBar;
    private double _contentWidth;
    private double _viewportWidth;

    public MainWindow()
    {
        InitializeComponent();
        CreateCustomScrollBar();
    }

    private void CreateCustomScrollBar()
    {
        _scrollBar = new ScrollBar
        {
            Orientation = Orientation.Horizontal,
            Minimum = 0,
            Maximum = 100, // Will be recalculated
            SmallChange = 20,
            LargeChange = 100,
            ViewportSize = 50,
            Height = 20,
        };

        _scrollBar.Scroll += OnScrollBarScroll;

        // Place below the content area
        Grid.SetRow(_scrollBar, 1);
        Grid.SetColumnSpan(_scrollBar, 2);
        ((Grid)Content!).RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        ((Grid)Content!).Children.Add(_scrollBar);

        // Recalculate when layout is ready
        ContentArea.LayoutUpdated += OnContentLayoutUpdated;
    }

    private void OnContentLayoutUpdated(object? sender, EventArgs e)
    {
        _contentWidth = ImagePanel.Bounds.Width;
        _viewportWidth = ContentArea.Bounds.Width;

        if (_contentWidth <= _viewportWidth)
        {
            _scrollBar!.IsVisible = false;
            return;
        }

        _scrollBar!.IsVisible = true;
        _scrollBar.Maximum = _contentWidth - _viewportWidth;
        _scrollBar.ViewportSize = _viewportWidth;
        _scrollBar.LargeChange = _viewportWidth * 0.8;
    }

    private void OnScrollBarScroll(object? sender, ScrollEventArgs e)
    {
        // Scroll the content by applying negative offset
        double offset = -e.NewValue;
        ImagePanel.Margin = new(offset, 0, 0, 0);
    }
}
```

### Key points

- Standalone `ScrollBar` manually controlling content position
- `ScrollBar.Orientation = Horizontal` for horizontal scrolling
- `ScrollBar.Maximum` set to `contentWidth - viewportWidth`
- `ScrollBar.ViewportSize` determines thumb proportion
- `Scroll` event drives content offset via negative margin
- Useful when `ScrollViewer` doesn't provide enough control

---

## See Also

- [058 — ScrollViewer & ScrollBar (core tutorial)](058-scrollviewer-scrollbar.md)
- [058V — ScrollViewer & ScrollBar (verbose companion)](058-scrollviewer-scrollbar-verbose.md)
- [058Q — ScrollViewer & ScrollBar (quiz)](058-scrollviewer-scrollbar-quiz.md)
