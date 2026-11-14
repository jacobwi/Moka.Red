---
title: Code Block
description: Styled code display with optional line numbers, copy-to-clipboard and syntax highlighting.
order: 49
---

# Code Block

`MokaCodeBlock` renders a code snippet with optional line numbers, a language label, a copy button and syntax highlighting. Ideal for documentation pages, code samples, and technical dashboards.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Code` | `string` | **required** | The source code to display |
| `Language` | `string?` | `null` | Language label shown in the header (e.g. `"csharp"`, `"json"`). Only a label: it never colors the code |
| `Highlighter` | `Func<string, IEnumerable<MokaCodeSpan>>?` | `null` | Colors the code. One of `MokaCodeHighlighters`, or your own. Null draws plain text. See [Syntax Highlighting](#syntax-highlighting) |
| `ShowLineNumbers` | `bool` | `true` | Show line number gutter |
| `ShowCopyButton` | `bool` | `true` | Show a copy-to-clipboard button |
| `MaxHeight` | `string?` | `null` | CSS max-height for scrollable overflow (e.g. `"300px"`) |
| `Wrap` | `bool` | `false` | Wrap long lines instead of horizontal scrolling |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic C# Code

```blazor-preview
<MokaCodeBlock Code="@_csharp" Language="csharp" />

@code {
    private string _csharp = @"public class HelloWorld
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""Hello, World!"");
    }
}";
}
```

## With Language Label

```blazor-preview
<MokaCodeBlock Code="@_json" Language="json" />

@code {
    private string _json = @"{
  ""name"": ""Moka.Red"",
  ""version"": ""0.1.2"",
  ""description"": ""Blazor UI component library""
}";
}
```

## No Line Numbers

```blazor-preview
<MokaCodeBlock Code="@_cmd" ShowLineNumbers="false" />

@code {
    private string _cmd = "dotnet add package Moka.Red";
}
```

## Max Height with Scroll

```blazor-preview
<MokaCodeBlock Code="@_long" Language="csharp" MaxHeight="150px" />

@code {
    private string _long = string.Join("\n", Enumerable.Range(1, 30).Select(i => $"// Line {i}: some code here"));
}
```

## Wrapped Lines

```blazor-preview
<MokaCodeBlock Code="@_wide" Language="text" Wrap="true" />

@code {
    private string _wide = "This is a very long line of text that would normally scroll horizontally, but with Wrap enabled it will break to fit the container width instead of overflowing.";
}
```

## Syntax Highlighting

`Highlighter` colors the code. `MokaCodeHighlighters`, in `Moka.Red.Core.Code`, has one for each language below. Each is a field, so the same delegate comes back every render and the block colors the code only when the code or the highlighter changes. `Language` stays a label and never picks a highlighter, so a block without `Highlighter` draws plain text, as it always has.

```blazor-preview
<MokaCodeBlock Code="@_razor" Language="razor" Highlighter="MokaCodeHighlighters.Razor" />

@code {
    private string _razor = @"@page ""/counter""
@inject ILogger<Counter> Logger

<h1 class=""title"">Count: @count</h1>
<MokaButton OnClick=""Increment"">Add one</MokaButton>

@code {
    private int count;

    // Counts the clicks.
    private void Increment() => count++;
}";
}
```

```blazor-preview
<MokaCodeBlock Code="@_json" Language="json" Highlighter="MokaCodeHighlighters.Json" ShowLineNumbers="false" />

@code {
    private string _json = @"{
  ""name"": ""Moka.Red"",
  ""preview"": true,
  ""packages"": 12
}";
}
```

| Field | Names `For` takes | What it colors |
|-------|-------------------|----------------|
| `Razor` | `razor`, `cshtml` | HTML with Razor's transitions: `@code`, `@if`, `@foreach` and the other blocks with their C#, `@expressions`, `@(...)`, directives such as `@page` and `@inject`, `@* comments *@`, component tags, directive attributes such as `@onclick`, and the markup inside code blocks |
| `CSharp` | `csharp`, `cs`, `c#` | Keywords, strings of every form with interpolation holes colored as C#, characters, numbers, comments, preprocessor lines and attributes. Types are a guess from where a name stands: after `new`, in type arguments, in a declaration such as `Foo bar`, and in a base list |
| `Html` | `html` | Tags, attributes and their values, comments, the doctype, entities, and CSS inside `<style>` |
| `Xml` | `xml`, `xaml` | Tags, attributes and their values, comments, CDATA, processing instructions and entities |
| `Css` | `css` | Selectors, at-rules, properties, custom properties, numbers with their units, colors, strings and comments |
| `Json` | `json` | Keys, strings, numbers, `true`, `false` and `null`, and comments |
| `Shell` | `bash`, `sh`, `shell`, `powershell`, `ps1`, `pwsh` | The command starting each step, options, variables, strings, here-documents and here-strings, comments and keywords, for bash and PowerShell alike |

`MokaCodeHighlighters.For(name)` gives the highlighter for one of those names, in any case, or null for any other, so a block can follow a language chosen at run time:

```razor
<MokaCodeBlock Code="@snippet.Code" Language="@snippet.Language"
               Highlighter="MokaCodeHighlighters.For(snippet.Language)" />
```

Each highlighter reads the text once, in linear time, so a long listing or a stray unclosed string or comment never slows it down. Anything it doesn't recognize stays in the text color.

### Your own highlighter

A highlighter is a `Func<string, IEnumerable<MokaCodeSpan>>`: given the whole code, it returns the stretches to color, each a start index into the code, a length and a `MokaCodeTokenKind`. The spans can come in any order: the block sorts them, clips them to the code and, where two overlap, keeps the one that starts first. Declare it as a static method or a field, not a lambda written in the markup, which is a new delegate every render and colors the code again each time.

```razor
<MokaCodeBlock Code="@_todo" Highlighter="Todos" />

@code {
    private string _todo = "TODO: write the docs\nDone: the code";

    private static IEnumerable<MokaCodeSpan> Todos(string code)
    {
        int at = code.IndexOf("TODO", StringComparison.Ordinal);
        if (at >= 0)
        {
            yield return new MokaCodeSpan(at, 4, MokaCodeTokenKind.Keyword);
        }
    }
}
```

### Colors

Every kind takes its color from a theme token, so the code follows the preset and the light or dark scheme. Until a theme sets the tokens, each falls back to one of the palette's colors.

| `MokaCodeTokenKind` | Token |
|---------------------|-------|
| `Tag` | `--moka-code-tag` |
| `Attribute`, `Key` | `--moka-code-attribute` |
| `String`, `Literal` | `--moka-code-string` |
| `Keyword`, `Heading` (semibold) | `--moka-code-keyword` |
| `Type` | `--moka-code-type` |
| `Number`, `Constant` | `--moka-code-number` |
| `Comment` (italic) | `--moka-code-comment` |
| `Punctuation` | `--moka-code-punctuation` |
| `Directive` | `--moka-code-directive` |
| `Value`, `Plain` and the uncolored text | `--moka-code-text` |

A span that runs over a line break, such as a block comment, is cut at each break, so every line holds its own spans. The copy button still copies the plain code: it reads the lines' text, which the colored spans don't change.

## Behaviour

- The copy happens in the browser, inside the click (or the click Enter and Space raise), the way `MokaCopyButton` copies: it joins the lines already on the page, without their numbers, so nothing is sent from .NET and nothing waits on a round trip. Safari, iOS and Firefox refused the copy up to 0.4.2 on Blazor Server, where it came a round trip after the click.
- It uses the Clipboard API in a secure context (HTTPS or `localhost`) and the copy command elsewhere, with focus left on the button.
- A successful copy shows "Copied!" for two seconds. Another copy within that time starts the two seconds again. The lines don't redraw for it, however long the code.
- The button's label is its text, "Copy" or "Copied!", so it carries no `title`: up to 0.1.19 a `title` repeated the label and brought the browser's own hint with it.
- If the browser refuses the copy, the button stays as it was. No error is shown or raised.
