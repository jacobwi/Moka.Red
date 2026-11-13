using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Primitives.Fab;

/// <summary>
///     A Material-style floating action button fixed to a corner of the viewport.
///     Supports icon-only and extended (icon + label) modes with shadow and hover effects.
///     <see cref="MokaComponentBase.Class" />, <see cref="MokaComponentBase.Style" /> and the margin go on the element
///     that holds the button in its corner, so a style can move it; <see cref="MokaComponentBase.Id" />, the padding,
///     the rounding and other attributes go on the button. An icon-only button shows its <see cref="Label" /> in a
///     tooltip.
/// </summary>
public partial class MokaFloatingActionButton : MokaVisualComponentBase
{
	private bool _announcing;

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

	/// <summary>
	///     What the button does. Shown beside the icon when <see cref="Extended" /> is true; otherwise it names the
	///     button and shows in a tooltip, opening away from the corner the button sits in.
	/// </summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     Marks the action busy, as <c>MokaButton.Loading</c> does: a spinner in the icon's place, <c>aria-busy</c>,
	///     and <see cref="LoadingText" /> announced. The button stays focusable with <c>aria-disabled</c>, so a keyboard
	///     user keeps their place, and <see cref="OnClick" /> isn't raised until it's done.
	/// </summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>What a screen reader hears when the action becomes busy. Defaults to "Loading".</summary>
	[Parameter]
	public string LoadingText { get; set; } = "Loading";

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
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string ButtonClass => new CssBuilder("moka-fab__button")
		.AddClass($"moka-fab--{ColorToKebab(ResolvedColor)}")
		.AddClass($"moka-fab--{SizeToKebab(Size)}")
		.AddClass("moka-fab--extended", Extended)
		.AddClass("moka-fab--disabled", Disabled || Loading)
		.AddClass("moka-fab--loading", Loading)
		.Build();

	private string? ButtonStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private bool ShowsLabel => Extended && !string.IsNullOrEmpty(Label);

	private bool Busy => Loading && !Disabled;

	// Only an icon shows, so the label names it in a tooltip. A button without a label has nothing to show.
	private string? TooltipText => ShowsLabel ? null : Label;

	// Away from the corner: above a button at the bottom, below one at the top, lined up with the window's edge.
	private MokaTooltipPosition TooltipPosition => Position switch
	{
		MokaFabPosition.BottomLeft => MokaTooltipPosition.TopStart,
		MokaFabPosition.TopRight => MokaTooltipPosition.BottomEnd,
		MokaFabPosition.TopLeft => MokaTooltipPosition.BottomStart,
		_ => MokaTooltipPosition.TopEnd
	};

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		if (!Loading)
		{
			_announcing = false;
		}
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);

		// The status region is drawn empty first and filled on the next render, since a region that arrives with its
		// text already in it isn't announced by every screen reader.
		if (Loading && !_announcing)
		{
			_announcing = true;
			ForceRender();
		}
	}

	private async Task HandleClick(MouseEventArgs args)
	{
		if (!Disabled && !Loading)
		{
			await OnClick.InvokeAsync(args);
		}
	}
}
