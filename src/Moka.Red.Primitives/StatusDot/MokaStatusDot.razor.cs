using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.StatusDot;

/// <summary>
///     The signature Moka glow-dot status indicator: a small colored dot with an optional
///     label, e.g. "ONLINE" or "saving...". Supports a pulse animation for in-progress
///     states and a soft currentColor glow ring.
///     Distinct from <c>MokaBadge</c>'s Dot mode, which overlays a positioned dot on top of
///     other content; MokaStatusDot is a standalone inline status element.
/// </summary>
public partial class MokaStatusDot : MokaVisualComponentBase
{
	/// <summary>Optional text rendered next to the dot (e.g. "ONLINE", "saving...").</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether the dot pulses (scale/opacity, 1.4s ease-in-out infinite). Defaults to false.</summary>
	[Parameter]
	public bool Pulse { get; set; }

	/// <summary>Whether the dot casts a soft currentColor glow. Defaults to true.</summary>
	[Parameter]
	public bool Glow { get; set; } = true;

	/// <summary>
	///     Whether the label renders in the uppercase micro-label style
	///     (extra-small, semibold, wide letter-spacing). Defaults to true.
	///     Set to false to render the label as-is (e.g. "saving...").
	/// </summary>
	[Parameter]
	public bool Uppercase { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-status-dot";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-status-dot--{SizeToKebab(Size)}")
		.AddClass($"moka-status-dot--{ColorToKebab(ResolvedColor)}")
		.AddClass("moka-status-dot--pulse", Pulse)
		.AddClass("moka-status-dot--glow", Glow)
		.AddClass("moka-status-dot--uppercase", Uppercase)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-status-dot-size", SizeValue)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private MokaColor ResolvedColor => Color ?? MokaColor.Success;

	private bool HasLabel => !string.IsNullOrEmpty(Label);
}
