---
tier: intermediate
topic: controls
avalonia-version: 12.0.4
quiz-format: multiple-choice
---

# 065Q — Range & Toggle Controls (quiz)

## Q1. Which two properties must be set together to restrict a Slider to discrete values?

- [ ] A. `SmallChange` and `LargeChange`
- [ ] B. `TickFrequency` and `IsSnapToTickEnabled`
- [ ] C. `Minimum` and `Maximum`
- [ ] D. `Orientation` and `IsDirectionReversed`

**Answer:** B. `TickFrequency` sets the interval; `IsSnapToTickEnabled` snaps the thumb to those ticks.

---

## Q2. How do you make a ProgressBar show an animated loop when the operation duration is unknown?

- [ ] A. Set `Value` to -1
- [ ] B. Set `IsIndeterminate="True"`
- [ ] C. Set `AnimationMode="Loop"`
- [ ] D. Set `ShowProgressText="False"`

**Answer:** B. `IsIndeterminate` displays a looping animation.

---

## Q3. Which NumericUpDown property controls the step size of the spinner buttons?

- [ ] A. `Step`
- [ ] B. `Increment`
- [ ] C. `SmallChange`
- [ ] D. `TickFrequency`

**Answer:** B. `Increment` sets the amount changed per spinner click, arrow key, or scroll.

---

## Q4. What type should a three-state CheckBox bind to in the view model?

- [ ] A. `bool`
- [ ] B. `bool?`
- [ ] C. `int`
- [ ] D. `CheckState`

**Answer:** B. `bool?` allows `null` for the indeterminate state.

---

## Q5. How do you group RadioButtons that are in different parent containers?

- [ ] A. Set the same `GroupName` on each
- [ ] B. Wrap them in a `GroupBox`
- [ ] C. Set `x:Shared="False"`
- [ ] D. RadioButtons in different parents are automatically grouped

**Answer:** A. `GroupName` creates a logical group regardless of parent container.

---

## Q6. Which converter enables binding RadioButton IsChecked to an enum property?

- [ ] A. `EnumToBoolConverter`
- [ ] B. `BoolToEnumConverter`
- [ ] C. `RadioButtonConverter`
- [ ] D. `ValueConverter`

**Answer:** A. `EnumToBoolConverter` maps each enum value to a `bool` via `ConverterParameter`.

---

## Q7. How do you hide the on/off label text on a ToggleSwitch?

- [ ] A. Set `ShowLabels="False"`
- [ ] B. Set `OnContent=""` and `OffContent=""`
- [ ] C. Set `Content=""`
- [ ] D. Set `LabelVisibility="Collapsed"`

**Answer:** B. Empty strings on `OnContent` and `OffContent` hide the labels.

---

## Q8. What happens when a user types a value outside the NumericUpDown range?

- [ ] A. An exception is thrown
- [ ] B. The value is clamped to the nearest boundary on loss of focus
- [ ] C. The input is rejected immediately
- [ ] D. The control enters an error state

**Answer:** B. The value is clamped to `Minimum` or `Maximum` when the field loses focus.

---

## Q9. Which ProgressBar part can be styled to change the filled area appearance?

- [ ] A. `PART_Fill`
- [ ] B. `PART_Indicator`
- [ ] C. `PART_Bar`
- [ ] D. `PART_Progress`

**Answer:** B. `PART_Indicator` is the `Border` representing the filled area.

---

## Q10. Which property reverses the direction of increasing value on a Slider?

- [ ] A. `ReverseDirection`
- [ ] B. `IsDirectionReversed`
- [ ] C. `FlowDirection`
- [ ] D. `MirrorLayout`

**Answer:** B. `IsDirectionReversed="True"` places the maximum at the left/bottom.

---

## Scoring

| Score | Interpretation |
|-------|---------------|
| 10/10 | Expert |
| 8-9 | Strong understanding |
| 6-7 | Getting there |
| <6 | Review the core tutorial |

---

## See Also

- [065 — Range & Toggle Controls (core)](065-range-toggle-controls.md)
- [065V — Range & Toggle Controls (verbose)](065-range-toggle-controls-verbose.md)
- [065E — Range & Toggle Controls (examples)](065-range-toggle-controls-examples.md)
