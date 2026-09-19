---
title: Diff Viewer
description: Monospace line diff with added and removed lines, markers and optional line numbers.
order: 65
---

# Diff Viewer

`MokaDiffViewer` shows a line-by-line diff in a monospace block. Give it `OldText` and `NewText` and it computes the diff itself, or pass precomputed `Lines`. Added lines are tinted with the success color and marked `+`, removed lines with the error color and marked `-`. For a single piece of code without change markers, use `MokaCodeBlock`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `OldText` | `string?` | -- | Original text. Used when `Lines` is `null` |
| `NewText` | `string?` | -- | Revised text. Used when `Lines` is `null` |
| `Lines` | `IReadOnlyList<MokaDiffLine>?` | -- | Precomputed diff lines. Takes precedence over `OldText` and `NewText` |
| `ShowLineNumbers` | `bool` | `false` | Shows the old and new line number gutters |
| `ShowMarkers` | `bool` | `true` | Shows the `+` and `-` marker gutter |
| `MaxHeight` | `string?` | -- | Any CSS length. Taller content scrolls vertically with the thin scrollbar |
| `Wrap` | `bool` | `false` | Wraps long lines. Without it, lines stay on one row and scroll horizontally |
| `AddedLabel` | `string` | `"Added"` | Word screen readers hear before each added line |
| `RemovedLabel` | `string` | `"Removed"` | Word screen readers hear before each removed line |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaDiffLine

A record: `new MokaDiffLine(kind, text)`.

| Name | Type | Description |
|------|------|-------------|
| `Kind` | `MokaDiffLineKind` | `Unchanged`, `Added` or `Removed` |
| `Text` | `string` | Line content without the trailing newline |

## Basic

```blazor-preview
<MokaDiffViewer OldText="@_old" NewText="@_new" Style="width:100%;max-width:520px" />

@code {
    private readonly string _old =
        "server:\n" +
        "  port: 8080\n" +
        "  logging: info\n" +
        "cache:\n" +
        "  enabled: true";

    private readonly string _new =
        "server:\n" +
        "  port: 8080\n" +
        "  logging: debug\n" +
        "  timeout: 30\n" +
        "cache:\n" +
        "  enabled: false";
}
```

## Line Numbers

The two gutters count separately: a removed line advances only the old number, an added line only the new one.

```blazor-preview
<MokaDiffViewer OldText="@_old" NewText="@_new" ShowLineNumbers Style="width:100%;max-width:520px" />

@code {
    private readonly string _old =
        "public int Total(int[] items)\n" +
        "{\n" +
        "    var sum = 0;\n" +
        "    foreach (var item in items) sum += item;\n" +
        "    return sum;\n" +
        "}";

    private readonly string _new =
        "public int Total(IEnumerable<int> items)\n" +
        "{\n" +
        "    return items.Sum();\n" +
        "}";
}
```

## Precomputed Lines

When the diff comes from somewhere else, such as a server or a version control tool, pass it as `Lines`.

```blazor-preview
<MokaDiffViewer Lines="_lines" ShowLineNumbers Style="width:100%;max-width:520px" />

@code {
    private readonly MokaDiffLine[] _lines =
    [
        new(MokaDiffLineKind.Unchanged, "dependencies:"),
        new(MokaDiffLineKind.Removed, "  serilog: 3.1.1"),
        new(MokaDiffLineKind.Added, "  serilog: 4.0.0"),
        new(MokaDiffLineKind.Unchanged, "  polly: 8.4.0"),
        new(MokaDiffLineKind.Added, "  humanizer: 2.14.1")
    ];
}
```

## Scrolling and Wrapping

`MaxHeight` caps the height and `Wrap` breaks long lines instead of scrolling sideways.

```blazor-preview
<MokaDiffViewer OldText="@_old" NewText="@_new" MaxHeight="160px" Wrap Style="width:100%;max-width:520px" />

@code {
    private static string Log(int n, string pool) =>
        $"[{n:00}] GET /api/orders handled by pool {pool} in {40 + n} ms, cache hit ratio within the expected range";

    private readonly string _old = string.Join("\n", Enumerable.Range(1, 10).Select(n => Log(n, "A")));
    private readonly string _new = string.Join("\n", Enumerable.Range(1, 10).Select(n => Log(n, n % 3 == 0 ? "B" : "A")));
}
```

## Behaviour

- The diff matches whole lines with Myers' diff algorithm, the one git uses by default, and keeps as many lines unchanged as possible. Lines match only when they are identical, so case and whitespace count.
- Any line ending splits lines, so text with Windows and Unix line endings compares cleanly.
- In a changed block, removed lines come before added lines.
- When a block of only added or only removed lines could sit in more than one place, because it repeats the lines around it, it goes as far down as it can, the way git places it. A function appended after another shows as one block after the existing closing brace.
- A trailing newline counts as a final empty line. When only one side ends with a newline, the diff shows an added or removed empty line. When both do, those two lines stay matched.
- The diff is recomputed only when `OldText` or `NewText` changes.
- There is no line limit. The work grows with the size of the texts times the number of changed lines, and lines that exist on only one side do not slow the matching, so edits to files of tens of thousands of lines take milliseconds.
- The matching stops after a fixed amount of work, about a tenth of a second on a server, and shows what it has not matched yet as removed followed by added. Only texts that differ almost everywhere while sharing many repeated lines, or that move a block of thousands of lines, get that far. To show such a diff in full, compute it elsewhere and pass `Lines`.
- Empty lines keep their height.

## Accessibility

Each added or removed line starts with a visually hidden word, "Added:" or "Removed:", so screen readers say which lines changed while the tint and the `+` and `-` markers stay visual. Set `AddedLabel` and `RemovedLabel` to translate the words. They are left out when the diff text is copied. The markers and line numbers are `aria-hidden`.
