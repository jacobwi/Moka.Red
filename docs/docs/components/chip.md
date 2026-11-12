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
| `Closable` | `bool` | `false` | Shows a remove button after the content, a tab stop of its own named "Remove" and the chip's `Text` |
| `OnClose` | `EventCallback` | -- | Fires when the remove button is clicked, or pressed with Enter or Space |
| `Selected` | `bool` | `false` | Whether the chip is selected (two-way bindable) |
| `SelectedChanged` | `EventCallback<bool>` | -- | Callback when selected state changes |
| `Controlled` | `bool` | `false` | After raising its change callback, the control shows what the parent passes back, so a refused change reverts. See [Controlled](#controlled) |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click event callback. With it, or with `SelectedChanged`, the chip is a button |
| `Variant` | `MokaVariant` | `Soft` | `Soft` (tinted background), `Filled` (tinted border), `Outlined`, `Text` |
| `Color` | `MokaColor?` | `Surface` | Color of the text, border and background. Unset is neutral; a selected neutral chip uses the primary color |
| `Label` | `MokaLabelColor?` | -- | A color someone gave the chip: its tint behind readable ink, and a dot before the text when there's no icon or avatar. Replaces `Color`; `Variant` still shapes it |
| `Size` | `MokaSize` | `Md` | `Xs`, `Sm`, `Md`, `Lg` |
| `Disabled` | `bool` | `false` | Disables the chip |
| `Class` | `string?` | -- | Additional CSS classes, on the outermost element |
| `Style` | `string?` | -- | Additional inline styles, on the outermost element |

## Buttons and Plain Chips

What a chip renders follows what it does:

- With nothing to click (no `OnClick`, no `SelectedChanged`) it is a `span`: no tab stop, no pointer, and no hover or press feedback. Its resting look is the same as a button chip's.
- With `OnClick` or `SelectedChanged` it is a `<button type="button">`. Once the parent passes `Selected` or `SelectedChanged`, it is a toggle button and carries `aria-pressed` as `"true"` or `"false"`, so screen readers say whether it's on. A chip with only `OnClick` is an action and has no `aria-pressed`.
- `Closable` puts the remove button beside the chip's own button, never inside it, since a button can't hold another: the chip is a `span` holding its button (when it has `OnClick` or `SelectedChanged`) and the remove button. A click anywhere on it but the cross still reaches its button, and the focus ring goes round the whole chip.

`Id`, `Class`, `Style` and any other attribute you pass go on the outermost element: the button, or the `span` of a closable or plain chip.

Up to 0.4.2 every chip was a button, so a chip with nothing to do was still a tab stop, and a closable chip put its remove button inside its own button, which isn't valid HTML: a page rendered on the server came out with the remove button outside the chip, and the keyboard couldn't reach it at all.

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

## Label colors

`Label` takes a [label color](/getting-started/label-colors), for filters on things people color themselves. It replaces `Color`, and draws as on `MokaTag`: `Soft` and `Filled` draw the tint, `Outlined` a border in the label's color, `Text` the ink alone, with a dot before the text when there's no icon or avatar. A selected chip keeps the label's colors and gets a border in it, in place of the accent.

```razor
<MokaChip Text="Design" Label="MokaLabelColor.Teal" @bind-Selected="_design"/>
<MokaChip Text="Research" Label="MokaLabelColor.Purple" @bind-Selected="_research"/>
<MokaChip Text="Archive" Label="MokaLabelColor.Slate" Closable OnClose="RemoveArchive"/>
```

## Closable

The remove button shows only a cross, so it is named by `aria-label` and by a `MokaTooltip` below it: "Remove" and the chip's `Text`, such as "Remove Blazor", so a row of them isn't a row of buttons all called "Remove". A chip with only `ChildContent` names it "Remove". It is a tab stop of its own, so Tab reaches it after the chip's own button, and Enter or Space removes the chip. A disabled chip disables it too.

A tab stop rather than Delete or Backspace on the chip: those keys belong to a set of chips that moves focus with the arrow keys (a grid, in WAI-ARIA terms), which a single chip isn't, and a plain chip has no focus to hear them. A tab stop needs no script and no instructions, and works the same in every screen reader mode. Removing a chip takes the focused button away with it, so put focus somewhere sensible in `OnClose` when the chips are in a list.

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

## Controlled

By default the chip shows the user's pick at once and follows the parent only when it passes a different value, so a `SelectedChanged` handler that says no can't take the click back: the parent's `Selected` hasn't changed, so there is nothing new to follow. With `Controlled` the chip always shows `Selected`. A click raises `SelectedChanged` with the opposite value and changes nothing on its own; the parent decides by setting `Selected` or not. A parent that awaits before deciding keeps the old state on screen until it sets the value.

```razor
<MokaChip Text="Audit log" Controlled Selected="_audit" SelectedChanged="SetAudit" />

@code {
    private bool _audit;

    private void SetAudit(bool on)
    {
        if (on)
        {
            _audit = true;
        }

        // Turning it off is refused: the chip stays selected.
    }
}
```

`MokaSegmentedControl` and `MokaToggleGroup` take the same `Controlled`, with the same meaning.

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
