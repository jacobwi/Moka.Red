namespace Moka.Red.Data.Chart;

/// <summary>Splits a line's values at its gaps. A value that isn't a finite number, usually NaN, is a gap.</summary>
internal static class MokaChartRuns
{
	/// <summary>The first and last index of each stretch of values with no gap in it, in order.</summary>
	public static IReadOnlyList<(int First, int Last)> Of(IReadOnlyList<double> values)
	{
		List<(int First, int Last)> runs = [];
		int first = -1;
		for (int i = 0; i < values.Count; i++)
		{
			if (double.IsFinite(values[i]))
			{
				if (first < 0)
				{
					first = i;
				}
			}
			else if (first >= 0)
			{
				runs.Add((first, i - 1));
				first = -1;
			}
		}

		if (first >= 0)
		{
			runs.Add((first, values.Count - 1));
		}

		return runs;
	}
}
