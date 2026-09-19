---
title: Selection Bar
description: Bulk action bar that appears while items are selected, with a count, actions and a deselect button.
order: 73
---

# Selection Bar

`MokaSelectionBar` shows how many items are selected, a slot for bulk actions and a deselect button. It is hidden while `Count` is zero and slides in once something is selected. By default it sticks to the bottom of its scroll container; `Fixed` pins it to the bottom center of the viewport instead.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Count` | `int` | `0` | Number of selected items. The bar shows only when it is above zero |
| `Label` | `string` | `"selected"` | Text after the count. Also the toolbar's accessible name, and read out with the count, as in "3 files selected" |
| `ChildContent` | `RenderFragment?` | -- | Action buttons, placed after the label |
| `ShowClear` | `bool` | `true` | Shows the deselect button at the end |
| `ClearLabel` | `string` | `"Deselect"` | Text and accessible name of the deselect button |
| `OnClear` | `EventCallback` | -- | Raised when the deselect button is clicked. Clear your selection here |
| `Fixed` | `bool` | `false` | `false` sticks the bar to the bottom of its scroll container. `true` fixes it to the bottom center of the viewport |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the bar. A fixed bar stays centered |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding inside the bar |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of the bar |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<div style="width:100%;max-width:480px">
    <MokaSelectionBar Count="3" Label="files selected">
        <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Download">Download</MokaButton>
        <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Text" Color="MokaColor.Error" StartIcon="MokaIcons.Action.Delete">Delete</MokaButton>
    </MokaSelectionBar>
</div>
```

## With a Selectable List

Tick a file to show the bar. Deselect and Delete both empty the selection, which hides it again.

```blazor-preview
<div style="width:100%;max-width:480px;display:flex;flex-direction:column;gap:8px">
    @foreach (var file in _files)
    {
        var name = file;
        <MokaCheckbox Label="@name"
                      Value="_selected.Contains(name)"
                      ValueChanged="@((bool on) => Toggle(name, on))" />
    }

    <MokaSelectionBar Count="_selected.Count" Label="files selected" OnClear="() => _selected.Clear()">
        <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Download">Download</MokaButton>
        <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Text" Color="MokaColor.Error" StartIcon="MokaIcons.Action.Delete"
                    OnClick="DeleteSelected">Delete</MokaButton>
    </MokaSelectionBar>
</div>

@code {
    private readonly List<string> _files = new() { "report-q3.pdf", "invoice-0142.pdf", "logo.svg", "notes.md" };
    private readonly HashSet<string> _selected = new() { "invoice-0142.pdf" };

    private void Toggle(string name, bool on)
    {
        if (on)
        {
            _selected.Add(name);
        }
        else
        {
            _selected.Remove(name);
        }
    }

    private void DeleteSelected()
    {
        _files.RemoveAll(_selected.Contains);
        _selected.Clear();
    }
}
```

## Inside a Scroll Container

In the default sticky mode the bar stays at the bottom of the scrolling element while the list moves under it. Put the bar after the list, inside the element that scrolls.

```blazor-preview
<div style="width:100%;max-width:480px;height:220px;overflow:auto;padding:8px;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    @for (var i = 1; i <= 24; i++)
    {
        <div style="padding:6px 4px;font-family:var(--moka-font-family-mono);font-size:var(--moka-font-size-sm)">invoice-@(i.ToString("0000")).pdf</div>
    }
    <MokaSelectionBar Count="5" Label="invoices selected">
        <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Download">Export</MokaButton>
    </MokaSelectionBar>
</div>
```

## Custom Labels

```blazor-preview
<div style="width:100%;max-width:480px;display:flex;flex-direction:column;gap:12px">
    <MokaSelectionBar Count="12" Label="rows selected" ClearLabel="Clear selection" OnClear="() => { }">
        <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Edit">Edit</MokaButton>
    </MokaSelectionBar>
    <MokaSelectionBar Count="2" Label="drafts selected" ShowClear="false">
        <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Send">Publish</MokaButton>
    </MokaSelectionBar>
</div>
```

## Behaviour

- The bar rises and fades in over 150ms when it appears.
- Sticky mode uses `position: sticky` with a bottom offset. It needs a scrolling ancestor and has to come after the content it floats over.
- `Fixed` mode centers the bar at the bottom of the viewport, above other content, wherever it sits in the markup. The bar sits in a strip along the bottom of the viewport that centers it with flexbox, so a margin keeps it centered, and the strip lets clicks through beside the bar. A long bar can use the whole width, less a small gap at each side. Up to 0.1.12 a transform centered it, so a margin pushed it off center, and it was never wider than half the viewport.
- The deselect button only raises `OnClear`. The bar keeps no selection of its own, so update `Count` yourself.
- The count is shown in the monospace font and the label as an uppercase micro-label.
- A visually hidden status element sits just before the bar and stays in the page while `Count` is zero. It holds the count and the label, and is empty while nothing is selected.

## Accessibility

The bar has `role="toolbar"`, named by `Label`. It is not a live region itself, since it holds buttons. The count is announced from the separate hidden element, a polite live region (`role="status"`) that is always rendered: screen readers only report changes to a region that already exists, and the bar appears already filled. They read "3 files selected" when a selection starts and each time the count changes, and nothing when the selection is cleared. Pick a `Label` that works both as a name and after a number, such as "files selected". The toolbar does not add arrow-key navigation between its buttons; Tab moves through them.
