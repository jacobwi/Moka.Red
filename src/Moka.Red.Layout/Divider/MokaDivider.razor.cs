using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Divider;

/// <summary>
///     A simple horizontal or vertical divider line. Optionally displays a text label
///     or custom content in the center of the rule.
/// </summary>
public partial class MokaDivider : MokaVisualComponentBase
{
	/// <summary>Whether the divider is vertical. Default is horizontal.</summary>
	[Parameter]
	public bool Vertical { get; set; }

	/// <summary>Optional text label in the middle of the divider.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Content in the middle of the divider (overrides <see cref="Label" />).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-divider";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-divider--vertical", Vertical)
		.AddClass("moka-divider--with-content", HasContent)
		.AddClass(Class)
		.Build();

	private bool HasContent => Label is not null || ChildContent is not null;
}
