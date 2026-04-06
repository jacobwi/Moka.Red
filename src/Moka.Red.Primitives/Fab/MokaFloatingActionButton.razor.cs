using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Fab;

/// <summary>
///     A Material-style floating action button fixed to a corner of the viewport.
///     Supports icon-only and extended (icon + label) modes with shadow and hover effects.
/// </summary>
public partial class MokaFloatingActionButton : MokaVisualComponentBase
{
	/// <inheritdoc />
	/// <remarks>FABs default to <see cref="MokaSize.Lg" /> for prominent visibility.</remarks>
	public override MokaSize Size { get; set; } = MokaSize.Lg;

	/// <summary>The icon displayed in the button. Required.</summary>
	[Parameter]
	[EditorRequired]
	public MokaIconDefinition Icon { get; set; }

	/// <summary>Click event callback.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>
	///     Fixed viewport position. Defaults to <see cref="MokaFabPosition.BottomRight" />.
	/// </summary>
	[Parameter]
	public MokaFabPosition Position { get; set; } = MokaFabPosition.BottomRight;

	/// <summary>
	///     When true, displays the <see cref="Label" /> text beside the icon.
	/// </summary>
	[Parameter]
	public bool Extended { get; set; }

	/// <summary>Text label displayed when <see cref="Extended" /> is true.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-fab";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	private string PositionClass => Position switch
	{
		MokaFabPosition.BottomRight => "moka-fab--bottom-right",
		MokaFabPosition.BottomLeft => "moka-fab--bottom-left",
		MokaFabPosition.TopRight => "moka-fab--top-right",
		MokaFabPosition.TopLeft => "moka-fab--top-left",
		_ => "moka-fab--bottom-right"
	};

	/// <summary>Maps FAB size to icon size (slightly smaller).</summary>
	private MokaSize IconSize => Size switch
	{
		MokaSize.Xs => MokaSize.Xs,
		MokaSize.Sm => MokaSize.Sm,
		MokaSize.Md => MokaSize.Sm,
		MokaSize.Lg => MokaSize.Md,
		_ => MokaSize.Sm
	};

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(PositionClass)
		.AddClass($"moka-fab--{ColorToKebab(ResolvedColor)}")
		.AddClass($"moka-fab--{SizeToKebab(Size)}")
		.AddClass("moka-fab--extended", Extended)
		.AddClass("moka-fab--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private async Task HandleClick(MouseEventArgs args)
	{
		if (!Disabled)
		{
			await OnClick.InvokeAsync(args);
		}
	}
}
