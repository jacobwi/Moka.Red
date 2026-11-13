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

The spec's `OnPick` runs from the ring, so on its own only the ring redraws. Pass the component that shows what the pick
changes, usually `this`, and the pick runs through it the way an `EventCallback` runs through its receiver, so it
redraws with no `StateHasChanged()`. A trigger takes it as `Owner`; without one it redraws itself and the content
inside it.

```razor
Menu.ShowRadial(args.MouseEvent, DeviceMenu(args.Item), this);

<MokaRadialMenuTrigger Spec="DeviceMenu(device)" Owner="this">...</MokaRadialMenuTrigger>
```

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

`MokaRadialTone.Danger` draws the button in the error color and `Good` in the success color, lit or not. The highlighted button otherwise takes the accent. On the `Red` preset a normal and a dangerous button then look much alike when lit; the hub's word and the error color on the unlit button tell them apart. Every shape and look below keeps the tones.

## Shapes

`Shape` (`MokaRadialShape`) says how the buttons sit around the target. Set it on the spec, so `ShowRadial` carries it, or on the trigger, the menu or the host. In every shape the highlight follows the pointer's angle, so the keys, the digits and the flick work the same.

| Shape | What it draws |
|-------|---------------|
| `Ring` | The default: round buttons on a band around the hub |
| `Pie` | Wedges that fill the ring out to its edge, each with its icon and word. The lit wedge fills with the accent and swings round to follow the pointer or the keys |
| `HalfRing` | The top half of a larger ring, opening upward from a half hub on the row. For something at the bottom of the window or under a thumb |
| `CornerFan` | A quarter circle with slightly smaller buttons. It opens toward the middle of the window from where it was opened, so it never runs off an edge |
| `Compact` | Small buttons close around the pointer, a dot for the centre and no hub. Only the lit button shows its word, in a tag outside it |
| `Labeled` | The ring with each button's word and digit printed outside it. The hub keeps saying what the thing is |
| `Marking` | Words in pills at the ends of lines from the centre. The line to the lit word lights up, so a flick shows where it is headed |

```razor
<MokaRadialMenuTrigger Spec="FileMenu(file)" Shape="MokaRadialShape.Pie">
    <FileRow File="file" />
</MokaRadialMenuTrigger>
```

```razor
Menu.ShowRadial(e, FileMenu(file) with { Shape = MokaRadialShape.Marking });
```

A `CornerFan` measures the window once as it opens, before it is drawn, to choose its corner. The pie's swing runs in the browser: the module turns the wedge for the pointer and watches the rendered highlight for the keys, so no key costs an extra call.

### Edges

`AdaptToEdges` lets a `Ring` change shape by itself where a whole ring would be pushed off its target: a `HalfRing` near the bottom of the window and a `CornerFan` near a corner. Anywhere else, and near the top or a side edge alone, it stays a ring. It is off by default.

```razor
<MokaContextMenuHost RadialAdaptToEdges="true" />
```

## Looks

`Look` (`MokaRadialLook`) sets the surfaces. Each look works with every shape.

| Look | What it draws |
|------|---------------|
| `Standard` | Raised buttons with a border and a shadow, on a band edged with the accent. The lit button fills with the accent and glows |
| `Glass` | See-through buttons that blur the page behind them. The lit one takes a tint of the accent. It costs a backdrop blur per button while the menu is open |
| `Solid` | Flat buttons on one raised disc, with no band. Only the lit button has color: it fills with the accent and grows |
| `Outline` | Thin rings over a light veil. The lit button gets the accent ring the library uses for focus |
| `Terminal` | Square-ish tiles with a monospace word under each icon, a dashed track, a glow on the lit tile and a prompt in the hub |
| `Soft` | Borderless buttons on a plain band, the lit one tinted |

Left unset, the menu follows the theme's popup look (`MokaTheme.WithPopups`) where a radial look has the same name: `Glass` and `Terminal`, so `WithComponentStyle(MokaComponentStyle.Terminal)` gives a Terminal ring. Any other popup look draws `Standard`. `MokaTheme.Dark` and `Light` never follow the theme and draw as they always have, but a look set on the spec, the trigger, the menu or the host still draws on them.

```razor
<MokaRadialMenuTrigger Spec="FileMenu(file)" Look="MokaRadialLook.Solid">
    <FileRow File="file" />
</MokaRadialMenuTrigger>
```

## Choice styles

`ChoiceStyle` (`MokaRadialChoiceStyle`) says how a button's `Choices` show.

| Style | What happens |
|-------|--------------|
| `List` | The default: a list beside the button, on the side away from the hub |
| `Ring` | The choices sit on a short arc outside the button, on a band cut to the arc. A flick carries on outward: press, pass through the button, keep going to a choice, let go |
| `Replace` | Picking the button swaps the ring for its choices, and the hub becomes Back. A click on the hub or Escape goes back to the button. Big targets and room for many choices |
| `Flyout` | A smaller list that grows straight out from the button along its direction, tied to it by a short accent line |

```razor
@code {
    private MokaRadialMenuSpec FileMenu(FileItem file) => new(
        file.Name,
        [
            new MokaRadialItem("open", "Open", MokaIcons.Content.ExternalLink),
            new MokaRadialItem("share", "Share", MokaIcons.Action.Share, Choices:
            [
                new MokaRadialChoice("email", "Email") { Icon = MokaIcons.Action.Send },
                new MokaRadialChoice("link", "Link", Checked: true) { Icon = MokaIcons.Content.Link },
                new MokaRadialChoice("team", "Team") { Icon = MokaIcons.Content.Users }
            ])
        ],
        Sub: file.Size,
        OnPick: pick => ShareAsync(file, pick.Answer))
    {
        ChoiceStyle = MokaRadialChoiceStyle.Replace,
        BackText = "Back"
    };
}
```

A choice's `Icon` shows before its label in every style, and in `Replace` it is the face of the choice's round button, which otherwise shows the label. With `Replace` the keys move round the choices as they do round the buttons, digits pick a choice, Enter picks the highlighted one, and Escape goes back before it closes. `BackText` is the hub's word for going back.

A list or flyout that would cover its own button or printed word moves on in the direction it opens until it is clear, then back inside the window.

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
| `Shape` | `MokaRadialShape?` | `null` | Init property. How the buttons sit. Null leaves it to the menu, else `Ring` |
| `Look` | `MokaRadialLook?` | `null` | Init property. The surfaces. Null leaves it to the menu, else the theme's |
| `ChoiceStyle` | `MokaRadialChoiceStyle?` | `null` | Init property. How choices show. Null leaves it to the menu, else `List` |
| `AdaptToEdges` | `bool?` | `null` | Init property. Whether a ring changes shape near the window's edges. Null leaves it off |
| `BackText` | `string?` | `null` | Init property. The hub's word for going back with `Replace`. Null says "Back" |

A component parameter set on the menu or the trigger wins over the spec, and the spec wins over the host's defaults.

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

`MokaRadialChoice(Id, Label, Checked = false)` is one choice; `Checked` marks the current one with a check. Its `Icon` (`MokaIconDefinition?`, an init property) shows before the label, and is the face of the choice's button with `Replace`. `MokaRadialPick(Item, Choice)` is what was picked, and its `Answer` is the item's id, or `{item}:{choice}`.

## MokaRadialMenuTrigger

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | **required** | What the menu acts on. The wrapper is `display: contents` |
| `Spec` | `MokaRadialMenuSpec?` | **required** | What the menu holds. Without it a right click shows the browser's own menu |
| `Disabled` | `bool` | `false` | Opens nothing, leaving the browser's own menu |
| `Shape` | `MokaRadialShape?` | `null` | Wins over the spec's when set |
| `Look` | `MokaRadialLook?` | `null` | Wins over the spec's when set |
| `ChoiceStyle` | `MokaRadialChoiceStyle?` | `null` | Wins over the spec's when set |
| `AdaptToEdges` | `bool?` | `null` | Wins over the spec's when set |
| `Owner` | `IHandleEvent?` | `null` | The component that redraws after the spec's `OnPick` runs. Pass `this`. Null redraws the trigger and the content inside it |

The trigger puts its own values into the spec it opens, so the shared host draws them too.

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
| `Shape` | `MokaRadialShape?` | `null` | Wins over the spec's. Null takes the spec's, else `Ring`. A change while open opens it again in the new shape |
| `Look` | `MokaRadialLook?` | `null` | Wins over the spec's. Null takes the spec's, else follows the theme's popups (`Glass`, `Terminal`), else `Standard` |
| `ChoiceStyle` | `MokaRadialChoiceStyle?` | `null` | Wins over the spec's. Null takes the spec's, else `List` |
| `AdaptToEdges` | `bool?` | `null` | Wins over the spec's. Null takes the spec's, else off |
| `BackText` | `string?` | `null` | Wins over the spec's. Null takes the spec's, else "Back" |
| `OnPick` | `EventCallback<MokaRadialPick>` | - | Raised after the spec's `OnPick` |
| `Owner` | `IHandleEvent?` | `null` | The component the spec's `OnPick` runs through, which redraws afterwards. `OnPick` here has its own receiver already |
| `OnClose` | `EventCallback` | - | Raised when the menu closes itself. Set `Visible` to `false` here |

`Class`, `Style`, `Id` and other attributes go on the ring, the element with `role="menu"`.

## MokaContextMenuHost

The host draws every radial menu the service opens, and can give them all a default. A spec's own value wins over these.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `RadialShape` | `MokaRadialShape?` | `null` | The shape of every ring, else `Ring` |
| `RadialLook` | `MokaRadialLook?` | `null` | The look of every ring, else the theme's |
| `RadialChoiceStyle` | `MokaRadialChoiceStyle?` | `null` | How every ring shows choices, else `List` |
| `RadialAdaptToEdges` | `bool?` | `null` | Whether every ring changes shape near the window's edges, else off |

```razor
<MokaContextMenuHost RadialShape="MokaRadialShape.Labeled" RadialChoiceStyle="MokaRadialChoiceStyle.Flyout" />
```

The open ring and its click-outside layer sit in the browser's top layer, so a trigger inside a card with a `transform`, a `filter` or `contain` still centres the ring on the point in the window, and nothing on the page paints over it.

## Accessibility

- The ring is a `role="menu"` named by `Title`, and each button a `menuitem` named by its `Label`, with `aria-keyshortcuts` for its digit. A disabled one has `aria-disabled`, and one with choices `aria-haspopup="menu"` and `aria-expanded`.
- A list of choices is a `menu` named by its button, and each choice a `menuitemradio` with `aria-checked`.
- Focus stays on the ring while it is open, and `aria-activedescendant` names the highlighted button or choice, so screen readers read it as it moves. Tab moves the highlight rather than leaving, and a click outside closes the menu. The hub draws the same name for the eye and is hidden from screen readers, so nothing is read twice.
- Escape, a pick or a click outside puts focus back on the element that had it, unless the picked action moved it somewhere else, such as into a dialog.
- Words a shape or look prints for the eye (the words on a pie's wedges, a marking menu's pills and Terminal's tiles, the labels, the compact tag, the digits) are hidden from screen readers, since each button is already named by its label.
- With `Replace`, the ring is named by the button whose choices it shows, and each choice is a `menuitemradio` with `aria-checked`. The hub that goes back is for the pointer; Escape does the same from the keyboard.
- With reduced motion the ring appears at once instead of flying out, and the pie's wedge jumps instead of swinging.
