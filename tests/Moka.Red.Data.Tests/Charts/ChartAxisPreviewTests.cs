using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Charts;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Chart;

namespace Moka.Red.Data.Tests.Charts;

/// <summary>
///     Writes the axis, row and annotation cases as HTML pages with the library's real stylesheets, to look at in a
///     browser at any width, since bUnit applies no CSS and no real chart draws on the plot in this package. Runs only
///     when <c>MOKA_CHART_PREVIEW</c> names a folder; otherwise it checks that every case still renders.
/// </summary>
public sealed class ChartAxisPreviewTests : BunitContext
{
	private static readonly string[] Hours = [.. Enumerable.Range(0, 12).Select(i => $"{i * 2:00}:00")];
	private static readonly string[] Days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

	private static readonly string[] Domains =
	[
		"telemetry.microsoft.com", "i.ytimg.com", "graph.facebook.com", "api.github.com", "time.cloudflare.com",
		"s3.eu-central-1.amazonaws.com", "updates.nas.local", "connectivity-check.ubuntu.com", "fonts.gstatic.com",
		"dns.google", "ocsp.digicert.com", "push.apple.com"
	];

	private static readonly string[] Regions =
	[
		"North America West", "North America East", "South America", "Europe West", "Europe North", "Middle East",
		"Africa South", "Asia Pacific East", "Asia Pacific South", "Oceania"
	];

	[Fact]
	public void EveryCase_Renders_AndIsWrittenWhenAsked()
	{
		this.Static();
		JSInterop.Mode = JSRuntimeMode.Loose;
		string? folder = Environment.GetEnvironmentVariable("MOKA_CHART_PREVIEW");

		foreach ((string name, Func<RenderFragment> page) in Pages())
		{
			foreach ((string theme, MokaTheme value) in new[] { ("red", MokaTheme.Default), ("dark", MokaTheme.Dark), ("redlight", MokaTheme.DefaultLight) })
			{
				IRenderedComponent<MokaThemeProvider> cut = Render<MokaThemeProvider>(p => p
					.Add(x => x.Theme, value)
					.Add(x => x.LoadFonts, false)
					.Add(x => x.ChildContent, page()));
				Assert.Contains("moka-chart-plot", cut.Markup, StringComparison.Ordinal);
				if (folder is not null)
				{
					Directory.CreateDirectory(folder);
					File.WriteAllText(Path.Combine(folder, $"{name}-{theme}.html"), Document(name, cut.Markup), Encoding.UTF8);
				}
			}
		}
	}

	private static IEnumerable<(string Name, Func<RenderFragment> Page)> Pages()
	{
		yield return ("labels", () => Cases(
			("Auto, 12 short hours at points", Chart(Set(("Labels", Hours)), values: 12)),
			("Auto, 7 days in slots", Chart(Set(("Labels", Days)), values: 7, slots: true)),
			("Auto, 12 long domains in slots", Chart(Set(("Labels", Domains)), values: 12, slots: true)),
			("Auto, 60 dates at points", Chart(Set(("Labels", ChartAxisTestChart.Names(60, i => $"Mar {i + 1}"))), values: 60)),
			("Rotate, 12 long domains", Chart(Set(("Labels", Domains), ("LabelFit", MokaAxisLabelFit.Rotate)), values: 12, slots: true)),
			("Stagger, 10 regions", Chart(Set(("Labels", Regions), ("LabelFit", MokaAxisLabelFit.Stagger)), values: 10, slots: true)),
			("Wrap, 10 regions in two lines", Chart(Set(("Labels", Regions), ("LabelFit", MokaAxisLabelFit.Wrap)), values: 10, slots: true)),
			("Truncate, 12 long domains", Chart(Set(("Labels", Domains), ("LabelFit", MokaAxisLabelFit.Truncate)), values: 12, slots: true)),
			("Thin, 100 numbered", Chart(Set(("Labels", ChartAxisTestChart.Names(100)), ("LabelFit", MokaAxisLabelFit.Thin)), values: 100)),
			("Scroll, 80 categories", Chart(Set(("Labels", ChartAxisTestChart.Names(80, i => $"host-{i + 1:000}")), ("LabelFit", MokaAxisLabelFit.Scroll)),
				values: 80, slots: true)),
			("MaxLabelEm 6, domains", Chart(Set(("Labels", Domains), ("XAxis", new MokaChartAxis { MaxLabelEm = 6 })), values: 12, slots: true))));

		yield return ("axes", () => Cases(
			("Time, 6 hours", Chart(Set(("Times", TimesOver(TimeSpan.FromHours(6), 72))), values: 72)),
			("Time, 48 hours", Chart(Set(("Times", TimesOver(TimeSpan.FromHours(48), 96))), values: 96)),
			("Time, 30 days", Chart(Set(("Times", TimesOver(TimeSpan.FromDays(30), 120))), values: 120)),
			("Numeric x", Chart(Set(), values: 20, numbers: (3, 97))),
			("Value axis inside, hairlines", Chart(Set(("Labels", Hours), ("TickLabels", MokaTickPlacement.Inside), ("Grid", MokaChartGrid.Hairlines)), values: 12)),
			("Value axis on the right, baseline", Chart(Set(("Labels", Hours), ("ValueSide", MokaAxisSide.End), ("Grid", MokaChartGrid.Baseline)), values: 12)),
			("Auto placement, dotted", Chart(Set(("Labels", Hours), ("TickLabels", MokaTickPlacement.Auto), ("Grid", MokaChartGrid.Dotted)), values: 12)),
			("Log, ms", Chart(Set(("Labels", Hours), ("YAxis", new MokaChartAxis { Scale = MokaAxisScale.Log, Unit = MokaChartUnit.Milliseconds, Title = "Duration" })),
				values: 12, spread: true)),
			("Bytes, nice, unit in the title", Chart(Set(("Labels", Hours),
				("YAxis", new MokaChartAxis { Unit = MokaChartUnit.Bytes, Nice = true, ShowUnit = false, Title = "Written" })), values: 12, scale: 3.4e9)),
			("Fixed height 140px", Chart(Set(("Labels", Hours), ("Height", "140px")), values: 12))));

		DateTimeOffset start = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
		yield return ("annotations", () => Cases(
			("Threshold and band", Chart(Set(("Labels", Hours), ("Unit", MokaChartUnit.Milliseconds),
				("Annotations", new MokaChartAnnotation[] { new MokaChartThreshold(200) { Label = "Limit 200 ms" }, new MokaChartBand(100, 150) { Label = "Target 100 to 150 ms" } })),
				values: 12, scale: 240)),
			("Target band", Chart(Set(("Labels", Hours), ("TargetLow", (double?)40), ("TargetHigh", (double?)70)), values: 12)),
			("Events and a window", Chart(Set(("Times", TimesOver(TimeSpan.FromHours(18), 72, start)),
				("Annotations", new MokaChartAnnotation[]
				{
					new MokaChartEvent(start.AddHours(9)) { Label = "Deploy 0.3.23", Tone = MokaColor.Info, Detail = "Rolled back the cache change" },
					new MokaChartEvent(start.AddHours(12.25)) { Label = "Incident", To = start.AddHours(13.25), Tone = MokaColor.Error, Shape = MokaChartMarker.Triangle },
					new MokaChartWindow(start.AddHours(2), start.AddHours(4)) { Label = "Maintenance" }
				})), values: 72))));

		yield return ("rows", () => Cases(
			("Rows, long names", Chart(Set(("Labels", Domains)), values: 12, rows: true)),
			("Rows with figures and a threshold", Chart(Set(("Labels", Domains),
				("Annotations", new MokaChartAnnotation[] { new MokaChartThreshold(60) { Label = "Quota" } })), values: 12, rows: true, figures: true)),
			("Rows in 160px, 40 rows", Chart(Set(("Labels", ChartAxisTestChart.Names(40, i => $"device-{i + 1:00}.lan")), ("MaxHeight", "160px")),
				values: 40, rows: true)),
			("Gantt-like time across", Chart(Set(("Labels", Days)), values: 7, rows: true,
				span: (start, start.AddHours(6))))));
	}

	private static Dictionary<string, object?> Set(params (string Name, object? Value)[] parameters) =>
		parameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Value);

	private static RenderFragment Cases(params (string Title, RenderFragment Chart)[] cases) => builder =>
	{
		int seq = 0;
		foreach ((string title, RenderFragment chart) in cases)
		{
			builder.OpenElement(seq++, "section");
			builder.AddAttribute(seq++, "class", "case");
			builder.OpenElement(seq++, "h3");
			builder.AddContent(seq++, title);
			builder.CloseElement();
			builder.AddContent(seq++, chart);
			builder.CloseElement();
		}
	};

	private static RenderFragment Chart(Dictionary<string, object?> parameters, int values, bool slots = false, bool rows = false,
		(double, double)? numbers = null, bool spread = false, double scale = 100, bool figures = false,
		(DateTimeOffset, DateTimeOffset)? span = null) => builder =>
	{
		double[] data = [.. Enumerable.Range(0, values).Select(i => spread
			? Math.Pow(10, 1 + (i % 4)) * (1 + (i % 3))
			: Math.Round(scale * (0.25 + (0.7 * Math.Abs(Math.Sin(i * 0.7))))))];

		builder.OpenComponent<ChartAxisTestChart>(0);
		builder.AddComponentParameter(1, nameof(ChartAxisTestChart.Values), data);
		builder.AddComponentParameter(2, nameof(ChartAxisTestChart.Slots), slots);
		builder.AddComponentParameter(3, nameof(ChartAxisTestChart.Rows), rows);
		builder.AddComponentParameter(4, nameof(ChartAxisTestChart.NumberRange), numbers);
		builder.AddComponentParameter(5, nameof(ChartAxisTestChart.TimeZone), TimeZoneInfo.Utc);
		builder.AddComponentParameter(6, nameof(ChartAxisTestChart.Figures), figures ? data.Select(v => $"{v:0} q").ToArray() : null);
		builder.AddComponentParameter(7, nameof(ChartAxisTestChart.TimeSpanAcross), span);
		foreach ((string name, object? value) in parameters)
		{
			builder.AddComponentParameter(8, name, value);
		}

		builder.CloseComponent();
	};

	internal static DateTimeOffset[] TimesOver(TimeSpan span, int count, DateTimeOffset? start = null)
	{
		DateTimeOffset from = start ?? new DateTimeOffset(2026, 11, 9, 18, 0, 0, TimeSpan.Zero);
		return [.. Enumerable.Range(0, count).Select(i => from + (span * i / (count - 1)))];
	}

	private static string Document(string name, string body)
	{
		string root = RepositoryRoot().Replace('\\', '/');
		string Link(string path) => $"<link rel=\"stylesheet\" href=\"file:///{root}/{path}\">";
		return $$"""
			<!doctype html>
			<html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width">
			<title>{{name}}</title>
			{{Link("src/Moka.Red.Core/wwwroot/moka-fonts.css")}}
			{{Link("src/Moka.Red.Core/wwwroot/moka.css")}}
			{{Link("src/Moka.Red.Core/obj/Debug/net10.0/scopedcss/projectbundle/Moka.Red.Core.bundle.scp.css")}}
			{{Link("src/Moka.Red.Primitives/obj/Debug/net10.0/scopedcss/projectbundle/Moka.Red.Primitives.bundle.scp.css")}}
			{{Link("src/Moka.Red.Data/obj/Debug/net10.0/scopedcss/projectbundle/Moka.Red.Data.bundle.scp.css")}}
			<style>
			body { margin: 0; }
			.moka-root { padding: 16px; }
			.case { margin: 0 0 28px; }
			.case h3 { margin: 0 0 6px; font: 600 13px var(--moka-font-family); color: var(--moka-color-on-surface); }
			.test-marks { position: absolute; inset: 0; width: 100%; height: 100%; }
			.test-bar { position: absolute; top: 25%; bottom: 25%; left: 0; background: var(--moka-color-primary); border-radius: 2px; }
			</style></head>
			<body>{{body}}</body></html>
			""";
	}

	private static string RepositoryRoot()
	{
		DirectoryInfo? directory = new(AppContext.BaseDirectory);
		while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Moka.Red.slnx")))
		{
			directory = directory.Parent;
		}

		return directory?.FullName ?? AppContext.BaseDirectory;
	}
}
