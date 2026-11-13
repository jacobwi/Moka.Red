---
title: Lightbox
description: MokaLightbox, a full-screen viewer that steps through a list of images and reads each full image only as it comes near.
order: 162
---

# Lightbox

`MokaLightbox` shows one image of a list at a time, over the page, and steps through the whole list with the arrow keys or its buttons. It is a modal dialog: it takes focus when it opens, keeps Tab inside, locks the page's scrolling, and hands focus back when it closes. `MokaMediaGallery` opens one for its own lightbox, so the library keeps one lightbox; use it directly when the viewer is yours, such as one list stepped through from several galleries, or a photo opened from a table.

It takes the same `MokaMediaItem`s as the gallery. Where the full images are expensive (decrypted, read from storage, made into object URLs), `LoadSource` reads each one only when it comes near the image on show and `ReleaseSource` gives it back when it drops away, so an app needn't hand over every full `Src` up front.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IReadOnlyList<MokaMediaItem>` | Required | The items it steps through, in order |
| `Open` | `bool` | `false` | Whether it is showing. Two-way bindable. It closes itself (Escape, the close button, a click beside the image) and reports that through `OpenChanged`; a new value from the parent replaces that state. Opening shows the image at `Index` |
| `OpenChanged` | `EventCallback<bool>` | -- | Raised with `false` when it closes itself |
| `Index` | `int` | `0` | The position of the image on show, from 0. Two-way bindable; kept inside the list |
| `IndexChanged` | `EventCallback<int>` | -- | Raised when the person steps to another image |
| `LoadSource` | `Func<MokaMediaItem, CancellationToken, Task<string?>>?` | -- | Reads an item's full image and returns a URL for it. The token is cancelled once the image is no longer wanted. Returning null, or throwing, shows the image as one that can't be shown. Unset, the lightbox shows `Src`, else `ThumbnailSrc`, and a skeleton while an item has neither |
| `ReleaseSource` | `Func<MokaMediaItem, string, Task>?` | -- | Gives back a URL `LoadSource` returned, with the item it was read for |
| `Preload` | `int` | `1` | How many images on each side of the one on show are read ahead |
| `Facts` | `RenderFragment<MokaMediaItem>?` | -- | Facts about the item on show (name, date, size), across the top of the lightbox |
| `Actions` | `RenderFragment<MokaMediaItem>?` | -- | Actions on the item on show (Download, Share), in a row with the close button. See [Actions](#actions). Unset, the close button sits alone in the corner |
| `Text` | `MokaMediaText` | English | Every word it writes or a screen reader hears |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of the picture |
| `Class` / `Style` / `Id` | `string?` | -- | On the dialog element, with any other attribute |

## Opening and Closing

The app owns both, the way `MokaDialog` does: bind `Open` and `Index`, set them to open at an image, and hear the lightbox close itself through `OpenChanged`.

```razor
<MokaButton OnClick="() => _open = true">View photos</MokaButton>

<MokaLightbox Items="_photos" @bind-Open="_open" @bind-Index="_index" />

@code {
    private bool _open;
    private int _index;
    private IReadOnlyList<MokaMediaItem> _photos =
    [
        new() { Src = "/photos/pier.jpg", Alt = "Pier", Caption = "June" },
        new() { Src = "/photos/harbour.jpg", Alt = "Harbour" },
        new() { Src = "/photos/dunes.jpg", Alt = "Dunes" }
    ];
}
```

Bound one way, a parent that passes the same `Index` again changes nothing, as with every Moka.Red component; opening always starts at the `Index` the parent passes.

## Full Images on Demand

`LoadSource` is asked for the image on show first, then the next and the previous (`Preload` sets how far). While an image is on its way the lightbox shows its thumbnail, blurred and as large as a full photo would be, so the step to the full one is a sharpening; with no thumbnail it shows a `MokaSkeleton`. What drops further away is cancelled if it is still coming, or given back through `ReleaseSource` if it arrived. Closing the lightbox and removing it give back everything. A URL that arrives after it stopped being wanted, from a read that ignored its token, is given back at once.

```razor
<MokaLightbox Items="_photos" @bind-Open="_open" @bind-Index="_index"
              LoadSource="LoadAsync" ReleaseSource="ReleaseAsync" />

@code {
    // _photos carry a Key (the file id) and a ThumbnailSrc; Src stays empty.

    private async Task<string?> LoadAsync(MokaMediaItem item, CancellationToken cancellationToken)
    {
        byte[]? bytes = await Storage.ReadAsync((string)item.Key!, cancellationToken);
        return bytes is null ? null : await Js.InvokeAsync<string>("makeObjectUrl", cancellationToken, bytes, "image/jpeg");
    }

    private async Task ReleaseAsync(MokaMediaItem item, string url) =>
        await Js.InvokeVoidAsync("URL.revokeObjectURL", url);
}
```

Reads are matched to items by `Key` when the item has one, else by value. Give items a `Key`: a thumbnail arriving makes a new item, and with a `Key` its full image is not read again. An image that fails is marked for the rest of the time the lightbox is open and tried again the next time it opens. An item marked `Failed` is never read.

## Facts

`Facts` is a template given the item on show. It sits across the top of the lightbox, clear of the close button, and clicks in it don't close the lightbox. A field or control in it keeps the keys it uses: the arrows in a text box move the caret, not the image.

```razor
<MokaLightbox Items="_photos" @bind-Open="_open" @bind-Index="_index">
    <Facts>
        <span>@context.Alt</span>
        <MokaBytes Value="SizeOf(context)" />
    </Facts>
</MokaLightbox>
```

## Actions

`Actions` is a template given the item on show, for what a person does with it: download it, share it, delete it. With it the top of the lightbox becomes one row: the facts on the left, then the actions, then the close button. The facts end where the buttons begin, however many there are, and the buttons come before the close button in the order Tab takes. Clicks on the buttons don't close the lightbox; a click in the space between the facts and the buttons still does, as the space beside the image does. Without `Actions` the close button sits alone in the corner, as it always has.

`MokaLightboxAction` draws one action in the look of the lightbox's own buttons: round, the size of the close button, named by its `Text` through `aria-label` and a `MokaTooltip` below it. Any other content works in the row too, but an icon-only button there needs the same name and tooltip.

```razor
<MokaLightbox Items="_photos" @bind-Open="_open" @bind-Index="_index">
    <Actions>
        <MokaLightboxAction Icon="MokaIcons.Action.Download" Text="@($"Download {context.Alt}")"
                            Href="@context.Src" Download="@($"{context.Alt}.jpg")" />
        <MokaLightboxAction Icon="MokaIcons.Action.Share" Text="Copy link"
                            Busy="_sharing == context.Key" OnClick="() => ShareAsync(context)" />
    </Actions>
</MokaLightbox>
```

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Icon` | `MokaIconDefinition` | Required | The icon it shows |
| `Text` | `string` | Required | What it does. Its name for screen readers and the text of its tooltip |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Raised when it is pressed, unless it is busy or disabled. Works on a link too. Without a listener the button sends nothing to .NET |
| `Href` | `string?` | -- | Makes it a link, such as to the full image. `javascript:`, `vbscript:` and `data:` addresses are dropped, leaving a button |
| `Download` | `string?` | -- | With `Href`, saves what the link points at: the file name to save it as, or `""` for the name the address gives. Null is an ordinary link |
| `Busy` | `bool` | `false` | Its work is under way: a turning ring in place of the icon, presses ignored, and it stays focusable (`aria-disabled`), so the keyboard keeps its place |
| `Disabled` | `bool` | `false` | A disabled button. A disabled link is a disabled button |
| `TooltipPosition` | `MokaTooltipPosition` | `Bottom` | Where its tooltip opens |
| `Class` / `Style` / `Id` | `string?` | -- | On the button or link, with any other attribute |

`MokaMediaGallery` hands its lightbox the same template through `LightboxActions`.

## Look

Everything the lightbox draws comes from the theme's tokens, so a theme that changes them reaches it:

| Part | Token | With the default tokens |
|------|-------|-------------------------|
| Arrow buttons | `--moka-control-height`; on the original design 1.25 times it, since its controls are 2rem | 40px, as before |
| Close button and actions | Nine tenths of an arrow button | 36px, as before |
| Icons | Six tenths of an arrow button | 24px, as before |
| Corners of the buttons | `--moka-radius-full` | Round, as before |
| Offsets from the window's edges | `--moka-spacing-xl` | 16px, as before |
| The picture's shadow | `--moka-shadow-modal` | The theme's modal shadow, as before |
| Fade in, hover | `--moka-transition-normal` | 150ms, as before |

So `WithElevation(MokaElevation.Glow)` and `MokaTheme.Ember` draw a ring and a glow round the picture, `WithElevation(MokaElevation.Line)` draws no shadow, and a compact theme (`WithDensity(MokaDensity.Compact)`, `MokaTheme.Crisp`) draws 34px arrows, 30.6px buttons and 20.4px icons. The fade in keeps its ease-out curve whatever the token's curve, and under reduced motion it doesn't play.

## Words

`Text` takes a `MokaMediaText`, the same record the gallery uses, with English defaults:

| Property | Default | Where |
|----------|---------|-------|
| `Viewer` | "Image viewer" | The dialog's name, read when it opens |
| `Position` | "Image 3 of 40" | Said on every step; also names a gallery tile with neither alt text nor a caption |
| `Previous` / `Next` | "Previous image" / "Next image" | The arrow buttons and their tooltips |
| `Close` | "Close" | The close button and its tooltip |
| `Failed` | "Can't be shown" | Shown for an image that can't be shown, and said with its position |
| `LoadingLabel` / `FailedLabel` | "{name}, loading" / "{name}, can't be shown" | A gallery tile's name while it waits or when it fails |

```razor
<MokaLightbox Items="_photos" @bind-Open="_open" @bind-Index="_index"
              Text="@(new MokaMediaText { Viewer = "Visionneuse", Close = "Fermer", Position = (n, count) => $"Photo {n} sur {count}" })" />
```

## Keyboard and Screen Readers

| Key | Action |
|-----|--------|
| Left / Right | Previous or next image |
| Home / End | First or last image |
| Escape | Close |
| Tab / Shift+Tab | Move between the arrows, anything in `Facts`, the actions and the close button, in that order, without leaving the lightbox |

- Opening moves focus into the lightbox. Closing hands it back to where it was before, unless something has taken it since: the gallery moves it to the tile of the image last on show.
- The previous and next buttons go away at either end. If one of them had focus, focus goes back to the lightbox and the keys keep working.
- The counter shows "3 / 40" for the eye and says "Image 3 of 40" and the image's name to a screen reader on every step, with "Can't be shown" when it can't be.
- The arrow buttons, the close button and each `MokaLightboxAction` show only an icon, so each is named by `aria-label` and by a `MokaTooltip`.
- The keys don't scroll the page behind the lightbox. With Alt, Ctrl or Cmd held they stay browser shortcuts, so Alt+Left goes back.
- Keys pressed in the lightbox stay there. Escape closes the lightbox, not a dialog or drawer it sits in. The keys arrive through `@onmokakeydown`, which says when a field or control in `Facts` owns the press.

## Page Scrolling

The page behind the lightbox doesn't scroll while it is open. It shares the counted lock with `MokaDialog` and `MokaBottomSheet`, so a lightbox opened from a dialog and closed again leaves the page locked until the dialog closes too. A lightbox removed while open gives its lock back.
