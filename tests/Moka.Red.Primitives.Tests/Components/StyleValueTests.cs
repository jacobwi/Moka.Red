using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.Primitives.ColorSwatch;
using Moka.Red.Primitives.Kanban;
using Moka.Red.Primitives.LogViewer;
using Moka.Red.Primitives.Meter;
using Moka.Red.Primitives.Terminal;
using Moka.Red.Primitives.ThemeSwitcher;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// These components wrote string parameters into style attributes they built by hand, outside
// StyleBuilder, so a semicolon or a closing parenthesis in a value added declarations of its own.
public class StyleValueTests : BunitContext
{
	public static TheoryData<string> HostileValues =>
	[
		"red; position: fixed; inset: 0; z-index: 9999",
		"red) 0 0, url(https://example.com/track.png",
		"10px; background: url(https://example.com/track.png)",
		"\"open string"
	];

	public StyleValueTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static List<string> Properties(IElement element) =>
		CssDeclarations.PropertyNames(element.GetAttribute("style") ?? "");

	[Theory]
	[MemberData(nameof(HostileValues))]
	public void ColorSwatch_DrawsAValueThatIsNotAColorEmpty(string hostile)
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Colors, new[] { "#c62828", hostile })
			.Add(x => x.SelectedColor, hostile));

		IReadOnlyList<IElement> swatches = cut.FindAll(".moka-color-swatch-grid > button");
		Assert.Equal("background-color: #c62828", swatches[0].GetAttribute("style"));
		Assert.Null(swatches[1].GetAttribute("style"));
		Assert.Null(cut.Find(".moka-color-swatch-preview-dot").GetAttribute("style"));
	}

	[Theory]
	[MemberData(nameof(HostileValues))]
	public void KanbanBoard_KeepsAColumnWidthInItsDeclarations(string hostile)
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Render<MokaKanbanBoard<string>>(p => p
			.Add(x => x.Columns, new[] { new MokaKanbanColumn<string>("To do", ["Write the docs"]) })
			.Add(x => x.ColumnWidth, hostile));

		Assert.Empty(Properties(cut.Find(".moka-kanban__column")));
	}

	[Fact]
	public void KanbanBoard_AppliesAColumnWidth()
	{
		IRenderedComponent<MokaKanbanBoard<string>> cut = Render<MokaKanbanBoard<string>>(p => p
			.Add(x => x.Columns, new[] { new MokaKanbanColumn<string>("To do", ["Write the docs"]) })
			.Add(x => x.ColumnWidth, "18rem"));

		Assert.Equal("width: 18rem; min-width: 18rem", cut.Find(".moka-kanban__column").GetAttribute("style"));
	}

	[Theory]
	[MemberData(nameof(HostileValues))]
	public void LogViewer_KeepsMaxHeightInItsDeclaration(string hostile)
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.MaxHeight, hostile));

		Assert.Empty(Properties(cut.Find(".moka-log-viewer__body")));
	}

	[Fact]
	public void LogViewer_AppliesMaxHeight()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.MaxHeight, "240px"));

		Assert.Equal("max-height: 240px", cut.Find(".moka-log-viewer__body").GetAttribute("style"));
	}

	[Theory]
	[MemberData(nameof(HostileValues))]
	public void Meter_DrawsASegmentWhoseColorIsNotAColorTransparent(string hostile)
	{
		IRenderedComponent<MokaMeter> cut = Render<MokaMeter>(p => p
			.Add(x => x.Value, 40)
			.Add(x => x.Segments, new[]
			{
				new MokaMeterSegment(0, 50, "#00e676"),
				new MokaMeterSegment(50, 100, hostile)
			}));

		IElement segments = cut.Find(".moka-meter__segments");
		Assert.Equal(["background"], Properties(segments));
		Assert.Equal("background: linear-gradient(to right, #00e676 0%, #00e676 50%, transparent 50%, transparent 100%)",
			segments.GetAttribute("style"));
	}

	[Theory]
	[MemberData(nameof(HostileValues))]
	public void Terminal_IgnoresALineColorThatIsNotAColor(string hostile)
	{
		IRenderedComponent<MokaTerminal> cut = Render<MokaTerminal>(p => p
			.Add(x => x.Lines, new[] { new MokaTerminalLine("ok", "#4ec9b0"), new MokaTerminalLine("bad", hostile) }));

		IReadOnlyList<IElement> texts = cut.FindAll(".moka-terminal-text");
		Assert.Equal("color: #4ec9b0", texts[0].GetAttribute("style"));
		Assert.Null(texts[1].GetAttribute("style"));
	}

	[Theory]
	[MemberData(nameof(HostileValues))]
	public void ThemeSwitcher_DrawsAThemeColorThatIsNotAColorEmpty(string hostile)
	{
		MokaTheme theme = MokaTheme.Light with { Palette = MokaPalette.Light with { Primary = hostile } };

		IRenderedComponent<MokaThemeSwitcher> cut = Render<MokaThemeSwitcher>(p => p
			.Add(x => x.Themes, new[] { new MokaThemeSwitcherItem("Brand", theme) })
			.Add(x => x.SelectedTheme, theme)
			.Add(x => x.ShowPreview, true));

		IReadOnlyList<IElement> swatches = cut.FindAll(".moka-theme-switcher__swatch");
		Assert.Null(swatches[0].GetAttribute("style"));
		Assert.Equal($"background-color: {MokaPalette.Light.Secondary}", swatches[1].GetAttribute("style"));
	}
}
