using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Media;

/// <summary>
///     Grid of media thumbnails with hover overlay. Emits click events for lightbox integration.
///     Clickable thumbnails are buttons, and the lightbox is a modal dialog that takes focus and
///     locks the page's scrolling while it is open.
/// </summary>
public partial class MokaMediaGallery : MokaVisualComponentBase
{
	private const string GalleryModule = "./_content/Moka.Red.Primitives/Media/MokaMediaGallery.razor.js";

	private int _lightboxIndex;
	private MokaMediaItem? _lightboxItem;
	private bool _lightboxShown;
	private ElementReference _root;
	private ElementReference _lightbox;

	/// <summary>Collection of media items to display. Required.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaMediaItem> Items { get; set; } = [];

	/// <summary>Number of grid columns. Default 3.</summary>
	[Parameter]
	public int Columns { get; set; } = 3;

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
	///     Whether clicking an image opens a built-in fullscreen lightbox. Default true.
	///     When false, only fires OnItemClick, and you handle the preview yourself.
	/// </summary>
	[Parameter]
	public bool Lightbox { get; set; } = true;

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
		.AddStyle("grid-template-columns", $"repeat({Columns}, 1fr)")
		.AddStyle("gap", MokaEnumHelpers.ToCssValue(Gap))
		.AddStyle(Style)
		.Build();

	private string? ItemStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	// A thumbnail that does nothing on click is not a button.
	private bool IsInteractive => Lightbox || OnItemClick.HasDelegate;

	private static string GetThumbnailSrc(MokaMediaItem item) =>
		item.ThumbnailSrc ?? item.Src;

	// A thumbnail button is named by its image's alt text. Without one it falls back to the
	// caption, then to its place in the gallery.
	private string? ItemLabel(MokaMediaItem item, int index)
	{
		if (!string.IsNullOrWhiteSpace(item.Alt))
		{
			return null;
		}

		return string.IsNullOrWhiteSpace(item.Caption)
			? string.Create(CultureInfo.CurrentCulture, $"Image {index + 1} of {Items.Count}")
			: item.Caption;
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Focus moves into the lightbox when it opens and back to a thumbnail when it closes: the
		// one on show, which is where the arrows left the user. The script also locks the page's
		// scrolling on open and gives the lock back on close.
		bool shown = _lightboxItem is not null;
		if (shown == _lightboxShown)
		{
			return;
		}

		_lightboxShown = shown;
		if (shown)
		{
			await SafeModuleInvokeVoidAsync(GalleryModule, "openLightbox", _root, _lightbox);
		}
		else
		{
			await SafeModuleInvokeVoidAsync(GalleryModule, "closeLightbox", _root, _lightboxIndex);
		}
	}

	private async Task HandleItemClick(MokaMediaItem item, int index)
	{
		if (Lightbox)
		{
			_lightboxItem = item;
			_lightboxIndex = index;
		}

		if (OnItemClick.HasDelegate)
		{
			await OnItemClick.InvokeAsync(item);
		}
	}

	private void CloseLightbox() => _lightboxItem = null;

	private void LightboxPrev()
	{
		if (_lightboxIndex > 0)
		{
			_lightboxIndex--;
			_lightboxItem = Items[_lightboxIndex];
		}
	}

	private void LightboxNext()
	{
		if (_lightboxIndex < Items.Count - 1)
		{
			_lightboxIndex++;
			_lightboxItem = Items[_lightboxIndex];
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		// A gallery removed with its lightbox open never renders it closed, and the page would stay
		// locked. Nothing is sent when the script never heard of an open lightbox.
		if (_lightboxShown)
		{
			_lightboxShown = false;
			await SafeModuleInvokeVoidAsync(GalleryModule, "disposeLightbox", _root);
		}

		await base.DisposeAsyncCore();
	}

	private void HandleLightboxKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Escape")
		{
			CloseLightbox();
			return;
		}

		// Alt+Left and Alt+Right are the browser's back and forward, not a change of image.
		if (e.AltKey || e.CtrlKey || e.MetaKey)
		{
			return;
		}

		switch (e.Key)
		{
			case "ArrowLeft":
				LightboxPrev();
				break;
			case "ArrowRight":
				LightboxNext();
				break;
		}
	}
}
