using Moka.Red.Core.Icons;

namespace Moka.Red.Data.Chart;

/// <summary>What a state of a <see cref="MokaStateTimeline" /> means, which decides its color, icon and how it merges.</summary>
public enum MokaStateTone
{
	/// <summary>Working as it should: the success color, with a check. The only tone that counts as up.</summary>
	Good,

	/// <summary>Working, but not well, such as degraded: the warning color, with a warning sign.</summary>
	Warn,

	/// <summary>Not working, such as down: the error color, with a cross.</summary>
	Bad,

	/// <summary>Planned or for information, such as maintenance: the info color, with a wrench.</summary>
	Info,

	/// <summary>Turned off on purpose, such as stopped: the muted color, with a power mark.</summary>
	Off,

	/// <summary>
	///     Not known, such as no data: hatched in a faint color, with a question mark. Time in it is left out of the uptime,
	///     and time no span covers takes the first state with this tone.
	/// </summary>
	Unknown
}

/// <summary>A state a <see cref="MokaStateTimeline" /> draws: what its spans' <see cref="MokaStateSpan.State" /> key means.</summary>
/// <param name="Key">The key spans name it by, and the legend hides and highlights it by.</param>
/// <param name="Name">What it is called, in the legend, the tooltip and the table.</param>
public sealed record MokaTimelineState(string Key, string Name)
{
	/// <summary>What it means, which picks its color and icon. Defaults to <see cref="MokaStateTone.Good" />.</summary>
	public MokaStateTone Tone { get; init; } = MokaStateTone.Good;

	/// <summary>A CSS color or token that wins over the tone's color. Anything that isn't a color is ignored.</summary>
	public string? ColorValue { get; init; }

	/// <summary>The icon beside its name in the legend and the tooltip. Null takes the tone's.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>
	///     Which state shows where two overlap, or where a pixel holds several: the higher wins, and between equals the one
	///     that started later. Null takes the tone's: Unknown 0, Good 10, Info 20, Off 30, Warn 40, Bad 50.
	/// </summary>
	public int? Severity { get; init; }

	/// <summary>Whether it is drawn hatched. Null hatches only the <see cref="MokaStateTone.Unknown" /> tone.</summary>
	public bool? Hatched { get; init; }
}
