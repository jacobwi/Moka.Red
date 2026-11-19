using Bunit;
using Moka.Red.Primitives.LogViewer;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     The whole markup of a log viewer as it was before it could virtualize, page through a provider or open a row.
///     Used as before, it has to stay byte for byte what these files hold, apart from the scope attributes.
///     <c>MOKA_RECORD_GOLDENS=1</c> writes them again.
/// </summary>
public sealed class MokaLogViewerGoldenTests : BunitContext
{
	private static readonly DateTime At = new(2026, 11, 19, 9, 30, 15, 250, DateTimeKind.Utc);

	private static readonly IReadOnlyList<MokaLogEntry> Entries =
	[
		new(At, MokaLogLevel.Trace, "Entering ProcessOrder"),
		new(At.AddSeconds(1), MokaLogLevel.Info, "Application started.", "Host"),
		new(At.AddSeconds(2), MokaLogLevel.Warning, "Cache miss for key 'user:42'", "Cache"),
		new(At.AddSeconds(3), MokaLogLevel.Error, "Connection refused: db-primary:5432", "Database"),
		new(At.AddSeconds(4), MokaLogLevel.Fatal, "Out of memory <heap>", "Runtime")
	];

	public MokaLogViewerGoldenTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void Default() => SmallChartGoldenTests.Golden("log-viewer-default", () => Render<MokaLogViewer>(p => p
		.Add(x => x.Entries, Entries)));

	[Fact]
	public void Empty() => SmallChartGoldenTests.Golden("log-viewer-empty", () => Render<MokaLogViewer>());

	[Fact]
	public void Options() => SmallChartGoldenTests.Golden("log-viewer-options", () => Render<MokaLogViewer>(p => p
		.Add(x => x.Entries, Entries)
		.Add(x => x.MaxEntries, 3)
		.Add(x => x.ShowTimestamp, false)
		.Add(x => x.ShowSearch, false)
		.Add(x => x.MonoFont, false)
		.Add(x => x.MaxHeight, "200px")
		.Add(x => x.Class, "mine")
		.Add(x => x.Style, "opacity: 0.9")
		.Add(x => x.Id, "log")
		.AddUnmatched("data-sample", "yes")));

	[Fact]
	public void Filtered()
	{
		SmallChartGoldenTests.Golden("log-viewer-filtered", () =>
		{
			IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
				.Add(x => x.Entries, Entries)
				.Add(x => x.ShowLevel, false));
			cut.Find(".moka-log-viewer__filter-btn--info").Click();
			cut.Find(".moka-log-viewer__search-input").Input("a");
			return cut;
		});
	}
}
