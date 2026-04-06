---
title: Organization Chart
description: Hierarchical org chart with cards and connectors for displaying reporting structures.
order: 71
---

# Organization Chart

`MokaOrganizationChart<TItem>` renders a hierarchical tree of cards connected by lines — ideal for company org charts, decision trees, or any parent-child structure. Supports top-down and left-right orientations, collapsible nodes, and custom card templates.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Root` | `MokaOrgNode<TItem>` | -- | Root node of the hierarchy |
| `ItemTemplate` | `RenderFragment<TItem>` | -- | Custom template for rendering each node card |
| `Orientation` | `MokaOrgOrientation` | `TopDown` | Layout direction: `TopDown` or `LeftRight` |
| `ConnectorColor` | `string?` | -- | CSS color for connector lines |
| `ConnectorWidth` | `int` | `2` | Connector line width in pixels |
| `NodeSpacing` | `int` | `24` | Horizontal spacing between sibling nodes (px) |
| `LevelSpacing` | `int` | `48` | Vertical spacing between hierarchy levels (px) |
| `Collapsible` | `bool` | `false` | Enables click-to-collapse on nodes with children |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaOrgNode&lt;TItem&gt;

| Property | Type | Description |
|----------|------|-------------|
| `Data` | `TItem` | The data item for this node |
| `Children` | `IReadOnlyList<MokaOrgNode<TItem>>?` | Child nodes |
| `IsCollapsed` | `bool` | Whether children are hidden |

## Basic Org Chart

```blazor-preview
@code {
    record Person(string Name, string Title);

    MokaOrgNode<Person> _root = new(new("Alice", "CEO"), new MokaOrgNode<Person>[]
    {
        new(new("Bob", "VP Engineering"), new MokaOrgNode<Person>[]
        {
            new(new("Dave", "Senior Dev")),
            new(new("Eve", "Senior Dev"))
        }),
        new(new("Carol", "VP Sales"))
    });
}

<MokaOrganizationChart TItem="Person" Root="_root">
    <ItemTemplate>
        <div style="text-align:center;">
            <strong>@context.Name</strong><br/>
            <span style="opacity:0.7; font-size: var(--moka-font-size-xs);">@context.Title</span>
        </div>
    </ItemTemplate>
</MokaOrganizationChart>
```

## Left-Right Orientation

```blazor-preview
@code {
    record Dept(string Name);

    MokaOrgNode<Dept> _deptRoot = new(new("Company"), new MokaOrgNode<Dept>[]
    {
        new(new("Engineering"), new MokaOrgNode<Dept>[]
        {
            new(new("Frontend")),
            new(new("Backend"))
        }),
        new(new("Marketing"))
    });
}

<MokaOrganizationChart TItem="Dept" Root="_deptRoot" Orientation="MokaOrgOrientation.LeftRight">
    <ItemTemplate>
        <strong>@context.Name</strong>
    </ItemTemplate>
</MokaOrganizationChart>
```

## Collapsible

```blazor-preview
@code {
    record Employee(string Name, string Role);

    MokaOrgNode<Employee> _cRoot = new(new("CEO", "Chief Executive"), new MokaOrgNode<Employee>[]
    {
        new(new("CTO", "Technology"), new MokaOrgNode<Employee>[]
        {
            new(new("Dev Lead", "Engineering")),
            new(new("QA Lead", "Quality"))
        }),
        new(new("CFO", "Finance"))
    });
}

<MokaOrganizationChart TItem="Employee" Root="_cRoot" Collapsible>
    <ItemTemplate>
        <div style="text-align:center;">
            <strong>@context.Name</strong><br/>
            <span style="opacity:0.7; font-size: var(--moka-font-size-xs);">@context.Role</span>
        </div>
    </ItemTemplate>
</MokaOrganizationChart>
```
