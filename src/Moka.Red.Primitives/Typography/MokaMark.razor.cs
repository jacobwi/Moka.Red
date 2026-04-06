using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     Highlighted/marked text component with theme-aware colors.
///     Renders a semantic &lt;mark&gt; element with subtle tinted backgrounds.
/// </summary>
public partial class MokaMark
{
	/// <summary>Content to render inside the mark element.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-mark";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Color.HasValue ? $"moka-mark--{ColorToKebab(Color.Value)}" : null)
		.AddClass(Class)
		.Build();
}
