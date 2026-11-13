---
title: List
description: Vertical list of rows with icons, secondary text, end content, links and keyboard-accessible click handling.
order: 87
---

# List

`MokaList` is a vertical list of `MokaListItem` rows. A row shows an optional icon, a primary and a secondary line of text, and trailing content. It can be static, a link (`Href`) or clickable (`OnClick`).

## MokaList Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | The `MokaListItem` rows |
| `Dense` | `bool` | `true` | Tighter vertical padding |
| `Bordered` | `bool` | `false` | Border and rounded corners around the list |
| `Hoverable` | `bool` | `true` | Highlights rows on hover |
| `RowStyle` | `MokaListRowStyle` | `Standard` | `Tree` sizes the rows like `MokaTree`'s, for a list that sits with a tree in one sidebar. See [Rows that match a tree](#rows-that-match-a-tree) |

## MokaListItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `string?` | -- | Primary line |
| `SecondaryText` | `string?` | -- | Smaller second line |
| `Icon` | `MokaIconDefinition?` | -- | Leading icon |
| `IconContent` | `RenderFragment?` | -- | Anything in the leading icon's place, such as an emoji, a small picture or a `MokaIdenticon`, in the icon's box and spacing. Wins over `Icon`; hidden from screen readers like the icon |
| `IconColor` | `string?` | -- | The leading icon's color: hex, a keyword or a color function such as `var()` |
| `Label` | `MokaLabelColor?` | -- | A color someone gave the item. By default the icon, or `IconContent`, takes it, and an item with no icon gets a dot before its name. `IconColor` wins on the icon |
| `LabelMark` | `MokaLabelMark` | `Icon` | How `Label` shows: `Icon`, `TintedIcon` (also a soft fill inside an outline icon) or `Dot` (a dot before the name, the icon keeps its color) |
| `EndIcon` | `MokaIconDefinition?` | -- | Trailing icon, shown when `EndContent` is not set |
| `EndContent` | `RenderFragment?` | -- | Trailing content such as a badge, switch or button |
| `ChildContent` | `RenderFragment?` | -- | Extra content under the text |
| `Href` | `string?` | -- | Renders the row as a link. A `javascript:`, `vbscript:` or `data:` URL is not rendered: the row stays a plain row |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Makes the row clickable |
| `OnContextMenu` | `EventCallback<MouseEventArgs>` | -- | Right-click handler. Suppresses the browser's own menu |
| `Active` | `bool` | `false` | Selected styling |
| `Divider` | `bool` | `false` | Line under the row |
| `Disabled` | `bool` | `false` | Dims the row and ignores clicks and keys |

## Basic List

```blazor-preview
<MokaList Bordered>
    <MokaListItem Text="Inbox" SecondaryText="5 unread messages" Icon="MokaIcons.Content.Filter" />
    <MokaListItem Text="Drafts" SecondaryText="2 drafts" Icon="MokaIcons.File.Document" Divider />
    <MokaListItem Text="Sent" Icon="MokaIcons.Navigation.ArrowRight" />
    <MokaListItem Text="Trash" Icon="MokaIcons.Action.Delete" Disabled />
</MokaList>
```

## Active Row and End Content

```blazor-preview
<MokaList Bordered>
    <MokaListItem Text="Dashboard" Icon="MokaIcons.Navigation.Home" Active />
    <MokaListItem Text="Settings" Icon="MokaIcons.Action.Settings" EndIcon="MokaIcons.Navigation.ChevronRight" />
    <MokaListItem Text="Profile" Icon="MokaIcons.Toggle.Eye">
        <EndContent>
            <MokaAttribute Size="MokaSize.Xs" Color="MokaColor.Success" Variant="MokaVariant.Soft">Online</MokaAttribute>
        </EndContent>
    </MokaListItem>
</MokaList>
```

## Anything Where the Icon Sits

`IconContent` takes the leading icon's place at the same size and spacing, for an emoji, a small picture or a
`MokaIdenticon`.

```razor
<MokaList>
    <MokaListItem Text="Reading list" Icon="MokaIcons.File.Folder" />
    <MokaListItem Text="Garden plans">
        <IconContent>&#x1F331;</IconContent>
    </MokaListItem>
    <MokaListItem Text="Dana Kim" SecondaryText="dana@example.com">
        <IconContent><MokaIdenticon Value="dana@example.com" IdenticonSize="16" /></IconContent>
    </MokaListItem>
</MokaList>
```

## Label Colors

`Label` takes a [label color](/getting-started/label-colors), for lists, libraries and folders people color themselves. An item with an icon shows the label on its icon; one without gets a small dot before its name, the quietest way to show it. `LabelMark` picks: `Icon`, `TintedIcon` for the look of a colored folder, or `Dot`, which keeps the icon's own color.

```razor
<MokaList>
    <MokaListItem Text="Reading list" Label="MokaLabelColor.Indigo" />
    <MokaListItem Text="Garden plans" Label="MokaLabelColor.Orange" />
    <MokaListItem Text="Photos" Icon="MokaIcons.File.Folder" Label="MokaLabelColor.Blue" LabelMark="MokaLabelMark.TintedIcon" />
</MokaList>
```

## Rows That Match a Tree

A sidebar often has a list of places above a tree of folders. `RowStyle="MokaListRowStyle.Tree"` sizes the list's rows like `MokaTree`'s: its text size and regular weight, its smaller icon, its row height and rounded hover, and its indent, the icon set in past the tree's toggle column. The list's icons and names then line up with the tree's top level, and the two read as one sidebar. The current row looks like the tree's selected row. `Dense` doesn't apply to these rows.

```razor
<MokaList RowStyle="MokaListRowStyle.Tree">
    <MokaListItem Text="Recent" Icon="MokaIcons.Action.History" Active OnClick="ShowRecent" />
    <MokaListItem Text="Starred" Icon="MokaIcons.Toggle.Star" OnClick="ShowStarred" />
</MokaList>
<MokaCaption Micro>Libraries</MokaCaption>
<MokaTree>
    <MokaTreeItem Text="Photos" Icon="MokaIcons.Content.Book" />
    <MokaTreeItem Text="Work" Icon="MokaIcons.Content.Book" />
</MokaTree>
```

## Clickable Rows

Rows with `OnClick` work from the keyboard. Tab moves between them, and Enter or Space activates the focused row, the same as a button.

```blazor-preview
@code {
    string _selected = "none";
}

<MokaList Bordered>
    <MokaListItem Text="Inbox" Icon="MokaIcons.Content.Filter" OnClick="@(() => _selected = "Inbox")" />
    <MokaListItem Text="Drafts" Icon="MokaIcons.File.Document" OnClick="@(() => _selected = "Drafts")" />
    <MokaListItem Text="Trash" Icon="MokaIcons.Action.Delete" Disabled OnClick="@(() => _selected = "Trash")" />
</MokaList>

<p>Selected: @_selected</p>
```

A click on a button inside a row also reaches the row's `OnClick`, whether it came from the mouse or the keyboard. Stop it at the button when the two do different things:

```razor
<MokaListItem Text="Drafts" OnClick="OpenDrafts">
    <EndContent>
        <span @onclick:stopPropagation="true">
            <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" OnClick="ArchiveDrafts">Archive</MokaButton>
        </span>
    </EndContent>
</MokaListItem>
```

## Links

`Href` renders the row as an anchor, so it keeps the browser's link behavior: Enter follows it, and middle-click opens it in a new tab.

```blazor-preview
<MokaList Bordered>
    <MokaListItem Text="Getting started" Href="#" Icon="MokaIcons.File.Document" />
    <MokaListItem Text="Components" Href="#" Icon="MokaIcons.Navigation.Home" />
</MokaList>
```

## Accessibility

- The list has `role="list"` and each row has `role="listitem"`. A link row keeps its link role: the listitem role sits on a wrapper around the `<a>`.
- Rows with `OnClick` or `OnContextMenu` join the tab order and show the focus ring. Static and disabled rows stay out of it.
- Enter and Space only act on the focused row itself. Keys pressed in a button or an input inside a row are left alone, and Space does not scroll the page.
- The keys are handled by the `MokaList`, with one listener for all of its rows, so a clickable `MokaListItem` needs a `MokaList` parent. On its own it does not take focus.
- The menu key or Shift+F10 on a focused row fires `OnContextMenu`.
