---
title: Glass Card
description: Frosted glass card with backdrop blur, optional glow border, and content slots.
order: 85
---

# Glass Card

`MokaGlassCard` is a glassmorphism-style surface container with backdrop blur, adjustable tint and opacity, and optional glowing borders. It supports `Header`, `Footer`, `Title`, and `Subtitle` slots like `MokaCard`, but with a translucent aesthetic suited for layered or image-backed layouts.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | Card body content |
| `Header` | `RenderFragment?` | - | Custom header slot |
| `Footer` | `RenderFragment?` | - | Footer slot |
| `Title` | `string?` | - | Title text |
| `Subtitle` | `string?` | - | Subtitle text below the title |
| `Blur` | `int` | `12` | Backdrop blur amount in pixels |
| `BackgroundOpacity` | `int` | `8` | Background opacity percentage |
| `Tint` | `string?` | - | Background tint color |
| `BorderColor` | `string?` | - | Border color override |
| `Glow` | `bool` | `false` | Enable glowing border effect |
| `GlowColor` | `string?` | - | Glow color override |
| `Clickable` | `bool` | `false` | Makes the card a button: hover effect, pointer cursor, tab stop, Enter and Space. A card with `Href` is a link instead |
| `OnClick` | `EventCallback` | - | Raised by a click on a `Clickable` card, or on the link of a card with `Href` |
| `Href` | `string?` | - | A click anywhere on the card follows it. The link is the title, or a hidden link named by `ActionLabel`. A `javascript:`, `vbscript:` or `data:` URL is not rendered. See [Link](#link) |
| `Target` | `string?` | - | Where the card's link opens, such as `_blank`. Only used with `Href`. One that opens another tab gets `rel="noopener noreferrer"`, and the link's name ends in `NewTabText` |
| `NewTabText` | `string` | `"opens in a new tab"` | What screen readers hear after the link's name, after a comma, when `Target` opens another tab |
| `ActionLabel` | `string?` | `Title` | Names the hidden link of a card with `Href` that shows a custom `Header` or no `Title` |

## Basic Glass Card

```blazor-preview
<div style="background:linear-gradient(135deg,#667eea,#764ba2);padding:40px;border-radius:8px">
    <MokaGlassCard Title="Glass Card" Subtitle="Frosted surface">
        Content is readable through the translucent backdrop.
    </MokaGlassCard>
</div>
```

## Glow Effect

```blazor-preview
<div style="background:linear-gradient(135deg,#0f0c29,#302b63,#24243e);padding:40px;border-radius:8px">
    <MokaGlassCard Title="Glowing Card" Glow GlowColor="#d32f2f">
        A subtle glow frames the card edges.
    </MokaGlassCard>
</div>
```

## With Header and Footer

```blazor-preview
<div style="background:linear-gradient(135deg,#1a1a2e,#16213e);padding:40px;border-radius:8px">
    <MokaGlassCard>
        <Header>
            <div style="display:flex;align-items:center;gap:8px">
                <MokaIcon Icon="MokaIcons.Status.Info" SizeValue="18" />
                <strong style="color:white">Custom Header</strong>
            </div>
        </Header>
        <ChildContent>
            <p style="color:rgba(255,255,255,0.8)">Glass card with named slots.</p>
        </ChildContent>
        <Footer>
            <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm">Learn More</MokaButton>
        </Footer>
    </MokaGlassCard>
</div>
```

## Clickable

```blazor-preview
@code { string _msg = ""; }
<div style="background:linear-gradient(135deg,#434343,#000000);padding:40px;border-radius:8px">
    <MokaGlassCard Title="Click Me" Clickable OnClick="@(() => _msg = "Clicked!")">
        Hover to see the effect. @_msg
    </MokaGlassCard>
</div>
```

A clickable card is a button to the keyboard and to screen readers. It joins the tab order with the focus ring, and Enter or Space activate it, the same as a click. Keys pressed in a control inside it are left alone, and Space does not scroll the page.

## Link

`Href` makes the card lead to a page, as `MokaCard`'s does. The card stays a plain element, and its one tab stop is a
real link stretched over it, so a click anywhere follows it, and a middle click, copying the link and opening it in
another tab all work. The title is the link; with a custom `Header` or no `Title` it is a visually hidden link named by
`ActionLabel`, else by `Title`, else by the card's header and body. An `aria-label` on the card names the link instead.
Links, buttons and inputs inside the card sit above the stretched link and keep their own clicks. The card draws the
focus ring while its link has keyboard focus. `OnClick` is raised by a click on the link, and with `Href` the card is a
link whether or not `Clickable` is set.

`Target` opens the link somewhere else, such as `_blank`. A target that opens another tab gives the link
`rel="noopener noreferrer"`, so the page it opens can't reach this one, and adds ", opens in a new tab" to its name for
screen readers (`NewTabText`). A `rel` or `download` attribute on the card goes on the link, and a `rel` wins over the one
`Target` gives.

```razor
<MokaGlassCard Title="Status page" Subtitle="Uptime and incidents" Href="https://status.example.com" Target="_blank">
    Every service, with its last 90 days.
</MokaGlassCard>
```
