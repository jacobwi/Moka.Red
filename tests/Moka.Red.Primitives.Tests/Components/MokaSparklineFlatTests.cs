using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Moka.Red.Data.Chart;

namespace Moka.Red.Primitives.Tests.Components;

// A series whose values are all the same has no span. Before 0.4.3 it sat halfway up whatever Min and Max said, so a quiet
// device's day of zeros with Min 0 looked like half its traffic (MokaGate's report).
public partial class MokaSparklineFlatTests : BunitContext
{
	private const double Floor = 35;
	private const double Middle = 18;
	private const double Top = 1;

	[Fact]
	public void AFlatSeriesAtMin_SitsOnTheFloor() =>
		Assert.All(LineYs(p => p.Add(x => x.Values, [0d, 0, 0]).Add(x => x.Min, 0d)), y => Assert.Equal(Floor, y));

	[Fact]
	public void AFlatSeriesAtMinWithAMax_SitsOnTheFloor() =>
		Assert.All(LineYs(p => p.Add(x => x.Values, [0d, 0, 0]).Add(x => x.Min, 0d).Add(x => x.Max, 0d)),
			y => Assert.Equal(Floor, y));

	[Fact]
	public void AFlatSeriesAtMaxAlone_SitsAtTheTop() =>
		Assert.All(LineYs(p => p.Add(x => x.Values, [5d, 5, 5]).Add(x => x.Max, 5d)), y => Assert.Equal(Top, y));

	[Fact]
	public void AFlatSeriesWithNoBounds_StaysInTheMiddle() =>
		Assert.All(LineYs(p => p.Add(x => x.Values, [3d, 3, 3])), y => Assert.Equal(Middle, y));

	[Fact]
	public void ASeriesWithASpan_IsUnchanged() =>
		Assert.Equal([Floor, Top], LineYs(p => p.Add(x => x.Values, [0d, 10]).Add(x => x.Min, 0d)));

	private List<double> LineYs(Action<ComponentParameterCollectionBuilder<MokaSparkline>> parameters)
	{
		IRenderedComponent<MokaSparkline> cut = Render(parameters);
		string d = cut.Find(".moka-sparkline-line").GetAttribute("d")!;
		return [.. Point().Matches(d).Select(m => double.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture))];
	}

	[GeneratedRegex(@"(?<x>-?\d+\.\d+) (?<y>-?\d+\.\d+)")]
	private static partial Regex Point();
}
