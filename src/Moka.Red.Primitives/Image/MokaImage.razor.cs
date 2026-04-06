using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Image;

/// <summary>
///     Enhanced image component with loading state, fallback, and aspect ratio control.
/// </summary>
public partial class MokaImage
{
	private bool _hasError;
	private bool _isLoaded;

	/// <summary>Image source URL. Required.</summary>
	[Parameter]
	[EditorRequired]
	public string Src { get; set; } = default!;

	/// <summary>Alt text for the image.</summary>
	[Parameter]
	public string? Alt { get; set; }

	/// <summary>Fallback image URL on error.</summary>
	[Parameter]
	public string? Fallback { get; set; }

	/// <summary>Custom fallback content to display on error.</summary>
	[Parameter]
	public RenderFragment? FallbackContent { get; set; }

	/// <summary>CSS aspect-ratio value (e.g., "16/9", "1/1").</summary>
	[Parameter]
	public string? AspectRatio { get; set; }

	/// <summary>How the image fills its container. Default Cover.</summary>
	[Parameter]
	public MokaObjectFit ObjectFit { get; set; } = MokaObjectFit.Cover;

	/// <summary>Shows skeleton while loading.</summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-image";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-image--rounded",
			Rounded is not null && Rounded != MokaRounding.None && Rounded != MokaRounding.Full)
		.AddClass("moka-image--circle", Rounded == MokaRounding.Full)
		.AddClass("moka-image--loading", Loading && !_isLoaded && !_hasError)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("aspect-ratio", AspectRatio)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string ObjectFitCss => ObjectFit switch
	{
		MokaObjectFit.Cover => "cover",
		MokaObjectFit.Contain => "contain",
		MokaObjectFit.Fill => "fill",
		MokaObjectFit.None => "none",
		_ => "cover"
	};

	private string ResolvedSrc => _hasError && !string.IsNullOrEmpty(Fallback) ? Fallback : Src;

	private void HandleLoad()
	{
		_isLoaded = true;
		ForceRender();
	}

	private void HandleError()
	{
		_hasError = true;
		ForceRender();
	}
}
