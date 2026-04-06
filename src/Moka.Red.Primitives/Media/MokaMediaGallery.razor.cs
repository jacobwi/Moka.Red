using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Media;

/// <summary>
///     Grid of media thumbnails with hover overlay. Emits click events for lightbox integration.
/// </summary>
public partial class MokaMediaGallery : MokaVisualComponentBase
{
	private int _lightboxIndex;

	private MokaMediaItem? _lightboxItem;

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
	///     When false, only fires OnItemClick — you handle the preview yourself.
	/// </summary>
	[Parameter]
	public bool Lightbox { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-media-gallery";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("grid-template-columns", $"repeat({Columns}, 1fr)")
		.AddStyle("gap", MokaEnumHelpers.ToCssValue(Gap))
		.AddStyle(Style)
		.Build();

	private static string GetThumbnailSrc(MokaMediaItem item) =>
		item.ThumbnailSrc ?? item.Src;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private async Task HandleItemClick(MokaMediaItem item)
	{
		if (Lightbox)
		{
			_lightboxItem = item;
			_lightboxIndex = IndexOf(Items, item);
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

	private void HandleLightboxKeyDown(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "Escape":
				CloseLightbox();
				break;
			case "ArrowLeft":
				LightboxPrev();
				break;
			case "ArrowRight":
				LightboxNext();
				break;
		}
	}

	private static int IndexOf(IReadOnlyList<MokaMediaItem> items, MokaMediaItem item)
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (ReferenceEquals(items[i], item))
			{
				return i;
			}
		}

		return -1;
	}
}
