using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Paper;

/// <summary>
///     The simplest surface component — a styled div with elevation and background.
///     Like a card without the structured slots.
/// </summary>
public partial class MokaPaper : MokaVisualComponentBase
{
	/// <summary>The child content to render inside the paper.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Box shadow depth. Range 0-4. Default 0.</summary>
	[Parameter]
	public int Elevation { get; set; }

	/// <summary>When true, renders a border instead of a box shadow.</summary>
	[Parameter]
	public bool Outlined { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-paper";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-paper--elevation-{ClampElevation}", !Outlined)
		.AddClass("moka-paper--outlined", Outlined)
		.AddClass("moka-paper--rounded", Rounded is not null && Rounded != MokaRounding.None)
		.AddClass("moka-paper--square", Rounded == MokaRounding.None)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private int ClampElevation => Math.Clamp(Elevation, 0, 4);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		Rounded ??= MokaRounding.Md;
	}
}
