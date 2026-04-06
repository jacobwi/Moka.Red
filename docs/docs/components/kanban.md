---
title: Kanban Board
description: Drag-and-drop Kanban board for organizing items across columns.
order: 35
---

# Kanban Board

`MokaKanbanBoard<TItem>` renders a multi-column Kanban board with drag-and-drop support for moving items between columns. Each column can be individually colored, and item rendering is fully customizable via templates.

## Parameters

### MokaKanbanBoard&lt;TItem&gt;

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Columns` | `IReadOnlyList<MokaKanbanColumn<TItem>>` | -- | Column definitions with their items |
| `ItemTemplate` | `RenderFragment<TItem>?` | -- | Custom render template for each card |
| `OnItemMoved` | `EventCallback<MokaKanbanItemMovedArgs<TItem>>` | -- | Callback when an item is dragged to a different column |
| `ColumnWidth` | `string?` | -- | Fixed width for each column (e.g., `"280px"`) |
| `MaxHeight` | `string?` | -- | Maximum height of the board (scrollable if exceeded) |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaKanbanColumn&lt;TItem&gt;

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string` | -- | Column header text |
| `Items` | `IList<TItem>` | -- | Items in this column |
| `Color` | `MokaColor` | `Primary` | Column header accent color |

### MokaKanbanItemMovedArgs&lt;TItem&gt;

| Name | Type | Description |
|------|------|-------------|
| `Item` | `TItem` | The item that was moved |
| `FromColumnIndex` | `int` | Index of the source column |
| `ToColumnIndex` | `int` | Index of the destination column |

## Basic Three-Column Board

```blazor-preview
<MokaKanbanBoard TItem="string" Columns="@columns" OnItemMoved="HandleMove" />

@code {
    IReadOnlyList<MokaKanbanColumn<string>> columns = new[]
    {
        new MokaKanbanColumn<string> { Title = "To Do", Items = new List<string> { "Design mockups", "Write specs" } },
        new MokaKanbanColumn<string> { Title = "In Progress", Items = new List<string> { "Build API" } },
        new MokaKanbanColumn<string> { Title = "Done", Items = new List<string> { "Set up repo" } }
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
        new MokaKanbanColumn<TaskItem>
        {
            Title = "Backlog",
            Items = new List<TaskItem>
            {
                new("Auth system", "High"),
                new("Dark mode", "Low")
            }
        },
        new MokaKanbanColumn<TaskItem>
        {
            Title = "Active",
            Items = new List<TaskItem> { new("Dashboard", "High") }
        },
        new MokaKanbanColumn<TaskItem>
        {
            Title = "Complete",
            Items = new List<TaskItem> { new("CI pipeline", "Medium") }
        }
    };
}
```

## Colored Columns

```blazor-preview
<MokaKanbanBoard TItem="string" Columns="@coloredColumns" />

@code {
    IReadOnlyList<MokaKanbanColumn<string>> coloredColumns = new[]
    {
        new MokaKanbanColumn<string>
        {
            Title = "To Do", Color = MokaColor.Info,
            Items = new List<string> { "Task A", "Task B" }
        },
        new MokaKanbanColumn<string>
        {
            Title = "In Progress", Color = MokaColor.Warning,
            Items = new List<string> { "Task C" }
        },
        new MokaKanbanColumn<string>
        {
            Title = "Done", Color = MokaColor.Success,
            Items = new List<string> { "Task D" }
        }
    };
}
```
