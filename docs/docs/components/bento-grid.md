---
title: Bento Grid
description: Asymmetric grid layout where items can span multiple rows and columns for dashboard and feature layouts.
order: 86
---

# Bento Grid

`MokaBentoGrid` and `MokaBentoItem` create an asymmetric grid layout inspired by bento box designs. Items can span multiple rows and columns, making it ideal for feature showcases, dashboards, and marketing layouts. With `Board` set, the grid is a board of cards the person reorders, resizes and hides (see [Board customizing](#board-customizing)).

## Setup

```razor
@using Moka.Red.Layout.BentoGrid
```

## MokaBentoGrid Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | `MokaBentoItem` children. Not drawn on a board |
| `Columns` | `int` | `4` | Number of grid columns. Not used on a board |
| `Gap` | `MokaSpacingScale` | `Md` | Gap between items. Not used on a board |
| `GapValue` | `string?` | - | Custom gap value (overrides `Gap`). Not used on a board |
| `RowHeight` | `string` | `"auto"` | Grid row height. Not used on a board |
| `MinRowHeight` | `string` | `"120px"` | Minimum row height. Not used on a board |
| `Board` | `IReadOnlyList<MokaBoardCard>?` | - | Makes the grid a board: its cards in order, hidden ones included |
| `BoardChanged` | `EventCallback<IReadOnlyList<MokaBoardCard>>` | - | Raised with the whole new layout after every change |
| `CardTemplate` | `RenderFragment<MokaBoardCard>?` | - | Draws one card. Put a `MokaBentoItem` with `Card` in it |
| `DefaultBoard` | `IReadOnlyList<MokaBoardCard>?` | - | What "Back to the default" puts back. Without it there is no such button |
| `Customizing` | `bool` | `false` | Customize mode: tools on each card, the tray of hidden cards, and the bar with "Done" |
| `CustomizingChanged` | `EventCallback<bool>` | - | Raised when "Done" ends customize mode |
| `CardTitle` | `Func<string, string>?` | - | Names a card by its id, for the tray and for a card with no `Title`. Default: the id |
| `CustomizeContent` | `RenderFragment?` | - | At the start of the customize bar, such as whether the last change was saved |
| `DoneText` | `string` | `"Done"` | Text of the button that ends customize mode |
| `ResetText` | `string` | `"Back to the default"` | Text of the button that puts `DefaultBoard` back |
| `TrayLabel` | `string` | `"Hidden"` | The label over the tray of hidden cards |
| `TrayEmptyText` | `string` | `"Nothing hidden"` | What the tray says when no card is hidden |

## MokaBentoItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | Item content |
| `ColSpan` | `int` | `1` | Number of columns to span. Not used on a card |
| `RowSpan` | `int` | `1` | Number of rows to span. Not used on a card |
| `ColStart` | `int?` | - | Explicit column start position |
| `RowStart` | `int?` | - | Explicit row start position |
| `Title` | `string?` | - | Item title. On a card, the card's name in its head |
| `Description` | `string?` | - | Item description text |
| `Clickable` | `bool` | `false` | Makes the item a button: hover effect, pointer cursor, tab stop, Enter and Space |
| `OnClick` | `EventCallback` | - | Click callback |
| `Card` | `MokaBoardCard?` | - | The board card this item draws. Sets its size, and adds its tools while the board is customized |
| `Icon` | `MokaIconDefinition?` | - | An icon in the head, before the title |
| `Source` | `string?` | - | A small tag after the title saying where the card comes from, such as "Plugin" |
| `Href` | `string?` | - | Where the head's Open link goes. Without it there is no link |
| `OpenText` | `string` | `"Open"` | Text of the head's Open link |

Setting `Card`, `Icon`, `Source` or `Href` gives the item a head: the icon, the title in 14 px, the source tag and the Open link in one row. Without any of them the item renders as it always has.

## Basic Bento Layout

```blazor-preview
<MokaBentoGrid Columns="3">
    <MokaBentoItem ColSpan="2" Title="Feature One" Description="Spans two columns">
        Main feature content here.
    </MokaBentoItem>
    <MokaBentoItem Title="Metric">
        <MokaStat Title="Users" Value="1,234" />
    </MokaBentoItem>
    <MokaBentoItem Title="Item A">Detail A</MokaBentoItem>
    <MokaBentoItem Title="Item B">Detail B</MokaBentoItem>
    <MokaBentoItem Title="Item C">Detail C</MokaBentoItem>
</MokaBentoGrid>
```

## Multi-Row Spanning

```blazor-preview
<MokaBentoGrid Columns="4" MinRowHeight="100px">
    <MokaBentoItem ColSpan="2" RowSpan="2" Title="Hero Block">
        This item spans 2 columns and 2 rows.
    </MokaBentoItem>
    <MokaBentoItem Title="Top Right 1">Small cell</MokaBentoItem>
    <MokaBentoItem Title="Top Right 2">Small cell</MokaBentoItem>
    <MokaBentoItem Title="Bottom Right 1">Small cell</MokaBentoItem>
    <MokaBentoItem Title="Bottom Right 2">Small cell</MokaBentoItem>
</MokaBentoGrid>
```

## Clickable Items

```blazor-preview
@code { string _clicked = "None"; }
<MokaText>Last clicked: @_clicked</MokaText>
<MokaBentoGrid Columns="3" Gap="MokaSpacingScale.Sm">
    <MokaBentoItem Title="Analytics" Clickable OnClick="@(() => _clicked = "Analytics")">Click me</MokaBentoItem>
    <MokaBentoItem Title="Reports" Clickable OnClick="@(() => _clicked = "Reports")">Click me</MokaBentoItem>
    <MokaBentoItem Title="Settings" Clickable OnClick="@(() => _clicked = "Settings")">Click me</MokaBentoItem>
</MokaBentoGrid>
```

A clickable item is a button to the keyboard and to screen readers. It joins the tab order with the focus ring, and Enter or Space activate it, the same as a click. Keys pressed in a control inside it are left alone, and Space does not scroll the page.

## Board customizing

A board is a page of cards each person arranges for themselves: in what order, how big, and which ones show. Give the grid a `Board` and a `CardTemplate`, and it draws the shown cards in that order on 4 columns, 2 below 1180 px and 1 on a phone. A `Small` card takes one column, `Wide` two and `Full` a row, packed so they leave no holes.

A board is a list of `MokaBoardCard(Id, Size, Hidden)` records, hidden cards included, so it can be stored as it is. Storing it is the app's job: every change draws at once and is raised through `BoardChanged` with the whole new layout.

```razor
<MokaButton Variant="MokaVariant.Outlined" Color="MokaColor.Secondary" OnClick="() => _customizing = !_customizing">
    Customize
</MokaButton>

<MokaBentoGrid Board="_board" BoardChanged="SaveAsync" @bind-Customizing="_customizing"
               DefaultBoard="Defaults" CardTitle="TitleOf">
    <CardTemplate Context="card">
        <MokaBentoItem Card="card" Icon="IconOf(card.Id)" Href="@($"/{card.Id}")">
            <HomeCardBody Id="@card.Id"/>
        </MokaBentoItem>
    </CardTemplate>
</MokaBentoGrid>

@code {
    private static readonly IReadOnlyList<MokaBoardCard> Defaults =
    [
        new("internet", MokaBoardSize.Wide),
        new("devices", MokaBoardSize.Small),
        new("security", MokaBoardSize.Small),
        new("traffic", MokaBoardSize.Full),
        new("plugins", MokaBoardSize.Small, Hidden: true)
    ];

    private IReadOnlyList<MokaBoardCard> _board = Defaults;
    private bool _customizing;

    private async Task SaveAsync(IReadOnlyList<MokaBoardCard> next)
    {
        IReadOnlyList<MokaBoardCard> before = _board;
        _board = next;
        if (!await Preferences.SaveBoardAsync(next))
        {
            _board = before;
        }
    }
}
```

### Customize mode

While `Customizing` is on, each card gets a dashed ring and a row of tools: a grip to drag it by, earlier and later, its size (a click goes Small, Wide, Full), and hide. Above the board sit "Back to the default", which puts `DefaultBoard` back, and "Done", which ends customize mode and raises `CustomizingChanged`. Under them, the tray lists the hidden cards; picking one puts it back after the last card shown.

`CustomizeContent` goes at the start of that bar, for a word on whether the last change was saved.

### Dragging and the keyboard

Dragging a card by its grip, with a mouse, a pen or a finger, never moves the page's own elements. A copy of the card follows the pointer and a bar marks where it would land: beside a card in a row, above or below a card that takes the whole row. Only the drop reaches .NET. Escape cancels a drag, and near the edge of whatever scrolls, it scrolls.

On the grip, the arrow keys move the card one place, Left and Up earlier, Right and Down later. After any move, focus goes back to the tool that did it on the moved card, or to its grip when that tool is now disabled. Hiding a card moves focus to its place in the tray, and putting it back moves focus to its grip.

### Putting a refused change back

The board keeps the change it drew until the app passes a layout whose cards differ from the last one it passed. So an app that sets its field to the new layout before saving puts a refused change back by setting the field to the one it had, as `SaveAsync` above does. An app that never changes the field it passes has to pass a new list to put one back.

### The card's head

A card's head holds its `Icon`, its title, an optional `Source` tag and an Open link to `Href`. The title is `Title`, or what `CardTitle` gives for the card's id. The card is an `article` named by its title.

```razor
<MokaBentoItem Card="card" Title="Plugins" Icon="MokaIcons.Content.Copy" Source="Plugin" Href="/plugins">
    ...
</MokaBentoItem>
```
