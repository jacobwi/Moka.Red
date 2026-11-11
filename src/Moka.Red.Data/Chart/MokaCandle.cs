namespace Moka.Red.Data.Chart;

/// <summary>One period of a <see cref="MokaCandlestickChart" />: where a price opened and closed, and how high and low it went.</summary>
/// <param name="At">When the period starts.</param>
/// <param name="Open">The first price.</param>
/// <param name="High">The highest price.</param>
/// <param name="Low">The lowest price.</param>
/// <param name="Close">The last price.</param>
public sealed record MokaCandle(DateTimeOffset At, double Open, double High, double Low, double Close)
{
	/// <summary>How much traded in the period, drawn along the bottom with <see cref="MokaCandlestickChart.ShowVolume" />.</summary>
	public double? Volume { get; init; }
}
