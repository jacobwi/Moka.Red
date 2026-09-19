---
title: Bottom Sheet
description: Panel that slides up from the bottom of the screen for action lists, pickers and short forms.
order: 101
---

# Bottom Sheet

`MokaBottomSheet` slides a panel up from the bottom edge over a dimmed backdrop. It suits the action lists, pickers and short forms that phone apps open from the bottom of the screen. For a panel from another edge, see [Drawer](drawer).

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Open` | `bool` | `false` | Shows the sheet. Two-way bindable |
| `OpenChanged` | `EventCallback<bool>` | -- | Raised with `false` when the sheet closes itself |
| `Header` | `RenderFragment?` | -- | Header row above the body, with a divider under it |
| `ChildContent` | `RenderFragment?` | -- | Sheet body. Scrolls when it is taller than the sheet |
| `ShowHandle` | `bool` | `true` | Grab bar at the top of the sheet. Dragging it down closes the sheet (see [Behaviour](#behaviour)) |
| `MaxHeight` | `string` | `"70vh"` | Maximum height of the sheet |
| `FullScreen` | `bool` | `false` | Fills the viewport height, with square top corners. `MaxHeight` is ignored |
| `CloseOnBackdrop` | `bool` | `true` | A click on the backdrop closes the sheet |
| `CloseOnEscape` | `bool` | `true` | Escape closes the sheet |
| `PreventScroll` | `bool` | `true` | Locks page scrolling while the sheet is open |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the sheet panel, so it floats clear of the screen edges. See [Spacing](#spacing) |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding inside the panel, around the handle, header and body |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Radius of all four corners of the panel. Replaces the rounded top corners |
| `Class` | `string?` | -- | Additional CSS classes on the sheet panel |
| `Style` | `string?` | -- | Additional inline styles on the sheet panel |

## Action Sheet

```blazor-preview
@code {
    bool _open;
    string _last = "nothing yet";

    void Pick(string action)
    {
        _last = action;
        _open = false;
    }
}

<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;gap:8px;width:100%;min-height:360px">
    <MokaButton OnClick="@(() => _open = true)">Share report</MokaButton>
    <MokaCaption>Last action: @_last</MokaCaption>
</div>

<MokaBottomSheet @bind-Open="_open">
    <Header>Share "Q3 report.pdf"</Header>
    <ChildContent>
        <MokaList>
            <MokaListItem Text="Copy link" Icon="MokaIcons.Content.Link" OnClick="@(() => Pick("Copy link"))" />
            <MokaListItem Text="Send by email" Icon="MokaIcons.Action.Send" OnClick="@(() => Pick("Send by email"))" />
            <MokaListItem Text="Download" Icon="MokaIcons.Action.Download" OnClick="@(() => Pick("Download"))" />
            <MokaListItem Text="Duplicate" Icon="MokaIcons.Content.Copy" OnClick="@(() => Pick("Duplicate"))" />
        </MokaList>
    </ChildContent>
</MokaBottomSheet>
```

## Full Screen

`FullScreen` takes the whole viewport height. Put a way to close it in the header.

```blazor-preview
@code {
    bool _open;
    bool _inStock = true;
    bool _onSale;
    bool _freeShipping;
}

<div style="display:flex;align-items:center;justify-content:center;width:100%;min-height:360px">
    <MokaButton StartIcon="MokaIcons.Content.Filter" OnClick="@(() => _open = true)">Filters</MokaButton>
</div>

<MokaBottomSheet @bind-Open="_open" FullScreen ShowHandle="false">
    <Header>
        <div style="display:flex;align-items:center;justify-content:space-between">
            <span>Filters</span>
            <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm" OnClick="@(() => _open = false)">Done</MokaButton>
        </div>
    </Header>
    <ChildContent>
        <div style="display:flex;flex-direction:column;gap:12px">
            <MokaCheckbox @bind-Value="_inStock" Label="In stock only" />
            <MokaCheckbox @bind-Value="_onSale" Label="On sale" />
            <MokaCheckbox @bind-Value="_freeShipping" Label="Free shipping" />
        </div>
    </ChildContent>
</MokaBottomSheet>
```

## Explicit Close Only

With `CloseOnBackdrop` and `CloseOnEscape` off, the sheet stays open until your own code closes it. Use this for a choice the user has to make.

```blazor-preview
@code {
    bool _open;
    string _choice = "none";

    void Choose(string choice)
    {
        _choice = choice;
        _open = false;
    }
}

<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;gap:8px;width:100%;min-height:320px">
    <MokaButton OnClick="@(() => _open = true)">Leave the editor</MokaButton>
    <MokaCaption>Chosen: @_choice</MokaCaption>
</div>

<MokaBottomSheet @bind-Open="_open" CloseOnBackdrop="false" CloseOnEscape="false" Style="max-width:560px">
    <Header>Unsaved changes</Header>
    <ChildContent>
        <MokaParagraph>You have changes that are not saved yet.</MokaParagraph>
        <div style="display:flex;gap:8px;justify-content:flex-end;margin-top:12px">
            <MokaButton Variant="MokaVariant.Text" OnClick="@(() => Choose("Discard"))">Discard</MokaButton>
            <MokaButton Color="MokaColor.Primary" OnClick="@(() => Choose("Save"))">Save</MokaButton>
        </div>
    </ChildContent>
</MokaBottomSheet>
```

## Spacing

`Margin`, `Padding` and `Rounded` apply to the sheet panel, the box you see. A margin lifts the sheet off the bottom edge and keeps it clear of the sides, which gives a floating card look together with `Rounded`:

```razor
<MokaBottomSheet @bind-Open="_open" Margin="MokaSpacingScale.Md" Rounded="MokaRounding.Xl">
    <Header>Share</Header>
    <ChildContent>...</ChildContent>
</MokaBottomSheet>
```

A full-screen sheet with a margin fills the height of the screen less the margin, so the margin shows on all four sides.

Up to 0.1.12 the sheet ignored all three.

## Behaviour

- Escape and a click on the backdrop close the sheet when `CloseOnEscape` and `CloseOnBackdrop` are set. A click counts only when the press also started on the backdrop, so a drag that starts inside the sheet (selecting text, say) and ends outside leaves it open.
- The sheet is a modal dialog. Focus moves into it when it opens, Tab stays inside, and focus goes back where it was on every way it closes. It is named by `Header` text, or by an `aria-label` you pass, and `Id` and extra attributes land on the dialog element.
- With `ShowHandle`, dragging the handle down past about a third of the sheet's height (120px on a tall sheet) closes it through the same path as the close button; a shorter drag slides back. Keyboard users close it with Escape.
- The sheet spans the full width of the viewport, less any margin. `Class` and `Style` go on the sheet panel, so `Style="max-width: 560px"` gives a narrower sheet, centered at the bottom.
- The sheet renders nothing while closed, so its content is created again each time it opens.
- `PreventScroll` shares its lock with `MokaDialog`, so a sheet over a dialog, or a dialog over a sheet, keeps the page locked until both are closed.
