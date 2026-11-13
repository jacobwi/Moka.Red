---
title: Button
description: Accessible, performant button with variants, colors, sizes, icons, and loading state.
order: 1
---

# Button

`MokaButton` is the primary interactive element. It renders as a `<button>` element by default and automatically switches to `<a>` when an `Href` is provided. All visual states are expressed via CSS modifier classes.

`MokaButtonGroup` connects multiple buttons with shared border radius and no gaps.

## Parameters

### MokaButton

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | Label content |
| `Variant` | `MokaVariant` | `Filled` | `Filled`, `Outlined`, `Text`, `Soft` |
| `Color` | `MokaColor?` | `Primary` | `Primary`, `Secondary`, `Error`, `Warning`, `Success`, `Info`, `Surface` |
| `Size` | `MokaSize` | `Md` | `Xs`, `Sm`, `Md`, `Lg` |
| `StartIcon` | `MokaIconDefinition?` | - | Icon displayed before the label |
| `EndIcon` | `MokaIconDefinition?` | - | Icon displayed after the label |
| `Loading` | `bool` | `false` | Busy: a spinner in the icon's place, focus and width kept, presses and form submits ignored. See [Loading State](#loading-state) |
| `LoadingText` | `string` | `"Loading"` | What a screen reader hears when the button becomes busy |
| `Disabled` | `bool` | `false` | Disables the button (native `disabled`) |
| `DisabledLook` | `MokaDisabledButtonLook?` | `null` | How it's drawn while disabled, ahead of the theme's `DisabledButtons`. New design only, see [Disabled looks](#disabled-looks) |
| `FullWidth` | `bool` | `false` | Stretches the button to fill its container |
| `Quiet` | `bool` | `false` | The quiet look for an icon-only button. `Variant` is ignored and `Size` changes only the icon |
| `Look` | `MokaButtonLook?` | `null` | How this button is drawn, ahead of the theme's `Buttons`. `null` follows the theme. New design only, see [Looks](#looks) |
| `Href` | `string?` | - | Renders as `<a>` when set. A `javascript:`, `vbscript:` or `data:` URL is not rendered: the button stays a `<button>` |
| `Target` | `string?` | - | Link target (e.g., `_blank`) |
| `Type` | `string` | `"button"` | HTML button type attribute |
| `OnClick` | `EventCallback<MouseEventArgs>` | - | Click callback |
| `Class` | `string?` | - | Additional CSS classes |
| `Style` | `string?` | - | Additional inline styles |

### MokaButtonGroup

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | `MokaButton` elements |
| `Orientation` | `MokaDirection` | `Row` | `Row` or `Column` layout |

## Variants

```blazor-preview
<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaButton Variant="MokaVariant.Filled">Filled</MokaButton>
    <MokaButton Variant="MokaVariant.Outlined">Outlined</MokaButton>
    <MokaButton Variant="MokaVariant.Text">Text</MokaButton>
    <MokaButton Variant="MokaVariant.Soft">Soft</MokaButton>
</div>
```

### Quiet text buttons

On the new design a text button at the primary color, which is what Cancel, Close and Edit usually are, is drawn in the light accent. `MokaTheme.WithTextButtons(MokaTextButtonColor.Neutral)` draws it in quiet text that takes the accent on hover and keyboard focus, so the accent is left for what matters on the page. A text button given another `Color` keeps it, and `MokaSplitButton`'s text variant follows the same choice. `MokaTheme.Ember` has it on; `MokaTheme.Dark` and `Light` don't change.

```razor
<MokaThemeProvider Theme="_theme">
    <MokaButton Variant="MokaVariant.Text">Cancel</MokaButton>
    <MokaButton Color="MokaColor.Error">Move to bin</MokaButton>
</MokaThemeProvider>

@code {
    private readonly MokaTheme _theme = MokaTheme.Red.WithTextButtons(MokaTextButtonColor.Neutral);
}
```

## Colors

```blazor-preview
<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaButton Color="MokaColor.Primary">Primary</MokaButton>
    <MokaButton Color="MokaColor.Secondary">Secondary</MokaButton>
    <MokaButton Color="MokaColor.Success">Success</MokaButton>
    <MokaButton Color="MokaColor.Warning">Warning</MokaButton>
    <MokaButton Color="MokaColor.Error">Error</MokaButton>
    <MokaButton Color="MokaColor.Info">Info</MokaButton>
</div>
```

## Sizes

```blazor-preview
<div style="display:flex;gap:8px;align-items:center">
    <MokaButton Size="MokaSize.Xs">Xs</MokaButton>
    <MokaButton Size="MokaSize.Sm">Sm</MokaButton>
    <MokaButton Size="MokaSize.Md">Md</MokaButton>
    <MokaButton Size="MokaSize.Lg">Lg</MokaButton>
</div>
```

## Icons

Use `StartIcon` and `EndIcon` to place icons adjacent to the label. Omit `ChildContent` for an icon-only button.

```blazor-preview
<div style="display:flex;gap:8px;align-items:center">
    <MokaButton StartIcon="MokaIcons.Action.Save">Save</MokaButton>
    <MokaButton EndIcon="MokaIcons.Navigation.ArrowRight">Next</MokaButton>
    <MokaButton StartIcon="MokaIcons.Action.Add" Variant="MokaVariant.Outlined" />
    <MokaButton StartIcon="MokaIcons.Action.Delete" Color="MokaColor.Error" Variant="MokaVariant.Soft" />
</div>
```

## Quiet

`Quiet` is for an icon-only button in a row or a list: a fixed square with no border or fill and a faint icon that brightens on hover. With `Color="MokaColor.Error"` the hover turns red.

It has no text, so whoever uses it has to name it: give it an `aria-label` and put it in a `MokaTooltip` that shows the same name.

```razor
<MokaTooltip Text="Rename">
    <MokaButton Quiet StartIcon="MokaIcons.Action.Edit" aria-label="Rename" />
</MokaTooltip>
<MokaTooltip Text="Delete">
    <MokaButton Quiet Color="MokaColor.Error" StartIcon="MokaIcons.Action.Delete" aria-label="Delete" />
</MokaTooltip>
```

## Looks

On the new design (`Red`, `Gate`, `Crisp` and `Matrix`) buttons have seven looks besides `Standard`. They work with any
colors, corners, density, text size and letter case.

| Look | What changes |
|------|--------------|
| `Raised` | A light top edge and a faint shadow, pressed in on click. Secondary buttons get a soft top to bottom shade |
| `Soft` | Secondary and outlined buttons are filled grey with no border. A colored filled button (Delete in red) turns into its soft tint |
| `Pill` | Fully round ends whatever the corners setting, with a little more room each side. A row in a `MokaButtonGroup` becomes one pill |
| `Glow` | Everything outlined. The main button is an accent outline and glows on hover, the colored ones glow in their own color. With uppercase letter case it is the original Moka button |
| `Offset` | A hard outline in the text color and a solid offset shadow that the button lifts from on hover and presses into on click |
| `Gradient` | The main button and the colored filled buttons run from their color to a warmer shade of it. The rest stays flat |
| `Ghost` | Secondary, outlined and colored filled buttons show no border or fill until hovered. The main button stays filled |

The main button keeps the theme's `WithMainButton` fill under every look but `Glow`, which outlines it, and `Gradient`, which
runs from that fill. Text and `Quiet` buttons stay as they are, except that `Pill` rounds them too. `Raised` and `Offset` don't
move for people who ask for reduced motion.

Set one for the whole app on the theme:

```razor
<MokaThemeProvider Theme="MokaTheme.Red.WithButtons(MokaButtonLook.Raised)">
    @Body
</MokaThemeProvider>
```

`MokaTheme.WithComponentStyle` sets it along with the toasts, notifications, popups, lists and dialogs: `Soft` gives soft
buttons, and `Terminal` glowing ones.

Or on one button, with `Look`, which wins over the theme. `MokaButtonLook.Standard` keeps a button standard in an app that
picked another look:

```razor
<MokaButton Look="MokaButtonLook.Pill">Get started</MokaButton>
<MokaButton Look="MokaButtonLook.Standard" Variant="MokaVariant.Outlined">Cancel</MokaButton>
```

`MokaTheme.Dark` and `MokaTheme.Light` are the original design and ignore both: `WithButtons` does nothing on them, and a
button's `Look` changes nothing under them.

## Loading State

While `Loading` is true the button is busy:

- A spinner takes the start icon's place, else the end icon's, else it covers the label. The label stays where it was, so the
  button keeps its width and its accessible name. Icon-only and `Quiet` buttons show the spinner in the icon's place.
- The button stays enabled and focusable, with `aria-disabled="true"` and `aria-busy="true"`, so a keyboard user who pressed
  Save keeps their place. A native `disabled` would have dropped focus to the page.
- A click, Enter or Space does nothing: `OnClick` isn't raised, and the click's default action is cancelled, so a
  `Type="submit"` button doesn't submit its form, by a click or by Enter in one of the form's fields.
- `LoadingText` ("Loading" unless set) is announced from a hidden status region inside the button. The visible label doesn't
  change, so the button doesn't jump in width and still says what is running.

Until the button renders interactively nothing could cancel a click, so a prerendered or static busy button is natively
disabled. `Disabled` is the native `disabled` as always, and a link button (`Href`) is disabled while busy as before. The
spinner stands still for people who ask for reduced motion.

```blazor-preview
@code {
    bool _saving;
    async Task Save() {
        _saving = true;
        await Task.Delay(2000);
        _saving = false;
    }
}
<MokaButton Loading="_saving" OnClick="Save" StartIcon="MokaIcons.Action.Save">Save</MokaButton>
```

A submit button in a form:

```razor
<EditForm Model="_model" OnValidSubmit="SendAsync">
    <MokaTextField @bind-Value="_model.Name" Label="Name" />
    <MokaButton Type="submit" Loading="_sending" LoadingText="Sending">Send</MokaButton>
</EditForm>

@code {
    private readonly Contact _model = new();
    private bool _sending;

    private async Task SendAsync()
    {
        _sending = true;
        await Api.SendAsync(_model);
        _sending = false;
    }
}
```

## Disabled

```blazor-preview
<div style="display:flex;gap:8px">
    <MokaButton Disabled>Disabled Filled</MokaButton>
    <MokaButton Disabled Variant="MokaVariant.Outlined">Disabled Outlined</MokaButton>
</div>
```

### Disabled looks

On the new design a disabled button is drawn in its own colors, faded (`MokaDisabledButtonLook.Faded`, the default). A solid
one then becomes a dim version of its fill, which can read as broken rather than unavailable. Two other looks draw every
disabled button in neutral colors, whatever its color and its `Look`:

| Look | What it draws |
|------|---------------|
| `Faded` | The button's own colors at the disabled opacity. The default |
| `Muted` | A neutral fill and muted text for filled and soft buttons, a neutral outline for outlined ones, muted text for text buttons |
| `Outline` | No fill: a neutral outline and muted text. Text buttons only mute their text |

A busy button (`Loading`) keeps its colors under every look: it's working, not unavailable. `Quiet` buttons keep the faded look.

Set one for the whole app on the theme:

```razor
<MokaThemeProvider Theme="MokaTheme.Red.WithDisabledButtons(MokaDisabledButtonLook.Muted)">
    @Body
</MokaThemeProvider>
```

Or on one button with `DisabledLook`, which wins over the theme:

```razor
<MokaButton Disabled="@(!_ready)" DisabledLook="MokaDisabledButtonLook.Muted">Create account</MokaButton>
```

Only the outermost `MokaThemeProvider` sets the theme's look, as with `WithButtons`. `MokaTheme.Dark` and `MokaTheme.Light`
ignore both: `WithDisabledButtons` does nothing on them.

## Full Width

```blazor-preview
<MokaButton FullWidth>Full Width Button</MokaButton>
```

## In a Row

A button keeps its whole label in a flex row: the text beside it wraps first. When a row is too narrow for its buttons, give
it `flex-wrap: wrap`, as `MokaDialog`'s actions and `MokaWizard`'s buttons have. A button given a fixed width ends its label
with an ellipsis.

```blazor-preview
<div style="display: flex; align-items: center; gap: 8px; width: 320px;">
    <span>localhost:5080, server 0.1.0</span>
    <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm">Open a vault</MokaButton>
    <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm">Change server</MokaButton>
</div>
```

## Link Button

When `Href` is set the component renders as an `<a>` element while keeping button styling.

```blazor-preview
<MokaButton Href="https://github.com" Target="_blank" EndIcon="MokaIcons.Content.Link">
    Open GitHub
</MokaButton>
```

## ButtonGroup

```blazor-preview
<MokaButtonGroup>
    <MokaButton Variant="MokaVariant.Outlined" StartIcon="MokaIcons.Navigation.ChevronLeft" />
    <MokaButton Variant="MokaVariant.Outlined">Page 1</MokaButton>
    <MokaButton Variant="MokaVariant.Outlined" EndIcon="MokaIcons.Navigation.ChevronRight" />
</MokaButtonGroup>
```

```blazor-preview
<MokaButtonGroup Orientation="MokaDirection.Column">
    <MokaButton Variant="MokaVariant.Outlined">Top</MokaButton>
    <MokaButton Variant="MokaVariant.Outlined">Middle</MokaButton>
    <MokaButton Variant="MokaVariant.Outlined">Bottom</MokaButton>
</MokaButtonGroup>
```
