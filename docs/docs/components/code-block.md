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

- The copy button uses the same clipboard code as `MokaCopyButton`: the Clipboard API in a secure context (HTTPS or `localhost`), a hidden text area elsewhere. The code lives in Core's `moka-drag.js`, which loads on the first click.
- A successful copy shows "Copied!" for two seconds. Another copy within that time starts the two seconds again.
- If the copy fails, the button stays as it was. No error is shown or raised, and the same goes for a Blazor Server connection lost mid-copy and a script that fails to load.
