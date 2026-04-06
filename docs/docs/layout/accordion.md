---
title: Accordion
description: Collapsible content panels with single-expand or multi-expand behavior.
order: 20
---

# Accordion

`MokaAccordion` is a container for collapsible sections. It can enforce single-expand mode (default) where opening one item closes others, or allow multiple items open simultaneously.

## Parameters

### MokaAccordion

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | `MokaAccordionItem` elements |
| `Multiple` | `bool` | `false` | Allow multiple items open at once |
| `Bordered` | `bool` | `true` | Outer border around the accordion |
| `Flush` | `bool` | `false` | Removes border and radius for flush layout |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaAccordionItem

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Content shown when expanded |
| `Header` | `RenderFragment?` | -- | Custom header content (overrides `Title`/`Subtitle`) |
| `Title` | `string?` | -- | Header title text |
| `Subtitle` | `string?` | -- | Secondary text below the title |
| `DefaultExpanded` | `bool` | `false` | Whether the item starts expanded |
| `Disabled` | `bool` | `false` | Prevents toggling |
| `Icon` | `MokaIconDefinition?` | -- | Custom expand icon |
| `Class` | `string?` | -- | Additional CSS classes |

## Basic Accordion

In single-expand mode, opening one item automatically closes the others.

```blazor-preview
<MokaAccordion>
    <MokaAccordionItem Title="Section 1">
        Content for the first section.
    </MokaAccordionItem>
    <MokaAccordionItem Title="Section 2">
        Content for the second section.
    </MokaAccordionItem>
    <MokaAccordionItem Title="Section 3">
        Content for the third section.
    </MokaAccordionItem>
</MokaAccordion>
```

## Multiple Expand

```blazor-preview
<MokaAccordion Multiple>
    <MokaAccordionItem Title="First" DefaultExpanded>
        This starts expanded.
    </MokaAccordionItem>
    <MokaAccordionItem Title="Second">
        Open multiple items at once.
    </MokaAccordionItem>
    <MokaAccordionItem Title="Third">
        All three can be open simultaneously.
    </MokaAccordionItem>
</MokaAccordion>
```

## With Subtitles

```blazor-preview
<MokaAccordion>
    <MokaAccordionItem Title="Personal Information" Subtitle="Name, email, phone">
        Your personal details form goes here.
    </MokaAccordionItem>
    <MokaAccordionItem Title="Billing Address" Subtitle="Street, city, country">
        Billing address form goes here.
    </MokaAccordionItem>
    <MokaAccordionItem Title="Payment Method" Subtitle="Card or bank transfer">
        Payment form goes here.
    </MokaAccordionItem>
</MokaAccordion>
```

## Disabled Item

```blazor-preview
<MokaAccordion>
    <MokaAccordionItem Title="Available">
        This section can be toggled.
    </MokaAccordionItem>
    <MokaAccordionItem Title="Locked (Disabled)" Disabled>
        This section cannot be toggled.
    </MokaAccordionItem>
</MokaAccordion>
```

## Flush Style

Removes the outer border and border-radius for embedding in other containers.

```blazor-preview
<MokaAccordion Flush>
    <MokaAccordionItem Title="Flush Item 1">Content A</MokaAccordionItem>
    <MokaAccordionItem Title="Flush Item 2">Content B</MokaAccordionItem>
</MokaAccordion>
```
