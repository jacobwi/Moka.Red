using System.Numerics;
using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Appearance;

namespace Moka.Red.ThemeGen.Tests.Components;

// Dock layouts, tables and table rows are style rows: they sit with the others, reach the theme through its With calls,
// and draw what a table or layout draws now, including the defaults a theme leaves unset.
public sealed class AppearanceDockAndTableRowsTests : BunitContext
{
	private const MokaAppearanceRows Rows = MokaAppearanceRows.Docks | MokaAppearanceRows.Tables | MokaAppearanceRows.TableDensity;

	public AppearanceDockAndTableRowsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TheRows_AreStyleRows_NotDefaultOnes()
	{
		Assert.Equal(Rows, MokaAppearanceRows.Style & Rows);
		Assert.Equal(MokaAppearanceRows.None, MokaAppearanceRows.Default & Rows);
		Assert.Equal(3, BitOperations.PopCount((ulong)Rows));
	}

	[Fact]
	public void TheValue_ReachesTheTheme_OnTheNewDesign()
	{
		MokaAppearance value = new() { Docks = MokaDockLook.Cards, Tables = MokaTableLook.Sheet, TableDensity = MokaTableDensity.Standard };

		MokaTheme theme = value.ToTheme(isDark: true);

		Assert.Equal(MokaDockLook.Cards, theme.Docks);
		Assert.Equal(MokaTableLook.Sheet, theme.Tables);
		Assert.Equal(MokaTableDensity.Standard, theme.TableDensity);
	}

	[Fact]
	public void TheOriginalDesign_IgnoresThem()
	{
		MokaAppearance value = new() { Palette = MokaAppearancePalette.Original.Id, Docks = MokaDockLook.Ide, Tables = MokaTableLook.Cards };

		MokaTheme theme = value.ToTheme(isDark: true);

		Assert.Equal(MokaDockLook.Classic, theme.Docks);
		Assert.Null(theme.Tables);
	}

	[Fact]
	public void UnsetChoices_ShowWhatTablesAndLayoutsDraw()
	{
		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p.Add(x => x.Rows, Rows));

		Assert.Equal("Classic", Checked(cut, "Dock layouts"));
		Assert.Equal("Lined", Checked(cut, "Tables"));
		Assert.Equal("Compact", Checked(cut, "Table rows"));
	}

	// An airy table is comfortable unless a density is set, so the row says so.
	[Fact]
	public void AnAiryLook_ShowsComfortableRows_UntilADensityIsPicked()
	{
		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p
			.Add(x => x.Rows, Rows)
			.Add(x => x.Value, new MokaAppearance { Tables = MokaTableLook.Airy }));

		Assert.Equal("Comfortable", Checked(cut, "Table rows"));
	}

	[Fact]
	public void EachDockLook_HasAPicture_ThatNamesTheLook()
	{
		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p.Add(x => x.Rows, MokaAppearanceRows.Docks));

		Assert.Equal(
			["classic", "ide", "cards", "minimal", "terminal"],
			cut.FindAll(".moka-appearance-panel__docks").Select(art => art.ClassList.Single(name => name.StartsWith("moka-appearance-panel__docks--", StringComparison.Ordinal))["moka-appearance-panel__docks--".Length..]));
	}

	[Fact]
	public void EachTableLook_HasAPicture()
	{
		IRenderedComponent<MokaAppearancePanel> cut = Render<MokaAppearancePanel>(p => p.Add(x => x.Rows, MokaAppearanceRows.Tables));

		Assert.Equal(Enum.GetValues<MokaTableLook>().Length, cut.FindAll(".moka-appearance-panel__tables").Count);
	}

	private static string? Checked(IRenderedComponent<MokaAppearancePanel> cut, string group)
	{
		IElement? selected = cut.FindAll($"[role='radiogroup'][aria-label='{group}'] label")
			.SingleOrDefault(label => label.QuerySelector("input[type=radio]")!.HasAttribute("checked"));
		return selected is null ? null : (selected.QuerySelector(".moka-radio-item__name") ?? selected.QuerySelector(".moka-segment__text"))!.TextContent.Trim();
	}
}
