---
tier: advanced
topic: layout
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 084 — Typography & Text Features — Examples

**Prerequisites:** [084-core](084-typography-text-features.md)

---

## Example 1: Complete type scale in App.axaml

```xml
<Application.Styles>
  <FluentTheme />
  <Style Selector="TextBlock.h1">
    <Setter Property="FontSize" Value="32" />
    <Setter Property="FontWeight" Value="Bold" />
    <Setter Property="LineHeight" Value="40" />
  </Style>
  <Style Selector="TextBlock.h2">
    <Setter Property="FontSize" Value="24" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="LineHeight" Value="32" />
  </Style>
  <Style Selector="TextBlock.h3">
    <Setter Property="FontSize" Value="20" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="LineHeight" Value="28" />
  </Style>
  <Style Selector="TextBlock.subtitle">
    <Setter Property="FontSize" Value="16" />
    <Setter Property="FontWeight" Value="Medium" />
    <Setter Property="Foreground" Value="#6B7280" />
  </Style>
  <Style Selector="TextBlock.body">
    <Setter Property="FontSize" Value="14" />
    <Setter Property="LineHeight" Value="20" />
  </Style>
  <Style Selector="TextBlock.body-large">
    <Setter Property="FontSize" Value="16" />
    <Setter Property="LineHeight" Value="24" />
  </Style>
  <Style Selector="TextBlock.caption">
    <Setter Property="FontSize" Value="12" />
    <Setter Property="Foreground" Value="#9CA3AF" />
  </Style>
  <Style Selector="TextBlock.code">
    <Setter Property="FontFamily" Value="Cascadia Code, JetBrains Mono, Consolas" />
    <Setter Property="FontSize" Value="13" />
    <Setter Property="Background" Value="#F3F4F6" />
    <Setter Property="Padding" Value="8" />
  </Style>
</Application.Styles>
```

---

## Example 2: Rich formatted text with inlines

```xml
<Border Padding="16" Background="White" CornerRadius="8"
        BorderThickness="1" BorderBrush="#E5E7EB">
  <StackPanel Spacing="12">
    <TextBlock Classes="h1">
      <Run Text="Welcome, " />
      <Run FontWeight="Bold" Foreground="#2563EB"
           Text="{Binding UserName}" />
    </TextBlock>

    <TextBlock Classes="body" TextWrapping="Wrap">
      Your account was created on
      <Run FontWeight="SemiBold" Text="{Binding CreatedDate, StringFormat='{0:MMMM dd, yyyy}'}" />
      . You have
      <Run FontWeight="Bold" Foreground="#059669"
           Text="{Binding ProjectCount}" />
      active projects.
      <LineBreak />
      <LineBreak />
      <Span Foreground="#6B7280" FontStyle="Italic">
        Tip: Use the sidebar to navigate between sections.
      </Span>
    </TextBlock>

    <TextBlock Classes="caption">
      <InlineUIContainer>
        <Image Width="14" Height="14" Source="/Assets/info.png" />
      </InlineUIContainer>
      Last updated 5 minutes ago
    </TextBlock>
  </StackPanel>
</Border>
```

---

## Example 3: Custom text decorations (link style)

```xml
<TextBlock Text="Hover for underlined link effect"
           Cursor="Hand">
  <TextBlock.Styles>
    <Style Selector="TextBlock:pointerover">
      <Setter Property="TextDecorations">
        <TextDecorationCollection>
          <TextDecoration Location="Underline"
                          Stroke="#2563EB"
                          StrokeThickness="1.5" />
        </TextDecorationCollection>
      </Setter>
    </Style>
  </TextBlock.Styles>
</TextBlock>
```

---

## Example 4: OpenType features for data display

```xml
<StackPanel Spacing="4" TextElement.FontFeatures="+tnum">
  <TextBlock Classes="h2">Financial Summary</TextBlock>
  <Border Padding="12" Background="#F9FAFB" CornerRadius="6">
    <StackPanel Spacing="4">
      <Grid ColumnDefinitions="120,*">
        <TextBlock Text="Revenue:" Foreground="#6B7280" />
        <TextBlock Text="{Binding Revenue, StringFormat='C'}"
                   FontWeight="SemiBold" />
      </Grid>
      <Grid ColumnDefinitions="120,*">
        <TextBlock Text="Expenses:" Foreground="#6B7280" />
        <TextBlock Text="{Binding Expenses, StringFormat='C'}"
                   FontWeight="SemiBold" />
      </Grid>
      <Rectangle Height="1" Fill="#E5E7EB" />
      <Grid ColumnDefinitions="120,*">
        <TextBlock Text="Net:" FontWeight="Bold" />
        <TextBlock Text="{Binding Net, StringFormat='C'}"
                   FontWeight="Bold" Foreground="#059669" />
      </Grid>
    </StackPanel>
  </Border>
</StackPanel>
```

Tabular numbers (`+tnum`) ensure all digits occupy the same width, so columns align vertically.

---

## Example 5: Embedded custom font with fallback

**Assets/Fonts/Nunito/ directory** containing `Nunito-Regular.ttf`, `Nunito-Bold.ttf`, etc.

**.csproj:**
```xml
<AvaloniaResource Include="Assets\Fonts\**" />
```

**App.axaml:**
```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary>
        <FontFamily x:Key="NunitoFont">avares://MyApp/Assets/Fonts/Nunito#Nunito</FontFamily>
      </ResourceDictionary>
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

**Usage:**
```xml
<TextBlock FontFamily="{StaticResource NunitoFont}"
           FontWeight="Bold" FontSize="28"
           Text="Welcome to MyApp" />
```

---

## Example 6: Embedded font collection for scheme-based lookup

**FontCollection.cs:**
```csharp
using Avalonia.Media.Fonts;

public sealed class AppFontCollection : EmbeddedFontCollection
{
    public AppFontCollection() : base(
        new Uri("fonts:App", UriKind.Absolute),
        new Uri("avares://MyApp/Assets/Fonts", UriKind.Absolute))
    {
    }
}
```

**Program.cs:**
```csharp
public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .ConfigureFonts(fm => fm.AddFontCollection(new AppFontCollection()))
        .LogToTrace();
```

**Usage:**
```xml
<TextBlock FontFamily="fonts:App#Nunito" FontSize="24" Text="Nunito via scheme" />
<TextBlock FontFamily="fonts:App#Open Sans" FontSize="24" Text="Open Sans via scheme" />
```

---

## Example 7: SelectableTextBlock with inlines

```xml
<SelectableTextBlock TextWrapping="Wrap" Classes="body">
  <Run Text="You can copy this text. " />
  <Run FontWeight="Bold" Text="{Binding ImportantNotice}" />
  <LineBreak />
  <Span Foreground="#DC2626">
    <Run Text="Warning: " FontWeight="Bold" />
    <Run Text="{Binding WarningMessage}" />
  </Span>
</SelectableTextBlock>
```

---

## Example 8: Text options for animation

```xml
<Border Background="White" Padding="16">
  <Border.Styles>
    <Style Selector="Border:pointerover TextBlock#slide">
      <Setter Property="RenderTransform" Value="translateX(20)" />
    </Style>
  </Border.Styles>

  <TextBlock x:Name="slide" Text="Smooth slide on hover"
             TextOptions.TextHintingMode="None"
             TextOptions.BaselinePixelAlignment="Unaligned">
    <TextBlock.Transitions>
      <TransformOperationsTransition Property="RenderTransform"
                                     Duration="0:0:0.3" />
    </TextBlock.Transitions>
  </TextBlock>
</Border>
```

---

## Example 9: Responsive type scale with container queries

```xml
<ScrollViewer>
  <Border Container.Name="content" Container.Sizing="Width"
          Padding="24" TextElement.FontFamily="Inter">
    <Border.Styles>
      <ContainerQuery Name="content" Query="max-width:500">
        <Style Selector="TextBlock.title">
          <Setter Property="FontSize" Value="20" />
          <Setter Property="LineHeight" Value="28" />
        </Style>
      </ContainerQuery>
      <ContainerQuery Name="content" Query="min-width:500">
        <Style Selector="TextBlock.title">
          <Setter Property="FontSize" Value="28" />
          <Setter Property="LineHeight" Value="36" />
        </Style>
      </ContainerQuery>
      <ContainerQuery Name="content" Query="min-width:900">
        <Style Selector="TextBlock.title">
          <Setter Property="FontSize" Value="36" />
          <Setter Property="LineHeight" Value="44" />
        </Style>
      </ContainerQuery>
    </Border.Styles>

    <StackPanel Spacing="16">
      <TextBlock Classes="title" Text="Responsive Typography" />
      <TextBlock Classes="body" TextWrapping="Wrap"
                 Text="The title above adjusts its size based on the container width, not the window width." />
    </StackPanel>
  </Border>
</ScrollViewer>
```

---

## Key Takeaways

- Define a type scale once in `App.axaml` using `Style` selectors on `TextBlock` classes
- Inlines (`Run`, `Span`, `LineBreak`, `InlineUIContainer`) enable mixed-format rich text
- Custom `TextDecoration` with pseudo-class styles creates interactive link effects
- `+tnum` OpenType feature aligns columns of numbers
- Custom fonts use `avares://` URIs with `#FontFamilyName` — static resources or `EmbeddedFontCollection`
- `TextOptions.BaselinePixelAlignment="Unaligned"` + `TextHintingMode="None"` for smooth animated text
- Container queries can scale typography based on container width, not just window width
