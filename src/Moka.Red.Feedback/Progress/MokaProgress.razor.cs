using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Progress;

/// <summary>
///     A progress indicator supporting linear and circular modes, determinate and indeterminate states.
///     Uses <c>role="progressbar"</c> with appropriate ARIA attributes.
/// </summary>
public partial class MokaProgress : MokaVisualComponentBase
{
	private const double CircleRadius = 18;
	private const double CircleCircumference = 2 * Math.PI * CircleRadius;

	/// <summary>
	///     Progress value from 0 to 100. Null indicates an indeterminate state.
	/// </summary>
	[Parameter]
	public double? Value { get; set; }

	/// <summary>Progress indicator type. Defaults to <see cref="MokaProgressType.Linear" />.</summary>
	[Parameter]
	public MokaProgressType ProgressType { get; set; } = MokaProgressType.Linear;

	/// <summary>Whether to show the numeric percentage value. Defaults to false.</summary>
	[Parameter]
	public bool ShowValue { get; set; }

	/// <summary>Whether to show animated stripes on the linear bar. Defaults to false.</summary>
	[Parameter]
	public bool Striped { get; set; }

	/// <summary>Whether to use rounded bar ends on the linear progress bar. Defaults to true.</summary>
	[Parameter]
	public bool RoundedEnds { get; set; } = true;

	/// <summary>A 6px round bar for inside a card or a list row. Linear only.</summary>
	[Parameter]
	public bool Thin { get; set; }

	/// <summary>
	///     Several colored parts side by side, each a percentage of the whole bar. They win over <see cref="Value" />,
	///     and the bar reports their sum. An empty list is an empty bar, not an indeterminate one. Linear only.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaProgressSegment>? Segments { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-progress";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width", ProgressType == MokaProgressType.Linear)
		.AddClass(ProgressType == MokaProgressType.Linear ? "moka-progress--linear" : "moka-progress--circular")
		.AddClass("moka-progress--indeterminate", IsIndeterminate)
		.AddClass("moka-progress--striped", Striped && ProgressType == MokaProgressType.Linear)
		.AddClass("moka-progress--rounded", RoundedEnds && ProgressType == MokaProgressType.Linear)
		.AddClass("moka-progress--thin", Thin && ProgressType == MokaProgressType.Linear)
		.AddClass("moka-progress--segmented", HasSegments)
		.AddClass(Class)
		.Build();

	// A linear bar's root only lines up the track and the value, so a radius there would not show.
	// It shapes the track instead, and the bar follows the track's corners.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding, ProgressType != MokaProgressType.Linear)
		.AddStyle(Style)
		.Build();

	private string? TrackStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private bool HasSegments => Segments is not null && ProgressType == MokaProgressType.Linear;

	private bool IsIndeterminate => !HasSegments && !Value.HasValue;

	private string? ValueText => HasSegments
		? Math.Clamp(Segments!.Sum(SegmentValue), 0, 100).ToString("F0", CultureInfo.InvariantCulture)
		: Value?.ToString("F0", CultureInfo.CurrentCulture);

	private double ClampedValue => Value.HasValue ? Math.Clamp(Value.Value, 0, 100) : 0;

	private string? BarWidth => new StyleBuilder()
		.AddStyle("width", ClampedValue.ToString("0.###", CultureInfo.InvariantCulture) + "%")
		.Build();

	private double StrokeDashoffset =>
		CircleCircumference - ClampedValue / 100.0 * CircleCircumference;

	private static double SegmentValue(MokaProgressSegment segment) =>
		double.IsNaN(segment.Value) ? 0 : Math.Clamp(segment.Value, 0, 100);

	private static string? SegmentStyle(MokaProgressSegment segment, double width) => new StyleBuilder()
		.AddStyle("width", width.ToString("0.###", CultureInfo.InvariantCulture) + "%")
		.AddStyle("background", $"var(--moka-color-{ColorToKebab(segment.Color)})")
		.Build();
}
