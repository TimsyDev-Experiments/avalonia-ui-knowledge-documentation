---
tier: advanced
topic: testing
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 038-headless-testing.md
---

# Quiz — Headless Testing

```quiz
Q: Which attribute from Avalonia.Headless.XUnit replaces [Fact] to run a test on the headless Avalonia platform?
A. [AvaloniaFact] (correct) || AvaloniaFact initializes a headless Avalonia application and dispatcher for the duration of the test method.
B. [HeadlessFact] || No such attribute exists in Avalonia.Headless.XUnit.
C. [AvaloniaTest] || The correct attribute name is AvaloniaFact, not AvaloniaTest.
D. [UIFact] || UIFact is not part of Avalonia.Headless; AvaloniaFact is the correct attribute.
Explanation: [AvaloniaFact] bootstraps the headless platform. For parameterized tests, use [AvaloniaTheory] with [InlineData].
```

```quiz
Q: Why must Measure and Arrange be called on a Button before raising a PointerPressedEvent in a headless test?
A. The headless platform does not run the layout pass automatically; manual Measure/Arrange ensures the control has valid bounds for hit testing (correct) || Controls must be measured and arranged to participate in layout-dependent operations like pointer events.
B. Measure and Arrange register the control in the visual tree || Measure/Arrange are layout operations; registering in the visual tree happens through a parent container.
C. Without Measure/Arrange, the Button.Command will not execute || Command execution depends on the Command property binding, not layout.
D. PointerPressedEvent requires a rendered bitmap which is produced during Arrange || Pointer events do not require a bitmap; they require bounds and layout.
Explanation: Headless tests do not run an automatic layout cycle. Call Measure and Arrange before raising input events to ensure the control has valid dimensions.
```

```quiz
Q: How do you verify the rendered visual state of a control in a headless test?
A. Call control.CaptureRenderedImage() and inspect pixels (correct) || CaptureRenderedImage returns a bitmap that can be queried with GetPixel for color assertions.
B. Read control.Bounds and compute the expected color from the Background property || Bounds gives position and size, not rendered pixel data.
C. Use VisualTreeHelper.GetDrawing(control) || There is no GetDrawing method in Avalonia's VisualTreeHelper.
D. Serialize the control to XAML and compare with a baseline || Serialization captures the logical tree, not rendered output.
Explanation: CaptureRenderedImage() renders the control off-screen. Use bitmap.GetPixel(x, y) with FluentAssertions for pixel-level verification.
```

```quiz
Q: Which approach correctly mocks IStorageProvider for testing a ViewModel that opens files?
A. 
```csharp
var mock = new Mock<IStorageProvider>();
mock.Setup(s => s.OpenFilePickerAsync(It.IsAny<FilePickerOpenOptions>()))
    .ReturnsAsync(new[] { new MockStorageFile("test.txt") });
```
 (correct) || Mocking the interface and injecting it into the ViewModel isolates file-picker logic from platform dependencies.
B. `var mock = new Mock<StorageProvider>();` || StorageProvider is a concrete class, not easily mockable; mock the IStorageProvider interface instead.
C. Create a real Window and call TopLevel.GetTopLevel to access its StorageProvider || This adds an unnecessary UI dependency and defeats the purpose of headless unit testing.
D. `var provider = new HeadlessStorageProvider();` || There is no HeadlessStorageProvider class in Avalonia.Headless.
Explanation: Mock IStorageProvider, set up OpenFilePickerAsync to return a collection of mock storage files, and inject the mock into the ViewModel.
```

```quiz
Q: A headless test for keyboard input shows that textBox.Text is empty after raising KeyDown with Key.A. What is missing?
A. The TextBox must be focused by calling Focus() after the window is shown (correct) || Focus() directs keyboard input to the TextBox; without it, the KeyDown event is not processed by the TextBox.
B. The KeyDown event should be KeyPress instead || Avalonia uses KeyDown; there is no KeyPress event for text input.
C. TextBox requires a Measure/Arrange cycle before it accepts text || Layout affects rendering but not focus or text input.
D. The key should be Key.A with a KeyModifiers.Shift modifier || The lowercase "a" does not require Shift; the issue is unhandled focus.
Explanation: Call window.Show() then textBox.Focus() before raising the KeyDown event to ensure the TextBox is the focused input element.
```
