using System.ComponentModel;
using Microsoft.AspNetCore.Components;

namespace Moka.Red.Data.Chart;

/// <summary>
///     Shared chart chrome: the HTML marks of the charts drawn in columns, bars, histogram bins and candles, so their
///     sizes, gaps, rounded ends, lift and printed values come from one stylesheet. Bars are boxes, so CSS gives exact
///     pixel caps and gaps, hatching by gradient, and container queries that print a value only where it fits. The chart
///     passes itself; the marks come from the chart. Not meant to be used on its own, and draws nothing without a chart.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class MokaChartColumns : ComponentBase
{
	/// <summary>The chart whose columns this draws.</summary>
	[Parameter]
	public MokaChartBase? Chart { get; set; }

	/// <summary>The panel it draws, for charts drawn as small multiples.</summary>
	[Parameter]
	public int Panel { get; set; }
}
