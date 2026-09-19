---
title: Empty State
description: Placeholder for an empty list, a search with no results or a folder with no files, with an icon or image, a title, a description and actions.
order: 102
---

# Empty State

`MokaEmptyState` fills the space where content would be when there is none: an empty list, a search with no results, a folder with no files. It shows an icon or an image, a title and a short description, then any actions you put in `ChildContent`, all centered.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Icon` | `MokaIconDefinition?` | -- | Large icon at the top, drawn at 48px and dimmed. Ignored when `ImageSrc` is set |
| `ImageSrc` | `string?` | -- | Image URL shown instead of the icon, at most 120px square. Treated as decoration, with an empty `alt` |
| `Title` | `string?` | -- | Title line |
| `Description` | `string?` | -- | Smaller, dimmed text under the title, at most 320px wide |
| `ChildContent` | `RenderFragment?` | -- | Actions in a row under the description, such as buttons |
| `Padding` | `MokaSpacingScale?` | -- | Replaces the default padding (32px top and bottom) |
| `PaddingValue` | `string?` | -- | Any CSS padding. Overrides `Padding` |
| `Margin` | `MokaSpacingScale?` | -- | Margin from the spacing scale |
| `MarginValue` | `string?` | -- | Any CSS margin. Overrides `Margin` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<MokaEmptyState Icon="MokaIcons.File.FolderOpen"
                Title="This folder is empty"
                Description="Files you upload to this folder show up here." />
```

## With Actions

```blazor-preview
<MokaEmptyState Icon="MokaIcons.Action.Search"
                Title="No results for 'invoice 2041'"
                Description="Check the spelling or search with fewer words.">
    <MokaButton Variant="MokaVariant.Outlined" Size="MokaSize.Sm">Clear search</MokaButton>
    <MokaButton Color="MokaColor.Primary" Size="MokaSize.Sm" StartIcon="MokaIcons.Action.Add">New invoice</MokaButton>
</MokaEmptyState>
```

## In Place of a List

Show the empty state when the list has nothing to show, and give the user a way to fill it.

```blazor-preview
@code {
    List<string> _tasks = [];

    void AddSamples() => _tasks = ["Review pull request", "Update the changelog", "Tag the release"];
}

<div style="width:100%;max-width:420px">
    @if (_tasks.Count == 0)
    {
        <MokaEmptyState Icon="MokaIcons.Status.CheckCircle"
                        Title="No tasks"
                        Description="You're all caught up.">
            <MokaButton Size="MokaSize.Sm" StartIcon="MokaIcons.Action.Add" OnClick="AddSamples">Add sample tasks</MokaButton>
        </MokaEmptyState>
    }
    else
    {
        <MokaList Bordered>
            @foreach (var task in _tasks)
            {
                <MokaListItem Text="@task" Icon="MokaIcons.Status.Clock" />
            }
        </MokaList>
        <div style="margin-top:8px">
            <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm" OnClick="@(() => _tasks = [])">Clear all</MokaButton>
        </div>
    }
</div>
```

## With an Image

`ImageSrc` replaces the icon with your own illustration:

```razor
<MokaEmptyState ImageSrc="images/empty-inbox.svg"
                Title="Inbox zero"
                Description="New messages arrive here." />
```

## Accessibility

- The component adds no ARIA role. The icon and the image are decorative, and the title is plain text, not a heading.
- When the empty state replaces results after a search, put it in a region with `role="status"` so screen readers announce the change.
