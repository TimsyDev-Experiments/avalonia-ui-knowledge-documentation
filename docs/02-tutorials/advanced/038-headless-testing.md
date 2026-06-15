---
tier: advanced
topic: testing
estimated: 35 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 038 -- Headless Testing with Avalonia.Headless

**What you'll learn:** How to write unit tests for Avalonia views and behaviors using the headless test platform, without a real window or display.

**Prerequisites:** [001 -- Project Setup](../basics/001-project-setup.md), [007 -- Observable Object and Property](../basics/007-observable-object-property.md)

---

## 1. Install the test packages

```bash
dotnet add package Avalonia.Headless.XUnit
dotnet add package Avalonia.Themes.Fluent
```

For NUnit or MSTest, use `Avalonia.Headless.NUnit` or `Avalonia.Headless.MSTest`.

## 2. Create a test fixture

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.Controls;
using Avalonia.Media;
using DemoApp.ViewModels;
using FluentAssertions; // Optional

namespace DemoApp.Tests;

public class MainViewModelTests
{
    [AvaloniaFact]
    public void UpdateLabel_ChangesText()
    {
        var vm = new MainViewModel();
        var window = new Window
        {
            DataContext = vm,
            Content = new TextBlock().Also(t => t.Bind(
                TextBlock.TextProperty,
                new Binding("Label")))
        };

        vm.UpdateLabelCommand.Execute(null);

        var textBlock = (TextBlock)((ContentControl)window.Content).Content;
        textBlock.Text.Should().Be("Updated");
    }
}
```

## 3. Use AvaloniaFact and AvaloniaTheory

```csharp
[AvaloniaFact]
public void Button_Click_ChangesCounter()
{
    var vm = new CounterViewModel();
    var button = new Button
    {
        Command = vm.IncrementCommand,
        Content = "Increment"
    };

    // Simulate layout and rendering
    button.Measure(new Size(100, 30));
    button.Arrange(new Rect(0, 0, 100, 30));

    button.RaiseEvent(new PointerPressedEventArgs(
        button, new Pointer(), button, Point.Empty, 0, new PointerPointProperties(),
        KeyModifiers.None));

    vm.Count.Should().Be(1);
}

[AvaloniaTheory]
[InlineData(0, false)]
[InlineData(5, true)]
public void CanExecute_ReflectsCount(int start, bool expected)
{
    var vm = new CounterViewModel { Count = start };
    vm.IncrementCommand.CanExecute(null).Should().Be(expected);
}
```

## 4. Render-driven testing with pixel assertions

```csharp
[AvaloniaFact]
public void Toggle_ChangesBackground()
{
    var border = new Border
    {
        Width = 100,
        Height = 100,
        Background = Brushes.Red
    };

    border.Measure(new Size(100, 100));
    border.Arrange(new Rect(0, 0, 100, 100));

    var bitmap = border.CaptureRenderedImage()!;
    var pixel = bitmap.GetPixel(50, 50);

    pixel.ToAvaloniaColor().Should().Be(Colors.Red);
}
```

## 5. Test data templates and view locators

```csharp
[AvaloniaFact]
public void ContentControl_ResolvesView()
{
    var vm = new MainViewModel();
    var contentControl = new ContentControl
    {
        Content = vm
    };

    contentControl.ApplyTemplate();

    // The view locator resolves MainView for MainViewModel
    var child = contentControl.Presenter?.Child;
    child.Should().BeOfType<MainView>();
}
```

## 6. Keyboard and focus testing

```csharp
[AvaloniaFact]
public void TextBox_ReceivesInput()
{
    var textBox = new TextBox();
    var window = new Window { Content = textBox };

    window.Show();
    textBox.Focus();
    textBox.RaiseEvent(new KeyEventArgs
    {
        RoutedEvent = InputElement.KeyDownEvent,
        Key = Key.A,
        KeyModifiers = KeyModifiers.None
    });

    textBox.Text.Should().Be("a");
}
```

## 7. Test fixture setup with themes

```csharp
using Avalonia.Headless;

public class TestApp : Application
{
    public TestApp()
    {
        Styles.Add(new FluentTheme());
    }
}

public class TestsBase : IDisposable
{
    public TestsBase()
    {
        var app = new TestApp();
        app.Start(new ClassicDesktopStyleApplicationLifetime(Array.Empty<string>()));
    }

    public void Dispose()
    {
        // Cleanup
    }
}
```

## 8. Mocking platform services

```csharp
[AvaloniaFact]
public async Task FileOpen_RequestsStorage()
{
    var mockStorage = new Mock<IStorageProvider>();
    mockStorage
        .Setup(s => s.OpenFilePickerAsync(It.IsAny<FilePickerOpenOptions>()))
        .ReturnsAsync(new[] { new MockStorageFile("test.txt") });

    var vm = new FileViewModel(mockStorage.Object);

    await vm.OpenFileCommand.ExecuteAsync(null);

    vm.CurrentFile.Should().Be("test.txt");
}
```

## Key takeaways

- `[AvaloniaFact]` and `[AvaloniaTheory]` run tests on a headless Avalonia platform without a real window
- Use `Control.CaptureRenderedImage()` for pixel-level assertions
- Measure/Arrange cycle is necessary for layout-dependent behavior
- Test focus and keyboard input by showing a `Window` and calling `Focus()`
- Mock `IStorageProvider` and other platform services for unit tests
- Apply themes in a test `Application` subclass to get access to styles and brushes

## See Also

- [038E — Headless Testing with Avalonia.Headless (examples)](038-headless-testing-examples.md)
- [038V — Headless Testing with Avalonia.Headless (verbose companion)](038-headless-testing-verbose.md)
- [039 — NativeAOT and Trimming](039-nativeaot-trimming.md)
- [007 — Observable Object and Property](../basics/007-observable-object-property.md)
- [Avalonia Docs: Headless Testing](https://docs.avaloniaui.net/docs/concepts/headless-testing)
