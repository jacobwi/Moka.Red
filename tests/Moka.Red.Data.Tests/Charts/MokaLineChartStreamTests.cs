#pragma warning disable CA5394 // Seeded Random keeps the property tests repeatable; nothing here is security related

using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;
using static Moka.Red.Data.Tests.Charts.MokaLineChartTestKit;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     A live chart whose window grows at its end or slides along reads only what arrived, and draws exactly what a chart
///     built from the same data at once draws.
/// </summary>
public sealed class MokaLineChartStreamTests : BunitContext
{
	private static readonly DateTimeOffset Start = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);

	public MokaLineChartStreamTests() => this.Static();

	[Theory]
	[InlineData(true, true, true)]
	[InlineData(true, true, false)]
	[InlineData(true, false, true)]
	[InlineData(false, true, true)]
	[InlineData(false, false, false)]
	[InlineData(false, true, false)]
	public void AWindowThatGrowsOrSlides_DrawsWhatAFreshChartDraws(bool versioned, bool timed, bool slide)
	{
		Random random = new(versioned ? 1 : 2);
		List<double> down = [.. Enumerable.Range(0, 5_000).Select(i => Sample(random, i))];
		List<double> up = [.. Enumerable.Range(0, 5_000).Select(i => Sample(random, i + 7))];
		List<DateTimeOffset> times = [.. Enumerable.Range(0, 5_000).Select(i => Start.AddSeconds(i * 2 + (i % 7 == 0 ? 1 : 0)))];
		long version = 1;
		IRenderedComponent<MokaLineChart> live = Render<MokaLineChart>(p => Live(p, down, up, timed ? times : null, versioned ? version : null));

		for (int tick = 0; tick < 12; tick++)
		{
			int arrived = random.Next(1, 4);
			for (int i = 0; i < arrived; i++)
			{
				if (slide)
				{
					down.RemoveAt(0);
					up.RemoveAt(0);
					times.RemoveAt(0);
				}

				down.Add(Sample(random, down.Count + tick));
				up.Add(Sample(random, up.Count + tick + 3));
				times.Add(times[^1].AddSeconds(2));
			}

			version++;
			live.Render(p => Live(p, versioned ? down : [.. down], versioned ? up : [.. up], timed ? versioned ? times : [.. times] : null,
				versioned ? version : null));

			Assert.True(live.Instance.LiveKept);
			AssertSameAsFresh(live, [.. down], [.. up], timed ? [.. times] : null);
		}
	}

	[Fact]
	public void AValueChangedInTheMiddle_StartsOver_AndStillDrawsTheData()
	{
		Random random = new(3);
		double[] values = [.. Enumerable.Range(0, 4_000).Select(i => Sample(random, i))];
		IRenderedComponent<MokaLineChart> live = Render<MokaLineChart>(p => Live(p, values, null, null, null));

		double[] edited = [.. values];
		edited[2_000] += 500;
		live.Render(p => Live(p, edited, null, null, null));

		Assert.False(live.Instance.LiveKept);
		AssertSameAsFresh(live, edited, null, null);
	}

	[Fact]
	public void TimesOutOfOrder_AreReadInFull_AndStillDrawTheData()
	{
		Random random = new(4);
		List<double> values = [.. Enumerable.Range(0, 4_000).Select(i => Sample(random, i))];
		List<DateTimeOffset> times = [.. Enumerable.Range(0, 4_000).Select(i => Start.AddSeconds(i % 500 == 0 ? i - 3 : i))];
		IRenderedComponent<MokaLineChart> live = Render<MokaLineChart>(p => Live(p, values.ToArray(), null, times.ToArray(), null));

		values.Add(42);
		times.Add(times[^1].AddSeconds(1));
		live.Render(p => Live(p, values.ToArray(), null, times.ToArray(), null));

		Assert.True(live.Instance.LiveKept);
		AssertSameAsFresh(live, [.. values], null, [.. times]);
	}

	[Fact]
	public void ASeriesThatComesOrGoes_StartsOver()
	{
		double[] values = [.. Enumerable.Range(0, 3_000).Select(i => (double)(i % 17))];
		IRenderedComponent<MokaLineChart> live = Render<MokaLineChart>(p => Live(p, values, null, null, null));

		live.Render(p => Live(p, [.. values, 3], [.. values, 4], null, null));

		Assert.False(live.Instance.LiveKept);
		AssertSameAsFresh(live, [.. values, 3], [.. values, 4], null);
	}

	[Fact]
	public void AChartThatIsntLive_KeepsNothing()
	{
		double[] values = [.. Enumerable.Range(0, 3_000).Select(i => (double)(i % 17))];
		IRenderedComponent<MokaLineChart> cut = Render<MokaLineChart>(p => p
			.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Series, [Series("Down", values)]));

		cut.Render(p => p.Add(x => x.Series, [Series("Down", [.. values, 5])]));

		Assert.False(cut.Instance.LiveKept);
	}

	private static MokaLineSeries Named(string name, IReadOnlyList<double> values) => new() { Name = name, Values = values };

	private static double Sample(Random random, int i) =>
		random.NextDouble() < 0.01 ? double.NaN : Math.Round(Math.Sin(i / 80d) * 40 + random.NextDouble() * 10, 0);

	private static ComponentParameterCollectionBuilder<MokaLineChart> Live(ComponentParameterCollectionBuilder<MokaLineChart> p,
		IReadOnlyList<double> down, IReadOnlyList<double>? up, IReadOnlyList<DateTimeOffset>? times, long? version, bool live = true) =>
		p.Add(x => x.Look, MokaChartLook.Quiet)
			.Add(x => x.Live, live)
			.Add(x => x.Times, times)
			.Add(x => x.DataVersion, version)
			.Add(x => x.Series, up is null ? [Named("Down", down)] : [Named("Down", down), Named("Up", up)]);

	private void AssertSameAsFresh(IRenderedComponent<MokaLineChart> live, double[] down, double[]? up, DateTimeOffset[]? times)
	{
		// Not live, so it picks its points through the downsampler, reading every value.
		IRenderedComponent<MokaLineChart> fresh = Render<MokaLineChart>(p => Live(p, down, up, times, null, live: false));
		Assert.False(fresh.Instance.LiveKept);

		Assert.Equal(Paths(fresh), Paths(live));
		ChartHoverData expected = fresh.Instance.HoverData(0)!;
		ChartHoverData actual = live.Instance.HoverData(0)!;
		Assert.Equal(expected.X.ToArray(), actual.X.ToArray());
		Assert.Equal(expected.Y.Count, actual.Y.Count);
		for (int s = 0; s < expected.Y.Count; s++)
		{
			Assert.Equal(expected.Y[s].ToArray(), actual.Y[s].ToArray());
		}
	}

	private static string[] Paths(IRenderedComponent<MokaLineChart> cut) =>
		[.. Marks(cut.Find(".moka-linechart")).QuerySelectorAll("path").Select(path => $"{path.ClassName} {path.GetAttribute("d")}")];
}
