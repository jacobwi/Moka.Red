using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Media;

/// <summary>
///     Grid of media thumbnails with hover overlay. Clickable thumbnails are buttons, and a click opens a
///     <see cref="MokaLightbox" /> over the gallery's items, a modal dialog that takes focus and locks the page's
///     scrolling while it is open. A tile draws a skeleton while its thumbnail is on its way and a struck-out picture
///     when it can't be shown.
/// </summary>
public partial class MokaMediaGallery : MokaVisualComponentBase
{
	private const string GalleryModule = "./_content/Moka.Red.Primitives/Media/MokaMediaGallery.razor.js";

	private static readonly MokaMediaText DefaultText = new();
	private static readonly string ShownTileClass = new CssBuilder("moka-media-gallery-item").Build();

	private static readonly string LoadingTileClass = new CssBuilder("moka-media-gallery-item")
		.AddClass("moka-media-gallery-item--loading")
		.Build();

	private static readonly string FailedTileClass = new CssBuilder("moka-media-gallery-item")
		.AddClass("moka-media-gallery-item--failed")
		.Build();

	// Thumbnails that failed to load, by value: a new thumbnail source makes a new item, which is tried again.
	private readonly HashSet<MokaMediaItem> _thumbnailFailed = [];

	// Stepping in the lightbox only notes where it is, so the grid doesn't redraw on every step.
	private readonly EventCallback<int> _noteLightboxIndex;

	private int? _focusAfterClose;
	private IReadOnlyList<MokaMediaItem>? _lastItems;
	private int _lightboxIndex;
	private bool _lightboxOpen;
	private ElementReference _root;

	/// <summary>Creates the gallery.</summary>
	public MokaMediaGallery() => _noteLightboxIndex = new EventCallback<int>(null, (Action<int>)NoteLightboxIndex);

	/// <summary>Collection of media items to display. Required.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaMediaItem> Items { get; set; } = [];

	/// <summary>Number of grid columns. Default 3. <see cref="MinColumn" /> wins over it.</summary>
	[Parameter]
	public int Columns { get; set; } = 3;

	/// <summary>
	///     The narrowest a tile gets, a length or a <c>var()</c> such as <c>"9rem"</c>. Set, the grid fits as many tiles
	///     as this allows in each row and they share what is left (<c>repeat(auto-fill, minmax(min(100%, x), 1fr))</c>),
	///     so a wider gallery shows more of them; it wins over <see cref="Columns" />. Null keeps <see cref="Columns" />.
	/// </summary>
	[Parameter]
	public string? MinColumn { get; set; }

	/// <summary>Gap between thumbnails. Default Sm.</summary>
	[Parameter]
	public MokaSpacingScale Gap { get; set; } = MokaSpacingScale.Sm;

	/// <summary>Whether to show a hover overlay with zoom icon. Default true.</summary>
	[Parameter]
	public bool ShowOverlay { get; set; } = true;

	/// <summary>Callback when a media item is clicked.</summary>
	[Parameter]
	public EventCallback<MokaMediaItem> OnItemClick { get; set; }

	/// <summary>
	///     Raised when a tile is pressed, with the item and its position in <see cref="Items" />, so items that are equal
	///     can be told apart and a <see cref="MokaLightbox" /> of your own opens at the right image. Raised after
	///     <see cref="OnItemClick" />.
	/// </summary>
	[Parameter]
	public EventCallback<MokaMediaItemOpenArgs> OnItemOpen { get; set; }

	/// <summary>
	///     Whether clicking an image opens a built-in fullscreen lightbox. Default true.
	///     When false, only fires OnItemClick and OnItemOpen, and you handle the preview yourself.
	/// </summary>
	[Parameter]
	public bool Lightbox { get; set; } = true;

	/// <summary>
	///     Reads an item's full image on demand for the built-in lightbox. See <see cref="MokaLightbox.LoadSource" />.
	/// </summary>
	[Parameter]
	public Func<MokaMediaItem, CancellationToken, Task<string?>>? LoadSource { get; set; }

	/// <summary>Gives back what <see cref="LoadSource" /> returned. See <see cref="MokaLightbox.ReleaseSource" />.</summary>
	[Parameter]
	public Func<MokaMediaItem, string, Task>? ReleaseSource { get; set; }

	/// <summary>Facts about the image on show in the built-in lightbox. See <see cref="MokaLightbox.Facts" />.</summary>
	[Parameter]
	public RenderFragment<MokaMediaItem>? LightboxFacts { get; set; }

	/// <summary>Every word the gallery and its lightbox write or a screen reader hears. English by default.</summary>
	[Parameter]
	public MokaMediaText Text { get; set; } = DefaultText;

	/// <inheritdoc />
	protected override string RootClass => "moka-media-gallery";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass(Class)
		.Build();

	// The root only lays out the grid and draws nothing, so a radius there would not show. Every
	// thumbnail draws its own box and takes it instead. Margin and padding stay on the root.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("grid-template-columns", ColumnsTemplate)
		.AddStyle("gap", MokaEnumHelpers.ToCssValue(Gap))
		.AddStyle(Style)
		.Build();

	private string ColumnsTemplate => CssValues.IsLengthOrVariable(MinColumn)
		? $"repeat(auto-fill, minmax(min(100%, {CssValues.WithPixels(MinColumn.Trim())}), 1fr))"
		: string.Create(CultureInfo.InvariantCulture, $"repeat({Columns}, 1fr)");

	private string? ItemStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	// A thumbnail that does nothing on click is not a button.
	private bool IsInteractive => Lightbox || OnItemClick.HasDelegate || OnItemOpen.HasDelegate;

	/// <summary>
	///     Moves focus to the tile at <paramref name="index" />, such as the tile of the image a viewer of your own showed
	///     last as it closes. The built-in lightbox does this itself. Does nothing when the tiles aren't buttons or the
	///     index is outside <see cref="Items" />.
	/// </summary>
	/// <param name="index">The tile's position in <see cref="Items" />, from 0.</param>
	/// <returns>A task that completes once the browser has been asked.</returns>
	public async Task FocusItemAsync(int index)
	{
		if (!IsInteractive || index < 0 || index >= Items.Count)
		{
			return;
		}

		await SafeModuleInvokeVoidAsync(GalleryModule, "focusItem", _root, index);
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Failed thumbnails are remembered only for items still in the gallery.
		if (!ReferenceEquals(Items, _lastItems))
		{
			_lastItems = Items;
			_thumbnailFailed.IntersectWith(Items);
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// After the render that closed the lightbox, focus goes to the tile of the image it showed last, which is where
		// the arrows left the person.
		if (_focusAfterClose is not { } index)
		{
			return;
		}

		_focusAfterClose = null;
		await FocusItemAsync(index);
	}

	private static string? ThumbnailOf(MokaMediaItem item) =>
		!string.IsNullOrWhiteSpace(item.ThumbnailSrc) ? item.ThumbnailSrc :
		!string.IsNullOrWhiteSpace(item.Src) ? item.Src : null;

	private static string TileClass(TileState state) => state switch
	{
		TileState.Loading => LoadingTileClass,
		TileState.Failed => FailedTileClass,
		_ => ShownTileClass
	};

	private TileState StateOf(MokaMediaItem item)
	{
		if (item.Failed || _thumbnailFailed.Contains(item))
		{
			return TileState.Failed;
		}

		return item.Loading || ThumbnailOf(item) is null ? TileState.Loading : TileState.Shown;
	}

	// The tiles' @key: each item's Key, unless another item shares it, since Blazor throws on a repeated key among
	// siblings. Items without a Key stay unkeyed, as every tile was before.
	private object?[] TileKeys()
	{
		object?[] keys = new object?[Items.Count];
		Dictionary<object, int>? seen = null;
		for (int i = 0; i < Items.Count; i++)
		{
			if (Items[i].Key is not { } key)
			{
				continue;
			}

			seen ??= [];
			if (seen.TryGetValue(key, out int first))
			{
				if (first >= 0)
				{
					keys[first] = null;
					seen[key] = -1;
				}

				continue;
			}

			seen[key] = i;
			keys[i] = key;
		}

		return keys;
	}

	// A thumbnail button is named by its image's alt text. Without one it falls back to the caption, then to its place
	// in the gallery. A tile with no picture to show says why after its name.
	private string? ItemLabel(MokaMediaItem item, int index, TileState state)
	{
		string name = !string.IsNullOrWhiteSpace(item.Alt) ? item.Alt :
			!string.IsNullOrWhiteSpace(item.Caption) ? item.Caption : Text.Position(index + 1, Items.Count);

		return state switch
		{
			TileState.Loading => Text.LoadingLabel(name),
			TileState.Failed => Text.FailedLabel(name),
			_ => string.IsNullOrWhiteSpace(item.Alt) ? name : null
		};
	}

	private void ThumbnailFailed(MokaMediaItem item) => _thumbnailFailed.Add(item);

	private void NoteLightboxIndex(int index) => _lightboxIndex = index;

	private void LightboxOpenChanged(bool open)
	{
		_lightboxOpen = open;
		if (!open)
		{
			_focusAfterClose = _lightboxIndex;
		}
	}

	private async Task HandleItemClick(MokaMediaItem item, int index)
	{
		if (Lightbox)
		{
			_lightboxIndex = index;
			_lightboxOpen = true;
		}

		if (OnItemClick.HasDelegate)
		{
			await OnItemClick.InvokeAsync(item);
		}

		if (OnItemOpen.HasDelegate)
		{
			await OnItemOpen.InvokeAsync(new MokaMediaItemOpenArgs(item, index));
		}
	}

	private enum TileState
	{
		Shown,
		Loading,
		Failed
	}
}
