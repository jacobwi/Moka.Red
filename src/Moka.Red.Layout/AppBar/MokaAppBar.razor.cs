using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.AppBar;

/// <summary>
///     Top navigation bar with title, actions, and optional navigation content.
///     Supports elevation, border, fixed positioning, and dense mode.
/// </summary>
public partial class MokaAppBar : MokaVisualComponentBase
{
	/// <summary>Custom content in the center area.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Title text displayed in the center. Ignored if <see cref="TitleContent" /> is set.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Rich title content. Overrides <see cref="Title" />.</summary>
	[Parameter]
	public RenderFragment? TitleContent { get; set; }

	/// <summary>Left side content (menu button, logo).</summary>
	[Parameter]
	public RenderFragment? StartContent { get; set; }

	/// <summary>Right side content (actions, avatar).</summary>
	[Parameter]
	public RenderFragment? EndContent { get; set; }

	/// <summary>Whether to show elevation shadow. Defaults to true.</summary>
	[Parameter]
	public bool Elevated { get; set; } = true;

	/// <summary>Whether to show a bottom border. Defaults to false.</summary>
	[Parameter]
	public bool Bordered { get; set; }

	/// <summary>Whether to use sticky positioning at the top. Defaults to false.</summary>
	[Parameter]
	public bool Fixed { get; set; }

	/// <summary>Whether to use dense (compact) height. Defaults to true.</summary>
	[Parameter]
	public bool Dense { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-appbar";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-appbar--elevated", Elevated)
		.AddClass("moka-appbar--bordered", Bordered)
		.AddClass("moka-appbar--fixed", Fixed)
		.AddClass("moka-appbar--dense", Dense)
		.AddClass(Class)
		.Build();
}
