---
title: App Shell
description: The frame around every page - side menu, icon rail, main area, and on a phone a top bar with the menu in a drawer.
order: 125
---

# App Shell

`MokaAppShell` is the frame around every page of an app. It draws a side menu and a main area. Below 1280 px the menu can shrink to an icon rail, and below 720 px it moves into a drawer behind a 52 px top bar. `MokaNavEntry` defines the menu once, and `MokaAccountCard` is the card pinned under it.

## Setup

```razor
@using Moka.Red.Navigation.AppShell
```

The shell needs an interactive layout. Put one `MokaHotkeyHost` in that layout for the `[` key. `AddMokaRed()` registers the service it needs, and without a host everything else still works.

## Example

```razor
<MokaThemeProvider>
    <MokaHotkeyHost/>
    <MokaAppShell Entries="Entries" @bind-Rail="_rail">
        <Brand>
            <img src="logo.svg" alt="" width="30" height="30"/>
            <span class="moka-app-shell__brand-text">
                <span class="moka-app-shell__brand-name">Moka</span>
                <span class="moka-app-shell__brand-line">home router</span>
            </span>
        </Brand>
        <Status>
            <MokaStatusDot Color="MokaColor.Success" Uppercase="false" Label="All good"/>
        </Status>
        <Account>
            <MokaAccountCard Name="BC" Detail="Owner" Items="AccountItems" OnItemClick="PickAsync"/>
        </Account>
        <TopBarEnd>
            <MokaTooltip Text="Search" Position="MokaTooltipPosition.BottomEnd">
                <MokaButton Quiet StartIcon="MokaIcons.Action.Search" aria-label="Search" OnClick="Palette.Open"/>
            </MokaTooltip>
        </TopBarEnd>
        <ChildContent>
            @Body
        </ChildContent>
    </MokaAppShell>
</MokaThemeProvider>
```

## MokaAppShell Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | The page. A flex column with a 16 px gap and `--moka-main-padding` around it, and 16 px at the sides on a phone. See [A page that fills the main area](#a-page-that-fills-the-main-area) |
| `Entries` | `IReadOnlyList<MokaNavEntry>` | empty | The screens, in order. Groups show in the order they first appear |
| `Brand` | `RenderFragment?` | -- | The logo and name over the menu. See [Brand](#brand) |
| `Status` | `RenderFragment?` | -- | A word on how things are, under the name in the phone's drawer. It replaces the mono line there, and doesn't show on wider screens |
| `Account` | `RenderFragment?` | -- | What's pinned under the list, usually a `MokaAccountCard` |
| `TopBarEnd` | `RenderFragment?` | -- | The right end of the phone's top bar, such as a search button |
| `TopBarTitle` | `string?` | -- | The name in the phone's top bar. Null takes it from the entry the address belongs to |
| `Rail` | `MokaRailChoice` | `Automatic` | `Expanded`, `Collapsed`, or `Automatic`, which is the rail below 1280 px. A new value also forgets a flip made with the `[` key |
| `RailChanged` | `EventCallback<MokaRailChoice>` | -- | Raised when the person flips the rail, and on first render when an earlier flip in this tab is still kept |
| `NavLabel` | `string` | `"Main"` | The name of the navigation, for screen readers |
| `OpenMenuLabel` | `string` | `"Open the menu"` | The name of the top bar's menu button |
| `CloseMenuLabel` | `string` | `"Close the menu"` | The name of the drawer's close button |
| `RailLabels` | `bool` | `false` | The rail shows each screen's name under its icon, and is 80 px wide. See [Names on the rail](#names-on-the-rail) |
| `Nonce` | `string?` | -- | The nonce of a strict Content-Security-Policy, for the script that sets the rail before first paint. Only a static render (prerendering) writes that script, so only it needs the nonce; an interactive render writes no script. Without it such a policy blocks the script, and the rail settles a moment later |

`FlipRailAsync()` flips the rail from your own button, as the `[` key does, and `CloseDrawerAsync()` closes the drawer.

## Defining the menu

`MokaNavEntry` is a plain record, so one list can feed the menu, the phone's title, a page's eyebrow and the command palette.

```csharp
private static readonly MokaNavEntry[] Entries =
[
    new("", "Home", "/", MokaIcons.Navigation.Home),
    new("Network", "Devices", "/devices", MokaIcons.Content.Columns, "names pause block",
        new MokaNavPill(14, MokaBadgeTone.Count, "14 devices")),
    new("System", "Updates", "/updates", MokaIcons.Action.Download, "version upgrade",
        new MokaNavPill(3, MokaBadgeTone.Ready, "3 versions ready to install"))
];
```

| Member | Type | Description |
|--------|------|-------------|
| `Group` | `string` | The group it's listed under. Empty puts it above the named groups, with no heading |
| `Label` | `string` | The screen's name |
| `Route` | `string` | The page's route, like `/devices`. `/devices/tv` counts as Devices, and the longest route wins |
| `Icon` | `MokaIconDefinition` | Its icon, which is all the rail shows of it |
| `Keywords` | `string` | More words a search finds it by. The menu doesn't read them |
| `Pill` | `MokaNavPill?` | The number it shows right now, if any |
| `Href` | `string` | The route as a link relative to the page's base address |
| `Bottom` | `bool` | Set with `{ Bottom = true }`: listed at the bottom of the menu, under the list that scrolls and above the account card, such as Settings. Its group isn't shown there |

`MokaNavEntry.Find(entries, path)` gives the entry a path belongs to, for a page's eyebrow or your own title.

The same list feeds the command palette:

```csharp
Palette.RegisterMany(Entries.Select(entry => new MokaCommand
{
    Id = $"go-{entry.Route}",
    Title = entry.Label,
    Description = entry.Group,
    Group = "Go to",
    Href = entry.Route,
    Keywords = entry.Keywords
}));
```

### Pills

`MokaNavPill(Count, Tone, Why)` is a number with a meaning, and the tone uses `MokaBadgeTone`: `Count` is grey, `Limited` amber, `NeedsYou` red and `Ready` blue. Show one only when it means something, and build the entry again with `with { Pill = ... }` when the number changes.

Beside a name it's a `MokaBadge`. On the rail it's a 15 px corner badge. Screen readers hear `Why`, and the rail's tag shows it.

## The rail

The rail is 68 px wide, and the full menu 248 px. On the rail each row shows its icon, a group's name becomes a line, and hovering or tabbing to a row shows its name and its pill's reason in a tag beside it. The tag is a `MokaTooltip`, so the scrolling list doesn't cut it off.

### Names on the rail

`RailLabels` puts each screen's name under its icon, in 11 px on one line, and widens the rail to 80 px. A name that doesn't fit is cut with an ellipsis. The tag beside an item then shows only for a pill's reason or a cut name: the shell measures the names in the browser and marks the cut ones.

```razor
<MokaAppShell Entries="Entries" RailLabels>
```

How the current page is drawn is the theme's choice, not the shell's: `MokaTheme.WithCurrentPlace(MokaCurrentPlace.Solid)` fills it with the accent and a soft glow, in the rail, the full menu, the account card and a settings list alike. See [Theming](../getting-started/theming.md#style). Settings fits at the bottom of the rail with `Bottom` on its entry.

`[` outside a text field flips the rail for the tab and keeps that in `sessionStorage`. The shell registers the key with `IMokaHotkeyService`, so a `MokaCheatsheet` lists it too.

Which one to draw is settled in the browser, and a window crossing 1280 px never reaches .NET. Both drawings are in the markup, and script sets one attribute on `<html>`, `data-moka-rail`. A style of your own can read it on any ancestor: `[data-moka-rail] .my-thing`.

When the shell is prerendered, a small script in the static page sets the attribute before first paint, so the page doesn't jump. It sets it on `<html>` because Blazor replaces the prerendered shell with a new element when the page goes interactive, and leaves `<html>` alone, so the rail holds across that handoff. An interactive render writes no script at all: Blazor would create it by setting its text, which a Trusted Types policy (`require-trusted-types-for 'script'`) blocks, and the attribute is already set. With no prerendering (a WebAssembly app with an `index.html`, or MAUI), the shell's module sets the attribute after the first render, so on a window between 720 and 1280 px the full menu shows for a moment before the rail, as it does under a strict policy without a nonce. The shell then works under Trusted Types and a CSP with no `'unsafe-inline'`, and needs no nonce.

## A page that fills the main area

A page such as a file explorer, with scrolling parts of its own, wants the main area to itself rather than a page that
grows. Put `moka-fill-main` on the page's root element, directly inside the shell. The main column is then as tall as
the window, under the phone's top bar or a fixed app bar, and the page grows to fill it. The room the shell keeps under
a page for a floating bar (120 px of the new design's `28px 32px 120px`) drops to the top padding, plus the height of an
`MokaActionBar` only while one is showing.

```razor
<div class="explorer moka-fill-main">
    <ExplorerToolbar />
    <FileList style="flex: 1; min-height: 0; overflow: auto" />
</div>
```

`--moka-main-padding` is a shorthand, so the shell also writes its two ends, `--moka-main-padding-top` and
`--moka-main-padding-bottom`, for a page that works out a height of its own:
`height: calc(100dvh - var(--moka-main-padding-top) - var(--moka-main-padding-bottom))`.

## On a phone

At 720 px and under, the side menu is a drawer behind a sticky 52 px top bar. The drawer keeps the brand, the status and the account pinned while the list scrolls. It closes on a pick, the scrim, Esc or a swipe left, keeps Tab inside and holds the page still. The top bar reports its height as `--moka-appbar-space`, as a fixed `MokaAppBar` does, so a link to part of a page lands below it and your own sticky parts can keep clear of it (see [Fixed bars and links](../layout/toolbar-appbar.md#fixed-bars-and-links)).

The top bar already names the page, so a page can hide its own title there with `MokaPageHeader`'s `TitleInTopBar`. It stays for screen readers.

```razor
<MokaPageHeader Title="Devices" TitleInTopBar Said="14 known, 9 online."/>
```

## Brand

`Brand` is yours to fill. These classes draw the look on the elements you give them:

| Class | What it does |
|-------|--------------|
| `moka-app-shell__brand-text` | Holds the words. Hidden on the rail, which leaves the logo |
| `moka-app-shell__brand-name` | The name, in the display font at 18 px and 700 |
| `moka-app-shell__brand-line` | The mono 11 px line under the name. On a phone `Status` takes its place |

On a phone `Status` sits under the name, lined up for a 30 px logo.

## MokaAccountCard

Who's signed in, as a card that opens a menu: above it in the side menu and the drawer, beside it on the rail. Arrows, Home and End move through the menu, Esc closes it and goes back to the card, and Tab closes it and goes on. A pick also closes the drawer.

The open menu sits in the browser's top layer, so the side menu's overflow doesn't cut it off and nothing on the page paints over it. Above the card it's as wide as the card, and beside the rail it keeps its own width. Rail or not is read from the browser when the menu opens, so a menu left open while the rail flips keeps its side until it closes. While it's open the library sets its position, so a stylesheet can restyle it but not move it.

```razor
<MokaAccountCard Name="BC" Detail="Owner" Heading="BC, owner" Items="AccountItems" OnItemClick="PickAsync"/>

@code {
    private static readonly MokaAccountItem[] AccountItems =
    [
        new("Your settings", MokaIcons.Action.Settings, "/settings"),
        MokaAccountItem.Divider,
        new("Sign out", MokaIcons.Navigation.ArrowRight)
    ];
}
```

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Name` | `string` | `""` | The person's name |
| `Detail` | `string?` | -- | A line under the name, such as their role. Hidden on the rail |
| `Initials` | `string?` | -- | The letters in the avatar. Null takes them from `Name` |
| `AvatarSrc` | `string?` | -- | A picture for the avatar, instead of the letters |
| `Heading` | `string?` | -- | The line over the rows |
| `Items` | `IReadOnlyList<MokaAccountItem>` | empty | The rows, in order |
| `OnItemClick` | `EventCallback<MokaAccountItem>` | -- | Raised when the person picks a row, a link or not |
| `MenuLabel` | `string` | `"Account menu"` | What the card does, said to screen readers after the name |
| `Disabled` | `bool` | `false` | The card can't be opened |

`MokaAccountItem(Text, Icon, Href, Danger, Active, Disabled, Checked)` is one row. With `Href` it's a link. `Danger` turns it red when hovered or focused, `Active` marks the page showing now (and the card itself is then drawn as the current place, like a menu's current page), and `Disabled` makes the arrow keys pass over it. `Checked` makes it a setting that turns on or off, which leaves the menu open. `MokaAccountItem.Divider` is a line between groups.
