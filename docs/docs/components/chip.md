---
title: Chip
description: Compact interactive element for filtering, selection, or tags.
order: 26
---

# Chip

`MokaChip` is a compact interactive element used for filtering, selection, or displaying tags. It shares `MokaButton`'s variants, defaulting to `Soft` so a chip sits on a tinted background, and a selected chip shows a check icon with a colored fill and border. Supports icons, avatars, and a closable delete button.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Custom label content (overrides `Text`) |
| `Text` | `string?` | -- | Chip label text |
| `Icon` | `MokaIconDefinition?` | -- | Leading icon |
| `Avatar` | `string?` | -- | Small avatar URL as leading image |
| `Closable` | `bool` | `false` | Shows a close/remove button |
| `OnClose` | `EventCallback` | -- | Fires when the close button is clicked |
| `Selected` | `bool` | `false` | Whether the chip is selected (two-way bindable) |
| `SelectedChanged` | `EventCallback<bool>` | -- | Callback when selected state changes |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click event callback |
| `Variant` | `MokaVariant` | `Soft` | `Soft` (tinted background), `Filled` (tinted border), `Outlined`, `Text` |
| `Color` | `MokaColor?` | `Surface` | Color of the text, border and background. Unset is neutral; a selected neutral chip uses the primary color |
| `Size` | `MokaSize` | `Md` | `Xs`, `Sm`, `Md`, `Lg` |
| `Disabled` | `bool` | `false` | Disables the chip |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Chips

```blazor-preview
<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaChip Text="Default" />
    <MokaChip Text="Primary" Color="MokaColor.Primary" />
    <MokaChip Text="Success" Color="MokaColor.Success" />
    <MokaChip Text="Warning" Color="MokaColor.Warning" />
    <MokaChip Text="Error" Color="MokaColor.Error" />
</div>
```

## Variants

Chips use the same variants as `MokaButton`. `Soft` is the default and puts the chip on a tinted background; `Filled` is a tinted border that fills on hover, `Outlined` a full-color border, and `Text` has neither. A selected chip gets a colored fill and border on any variant.

```blazor-preview
<div style="display:flex;flex-direction:column;gap:8px">
    @foreach (var variant in new[] { MokaVariant.Soft, MokaVariant.Filled, MokaVariant.Outlined, MokaVariant.Text })
    {
        <div style="display:flex;gap:8px;flex-wrap:wrap">
            <MokaChip Text="@variant.ToString()" Variant="variant" />
            <MokaChip Text="Primary" Variant="variant" Color="MokaColor.Primary" />
            <MokaChip Text="Success" Variant="variant" Color="MokaColor.Success" />
            <MokaChip Text="Warning" Variant="variant" Color="MokaColor.Warning" />
            <MokaChip Text="Selected" Variant="variant" Color="MokaColor.Primary" Selected />
        </div>
    }
</div>
```

## With Icons

```blazor-preview
<div style="display:flex;gap:8px">
    <MokaChip Text="Settings" Icon="MokaIcons.Action.Settings" />
    <MokaChip Text="Home" Icon="MokaIcons.Navigation.Home" />
</div>
```

## Selectable

Toggle selection by clicking. A selected chip shows a check icon with a colored fill and border.

```blazor-preview
@code {
    bool _selected1, _selected2, _selected3;
}
<div style="display:flex;gap:8px">
    <MokaChip Text="Option A" @bind-Selected="_selected1" Color="MokaColor.Primary" />
    <MokaChip Text="Option B" @bind-Selected="_selected2" Color="MokaColor.Primary" />
    <MokaChip Text="Option C" @bind-Selected="_selected3" Color="MokaColor.Primary" />
</div>
```

## Closable

```blazor-preview
@code {
    List<string> _tags = new() { "Blazor", "C#", "CSS" };
}
<div style="display:flex;gap:8px">
    @foreach (var tag in _tags)
    {
        var t = tag;
        <MokaChip Text="@t" Closable OnClose="@(() => _tags.Remove(t))" />
    }
</div>
```

## Sizes

```blazor-preview
<div style="display:flex;gap:8px;align-items:center">
    <MokaChip Text="Xs" Size="MokaSize.Xs" />
    <MokaChip Text="Sm" Size="MokaSize.Sm" />
    <MokaChip Text="Md" Size="MokaSize.Md" />
    <MokaChip Text="Lg" Size="MokaSize.Lg" />
</div>
```

## Disabled

```blazor-preview
<MokaChip Text="Disabled" Disabled />
```
