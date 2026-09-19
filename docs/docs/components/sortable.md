---
title: Sortable
description: Drag-to-reorder list with vertical/horizontal orientation, drag handles, and disabled items.
order: 12
---

# Sortable

`MokaSortable<TItem>` provides drag-and-drop list reordering powered by a lightweight JS module. It works with any `IList<TItem>` and automatically mutates the list in-place on a successful drop. An `OnReorder` callback fires with old and new indices for external state synchronisation.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IList<TItem>` | **required** | The mutable list to render and reorder |
| `ItemTemplate` | `RenderFragment<TItem>?` | - | Custom item renderer |
| `OnReorder` | `EventCallback<(int OldIndex, int NewIndex)>` | - | Fired after a successful reorder |
| `DragHandle` | `bool` | `false` | Only the grip handle initiates drag |
| `Horizontal` | `bool` | `false` | Horizontal flex layout |
| `IsItemDisabled` | `Func<TItem, bool>?` | - | Per-item disabled predicate |
| `ItemKey` | `Func<TItem, object>?` | - | Identifies an item across renders, so a moved item keeps its DOM and state. Without it the item itself is the key. Items that share a key render unkeyed instead of throwing |
| `Class` | `string?` | - | Additional CSS classes on the container |
| `Style` | `string?` | - | Additional inline styles |

## Basic Vertical List

```blazor-preview
@code {
    List<string> _items = ["Alpha", "Beta", "Gamma", "Delta", "Epsilon"];
}

<MokaSortable Items="_items">
    <ItemTemplate Context="item">
        <div class="moka-sortable-item">@item</div>
    </ItemTemplate>
</MokaSortable>
```

## Horizontal Orientation

```blazor-preview
@code {
    List<string> _columns = ["Name", "Status", "Priority", "Assignee"];
}

<MokaSortable Items="_columns" Horizontal>
    <ItemTemplate Context="col">
        <div style="padding:8px 16px;background:var(--moka-color-surface-2);border-radius:4px;cursor:grab">
            @col
        </div>
    </ItemTemplate>
</MokaSortable>
```

## Drag Handle

When `DragHandle` is true only the `.moka-sortable-handle` element initiates drag. The rest of the item receives normal pointer events.

```blazor-preview
@code {
    record Task(string Id, string Title);
    List<Task> _tasks = [
        new("1", "Design system audit"),
        new("2", "Update dependencies"),
        new("3", "Write release notes"),
        new("4", "Deploy to staging"),
    ];
}

<MokaSortable Items="_tasks" DragHandle ItemKey="t => t.Id">
    <ItemTemplate Context="task">
        <div style="display:flex;align-items:center;gap:8px;padding:8px;
                    border:1px solid var(--moka-border-color);border-radius:4px">
            <span class="moka-sortable-handle" style="cursor:grab;color:var(--moka-color-text-secondary)">
                <MokaIcon Icon="MokaIcons.Content.Sort" Size="MokaSize.Sm" />
            </span>
            @task.Title
        </div>
    </ItemTemplate>
</MokaSortable>
```

## OnReorder Callback

The list is mutated in place before `OnReorder` fires. Use the callback to persist the new order.

```blazor-preview
@code {
    List<string> _items = ["First", "Second", "Third", "Fourth"];
    string _log = "";
}

<MokaSortable Items="_items">
    <ItemTemplate Context="item">
        <div style="padding:8px 12px;border:1px solid var(--moka-border-color);border-radius:4px">
            @item
        </div>
    </ItemTemplate>
</MokaSortable>
@if (!string.IsNullOrEmpty(_log))
{
    <MokaCaption>@_log</MokaCaption>
}
```

## Disabled Items

Items for which `IsItemDisabled` returns `true` render with a `moka-sortable-item--disabled` class and cannot be dragged.

```blazor-preview
@code {
    record Priority(string Name, bool Locked);
    List<Priority> _list = [
        new("Critical", true),
        new("High", false),
        new("Medium", false),
        new("Low", false),
        new("Backlog", true),
    ];
}

<MokaSortable Items="_list" IsItemDisabled="p => p.Locked" ItemKey="p => p.Name">
    <ItemTemplate Context="p">
        <div style="display:flex;align-items:center;gap:8px;padding:8px 12px;
                    border:1px solid var(--moka-border-color);border-radius:4px">
            @if (p.Locked)
            {
                <MokaIcon Icon="MokaIcons.Toggle.Lock" Size="MokaSize.Sm" Color="MokaColor.Secondary" />
            }
            @p.Name
        </div>
    </ItemTemplate>
</MokaSortable>
```

## Moving Items Between Lists

`MokaSortable` reorders within a single list. To move items between two lists, handle
`OnReorder` on each and move the item yourself, or use `MokaKanbanBoard`, which is built
for exactly that.

```blazor-preview
@code {
    List<string> _todo = ["Design", "Development", "Testing"];
    List<string> _done = ["Planning", "Research"];

    void Promote(string item)
    {
        _todo.Remove(item);
        _done.Add(item);
    }
}

<div style="display:flex;gap:24px">
    <div>
        <MokaLabel>To Do</MokaLabel>
        <MokaSortable Items="_todo">
            <ItemTemplate Context="item">
                <div style="display:flex;gap:8px;align-items:center;padding:8px 12px;background:var(--moka-color-surface-2);border-radius:4px">
                    <span>@item</span>
                    <MokaButton Size="MokaSize.Xs" OnClick="() => Promote(item)">Done</MokaButton>
                </div>
            </ItemTemplate>
        </MokaSortable>
    </div>
    <div>
        <MokaLabel>Done</MokaLabel>
        <MokaSortable Items="_done">
            <ItemTemplate Context="item">
                <div style="padding:8px 12px;background:var(--moka-color-surface-2);border-radius:4px">
                    @item
                </div>
            </ItemTemplate>
        </MokaSortable>
    </div>
</div>
```
