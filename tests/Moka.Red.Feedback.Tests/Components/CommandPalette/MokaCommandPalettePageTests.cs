using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.CommandPalette;

namespace Moka.Red.Feedback.Tests.Components.CommandPalette;

public class MokaCommandPalettePageTests : CommandPaletteTestBase
{
	private readonly List<string> _ran = [];

	private MokaCommand Theme => new()
	{
		Id = "theme",
		Title = "Change theme",
		OnExecuteSync = () => _ran.Add("theme"),
		Children = [Command("dark", "Dark", run: () => _ran.Add("dark")), Command("light", "Light", run: () => _ran.Add("light"))]
	};

	[Fact]
	public async Task PickingACommandWithChildren_OpensThem_AndRunsNothing()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Theme, Command("save", "Save")]);

		await Press(cut, "Enter");

		Assert.Equal(["Dark", "Light"], Titles(cut));
		Assert.Empty(_ran);
		Assert.True(Palette.IsOpen);
		Assert.Empty(Palette.Recent);
	}

	[Fact]
	public async Task AClick_OpensThemToo()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Theme]);

		await cut.Find("[role=option]").ClickAsync(new());

		Assert.Equal(["Dark", "Light"], Titles(cut));
	}

	[Fact]
	public async Task ThePage_ShowsTheCommandsTitle_AndTheSearchBoxPointsAtIt()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Theme]);
		Assert.Empty(cut.FindAll(".moka-command-palette__page"));
		Assert.False(cut.Find("input").HasAttribute("aria-describedby"));

		await Press(cut, "Enter");

		IElement page = cut.Find(".moka-command-palette__page");
		Assert.Equal("Change theme", page.TextContent);
		Assert.Equal(page.Id, cut.Find("input").GetAttribute("aria-describedby"));
	}

	[Fact]
	public async Task OpeningAPage_ClearsTheSearch_AndTypingSearchesOnlyThatPage()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Theme, Command("darken", "Darken image")]);
		await Type(cut, "change");

		await Press(cut, "Enter");
		Assert.Equal(string.Empty, cut.Find("input").GetAttribute("value"));

		await Type(cut, "dark");

		Assert.Equal(["Dark"], Titles(cut));
	}

	[Fact]
	public async Task PickingAChild_RunsIt_ClosesThePalette_AndRemembersIt()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Theme]);
		await Press(cut, "Enter");

		await Press(cut, "ArrowDown");
		await Press(cut, "Enter");

		Assert.Equal(["light"], _ran);
		Assert.False(Palette.IsOpen);
		Assert.Equal(["light"], Palette.Recent);
	}

	[Fact]
	public async Task Escape_GoesBackALevel_ThenCloses()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Theme, Command("save", "Save")]);
		await Press(cut, "Enter");

		await Press(cut, "Escape");

		Assert.True(Palette.IsOpen);
		Assert.Equal(["Change theme", "Save"], Titles(cut));
		Assert.Empty(cut.FindAll(".moka-command-palette__page"));

		await Press(cut, "Escape");

		Assert.False(Palette.IsOpen);
	}

	[Fact]
	public async Task Backspace_InAnEmptySearchBox_GoesBack()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Theme, Command("save", "Save")]);
		await Press(cut, "Enter");

		await Press(cut, "Backspace");

		Assert.Equal(["Change theme", "Save"], Titles(cut));
	}

	[Fact]
	public async Task Backspace_WithTextTyped_StaysOnThePage()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Theme]);
		await Press(cut, "Enter");
		await Type(cut, "d");

		await Press(cut, "Backspace");

		Assert.Equal("Change theme", cut.Find(".moka-command-palette__page").TextContent);
	}

	[Fact]
	public async Task GoingBack_HighlightsTheCommandItCameFrom()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("save", "Save"), Theme]);
		await Press(cut, "ArrowDown");
		await Press(cut, "Enter");

		await Press(cut, "Escape");

		Assert.Equal("Change theme", Highlighted(cut));
	}

	[Fact]
	public async Task OpeningAPage_IsAnnounced_AndGoingBackSaysThePalettesName()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Theme]);

		await Press(cut, "Enter");
		Assert.Equal("Change theme", Status(cut).TextContent);

		await Press(cut, "Escape");
		Assert.Equal("Command palette", Status(cut).TextContent);

		await Type(cut, "c");
		Assert.Equal(string.Empty, Status(cut).TextContent);
	}

	[Fact]
	public async Task TheAnnouncement_ComesFromText()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Theme],
			p => p.Add(x => x.Text, new MokaCommandPaletteText { PageOpened = title => $"In {title}" }));

		await Press(cut, "Enter");

		Assert.Equal("In Change theme", Status(cut).TextContent);
	}

	[Fact]
	public async Task OnAPage_TheFooterSaysEscapeGoesBack()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Theme]);
		Assert.EndsWith("Close", cut.FindAll(".moka-command-palette__footer > span")[2].TextContent.Trim(), StringComparison.Ordinal);

		await Press(cut, "Enter");

		Assert.EndsWith("Back", cut.FindAll(".moka-command-palette__footer > span")[2].TextContent.Trim(), StringComparison.Ordinal);
	}

	[Fact]
	public async Task OpeningThePaletteAgain_StartsAtTheTop()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Theme]);
		await Press(cut, "Enter");

		await cut.InvokeAsync(Palette.Close);
		await cut.InvokeAsync(Palette.Open);

		Assert.Equal(["Change theme"], Titles(cut));
	}

	[Fact]
	public async Task ACommandWithChildren_IsNeverALink_AndShowsItOpensMore()
	{
		MokaCommand docs = new()
		{
			Id = "docs",
			Title = "Docs",
			Href = "https://example.com",
			Target = "_blank",
			Children = [Command("api", "API")]
		};
		IRenderedComponent<MokaCommandPalette> cut = await Open([docs, Command("save", "Save")]);

		IElement row = cut.FindAll("[role=option]")[0];
		Assert.Equal("DIV", row.TagName);
		Assert.NotNull(row.QuerySelector(".moka-command-palette__item-more"));
		Assert.Null(cut.FindAll("[role=option]")[1].QuerySelector(".moka-command-palette__item-more"));
	}

	[Fact]
	public async Task ADisabledCommandWithChildren_DoesntOpen()
	{
		MokaCommand locked = new() { Id = "locked", Title = "Locked", Disabled = true, Children = [Command("a", "A")] };
		IRenderedComponent<MokaCommandPalette> cut = await Open([locked]);

		await Press(cut, "Enter");

		Assert.Equal(["Locked"], Titles(cut));
	}

	[Fact]
	public async Task AnEmptyPage_ShowsTheNoResultsText()
	{
		MokaCommand empty = new() { Id = "empty", Title = "Empty", Children = [] };
		IRenderedComponent<MokaCommandPalette> cut = await Open([empty]);

		await Press(cut, "Enter");

		Assert.Equal("No commands found", cut.Find(".moka-command-palette__empty").TextContent);
	}
}
