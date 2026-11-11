using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Chart;
using Moka.Red.Icons;
using Moka.Red.Primitives.Meter;
using Moka.Red.Primitives.Stat;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     The whole markup of the meter, the stat and the sparkline as they were before the chart work gave them new parameters. With no
///     new parameter set they have to stay byte for byte what these files hold, apart from the scope attributes. Set
///     <c>MOKA_RECORD_GOLDENS=1</c> to write the files again after a change that is meant to show. The meter's were
///     written again in 0.4.2, when its divs became spans with every pixel unchanged (<c>MokaMeterInlineTests</c>).
/// </summary>
public sealed partial class SmallChartGoldenTests : BunitContext
{
	private const string RecordVariable = "MOKA_RECORD_GOLDENS";

	[Fact]
	public void Meter_Default() => Golden("meter-default", () => Render<MokaMeter>(p => p
		.Add(x => x.Value, 42d)
		.Add(x => x.Label, "CPU")));

	[Fact]
	public void Meter_Segments() => Golden("meter-segments", () => Render<MokaMeter>(p => p
		.Add(x => x.Value, 63.5d)
		.Add(x => x.ShowValue, false)
		.Add(x => x.Size, MokaSize.Sm)
		.Add(x => x.Color, MokaColor.Warning)
		.Add(x => x.Rounded, MokaRounding.Sm)
		.Add(x => x.Segments,
		[
			new MokaMeterSegment(0, 60, "var(--moka-color-success)"),
			new MokaMeterSegment(60, 85, "#ffab40"),
			new MokaMeterSegment(85, 100, "not a color")
		])));

	[Fact]
	public void Meter_OverTheTop() => Golden("meter-over", () => Render<MokaMeter>(p => p
		.Add(x => x.Value, 1200d)
		.Add(x => x.Min, 200d)
		.Add(x => x.Max, 1000d)
		.Add(x => x.Format, "N1")
		.Add(x => x.Label, "Disk")
		.Add(x => x.Size, MokaSize.Lg)
		.Add(x => x.Color, MokaColor.Error)
		.Add(x => x.Class, "mine")
		.Add(x => x.Style, "opacity: 0.9")
		.Add(x => x.Id, "disk-meter")
		.Add(x => x.Margin, MokaSpacingScale.Sm)));

	[Fact]
	public void Stat_Default() => Golden("stat-default", () => Render<MokaStat>(p => p
		.Add(x => x.Value, "1.28M")
		.Add(x => x.Label, "Requests today")
		.Add(x => x.Trend, 4.2)
		.Add(x => x.TrendLabel, "vs yesterday")
		.Add(x => x.HelpText, "Since midnight")
		.Add(x => x.Icon, MokaIcons.Status.Info)));

	[Fact]
	public void Stat_TileWithVisual() => Golden("stat-tile", () => Render<MokaStat>(p => p
		.Add(x => x.Value, "212")
		.Add(x => x.Prefix, "p95")
		.Add(x => x.Suffix, "ms")
		.Add(x => x.Tile, true)
		.Add(x => x.Mono, true)
		.Add(x => x.Color, MokaColor.Success)
		.Add(x => x.Trend, -8d)
		.Add(x => x.Visual, (RenderFragment)(builder => builder.AddMarkupContent(0, "<i>spark</i>")))));

	[Fact]
	public void Stat_IconContent() => Golden("stat-icon-content", () => Render<MokaStat>(p => p
		.Add(x => x.Value, "7")
		.Add(x => x.Size, MokaSize.Sm)
		.Add(x => x.Align, MokaTextAlign.Center)
		.Add(x => x.Padding, MokaSpacingScale.Md)
		.Add(x => x.IconContent, (RenderFragment)(builder => builder.AddContent(0, "x")))));

	[Fact]
	public void Sparkline_Default() => Golden("sparkline-default", () => Render<MokaSparkline>(p => p
		.Add(x => x.Values, [3d, 5, 2, 8, 6, 9, 4])));

	[Fact]
	public void Sparkline_Gaps() => Golden("sparkline-gaps", () => Render<MokaSparkline>(p => p
		.Add(x => x.Values, [double.NaN, 1d, 4, double.NaN, 7, double.NaN, 2, 3, double.NaN])
		.Add(x => x.ShowArea, false)
		.Add(x => x.Color, MokaColor.Success)
		.Add(x => x.StrokeWidth, 1.5)
		.Add(x => x.Height, "24px")
		.Add(x => x.Label, "Latency")
		.Add(x => x.Size, MokaSize.Lg)));

	[Fact]
	public void Sparkline_Scale() => Golden("sparkline-scale", () => Render<MokaSparkline>(p => p
		.Add(x => x.Values, [-2d, 0, 15, 4, 11])
		.Add(x => x.Min, 0d)
		.Add(x => x.Max, 10d)
		.Add(x => x.ColorValue, "var(--moka-color-info)")
		.Add(x => x.ShowLastPoint, false)
		.Add(x => x.Class, "mine")
		.Add(x => x.Style, "opacity: 0.8")
		.Add(x => x.Id, "spark")
		.Add(x => x.Margin, MokaSpacingScale.Xs)));

	// The longest series still drawn point for point.
	[Fact]
	public void Sparkline_FourHundredEightyValues() => Golden("sparkline-480", () => Render<MokaSparkline>(p => p
		.Add(x => x.Values, [.. Enumerable.Range(0, 480).Select(i => Math.Round(Math.Sin(i / 9d) * 10 + (i % 7), 2))])));

	internal static void Golden<TComponent>(string name, Func<IRenderedComponent<TComponent>> render)
		where TComponent : IComponent
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo uiCulture = CultureInfo.CurrentUICulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
		try
		{
			string markup = Normalize(render().Markup);
			string file = Path.Combine(GoldenFolder(), $"{name}.html");

			if (Environment.GetEnvironmentVariable(RecordVariable) == "1")
			{
				File.WriteAllText(file, markup);
				return;
			}

			Assert.True(File.Exists(file), $"No golden for {name}. Run the tests once with {RecordVariable}=1 to write it.");
			Assert.Equal(File.ReadAllText(file).ReplaceLineEndings("\n"), markup);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
		}
	}

	private static string Normalize(string markup) =>
		ScopeAttribute().Replace(markup, string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal);

	private static string GoldenFolder()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "Moka.Red.slnx")))
			{
				string goldens = Path.Combine(folder.FullName, "tests", "Moka.Red.Primitives.Tests", "Goldens");
				Directory.CreateDirectory(goldens);
				return goldens;
			}
		}

		Assert.Fail("The tests are not running inside the repository, so there are no goldens to read.");
		return string.Empty;
	}

	[GeneratedRegex(@" b-[a-z0-9]{10}(="""")?")]
	private static partial Regex ScopeAttribute();
}
