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
  └─ MokaComponentBase          — JS interop, CSS composition, disposal, render optimization
       └─ MokaVisualComponentBase  — Size, Color, Variant, Disabled, Margin, Padding, Rounded

InputBase<TValue> (Blazor)
  └─ MokaInputBase<TValue>      — mirrors MokaComponentBase for form inputs
       └─ MokaVisualInputBase<TValue>  — mirrors MokaVisualComponentBase for form inputs
```

---

## MokaComponentBase

`MokaComponentBase` is the root for all non-input components. It provides:

- **Parameter change tracking** — `ShouldRender()` returns `false` unless parameters have
  changed since the last render. Components with internal mutable state call `ForceRender()`
  to bypass this guard.
- **CSS composition** — `RootClass` (abstract), `CssClass` (virtual), and `CssStyle`
  (virtual) are the three hooks every component implements.
- **Safe JS interop** — `SafeJsInvokeAsync` and `SafeJsInvokeVoidAsync` silently absorb
  `JSDisconnectedException` and prerendering `InvalidOperationException`.
- **Lazy JS modules** — `GetJsModuleAsync(modulePath)` imports a collocated `.razor.js`
  file once and caches the module reference for the component's lifetime.
- **Async disposal** — `DisposeAsync()` is idempotent. Override `DisposeAsyncCore()` to
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
| `SizeValue` | `string?` | — | Custom CSS size, overrides `Size`. |
| `Color` | `MokaColor?` | — | Semantic color from the theme palette. |
| `Variant` | `MokaVariant` | `Filled` | Visual style variant. |
| `Disabled` | `bool` | `false` | Disabled state. |
| `Margin` | `MokaSpacingScale?` | — | Margin from the spacing scale. |
| `MarginValue` | `string?` | — | Custom CSS margin, overrides `Margin`. |
| `Padding` | `MokaSpacingScale?` | — | Padding from the spacing scale. |
| `PaddingValue` | `string?` | — | Custom CSS padding, overrides `Padding`. |
| `Rounded` | `MokaRounding?` | — | Border radius from the rounding scale. |
| `RoundedValue` | `string?` | — | Custom CSS border-radius, overrides `Rounded`. |

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
