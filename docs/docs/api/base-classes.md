---
title: Base Classes
description: Component base class hierarchy, CSS composition utilities, and disposal patterns used throughout Moka.Red.
---

# Base Classes

All Moka.Red components derive from a two-level hierarchy rooted in Blazor's own base
classes. Understanding these bases makes it straightforward to build new components or
override existing behavior.

## Class hierarchy

```
ComponentBase (Blazor)
  └─ MokaComponentBase - JS interop, CSS composition, disposal, render optimization
       └─ MokaVisualComponentBase - Size, Color, Variant, Disabled, Margin, Padding, Rounded

InputBase<TValue> (Blazor)
  └─ MokaInputBase<TValue> - mirrors MokaComponentBase for form inputs
       └─ MokaVisualInputBase<TValue> - mirrors MokaVisualComponentBase for form inputs
```

---

## MokaComponentBase

`MokaComponentBase` is the root for all non-input components. It provides:

- **Parameter change tracking** - `ShouldRender()` returns `false` unless parameters have
  changed since the last render. Components with internal mutable state call `ForceRender()`
  to bypass this guard.
- **CSS composition** - `RootClass` (abstract), `CssClass` (virtual), and `CssStyle`
  (virtual) are the three hooks every component implements.
- **Safe JS interop** - `SafeJsInvokeAsync` and `SafeJsInvokeVoidAsync` silently absorb
  `JSDisconnectedException` and prerendering `InvalidOperationException`.
- **Lazy JS modules** - `GetJsModuleAsync(modulePath)` imports a collocated `.razor.js`
  file once and caches the module reference for the component's lifetime.
- **Async disposal** - `DisposeAsync()` is idempotent. Override `DisposeAsyncCore()` to
  release component-specific resources; call `base.DisposeAsyncCore()` at the end.

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `Class` | `string?` | Extra CSS classes merged onto the root element. |
| `Style` | `string?` | Extra inline styles merged onto the root element. |
| `Id` | `string?` | HTML `id` attribute. |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | Splatted onto the root element (`CaptureUnmatchedValues`). |
| `Theme` | `MokaTheme?` | Cascaded from `MokaThemeProvider` (read-only). |

### Abstract contract

Every component must declare:

```csharp
protected abstract string RootClass { get; }
```

And typically overrides:

```csharp
protected override string CssClass => new CssBuilder(RootClass)
    .AddClass("moka-button--filled", Variant == MokaVariant.Filled)
    .AddClass(Class)
    .Build();

protected override string? CssStyle => new StyleBuilder()
    .AddStyle("width", WidthValue)
    .AddStyle(Style)
    .Build();
```

---

## MokaVisualComponentBase

Extends `MokaComponentBase` with the visual appearance parameters shared by most
rendered components.

### Additional parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Size` | `MokaSize` | `Md` | Component size from the standard scale. |
| `SizeValue` | `string?` | - | Custom CSS size, overrides `Size`. |
| `Color` | `MokaColor?` | - | Semantic color from the theme palette. |
| `Variant` | `MokaVariant` | `Filled` | Visual style variant. |
| `Disabled` | `bool` | `false` | Disabled state. |
| `Margin` | `MokaSpacingScale?` | - | Margin from the spacing scale. |
| `MarginValue` | `string?` | - | Custom CSS margin, overrides `Margin`. |
| `Padding` | `MokaSpacingScale?` | - | Padding from the spacing scale. |
| `PaddingValue` | `string?` | - | Custom CSS padding, overrides `Padding`. |
| `Rounded` | `MokaRounding?` | - | Border radius from the rounding scale. |
| `RoundedValue` | `string?` | - | Custom CSS border-radius, overrides `Rounded`. |

### Protected helpers

| Member | Returns | Description |
|--------|---------|-------------|
| `ResolvedSize` | `string` | `SizeValue` or the px mapping of `Size`. |
| `ResolvedMargin` | `string?` | `MarginValue` or the CSS var for `Margin`. |
| `ResolvedPadding` | `string?` | `PaddingValue` or the CSS var for `Padding`. |
| `ResolvedRounding` | `string?` | `RoundedValue` or the CSS var for `Rounded`. |
| `SizeToKebab(size)` | `string` | e.g., `MokaSize.Lg` → `"lg"`. |
| `ColorToKebab(color)` | `string` | e.g., `MokaColor.Error` → `"error"`. |
| `VariantToKebab(variant)` | `string` | e.g., `MokaVariant.Outlined` → `"outlined"`. |

---

## MokaInputBase\<TValue\> and MokaVisualInputBase\<TValue\>

Form input components extend Blazor's `InputBase<TValue>` instead of `ComponentBase`,
so they integrate with `EditForm` validation out of the box. `MokaInputBase<TValue>` and
`MokaVisualInputBase<TValue>` mirror the non-input hierarchy exactly, adding the same
JS interop, CSS composition, and disposal support.

`ComponentCssClass` (the CSS composition property on inputs) merges the component's
`RootClass`, Blazor's built-in validation classes (`valid`, `invalid`, `modified`), and
the user's `Class` parameter.

## MokaSegmentedInputBase

The OTP, PIN, IP and MAC inputs render several boxes for one value, so they extend
`MokaVisualComponentBase` rather than `InputBase<TValue>`, which models a single input
element. They still take part in `EditForm` validation. `@bind-Value` supplies
`ValueExpression`, and the base reads the cascaded `EditContext`, tells it when the value
changes, and gives the component `ValidationMessages`, `HasError`, `ResolvedErrorText` and
`ValidationCssClass`. A binding whose getter is not a field or property stays out of
validation instead of throwing.

Its `CssStyle` puts the margin and the padding on the root, around the label, the boxes and
the message. Each box draws its own border, so the radius from `Rounded` is in
`SegmentStyle` instead. A segmented input of your own puts `style="@SegmentStyle"` on
every box.

---

## CssBuilder

`CssBuilder` composes space-separated CSS class strings. Null and whitespace values are
skipped automatically.

```csharp
string classes = new CssBuilder("moka-chip")
    .AddClass("moka-chip--filled",  Variant == MokaVariant.Filled)
    .AddClass("moka-chip--primary", Color == MokaColor.Primary)
    .AddClass("moka-chip--disabled", Disabled)
    .AddClass(Class)      // user-supplied, always last
    .Build();
// → "moka-chip moka-chip--filled moka-chip--primary"
```

The conditional overload accepts either a `bool` or a `Func<bool>` predicate.

## StyleBuilder

`StyleBuilder` composes semicolon-separated inline style strings.

```csharp
string? style = new StyleBuilder()
    .AddStyle("width",  WidthValue)                             // skipped when null
    .AddStyle("color",  "red",       Color == MokaColor.Error) // conditional
    .AddStyle("margin", ResolvedMargin)
    .AddStyle(Style)    // user-supplied passthrough
    .Build();
```

`AddStyle(property, value)` checks each value with `CssValues.IsSelfContained` and leaves out one
that could end its own declaration or run into the next: a `;` outside quotes and brackets, a brace,
a string, bracket or comment left open, or a trailing `\`. So a parameter cannot add declarations
of its own, and a semicolon inside quotes or a `url()`, as in a data URI, still works. The raw
`AddStyle(style)` overload is written as it is, because it carries the consumer's own `Style` text.
The check does not say whether a value is valid CSS: check a color with `CssValues.IsColor` before
you add it when a parameter should be a color.

## CssValues

`CssValues` checks and escapes strings a component writes where Blazor does not encode them: SVG
built as a string (rendered as a `MarkupString` or put in a data URI), a CSS `url()`, or a
`<style>` block. In those places a quote, a parenthesis or a semicolon from a parameter ends the
value it was meant to stay inside, and whatever follows becomes markup or CSS. `MokaBarcode`,
`MokaQRCode`, `MokaIdenticon`, `MokaWatermark`, `MokaGridBackground`, `MokaParallax`, `MokaTheme`
and the responsive styles of `MokaGrid` and `MokaFlexbox` all use it.

| Member | Returns | Description |
|--------|---------|-------------|
| `IsColor(value)` | `bool` | A hex color, a keyword such as `red` or `currentColor`, or one color function: `rgb()`, `hsl()`, `hwb()`, `lab()`, `lch()`, `oklab()`, `oklch()`, `color()`, `color-mix()`, `light-dark()` or `var()`. Arguments may nest color functions. `url()` and other functions that fetch something are not colors. |
| `IsLength(value)` | `bool` | A non-negative number with an optional unit: px, em, rem, %, pt, pc, ex, ch, cm, mm, in, q, vw, vh, vmin or vmax. `calc()` is not a length here. |
| `IsLengthList(value)` | `bool` | One or more lengths separated by spaces or commas, the form of `stroke-dasharray`. |
| `IsSafe(value)` | `bool` | Holds none of `; { } < > \`, so it cannot end the declaration, the rule or the `<style>` element it is written into. It says nothing about whether the value is valid CSS. |
| `IsSelfContained(value)` | `bool` | Every string, bracket, `url()` and comment it opens is closed, and it holds no `;` outside them, no brace and no trailing `\`, so as a declaration's value in a `style` attribute it cannot end that declaration or run into the next one. A `;` inside quotes or brackets is allowed. `StyleBuilder` uses it. Not enough for a `<style>` element, where `</style>` ends the element even inside a string: use `IsSafe` there. |
| `ColorOrDefault(value, fallback)` | `string` | The trimmed value when `IsColor` accepts it, otherwise `fallback`. |
| `LengthOrDefault(value, fallback)` | `string` | The trimmed value when `IsLength` accepts it, otherwise `fallback`. |
| `TryParsePixels(value, out pixels)` | `bool` | Reads an absolute length (px, pt, pc, in, cm, mm, q, em, rem, or no unit) as pixels. em and rem count as 16px. |
| `Url(url)` | `string` | `url("...")` with quotes and backslashes escaped and control characters written as CSS escapes. The URL itself is not checked. |
| `SvgDataUrl(svg)` | `string` | `url("data:image/svg+xml,...")` with the whole document percent-encoded. |
| `EscapeXml(value)` | `string` | Escapes text for an XML or SVG attribute or text node, and drops characters XML does not allow. |

A value that passes `IsColor`, `IsLength` or `IsLengthList` holds no quote, angle bracket,
ampersand, semicolon, brace or backslash. Escape it anyway when you build markup by hand, so the
markup stays safe if the check ever changes.

```csharp
// A striped tile drawn as SVG: the color is checked and escaped, and the document is encoded for
// the data URI. The border color goes straight into a declaration, so the check is enough.
string stroke = CssValues.EscapeXml(CssValues.ColorOrDefault(StripeColor, "#808080"));
string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='8' height='8'>"
    + $"<path d='M0 8L8 0' stroke='{stroke}'/></svg>";

string? style = new StyleBuilder()
    .AddStyle("background-image", CssValues.SvgDataUrl(svg))
    .AddStyle("border-color", CssValues.IsColor(BorderColor) ? BorderColor.Trim() : null)
    .Build();
```

## UrlValues

`UrlValues` checks a URL before a component renders it as a link target. Blazor writes an `href` as
it is given, and a `javascript:` URL there runs script in the page when the link is followed.
`MokaLink`, `MokaButton`, `MokaCard`, `MokaListItem`, `MokaBreadcrumbItem`, `MokaMenuItem`,
`MokaBlockquote` (`CitationHref`) and the command palette's `MokaCommand.Href` all use it.

| Member | Returns | Description |
|--------|---------|-------------|
| `HasBlockedScheme(href)` | `bool` | True for a `javascript:`, `vbscript:` or `data:` URL, read the way a browser reads it: leading spaces and control characters are skipped, tabs and line breaks inside the scheme are ignored, and case does not matter. |
| `SafeHref(href)` | `string?` | `href` as given, or `null` when `HasBlockedScheme` blocks it, so the link renders with no `href`. |

Relative URLs, fragments, `http`, `https`, `mailto`, `tel` and every other scheme pass unchanged.

```razor
<a href="@UrlValues.SafeHref(Href)">@ChildContent</a>
```

---

## IAsyncDisposable pattern

Override `DisposeAsyncCore` to release resources:

```csharp
protected override async ValueTask DisposeAsyncCore()
{
    if (_subscription is not null)
    {
        await _subscription.DisposeAsync();
        _subscription = null;
    }

    await base.DisposeAsyncCore();  // always call base
}
```

`DisposeAsync()` on `MokaComponentBase` is idempotent (guarded by a `_disposed` flag) and
automatically disposes any JS module loaded via `GetJsModuleAsync`.
