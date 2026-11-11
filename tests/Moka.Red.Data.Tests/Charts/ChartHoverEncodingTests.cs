using System.Buffers.Binary;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     The bytes the chart script reads for hover: a header, the shared x shares and each series' y shares. Decoded here
///     the way the script decodes them.
/// </summary>
public sealed class ChartHoverEncodingTests : BunitContext
{
	private static readonly float[] Three = [0, 0.5f, 1];
	private static readonly float[] One = [0.3f];

	[Fact]
	public void TheHeader_SaysTheFormatTheCountsTheEdgesAndTheVersion()
	{
		ChartHoverData data = new(10, 20, new float[] { 0, 0.5f, 1 },
			[new float[] { 0, 0.25f, 1 }, new float[] { 1, 0.75f, 0 }]);

		Decoded geo = Decode(ChartHoverEncoding.Encode(data, 42));

		Assert.Equal(1, geo.Format);
		Assert.Equal(2, geo.Y.Count);
		Assert.Equal(3, geo.X.Length);
		Assert.Equal((10d, 20d, 42d), (geo.XMin, geo.XMax, geo.Version));
	}

	[Fact]
	public void Shares_RoundTrip_WithinOneStep()
	{
		float[] x = [.. Enumerable.Range(0, 101).Select(i => i / 100f)];
		float[] y = [.. x.Select(v => 1 - v)];

		Decoded geo = Decode(ChartHoverEncoding.Encode(new ChartHoverData(0, 1, x, [y]), 1));

		for (int i = 0; i < x.Length; i++)
		{
			Assert.InRange(Math.Abs(geo.X[i] - x[i]), 0, 1 / ChartHoverEncoding.Scale);
			Assert.InRange(Math.Abs(geo.Y[0][i] - y[i]), 0, 1 / ChartHoverEncoding.Scale);
		}

		Assert.Equal(0, ChartHoverEncoding.Share(0, true));
		Assert.Equal(65534, ChartHoverEncoding.Share(1, true));
	}

	[Fact]
	public void AGap_AndAnXOutsideThePlot_CantBePointedAt_WhileAYOutsideSitsOnTheEdge()
	{
		ChartHoverData data = new(0, 1, new float[] { -0.2f, 0.5f, 1.3f, float.NaN },
			[new float[] { float.NaN, 1.4f, -0.3f, 0.5f }]);

		Decoded geo = Decode(ChartHoverEncoding.Encode(data, 1));

		Assert.True(double.IsNaN(geo.X[0]));
		Assert.True(double.IsNaN(geo.X[2]));
		Assert.True(double.IsNaN(geo.X[3]));
		Assert.True(double.IsNaN(geo.Y[0][0]));
		Assert.Equal(1d, geo.Y[0][1]);
		Assert.Equal(0d, geo.Y[0][2]);
	}

	[Fact]
	public void ShortSeries_AreFilledWithGaps()
	{
		ChartHoverData data = new(0, 1, Three, [One]);

		Decoded geo = Decode(ChartHoverEncoding.Encode(data, 1));

		Assert.True(double.IsNaN(geo.Y[0][1]));
		Assert.True(double.IsNaN(geo.Y[0][2]));
	}

	[Fact]
	public void TheBytes_EndAfterTheLastSeries()
	{
		ChartHoverData data = new(0, 100, Three, [Three, Three]);

		byte[] bytes = ChartHoverEncoding.Encode(data, 7);

		Assert.Equal(ChartHoverEncoding.HeaderLength + (2 * 3 * 3), bytes.Length);
	}

	[Fact]
	public void FourSeriesOf1600Points_AreAbout16Kilobytes()
	{
		float[] x = new float[1600];
		byte[] bytes = ChartHoverEncoding.Encode(new ChartHoverData(0, 1, x, [x, x, x, x]), 1);

		Assert.Equal(ChartHoverEncoding.HeaderLength + 2 * 1600 * 5, bytes.Length);
		Assert.InRange(bytes.Length, 15_000, 17_000);
	}

	[Fact]
	public void HoverGeometry_IsTheChartsDrawnPoints_AtItsCurrentVersion()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Render<ChartSurfaceTestChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Series, [new SurfaceSeries("LAN", [10, 20, 40]), new SurfaceSeries("WAN", [0, 40, 20])]));
		IRenderedComponent<ChartSurfaceTestChart> empty = Render<ChartSurfaceTestChart>(p => p.Add(x => x.Look, MokaChartLook.Quiet));

		Decoded geo = Decode(cut.Instance.HoverGeometry(0, 0)!);

		Assert.Equal(cut.Instance.Base.GeometryVersion, (long)geo.Version);
		Assert.Equal([0d, 0.5, 1], geo.X.Select(v => Math.Round(v, 3)));
		Assert.Equal([0.25, 0.5, 1], geo.Y[0].Select(v => Math.Round(v, 3)));
		Assert.Null(empty.Instance.HoverGeometry(0, 0));
	}

	[Fact]
	public void AHiddenSeries_HasNoPointsToLandOn()
	{
		this.Interactive();
		IRenderedComponent<ChartSurfaceTestChart> cut = Render<ChartSurfaceTestChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.HiddenSeries, ["WAN"])
			.Add(x => x.Series, [new SurfaceSeries("LAN", [10, 20]), new SurfaceSeries("WAN", [5, 6])]));

		Decoded geo = Decode(cut.Instance.HoverGeometry(0, 0)!);

		Assert.All(geo.Y[1], value => Assert.True(double.IsNaN(value)));
		Assert.True(cut.Find(".moka-chart-surface__series > i[data-moka-key=WAN]").HasAttribute("data-moka-hidden"));
	}

	private static Decoded Decode(byte[] bytes)
	{
		ReadOnlySpan<byte> span = bytes;
		ushort format = BinaryPrimitives.ReadUInt16LittleEndian(span);
		int seriesCount = BinaryPrimitives.ReadUInt16LittleEndian(span[2..]);
		int count = (int)BinaryPrimitives.ReadUInt32LittleEndian(span[4..]);
		int offset = ChartHoverEncoding.HeaderLength;

		double[] Read(ReadOnlySpan<byte> source, int n)
		{
			double[] values = new double[n];
			for (int i = 0; i < n; i++, offset += 2)
			{
				ushort raw = BinaryPrimitives.ReadUInt16LittleEndian(source[offset..]);
				values[i] = raw == ChartHoverEncoding.Gap ? double.NaN : raw / ChartHoverEncoding.Scale;
			}

			return values;
		}

		double[] x = Read(span, count);
		List<double[]> y = [];
		for (int s = 0; s < seriesCount; s++)
		{
			y.Add(Read(span, count));
		}

		Assert.Equal(bytes.Length, offset);
		return new Decoded(format, BinaryPrimitives.ReadDoubleLittleEndian(span[8..]),
			BinaryPrimitives.ReadDoubleLittleEndian(span[16..]), BinaryPrimitives.ReadDoubleLittleEndian(span[24..]), x, y);
	}

	private sealed record Decoded(
		ushort Format,
		double XMin,
		double XMax,
		double Version,
		double[] X,
		IReadOnlyList<double[]> Y);
}
