namespace Moka.Red.Data.Chart;

/// <summary>Every word a <see cref="MokaCandlestickChart" /> writes beyond the shared chart words. English by default.</summary>
public sealed record MokaCandleText
{
	/// <summary>The words in English.</summary>
	public static MokaCandleText Default { get; } = new();

	/// <summary>The first price, in the tooltip and the table.</summary>
	public string Open { get; init; } = "open";

	/// <summary>The highest price.</summary>
	public string High { get; init; } = "high";

	/// <summary>The lowest price.</summary>
	public string Low { get; init; } = "low";

	/// <summary>The last price.</summary>
	public string Close { get; init; } = "close";

	/// <summary>How much traded.</summary>
	public string Volume { get; init; } = "volume";

	/// <summary>When a period starts, as the table's first heading.</summary>
	public string Time { get; init; } = "Time";

	/// <summary>The legend's name for a period that closed at or above its open.</summary>
	public string Up { get; init; } = "Up";

	/// <summary>The legend's name for a period that closed below its open.</summary>
	public string Down { get; init; } = "Down";
}
