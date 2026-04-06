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

Grid of media thumbnails with hover overlay and built-in lightbox. Supports click events for custom preview integration.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IReadOnlyList<MokaMediaItem>` | Required | Collection of media items |
| `Columns` | `int` | `3` | Number of grid columns |
| `Gap` | `MokaSpacingScale` | `Sm` | Gap between thumbnails |
| `ShowOverlay` | `bool` | `true` | Hover overlay with zoom icon |
| `OnItemClick` | `EventCallback<MokaMediaItem>` | -- | Callback when item is clicked |
| `Lightbox` | `bool` | `true` | Built-in fullscreen lightbox on click |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaMediaItem Model

| Property | Type | Description |
|----------|------|-------------|
| `Src` | `string` | Full-resolution image URL (required) |
| `ThumbnailSrc` | `string?` | Thumbnail URL (falls back to `Src`) |
| `Alt` | `string?` | Alt text |
| `Caption` | `string?` | Caption for overlay/lightbox |

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

---

## MokaVideoEmbed

Responsive video embed supporting YouTube, Vimeo, and direct video URLs. Auto-detects the platform from the URL and converts to the appropriate embed format.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Src` | `string` | Required | Video URL or embed URL |
| `Title` | `string?` | -- | Accessibility title (iframe title attribute) |
| `AspectRatio` | `string` | `"16/9"` | CSS aspect ratio |
| `AllowFullscreen` | `bool` | `true` | Allow fullscreen |
| `AutoPlay` | `bool` | `false` | Auto-play the video |
| `Rounded` | `MokaRounding?` | -- | Border radius |
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
