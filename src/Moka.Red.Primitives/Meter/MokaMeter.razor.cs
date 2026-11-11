using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Primitives.Meter;

/// <summary>
///     A horizontal gauge/meter visualization showing a value within a range.
///     Supports colored segments for zone indicators (e.g., green/yellow/red).
/// </summary>
/// <remarks>
///     <see cref="Target" /> turns it into a bullet chart, <see cref="Layout" /> lines a list of them up as rows, and
///     <see cref="WarnAt" /> and <see cref="CriticalAt" /> say in a word and an icon when it runs near full. With none of
///     those set it draws what it always has. It is built of spans, as the native <c>meter</c> element is phrasing
///     content, so it can sit inside a button, a link or a label.
/// </remarks>
public partial class MokaMeter : MokaVisualComponentBase
{
	private const int BandCount = 3;

	/// <summary>The current meter value.</summary>
	[Parameter]
	public double Value { get; set; }

	/// <summary>Minimum value of the meter range. Defaults to 0.</summary>
	[Parameter]
	public double Min { get; set; }

	/// <summary>Maximum value of the meter range. Defaults to 100.</summary>
	[Parameter]
	public double Max { get; set; } = 100;

	/// <summary>Optional label displayed above the meter.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Whether to display the current value. Defaults to true.</summary>
	[Parameter]
	public bool ShowValue { get; set; } = true;

	/// <summary>Numeric format string for the displayed value. Defaults to "F0".</summary>
	[Parameter]
	public string Format { get; set; } = "F0";

	/// <summary>
	///     Optional colored segments for zone indicators. A segment whose color is not a CSS color is
	///     drawn transparent. With a <see cref="Target" /> they are the bullet's gray ranges instead, and their colors
	///     are ignored.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaMeterSegment>? Segments { get; set; }

	/// <summary>
	///     A goal or a limit, in the meter's range. Draws a bullet chart: the value as a thin bar over gray ranges (the
	///     <see cref="Segments" />, else one range), with a tick at the target.
	/// </summary>
	[Parameter]
	public double? Target { get; set; }

	/// <summary>What a screen reader hears before the target's value. Defaults to "target".</summary>
	[Parameter]
	public string TargetText { get; set; } = "target";

	/// <summary>
	///     How the name, the bar and the figure are laid out. <see cref="MokaMeterLayout.Row" /> puts them on one line;
	///     rows inside an element with the <c>moka-meter-rows</c> class line up.
	/// </summary>
	[Parameter]
	public MokaMeterLayout Layout { get; set; }

	/// <summary>
	///     A share of the range, 0 to 1, from which the fill turns the warning color and the name gains an icon and
	///     <see cref="WarnText" />, such as 0.8 for a volume nearly full.
	/// </summary>
	[Parameter]
	public double? WarnAt { get; set; }

	/// <summary>A share of the range, 0 to 1, from which the fill turns the error color and says <see cref="CriticalText" />.</summary>
	[Parameter]
	public double? CriticalAt { get; set; }

	/// <summary>The word beside the name past <see cref="WarnAt" />. Defaults to "Nearly full".</summary>
	[Parameter]
	public string WarnText { get; set; } = "Nearly full";

	/// <summary>The word beside the name past <see cref="CriticalAt" />. Defaults to "Full".</summary>
	[Parameter]
	public string CriticalText { get; set; } = "Full";

	/// <summary>The figure shown in place of the formatted value, such as "412 of 500 GB".</summary>
	[Parameter]
	public string? Figure { get; set; }

	/// <summary>
	///     A CSS color or token to fill the bar with, such as a zone's own <c>var(--zone)</c>. Wins over <c>Color</c>;
	///     anything that isn't a color is ignored. Past <see cref="WarnAt" /> and <see cref="CriticalAt" /> the fill still
	///     turns amber and red, and <see cref="Segments" /> keep their own colors.
	/// </summary>
	[Parameter]
	public string? ColorValue { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-meter";

	private MokaColor ResolvedColor => Color ?? MokaColor.Primary;

	private string? OwnColor => CssValues.IsColor(ColorValue) ? ColorValue.Trim() : null;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass($"moka-meter--{SizeToKebab(Size)}")
		.AddClass(OwnColor is null ? $"moka-meter--{ColorToKebab(ResolvedColor)}" : "moka-meter--own")
		.AddClass("moka-meter--row", Layout == MokaMeterLayout.Row)
		.AddClass("moka-meter--bullet", Target.HasValue)
		.AddClass("moka-meter--warn", State == MeterState.Warn)
		.AddClass("moka-meter--critical", State == MeterState.Critical)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("--_meter-color", OwnColor)
		.AddStyle(Style)
		.Build();

	private enum MeterState
	{
		None,
		Warn,
		Critical
	}

	private sealed record Band(string Class, string? Style);

	// The root only stacks the header over the track, so a radius there would not show. The track
	// is the box the meter draws.
	private string? TrackStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string TrackClass => new CssBuilder("moka-meter__track")
		.AddClass("moka-meter__track--bullet", Target.HasValue)
		.Build();

	/// <summary>Gets the fill percentage clamped between 0 and 100.</summary>
	private double FillPercent => Percent(Value);

	/// <summary>Gets the formatted value string.</summary>
	private string FormattedValue => Value.ToString(Format, CultureInfo.CurrentCulture);

	private string ShownValue => Figure ?? FormattedValue;

	private double Share => Max <= Min ? 0 : (Value - Min) / (Max - Min);

	private MeterState State => CriticalAt is { } critical && Share >= critical
		? MeterState.Critical
		: WarnAt is { } warn && Share >= warn
			? MeterState.Warn
			: MeterState.None;

	private string? StateWord => State switch
	{
		MeterState.Critical => CriticalText,
		MeterState.Warn => WarnText,
		_ => null
	};

	private MokaIconDefinition? StateIcon => State switch
	{
		MeterState.Critical => MokaIcons.Status.Error,
		MeterState.Warn => MokaIcons.Status.Warning,
		_ => default(MokaIconDefinition?)
	};

	// Read only when something beyond the number needs saying, so a plain meter keeps its markup.
	private string? ValueText
	{
		get
		{
			if (Figure is null && StateWord is null && Target is null)
			{
				return null;
			}

			List<string> parts = [ShownValue];
			if (Target is { } target)
			{
				parts.Add($"{TargetText} {target.ToString(Format, CultureInfo.CurrentCulture)}");
			}

			if (StateWord is { } word)
			{
				parts.Add(word);
			}

			return string.Join(", ", parts);
		}
	}

	private bool HasSegments => Segments is { Count: > 0 };

	private string? SegmentStyle => new StyleBuilder()
		.AddStyle("background", SegmentGradient)
		.Build();

	// Clipped rather than narrowed, so each zone stays at its place along the track.
	private string? FilledSegmentStyle => new StyleBuilder()
		.AddStyle("background", SegmentGradient)
		.AddStyle("clip-path",
			$"inset(0 {(100 - FillPercent).ToString(CultureInfo.InvariantCulture)}% 0 0 round {ResolvedRounding ?? "var(--moka-radius-full)"})")
		.Build();

	/// <summary>Builds the CSS gradient for segment backgrounds.</summary>
	private string? SegmentGradient
	{
		get
		{
			if (!HasSegments)
			{
				return null;
			}

			var stops = new List<string>();
			foreach (MokaMeterSegment seg in Segments!)
			{
				// Checked, because a parenthesis in the color would end the gradient and add layers of its own.
				string color = CssValues.ColorOrDefault(seg.Color, "transparent");
				stops.Add($"{color} {seg.FromPercent.ToString(CultureInfo.InvariantCulture)}%");
				stops.Add($"{color} {seg.ToPercent.ToString(CultureInfo.InvariantCulture)}%");
			}

			return $"linear-gradient(to right, {string.Join(", ", stops)})";
		}
	}

	// The bullet's ranges, darker each step: the segments in order, or one range over the whole track.
	private IReadOnlyList<Band> Bands
	{
		get
		{
			if (!HasSegments)
			{
				return [new Band("moka-meter__band moka-meter__band--1", null)];
			}

			List<Band> bands = [];
			for (int i = 0; i < Segments!.Count; i++)
			{
				MokaMeterSegment segment = Segments[i];
				double from = Math.Clamp(Math.Min(segment.FromPercent, segment.ToPercent), 0, 100);
				double to = Math.Clamp(Math.Max(segment.FromPercent, segment.ToPercent), 0, 100);
				string level = ((i % BandCount) + 1).ToString(CultureInfo.InvariantCulture);
				bands.Add(new Band($"moka-meter__band moka-meter__band--{level}", new StyleBuilder()
					.AddStyle("left", $"{Num(from)}%")
					.AddStyle("width", $"{Num(to - from)}%")
					.Build()));
			}

			return bands;
		}
	}

	private string? BulletFillStyle => new StyleBuilder().AddStyle("width", $"{Num(FillPercent)}%").Build();

	private string? TargetStyle => new StyleBuilder().AddStyle("left", $"{Num(Percent(Target ?? Min))}%").Build();

	private double Percent(double value) =>
		Max <= Min ? 0 : Math.Clamp((value - Min) / (Max - Min) * 100, 0, 100);

	private static string Num(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
