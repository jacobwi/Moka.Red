using Moka.Red.Core.Icons;
using Moka.Red.Icons;

namespace Moka.Red.Primitives.Meter;

internal enum MokaLimitState
{
	None,
	Warn,
	Critical
}

/// <summary>
///     The warning and critical limits <see cref="MokaMeter" /> and <see cref="Gauge.MokaGauge" /> share: which one a
///     value has reached, and the word and icon that say so.
/// </summary>
internal static class MokaLimits
{
	/// <summary>
	///     The limit <paramref name="share" /> of the range has reached: at or above a limit, or at or below it when
	///     <paramref name="lowIsBad" />. The critical limit wins.
	/// </summary>
	internal static MokaLimitState StateOf(double share, double? warnAt, double? criticalAt, bool lowIsBad)
	{
		if (criticalAt is { } critical && Reached(share, critical, lowIsBad))
		{
			return MokaLimitState.Critical;
		}

		return warnAt is { } warn && Reached(share, warn, lowIsBad) ? MokaLimitState.Warn : MokaLimitState.None;
	}

	internal static string? WordOf(MokaLimitState state, string? warnText, string? criticalText, bool lowIsBad) => state switch
	{
		MokaLimitState.Critical => criticalText ?? (lowIsBad ? "Empty" : "Full"),
		MokaLimitState.Warn => warnText ?? (lowIsBad ? "Low" : "Nearly full"),
		_ => null
	};

	internal static MokaIconDefinition? IconOf(MokaLimitState state) => state switch
	{
		MokaLimitState.Critical => MokaIcons.Status.Error,
		MokaLimitState.Warn => MokaIcons.Status.Warning,
		_ => default(MokaIconDefinition?)
	};

	private static bool Reached(double share, double limit, bool lowIsBad) => lowIsBad ? share <= limit : share >= limit;
}
