using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Marquee;

/// <summary>
///     A scrolling text/content ticker for announcements, news tickers, and notification bars.
///     Content is duplicated for seamless looping via CSS animation.
/// </summary>
public partial class MokaMarquee : MokaComponentBase
{
	// Nominal travel of one loop. CSS cannot divide a length by a length, so the duration is
	// computed here instead: a loop moves roughly one screen of content, and screens are wider
	// than they are tall.
	private const int HorizontalLoopDistancePx = 1200;
	private const int VerticalLoopDistancePx = 800;

	/// <summary>The content to scroll.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Scroll speed in pixels per second. Defaults to 30. The loop duration is derived from
	///     it as <c>1200 / Speed</c> seconds horizontally and <c>800 / Speed</c> vertically, so a
	///     higher value scrolls faster. Values below 1 are treated as 1.
	/// </summary>
	[Parameter]
	public int Speed { get; set; } = 30;

	/// <summary>Scroll direction. Defaults to <see cref="MokaMarqueeDirection.Left" />.</summary>
	[Parameter]
	public MokaMarqueeDirection Direction { get; set; } = MokaMarqueeDirection.Left;

	/// <summary>Whether to pause scrolling when the user hovers over the marquee. Defaults to true.</summary>
	[Parameter]
	public bool PauseOnHover { get; set; } = true;

	/// <summary>Gap between repeated content. Defaults to <see cref="MokaSpacingScale.Xl" />.</summary>
	[Parameter]
	public MokaSpacingScale? Gap { get; set; } = MokaSpacingScale.Xl;

	/// <inheritdoc />
	protected override string RootClass => "moka-marquee";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-marquee--{DirectionToKebab(Direction)}")
		.AddClass("moka-marquee--pause-on-hover", PauseOnHover)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-marquee-duration", LoopDuration)
		.AddStyle("--moka-marquee-gap", Gap.HasValue ? MokaEnumHelpers.ToCssValue(Gap.Value) : null)
		.AddStyle(Style)
		.Build();

	private bool IsVertical => Direction is MokaMarqueeDirection.Up or MokaMarqueeDirection.Down;

	private string LoopDuration
	{
		get
		{
			double distance = IsVertical ? VerticalLoopDistancePx : HorizontalLoopDistancePx;
			double seconds = distance / Math.Max(1, Speed);
			return $"{seconds.ToString("F2", CultureInfo.InvariantCulture)}s";
		}
	}

	private static string DirectionToKebab(MokaMarqueeDirection direction) => direction switch
	{
		MokaMarqueeDirection.Left => "left",
		MokaMarqueeDirection.Right => "right",
		MokaMarqueeDirection.Up => "up",
		MokaMarqueeDirection.Down => "down",
		_ => "left"
	};
}
