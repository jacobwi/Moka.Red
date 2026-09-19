---
title: Spacer
description: Empty flex item that takes the free space to push its neighbours apart.
order: 114
---

# Spacer

`MokaSpacer` is an empty element that takes up the free space in a flex container. Put it between items to push them apart, for example to move a toolbar's last buttons to the far end. It works in any flex container: `MokaToolbar`, `MokaFlexbox`, or your own element with `display: flex`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Grow` | `double` | `1` | The spacer's `flex-grow`. Spacers in one container share the free space in proportion to their `Grow` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

A spacer is a `div` with only `flex-grow` set. It has no size of its own, so outside a flex container it takes no space.

## Push to the End

```blazor-preview
<div style="width:100%">
    <MokaToolbar>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Save">Save</MokaButton>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Refresh">Reload</MokaButton>
        <MokaSpacer />
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Settings">Settings</MokaButton>
    </MokaToolbar>
</div>
```

## Sharing the Space

With `Grow` 1 and 2, the second gap is twice as wide as the first. The spacers get a line here so the gaps show.

```blazor-preview
<div style="width:100%;display:flex;align-items:center;gap:var(--moka-spacing-xs);padding:var(--moka-spacing-sm);border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaTag Text="Start" />
    <MokaSpacer Style="height:2px;background:var(--moka-color-outline)" />
    <MokaTag Text="Middle" />
    <MokaSpacer Grow="2" Style="height:2px;background:var(--moka-color-outline)" />
    <MokaTag Text="End" />
</div>
```

## Vertical

In a column, a spacer pushes the items after it to the bottom.

```blazor-preview
<MokaFlexbox Direction="MokaDirection.Column" Gap="MokaSpacingScale.Xs"
             Style="width:220px;height:220px;padding:var(--moka-spacing-sm);border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <strong>Filters</strong>
    <MokaCheckbox Label="Only open issues" />
    <MokaCheckbox Label="Assigned to me" />
    <MokaSpacer />
    <MokaButton Size="MokaSize.Sm" FullWidth>Apply</MokaButton>
</MokaFlexbox>
```
