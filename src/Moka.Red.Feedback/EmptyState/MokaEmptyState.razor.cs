using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.EmptyState;

/// <summary>
///     A visual placeholder for empty lists, search results, or error states.
///     Displays an icon or image, title, description, and optional action buttons.
/// </summary>
public partial class MokaEmptyState
{
	/// <summary>Large icon displayed in the empty state.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Title text, displayed in semibold.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Description text, displayed in caption size.</summary>
	[Parameter]
	public string? Description { get; set; }

	/// <summary>Action buttons or other content below the description.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Custom image URL instead of an icon.</summary>
	[Parameter]
	public string? ImageSrc { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-empty-state";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();
}
