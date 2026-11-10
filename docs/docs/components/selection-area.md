---
title: Selection Area
description: Box selection around any content. Press on empty space, drag a rectangle, and every item it touches is reported.
order: 158
---

# Selection Area

`MokaSelectionArea` lets people select items with a box, as in a file manager: a mouse press on empty space and a drag draw a rectangle, and every item the rectangle touches is reported to your app, live while dragging and once on release. Your app keeps the selection. The area only says what the box covers and how to combine it with what was selected before.

It draws boxes as soon as it's there, since that's what it's for. `BoxSelect="false"` turns it off, for example behind a setting, and then it renders its content in a plain `div` and nothing else: no box element, no script, no listeners.

```razor
@using Moka.Red.Primitives.SelectionArea
```

## A grid of tiles

Mark each item with `data-moka-select-key`. Keep the selection kept at the start of a box, and let `Combine` work out the rest.

```razor
<div class="gallery" @onclick="() => _selected.Clear()">
    <MokaSelectionArea OnBoxSelect="Boxed">
        <div class="tiles">
            @foreach (Photo photo in _photos)
            {
                <div class="@(_selected.Contains(photo.Id) ? "tile tile--on" : "tile")" data-moka-select-key="@photo.Id"
                     @onclick="() => Pick(photo)" @onclick:stopPropagation="true">
                    <img src="@photo.Thumbnail" alt="@photo.Name" draggable="false" />
                </div>
            }
        </div>
    </MokaSelectionArea>
</div>

@code {
    private HashSet<string> _selected = new();
    private HashSet<string> _before = new();

    private void Boxed(MokaBoxSelectArgs box)
    {
        if (box.Phase == MokaBoxSelectPhase.Started)
        {
            _before = new HashSet<string>(_selected);
        }

        _selected = box.Combine(_before, box.Keys);
    }
}
```

The click that ends a real drag is swallowed, so a container's own `@onclick`, such as the one above that clears the selection, doesn't undo what the box selected. A press that moves less than four pixels is still a click.

## What starts a box

Only a press of the main mouse button on empty space. A press on an item, a button, a link, a field, `[contenteditable]`, anything with an interactive role, anything draggable (images and links are, unless they say `draggable="false"`) or anything inside `[data-moka-no-box-select]` keeps its own click, drag and context menu. Touch and pen keep scrolling. A press on the scroll container's own background counts, so the padding around the content works too, but not a press on its scrollbar.

| While dragging | |
|------|------|
| Shift | Adds the box to the selection (`MokaBoxSelectMode.Add`), with or without Control |
| Control, or Command on a Mac | Flips each item the box touches (`Toggle`) |
| Neither | The box is the selection (`Replace`) |
| Esc | Cancels, and the release that follows clicks nothing |
| Near an edge | Scrolls the container, faster the closer the pointer is |

The keys held when the box starts decide the mode. The box keeps its start in the content, so it stays right while the container scrolls under it, and the pointer is captured, so releasing outside the window still ends it. Keyboard selection is up to your app and is untouched.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `BoxSelect` | `bool` | `true` | Lets a mouse press on empty space draw a box. `false` renders a plain `div` and loads no script |
| `OnBoxSelect` | `EventCallback<MokaBoxSelectArgs>` | -- | Raised when a box starts, when what it covers changes, and when it finishes or is cancelled |
| `KeyAttribute` | `string` | `"data-moka-select-key"` | The attribute an item carries its key in |
| `ItemSelector` | `string?` | -- | A CSS selector for the items. Null is every element carrying `KeyAttribute` |
| `ScrollContainerSelector` | `string?` | -- | The element that scrolls, looked for among the ancestors first, then in the page. Null is the nearest scrolling ancestor, the area included, or the window |
| `Geometry` | `MokaBoxSelectGeometry?` | -- | Lines of equal height, so the box also covers items that aren't drawn |
| `ChildContent` | `RenderFragment?` | -- | The content, with the items somewhere inside it |
| `Disabled` | `bool` | `false` | Turns box selection off, as `BoxSelect="false"` does |
| `Class`, `Style`, `Id`, `Margin`, `Padding`, `Rounded` | | -- | On the wrapper. Other attributes go there too |

While a box is drawn the wrapper carries `data-moka-box-selecting`, for a style such as dropping a hover effect.

## The report

`MokaBoxSelectArgs` says:

| Member | Description |
|------|------|
| `Phase` | `Started`, `Changed`, `Finished` or `Cancelled` |
| `Mode` | `Replace`, `Add` or `Toggle`, from the keys held when the box started |
| `Keys` | The keys of the drawn items the box touches, in page order. Empty when cancelled |
| `Range` | With a `Geometry`, the items covered whether drawn or not, as a `MokaBoxSelectRange`. Null otherwise |
| `Combine(before, covered, comparer)` | The selection the report asks for, as a new `HashSet<T>`. A cancelled box gives back `before` |

Keep the selection when `Phase` is `Started` and pass it as `before` on every report, so a cancelled box puts it back. Reports go to .NET one at a time: a change is sent at most once a frame, only when what is covered changed, and never while the last report is still on its way, which keeps Blazor Server's connection quiet. The final report always arrives.

## A virtualized list

`Virtualize` only draws the rows in view, so a box dragged from row 3 to row 4000 can't find most of them on the page. Describe the lines with `MokaBoxSelectGeometry` and give each drawn item its index. The box then reports the lines and columns it covers as a `Range`, measured from whichever rows are drawn, and your app maps the indexes to its own items.

```razor
<div class="log" @onclick="() => _selected.Clear()">
    <MokaSelectionArea Geometry="_geometry" OnBoxSelect="Boxed">
        <Virtualize Items="_rows" ItemSize="32" Context="row">
            <div class="row" data-moka-select-key="@row.Key" data-moka-select-index="@row.Index">@row.Text</div>
        </Virtualize>
    </MokaSelectionArea>
</div>

@code {
    private MokaBoxSelectGeometry _geometry = new(5000);

    private void Boxed(MokaBoxSelectArgs box)
    {
        if (box.Phase == MokaBoxSelectPhase.Started)
        {
            _before = new HashSet<string>(_selected);
        }

        IEnumerable<string> covered = box.Range is { } range
            ? range.Indexes.Select(index => _rows[index].Key)
            : box.Keys;
        _selected = box.Combine(_before, covered);
    }
}
```

| `MokaBoxSelectGeometry` | Description |
|------|------|
| `Count` | How many items there are, drawn or not |
| `Columns` | Items on each line. Default 1, a list |
| `LineHeight` | The distance from one line's top to the next, in CSS pixels. Null measures it |
| `IndexAttribute` | Where each drawn item carries its index. Default `data-moka-select-index` |
| `IndexBase` | What that attribute holds on the first item. 1 for `aria-posinset`, 2 for `aria-rowindex` under a header row |

A grid with a header row can use the `aria-rowindex` its rows need anyway:

```razor
<MokaSelectionArea KeyAttribute="data-node"
                   Geometry="@(new MokaBoxSelectGeometry(_rows.Count) { IndexAttribute = "aria-rowindex", IndexBase = 2 })"
                   OnBoxSelect="Boxed">
```

`MokaBoxSelectRange` holds `FirstLine`, `LastLine`, `FirstColumn`, `LastColumn`, `Columns` and `Count`. `Indexes` lists every index it covers, `Contains(index)` checks one, and with one column `FirstIndex` to `LastIndex` is the whole run.

## Your own attribute

An app that already marks its items can point the area at that attribute. The key is read from it.

```razor
<div class="explorer__view" tabindex="0" @onclick="ClearSelection">
    <MokaSelectionArea KeyAttribute="data-node" OnBoxSelect="Boxed">
        <EntryTiles Items="_visible" />
    </MokaSelectionArea>
</div>
```

## Looks

The box is drawn by `.moka-box-select` in `moka.css`: a border in `--moka-color-primary-border` and a fill of `--moka-color-primary-glow`, so it follows the accent and the light or dark scheme on every theme. It has no animation. The script places it in whatever its containing block is and clips it to the scroll container, so the area needs no `position` of its own.

## In the file browser

`MokaFileBrowser` has the same box behind its own `BoxSelect`, wired to its selection. See [File Browser](/components/file-browser).
