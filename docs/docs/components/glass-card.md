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
| `ChildContent` | `RenderFragment?` | — | Card body content |
| `Header` | `RenderFragment?` | — | Custom header slot |
| `Footer` | `RenderFragment?` | — | Footer slot |
| `Title` | `string?` | — | Title text |
| `Subtitle` | `string?` | — | Subtitle text below the title |
| `Blur` | `int` | `12` | Backdrop blur amount in pixels |
| `BackgroundOpacity` | `int` | `8` | Background opacity percentage |
| `Tint` | `string?` | — | Background tint color |
| `BorderColor` | `string?` | — | Border color override |
| `Glow` | `bool` | `false` | Enable glowing border effect |
| `GlowColor` | `string?` | — | Glow color override |
| `Clickable` | `bool` | `false` | Hover effect and pointer cursor |
| `OnClick` | `EventCallback` | — | Click callback |

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
