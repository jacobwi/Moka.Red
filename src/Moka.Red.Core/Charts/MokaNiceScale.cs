namespace Moka.Red.Core.Charts;

/// <summary>
///     Works out a linear axis: ends that reach the data, ticks on round steps in the unit the top of the axis reaches,
///     and the decimals to write them with.
/// </summary>
public static class MokaNiceScale
{
	private static readonly double[] DefaultMultiples = [1, 2, 2.5, 4, 5, 10];

	private const double Slack = 1e-9;

	/// <summary>An axis for these values. A value that isn't a finite number doesn't count.</summary>
	/// <param name="values">The values the axis has to show.</param>
	/// <param name="options">What the axis has to show besides, and how its ticks are rounded.</param>
	/// <returns>The axis.</returns>
	public static MokaScale Linear(ReadOnlySpan<double> values, MokaScaleOptions options)
	{
		double low = double.NaN;
		double high = double.NaN;
		foreach (double value in values)
		{
			if (!double.IsFinite(value))
			{
				continue;
			}

			if (!(value >= low))
			{
				low = value;
			}

			if (!(value <= high))
			{
				high = value;
			}
		}

		return Linear(low, high, options);
	}

	/// <summary>An axis for data that runs from <paramref name="dataMin" /> to <paramref name="dataMax" />.</summary>
	/// <param name="dataMin">The lowest value. NaN when there is none.</param>
	/// <param name="dataMax">The highest value. NaN when there is none.</param>
	/// <param name="options">What the axis has to show besides, and how its ticks are rounded.</param>
	/// <returns>The axis.</returns>
	public static MokaScale Linear(double dataMin, double dataMax, MokaScaleOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		(double low, double high) = Span(dataMin, dataMax, options);
		int maxTicks = Math.Max(2, options.MaxTicks);
		int divisions = Math.Clamp(options.TickCount, 1, maxTicks - 1);
		MokaChartUnit? unit = options.Unit;
		MokaUnitStep unitStep = UnitStep(unit, Math.Max(Math.Abs(low), Math.Abs(high)));

		if (options.TickValues is { Count: > 0 } chosen)
		{
			double[] ticks = [.. chosen.Where(double.IsFinite).Distinct().Order().Take(maxTicks)];
			if (ticks.Length > 0)
			{
				return Build(low, high, ticks, null, unitStep, unit);
			}
		}

		if (!options.Nice)
		{
			return Build(low, high, EqualTicks(low, high, divisions), null, unitStep, unit);
		}

		double step = 1;
		double niceLow = low;
		double niceHigh = high;
		for (int pass = 0; pass < 2; pass++)
		{
			double factor = FactorOf(unitStep);
			step = NiceStep((high - low) / factor / divisions, unitStep.Multiples) * factor;
			niceLow = options.Min is { } min && double.IsFinite(min) ? min : Math.Floor(low / step + Slack) * step;
			niceHigh = options.Max is { } max && double.IsFinite(max) ? max : Math.Ceiling(high / step - Slack) * step;

			MokaUnitStep reached = UnitStep(unit, Math.Max(Math.Abs(niceLow), Math.Abs(niceHigh)));
			if (reached.Equals(unitStep))
			{
				break;
			}

			unitStep = reached;
		}

		if (niceHigh - niceLow < double.Epsilon || !double.IsFinite(step) || step <= 0)
		{
			return Build(low, high, EqualTicks(low, high, divisions), null, unitStep, unit);
		}

		return Build(niceLow, niceHigh, SteppedTicks(niceLow, niceHigh, step, maxTicks), step, unitStep, unit);
	}

	/// <summary>
	///     Rounds a step up to a round one: 1, 2, 2.5, 4 or 5 times a power of ten, or the unit's own multiples when the
	///     step falls among them, such as 15 or 30 minutes.
	/// </summary>
	/// <param name="raw">The step the span divided evenly would take.</param>
	/// <param name="multiples">The unit's round steps, or null.</param>
	/// <returns>The round step, at least <paramref name="raw" />. 1 for a step that isn't a positive number.</returns>
	public static double NiceStep(double raw, IReadOnlyList<double>? multiples = null)
	{
		if (!double.IsFinite(raw) || raw <= 0)
		{
			return 1;
		}

		if (multiples is { Count: > 0 })
		{
			double[] usable = [.. multiples.Where(m => double.IsFinite(m) && m > 0).Order()];
			if (usable.Length > 0 && raw >= usable[0] * (1 - Slack) && raw <= usable[^1] * (1 + Slack))
			{
				foreach (double multiple in usable)
				{
					if (multiple >= raw * (1 - Slack))
					{
						return multiple;
					}
				}
			}
		}

		int exponent = (int)Math.Floor(Math.Log10(raw));
		foreach (double multiple in DefaultMultiples)
		{
			double candidate = Times(multiple, exponent);
			if (candidate >= raw * (1 - Slack))
			{
				return candidate;
			}
		}

		return Times(10, exponent);
	}

	// Divided by an exact power of ten below 1, so 2.5 times 10^-1 is 0.25 rather than 0.25000000000000006.
	internal static double Times(double multiple, int exponent) =>
		exponent >= 0 ? multiple * Math.Pow(10, exponent) : multiple / Math.Pow(10, -exponent);

	private static (double Low, double High) Span(double dataMin, double dataMax, MokaScaleOptions options)
	{
		double low = double.IsFinite(dataMin) ? dataMin : dataMax;
		double high = double.IsFinite(dataMax) ? dataMax : dataMin;
		if (!double.IsFinite(low) || !double.IsFinite(high))
		{
			(low, high) = (0, 1);
		}
		else if (low > high)
		{
			(low, high) = (high, low);
		}

		foreach (double value in (options.Reach ?? []).Concat(options.TickValues ?? []))
		{
			if (double.IsFinite(value))
			{
				low = Math.Min(low, value);
				high = Math.Max(high, value);
			}
		}

		if (options.IncludeZero)
		{
			low = Math.Min(low, 0);
			high = Math.Max(high, 0);
		}

		if (options.SuggestedMin is { } suggestedMin && double.IsFinite(suggestedMin))
		{
			low = Math.Min(low, suggestedMin);
		}

		if (options.SuggestedMax is { } suggestedMax && double.IsFinite(suggestedMax))
		{
			high = Math.Max(high, suggestedMax);
		}

		if (options.Min is { } min && double.IsFinite(min))
		{
			low = min;
		}

		if (options.Max is { } max && double.IsFinite(max))
		{
			high = max;
		}

		if (low > high)
		{
			(low, high) = (high, low);
		}

		if (high - low < double.Epsilon)
		{
			low -= 1;
			high += 1;
		}

		return (low, high);
	}

	private static MokaUnitStep UnitStep(MokaChartUnit? unit, double top) =>
		unit?.StepFor(top) ?? new MokaUnitStep(1, string.Empty);

	private static double FactorOf(MokaUnitStep step) =>
		double.IsFinite(step.Factor) && step.Factor > 0 ? step.Factor : 1;

	private static double[] EqualTicks(double low, double high, int divisions)
	{
		double[] ticks = new double[divisions + 1];
		for (int i = 0; i <= divisions; i++)
		{
			ticks[i] = low + (high - low) * i / divisions;
		}

		return ticks;
	}

	// A fixed end off the step still gets a tick.
	private static List<double> SteppedTicks(double low, double high, double step, int maxTicks)
	{
		List<double> ticks = [low];
		long first = (long)Math.Ceiling(low / step + Slack);
		for (long k = first; ticks.Count < maxTicks - 1; k++)
		{
			double tick = k * step;
			if (tick >= high - step * 1e-6)
			{
				break;
			}

			if (tick > low + step * 1e-6)
			{
				ticks.Add(tick);
			}
		}

		ticks.Add(high);
		return ticks;
	}

	private static MokaScale Build(double low, double high, IReadOnlyList<double> ticks, double? step, MokaUnitStep unitStep,
		MokaChartUnit? unit)
	{
		double factor = FactorOf(unitStep);
		int decimals = MokaScaleMath.DecimalsOf(ticks.Select(tick => tick / factor));
		return new MokaScale(low, high, ticks, step, unitStep, decimals, MokaScaleKind.Linear) { ChartUnit = unit };
	}
}
