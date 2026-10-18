using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Extensions;
using Moka.Red.Core.Hotkeys;
using Moka.Red.Feedback.Cheatsheet;

namespace Moka.Red.Feedback.Tests.Components;

// The cheatsheet listed whatever static Groups it was handed, so an app's shortcut list was written twice: once
// where the key was bound and once here, and the two drifted. Without Groups it reads the registry instead.
public class MokaCheatsheetFromHotkeysTests : BunitContext
{
	public MokaCheatsheetFromHotkeysTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaHotkeys();
	}

	private IMokaHotkeyService Hotkeys => Services.GetRequiredService<IMokaHotkeyService>();

	private IRenderedComponent<MokaCheatsheet> Open() =>
		Render<MokaCheatsheet>(p => p.Add(x => x.Open, true));

	[Fact]
	public void RegisteredHotkeysAreListed()
	{
		Hotkeys.Register(new MokaHotkey
		{
			Keys = "Ctrl+S",
			Description = "Save",
			Group = "File",
			OnInvokeSync = () => { }
		});

		IRenderedComponent<MokaCheatsheet> cut = Open();

		Assert.Equal("File", cut.Find(".moka-cheatsheet-group-title").TextContent.Trim());
		Assert.Equal("Save", cut.Find(".moka-cheatsheet-desc").TextContent.Trim());
		Assert.Equal(["Ctrl", "S"], cut.FindAll("kbd").Select(k => k.TextContent.Trim()));
	}

	// Groups keep the order they first appear in, and a hotkey without one goes under the general heading.
	[Fact]
	public void GroupsKeepTheirOrderAndTheUngroupedGetAHeading()
	{
		Hotkeys.Register(new MokaHotkey { Keys = "Ctrl+S", Description = "Save", Group = "File", OnInvokeSync = () => { } });
		Hotkeys.Register(new MokaHotkey { Keys = "Ctrl+K", Description = "Commands", OnInvokeSync = () => { } });
		Hotkeys.Register(new MokaHotkey { Keys = "Ctrl+O", Description = "Open", Group = "File", OnInvokeSync = () => { } });

		IRenderedComponent<MokaCheatsheet> cut = Open();

		Assert.Equal(
			["File", "General"],
			cut.FindAll(".moka-cheatsheet-group-title").Select(t => t.TextContent.Trim()));
		Assert.Equal(
			["Save", "Open", "Commands"],
			cut.FindAll(".moka-cheatsheet-desc").Select(d => d.TextContent.Trim()));
	}

	// A row with no words says nothing, so a hotkey that is bound but not described is left out of the list.
	[Fact]
	public void AHotkeyWithoutADescriptionIsNotListed()
	{
		Hotkeys.Register(new MokaHotkey { Keys = "Ctrl+S", OnInvokeSync = () => { } });

		Assert.Empty(Open().FindAll(".moka-cheatsheet-desc"));
	}

	// Groups still wins, so an app that wants its own list, or has no registry, is unaffected.
	[Fact]
	public void GroupsWinsOverTheRegistry()
	{
		Hotkeys.Register(new MokaHotkey { Keys = "Ctrl+S", Description = "Save", OnInvokeSync = () => { } });

		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Groups, [new MokaCheatsheetGroup("Mine", [new MokaCheatsheetItem("Something else", ["F1"])])]));

		Assert.Equal("Something else", cut.Find(".moka-cheatsheet-desc").TextContent.Trim());
	}

	[Fact]
	public void TheUngroupedHeadingCanBeRenamed()
	{
		Hotkeys.Register(new MokaHotkey { Keys = "Ctrl+K", Description = "Commands", OnInvokeSync = () => { } });

		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.UngroupedTitle, "Everything else"));

		Assert.Equal("Everything else", cut.Find(".moka-cheatsheet-group-title").TextContent.Trim());
	}

	// The conversion is public, for an app that wants the groups without opening a cheatsheet.
	[Fact]
	public void TheConversionIsAvailableOnItsOwn()
	{
		IReadOnlyList<MokaCheatsheetGroup> groups = MokaCheatsheet.FromRegistry(
			[new MokaHotkey { Keys = "Mod+Shift+P", Description = "Run a command", Group = "Go" }]);

		MokaCheatsheetGroup group = Assert.Single(groups);
		Assert.Equal("Go", group.Title);
		Assert.Equal(["Mod", "Shift", "P"], Assert.Single(group.Items).Keys);
	}
}

// Without the registry registered at all, a cheatsheet given no groups just has nothing to show.
public class MokaCheatsheetWithoutHotkeysTests : BunitContext
{
	public MokaCheatsheetWithoutHotkeysTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void NoRegistryIsNotAnError()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p.Add(x => x.Open, true));

		Assert.Empty(cut.FindAll(".moka-cheatsheet-desc"));
		Assert.NotNull(cut.Find(".moka-cheatsheet"));
	}
}
