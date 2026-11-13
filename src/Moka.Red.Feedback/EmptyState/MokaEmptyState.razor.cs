using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
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

	/// <summary>A row for inside a card's list: a small icon tile, the text left-aligned, the actions at the right.</summary>
	[Parameter]
	public bool Compact { get; set; }

	/// <summary>A dashed outline, for an empty area on its own.</summary>
	[Parameter]
	public bool Framed { get; set; }

	/// <summary>Has screen readers announce it politely when it appears, for a search that found nothing.</summary>
	[Parameter]
	public bool Live { get; set; }

	/// <summary>
	///     How the title and description break over lines. <see cref="MokaTextWrap.Pretty" /> keeps a description of two
	///     or three lines from ending on one word; <see cref="MokaTextWrap.Balance" /> evens out the lines of short text.
	///     Defaults to <see cref="MokaTextWrap.Normal" />, the browser's own breaks.
	/// </summary>
	[Parameter]
	public MokaTextWrap TextWrap { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-empty-state";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-empty-state--compact", Compact)
		.AddClass("moka-empty-state--framed", Framed)
		.AddClass("moka-empty-state--wrap-pretty", TextWrap == MokaTextWrap.Pretty)
		.AddClass("moka-empty-state--wrap-balance", TextWrap == MokaTextWrap.Balance)
		.AddClass(Class)
		.Build();
}
