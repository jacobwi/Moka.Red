namespace Moka.Red.Core.Charts;

/// <summary>
///     Works out a log axis: whole powers of ten from below the smallest value to above the largest, with minor ticks
///     at 2 and 5 times each power when the axis spans three decades or fewer. Values at or below zero can't be shown
///     on it, so a chart draws them as gaps.
/// </summary>
public static class MokaLogScale
{
	private const int MostDecades = 300;

	/// <summary>A log axis for data that runs from <paramref name="dataMin" /> to <paramref name="dataMax" />.</summary>
	/// <param name="dataMin">
	///     The smallest value above zero. Anything else is read as a tenth of <paramref name="dataMax" />, so a series that
	///     touches zero still gets a decade below its top.
	/// </param>
	/// <param name="dataMax">The largest value.</param>
	/// <param name="options">
	///     Fixed and suggested ends, values to reach and explicit ticks, each used only when above zero. The nice and
	///     zero options don't apply to a log axis.
	/// </param>
	/// <returns>The axis.</returns>
	public static MokaScale Create(double dataMin, double dataMax, MokaScaleOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		double high = Positive(dataMax) ? dataMax : Positive(dataMin) ? dataMin : 1;
		double low = Positive(dataMin) ? dataMin : high / 10;
		if (low > high)
		{
			(low, high) = (high, low);
		}

		foreach (double value in (options.Reach ?? []).Concat(options.TickValues ?? []))
		{
			if (Positive(value))
			{
				low = Math.Min(low, value);
				high = Math.Max(high, value);
			}
		}

		if (options.SuggestedMin is { } suggestedMin && Positive(suggestedMin))
		{
			low = Math.Min(low, suggestedMin);
		}

		if (options.SuggestedMax is { } suggestedMax && Positive(suggestedMax))
		{
			high = Math.Max(high, suggestedMax);
		}

		int first = (int)Math.Floor(Math.Log10(options.Min is { } fixedMin && Positive(fixedMin) ? fixedMin : low) + 1e-9);
		int last = Math.Max(first + 1,
			(int)Math.Ceiling(Math.Log10(options.Max is { } fixedMax && Positive(fixedMax) ? fixedMax : high) - 1e-9));
		last = Math.Min(last, first + MostDecades);

		double min = options.Min is { } setMin && Positive(setMin) ? setMin : MokaNiceScale.Times(1, first);
		double max = options.Max is { } setMax && Positive(setMax) ? setMax : MokaNiceScale.Times(1, last);
		if (min >= max)
		{
			max = min * 10;
		}

		int maxTicks = Math.Max(2, options.MaxTicks);
		List<double> ticks;
		if (options.TickValues is { Count: > 0 } chosen && chosen.Any(Positive))
		{
			ticks = [.. chosen.Where(Positive).Distinct().Order().Take(maxTicks)];
		}
		else
		{
			ticks = Decades(first, last, min, max, last - first <= 3, maxTicks);
		}

		MokaChartUnit? unit = options.Unit;
		MokaUnitStep unitStep = unit?.StepFor(max) ?? new MokaUnitStep(1, string.Empty);
		double factor = double.IsFinite(unitStep.Factor) && unitStep.Factor > 0 ? unitStep.Factor : 1;
		int decimals = MokaScaleMath.DecimalsOf(ticks.Select(tick => tick / factor));
		return new MokaScale(min, max, ticks, null, unitStep, decimals, MokaScaleKind.Log) { ChartUnit = unit };
	}

	private static bool Positive(double value) => double.IsFinite(value) && value > 0;

	private static List<double> Decades(int first, int last, double min, double max, bool minors, int maxTicks)
	{
		List<double> ticks = [];
		double[] multiples = minors ? [1, 2, 5] : [1];
		for (int exponent = first; exponent <= last && ticks.Count < maxTicks; exponent++)
		{
			foreach (double multiple in multiples)
			{
				double tick = MokaNiceScale.Times(multiple, exponent);
				if (tick >= min * (1 - 1e-9) && tick <= max * (1 + 1e-9) && ticks.Count < maxTicks)
				{
					ticks.Add(tick);
				}
			}
		}

		if (ticks.Count == 0 || ticks[0] > min * (1 + 1e-9))
		{
			ticks.Insert(0, min);
		}

		if (ticks[^1] < max * (1 - 1e-9))
		{
			ticks.Add(max);
		}

		return ticks;
	}
}
