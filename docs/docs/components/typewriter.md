---
title: Typewriter
description: Character-by-character text reveal with optional blinking cursor.
order: 69
---

# Typewriter

`MokaTypewriter` reveals text one character at a time, simulating a typing effect. Supports configurable speed, a blinking cursor, looping, and a completion callback.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `string` | `""` | The text to type out |
| `Speed` | `int` | `50` | Delay between each character in milliseconds |
| `Delay` | `int` | `0` | Initial delay before typing starts, in milliseconds |
| `ShowCursor` | `bool` | `true` | Show a blinking cursor at the end |
| `CursorChar` | `string` | `"\|"` | Character used for the cursor |
| `Loop` | `bool` | `false` | When `true`, erases and retypes the text indefinitely |
| `OnComplete` | `EventCallback` | -- | Fires when the full text has been typed |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Typewriter

```blazor-preview
<MokaTypewriter Text="Welcome to Moka.Red, a Blazor component library." />
```

## Fast Speed

Increase typing speed by lowering the delay between characters.

```blazor-preview
<MokaTypewriter Text="This text types very quickly!" Speed="20" />
```

## Custom Cursor

Change the cursor character and disable it when typing completes.

```blazor-preview
<MokaTypewriter Text="Building beautiful UIs with Blazor." CursorChar="_" />
```

## Looping

The text erases and retypes continuously.

```blazor-preview
<MokaTypewriter Text="Moka.Red" Speed="80" Loop ShowCursor />
```
