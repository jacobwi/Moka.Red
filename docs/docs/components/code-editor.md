---
title: Code Editor
description: A text area for code or settings, with line numbers, coloring and a list of problems, all supplied by the app.
order: 153
---

# Code Editor

`MokaCodeEditor` is a text area for code or a configuration file: monospace, with line numbers down the side, coloring from a highlighter the app passes, and problems from a checker the app passes, underlined in place and listed below with a jump to each. Neither the highlighter nor the checker is built in, so it suits any language. It's meant for texts of a few thousand lines at most, since every edit colors the whole text again. For plain text, use [`MokaTextArea`](/components/forms).

## Setup

```razor
@using Moka.Red.Forms.CodeEditor
```

## Parameters

Besides these, it takes what every text input does: `Value` and `@bind-Value`, `Placeholder`, `MaxLength`, `ReadOnly`, `Disabled` and `DebounceDelay`.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Label`, `HelperText`, `ErrorText` | `string?` | -- | The field's label and the lines under it |
| `Required`, `Optional` | `bool` | `false` | Marks on the label |
| `Highlighter` | `Func<string, IEnumerable<MokaCodeSpan>>?` | -- | Given the whole text, the stretches to color, with indexes into it |
| `Diagnose` | `Func<string, IReadOnlyList<MokaCodeDiagnostic>>?` | -- | Checks the text. Runs when the value is set and `DiagnoseDelay` after typing stops |
| `Diagnostics` | `IReadOnlyList<MokaCodeDiagnostic>?` | -- | Problems the app found itself, such as a save the server refused. Hidden once the text is edited, until the app passes a new list |
| `DiagnoseDelay` | `int` | `250` | Milliseconds after the last key before `Diagnose` runs |
| `OnDiagnosed` | `EventCallback<IReadOnlyList<MokaCodeDiagnostic>>` | -- | The problems after `Diagnose` has run on what was typed |
| `Rows` | `int` | `12` | How many lines show before it scrolls |
| `ShowLineNumbers` | `bool` | `true` | Whether line numbers run down the left edge |
| `ShowProblems` | `bool` | `true` | Whether the problems are listed under the editor |
| `ProblemsLabel` | `string` | `"Problems"` | The list's title |
| `CountText` | `Func<MokaCodeSeverity, int, string>?` | -- | Writes a count in the list's head. Null writes "2 errors" |
| `LocationText` | `Func<MokaCodeDiagnostic, string>?` | -- | Writes where a problem is. Null writes "Ln 3, Col 5" |

`Problems` reads the problems showing now, errors first, and `HasErrors` whether any is an error. `FocusAsync()` moves focus into the editor.

## Coloring

A `MokaCodeSpan` is a start index into the whole text, a length and a `MokaCodeTokenKind`: `Heading`, `Keyword`, `Key`, `Value`, `Literal`, `Number`, `Constant`, `Comment`, `Punctuation` or `Plain`. The editor picks each kind's color from the theme's tokens, so it follows the preset and the light or dark scheme. Spans can come in any order; where two overlap the earlier one wins, and anything past the end of the text is cut off.

```razor
<MokaCodeEditor @bind-Value="_ini" Label="settings.ini" Highlighter="Ini" />

@code {
    private string _ini = "[server]\nport = 8080";

    private static IEnumerable<MokaCodeSpan> Ini(string text)
    {
        int offset = 0;
        foreach (string line in text.Split('\n'))
        {
            int equals = line.IndexOf('=');
            if (line.StartsWith('['))
            {
                yield return new MokaCodeSpan(offset, line.Length, MokaCodeTokenKind.Heading);
            }
            else if (equals > 0)
            {
                yield return new MokaCodeSpan(offset, equals, MokaCodeTokenKind.Key);
                yield return new MokaCodeSpan(offset + equals + 1, line.Length - equals - 1, MokaCodeTokenKind.Value);
            }

            offset += line.Length + 1;
        }
    }
}
```

## Problems

A `MokaCodeDiagnostic` has a `Severity` (`Error`, `Warning`, `Info`), a `Message`, and where it is: a 1-based `Line` and `Column` and a `Length`, plus an optional short `Code`. Column 0 marks the whole line without its indent, length 0 runs to the end of the line, and line 0 is a problem with the whole text. Each one is underlined in the text, its line number takes its color, and the list under the editor names it. Clicking it selects the text it covers and scrolls it into view.

```razor
<MokaCodeEditor @bind-Value="_ini" Highlighter="Ini" Diagnose="Check" />

@code {
    private static IReadOnlyList<MokaCodeDiagnostic> Check(string text) =>
        [.. text.Split('\n')
            .Select((line, i) => (line, number: i + 1))
            .Where(l => l.line.Length > 0 && !l.line.StartsWith('[') && !l.line.Contains('='))
            .Select(l => new MokaCodeDiagnostic(MokaCodeSeverity.Error, "A setting needs a value.", l.number))];
}
```

## How it works

The textarea sits over a colored copy of its text and its own text is transparent, with only the caret and the selection drawn. Its module keeps the copy, the underlines and the line numbers scrolled with it. On a static page, where no module runs, the textarea shows its own text and the copy stays hidden, and it posts under the bound field's name like any Moka input.

## Screen readers

The textarea is the field: the label names it, `aria-invalid` is set while an error shows, and `aria-describedby` points at the problems list, which is a list of buttons. The colored copy, the underlines and the line numbers are hidden from screen readers. Tab moves focus out of the editor as in any text area; it doesn't insert a tab.
