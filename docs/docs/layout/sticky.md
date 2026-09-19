---
title: Sticky
description: Wrapper that keeps its content in view while its scroll container scrolls.
order: 115
---

# Sticky

`MokaSticky` keeps its content in view while the page or a scroll container scrolls, using CSS `position: sticky`. It sticks to the top by default, or to the bottom when `Bottom` is set. Use it for table headers, section titles and action bars in long content.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Content to keep in view |
| `Top` | `string?` | `"0"` | Distance from the top of the scroll container while stuck |
| `Bottom` | `string?` | -- | Distance from the bottom. When set, the content sticks to the bottom and `Top` and `OffsetValue` are ignored |
| `OffsetValue` | `string?` | -- | Top distance that replaces `Top` |
| `ZIndex` | `int?` | -- | Stacking order. Unset uses `--moka-z-sticky` (1020) |
| `Padding` | `MokaSpacingScale?` | -- | Padding from the spacing scale |
| `PaddingValue` | `string?` | -- | Any CSS padding. Wins over `Padding` |
| `Margin` | `MokaSpacingScale?` | -- | Margin from the spacing scale |
| `MarginValue` | `string?` | -- | Any CSS margin. Wins over `Margin` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Sticky Header

The title stays at the top of the log while the lines scroll under it. The wrapper has no background of its own, so the header sets one.

```blazor-preview
<div style="width:100%;height:220px;overflow-y:auto;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaSticky>
        <div style="padding:var(--moka-spacing-sm) var(--moka-spacing-md);background:var(--moka-color-surface);border-bottom:1px solid var(--moka-color-outline-variant);font-weight:var(--moka-font-weight-semibold)">
            Build log
        </div>
    </MokaSticky>
    @for (var i = 1; i <= 30; i++)
    {
        <div style="padding:2px var(--moka-spacing-md);font-family:var(--moka-font-family-mono);font-size:var(--moka-font-size-sm)">
            [@(i.ToString("00"))] step completed
        </div>
    }
</div>
```

## Section Headings

A sticky element only sticks inside its parent. Each heading below stays up while its own section scrolls, and the next section pushes it out.

```blazor-preview
<div style="width:100%;height:220px;overflow-y:auto;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    @foreach (var group in _contacts)
    {
        <section>
            <MokaSticky>
                <div style="padding:4px var(--moka-spacing-md);background:var(--moka-color-surface-variant);font-size:var(--moka-font-size-xs);font-weight:var(--moka-font-weight-semibold)">
                    @group.Key
                </div>
            </MokaSticky>
            @foreach (var name in group.Value)
            {
                <div style="padding:6px var(--moka-spacing-md)">@name</div>
            }
        </section>
    }
</div>

@code {
    private readonly Dictionary<string, string[]> _contacts = new()
    {
        ["A"] = new[] { "Ada Lovelace", "Alan Turing", "Anita Borg", "Annie Easley" },
        ["B"] = new[] { "Barbara Liskov", "Bjarne Stroustrup", "Brian Kernighan" },
        ["D"] = new[] { "Dennis Ritchie", "Donald Knuth", "Dorothy Vaughan" },
        ["G"] = new[] { "Grace Hopper", "Guido van Rossum" }
    };
}
```

## Bottom Bar

With `Bottom` set, the bar sits at the bottom of the scroll container until the content above it runs out. Put it after the content it follows.

```blazor-preview
<div style="width:100%;height:220px;overflow-y:auto;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <div style="padding:var(--moka-spacing-md)">
        @for (var i = 1; i <= 8; i++)
        {
            <MokaParagraph>Clause @i of the agreement. Read it before you accept.</MokaParagraph>
        }
    </div>
    <MokaSticky Bottom="0">
        <div style="display:flex;justify-content:flex-end;gap:var(--moka-spacing-sm);padding:var(--moka-spacing-sm) var(--moka-spacing-md);background:var(--moka-color-surface);border-top:1px solid var(--moka-color-outline-variant)">
            <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Text">Decline</MokaButton>
            <MokaButton Size="MokaSize.Sm">Accept</MokaButton>
        </div>
    </MokaSticky>
</div>
```

## Behaviour

- The content sticks inside the nearest ancestor whose `overflow` is `hidden`, `auto` or `scroll`. If that ancestor does not scroll, nothing sticks. An `overflow: hidden` between the sticky element and the container you want it to stick to is the usual cause.
- `Top`, `Bottom` and `OffsetValue` take any CSS length, such as `"56px"` to sit below a fixed app bar.
- The default layer, `--moka-z-sticky` (1020), is above `--moka-z-dropdown` (1000) and below `--moka-z-fixed` (1030) and dialogs. Set `ZIndex` to change it.
