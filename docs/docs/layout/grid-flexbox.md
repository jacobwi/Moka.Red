---
title: Grid & Flexbox
description: CSS Grid and Flexbox layout primitives with responsive breakpoint support.
---

# Grid & Flexbox

Moka.Red provides three complementary layout primitives: `MokaGrid` for CSS Grid,
`MokaFlexbox` for flex containers, and `MokaContainer` for page-level width constraints.

## MokaGrid

`MokaGrid` renders a CSS grid container. Specify columns as an integer count (equal-width
`1fr` tracks) or supply a fully custom `ColumnsValue` template. Responsive shorthand
parameters (`ColumnsMd`, `ColumnsLg`, `ColumnsXl`) cover the most common breakpoints;
use `Breakpoints` for full control.

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Columns` | `int` | `1` | Number of equal-width columns. |
| `ColumnsValue` | `string?` | — | Custom `grid-template-columns`. Overrides `Columns`. |
| `Rows` | `int?` | — | Number of equal-height rows (auto when null). |
| `RowsValue` | `string?` | — | Custom `grid-template-rows`. Overrides `Rows`. |
| `ColumnsMd` | `int?` | — | Column count at ≥ 768 px. |
| `ColumnsLg` | `int?` | — | Column count at ≥ 1024 px. |
| `ColumnsXl` | `int?` | — | Column count at ≥ 1280 px. |
| `Breakpoints` | `IReadOnlyList<MokaBreakpoint>?` | — | Fully programmable breakpoints. Overrides suffixed parameters. |
| `Gap` | `MokaSpacingScale?` | — | Uniform gap from the spacing scale. |
| `GapValue` | `string?` | — | Custom gap CSS value. |
| `RowGap` | `MokaSpacingScale?` | — | Row-only gap when different from column gap. |
| `AlignItems` | `MokaAlign` | `Stretch` | `align-items` for grid children. |
| `JustifyItems` | `MokaJustify` | `Start` | `justify-items` for grid children. |
| `Inline` | `bool` | `false` | Use `inline-grid` instead of `grid`. |

### Basic grid

```razor
<MokaGrid Columns="3" Gap="MokaSpacingScale.Md">
    <div>Item 1</div>
    <div>Item 2</div>
    <div>Item 3</div>
</MokaGrid>
```

### Responsive grid

```razor
@* 1 col → 2 at md → 4 at lg *@
<MokaGrid Columns="1" ColumnsMd="2" ColumnsLg="4" Gap="MokaSpacingScale.Sm">
    @foreach (var card in cards)
    {
        <ProductCard Item="card" />
    }
</MokaGrid>
```

### Custom template with programmatic breakpoints

```razor
<MokaGrid ColumnsValue="200px 1fr" Breakpoints="@_bp">
    <Sidebar />
    <MainContent />
</MokaGrid>

@code {
    private static readonly IReadOnlyList<MokaBreakpoint> _bp =
    [
        new() { MinWidth = "768px", ColumnsValue = "260px 1fr" },
        new() { MinWidth = "1280px", ColumnsValue = "320px 1fr" }
    ];
}
```

---

## MokaFlexbox

`MokaFlexbox` renders a flex container. The default direction is `Column` (vertical stack).
Set `Direction="MokaDirection.Row"` for horizontal layouts. Responsive breakpoints can
change direction, alignment, and gap at any viewport width.

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Direction` | `MokaDirection` | `Column` | Flex direction (Column, Row, ColumnReverse, RowReverse). |
| `Justify` | `MokaJustify` | `Start` | `justify-content` along the main axis. |
| `Align` | `MokaAlign` | `Stretch` | `align-items` along the cross axis. |
| `Gap` | `MokaSpacingScale?` | — | Gap between items from the spacing scale. |
| `GapValue` | `string?` | — | Custom gap CSS value. |
| `Wrap` | `bool` | `false` | Enable `flex-wrap: wrap`. |
| `Inline` | `bool` | `false` | Use `inline-flex` instead of `flex`. |
| `Breakpoints` | `IReadOnlyList<MokaBreakpoint>?` | — | Responsive overrides. |

### Horizontal toolbar layout

```razor
<MokaFlexbox Direction="MokaDirection.Row"
             Align="MokaAlign.Center"
             Justify="MokaJustify.SpaceBetween"
             Gap="MokaSpacingScale.Sm">
    <Logo />
    <NavLinks />
    <UserMenu />
</MokaFlexbox>
```

### Stack that switches to row at md

```razor
<MokaFlexbox Direction="MokaDirection.Column"
             Gap="MokaSpacingScale.Md"
             Breakpoints="@_bp">
    <FeatureCard />
    <FeatureCard />
</MokaFlexbox>

@code {
    private static readonly IReadOnlyList<MokaBreakpoint> _bp =
    [
        new() { MinWidth = "768px", Direction = MokaDirection.Row, Align = MokaAlign.Start }
    ];
}
```

---

## MokaContainer

`MokaContainer` is a centered max-width wrapper with configurable gutters. Use it to
constrain page content to a readable line width.

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `MaxWidth` | `string` | `"1200px"` | Maximum width. Any CSS length. |
| `Fluid` | `bool` | `false` | Remove max-width for full-width layout (keeps gutters). |
| `Centered` | `bool` | `true` | Auto-center with `margin: auto`. |
| `GutterX` | `MokaSpacingScale?` | `Lg` | Horizontal (left/right) padding. |
| `GutterXValue` | `string?` | — | Custom horizontal padding override. |
| `GutterY` | `MokaSpacingScale?` | — | Vertical (top/bottom) padding. |
| `GutterYValue` | `string?` | — | Custom vertical padding override. |

### Usage

```razor
<MokaContainer MaxWidth="960px">
    <ArticleBody />
</MokaContainer>

@* Full-width with consistent gutters *@
<MokaContainer Fluid="true" GutterX="MokaSpacingScale.Xl">
    <HeroBanner />
</MokaContainer>
```
