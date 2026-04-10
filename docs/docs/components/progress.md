---
title: Progress & Loading
description: Progress bars, spinners, and skeleton placeholders for loading states.
order: 23
---

# Progress & Loading

Moka.Red provides three loading indicators: `MokaProgress` for determinate/indeterminate progress bars, `MokaSpinner` for animated loading spinners, and `MokaSkeleton` for content placeholder shimmer effects.

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
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

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

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Shape` | `MokaSkeletonShape` | `Text` | `Text`, `Circle`, `Rectangle`, `Card` |
| `Lines` | `int` | `1` | Number of text lines (only for `Text` shape) |
| `Width` | `string?` | -- | Custom width (defaults: `"100%"` for text/rect, `"40px"` for circle) |
| `Height` | `string?` | -- | Custom height (varies by shape) |
| `Animation` | `MokaSkeletonAnimation` | `Shimmer` | `Shimmer`, `Pulse`, `None` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

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

### Card Skeleton

```blazor-preview
<div style="max-width:300px">
    <MokaSkeleton Shape="MokaSkeletonShape.Card" />
</div>
```
