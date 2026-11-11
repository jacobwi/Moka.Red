namespace Moka.Red.Data.Chart;

/// <summary>
///     A place along a chart's x axis: a number, or a point in time held as Unix milliseconds. Converts from
///     <see cref="double" />, <see cref="int" />, <see cref="DateTimeOffset" /> and <see cref="DateTime" />.
/// </summary>
/// <param name="Value">The number, or the time as Unix milliseconds.</param>
/// <param name="IsTime">Whether <paramref name="Value" /> is a time.</param>
public readonly record struct MokaChartX(double Value, bool IsTime)
{
	/// <summary>The time this place stands for, reading <see cref="Value" /> as Unix milliseconds.</summary>
	public DateTimeOffset Time
	{
		get
		{
			if (!double.IsFinite(Value))
			{
				return DateTimeOffset.UnixEpoch;
			}

			double lowest = (DateTimeOffset.MinValue - DateTimeOffset.UnixEpoch).TotalMilliseconds;
			double highest = (DateTimeOffset.MaxValue - DateTimeOffset.UnixEpoch).TotalMilliseconds;
			return DateTimeOffset.UnixEpoch.AddMilliseconds(Math.Clamp(Value, lowest, highest));
		}
	}

	/// <summary>A number along the axis.</summary>
	/// <param name="value">The number.</param>
	public static implicit operator MokaChartX(double value) => FromDouble(value);

	/// <summary>A number along the axis.</summary>
	/// <param name="value">The number.</param>
	public static implicit operator MokaChartX(int value) => FromInt32(value);

	/// <summary>A point in time.</summary>
	/// <param name="value">The time.</param>
	public static implicit operator MokaChartX(DateTimeOffset value) => FromDateTimeOffset(value);

	/// <summary>A point in time. A time with no kind is read as local, as <see cref="DateTimeOffset" /> does.</summary>
	/// <param name="value">The time.</param>
	public static implicit operator MokaChartX(DateTime value) => FromDateTime(value);

	/// <summary>A number along the axis.</summary>
	/// <param name="value">The number.</param>
	/// <returns>The place.</returns>
	public static MokaChartX FromDouble(double value) => new(value, false);

	/// <summary>A number along the axis.</summary>
	/// <param name="value">The number.</param>
	/// <returns>The place.</returns>
	public static MokaChartX FromInt32(int value) => new(value, false);

	/// <summary>A point in time.</summary>
	/// <param name="value">The time.</param>
	/// <returns>The place.</returns>
	public static MokaChartX FromDateTimeOffset(DateTimeOffset value) =>
		new((value - DateTimeOffset.UnixEpoch).TotalMilliseconds, true);

	/// <summary>A point in time. A time with no kind is read as local, as <see cref="DateTimeOffset" /> does.</summary>
	/// <param name="value">The time.</param>
	/// <returns>The place.</returns>
	public static MokaChartX FromDateTime(DateTime value) => FromDateTimeOffset(new DateTimeOffset(value));
}

/// <summary>A span along a chart's x axis, such as the part it is zoomed into.</summary>
/// <param name="From">Where the span starts.</param>
/// <param name="To">Where the span ends.</param>
public readonly record struct MokaChartRange(MokaChartX From, MokaChartX To);

/// <summary>A time range a chart offers as a preset, such as the last 24 hours.</summary>
/// <param name="Label">What the preset reads, such as <c>24 h</c>.</param>
/// <param name="Span">How far back from the end of the data it reaches.</param>
public sealed record MokaChartRangePreset(string Label, TimeSpan Span);
