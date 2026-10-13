using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Tooltip;

/// <summary>
///     A pure CSS tooltip that appears on hover or on keyboard focus.
///     Zero JS interop, zero C# event handlers, zero allocations per hover.
///     The tooltip is always in the DOM but takes no layout until it shows; CSS handles show/hide.
///     Where the browser supports anchor positioning the popup is placed against the window rather than
///     inside its trigger, so a container that clips its overflow cannot cut it off, and it flips to the
///     side with room at the window's edge.
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

	/// <summary>Delay in milliseconds before showing the tooltip. Defaults to 300. Hiding never waits.</summary>
	[Parameter]
	public int Delay { get; set; } = 300;

	/// <summary>
	///     The widest the popup grows, as a CSS length such as <c>20rem</c>, before longer text wraps onto more lines.
	///     Never wider than the window, and a line break in <see cref="Text" /> starts a new line. Null, the default,
	///     keeps the text on one line, which suits a short name; set it for a sentence or a value of unknown length.
	/// </summary>
	[Parameter]
	public string? MaxWidth { get; set; }

	// Each trigger names itself as the anchor its popup is placed against. The name is per tooltip: with one
	// shared name every popup would be placed against whichever trigger carried it last.
	private readonly string _anchor = $"--moka-tooltip-{Guid.NewGuid():N}";

	/// <inheritdoc />
	protected override string RootClass => "moka-tooltip-trigger";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("anchor-name", _anchor)
		.AddStyle(Style)
		.Build();

	private string PopupCss => new CssBuilder("moka-tooltip-popup")
		.AddClass($"moka-tooltip-popup--{MokaEnumHelpers.ToCssClass(Position)}")
		.AddClass("moka-tooltip-popup--wrap", !string.IsNullOrWhiteSpace(MaxWidth))
		.Build();

	// The delay is a variable the shown state reads, so only showing waits for it.
	private string? PopupStyle => new StyleBuilder()
		.AddStyle("position-anchor", _anchor)
		.AddStyle("--moka-tooltip-delay", $"{Delay}ms")
		.AddStyle("--moka-tooltip-max-width", MaxWidth, !string.IsNullOrWhiteSpace(MaxWidth))
		.Build();
}
