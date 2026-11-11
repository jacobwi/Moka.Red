using System.Globalization;

namespace Moka.Red.Data.Chart;

/// <summary>What the charts of shares of a whole (donut, treemap, sankey) share: shares as text, squares by share, tooltips.</summary>
internal static class ChartWholes
{
	/// <summary>A share as people read it: a whole percent, in the reader's culture.</summary>
	/// <param name="share">The share, 0 to 1.</param>
	public static string Percent(double share) =>
		((int)Math.Round(share * 100, MidpointRounding.AwayFromZero)).ToString(CultureInfo.CurrentCulture) + "%";

	/// <summary>A number for markup: two decimals, invariant.</summary>
	/// <param name="value">The number.</param>
	public static string Num(double value) => value.ToString("F2", CultureInfo.InvariantCulture);

	/// <summary>A share as a CSS percentage, invariant, such as <c>12.345%</c>.</summary>
	/// <param name="share">The share, 0 to 1.</param>
	public static string Css(double share) => (share * 100).ToString("0.###", CultureInfo.InvariantCulture) + "%";

	/// <summary>
	///     Splits <paramref name="cells" /> squares between values by the largest remainder, so whole squares add up
	///     exactly: each value takes the whole squares its share gives, and what is left goes one at a time to the
	///     largest fractions. Values that are a share of more than their sum fill only their part of the squares.
	/// </summary>
	/// <param name="values">The values, none negative.</param>
	/// <param name="whole">What they are shares of; at least their sum.</param>
	/// <param name="cells">How many squares in all.</param>
	/// <returns>The squares each value takes, in order.</returns>
	public static int[] LargestRemainder(IReadOnlyList<double> values, double whole, int cells)
	{
		ArgumentNullException.ThrowIfNull(values);

		int[] counts = new int[values.Count];
		double sum = values.Sum();
		if (values.Count == 0 || whole <= 0 || !double.IsFinite(whole) || sum <= 0)
		{
			return counts;
		}

		int target = (int)Math.Round(Math.Min(1, sum / whole) * cells, MidpointRounding.AwayFromZero);
		double[] exact = [.. values.Select(value => value / whole * cells)];
		int given = 0;
		for (int i = 0; i < exact.Length; i++)
		{
			counts[i] = (int)Math.Floor(exact[i]);
			given += counts[i];
		}

		foreach (int i in Enumerable.Range(0, exact.Length)
			         .OrderByDescending(i => exact[i] - Math.Floor(exact[i]))
			         .ThenByDescending(i => values[i])
			         .Take(Math.Max(0, target - given)))
		{
			counts[i]++;
		}

		return counts;
	}

	/// <summary>A tooltip and keyboard reading for one mark of a whole.</summary>
	/// <param name="index">The mark's drawn position.</param>
	/// <param name="heading">The tooltip's heading, such as the segment's or the tile's name.</param>
	/// <param name="colorClass">The class that colors its key.</param>
	/// <param name="colorStyle">An inline color for its key.</param>
	/// <param name="valueText">The value, written.</param>
	/// <param name="note">A line under it, such as its share.</param>
	/// <param name="point">What a template and the callbacks get.</param>
	public static ChartActiveInfo Describe(int index, string heading, string colorClass, string? colorStyle, string valueText,
		string? note, MokaChartPoint point)
	{
		IReadOnlyList<string> notes = note is null ? [] : [note];
		string cell = note is null ? $"{heading}, {valueText}" : $"{heading}, {valueText}, {note}";
		return new ChartActiveInfo(heading, [new ChartActiveRow(point.SeriesName, colorClass, colorStyle, valueText, null, true)],
			notes, cell, new MokaChartTooltipContext(index, heading, [point], point, null, notes));
	}
}
