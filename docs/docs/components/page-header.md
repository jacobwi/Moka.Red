---
title: Page Header
description: What a page starts with - its title, what is so right now, what the page is for, and its actions.
order: 121
---

# Page Header

`MokaPageHeader` is the top of a page. It keeps two kinds of text apart: `Said` is what is so right now, and always shows. `Lead` is what the page is for, and a person who knows the app can hide it.

## Setup

```razor
@using Moka.Red.Layout.PageHeader
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string?` | - | The page's name, drawn as its one `h1` |
| `TitleContent` | `RenderFragment?` | - | Instead of `Title`, for a title with controls beside it. Include the `h1`: any `h1` in here takes the title's look |
| `Eyebrow` | `string?` | - | The small line above the title. It hides on a phone unless it links back |
| `EyebrowHref` | `string?` | - | Makes `Eyebrow` a way back, like "Devices" over one device. `javascript:`, `vbscript:` and `data:` addresses are dropped |
| `Said` | `string?` | - | What is so right now, like "12 known, 8 online". It always shows |
| `Lead` | `string?` | - | What the page is for. `MokaTheme.WithDescriptions(false)` hides it |
| `LeadContent` | `RenderFragment?` | - | Instead of `Lead`, for a description with links in it |
| `Actions` | `RenderFragment?` | - | Buttons and controls beside the titles, and full width below them on a phone |
| `Media` | `RenderFragment?` | - | A picture to the left of the titles, such as a `MokaAvatar` with `Tile` |
| `ChildContent` | `RenderFragment?` | - | Under the lead, for tags or facts |
| `TitleInTopBar` | `bool` | `false` | The app's top bar already names the page on a phone, so the title hides there and stays for screen readers |
| `Class` | `string?` | - | Additional CSS classes on the header |
| `Style` | `string?` | - | Additional inline styles on the header |

`Margin`, `Padding` and `Rounded` land on the header too.

## Basic

```razor
<MokaPageHeader Eyebrow="Network"
                Title="Devices"
                Said="14 known, 9 online, 1 needs you."
                Lead="Everything that has joined your network, and what each one may reach.">
    <Actions>
        <MokaButton Variant="MokaVariant.Outlined" Color="MokaColor.Secondary">Scan now</MokaButton>
        <MokaButton Color="MokaColor.Primary">Add device</MokaButton>
    </Actions>
</MokaPageHeader>
```

## A way back, a picture and tags

```razor
<MokaPageHeader Eyebrow="Devices" EyebrowHref="/devices"
                Title="Office desktop"
                Said="Online for 3 days, on the home network.">
    <Media>
        <MokaAvatar Icon="MokaIcons.Navigation.Home" Tile />
    </Media>
    <Actions>
        <MokaButton Variant="MokaVariant.Outlined" Color="MokaColor.Secondary">Pause</MokaButton>
    </Actions>
    <ChildContent>
        <MokaFlexbox Direction="MokaDirection.Row" Gap="MokaSpacingScale.Sm" Wrap>
            <MokaTag Text="Allowed" Color="MokaColor.Success" Size="MokaSize.Md" />
            <MokaTag Text="Windows" Size="MokaSize.Md" />
        </MokaFlexbox>
    </ChildContent>
</MokaPageHeader>
```

## A title with a control beside it

```razor
<MokaPageHeader Said="6 installed, 1 update ready.">
    <TitleContent>
        <h1>Plugins</h1>
        <MokaBadge Tone="MokaBadgeTone.Ready" Content="1" />
    </TitleContent>
</MokaPageHeader>
```

## Hiding what a page is for

Text that says what a thing is for helps the first time and gets in the way after that. Turn it off for the whole app with the theme:

```razor
<MokaThemeProvider Theme="MokaTheme.Default.WithDescriptions(false)">
```

`Lead` and `LeadContent` go, and `Said` stays. Warnings and empty states never hide. Your own help text can follow the same choice by carrying the `moka-about` class.

## On a phone

Below 720 px the header stacks: the title drops to 24 px, a plain eyebrow hides, and the actions take the full width. If the app's top bar already shows the page's name, set `TitleInTopBar` so it isn't said twice.
