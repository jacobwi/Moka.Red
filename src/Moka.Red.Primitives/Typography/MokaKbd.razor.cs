using Microsoft.AspNetCore.Components;

namespace Moka.Red.Primitives.Typography;

/// <summary>
///     Keyboard key display component with a raised key-cap style.
///     Renders a semantic &lt;kbd&gt; element for representing keyboard input.
/// </summary>
public partial class MokaKbd
{
	/// <summary>Content to render inside the keyboard key.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-kbd";
}
