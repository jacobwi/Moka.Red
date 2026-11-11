---
title: Data List
description: Definition/description list for displaying labeled key-value data.
order: 42
---

# Data List

`MokaDataList` renders labelled facts as a definition list: a `<dl>` with each term and value in a `<div>`. It stacks terms over values or sets them side by side, with stripes, lines between rows, dense spacing, columns and rich values, in one of six looks. For a single label and value on its own, use [`MokaKeyValue`](/components/key-value).

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IReadOnlyList<MokaDataListItem>?` | -- | Rows to render. Set `ValueContent` on an item for rich values |
| `ChildContent` | `RenderFragment?` | -- | Your own pairs, used when `Items` is null: each a `<div>` holding a `<dt>` and a `<dd>` |
| `Look` | `MokaDataListLook` | `Standard` | How the pairs are drawn: `Standard`, `SettingsRows`, `InsetPanel`, `FactTiles`, `SpecSheet` or `Terminal` |
| `TermStyle` | `MokaDataListTermStyle` | `Standard` | How the terms weigh against the values: `Standard`, `Quiet`, `Micro` or `Strong`. Fact tiles and the terminal ignore it |
| `CopyStyle` | `MokaDataListCopyStyle` | `Faint` | How a copyable value shows it can be copied: `Faint`, `Always` or `ClickValue` |
| `Orientation` | `MokaDirection` | `Column` | `Column` stacks each term over its value, `Row` sets them side by side. Only the `Standard` look reads it |
| `Striped` | `bool` | `false` | Shades every other row |
| `Bordered` | `bool` | `false` | Draws a line between rows, or outlines each fact tile |
| `Dense` | `bool` | `false` | Reduces padding for compact display |
| `EmptyText` | `string?` | -- | Shown in a muted color for an item with nothing to show, such as "Not set". The terminal reads "not set" without it |
| `TermWidth` | `string?` | -- | The width of every term, so the values line up: in a `Row` list and in every look but the tiles. A length or a `var()` |
| `Columns` | `int` | `1` | Lays the items out in up to this many columns, as many as fit |
| `MinColumn` | `string?` | `16rem` | The narrowest a column gets when `Columns` is above 1, and the narrowest a fact tile gets (`11rem` there). A length or a `var()` |
| `OnItemContextMenu` | `EventCallback<MokaItemContextMenuArgs<MokaDataListItem>>` | -- | Raised when a row is right-clicked, with the item and the mouse event |
| `CopyLabel` | `Func<MokaDataListItem, string>?` | -- | Names each copy button and its tooltip. Defaults to "Copy " and the item's `Label` |
| `CopiedText` | `string` | `"Copied"` | Shown above a copy button after a copy |
| `CopyFailedText` | `string` | `"Couldn't copy"` | Shown above a copy button when the browser refused the copy |
| `Size` | `MokaSize` | `Md` | Text size: `Xs`, `Sm`, `Md`, `Lg` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

Other attributes land on the `<dl>`, and so do `Margin`, `Padding` and `Rounded`.

### MokaDataListItem

A positional record, `new MokaDataListItem(label, value, valueContent)`, with more options as `init` properties.

| Name | Type | Description |
|------|------|-------------|
| `Label` | `string` | The term |
| `Value` | `string?` | Plain text value |
| `ValueContent` | `RenderFragment?` | Rich content for the value (takes precedence over `Value`) |
| `Icon` | `MokaIconDefinition?` | An icon before the term, hidden from screen readers |
| `Help` | `string?` | A line under the term saying what it's for. A theme with descriptions turned off hides it |
| `Mono` | `bool` | Draws the value in the mono font with even-width digits |
| `Copyable` | `bool` | Lets the value be copied, drawn as the list's `CopyStyle` says. By default a quiet copy button after the value, named "Copy " and the `Label` |
| `CopyText` | `string?` | What the copy button copies, when it differs from `Value` |
| `EndContent` | `RenderFragment?` | Controls at the row's end, after the value |
| `Wide` | `bool` | Takes a whole row in a list laid out in `Columns` or as fact tiles, for a long value such as a key |

## Basic Vertical List

```blazor-preview
<MokaDataList Items="profileItems" />

@code {
    IReadOnlyList<MokaDataListItem> profileItems = new[]
    {
        new MokaDataListItem("Name", "Jane Doe"),
        new MokaDataListItem("Email", "jane@example.com"),
        new MokaDataListItem("Role", "Administrator"),
        new MokaDataListItem("Joined", "March 2024")
    };
}
```

## Horizontal Layout

Each term is at least 8rem wide. When there isn't 8rem left beside it, the value moves under its term, so a narrow card never cuts it off. A long value with no spaces, such as a key, breaks wherever it has to.

```blazor-preview
<MokaDataList Items="serverInfo" Orientation="MokaDirection.Row" />

@code {
    IReadOnlyList<MokaDataListItem> serverInfo = new[]
    {
        new MokaDataListItem("Status", "Running"),
        new MokaDataListItem("Uptime", "14 days"),
        new MokaDataListItem("Region", "US-East")
    };
}
```

## Term Width

A term longer than 8rem pushes its own value right. `TermWidth` gives every term the same width so the values line up in one column, and a longer term wraps.

```razor
<MokaDataList Items="settings" Orientation="MokaDirection.Row" TermWidth="10rem" />
```

## Striped

```blazor-preview
<MokaDataList Items="specItems" Striped="true" />

@code {
    IReadOnlyList<MokaDataListItem> specItems = new[]
    {
        new MokaDataListItem("CPU", "8 cores"),
        new MokaDataListItem("Memory", "32 GB"),
        new MokaDataListItem("Storage", "1 TB SSD"),
        new MokaDataListItem("Network", "10 Gbps")
    };
}
```

## Bordered

```blazor-preview
<MokaDataList Items="orderItems" Bordered="true" />

@code {
    IReadOnlyList<MokaDataListItem> orderItems = new[]
    {
        new MokaDataListItem("Order #", "ORD-2024-1234"),
        new MokaDataListItem("Status", "Shipped"),
        new MokaDataListItem("Total", "$149.99")
    };
}
```

## Dense

```blazor-preview
<MokaDataList Items="denseItems" Dense="true" Bordered="true" />

@code {
    IReadOnlyList<MokaDataListItem> denseItems = new[]
    {
        new MokaDataListItem("Version", "0.1.3"),
        new MokaDataListItem("Framework", ".NET 10"),
        new MokaDataListItem("License", "MIT"),
        new MokaDataListItem("Size", "42 KB")
    };
}
```

## Columns

`Columns` lays the items out in up to that many columns, each at least `MinColumn` wide, so the list's own width decides how many fit and a phone gets one. With `Bordered` the lines run under every row but the last, however many columns fit. `Striped` shades every other item, which reads best in one column.

```razor
<MokaDataList Items="facts" Columns="3" MinColumn="12rem" Bordered />
```

An item with `Wide` takes a whole row, so a long key doesn't squeeze into one column.

## Looks

`Look` changes how the pairs are drawn. Every look keeps the `<dl>`, `<dt>` and `<dd>`, so a screen reader reads each term with its value whichever is picked, and every look works on the original design and the new one.

| Look | What it is |
|------|------------|
| `Standard` | The list as it has always been. `Orientation` decides whether a value sits under its term or beside it |
| `SettingsRows` | The new design's own sizes: a muted 13 px term in a 9.5rem column, the value beside it at 14 px, room above and below, hairlines between. On a phone the value moves under its term |
| `InsetPanel` | The list in a panel of its own, terms on the left and values on the right, like a phone's settings. Reads as one block on a page with several lists |
| `FactTiles` | Each pair a small tile, an uppercase micro label over a bigger value, as many tiles to a row as fit. For a summary at the top of a page |
| `SpecSheet` | Dotted leaders from each term to its value on the right, with even-width digits, like a printed spec sheet |
| `Terminal` | Everything in mono: lowercase keys with a colon in a fixed column, an accent edge, square corners. An empty value reads "- not set", and icons and help are left out |

```razor
<MokaDataList Items="device" Look="MokaDataListLook.SettingsRows" />
<MokaDataList Items="device" Look="MokaDataListLook.FactTiles" />
<MokaDataList Items="device" Look="MokaDataListLook.Terminal" />

@code {
    IReadOnlyList<MokaDataListItem> device =
    [
        new("Name", "Living room TV") { Icon = MokaIcons.Content.Tv },
        new("Address", "192.168.1.24") { Icon = MokaIcons.Content.Globe, Mono = true, Copyable = true },
        new("Owner") { Icon = MokaIcons.Content.User },
        new("SSH key", "ssh-ed25519 AAAAC3Nza...") { Mono = true, Copyable = true, Wide = true }
    ];
}
```

What each look does with the other options:

- **Orientation** applies to `Standard` only. The other looks arrange their pairs themselves, and a value moves under its term when there isn't room beside it.
- **Size** applies to `Standard` only, since each look sets its own type.
- **Bordered** draws lines between rows. `SettingsRows` and `InsetPanel` always have them; on `FactTiles` it outlines each tile.
- **Striped** shades every other row. Tiles already carry a background each, so it changes nothing there.
- **Dense** tightens each look's rows, and the gap between tiles.
- **TermWidth** sets the term column of every look but the tiles.
- **Columns** and `MinColumn` lay rows out in columns; on `FactTiles` they cap how many tiles sit in a row and set the narrowest tile.
- **EmptyText** fills an empty value in every look. The terminal reads "not set" without it.

## Term Styles

`TermStyle` sets how the terms weigh against their values. It works with every look but `FactTiles` and `Terminal`, which have their own.

| Style | What it does |
|-------|--------------|
| `Standard` | The look's own term |
| `Quiet` | Regular weight and a muted color, so the value is what the eye lands on |
| `Micro` | The small uppercase label used over groups. Distinct at a glance, louder in a long list |
| `Strong` | The term in semibold full color and the value muted, the way a glossary reads |

```razor
<MokaDataList Items="device" Look="MokaDataListLook.SettingsRows" TermStyle="MokaDataListTermStyle.Quiet" />
```

## Icons, Help and Mono Values

```razor
<MokaDataList Items="device" Orientation="MokaDirection.Row" />

@code {
    IReadOnlyList<MokaDataListItem> device =
    [
        new("Name", "Living room TV") { Icon = MokaIcons.Content.Tv },
        new("Address", "192.168.1.24") { Icon = MokaIcons.Content.Globe, Mono = true },
        new("Wi-Fi key", "correct horse") { Icon = MokaIcons.Content.Key, Help = "Shared by every device on this network." }
    ];
}
```

`Help` carries the `moka-about` class, so `MokaTheme.WithDescriptions(false)` hides it along with the rest of the text that explains what things are for.

## Copyable Values

`Copyable` puts a quiet copy button after the value. It stays faint until the row is hovered, and is named after the term, so a list of them isn't a row of buttons all called "Copy". Set `CopyText` when what's copied differs from what shows.

```razor
<MokaDataList Items="keys" />

@code {
    IReadOnlyList<MokaDataListItem> keys =
    [
        new("Address", "192.168.1.24") { Mono = true, Copyable = true },
        new("API key", "sk-live-...cdef") { Mono = true, Copyable = true, CopyText = _fullKey }
    ];
}
```

`CopyStyle` changes how that shows:

| Style | What it does |
|-------|--------------|
| `Faint` | The quiet button after the value, faint until its row is hovered or it has focus |
| `Always` | A small outlined button that's always there, so nobody has to find it. Better on touch screens, busier on a long list |
| `ClickValue` | The value itself is the button. Hovering raises it and shows a copy icon, a click or Enter copies, and a "Copied" tag answers. Its name is "Copy ", the term and the value, and a hidden status says it was copied |

```razor
<MokaDataList Items="keys" CopyStyle="MokaDataListCopyStyle.ClickValue" />
```

A value that holds a link or another control can't sit inside a button, so keep those lists on `Faint` or `Always`. In a disabled list the values are disabled buttons.

`CopyLabel`, `CopiedText` and `CopyFailedText` replace the words, for another language:

```razor
<MokaDataList Items="keys" CopyLabel="@(item => $"{item.Label} kopieren")" CopiedText="Kopiert" CopyFailedText="Nicht kopiert" />
```

## Empty Values

An item with no value renders an empty `<dd>`. `EmptyText` fills it in a muted color instead. The `Terminal` look reads "not set" (`MokaDataList.DefaultTerminalEmptyText`) unless `EmptyText` says otherwise.

```razor
<MokaDataList Items="contact" EmptyText="Not set" />
```

## Actions at the Row's End

`EndContent` sits after the value, at the end of the row.

```razor
<MokaDataList Items="secrets" Orientation="MokaDirection.Row" />

@code {
    bool _shown;

    IReadOnlyList<MokaDataListItem> secrets =>
    [
        new("Wi-Fi key", _shown ? "correct horse battery staple" : "********")
        {
            Mono = true,
            EndContent = @<MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm" OnClick="() => _shown = !_shown">@(_shown ? "Hide" : "Show")</MokaButton>
        }
    ];
}
```

## Right-Click

`OnItemContextMenu` carries the item and the mouse event, so a context menu can open at the pointer. The browser's own menu is cancelled only while something listens.

```razor
<MokaDataList Items="device" OnItemContextMenu="a => Menu.Show(a.MouseEvent, ItemsFor(a.Item))" />
```

## Rich Content

Give an item a `ValueContent` render fragment for anything richer than text. A Razor template (`@<...>`) builds one inline.

```blazor-preview
<MokaDataList Items="richItems" />

@code {
    IReadOnlyList<MokaDataListItem> richItems =
    [
        new("User", ValueContent: @<MokaFlexbox Direction="MokaDirection.Row" Align="MokaAlign.Center" Gap="MokaSpacingScale.Xs">
            <MokaAvatar Initials="JD" Size="MokaSize.Sm" />
            <MokaText>Jane Doe</MokaText>
        </MokaFlexbox>),
        new("Status", ValueContent: @<MokaBadge Content="Active" Color="MokaColor.Success" />),
        new("Tags", ValueContent: @<MokaFlexbox Direction="MokaDirection.Row" Gap="MokaSpacingScale.Xxs">
            <MokaChip Text="Admin" Size="MokaSize.Sm" />
            <MokaChip Text="Editor" Size="MokaSize.Sm" />
        </MokaFlexbox>)
    ];
}
```

## Your Own Pairs

Without `Items`, write the pairs yourself, each a `<div>` holding a `<dt>` and a `<dd>`. They take the list's styles, `Look`, `TermStyle`, `Orientation`, `Striped`, `Bordered` and `Dense` included. Give a pair's `<div>` the class `moka-data-list-entry--wide` to make it `Wide`.

```razor
<MokaDataList Orientation="MokaDirection.Row" Striped>
    <div><dt>Name</dt><dd>Jane Doe</dd></div>
    <div><dt>Role</dt><dd><MokaTag Text="Administrator" /></dd></div>
</MokaDataList>
```

## Groups

A `<dl>` can't hold a heading, so a list in groups is one `MokaDataList` per group, each under its own heading:

```razor
<MokaCaption Micro>Network</MokaCaption>
<MokaDataList Items="network" />
<MokaCaption Micro>Hardware</MokaCaption>
<MokaDataList Items="hardware" />
```

## Accessibility

The list is a `<dl>`, and each term and value pair sits in a `<div>`, which HTML allows, so a screen reader reads every term with its value, in every look. Icons are hidden from screen readers; the term names the value. A copy button is named "Copy " and the term; with `ClickValue` the value's button is named "Copy ", the term and the value, Tab reaches it and Enter or Space copies. Every copy says "Copied" in a hidden status region.
