using Bunit;
using Moka.Red.Feedback.CommandPalette;

namespace Moka.Red.Feedback.Tests.Components.CommandPalette;

public class MokaCommandPaletteGroupTests : CommandPaletteTestBase
{
	[Fact]
	public async Task GroupOrder_PutsTheNamedGroupsFirst_AndTheRestByName()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("c", "C", "Charlie"), Command("a", "A", "Alpha"), Command("b", "B", "Bravo"), Command("d", "D", "Delta")],
			p => p.Add(x => x.GroupOrder, ["Delta", "Bravo"]));

		Assert.Equal(["Delta", "Bravo", "Alpha", "Charlie"], Headings(cut));
		Assert.Equal(["D", "B", "A", "C"], Titles(cut));
	}

	[Fact]
	public async Task GroupOrder_KeepsCommandsWithoutAGroupFirst()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("a", "A", "Alpha"), Command("free", "Free")],
			p => p.Add(x => x.GroupOrder, ["Alpha"]));

		Assert.Equal(["Free", "A"], Titles(cut));
	}

	[Fact]
	public async Task WithoutGroupOrder_GroupsKeepTheirOrderByName()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("b", "B", "Bravo"), Command("a", "A", "Alpha")]);

		Assert.Equal(["Alpha", "Bravo"], Headings(cut));
	}

	[Fact]
	public async Task RunningACommand_PutsItFirstInRecent()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "A"), Command("b", "B")]);

		await Press(cut, "Enter");
		await cut.InvokeAsync(Palette.Open);
		await Press(cut, "ArrowDown");
		await Press(cut, "Enter");
		await cut.InvokeAsync(Palette.Open);
		await Press(cut, "Enter");

		Assert.Equal(["a", "b"], Palette.Recent);
	}

	[Fact]
	public void SetRecent_KeepsARepeatedIdsFirstPlace_AndSaysWhenTheListChanged()
	{
		int changes = 0;
		Palette.RecentChanged += () => changes++;

		Palette.SetRecent(["b", "a", "b"]);
		Palette.SetRecent(["b", "a"]);

		Assert.Equal(["b", "a"], Palette.Recent);
		Assert.Equal(1, changes);
	}

	[Fact]
	public async Task RecentCommands_AreNotListed_UnlessShowRecentIsOn()
	{
		Palette.SetRecent(["b"]);
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "A"), Command("b", "B")]);

		Assert.Empty(Headings(cut));
		Assert.Equal(["A", "B"], Titles(cut));
	}

	[Fact]
	public async Task ShowRecent_ListsThemFirst_AndTakesThemOutOfTheirGroups()
	{
		Palette.SetRecent(["save", "home"]);
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("home", "Home", "Go"), Command("save", "Save", "Edit"), Command("undo", "Undo", "Edit")],
			p => p.Add(x => x.ShowRecent, true));

		Assert.Equal(["Recent", "Edit"], Headings(cut));
		Assert.Equal(["Save", "Home", "Undo"], Titles(cut));
		Assert.Equal("Save", Highlighted(cut));
	}

	[Fact]
	public async Task ShowRecent_SkipsCommandsThatAreGone_AndStopsAtRecentCount()
	{
		Palette.SetRecent(["gone", "c", "b", "a"]);
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("a", "A"), Command("b", "B"), Command("c", "C")],
			p => p.Add(x => x.ShowRecent, true).Add(x => x.RecentCount, 2));

		Assert.Equal(["C", "B", "A"], Titles(cut));
		Assert.Equal(["Recent"], Headings(cut));
		Assert.Equal(2, cut.FindAll(".moka-command-palette__section .moka-command-palette__item").Count);
	}

	[Fact]
	public async Task ShowRecent_StepsAsideWhileSomethingIsTyped()
	{
		Palette.SetRecent(["b"]);
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "Alpha"), Command("b", "Beta")], p => p.Add(x => x.ShowRecent, true));

		await Type(cut, "a");

		Assert.Empty(Headings(cut));
		Assert.Equal(["Alpha", "Beta"], Titles(cut));
	}

	[Fact]
	public async Task ShowRecent_FindsACommandOneLevelDown()
	{
		Palette.SetRecent(["dark"]);
		MokaCommand theme = new() { Id = "theme", Title = "Theme", Children = [Command("dark", "Dark")] };
		IRenderedComponent<MokaCommandPalette> cut = await Open([theme], p => p.Add(x => x.ShowRecent, true));

		Assert.Equal(["Dark", "Theme"], Titles(cut));
	}

	[Fact]
	public async Task ShowRecent_WithoutGroups_PutsThemFirstWithoutAHeading()
	{
		Palette.SetRecent(["b"]);
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("a", "A"), Command("b", "B")],
			p => p.Add(x => x.ShowRecent, true).Add(x => x.ShowGroups, false));

		Assert.Empty(Headings(cut));
		Assert.Equal(["B", "A"], Titles(cut));
	}

	[Fact]
	public async Task TheRecentHeading_ComesFromText()
	{
		Palette.SetRecent(["a"]);
		IRenderedComponent<MokaCommandPalette> cut = await Open(
			[Command("a", "A")],
			p => p.Add(x => x.ShowRecent, true).Add(x => x.Text, new MokaCommandPaletteText { Recent = "Zuletzt" }));

		Assert.Equal(["Zuletzt"], Headings(cut));
	}

	[Fact]
	public void AServiceWrittenBeforeRecent_StillCompiles_AndHasNone()
	{
		IMokaCommandPaletteService service = new MinimalService();

		service.SetRecent(["a"]);

		Assert.Empty(service.Recent);
	}

	private sealed class MinimalService : IMokaCommandPaletteService
	{
		public IReadOnlyList<MokaCommand> Commands => [];

		public bool IsOpen { get; set; }

		public event Action? OnToggle
		{
			add { }
			remove { }
		}

		public void Register(MokaCommand command)
		{
		}

		public void RegisterMany(IEnumerable<MokaCommand> commands)
		{
		}

		public void Unregister(string id)
		{
		}

		public void Open()
		{
		}

		public void Close()
		{
		}

		public void Toggle()
		{
		}
	}
}
