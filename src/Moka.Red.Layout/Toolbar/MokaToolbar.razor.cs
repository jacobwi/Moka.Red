using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Toolbar;

/// <summary>
///     A horizontal bar of actions and controls — suitable for rich text editor toolbars,
///     action bars, or any row of grouped controls. Flows in-document (not fixed).
/// </summary>
public partial class MokaToolbar : MokaVisualComponentBase
{
	/// <summary>Toolbar content (buttons, selects, dividers, custom controls).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether to use compact height (32px). Default true.</summary>
	[Parameter]
	public bool Dense { get; set; } = true;

	/// <summary>Whether to show a border. Default true.</summary>
	[Parameter]
	public bool Bordered { get; set; } = true;

	/// <summary>Whether to show elevation shadow. Default false.</summary>
	[Parameter]
	public bool Elevated { get; set; }

	/// <summary>Whether to allow items to wrap to the next line. Default false.</summary>
	[Parameter]
	public bool Wrap { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-toolbar";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-toolbar--dense", Dense)
		.AddClass("moka-toolbar--bordered", Bordered)
		.AddClass("moka-toolbar--elevated", Elevated)
		.AddClass("moka-toolbar--rounded", Rounded is not null && Rounded != MokaRounding.None)
		.AddClass("moka-toolbar--wrap", Wrap)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		Rounded ??= MokaRounding.Md;
	}
}
