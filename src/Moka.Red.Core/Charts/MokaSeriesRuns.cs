namespace Moka.Red.Core.Charts;

/// <summary>Splits a series at its gaps. A value that isn't a finite number, usually NaN, is a gap.</summary>
public static class MokaSeriesRuns
{
	/// <summary>The first and last index of each stretch of values with no gap in it, in order.</summary>
	/// <param name="values">The series.</param>
	/// <returns>The stretches.</returns>
	public static IReadOnlyList<(int First, int Last)> Of(IReadOnlyList<double> values)
	{
		ArgumentNullException.ThrowIfNull(values);

		List<(int First, int Last)> runs = [];
		int first = -1;
		for (int i = 0; i < values.Count; i++)
		{
			first = Step(runs, first, i, double.IsFinite(values[i]));
		}

		if (first >= 0)
		{
			runs.Add((first, values.Count - 1));
		}

		return runs;
	}

	/// <inheritdoc cref="Of(IReadOnlyList{double})" />
	public static IReadOnlyList<(int First, int Last)> Of(ReadOnlySpan<double> values)
	{
		List<(int First, int Last)> runs = [];
		int first = -1;
		for (int i = 0; i < values.Length; i++)
		{
			first = Step(runs, first, i, double.IsFinite(values[i]));
		}

		if (first >= 0)
		{
			runs.Add((first, values.Length - 1));
		}

		return runs;
	}

	private static int Step(List<(int First, int Last)> runs, int first, int index, bool finite)
	{
		if (finite)
		{
			return first < 0 ? index : first;
		}

		if (first >= 0)
		{
			runs.Add((first, index - 1));
		}

		return -1;
	}
}
