---
title: Reactions
description: Emoji reactions on a message, with a quick bar on hover or focus and counts the app owns.
order: 152
---

# Reactions

`MokaReactions` puts emoji reactions on a message: a bar of six quick reactions over it on hover or focus, a smile that opens every emoji, and the counts under it. Clicking a count you're part of takes your reaction back. The app owns the counts: it passes them in and changes them when `OnToggle` fires.

## Setup

```razor
@using Moka.Red.Primitives.Reactions
```

## A message with reactions

```razor
<MokaReactions Reactions="_reactions" OnToggle="Toggle">
    <b>Mira</b> Release 0.4 is tagged. Notes are in the changelog.
</MokaReactions>

@code {
    private IReadOnlyList<MokaReaction> _reactions = [new("👍", 3, true), new("🎉", 2, false)];

    private void Toggle(string emoji)
    {
        List<MokaReaction> list = [.. _reactions];
        int at = list.FindIndex(r => r.Emoji == emoji);
        if (at < 0)
        {
            list.Add(new MokaReaction(emoji, 1, true));
        }
        else
        {
            MokaReaction r = list[at];
            list[at] = r with { Mine = !r.Mine, Count = r.Count + (r.Mine ? -1 : 1) };
        }

        _reactions = list;
    }
}
```

The bar sits above the message's top right corner, so leave about 2rem of room above the first message. Without `ChildContent` there's no bar, only the counts and the add button.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Reactions` | `IReadOnlyList<MokaReaction>?` | -- | The reactions so far. A reaction at 0 isn't shown |
| `OnToggle` | `EventCallback<string>` | -- | An emoji the viewer reacted with or took back: from the bar, a count, or a pick in the panel of one they haven't reacted with yet |
| `Quick` | `IReadOnlyList<string>?` | 👍 ❤️ 😂 🎉 😮 🙏 | The bar's quick reactions |
| `ShowBar` | `bool` | `true` | Whether the bar shows over `ChildContent` on hover or focus |
| `ShowAdd` | `bool` | `true` | Whether a smile opens the full emoji panel, in the bar and after the counts |
| `Options` | `MokaEmojiOptions?` | default | The panel's options. It opens as a popup: `List` stays a list, other shapes open the grid |
| `OptionsChanged` | `EventCallback<MokaEmojiOptions>` | -- | The new skin tone and frequently used list, for the app to store |
| `Text` | `MokaReactionsText?` | English | Every word it shows or says |
| `ChildContent` | `RenderFragment?` | -- | The message |

`MokaReaction` is `(string Emoji, int Count, bool Mine)`.

## Screen readers

The bar is a toolbar named "Quick reactions", its buttons "React with 👍" (a screen reader reads the emoji's name). Each count is a toggle button with `aria-pressed`, named like "👍, 3 people, including you". The smile buttons are named "All emoji" and "Add reaction" in tooltips, and open the [emoji panel](/components/emoji-picker) as a dialog.
