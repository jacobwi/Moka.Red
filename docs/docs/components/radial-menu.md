---
title: Radial Menu
description: Round buttons around what was right-clicked, its name in the hub, choices in a list beside a button, picked by click, flick, arrows or digits.
order: 147
---

# Radial Menu

`MokaRadialMenu` puts round buttons in a ring around what was right-clicked, with its name in the hub. A button can open a short list of choices beside itself. It lives in `Moka.Red.ContextMenu` next to the list menu, and shares its service and host, so only one menu of either kind is ever open.

Open one from markup with `MokaRadialMenuTrigger`, or from code with `IMokaContextMenuService.ShowRadial`.

## Setup

`AddMokaRed()` registers the service. With the individual package, call `AddMokaContextMenu()`. Place one `MokaContextMenuHost` in the layout: it draws the radial menu as well as the list menu.

```razor
@* MainLayout.razor *@
<MokaContextMenuHost />
```

`MokaRadialMenuTrigger` works without a host too, and then draws its own ring.

## From Markup

```razor
<MokaRadialMenuTrigger Spec="DeviceMenu(device)">
    <MokaCard Title="@device.Name">...</MokaCard>
</MokaRadialMenuTrigger>

@code {
    private MokaRadialMenuSpec DeviceMenu(Device device) => new(
        device.Name,
        [
            new MokaRadialItem("open", "Open", MokaIcons.Content.ExternalLink),
            new MokaRadialItem("limit", "Limit", MokaIcons.Status.Clock, Choices:
            [
                new MokaRadialChoice("10", "10 Mbit/s"),
                new MokaRadialChoice("50", "50 Mbit/s", Checked: device.Limit == 50),
                new MokaRadialChoice("100", "100 Mbit/s")
            ]),
            new MokaRadialItem("allow", "Allow", MokaIcons.Status.Check, MokaRadialTone.Good),
            new MokaRadialItem("block", "Block", MokaIcons.Toggle.Lock, MokaRadialTone.Danger),
            new MokaRadialItem("forget", "Forget", MokaIcons.Action.Delete, Disabled: !device.CanForget)
        ],
        Sub: "Device",
        Icon: MokaIcons.File.Terminal,
        Color: MokaColor.Success,
        OnPick: pick => HandleAsync(device, pick.Answer));
}
```

A right click, Shift+F10, the context-menu key or a long press on a touch screen opens it. The spec is read when the menu opens, so building a new one on every render is fine.

## From Code

Call `ShowRadial` from any handler that has the mouse event, such as a table row's right click:

```razor
<MokaTable Items="devices" OnRowContextMenu="OpenRing">...</MokaTable>

@code {
    [Inject] private IMokaContextMenuService Menu { get; set; } = default!;

    private void OpenRing(MokaItemContextMenuArgs<Device> args) =>
        Menu.ShowRadial(args.MouseEvent, DeviceMenu(args.Item));
}
```

`ShowRadial(x, y, spec)` opens one centred on a point you choose, for a canvas or a scene that knows where things are.

## Picking

| How | What happens |
|-----|--------------|
| Click a button | Picks it |
| Flick | With the button that opened the menu still held (a right press on macOS and Linux, a long press), move toward a button and let go |
| Right, Down, Tab | Next button, clockwise, wrapping |
| Left, Up, Shift+Tab | Previous button |
| Home / End | First or last button |
| 1 to 9 | Picks that button at once |
| Enter / Space | Picks the highlighted button |
| Escape | Closes the menu, and focus goes back where it was |

The highlight follows the pointer's angle around the hub, and the hub says the name of whatever is highlighted. A menu opened from the keyboard starts on its first button; one opened with the mouse starts with nothing highlighted. A disabled button can be reached and its name shows, but it can't be picked.

## Choices

An item with `Choices` opens a list beside its button instead of picking straight away: when the pointer is on the button, or with Enter, a click or its digit. The keyboard lands on the `Checked` choice. Down and Up move in the list, Left or Escape go back to the button, and Right moves on to the next button. Picking a choice answers `{item}:{choice}`, such as `limit:50`.

The list opens on the side away from the hub and is moved back inside the window when it would cross an edge.

## Tones

`MokaRadialTone.Danger` draws the button in the error color and `Good` in the success color, lit or not. The highlighted button otherwise takes the accent. On the `Red` preset a normal and a dangerous button then look much alike when lit; the hub's word and the error color on the unlit button tell them apart.

## Models

### MokaRadialMenuSpec

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string` | **required** | The name of what the menu acts on. Shown in the hub, and names the menu for screen readers |
| `Items` | `IReadOnlyList<MokaRadialItem>` | **required** | The buttons, clockwise from the top |
| `Sub` | `string?` | `null` | The word under the title while nothing is highlighted |
| `Icon` | `MokaIconDefinition?` | `null` | An icon over the title |
| `Color` | `MokaColor` | `Primary` | The hub's edge, glow and icon |
| `OnPick` | `Func<MokaRadialPick, Task>?` | `null` | Runs with what was picked, once the menu has closed |

### MokaRadialItem

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Id` | `string` | **required** | What picking the button answers |
| `Label` | `string` | **required** | The button's name, for screen readers and the hub |
| `Icon` | `MokaIconDefinition` | **required** | The icon on the button |
| `Tone` | `MokaRadialTone` | `Default` | `Default`, `Danger` or `Good` |
| `Disabled` | `bool` | `false` | Shown faded, can't be picked |
| `Choices` | `IReadOnlyList<MokaRadialChoice>?` | `null` | A list that opens beside the button |

### MokaRadialChoice and MokaRadialPick

`MokaRadialChoice(Id, Label, Checked = false)` is one choice; `Checked` marks the current one with a check. `MokaRadialPick(Item, Choice)` is what was picked, and its `Answer` is the item's id, or `{item}:{choice}`.

## MokaRadialMenuTrigger

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | **required** | What the menu acts on. The wrapper is `display: contents` |
| `Spec` | `MokaRadialMenuSpec?` | **required** | What the menu holds. Without it a right click shows the browser's own menu |
| `Disabled` | `bool` | `false` | Opens nothing, leaving the browser's own menu |

## MokaRadialMenu

The host and the trigger render `MokaRadialMenu`. Render it yourself when you decide when it shows:

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Spec` | `MokaRadialMenuSpec?` | `null` | What the menu holds |
| `Visible` | `bool` | `false` | Opens the menu. It opens again when this turns true or the point moves |
| `X`, `Y` | `double` | `0` | The centre in viewport pixels. The ring is kept clear of the window's edges |
| `OpenedFromKeyboard` | `bool` | `false` | Starts on the first button |
| `AnchorToFocus` | `bool` | `false` | Centres on the focused element instead of `X` and `Y` |
| `Flick` | `bool` | `false` | A button is still held, so letting go over a button picks it |
| `OnPick` | `EventCallback<MokaRadialPick>` | - | Raised after the spec's `OnPick` |
| `OnClose` | `EventCallback` | - | Raised when the menu closes itself. Set `Visible` to `false` here |

`Class`, `Style`, `Id` and other attributes go on the ring, the element with `role="menu"`.

The open ring and its click-outside layer sit in the browser's top layer, so a trigger inside a card with a `transform`, a `filter` or `contain` still centres the ring on the point in the window, and nothing on the page paints over it.

## Accessibility

- The ring is a `role="menu"` named by `Title`, and each button a `menuitem` named by its `Label`, with `aria-keyshortcuts` for its digit. A disabled one has `aria-disabled`, and one with choices `aria-haspopup="menu"` and `aria-expanded`.
- A list of choices is a `menu` named by its button, and each choice a `menuitemradio` with `aria-checked`.
- Focus stays on the ring while it is open, and `aria-activedescendant` names the highlighted button or choice, so screen readers read it as it moves. Tab moves the highlight rather than leaving, and a click outside closes the menu. The hub draws the same name for the eye and is hidden from screen readers, so nothing is read twice.
- Escape, a pick or a click outside puts focus back on the element that had it, unless the picked action moved it somewhere else, such as into a dialog.
- With reduced motion the ring appears at once instead of flying out.
