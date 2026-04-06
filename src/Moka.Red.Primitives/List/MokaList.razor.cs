using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.List;

/// <summary>
///     Structured list for navigation, selection, or data display.
///     Wraps <see cref="MokaListItem" /> children.
/// </summary>
public partial class MokaList
{
	/// <summary>Child content containing <see cref="MokaListItem" /> elements.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Reduces vertical padding for compact layouts. Default true.</summary>
	[Parameter]
	public bool Dense { get; set; } = true;

	/// <summary>Adds a border around the list. Default false.</summary>
	[Parameter]
	public bool Bordered { get; set; }

	/// <summary>Enables hover highlight on items. Default true.</summary>
	[Parameter]
	public bool Hoverable { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-list";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-list--dense", Dense)
		.AddClass("moka-list--bordered", Bordered)
		.AddClass("moka-list--hoverable", Hoverable)
		.AddClass(Class)
		.Build();
}
