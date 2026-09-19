---
title: Timeline
description: Vertical list of events with dots, titles, timestamps and a connecting line.
order: 71
---

# Timeline

`MokaTimeline` lays out `MokaTimelineItem` children as a vertical list of events joined by a line. Each item has a dot, optionally with an icon and a color, plus a title, content and a timestamp. Use it for activity feeds, order histories and release logs. To show progress through a fixed series of steps, use `MokaSteps` or `MokaStepper`.

## MokaTimeline Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | `MokaTimelineItem` elements |
| `Alternate` | `bool` | `false` | Runs the connecting line down the middle and puts the items on alternating sides of it: the first on the right, the second on the left, and so on |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## MokaTimelineItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string?` | -- | Event title |
| `Timestamp` | `string?` | -- | Time text under the content, such as `"2 hours ago"` or `"Mar 15, 09:40"`. Shown as given |
| `Icon` | `MokaIconDefinition?` | -- | Icon inside the dot |
| `DotColor` | `MokaColor?` | `Primary` | Dot color. The icon inside takes the matching on-color, and a `Surface` dot gets an outline |
| `ChildContent` | `RenderFragment?` | -- | Content between the title and the timestamp |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<div style="width:100%;max-width:420px">
    <MokaTimeline>
        <MokaTimelineItem Title="Order placed" Timestamp="Sep 12, 09:14">Order #10442, 3 items.</MokaTimelineItem>
        <MokaTimelineItem Title="Payment confirmed" Timestamp="Sep 12, 09:15" />
        <MokaTimelineItem Title="Shipped" Timestamp="Sep 13, 16:02">Tracking number RX-88213-DE.</MokaTimelineItem>
        <MokaTimelineItem Title="Delivered" Timestamp="Sep 15, 11:47" />
    </MokaTimeline>
</div>
```

## Icons and Colors

The dot is decorative, so the title carries the outcome.

```blazor-preview
<div style="width:100%;max-width:420px">
    <MokaTimeline>
        <MokaTimelineItem Title="Deployed to production" Timestamp="12 minutes ago"
                          Icon="MokaIcons.Status.CheckCircle" DotColor="MokaColor.Success" />
        <MokaTimelineItem Title="Smoke tests failed on staging" Timestamp="1 hour ago"
                          Icon="MokaIcons.Status.Error" DotColor="MokaColor.Error">
            Passed on the second run after the cache was cleared.
        </MokaTimelineItem>
        <MokaTimelineItem Title="Build 2026.09.18-4412 finished" Timestamp="2 hours ago"
                          Icon="MokaIcons.File.Code" DotColor="MokaColor.Info" />
        <MokaTimelineItem Title="Pull request merged" Timestamp="3 hours ago"
                          Icon="MokaIcons.Action.Upload" />
    </MokaTimeline>
</div>
```

## Alternate

The line runs down the middle, and the items take turns on its right and left. Text on the left side is aligned towards the line.

```blazor-preview
<div style="width:100%;max-width:560px">
    <MokaTimeline Alternate>
        <MokaTimelineItem Title="Kickoff" Timestamp="Jan 8">Scope and milestones agreed.</MokaTimelineItem>
        <MokaTimelineItem Title="Design review" Timestamp="Feb 2" DotColor="MokaColor.Info">Two rounds of feedback.</MokaTimelineItem>
        <MokaTimelineItem Title="Beta" Timestamp="Mar 20" DotColor="MokaColor.Warning">Opened to 40 teams.</MokaTimelineItem>
        <MokaTimelineItem Title="Launch" Timestamp="Apr 15" DotColor="MokaColor.Success"
                          Icon="MokaIcons.Status.CheckCircle" />
    </MokaTimeline>
</div>
```

## Rich Content

```blazor-preview
<div style="width:100%;max-width:420px">
    <MokaTimeline>
        <MokaTimelineItem Title="v0.4.0" Timestamp="September 2026" DotColor="MokaColor.Success">
            <div style="display:flex;gap:6px;flex-wrap:wrap;margin-top:4px">
                <MokaTag Text="Feature" Color="MokaColor.Primary" />
                <MokaTag Text="Breaking" Color="MokaColor.Error" />
            </div>
        </MokaTimelineItem>
        <MokaTimelineItem Title="v0.3.2" Timestamp="August 2026">
            <span style="font-size:var(--moka-font-size-sm);color:var(--moka-color-on-surface-variant)">Fixes a crash when the list is empty.</span>
        </MokaTimelineItem>
        <MokaTimelineItem Title="v0.3.0" Timestamp="July 2026" DotColor="MokaColor.Secondary" />
    </MokaTimeline>
</div>
```

## Behaviour

- Items render in the order you write them, so newest first or oldest first is up to you.
- The line runs down the left edge behind the 24px dots, from the top of the first item to the bottom of the last.
- `DotColor` sets the dot's fill and border to that palette color, and the 14px icon inside to the matching on-color, such as on-success on a `Success` dot. A `Surface` dot has the page's own color, so it gets an outline border instead.
- `Timestamp` is plain text. Format dates before passing them.
- With `Alternate`, each item becomes three columns: content, dot and content, with equal outer columns so the dots sit on the centered line. Odd items put their content on the right and even items on the left. Sides go by position among the timeline's children, so keep other elements out of the timeline. Below 641px wide the two columns do not fit, so the timeline falls back to the one-sided layout.

## Accessibility

The timeline renders plain `div` elements with no list semantics. Both components pass unmatched attributes to their root element, so add `role="list"` to the timeline and `role="listitem"` to each item when screen readers should treat it as a list:

```razor
<MokaTimeline role="list">
    <MokaTimelineItem role="listitem" Title="Order placed" Timestamp="Sep 12, 09:14" />
    <MokaTimelineItem role="listitem" Title="Shipped" Timestamp="Sep 13, 16:02" />
</MokaTimeline>
```
