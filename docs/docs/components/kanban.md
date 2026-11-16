---
title: Kanban Board
description: Drag-and-drop Kanban board for organizing items across columns.
order: 35
---

# Kanban Board

`MokaKanbanBoard<TItem>` renders a multi-column Kanban board. People move cards with a mouse, a pen, a finger or the keyboard, to another column or to another place in their own. Each column can be individually colored, and item rendering is fully customizable via templates.

## Parameters

### MokaKanbanBoard&lt;TItem&gt;

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Columns` | `IReadOnlyList<MokaKanbanColumn<TItem>>` | -- | Column definitions with their items |
| `ItemTemplate` | `RenderFragment<TItem>?` | -- | Custom render template for each card |
| `OnItemMoved` | `EventCallback<MokaKanbanItemMovedArgs<TItem>>` | -- | Raised after an item moved, to another column or another place in its own. The board has already moved it in the columns' lists |
| `ColumnWidth` | `string?` | -- | Fixed width for each column (e.g., `"280px"`) |
| `MaxHeight` | `string?` | -- | Maximum height of the board (scrollable if exceeded) |
| `ItemKey` | `Func<TItem, object?>?` | -- | What identifies an item, so a card keeps its element when items move. Unset, the item itself does |
| `ItemLabel` | `Func<TItem, string?>?` | -- | A card's name for screen readers when it shows more than its name. Unset, its text names it |
| `Text` | `MokaKanbanText?` | English | What a screen reader hears while a card moves |
| `Disabled` | `bool` | `false` | Stops cards from moving: no dragging and no keyboard moves |
| `OnCardContextMenu` | `EventCallback<MokaItemContextMenuArgs<TItem>>` | -- | A right-click on a card; the browser's menu is suppressed only while this listens |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

`MokaKanbanBoard` inherits from `MokaVisualComponentBase` (see [Base Classes](../api/base-classes)). `Id`, other attributes, `Margin`, `Padding` and `Rounded` land on the board; `Padding` replaces its own padding around the columns. `Size`, `SizeValue`, `Color` and `Variant` have no effect on it; a column's color is `MokaKanbanColumn.Color`.

### MokaKanbanColumn&lt;TItem&gt;

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string` | -- | Column header text |
| `Items` | `IList<TItem>` | -- | Items in this column |
| `Color` | `MokaColor?` | `null` | Column header accent color |
| `Limit` | `int?` | `null` | The column's work-in-progress limit. The header shows the count against it and marks the column once it holds more; a card can still be dropped there |

### MokaKanbanItemMovedArgs&lt;TItem&gt;

| Name | Type | Description |
|------|------|-------------|
| `Item` | `TItem` | The item that was moved |
| `FromColumnIndex` | `int` | Index of the source column |
| `ToColumnIndex` | `int` | Index of the destination column, the same as the source for a move within a column |
| `FromIndex` | `int` | Where the item was in its source column, from 0 |
| `ToIndex` | `int` | Where the item is now in its destination column, from 0 |

## Moving Cards

- **Mouse and pen:** press a card and move a few pixels. A copy follows the pointer, the card's place dims, the column it would land in lights up and a bar marks the spot. Release to drop it there, or press Escape to put it back. Near the board's sides it scrolls sideways, and near a column's top or bottom that column scrolls.
- **Touch:** hold a card for a moment, then move it. A quick swipe still scrolls.
- **Keyboard:** Tab to a card and press Space to pick it up. The arrow keys move where it would land (Left and Right between columns, Up and Down within one, Home and End to a column's ends), Space or Enter drops it and Escape puts it back. Focus follows the card to its new place, and a screen reader hears each step (`Text`).

A press on a control inside a card (a button, a link, a field, or anything marked `data-moka-no-drag`) works as that control and doesn't start a drag. All the moving happens in the browser; only the drop reaches .NET, which moves the item in the lists, draws the board again and raises `OnItemMoved`.

## Basic Three-Column Board

```blazor-preview
<MokaKanbanBoard TItem="string" Columns="@columns" OnItemMoved="HandleMove" />

@code {
    IReadOnlyList<MokaKanbanColumn<string>> columns = new[]
    {
        new MokaKanbanColumn<string>("To Do", new List<string> { "Design mockups", "Write specs" }),
        new MokaKanbanColumn<string>("In Progress", new List<string> { "Build API" }),
        new MokaKanbanColumn<string>("Done", new List<string> { "Set up repo" })
    };

    void HandleMove(MokaKanbanItemMovedArgs<string> args)
    {
        // Items are automatically moved in the lists
    }
}
```

## Custom Card Template

```blazor-preview
<MokaKanbanBoard TItem="TaskItem" Columns="@taskColumns" ColumnWidth="300px">
    <ItemTemplate>
        <MokaCard Style="margin-bottom:4px">
            <MokaText Weight="MokaFontWeight.Semibold">@context.Title</MokaText>
            <MokaTag Text="@context.Priority" Size="MokaSize.Xs"
                     Color="@(context.Priority == "High" ? MokaColor.Error : MokaColor.Info)" />
        </MokaCard>
    </ItemTemplate>
</MokaKanbanBoard>

@code {
    record TaskItem(string Title, string Priority);

    IReadOnlyList<MokaKanbanColumn<TaskItem>> taskColumns = new[]
    {
        new MokaKanbanColumn<TaskItem>("Backlog", new List<TaskItem>
        {
            new("Auth system", "High"),
            new("Dark mode", "Low")
        }),
        new MokaKanbanColumn<TaskItem>("Active", new List<TaskItem> { new("Dashboard", "High") }),
        new MokaKanbanColumn<TaskItem>("Complete", new List<TaskItem> { new("CI pipeline", "Medium") })
    };
}
```

## Colored Columns

```blazor-preview
<MokaKanbanBoard TItem="string" Columns="@coloredColumns" />

@code {
    IReadOnlyList<MokaKanbanColumn<string>> coloredColumns = new[]
    {
        new MokaKanbanColumn<string>("To Do", new List<string> { "Task A", "Task B" }, Color: MokaColor.Info),
        new MokaKanbanColumn<string>("In Progress", new List<string> { "Task C" }, Color: MokaColor.Warning),
        new MokaKanbanColumn<string>("Done", new List<string> { "Task D" }, Color: MokaColor.Success)
    };
}
```

## Work-in-Progress Limits

```blazor-preview
<MokaKanbanBoard TItem="string" Columns="@limited" />

@code {
    IReadOnlyList<MokaKanbanColumn<string>> limited = new[]
    {
        new MokaKanbanColumn<string>("Ready", new List<string> { "Write the brief" }),
        new MokaKanbanColumn<string>("Doing", new List<string> { "Order the prints", "Call the printer", "Pick a venue" }, MokaColor.Warning) { Limit = 2 },
        new MokaKanbanColumn<string>("Done", new List<string>(), MokaColor.Success)
    };
}
```

The header reads "3/2" and turns to the error colors once the column holds more than its limit.
