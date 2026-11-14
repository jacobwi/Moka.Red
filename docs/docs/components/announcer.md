---
title: Announcer
description: Say things to screen readers that have no place on the screen, through one pair of live regions for the whole app.
order: 164
---

# Announcer

`IMokaAnnouncer` says things to screen readers that have no place on the screen: "Saved", "3 results", "Connection
lost". The words go to two live regions, one polite and one assertive, that `MokaAnnouncerHost` keeps in the page.
Toasts and banners speak through it too, so a page has one place where announcements come from rather than a region in
every component.

## Setup

`AddMokaRed()` and `AddMokaFeedback()` register it; on Core alone call `AddMokaAnnouncer()`. A `MokaToastHost` or
`MokaBannerHost` carries a `MokaAnnouncerHost` of its own, so an app with either needs nothing more. Otherwise place one
in the layout:

```razor
@* MainLayout.razor *@
<MokaAnnouncerHost />
```

Several hosts in a page take turns: the first one mounted speaks, and the next takes over when it goes, so nothing is
heard twice.

## Announcing

```blazor-preview
@inject IMokaAnnouncer Announcer

<MokaButton OnClick='() => Announcer.Announce("3 results")'>Search</MokaButton>
<MokaButton OnClick='() => Announcer.Announce("Connection lost", MokaPoliteness.Assertive)'>Lose the connection</MokaButton>
```

| Member | Description |
|--------|-------------|
| `Announce(text, politeness)` | Says the words. `Polite` waits its turn; `Assertive` interrupts, for errors and anything else that can't wait |
| `Recent` | The latest announcements, up to three of each politeness |
| `Speaker` | The host that speaks, or null |
| `Changed` | Raised when `Recent` or `Speaker` changes |
| `Join(host)`, `Leave(host)` | For hosts |

`Announce` is safe to call from any thread.

## How It's Read

- Both regions are in the page from the first render and stay empty until words arrive, since a screen reader only
  speaks for what is added to a region it is already watching.
- Each announcement is an element of its own, so the same words said twice are heard twice, and a message that arrives
  while another is being spoken is still heard.
- The regions are visually hidden with `moka-visually-hidden`.
