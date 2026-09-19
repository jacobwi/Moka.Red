---
title: Status Dot
description: Small glowing dot with an optional label for online, offline, busy and in-progress states.
order: 69
---

# Status Dot

`MokaStatusDot` is a small colored dot with an optional label, such as `● ONLINE` or `● saving...`. The dot has a soft glow by default and can pulse for work in progress. Unlike `MokaBadge` with `Dot`, which pins a dot to the corner of other content, the status dot is a standalone inline element.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Label` | `string?` | -- | Text next to the dot |
| `Color` | `MokaColor?` | `Success` | Dot color. `Surface` gives a muted neutral dot. The label keeps the secondary text color |
| `Size` | `MokaSize` | `Md` | Dot diameter: `Xs` 6px, `Sm` 8px, `Md` 10px, `Lg` 12px |
| `SizeValue` | `string?` | -- | Any CSS length for the dot diameter. Overrides `Size` |
| `Pulse` | `bool` | `false` | Pulses the dot in a 1.4s loop, for in-progress states |
| `Glow` | `bool` | `true` | Adds a soft glow in the dot color |
| `Uppercase` | `bool` | `true` | Renders the label as an uppercase micro-label. `false` keeps the label as written, at the small text size |
| `Live` | `bool` | `false` | Makes the dot a polite live region (`role="status"`), so screen readers announce changes to `Label`. See Accessibility |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of the dot. Round by default; `None` gives a square |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Margin around the dot and its label |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding around the dot and its label |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

`Rounded` shapes the dot itself, since the element around the dot and its label draws nothing:

```razor
<MokaStatusDot Label="Queued" Color="MokaColor.Warning" Rounded="MokaRounding.None" />
```

Up to 0.1.12 `Rounded` was ignored.

## Basic

```blazor-preview
<div style="display:flex;gap:20px;flex-wrap:wrap">
    <MokaStatusDot Label="Online" />
    <MokaStatusDot Label="Away" Color="MokaColor.Warning" />
    <MokaStatusDot Label="Error" Color="MokaColor.Error" />
    <MokaStatusDot Label="Offline" Color="MokaColor.Surface" />
</div>
```

## Pulse

```blazor-preview
<div style="display:flex;gap:20px;flex-wrap:wrap">
    <MokaStatusDot Label="Syncing" Color="MokaColor.Info" Pulse />
    <MokaStatusDot Label="Deploying" Color="MokaColor.Primary" Pulse />
</div>
```

## Plain Label

```blazor-preview
<div style="display:flex;gap:20px;flex-wrap:wrap">
    <MokaStatusDot Label="saving..." Color="MokaColor.Warning" Uppercase="false" Pulse />
    <MokaStatusDot Label="All changes saved" Uppercase="false" />
</div>
```

## Sizes

```blazor-preview
<div style="display:flex;gap:20px;flex-wrap:wrap;align-items:center">
    <MokaStatusDot Label="Xs" Size="MokaSize.Xs" />
    <MokaStatusDot Label="Sm" Size="MokaSize.Sm" />
    <MokaStatusDot Label="Md" Size="MokaSize.Md" />
    <MokaStatusDot Label="Lg" Size="MokaSize.Lg" />
    <MokaStatusDot Label="16px" SizeValue="16px" />
</div>
```

## In a List

The paused service drops the glow so it reads as idle.

```blazor-preview
<div style="display:flex;flex-direction:column;gap:8px;width:100%;max-width:320px">
    @foreach (var service in _services)
    {
        <div style="display:flex;align-items:center;justify-content:space-between;font-size:var(--moka-font-size-sm)">
            <span style="font-family:var(--moka-font-family-mono)">@service.Name</span>
            <MokaStatusDot Label="@service.State" Color="service.Color" Glow="service.Color != MokaColor.Surface" />
        </div>
    }
</div>

@code {
    private readonly (string Name, string State, MokaColor Color)[] _services =
    [
        ("api-gateway", "Healthy", MokaColor.Success),
        ("billing-worker", "Degraded", MokaColor.Warning),
        ("search-index", "Down", MokaColor.Error),
        ("legacy-export", "Paused", MokaColor.Surface)
    ];
}
```

## Dot Only

Without a label the dot has no text, so give it an `aria-label`. The dot then becomes an image (`role="img"`) with that name.

```blazor-preview
<div style="display:flex;align-items:center;gap:8px">
    <MokaAvatar Initials="MO" Size="MokaSize.Sm" />
    <span>Mara Okafor</span>
    <MokaStatusDot Size="MokaSize.Sm" aria-label="Online" />
</div>
```

## Announcing Changes

`Live` makes a dot a live region, so screen readers read its new label when it changes. Use it for a single status the user waits on, such as a save indicator.

```blazor-preview
<div style="display:flex;align-items:center;gap:12px">
    <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Outlined" OnClick="SaveAsync">Save</MokaButton>
    <MokaStatusDot Live Uppercase="false"
                   Label="@(_saving ? "saving..." : "All changes saved")"
                   Color="@(_saving ? MokaColor.Warning : MokaColor.Success)"
                   Pulse="_saving" />
</div>

@code {
    private bool _saving;

    private async Task SaveAsync()
    {
        _saving = true;
        await Task.Delay(1200);
        _saving = false;
    }
}
```

## Accessibility

- By default the dot is not a live region, because a page of status dots that update often would talk over itself. The dot is `aria-hidden` and the label is read as ordinary text.
- `Live` gives the root `role="status"`, a polite live region, so screen readers announce the new `Label` after they finish what they are saying. Use it on a single status that changes while the user works; leave it off for dots in lists and tables. A live dot needs a `Label`, since the region reads its text.
- A dot without `Label` has no text, and color alone does not reach every user. Give it an `aria-label`, which makes it `role="img"` with that name, or state the status in nearby text. A dot with neither is left as decoration.
- Attributes you pass land on the root and override the built-in ones, `role` included.
- When the operating system asks for reduced motion, `moka.css` stops the pulse.
