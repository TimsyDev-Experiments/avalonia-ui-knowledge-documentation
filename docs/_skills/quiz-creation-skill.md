# Quiz Creation Skill

**Purpose:** Create interactive quiz files that pair with tutorial content. Quizzes are rendered as graded widgets by `docs/viewer.html` and degrade gracefully in plain markdown viewers.

## Trigger Conditions

Run this skill when:
- A tutorial needs a knowledge-check layer to reinforce key concepts
- A reader would benefit from self-assessment after completing a tutorial
- Expanding the quiz layer across the doc set (currently piloted on `001-project-setup`)

## File Convention

### Naming

`NNN-topic-quiz.md` — same number prefix as the original tutorial, append `-quiz`.

Example: `001-project-setup.md` → `001-project-setup-quiz.md`

### Location

Same directory as the original tutorial. Quiz lives alongside, not in a separate folder.

### Front-matter

```yaml
---
tier: basics|intermediate|advanced
topic: <same as original>
estimated: 1-2 min per question
researched: YYYY-MM-DD
avalonia-version: 12.0.4
example-of: NNN-topic.md
---
```

Use `example-of` (not `companion-to`) to indicate the quiz tests knowledge from the referenced tutorial.

### Title

`001Q — Project Setup (quiz)` in the DOC_INDEX. The page itself should have `# Quiz — Topic Title` as the H1.

---

## Quiz Format

Each quiz question is a fenced code block with the language `quiz`:

````markdown
```quiz
Q: Question text
A. Option 1 || Per-option explanation for A.
B. Option 2 (correct) || Per-option explanation for B.
C. Option 3 || Per-option explanation for C.
D. Option 4 || Per-option explanation for D.
Explanation: Overall explanation covering the key concept.
```
````

### Rules

| Element | Required | Format |
|---|---|---|
| `Q:` | Yes | Question text. Must be on its own line. |
| `A.` / `B.` / `C.` / `D.` | Yes (at least 2) | `Letter. <text> || <explanation>`. Letters run A-Z. |
| `(correct)` | Yes (exactly 1 per question) | Placed after the option text, before ` || `. Marks the right answer. |
| ` || ` | Yes | Separates option text from per-option explanation. Space before and after pipes. |
| `Explanation:` | Yes | Overall summary text. Single line after all options. |

### Option lettering

- Start at `A.` for the first option in each question.
- Maximum 26 options (A–Z), but 4 is standard.
- Lettering resets per question (each ````quiz` block is independent).

### Quiz blocks

- Each ````quiz` fenced code block is **one question**.
- Multiple blocks on the same page are **merged** by `viewer.html` into a single quiz widget with one "Complete Quiz" button.
- Blocks are separated by blank lines in the markdown file.

### Content guidelines

- **Options:** 1 correct, rest incorrect. Make distractors plausible — common mistakes, off-by-one API names, wrong return types, wrong patterns from the same topic area.
- **Per-option explanations** (` || `): Explain *why* the option is right or wrong. Be specific — reference the API, the pattern, or the constraint. Keep to 1-2 sentences.
- **Overall explanation:** 1-3 sentences that reinforce the correct answer and summarize why the distractors don't apply. Connect back to the tutorial's key takeaway.
- **Code in options:** Wrap inline code in backticks. Avoid multi-line code blocks inside quiz options — they break the single-line format.
- **Tone:** Technical, direct, neutral. Same rules as `documentation-skill.md`.

---

## Question Count

The number of questions should reflect the topic material — cover what the tutorial teaches. Gauge by:

- How many distinct concepts or APIs the tutorial introduces
- What the reader needs to demonstrate to prove comprehension
- The depth of the material (a 2-step basics tutorial needs fewer than a 10-step advanced tutorial)

If a topic is broad enough that a single quiz would feel bloated, split into smaller quizzes (e.g., `001A-quiz.md` and `001B-quiz.md`). The quiz must ensure the user understands the concept or topic *and* can apply it — include at least one question requiring the reader to identify the correct code or spot the error.

---

## Registration

### docs/00-index.md

Add the quiz indented under the original tutorial entry:

```markdown
N. [NNN — Original Title](02-tutorials/.../NNN-topic.md)
   ⌞ [NNNQ — Original Title (quiz)](02-tutorials/.../NNN-topic-quiz.md)
```

### docs/viewer.html

Add an entry in the `DOC_INDEX` array near the original:

```javascript
{ label: "NNNQ — Original Title (quiz)", file: "02-tutorials/.../NNN-topic-quiz.md" },
```

---

## Implementation Detail (viewer.html)

### Rendering pipeline

```
markdown ──▶ marked ──▶ HTML ──▶ initQuizzes()
                                    │
                                    ▼
                              parseQuizText()  ──▶  [{question, options, explanation}, ...]
                                    │
                                    ▼
                              buildQuizWidget() ──▶  <div.quiz-container>
                                    │
                                    ▼
                              completeQuiz()    ──▶  grade + reveal
```

### How initQuizzes works

1. Queries `.quiz-container` for `pre code.language-quiz` elements.
2. Collects all `<pre>` parent elements into an array (to avoid DOM removal during iteration).
3. Calls `parseQuizText()` on each code block's `textContent`.
4. Builds a single quiz widget via `buildQuizWidget()`.
5. Inserts the widget before the first original `<pre>` block, then removes all originals.

Critical fix (2026-06-15): Previously `firstPre` was captured before the `forEach` loop, but `codeEl.parentElement.remove()` inside the loop removed it from the DOM, causing `null.parentElement.insertBefore` when the loop finished. Fixed by collecting all `<pre>` elements first, building the widget, then removing.

### How buildQuizWidget works

Creates:
- `.quiz-header` — "Quiz" badge + question count
- `.quiz-question` per question — each contains:
  - `.quiz-question-text` — "1. What is..."
  - `.quiz-options` — wrapper for option divs
  - `.quiz-option` per option — marker circle + text span + explanation div (hidden)
  - `.quiz-explanation` — overall explanation (hidden)
- `.quiz-actions` — "Complete Quiz" button
- `.quiz-result` — score display (hidden)

Option click handler: selects the clicked option (adds `.selected`, removes from siblings). Disabled once quiz is completed.

### How completeQuiz works

1. Adds `.quiz-completed` class to `.quiz-container` — triggers CSS transitions.
2. Adds `.disabled` + `.correct`/`.incorrect` classes to options.
3. CSS handles explanation visibility via `.quiz-completed .quiz-option-explanation` and `.quiz-completed .quiz-explanation` selectors (max-height + opacity transition).
4. Calculates score: counts selected options with `data-correct="true"`.
5. Displays result with percentage and status text (Perfect / Good / Needs Work).

### CSS architecture

| Element | Hidden state | Revealed state | Mechanism |
|---|---|---|---|
| `.quiz-option-explanation` | `max-height: 0; opacity: 0; overflow: hidden` | `max-height: 300px; opacity: 1; padding: .4rem .5rem` | CSS transition on `.quiz-completed` parent |
| `.quiz-explanation` | `max-height: 0; opacity: 0; overflow: hidden` | `max-height: 500px; opacity: 1; padding: .75rem .85rem; margin-top: .65rem` | Same |
| `.quiz-result` | `display: none` | `display: block` (via JS) | JS sets `style.display = 'block'` |

### Layout fix (2026-06-15)

`.quiz-option` uses `display: flex; flex-wrap: wrap;`. The `.quiz-option-explanation` has `flex-basis: 100%; margin-left: calc(1.5rem + .5rem)` to wrap below the option text and align under where the text starts (after the 1.5rem marker circle + .5rem gap).

Previously `.quiz-option` had no `flex-wrap`, so all children (marker, text, explanation) sat in a single row. The explanation appeared to the right of the text, pushing content aside.

### Transition smoothness

Both explanation types use `transition: max-height .35s ease, opacity .35s ease, padding .35s ease, border-color .35s ease`. This provides a smooth slide-in + fade-in when `.quiz-completed` is added. The 350ms duration feels deliberate without being slow.

### Word break safety

Both `quiz-option-explanation` and `quiz-explanation` have `word-break: break-word` to prevent long unbroken strings from overflowing the layout.

### HTML injection prevention

All user-facing text in the quiz widget is set via `textContent` (not `innerHTML`), including per-option explanations, overall explanations, option text, question text, and the score display. This prevents HTML injection from quiz code blocks in the markdown that contain angle brackets.

---

## Tools Referenced

| Tool | Where |
|---|---|
| `docs/viewer.html` | Quiz rendering engine (CSS + JS) |
| `docs/_skills/quiz-creation-skill.md` | This file |
| `docs/_assets/templates/quiz-template.md` | Blank quiz template |
| `docs/00-index.md` | Registration of all quiz files |
| `docs/02-tutorials/basics/001-project-setup-quiz.md` | Pilot quiz (reference implementation) |

---

## Audit Checklist

- [ ] Front-matter uses `example-of`, not `companion-to`.
- [ ] Question count matches the topic depth (not an arbitrary default).
- [ ] Each question has exactly 1 `(correct)` marker.
- [ ] Every option has a ` || ` per-option explanation.
- [ ] Every question has an `Explanation:` line.
- [ ] No HTML in quiz code blocks — safe with `textContent`.
- [ ] Distractors are plausible (common mistakes, not obviously wrong).
- [ ] Per-option explanations are specific (reference API names, patterns).
- [ ] Overall explanations connect to the tutorial's key takeaways.
- [ ] Tutorial registered in `docs/00-index.md` and `docs/viewer.html`.
- [ ] No `file:///` paths in cross-references (see `link-audit.ps1`).
- [ ] Preceding `x:DataType` rules apply if code examples reference Avalonia XAML.
- [ ] Quiz renders correctly in both `viewer.html` and plain markdown viewers.
