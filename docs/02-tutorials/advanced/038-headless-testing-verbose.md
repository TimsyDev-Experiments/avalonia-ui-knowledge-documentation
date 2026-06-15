---
tier: advanced
topic: testing
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 038-headless-testing.md
---

# 038V — Headless Testing with Avalonia.Headless: An In-Depth Companion

**Why this exists.** The original tutorial shows how to write tests with `[AvaloniaFact]`, measure/arrange, render capture, and mocking. This companion explains how the headless platform works under the hood, why `[AvaloniaFact]` must bootstrap an entire `Application` per test, what `Measure`/`Arrange` actually does in a headless context, why `CaptureRenderedImage` can return `null`, and how to avoid the common pitfalls around focus, keyboard, and disposal.

**Read this alongside:** [038 — Headless Testing](../advanced/038-headless-testing.md)

---

## 1. What the headless platform is

The headless platform is a full Avalonia compositor that runs without a real OS window, GPU, or display. It uses:

- **A software renderer** (Skia — no GPU) that writes to an offscreen bitmap.
- **A virtual input system** that dispatches pointer, keyboard, and focus events within the compositor.
- **No real window manager** — `Window.Show()` creates an invisible headless surface, not an HWND.
- **`Dispatcher` still runs** — the test thread pumps the dispatcher loop as needed.

The headless platform is a standard Avalonia `Application` instance configured with `UseHeadless()` in the test setup:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect() // Detects headless
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
```

The `Avalonia.Headless.XUnit` package's `[AvaloniaFact]` attribute handles this setup and teardown automatically.

---

## 2. How `[AvaloniaFact]` works

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
    // ...
}
```

`[AvaloniaFact]` is a custom xUnit fact attribute that:

1. Before the test: creates an `Application` instance and starts it with the headless lifetime (if no headless app is running).
2. During the test: runs the test code with `SynchronizationContext` set to the Avalonia `Dispatcher`.
3. After the test: does not shut down the app (it is reused for the next test). The headless app is created once per test class.

This means your test code runs on the UI thread. You can create controls, set properties, and read values without needing `Dispatcher.UIThread.InvokeAsync`.

**For NUnit:** use `[AvaloniaTest]` from `Avalonia.Headless.NUnit`.

**For MSTest:** use `[AvaloniaTestMethod]` from `Avalonia.Headless.MSTest`.

---

## 3. Why `Measure` and `Arrange` are necessary

```csharp
button.Measure(new Size(100, 30));
button.Arrange(new Rect(0, 0, 100, 30));
```

Avalonia's layout system is lazy — controls do not measure or arrange themselves until a parent panel or `TopLevel` triggers the layout pass. In a headless test, there is no active layout pass running unless you:

- Add the control to a `Window` and call `window.Show()`, which triggers one layout pass.
- Manually call `Measure` and `Arrange`.

`Measure(Size)` computes the control's desired size: it walks the visual tree calling `MeasureOverride` on each element.

`Arrange(Rect)` positions the control within a rectangle: it calls `ArrangeOverride` on each element.

Without these calls:

- `Bounds` is `Rect.Empty`.
- `DesiredSize` is `Size.Empty`.
- `RenderBounds` is undefined.
- `CaptureRenderedImage()` returns `null` (no layout = no render).
- Hit testing (`InputHitTest`) returns no results.

**Which size to pass to `Measure`?** Pass the size constraint that the control would receive from its parent. For a button in a `StackPanel`, that is usually `(availableWidth, Infinity)`. For a fixed-size control, use `(width, height)`.

**Which rect to pass to `Arrange`?** The final position and size. Usually `(0, 0, measuredWidth, measuredHeight)`.

---

## 4. Using compiled bindings in tests

The original tutorial uses reflection binding:

```csharp
new TextBlock().Also(t => t.Bind(
    TextBlock.TextProperty,
    new Binding("Label")))
```

For NativeAOT-safe tests, use compiled bindings or direct property assignment:

```csharp
// Direct assignment — simplest, no binding infrastructure needed
var textBlock = new TextBlock();
textBlock.Text = vm.Label;

// Or use compiled binding with x:DataType via a DataTemplate and ContentControl
```

Direct property assignment is preferred in unit tests because it avoids the binding engine entirely, making tests faster and more deterministic.

If you must test bindings (e.g., testing a binding converter), create a `Window` with the bound control and show it:

```csharp
[AvaloniaFact]
public void Label_Binding_Works()
{
    var vm = new MainViewModel { Label = "Hello" };
    var textBlock = new TextBlock();
    textBlock.Bind(TextBlock.TextProperty, new Binding("Label"));
    textBlock.DataContext = vm;

    var window = new Window { Content = textBlock };
    window.Show();

    textBlock.Text.Should().Be("Hello");
}
```

`window.Show()` triggers the layout pass and binding evaluation.

---

## 5. `CaptureRenderedImage()` — pixel-level assertion

```csharp
var bitmap = border.CaptureRenderedImage()!;
var pixel = bitmap.GetPixel(50, 50);
pixel.ToAvaloniaColor().Should().Be(Colors.Red);
```

`CaptureRenderedImage()` renders the control's subtree to an offscreen bitmap using the Skia software renderer. It returns `null` if:

- The control has not been laid out (no `Measure`/`Arrange` or `Show()`).
- The control has zero width or height.
- The control is not attached to a visual tree.

The pixel returned by `GetPixel(x, y)` is in the sRGB color space. `ToAvaloniaColor()` converts from the internal pixel format to `Avalonia.Media.Color`.

**Performant assertions:** comparing single pixels is fast. Comparing full bitmaps (`bitmap == expectedBitmap`) is slow. For functional tests, check a few key pixels. For visual regression tests, use `CaptureRenderedImage()` with a tolerance comparison.

---

## 6. Focus and keyboard testing — why `Show()` is required

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

**Why `window.Show()` is required:** `Focus()` only works when the control is part of a `TopLevel` that has been shown. In the headless platform, `Show()` activates the window's focus scope. Without it, `textBox.Focus()` returns `false` and `textBox.IsFocused` stays `false`.

**`KeyEventArgs`** must set `RoutedEvent` explicitly — the parameterless constructor does not set a default routed event. `InputElement.KeyDownEvent` is the bubbling event. For key-up, use `InputElement.KeyUpEvent`.

**Character input:** the `KeyEventArgs.Key` property maps to physical keys, not characters. `Key.A` produces `"a"` because no modifier is held. `Key.A + KeyModifiers.Shift` produces `"A"`. For text input with IME support, use `TextInputEventArgs` with `RoutedEvent = InputElement.TextInputEvent`.

**Common mistake:** forgetting `window.Show()` — `Focus()` silently fails.

---

## 7. Test fixture setup with themes

```csharp
public class TestApp : Application
{
    public TestApp()
    {
        Styles.Add(new FluentTheme());
    }
}
```

Themes are required for:

- **Lookup of dynamic resources** (`{DynamicResource}` in styles).
- **Default control styles** — without a theme, `Button`, `TextBox`, `DataGrid`, etc., have no visual tree and their template children are null.
- **`CaptureRenderedImage()`** — controls use theme-defined brushes and templates.

The `[AvaloniaFact]` attribute from `Avalonia.Headless.XUnit` auto-configures `FluentTheme` by default. If you use the base headless setup (without XUnit attribute), you must apply a theme manually.

**Common mistake:** tests pass without themes (controls exist and properties work) but `CaptureRenderedImage()` returns an empty or incorrectly styled image.

---

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

Platform services are interfaces (`IStorageProvider`, `IClipboard`, `IScreens`, etc.), which makes them mockable. The pattern:

1. Create a mock using Moq, NSubstitute, or a hand-written stub.
2. Inject it into the ViewModel (via constructor or property).
3. Call the command.
4. Assert the ViewModel state changed as expected.

**Do not mock more than necessary.** If the test only checks that the ViewModel updates `CurrentFile`, mock only `IStorageProvider`. Do not mock `IClipboard`, `IScreens`, or other unrelated services.

**Hand-written stub vs mocking framework:** for simple cases, a hand-written `IStorageProvider` stub is easier:

```csharp
public class StubStorageProvider : IStorageProvider
{
    public Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        => Task.FromResult<IReadOnlyList<IStorageFile>>(new[] { new MockStorageFile("test.txt") });

    // Other members throw NotSupportedException
}
```

---

## Key differences from the original

| Concept | Original says | Why it matters |
|---------|---------------|----------------|
| Reflection `Binding("Label")` | Used in test | Does not work under NativeAOT — use direct assignment or compiled bindings |
| `PointerPressedEventArgs` constructor | Shown with complex args | Constructor signature may differ per Avalonia version — check current API |
| `CaptureRenderedImage()` | Shown as always returning a bitmap | Returns `null` if not laid out — always guard with null check |
| `KeyEventArgs.RoutedEvent` | Set to `InputElement.KeyDownEvent` | The parameterless constructor does not set `RoutedEvent` — omitting it causes silent routing failure |

---

## See Also

- [038 — Headless Testing](038-headless-testing.md) — the original tutorial
- [038E — Headless Testing with Avalonia.Headless (examples)](038-headless-testing-examples.md)
- [039 — NativeAOT and Trimming](039-nativeaot-trimming.md) — why compiled bindings matter in tests
- [007 — Observable Object and Property](../basics/007-observable-object-property.md) — ViewModel property changes that tests verify
- [010 — Window and Dialog Basics](../basics/010-window-dialog-basics.md) — window creation, used in integration tests
- [Avalonia Docs: Headless Testing](https://docs.avaloniaui.net/docs/concepts/headless-testing)
