using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Tooltip;

/// <summary>
///     A pure CSS tooltip that appears on hover or focus.
///     Zero JS interop, zero C# event handlers, zero allocations per hover.
///     The tooltip is always in the DOM but invisible; CSS handles show/hide.
/// </summary>
public partial class MokaTooltip : MokaComponentBase
{
	/// <summary>The trigger element that the tooltip wraps.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Tooltip text content. Ignored if <see cref="Content" /> is set.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Rich tooltip content. Overrides <see cref="Text" />.</summary>
	[Parameter]
	public RenderFragment? Content { get; set; }

	/// <summary>Position relative to the trigger. Defaults to <see cref="MokaTooltipPosition.Top" />.</summary>
	[Parameter]
	public MokaTooltipPosition Position { get; set; } = MokaTooltipPosition.Top;

	/// <summary>Delay in milliseconds before showing the tooltip. Defaults to 300.</summary>
	[Parameter]
	public int Delay { get; set; } = 300;

	/// <inheritdoc />
	protected override string RootClass => "moka-tooltip-trigger";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	private string PopupCss => new CssBuilder("moka-tooltip-popup")
		.AddClass($"moka-tooltip-popup--{MokaEnumHelpers.ToCssClass(Position)}")
		.Build();

	private string PopupStyle { get; set; } = "";

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		PopupStyle = $"transition-delay: {Delay}ms";
	}
}
