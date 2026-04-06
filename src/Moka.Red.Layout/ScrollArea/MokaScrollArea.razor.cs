using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.ScrollArea;

/// <summary>
///     Styled scrollable container with custom scrollbar styling.
///     Supports horizontal/vertical scroll, thin scrollbar, and hidden scrollbar modes.
/// </summary>
public partial class MokaScrollArea : MokaVisualComponentBase
{
	/// <summary>The scrollable content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Max height of the scroll area. Null = auto.</summary>
	[Parameter]
	public string? Height { get; set; }

	/// <summary>Max width of the scroll area. Null = auto.</summary>
	[Parameter]
	public string? Width { get; set; }

	/// <summary>Enable horizontal scrolling. Defaults to false.</summary>
	[Parameter]
	public bool ScrollX { get; set; }

	/// <summary>Enable vertical scrolling. Defaults to true.</summary>
	[Parameter]
	public bool ScrollY { get; set; } = true;

	/// <summary>Hides scrollbar but keeps content scrollable. Defaults to false.</summary>
	[Parameter]
	public bool HideScrollbar { get; set; }

	/// <summary>Use thin (6px) scrollbar style. Defaults to true.</summary>
	[Parameter]
	public bool ThinScrollbar { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-scroll-area";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-hide-scrollbar", HideScrollbar)
		.AddClass("moka-thin-scrollbar", ThinScrollbar && !HideScrollbar)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("max-height", Height, !string.IsNullOrEmpty(Height))
		.AddStyle("max-width", Width, !string.IsNullOrEmpty(Width))
		.AddStyle("overflow-x", ScrollX ? "auto" : "hidden")
		.AddStyle("overflow-y", ScrollY ? "auto" : "hidden")
		.AddStyle(Style)
		.Build();
}
