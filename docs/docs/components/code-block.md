---
title: Code Block
description: Styled code display with optional line numbers and copy-to-clipboard.
order: 49
---

# Code Block

`MokaCodeBlock` renders a syntax-highlighted code snippet with optional line numbers, a language label, and a copy button. Ideal for documentation pages, code samples, and technical dashboards.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Code` | `string` | **required** | The source code to display |
| `Language` | `string?` | `null` | Language label shown in the header (e.g. `"csharp"`, `"json"`) |
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

## Behaviour

- The copy happens in the browser, inside the click (or the click Enter and Space raise), the way `MokaCopyButton` copies: it joins the lines already on the page, without their numbers, so nothing is sent from .NET and nothing waits on a round trip. Safari, iOS and Firefox refused the copy up to 0.4.2 on Blazor Server, where it came a round trip after the click.
- It uses the Clipboard API in a secure context (HTTPS or `localhost`) and the copy command elsewhere, with focus left on the button.
- A successful copy shows "Copied!" for two seconds. Another copy within that time starts the two seconds again. The lines don't redraw for it, however long the code.
- The button's label is its text, "Copy" or "Copied!", so it carries no `title`: up to 0.1.19 a `title` repeated the label and brought the browser's own hint with it.
- If the browser refuses the copy, the button stays as it was. No error is shown or raised.
