using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     Code display component for inline or block code snippets.
///     Renders &lt;code&gt; for inline or &lt;pre&gt;&lt;code&gt; for block mode.
/// </summary>
public partial class MokaCode
{
	/// <summary>Content to render inside the code element.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>When true, renders as a block-level &lt;pre&gt;&lt;code&gt; instead of inline &lt;code&gt;.</summary>
	[Parameter]
	public bool Block { get; set; }

	/// <summary>Optional language hint for future syntax highlighting support.</summary>
	[Parameter]
	public string? Language { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-code";

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("color", Color.HasValue ? $"var(--moka-color-{ColorToKebab(Color.Value)})" : null)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-code--{Language}", !string.IsNullOrWhiteSpace(Language))
		.AddClass(Class)
		.Build();
}
