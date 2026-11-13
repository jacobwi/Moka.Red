---
title: Progress & Loading
description: Progress bars, spinners, skeleton placeholders and loading overlays for loading states.
order: 23
---

# Progress & Loading

Moka.Red provides four loading indicators: `MokaProgress` for determinate/indeterminate progress bars, `MokaSpinner` for animated loading spinners, `MokaSkeleton` for content placeholder shimmer effects, and `MokaLoadingOverlay` to cover content while it loads.

---

## MokaProgress

A progress indicator supporting linear and circular modes with determinate and indeterminate states. Uses `role="progressbar"` with appropriate ARIA attributes.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `double?` | -- | Progress 0--100. `null` for indeterminate |
| `ProgressType` | `MokaProgressType` | `Linear` | `Linear` or `Circular` |
| `ShowValue` | `bool` | `false` | Show the numeric percentage |
| `Striped` | `bool` | `false` | Animated stripes (linear only) |
| `RoundedEnds` | `bool` | `true` | Rounded bar ends (linear only) |
| `Thin` | `bool` | `false` | A 6px round bar for inside a card or a list row (linear only) |
| `Segments` | `IReadOnlyList<MokaProgressSegment>?` | -- | Several colored parts side by side, each a percentage of the whole bar. They win over `Value` (linear only) |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the bar. A linear bar still fills its container, with the margin inside |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

A linear bar fills the width of its container, also as a flex item in a row. Up to 0.1.12 it was `width: 100%`, so a margin made it wider than the container.

### Linear Progress

```blazor-preview
<div style="display:flex;flex-direction:column;gap:12px">
    <MokaProgress Value="25" />
    <MokaProgress Value="50" ShowValue />
    <MokaProgress Value="75" Striped />
</div>
```

### Indeterminate

When `Value` is null, the bar shows an animated indeterminate state.

```blazor-preview
<MokaProgress />
```

### Circular Progress

```blazor-preview
<div style="display:flex;gap:16px;align-items:center">
    <MokaProgress ProgressType="MokaProgressType.Circular" Value="65" ShowValue />
    <MokaProgress ProgressType="MokaProgressType.Circular" />
</div>
```

### Thin, with Segments

Each `MokaProgressSegment` has a value out of 100 and a `MokaColor`, and they sit side by side with a small gap. `aria-valuenow` and `ShowValue` report their sum, at most 100. A segment at 0 isn't drawn, and an empty list is an empty bar rather than an indeterminate one.

```razor
<MokaProgress Thin Segments="_used" />

@code {
    private static readonly IReadOnlyList<MokaProgressSegment> _used =
    [
        new(46, MokaColor.Success),
        new(18, MokaColor.Warning),
        new(9, MokaColor.Error),
        new(12, MokaColor.Info)
    ];
}
```

---

## MokaSpinner

An animated loading spinner with multiple visual styles. All animations are pure CSS.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `SpinnerStyle` | `MokaSpinnerStyle` | `Circular` | `Circular`, `Dots`, `Pulse`, `Bars`, `Ring` |
| `Label` | `string?` | -- | Text displayed alongside the spinner |
| `LabelPlacement` | `MokaLabelPlacement` | `Bottom` | `Bottom` or `Right` |
| `Size` | `MokaSize` | `Md` | `Xs`, `Sm`, `Md`, `Lg` |
| `Color` | `MokaColor?` | `Primary` | Spinner color |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### Spinner Styles

```blazor-preview
<div style="display:flex;gap:24px;align-items:center">
    <MokaSpinner SpinnerStyle="MokaSpinnerStyle.Circular" Label="Circular" />
    <MokaSpinner SpinnerStyle="MokaSpinnerStyle.Dots" Label="Dots" />
    <MokaSpinner SpinnerStyle="MokaSpinnerStyle.Pulse" Label="Pulse" />
    <MokaSpinner SpinnerStyle="MokaSpinnerStyle.Bars" Label="Bars" />
    <MokaSpinner SpinnerStyle="MokaSpinnerStyle.Ring" Label="Ring" />
</div>
```

### Sizes and Colors

```blazor-preview
<div style="display:flex;gap:16px;align-items:center">
    <MokaSpinner Size="MokaSize.Xs" Color="MokaColor.Primary" />
    <MokaSpinner Size="MokaSize.Sm" Color="MokaColor.Success" />
    <MokaSpinner Size="MokaSize.Md" Color="MokaColor.Warning" />
    <MokaSpinner Size="MokaSize.Lg" Color="MokaColor.Error" />
</div>
```

---

## MokaSkeleton

Shimmer placeholder shapes for content that is loading. Supports text lines, circles, rectangles, and card layouts.

After 0.4.4 it lives in `Moka.Red.Primitives`, under its old namespace `Moka.Red.Feedback.Loading` and forwarded from `Moka.Red.Feedback`, so Primitives, Layout and Forms components can draw one (the media gallery's waiting tiles do). Nothing changes for an app: the `@using` and the markup are the same.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Shape` | `MokaSkeletonShape` | `Text` | `Text`, `Circle`, `Rectangle`, `Card` |
| `Lines` | `int` | `1` | Number of text lines (only for `Text` shape) |
| `Width` | `string?` | -- | Custom width (defaults: `"100%"` for text, rectangle and card, `"40px"` for circle). With several text lines the last is 70% of it |
| `Height` | `string?` | -- | Custom height: each text line's (12px by default), the circle's (its width by default), the rectangle's (48px by default), or the whole card's |
| `Animation` | `MokaSkeletonAnimation` | `Shimmer` | `Shimmer`, `Pulse`, `None` |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of every shape: the lines, the circle, the rectangle and the card's parts. `None` squares them |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Margin around the skeleton |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding around the shapes |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

`Rounded` sets the radius of the shapes themselves, since the skeleton's own box draws nothing:

```razor
<MokaSkeleton Shape="MokaSkeletonShape.Rectangle" Height="120px" Rounded="MokaRounding.Xl" />
<MokaSkeleton Shape="MokaSkeletonShape.Text" Lines="2" Rounded="MokaRounding.None" />
```

Up to 0.1.12 the radius went on that invisible box, and a `Rounded` value only switched the rectangle to the large radius, whichever size it named.

### Shapes

```blazor-preview
<div style="display:flex;flex-direction:column;gap:16px;max-width:400px">
    <MokaSkeleton Shape="MokaSkeletonShape.Text" Lines="3" />
    <div style="display:flex;gap:12px;align-items:center">
        <MokaSkeleton Shape="MokaSkeletonShape.Circle" Width="48px" />
        <div style="flex:1">
            <MokaSkeleton Shape="MokaSkeletonShape.Text" Lines="2" />
        </div>
    </div>
    <MokaSkeleton Shape="MokaSkeletonShape.Rectangle" Height="120px" />
</div>
```

### Animation Types

```blazor-preview
<div style="display:flex;flex-direction:column;gap:12px;max-width:300px">
    <MokaSkeleton Animation="MokaSkeletonAnimation.Shimmer" />
    <MokaSkeleton Animation="MokaSkeletonAnimation.Pulse" />
    <MokaSkeleton Animation="MokaSkeletonAnimation.None" />
</div>
```

### Sizes

Text lines take `Width` and `Height`. With several lines the last is 70% of `Width`, written as `calc(<Width> * 0.7)`, so a paragraph still ends short; a keyword such as `fit-content` can't be scaled, and then every line takes it. Without `Width` the lines fill the row and the last is 70%, as before.

```blazor-preview
<div style="display:flex;flex-direction:column;gap:16px">
    <MokaSkeleton Lines="3" Width="240px" Height="0.875rem" />
    <MokaSkeleton Lines="2" Width="60%" />
</div>
```

Up to 0.4.2 text lines ignored `Height`, the last of several lines was 70% of the row whatever `Width` said, and the card ignored both.

### Card Skeleton

```blazor-preview
<div style="max-width:300px">
    <MokaSkeleton Shape="MokaSkeletonShape.Card" />
</div>
```

`Width` and `Height` size the whole card, so the placeholder takes the place the real card will. With a `Height` the picture area fills what the three lines leave; without one it stays 16:9.

```blazor-preview
<div style="display:flex;gap:12px;flex-wrap:wrap">
    <MokaSkeleton Shape="MokaSkeletonShape.Card" Width="200px" Height="180px" />
    <MokaSkeleton Shape="MokaSkeletonShape.Card" Width="200px" />
</div>
```

---

## MokaLoadingOverlay

Wraps content and, while `Loading` is true, covers it with a translucent layer and a spinner. The content stays rendered underneath, so its state survives the loading, but it is `inert` until loading ends. `FullScreen` covers the whole viewport instead.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Loading` | `bool` | `false` | Shows the overlay and makes the content inert. The overlay never changes it itself, so pass it one way |
| `Message` | `string?` | -- | Text under the spinner. Also names the overlay for screen readers ("Loading" when null) |
| `SpinnerStyle` | `MokaSpinnerStyle` | `Circular` | `Circular`, `Dots`, `Pulse`, `Bars`, `Ring` |
| `Size` | `MokaSize` | `Md` | Spinner size |
| `Color` | `MokaColor?` | -- | Spinner color. Unset uses the primary color |
| `Blur` | `bool` | `false` | Blurs the content while loading |
| `BlurAmount` | `string` | `"4px"` | Blur radius used with `Blur` |
| `Opacity` | `double` | `0.7` | Opacity of the layer, from 0 to 1. The layer is the surface color at that strength |
| `OverlayColor` | `string?` | -- | Any CSS background for the layer. Replaces the surface color and ignores `Opacity` |
| `FullScreen` | `bool` | `false` | Fixes the layer over the whole viewport, above page content |
| `ShowSkeleton` | `bool` | `false` | Shows a `MokaSkeleton` instead of the spinner. See [Skeleton](#skeleton) |
| `SkeletonShape` | `MokaSkeletonShape` | `Text` | Skeleton shape with `ShowSkeleton` |
| `SkeletonLines` | `int` | `3` | Number of lines with `ShowSkeleton` and the `Text` shape |
| `ChildContent` | `RenderFragment?` | -- | Content to cover |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of the wrapper. The layer takes the same corners, so match it to rounded content. A `FullScreen` layer stays square |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Margin around the wrapper |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding between the wrapper and the content. The layer covers it too |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

```razor
<MokaLoadingOverlay Loading="_loading" Rounded="MokaRounding.Lg">
    <MokaCard>...</MokaCard>
</MokaLoadingOverlay>
```

Up to 0.1.12 `Rounded` was ignored, so the layer kept square corners over a rounded card.

### Covering a Section

```blazor-preview
@code {
    bool _loading;

    async Task ReloadAsync()
    {
        _loading = true;
        await Task.Delay(1500);
        _loading = false;
    }
}

<div style="display:flex;flex-direction:column;gap:12px;width:100%;max-width:420px">
    <MokaLoadingOverlay Loading="_loading" Message="Loading orders">
        <MokaCard Title="Orders" Subtitle="Last 30 days">
            <MokaParagraph>42 open, 7 overdue and 118 shipped.</MokaParagraph>
        </MokaCard>
    </MokaLoadingOverlay>
    <MokaButton OnClick="ReloadAsync" Disabled="_loading">Reload</MokaButton>
</div>
```

### Blur and Spinner Style

```blazor-preview
@code {
    bool _syncing = true;
}

<div style="display:flex;flex-direction:column;gap:12px;width:100%;max-width:420px">
    <MokaLoadingOverlay Loading="_syncing" Blur Message="Syncing" SpinnerStyle="MokaSpinnerStyle.Dots" Opacity="0.4">
        <MokaCard Title="Shared notes">
            <MokaParagraph>Release checklist, meeting notes and the draft changelog.</MokaParagraph>
        </MokaCard>
    </MokaLoadingOverlay>
    <MokaButton Variant="MokaVariant.Outlined" OnClick="@(() => _syncing = !_syncing)">
        @(_syncing ? "Finish sync" : "Start sync")
    </MokaButton>
</div>
```

### Skeleton

With `ShowSkeleton`, the text, rectangle and card skeletons span the covered area, inside a small padding, and a card taller than the area is cut off at its edge. The circle keeps its own size and stays centred.

```razor
<MokaLoadingOverlay Loading="_loading" ShowSkeleton SkeletonLines="4" Opacity="0.9">
    <MokaCard Title="Orders" Subtitle="Last 30 days">
        <MokaParagraph>42 open, 7 overdue and 118 shipped.</MokaParagraph>
    </MokaCard>
</MokaLoadingOverlay>
```

Up to 0.1.12 only the circle showed: the other shapes came out zero wide.

### Full Screen

```blazor-preview
@code {
    bool _saving;

    async Task SaveAsync()
    {
        _saving = true;
        await Task.Delay(2000);
        _saving = false;
    }
}

<MokaButton OnClick="SaveAsync">Save and wait</MokaButton>

<MokaLoadingOverlay Loading="_saving" FullScreen Message="Saving" />
```

### Accessibility

- While loading, the content is `inert` and carries `aria-busy="true"`: Tab skips its controls, clicks and text selection stop, and screen readers leave it out until loading ends.
- The layer is a `role="alert"` named by `Message`, and the spinner inside it a `role="status"`.
- When loading ends the content is reachable again and carries `aria-busy="false"`.

Up to 0.1.12 the layer only stopped the mouse, and Tab still reached the controls underneath. The layer itself carried `aria-busy="true"`, which tells screen readers to wait, so its alert could go unannounced. The overlay also took `LoadingChanged` and `MessageChanged` parameters that it never raised. They are gone: pass `Loading` and `Message` one way.
