using AngleSharp.Dom;
using Bunit;
using Moka.Red.Navigation.Tabs;
using Moka.Red.Navigation.Tabs.Models;
using Moka.Red.Navigation.Tabs.Theming;

namespace Moka.Red.Navigation.Tests.Components;

// TabTheme built its style with its own StringBuilder, and the strip wrote a tab's ActiveColor and a
// group's Color into hand-built declarations, so a semicolon in any of them added declarations.
public class TabStyleTests : BunitContext
{
	private const string Hostile = "red; position: fixed; inset: 0; z-index: 9999";

	public TabStyleTests() => JSInterop.SetupModule("./_content/Moka.Red.Navigation/moka-tabs.js").Mode =
		JSRuntimeMode.Loose;

	[Fact]
	public void TabTheme_LeavesOutAValueThatCouldLeaveItsDeclaration()
	{
		var theme = new TabTheme
		{
			StripBackground = Hostile,
			ActiveTabColor = "#00e676",
			ContainerRadius = "8px\"",
			BadgeColor = "   "
		};

		Assert.Equal("--moka-tab-active-color: #00e676", theme.ToContainerStyle());
	}

	[Fact]
	public void TabTheme_WithNothingSet_HasNoStyle() => Assert.Null(new TabTheme().ToContainerStyle());

	[Fact]
	public void ATabsActiveColor_ThatIsNotAColor_IsIgnored()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = Render<MokaTabStrip<string>>(p => p
			.Add(x => x.Tabs, new List<TabInfo<string>> { new() { Id = "a", Title = "Alpha", ActiveColor = Hostile } })
			.Add(x => x.ActiveTabId, "a"));

		Assert.Null(Tab(cut, "a").GetAttribute("style"));
	}

	[Fact]
	public void ATabsActiveColor_IsApplied()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = Render<MokaTabStrip<string>>(p => p
			.Add(x => x.Tabs, new List<TabInfo<string>> { new() { Id = "a", Title = "Alpha", ActiveColor = "#42a5f5" } })
			.Add(x => x.ActiveTabId, "a"));

		Assert.Equal("--moka-tab-active-color: #42a5f5; --moka-tab-active-border-color: #42a5f5",
			Tab(cut, "a").GetAttribute("style"));
	}

	[Fact]
	public void AGroupColor_ThatIsNotAColor_FallsBackToTheGroupsOwnColor()
	{
		IRenderedComponent<MokaTabStrip<string>> cut = Render<MokaTabStrip<string>>(p => p
			.Add(x => x.Tabs, new List<TabInfo<string>> { new() { Id = "a", Title = "Alpha", GroupName = "docs" } })
			.Add(x => x.Groups, new[] { new TabGroupInfo { Name = "docs", Color = Hostile } })
			.Add(x => x.ActiveTabId, "a"));

		Assert.Equal(
			$"border-left: var(--moka-tab-group-border-width, 3px) solid {ColorHelper.GetDeterministicColor("docs")}",
			cut.Find(".moka-tab-group").GetAttribute("style"));
	}

	private static IElement Tab(IRenderedComponent<MokaTabStrip<string>> cut, string id) =>
		cut.Find($"[role=tab][data-tab-id='{id}']");
}
