---
title: Media
description: Carousel, Image, Media Gallery, and Video Embed components for rich media display.
order: 30
---

# Media

Moka.Red provides several components for displaying rich media: `MokaCarousel` for slideshows, `MokaImage` for enhanced images, `MokaMediaGallery` for thumbnail grids with lightbox, and `MokaVideoEmbed` for responsive video embeds.

---

## MokaCarousel

Image/content carousel with navigation arrows, dot indicators, and auto-play support. Uses CSS transforms for slide transitions with no JavaScript required.

The arrows ("Previous slide", "Next slide") and dots ("Go to slide *n*") show only a glyph, so each is named by
`aria-label` and by a `MokaTooltip`: below the arrows, lined up with the carousel's edges, and above the dots. The arrows
and dots are placed with auto margins rather than a transform, which would hold the tooltips inside them.

### MokaCarousel Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | `MokaCarouselSlide` elements or any content |
| `AutoPlay` | `bool` | `false` | Slides advance automatically |
| `Interval` | `int` | `5000` | Auto-play interval in milliseconds |
| `ShowArrows` | `bool` | `true` | Show left/right navigation arrows |
| `ShowDots` | `bool` | `true` | Show dot indicators |
| `Loop` | `bool` | `true` | Wrap around at ends |
| `ActiveIndex` | `int` | `0` | Currently active slide (two-way bindable) |
| `ActiveIndexChanged` | `EventCallback<int>` | -- | Callback when active slide changes |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the carousel. It still fills its container, with the margin inside (up to 0.1.12 a margin made it wider than the container) |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### Basic Carousel

```blazor-preview
<MokaCarousel Style="height:200px">
    <MokaCarouselSlide>
        <div style="display:flex;align-items:center;justify-content:center;height:100%;background:var(--moka-color-primary);color:white">
            Slide 1
        </div>
    </MokaCarouselSlide>
    <MokaCarouselSlide>
        <div style="display:flex;align-items:center;justify-content:center;height:100%;background:var(--moka-color-secondary);color:white">
            Slide 2
        </div>
    </MokaCarouselSlide>
    <MokaCarouselSlide>
        <div style="display:flex;align-items:center;justify-content:center;height:100%;background:var(--moka-color-success);color:white">
            Slide 3
        </div>
    </MokaCarouselSlide>
</MokaCarousel>
```

### Auto-Play

```blazor-preview
<MokaCarousel AutoPlay Interval="3000" Style="height:150px">
    <MokaCarouselSlide>
        <div style="display:flex;align-items:center;justify-content:center;height:100%;background:var(--moka-color-info);color:white">
            Auto Slide 1
        </div>
    </MokaCarouselSlide>
    <MokaCarouselSlide>
        <div style="display:flex;align-items:center;justify-content:center;height:100%;background:var(--moka-color-warning);color:white">
            Auto Slide 2
        </div>
    </MokaCarouselSlide>
</MokaCarousel>
```

---

## MokaImage

Enhanced image component with loading state, fallback, and aspect ratio control.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Src` | `string` | Required | Image source URL |
| `Alt` | `string?` | -- | Alt text |
| `Fallback` | `string?` | -- | Fallback image URL on error |
| `FallbackContent` | `RenderFragment?` | -- | Custom fallback content on error |
| `AspectRatio` | `string?` | -- | CSS aspect-ratio (e.g., `"16/9"`, `"1/1"`) |
| `ObjectFit` | `MokaObjectFit` | `Cover` | `Cover`, `Contain`, `Fill`, `None` |
| `Loading` | `bool` | `false` | Shows skeleton while loading |
| `Rounded` | `MokaRounding?` | -- | `Md`, `Lg`, `Full` (circle), etc. |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### Basic Image

```blazor-preview
<MokaImage Src="https://picsum.photos/400/200" Alt="Sample image" Style="max-width:400px" />
```

### Aspect Ratio

```blazor-preview
<div style="display:flex;gap:12px">
    <MokaImage Src="https://picsum.photos/300/300" AspectRatio="1/1" Style="width:150px" Alt="Square" />
    <MokaImage Src="https://picsum.photos/300/200" AspectRatio="16/9" Style="width:200px" Alt="Widescreen" />
</div>
```

### Rounded and Circle

```blazor-preview
<div style="display:flex;gap:12px;align-items:start">
    <MokaImage Src="https://picsum.photos/200/200" Rounded="MokaRounding.Lg" Style="width:120px" AspectRatio="1/1" Alt="Rounded" />
    <MokaImage Src="https://picsum.photos/200/200" Rounded="MokaRounding.Full" Style="width:120px" AspectRatio="1/1" Alt="Circle" />
</div>
```

---

## MokaMediaGallery

Grid of media thumbnails with hover overlay and a built-in lightbox, a [`MokaLightbox`](/components/lightbox) over the gallery's items. Supports click events for custom preview integration.

The lightbox's arrows ("Previous image", "Next image") and close button show only an icon, so each is named by
`aria-label` and by a `MokaTooltip`, below the arrows and below the close button at the window's corner.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IReadOnlyList<MokaMediaItem>` | Required | Collection of media items |
| `Columns` | `int` | `3` | Number of grid columns. `MinColumn` wins over it |
| `MinColumn` | `string?` | -- | The narrowest a tile gets, a length or a `var()` such as `"9rem"`. The grid fits as many tiles in a row as this allows and they share what is left (`repeat(auto-fill, minmax(min(100%, x), 1fr))`). A bare number is pixels; anything that isn't a length or `var()` is ignored |
| `Gap` | `MokaSpacingScale` | `Sm` | Gap between thumbnails |
| `ShowOverlay` | `bool` | `true` | Hover overlay with zoom icon |
| `OnItemClick` | `EventCallback<MokaMediaItem>` | -- | Callback when item is clicked. With it, `OnItemOpen` or `Lightbox`, the thumbnails are buttons |
| `OnItemOpen` | `EventCallback<MokaMediaItemOpenArgs>` | -- | Raised after `OnItemClick` with the item and its `Index` in `Items`, so equal items can be told apart and a lightbox of your own opens at the right image |
| `Lightbox` | `bool` | `true` | Built-in fullscreen lightbox on click |
| `LoadSource` | `Func<MokaMediaItem, CancellationToken, Task<string?>>?` | -- | Reads a full image on demand for the built-in lightbox. See [Lightbox](/components/lightbox) |
| `ReleaseSource` | `Func<MokaMediaItem, string, Task>?` | -- | Gives back what `LoadSource` returned |
| `LightboxFacts` | `RenderFragment<MokaMediaItem>?` | -- | Facts about the image on show, at the top of the built-in lightbox |
| `LightboxActions` | `RenderFragment<MokaMediaItem>?` | -- | Actions on the image on show (Download, Share), beside the built-in lightbox's close button. See [Lightbox actions](/components/lightbox#actions) |
| `TileCaption` | `RenderFragment<MokaMediaItem>?` | -- | A caption under each tile: a name, a size, a download button. See [Captions Under Tiles](#captions-under-tiles). Unset, the tiles are drawn as before |
| `Text` | `MokaMediaText` | English | Every word the gallery and its lightbox write or a screen reader hears |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of every thumbnail. `None` squares them, `Full` makes them round |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the gallery. It still fills its container, with the margin inside |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding around the grid |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

| Method | Description |
|--------|-------------|
| `FocusItemAsync(int index)` | Moves focus to the tile at `index`, such as the tile of the photo a viewer of your own showed last as it closes. Does nothing when the tiles aren't buttons or the index is outside `Items`. The built-in lightbox does this itself |

`Rounded` shapes each thumbnail, since the grid around them draws nothing:

```razor
<MokaMediaGallery Items="_photos" Columns="4" Rounded="MokaRounding.Lg" />
```

Up to 0.1.12 the radius went on the grid, where it did not show, and the gallery was `width: 100%`, so a margin made it wider than its container.

### MokaMediaItem Model

A record, so two items with the same values are equal. Give each a `Key` to tell identical ones apart.

| Property | Type | Description |
|----------|------|-------------|
| `Src` | `string` | Full-resolution image URL, shown in the lightbox. Empty by default: it can stay empty when the lightbox's `LoadSource` reads the full image on demand |
| `ThumbnailSrc` | `string?` | Thumbnail URL (falls back to `Src`) |
| `Alt` | `string?` | Alt text. Also names the thumbnail button |
| `Caption` | `string?` | Caption for overlay/lightbox. Names the thumbnail button when there is no `Alt` |
| `Key` | `object?` | Identifies the item, such as a file id. It becomes the tile's `@key`, so a tile keeps its element when the list is reordered, and it makes items that otherwise match unequal. A key several items share is ignored for all of them |
| `Loading` | `bool` | The thumbnail is on its way: the tile draws a skeleton. A tile with neither `ThumbnailSrc` nor `Src` draws one too |
| `Failed` | `bool` | The item can't be shown: the tile draws a struck-out picture and "Can't be shown", as it does for a thumbnail that fails to load, and the lightbox shows the same. Wins over `Loading` |

Up to 0.4.4 `Src` was required, a tile with no thumbnail drew an empty image on a transparent box, and a thumbnail that failed to load showed the browser's broken image.

### Gallery Example

```blazor-preview
@code {
    IReadOnlyList<MokaMediaItem> _items = new[]
    {
        new MokaMediaItem { Src = "https://picsum.photos/400/300?random=1", Alt = "Image 1" },
        new MokaMediaItem { Src = "https://picsum.photos/400/300?random=2", Alt = "Image 2" },
        new MokaMediaItem { Src = "https://picsum.photos/400/300?random=3", Alt = "Image 3" },
        new MokaMediaItem { Src = "https://picsum.photos/400/300?random=4", Alt = "Image 4" },
        new MokaMediaItem { Src = "https://picsum.photos/400/300?random=5", Alt = "Image 5" },
        new MokaMediaItem { Src = "https://picsum.photos/400/300?random=6", Alt = "Image 6" },
    };
}
<MokaMediaGallery Items="_items" Columns="3" />
```

### Tiles While Thumbnails Arrive

A photo library makes thumbnails as it goes. Hand the gallery what you have: a tile marked `Loading`, or with no thumbnail source yet, draws a `MokaSkeleton`; one marked `Failed`, or whose thumbnail fails to load, draws a struck-out picture and a word. A tile that is a button says it after its name ("Pier, loading", "Pier, can't be shown"). A thumbnail that failed is tried again once the item gets a new thumbnail source.

```razor
<MokaMediaGallery Items="_photos" MinColumn="9rem" />

@code {
    private IReadOnlyList<MokaMediaItem> _photos =
    [
        new() { Key = "f1", ThumbnailSrc = "/thumbs/f1.jpg", Alt = "Pier" },
        new() { Key = "f2", Alt = "Harbour" },
        new() { Key = "f3", ThumbnailSrc = "/thumbs/f3.jpg", Alt = "Dunes", Loading = true },
        new() { Key = "f4", Alt = "scan.heic", Failed = true }
    ];
}
```

The second tile has no thumbnail yet and the third is marked as waiting for a new one, so both draw a skeleton; the fourth can't be shown.

### Columns That Fill Rows

`MinColumn` fits as many tiles in each row as the width allows, so a wider gallery shows more of them. It is the name `MokaGrid`, `MokaRadioGroup` and `MokaDataList` use for the same thing, and it wins over `Columns`.

```razor
<MokaMediaGallery Items="_photos" MinColumn="var(--my-tile-size)" Gap="MokaSpacingScale.Xs" />
```

### Captions Under Tiles

`TileCaption` is a template given each tile's item, drawn in a row under the tile: a name, a size, a download button. The row's first element takes the room left and is cut short with an ellipsis, and the rest keep their size at its end, so put the name first. Give the name a `title` when it may be cut, so the whole of it can still be read.

The caption sits beside the tile's button rather than in it, so pressing a control in it doesn't open the lightbox or raise `OnItemClick`. Each tile and its caption become a `figure` and its `figcaption`; without `TileCaption` the tiles are drawn as before. An icon-only button in the caption follows the library's rule: named by `aria-label` and in a `MokaTooltip` with the same name.

`LightboxActions` puts actions in the built-in lightbox, beside its close button (see [Lightbox actions](/components/lightbox#actions)).

```razor
<MokaMediaGallery Items="_photos" MinColumn="9rem">
    <TileCaption>
        <span title="@context.Alt">@context.Alt</span>
        <MokaBytes Value="SizeOf(context)" Style="font-family: var(--moka-font-family-mono);" />
        <MokaTooltip Text="Download">
            <MokaButton Quiet Size="MokaSize.Sm" StartIcon="MokaIcons.Action.Download"
                        aria-label="@($"Download {context.Alt}")" OnClick="() => DownloadAsync(context)" />
        </MokaTooltip>
    </TileCaption>
    <LightboxActions>
        <MokaLightboxAction Icon="MokaIcons.Action.Download" Text="@($"Download {context.Alt}")"
                            Href="@context.Src" Download="" />
    </LightboxActions>
</MokaMediaGallery>
```

### A Viewer of Your Own

Turn the built-in lightbox off and open a `MokaLightbox` from `OnItemOpen`, whose `Index` says which tile was pressed even among equal items. When it closes, `FocusItemAsync` hands focus to the tile of the photo last on show.

```razor
<MokaMediaGallery @ref="_gallery" Items="_photos" Lightbox="false" OnItemOpen="Open" />
<MokaLightbox Items="_photos" @bind-Open="_open" @bind-Open:after="ClosedAsync" @bind-Index="_index" />

@code {
    private MokaMediaGallery? _gallery;
    private bool _open;
    private int _index;

    private void Open(MokaMediaItemOpenArgs args)
    {
        _index = args.Index;
        _open = true;
    }

    private async Task ClosedAsync()
    {
        if (!_open && _gallery is not null)
        {
            await _gallery.FocusItemAsync(_index);
        }
    }
}
```

### Keyboard and Screen Readers

A thumbnail that does something on click (the lightbox, `OnItemClick` or `OnItemOpen`) is a native button, so Tab reaches it and Enter or Space open it. Its name is the image's `Alt`, then its `Caption`, then its place in the gallery ("Image 3 of 6", `Text.Position`). With `Lightbox="false"` and neither callback, the thumbnails are plain images and take no focus; a plain tile with no picture to show is a named image (`role="img"`).

The built-in lightbox is a `MokaLightbox`; its keys, focus and words are on the [Lightbox](/components/lightbox) page. Opening moves focus into it; closing moves focus to the thumbnail of the image on show, so after browsing with the arrows you land where you stopped. Stepping through it doesn't redraw the grid.

Up to 0.4.4 the lightbox was drawn by the gallery itself. Its markup is the same apart from the counter, which now holds the figures for the eye (`aria-hidden`) and the words a screen reader hears ("Image 3 of 6" and the image's name) in hidden spans, where it used to read "3 / 6" aloud.

### Page Scrolling

The page behind the lightbox doesn't scroll while it is open (the lightbox sets `overflow: hidden` on the body). It shares this lock with `MokaDialog` and `MokaBottomSheet`, and the lock is counted: a lightbox opened from a dialog and closed again leaves the page locked until the dialog closes too. A gallery removed while its lightbox is open gives its lock back.

Up to 0.1.12 the thumbnails took no focus, and opening the lightbox left focus on the page, so its keys only worked after a click inside it. The page behind the lightbox scrolled, and Escape in a lightbox inside a dialog closed the dialog as well.

---

## MokaVideoEmbed

Responsive video embed supporting YouTube, Vimeo, and direct video URLs. Auto-detects the platform from the URL and converts to the appropriate embed format.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Src` | `string` | Required | Video URL or embed URL. Only http and https addresses are loaded in the iframe |
| `Title` | `string?` | -- | Accessibility title (iframe title attribute) |
| `AspectRatio` | `string` | `"16/9"` | CSS aspect ratio |
| `AllowFullscreen` | `bool` | `true` | Allow fullscreen |
| `AutoPlay` | `bool` | `false` | Auto-play the video |
| `Rounded` | `MokaRounding?` | -- | Border radius |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the player. It still fills its container, with the margin inside (up to 0.1.12 a margin made it wider than the container) |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### YouTube Embed

The component auto-converts standard YouTube URLs to embed format.

```razor
<MokaVideoEmbed Src="https://www.youtube.com/watch?v=dQw4w9WgXcQ"
                Title="Video Title"
                Rounded="MokaRounding.Md" />
```

### Vimeo Embed

```razor
<MokaVideoEmbed Src="https://vimeo.com/123456789" Title="Vimeo Video" />
```

### Custom Aspect Ratio

```razor
<MokaVideoEmbed Src="https://www.youtube.com/watch?v=example"
                AspectRatio="4/3"
                Title="4:3 Video" />
```

### Behaviour

- `youtube.com/watch?v=` and `youtu.be/` links become `https://www.youtube.com/embed/...`, and `vimeo.com/<id>` links become `https://player.vimeo.com/video/<id>`. Other YouTube and Vimeo addresses, such as an embed URL, are used as given.
- Only an http or https address, or a protocol-relative one starting with `//`, is loaded in the iframe. Anything else goes to a plain `<video>` element, so a `javascript:` URL that mentions youtube.com cannot run script in your page.
- Any other URL plays in a native `<video>` element with controls.
