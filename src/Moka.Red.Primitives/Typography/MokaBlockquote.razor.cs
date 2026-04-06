using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     Styled blockquote component with left accent border, optional citation, and subtle background.
///     Renders a semantic &lt;blockquote&gt; element with design token styling.
/// </summary>
public partial class MokaBlockquote
{
	/// <summary>Content to render inside the blockquote.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Attribution text rendered in a &lt;cite&gt; element.</summary>
	[Parameter]
	public string? Citation { get; set; }

	/// <summary>Optional link for the citation source.</summary>
	[Parameter]
	public string? CitationHref { get; set; }

	/// <summary>When true, shows the left border accent. Defaults to true.</summary>
	[Parameter]
	public bool Accent { get; set; } = true;

	/// <summary>When true, adds a subtle background fill. Defaults to false.</summary>
	[Parameter]
	public bool Filled { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-blockquote";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-blockquote--accent", Accent)
		.AddClass("moka-blockquote--filled", Filled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin ?? "var(--moka-spacing-md) 0")
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();
}
