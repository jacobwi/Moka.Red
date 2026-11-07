---
title: Page Title
description: Pages say their name once, one host writes the tab's title, and the app can put something in front of it.
order: 140
---

# Page Title

With a `PageTitle` on each page the last one rendered wins, so an app can't put anything in front of it. Here a page says its name with `MokaPageTitle`, and one `MokaPageTitleHost` in the layout writes the title. That lets the app write `(2) Devices` while two things need someone, or `Keep? 0:42 · Devices` while a change waits, and a page opened meanwhile can't hide it.

`MokaTabPresence` is here too: it tells the app when the tab has been out of sight for a while.

## Setup

```razor
@using Moka.Red.Core.PageTitle
```

`AddMokaPageTitles()` registers `IMokaPageTitleService`, from `Moka.Red.Core.Extensions`:

```csharp
builder.Services.AddMokaPageTitles();
```

## Parameters

### MokaPageTitle

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `string` | **required** | The page's name |

It renders nothing.

### MokaPageTitleHost

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Prefix` | `string?` | - | Goes in front of the page's name, with a space between: `(2)` makes `(2) Devices` |
| `Default` | `string?` | - | The title while no page has said one. Left out, the title stays as the host page wrote it |

### MokaTabPresence

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Hidden` | `EventCallback` | - | Raised once the tab has been out of sight for `HiddenAfter` |
| `Visible` | `EventCallback` | - | Raised when the tab comes back, only if `Hidden` was raised |
| `HiddenAfter` | `TimeSpan` | 1 minute | How long the tab stays out of sight before `Hidden`. A look at another tab says nothing |

## In the layout

Place one host in the layout, and let the app decide the prefix.

```razor
<MokaPageTitleHost Prefix="@Prefix" Default="MokaGate"/>

@Body

@code {
    private string? Prefix => _needsYou > 0 ? $"({_needsYou})" : null;
}
```

## On a page

```razor
@page "/devices"

<MokaPageTitle Text="Devices"/>
```

A name can come from data, and the title follows it:

```razor
<MokaPageTitle Text="@_device.Name"/>
```

When two say a name at once, such as a page and something opened over it, the last one counts. When that one goes, the one before it counts again.

## Letting refreshes rest

```razor
<MokaTabPresence Hidden="() => _refreshing = false" Visible="ResumeAsync"/>
```

It uses the Page Visibility API, and says nothing to .NET until the wait is over. `Visible` only follows a `Hidden`, so a quick look at another tab costs nothing.

## From code

`IMokaPageTitleService` is what the two talk through. `Say(owner, text)` says a name, `Forget(owner)` takes it back, `Text` is the name that counts, and `OnChanged` is raised when it changes.
