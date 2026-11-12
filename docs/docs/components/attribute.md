---
title: Attribute
description: A small labelled value, such as a status, a version or a filter you can pick or remove.
order: 161
---

# Attribute

`MokaAttribute` is a dense pill for a value: a status, a version, a `Label: value` pair, or a filter someone can click, select or remove. With nothing to click it is plain text in a box. `MokaTag` is the simpler label, and `MokaChip` the filter chip in the button style.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | The value |
| `Label` | `string?` | -- | A dimmer prefix for key-value display, written before the value with a colon |
| `StartIcon` | `MokaIconDefinition?` | -- | Icon before the content |
| `EndIcon` | `MokaIconDefinition?` | -- | Icon after the content. Not drawn when `Dismissible` |
| `Avatar` | `string?` | -- | Address of a small round picture before the content |
| `Clickable` | `bool` | `false` | Makes the attribute a button with a pointer cursor |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Fires when a clickable attribute is clicked |
| `Selectable` | `bool` | `false` | Makes the attribute a toggle button, with a check while selected and `aria-pressed` |
| `Selected` | `bool` | `false` | Whether it is selected. Two-way bindable |
| `SelectedChanged` | `EventCallback<bool>` | -- | Raised when a click changes `Selected` |
| `Dismissible` | `bool` | `false` | Shows a remove button at the end, a tab stop of its own |
| `OnDismiss` | `EventCallback` | -- | Fires when the remove button is clicked, or pressed with Enter or Space |
| `DismissLabel` | `string?` | -- | The remove button's name, for its `aria-label` and tooltip. Unset, it is "Remove" and the `Label`, or "Remove" with no label |
| `Pill` | `bool` | `true` | Fully rounded. `false` uses the medium radius |
| `Variant` | `MokaVariant` | `Filled` | `Filled`, `Outlined`, `Soft` or `Text` |
| `Color` | `MokaColor?` | `Surface` | What it means. Unset is neutral |
| `Size` | `MokaSize` | `Md` | `Xs`, `Sm`, `Md`, `Lg` |
| `Disabled` | `bool` | `false` | Dims the attribute and disables its buttons |
| `Class` | `string?` | -- | Additional CSS classes, on the outermost element |
| `Style` | `string?` | -- | Additional inline styles, on the outermost element |

## Buttons and Plain Attributes

What an attribute renders follows what it does:

- With nothing to do it is a `span`: no tab stop and no pointer.
- `Clickable` or `Selectable` makes it a `<button type="button">`. A selectable one is a toggle button and carries `aria-pressed` as `"true"` or `"false"`, so screen readers say whether it's on. A clickable one that isn't selectable has no `aria-pressed`.
- `Dismissible` puts the remove button beside the attribute's own button, never inside it, since a button can't hold another: the attribute is a `span` holding its button (when it is clickable or selectable) and the remove button. A click anywhere on it but the cross still reaches its button, and the focus ring goes round the whole attribute.

`Id`, `Class`, `Style` and any other attribute you pass go on the outermost element: the button, or the `span` of a dismissible or plain attribute.

Up to 0.4.2 a dismissible attribute was a button holding its remove button, which isn't valid HTML: a page rendered on the server came out with the remove button outside the attribute, a dismissible attribute with nothing else to do was a button that did nothing, and the keyboard couldn't reach the remove button at all.

## Variants

```blazor-preview
<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaAttribute Variant="MokaVariant.Filled" Color="MokaColor.Primary">Filled</MokaAttribute>
    <MokaAttribute Variant="MokaVariant.Outlined" Color="MokaColor.Primary">Outlined</MokaAttribute>
    <MokaAttribute Variant="MokaVariant.Soft" Color="MokaColor.Primary">Soft</MokaAttribute>
    <MokaAttribute Variant="MokaVariant.Text" Color="MokaColor.Primary">Text</MokaAttribute>
</div>
```

## Colors

```blazor-preview
<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaAttribute Variant="MokaVariant.Soft">Neutral</MokaAttribute>
    <MokaAttribute Variant="MokaVariant.Soft" Color="MokaColor.Primary">Primary</MokaAttribute>
    <MokaAttribute Variant="MokaVariant.Soft" Color="MokaColor.Success">Success</MokaAttribute>
    <MokaAttribute Variant="MokaVariant.Soft" Color="MokaColor.Warning">Warning</MokaAttribute>
    <MokaAttribute Variant="MokaVariant.Soft" Color="MokaColor.Error">Error</MokaAttribute>
    <MokaAttribute Variant="MokaVariant.Soft" Color="MokaColor.Info">Info</MokaAttribute>
</div>
```

## Sizes

```blazor-preview
<div style="display:flex;gap:8px;align-items:center">
    <MokaAttribute Size="MokaSize.Xs" Variant="MokaVariant.Soft" Color="MokaColor.Primary">XS</MokaAttribute>
    <MokaAttribute Size="MokaSize.Sm" Variant="MokaVariant.Soft" Color="MokaColor.Primary">SM</MokaAttribute>
    <MokaAttribute Size="MokaSize.Md" Variant="MokaVariant.Soft" Color="MokaColor.Primary">MD</MokaAttribute>
    <MokaAttribute Size="MokaSize.Lg" Variant="MokaVariant.Soft" Color="MokaColor.Primary">LG</MokaAttribute>
</div>
```

## Key-Value Pairs and Icons

```blazor-preview
<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaAttribute Label="Status" Variant="MokaVariant.Soft" Color="MokaColor.Success">Active</MokaAttribute>
    <MokaAttribute Label="Version" Variant="MokaVariant.Outlined">2.1.0</MokaAttribute>
    <MokaAttribute StartIcon="MokaIcons.Status.CheckCircle" Variant="MokaVariant.Soft" Color="MokaColor.Success">Verified</MokaAttribute>
    <MokaAttribute StartIcon="MokaIcons.File.Code" Variant="MokaVariant.Outlined">C#</MokaAttribute>
</div>
```

## Selectable

A selectable attribute toggles on click, shows a check while selected, and says whether it is pressed through `aria-pressed`. `Selected` only takes a new value from the parent when it differs from the last one passed, so bind it with `@bind-Selected` to keep the two in step.

```blazor-preview
@code {
    bool _active = true, _pending, _failed;
}
<div style="display:flex;gap:8px">
    <MokaAttribute Selectable @bind-Selected="_active" Variant="MokaVariant.Outlined" Color="MokaColor.Success">Active</MokaAttribute>
    <MokaAttribute Selectable @bind-Selected="_pending" Variant="MokaVariant.Outlined" Color="MokaColor.Warning">Pending</MokaAttribute>
    <MokaAttribute Selectable @bind-Selected="_failed" Variant="MokaVariant.Outlined" Color="MokaColor.Error">Failed</MokaAttribute>
</div>
```

## Dismissible

The remove button shows only a cross, so it is named by `aria-label` and by a `MokaTooltip` below it. The attribute can't read its content, which is markup, so the name is "Remove" and the `Label` when there is one ("Remove Status"), else "Remove". Set `DismissLabel` to name it after the value, so a row of them isn't a row of buttons all called "Remove", or to say it in another language.

The remove button is a tab stop of its own, so Tab reaches it after the attribute's own button, and Enter or Space removes the attribute. A disabled attribute disables it too. Removing an attribute takes the focused button away with it, so put focus somewhere sensible in `OnDismiss` when the attributes are in a list.

```razor
@foreach (string tag in _tags)
{
    <MokaAttribute @key="tag" Dismissible DismissLabel="@($"Remove {tag}")"
                   OnDismiss="() => _tags.Remove(tag)" Variant="MokaVariant.Soft">@tag</MokaAttribute>
}

@code {
    private readonly List<string> _tags = ["Blazor", "C#", "CSS"];
}
```

A dismissible attribute can still be clicked or selected. Its own button takes the click anywhere but the cross:

```razor
<MokaAttribute Label="Status" Selectable @bind-Selected="_pinned" Dismissible OnDismiss="ClearStatus"
               Variant="MokaVariant.Outlined" Color="MokaColor.Primary">Active</MokaAttribute>
```

## Non-Pill

```blazor-preview
<div style="display:flex;gap:8px">
    <MokaAttribute Pill="false" Variant="MokaVariant.Filled" Color="MokaColor.Primary">Tag</MokaAttribute>
    <MokaAttribute Pill="false" Variant="MokaVariant.Outlined" Color="MokaColor.Secondary">Badge</MokaAttribute>
    <MokaAttribute Pill="false" Variant="MokaVariant.Soft" Color="MokaColor.Info">Label</MokaAttribute>
</div>
```
