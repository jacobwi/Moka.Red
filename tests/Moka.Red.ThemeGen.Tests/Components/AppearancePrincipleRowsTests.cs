using System.Numerics;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Appearance;

namespace Moka.Red.ThemeGen.Tests.Components;

// The rows for the looks picked from a design audit: four style rows, three component rows and a third option in
// the current page row, and the Ember palette that has every pick chosen.
public sealed partial class MokaAppearancePanelTests
{
	private const MokaAppearanceRows PrincipleStyleRows =
		MokaAppearanceRows.Elevation | MokaAppearanceRows.TextButtons | MokaAppearanceRows.TitleSize | MokaAppearanceRows.NumberFont;

	private const MokaAppearanceRows PrincipleComponentRows =
		MokaAppearanceRows.Callouts | MokaAppearanceRows.Switches | MokaAppearanceRows.SettingsHeaders;

	[Fact]
	public void ThePrincipleRows_AreInStyleAndComponents_NotDefault()
	{
		Assert.Equal(PrincipleStyleRows, MokaAppearanceRows.Style & PrincipleStyleRows);
		Assert.Equal(PrincipleComponentRows, MokaAppearanceRows.Components & PrincipleComponentRows);
		Assert.Equal(MokaAppearanceRows.None, MokaAppearanceRows.Default & (PrincipleStyleRows | PrincipleComponentRows));
		Assert.Equal(MokaAppearanceRows.None, MokaAppearanceRows.Style & PrincipleComponentRows);
		Assert.Equal(15, BitOperations.PopCount((ulong)MokaAppearanceRows.Style));
	}

	[Fact]
	public void EveryRowFlag_IsItsOwnBit()
	{
		MokaAppearanceRows[] single = [.. Enum.GetValues<MokaAppearanceRows>().Where(row => BitOperations.PopCount((ulong)row) == 1)];

		Assert.Equal(single.Length, single.Distinct().Count());
		Assert.Equal(37, single.Length);
		Assert.Equal((ulong)MokaAppearanceRows.All, single.Aggregate(0UL, (all, row) => all | (ulong)row));
	}

	[Theory]
	[InlineData("Depth", "Shadow", "Glow")]
	[InlineData("Text buttons", "Accent", "Neutral")]
	[InlineData("Page titles", "Display", "Compact")]
	[InlineData("Numbers", "Title font", "Mono")]
	[InlineData("Current page", "Ring", "Solid inset")]
	[InlineData("Callouts", "Filled", "Edge")]
	[InlineData("Switches", "Solid", "Tinted")]
	[InlineData("Settings headers", "Solid", "Tinted")]
	public void EachRow_ShowsTodayOnRed_AndThePickOnEmber(string group, string today, string picked)
	{
		IRenderedComponent<MokaAppearancePanel> red = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.All));
		IRenderedComponent<MokaAppearancePanel> ember = RenderPanel(p => p
			.Add(x => x.Rows, MokaAppearanceRows.All)
			.Add(x => x.Value, new MokaAppearance { Palette = "ember" }));

		Assert.Equal(today, CheckedOption(red, group));
		Assert.Equal(picked, CheckedOption(ember, group));
	}

	[Fact]
	public void TheCurrentPageRow_OffersTheInsetSolid()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p.Add(x => x.Rows, MokaAppearanceRows.CurrentPlace));

		Assert.Equal(["Ring", "Solid", "Solid inset"], OptionsIn(cut, "Current page"));
	}

	[Fact]
	public void ThePrincipleRows_HideOnTheOriginalDesign()
	{
		IRenderedComponent<MokaAppearancePanel> cut = RenderPanel(p => p
			.Add(x => x.Rows, PrincipleStyleRows | PrincipleComponentRows)
			.Add(x => x.Value, new MokaAppearance { Palette = MokaAppearancePalette.Original.Id }));

		Assert.Empty(GroupNames(cut));
	}
}
