using AngleSharp.Dom;
using Bunit;
using Moka.Red.Primitives.LogViewer;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     A long source name on a narrow row: the message keeps room to be read. The rules live in the stylesheet, which bUnit
///     doesn't apply, so they are read as text.
/// </summary>
public sealed class MokaLogViewerLayoutTests : BunitContext
{
	private const string Css = "MokaLogViewer.razor.css";

	private static readonly MokaLogEntry Line = new(new DateTime(2026, 11, 19, 9, 30, 0, DateTimeKind.Utc), MokaLogLevel.Info,
		"Request handled", "Shop.Billing.Infrastructure.Persistence.InvoiceRepository");

	public MokaLogViewerLayoutTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// Where the message has its 16em beside the time, level and source, the row is the one line it was; with less, the
	// message takes a line of its own at the row's full width.
	[Fact]
	public void APlainRow_GivesTheMessageALineOfItsOwn_WhenItWouldBeSqueezed()
	{
		Assert.Equal("wrap", ScopedCss.Declarations(Css, ".moka-log-viewer__row")["flex-wrap"]);
		IReadOnlyDictionary<string, string> message = ScopedCss.Declarations(Css, ".moka-log-viewer__message");
		Assert.Equal("1 1 16em", message["flex"]);
		Assert.Equal("0", message["min-width"]);
	}

	[Fact]
	public void ASourceLongerThanTheRow_BreaksInsideIt()
	{
		IReadOnlyDictionary<string, string> source = ScopedCss.Declarations(Css, ".moka-log-viewer__source");

		Assert.Equal("100%", source["max-width"]);
		Assert.Equal("anywhere", source["overflow-wrap"]);
		Assert.Equal("0", source["flex-shrink"]);
		Assert.False(source.ContainsKey("white-space"));
	}

	// A virtualized row is one line of one height, so it can't wrap: the source gives way instead.
	[Fact]
	public void AVirtualRow_StaysOneLine_AndCutsTheSource_SoTheMessageKeepsItsShare()
	{
		IReadOnlyDictionary<string, string> row = ScopedCss.Declarations(Css, ".moka-log-viewer--virtual .moka-log-viewer__row");
		IReadOnlyDictionary<string, string> source = ScopedCss.Declarations(Css, ".moka-log-viewer--virtual .moka-log-viewer__source");
		IReadOnlyDictionary<string, string> message = ScopedCss.Declarations(Css, ".moka-log-viewer--virtual .moka-log-viewer__message");

		Assert.Equal("nowrap", row["flex-wrap"]);
		Assert.Equal("1", source["flex-shrink"]);
		Assert.Equal("0", source["min-width"]);
		Assert.Equal("ellipsis", source["text-overflow"]);
		Assert.Equal("hidden", source["overflow"]);
		Assert.Equal("0%", message["flex-basis"]);
		Assert.Equal("min(16em, 30%)", message["min-width"]);
	}

	[Fact]
	public void AVirtualRowsSource_CarriesItsWholeNameInATitle()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, [Line])
			.Add(x => x.Virtualize, true));

		Assert.Equal(Line.Source, cut.Find(".moka-log-viewer__source").GetAttribute("title"));
	}

	// A plain row shows the whole name, so it needs no title and its markup is what it was.
	[Fact]
	public void APlainRowsSource_HasNoTitle()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.Entries, [Line]));

		IElement source = cut.Find(".moka-log-viewer__source");
		Assert.False(source.HasAttribute("title"));
	}

	[Fact]
	public void ThePaneTitle_Wraps()
	{
		Assert.Equal("wrap", ScopedCss.Declarations(Css, ".moka-log-viewer__pane-title")["flex-wrap"]);
	}

	// Rows the viewer puts above the reader are moved past by its script; the browser's anchoring would move them twice.
	[Fact]
	public void KeepingPlace_TurnsTheBrowsersScrollAnchoringOff()
	{
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-log-viewer--anchored .moka-log-viewer__body")["overflow-anchor"]);
		Assert.False(ScopedCss.Declarations(Css, ".moka-log-viewer__body").ContainsKey("overflow-anchor"));
	}
}
