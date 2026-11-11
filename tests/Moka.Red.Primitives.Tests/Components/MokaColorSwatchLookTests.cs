using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Primitives.ColorSwatch;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// BC's board for the swatch: a Look, a Mark, a CustomLook and a NoneLook, each defaulting to today's markup.
public class MokaColorSwatchLookTests : BunitContext
{
	private static readonly string[] Palette = ["#ef5350", "#42a5f5", "#66bb6a"];

	private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
	{
		["#ef5350"] = "Red",
		["#42a5f5"] = "Blue",
		["#66bb6a"] = "Green"
	};

	public static TheoryData<MokaSwatchLook, string, string> LookClasses => new()
	{
		{ MokaSwatchLook.Ring, "moka-color-swatch--ring", "26px" },
		{ MokaSwatchLook.Tiles, "moka-color-swatch--tiles", "30px" },
		{ MokaSwatchLook.Chips, "moka-color-swatch--chips", "18px" },
		{ MokaSwatchLook.Strip, "moka-color-swatch--strip", "30px" },
		{ MokaSwatchLook.Menu, "moka-color-swatch--menu", "14px" },
		{ MokaSwatchLook.Terminal, "moka-color-swatch--terminal", "22px" }
	};

	private static IReadOnlyList<IElement> Radios(IRenderedComponent<MokaColorSwatch> cut) =>
		cut.FindAll("input.moka-color-swatch-input");

	private static string? NameOf(string color) => Names.GetValueOrDefault(color);

	private IRenderedComponent<MokaColorSwatch> Swatch(Action<ComponentParameterCollectionBuilder<MokaColorSwatch>>? more = null) =>
		Render<MokaColorSwatch>(p =>
		{
			p.Add(x => x.Colors, Palette).Add(x => x.ColorLabel, NameOf).Add(x => x.SelectedColor, "#42a5f5");
			more?.Invoke(p);
		});

	[Fact]
	public void TheDefaults_AreTodaysLook()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.ShowNone, true).Add(x => x.AllowCustom, true));

		Assert.Equal(MokaSwatchLook.Standard, cut.Instance.Look);
		Assert.Equal(MokaSwatchMark.Standard, cut.Instance.Mark);
		Assert.Equal(MokaSwatchCustomLook.Standard, cut.Instance.CustomLook);
		Assert.Equal(MokaSwatchNoneLook.Standard, cut.Instance.NoneLook);
		Assert.Equal("moka-color-swatch", cut.Find(".moka-color-swatch").ClassName);
		Assert.Equal("moka-color-swatch-grid", cut.Find(".moka-color-swatch-grid").ClassName);
		Assert.Equal("moka-color-swatch-item moka-color-swatch-item--circle moka-color-swatch-item--none",
			cut.FindAll(".moka-color-swatch-item")[0].ClassName);
		Assert.Equal("moka-color-swatch-item moka-color-swatch-item--circle moka-color-swatch-item--selected",
			cut.FindAll(".moka-color-swatch-item")[2].ClassName);
		Assert.Contains("width: 12px", cut.Find(".moka-color-swatch-check").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Single(cut.FindAll(".moka-color-swatch-add"));
		Assert.Empty(cut.FindAll(".moka-color-swatch-dot, .moka-color-swatch-name, .moka-color-swatch-end, .moka-color-swatch-hex"));
	}

	[Theory]
	[MemberData(nameof(LookClasses))]
	public void ALook_MarksTheRoot_AndHasASizeOfItsOwn(MokaSwatchLook look, string css, string size)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, look));

		IElement root = cut.Find(".moka-color-swatch");
		Assert.Contains("moka-color-swatch--look", root.ClassList);
		Assert.Contains(css, root.ClassList);
		Assert.Contains($"--moka-swatch-size: {size}", root.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("radiogroup", root.GetAttribute("role"));
		Assert.Equal(3, Radios(cut).Count);
		Assert.Single(Radios(cut).Select(radio => radio.GetAttribute("name")).Distinct());
	}

	[Theory]
	[InlineData(MokaSwatchLook.Ring, MokaSize.Xs, "18px")]
	[InlineData(MokaSwatchLook.Ring, MokaSize.Lg, "34px")]
	[InlineData(MokaSwatchLook.Tiles, MokaSize.Sm, "26px")]
	[InlineData(MokaSwatchLook.Chips, MokaSize.Xs, "14px")]
	[InlineData(MokaSwatchLook.Menu, MokaSize.Lg, "18px")]
	[InlineData(MokaSwatchLook.Terminal, MokaSize.Xs, "14px")]
	[InlineData(MokaSwatchLook.Standard, MokaSize.Lg, "32px")]
	public void Size_StepsFromTheLooksOwnSize(MokaSwatchLook look, MokaSize size, string expected)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, look).Add(x => x.Size, size));

		Assert.Contains($"--moka-swatch-size: {expected}", cut.Find(".moka-color-swatch").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void SwatchSize_StillWinsOverALook()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, MokaSwatchLook.Tiles).Add(x => x.SwatchSize, "2rem"));

		Assert.Contains("--moka-swatch-size: 2rem", cut.Find(".moka-color-swatch").GetAttribute("style"), StringComparison.Ordinal);
	}

	// The swatches that show only their color keep their names in aria-label and a tooltip under them.
	[Theory]
	[InlineData(MokaSwatchLook.Ring)]
	[InlineData(MokaSwatchLook.Tiles)]
	[InlineData(MokaSwatchLook.Strip)]
	[InlineData(MokaSwatchLook.Terminal)]
	public void AColorOnlyLook_NamesEachSwatchInATooltip(MokaSwatchLook look)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, look));

		IconTooltip.AssertNamed(Radios(cut)[1], "Blue", MokaTooltipPosition.Bottom);
		Assert.Empty(cut.FindAll(".moka-color-swatch-name"));
		Assert.DoesNotContain("moka-color-swatch--named", cut.Find(".moka-color-swatch").ClassList);
	}

	// Chips and the menu show each name, so the eye and a screen reader read the same word, and no tooltip repeats it.
	[Theory]
	[InlineData(MokaSwatchLook.Chips)]
	[InlineData(MokaSwatchLook.Menu)]
	public void ANamedLook_ShowsEachName_WithNoTooltip(MokaSwatchLook look)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, look));

		Assert.Contains("moka-color-swatch--named", cut.Find(".moka-color-swatch").ClassList);
		Assert.Empty(cut.FindAll(".moka-tooltip-trigger"));
		IReadOnlyList<IElement> items = cut.FindAll(".moka-color-swatch-item");
		Assert.Equal(["Red", "Blue", "Green"], items.Select(item => item.QuerySelector(".moka-color-swatch-name")!.TextContent));
		Assert.Equal(["Red", "Blue", "Green"], Radios(cut).Select(radio => radio.GetAttribute("aria-label")));
		Assert.All(items, item =>
		{
			Assert.Equal("true", item.QuerySelector(".moka-color-swatch-name")!.GetAttribute("aria-hidden"));
			Assert.Equal("true", item.QuerySelector(".moka-color-swatch-dot")!.GetAttribute("aria-hidden"));
		});
		Assert.Equal("--_swatch-color: #42a5f5", items[1].GetAttribute("style"));
	}

	[Fact]
	public void Chips_PutTheCheckInTheChosenDot()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, MokaSwatchLook.Chips));

		IElement chosen = cut.Find(".moka-color-swatch-item--selected");
		Assert.NotNull(chosen.QuerySelector(".moka-color-swatch-dot .moka-color-swatch-check"));
		Assert.Contains("width: 55%", chosen.QuerySelector(".moka-color-swatch-check")!.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Single(cut.FindAll(".moka-color-swatch-check"));
	}

	[Fact]
	public void Menu_MarksTheChosenRowWithATickAtItsEnd_AndSeparatesNoColor()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, MokaSwatchLook.Menu).Add(x => x.ShowNone, true));

		IElement chosen = cut.Find(".moka-color-swatch-item--selected");
		Assert.Contains("moka-color-swatch-tick", chosen.LastElementChild!.ClassList);
		Assert.Empty(cut.FindAll(".moka-color-swatch-check"));
		Assert.Single(cut.FindAll(".moka-color-swatch-tick"));

		IElement grid = cut.Find(".moka-color-swatch-grid");
		Assert.Contains("moka-color-swatch-item--none", grid.Children[0].ClassList);
		Assert.Equal("moka-color-swatch-separator", grid.Children[1].ClassName);
		Assert.Equal("true", grid.Children[1].GetAttribute("aria-hidden"));
		Assert.Equal("No color", grid.Children[0].QuerySelector(".moka-color-swatch-name")!.TextContent);
	}

	// A strip rounds only its two ends, so the swatches that start and end it say so.
	[Fact]
	public void Strip_MarksItsEnds_AndCountsNoColorAsTheStart()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, MokaSwatchLook.Strip));

		IReadOnlyList<IElement> items = cut.FindAll(".moka-color-swatch-item");
		Assert.Equal([true, false, false], items.Select(item => item.ClassList.Contains("moka-color-swatch-item--start")));
		Assert.Equal([false, false, true], items.Select(item => item.ClassList.Contains("moka-color-swatch-item--end")));

		cut.Render(p => p.Add(x => x.ShowNone, true));

		items = cut.FindAll(".moka-color-swatch-item");
		Assert.Contains("moka-color-swatch-item--none", items[0].ClassList);
		Assert.Equal([true, false, false, false], items.Select(item => item.ClassList.Contains("moka-color-swatch-item--start")));
	}

	[Fact]
	public void TheEnds_AreOnlyMarkedOnAStrip()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, MokaSwatchLook.Ring));

		Assert.Empty(cut.FindAll(".moka-color-swatch-item--start, .moka-color-swatch-item--end"));
	}

	[Fact]
	public async Task ALook_KeepsTheKeyboardModel_DeleteClears()
	{
		List<string?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p
			.Add(x => x.Look, MokaSwatchLook.Chips)
			.Add(x => x.AllowClear, true)
			.Add(x => x.SelectedColorChanged, color => changes.Add(color)));

		Assert.Throws<MissingEventHandlerException>(() => Radios(cut)[0].KeyDown(new KeyboardEventArgs { Key = "Delete" }));
		await Radios(cut)[1].KeyDownAsync(new KeyboardEventArgs { Key = "Delete" });

		Assert.Equal([null], changes);
		Assert.Empty(cut.FindAll("input.moka-color-swatch-input[checked]"));
	}

	[Fact]
	public void ALook_DisablesWithTheRest()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, MokaSwatchLook.Menu).Add(x => x.Disabled, true));

		Assert.All(Radios(cut), radio => Assert.True(radio.HasAttribute("disabled")));
		Assert.All(cut.FindAll(".moka-color-swatch-item"), item => Assert.Contains("moka-color-swatch-item--disabled", item.ClassList));
	}

	[Fact]
	public void ALook_WorksForLabels()
	{
		List<MokaLabelColor?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Labels, MokaLabelColors.Distinct)
			.Add(x => x.Look, MokaSwatchLook.Chips)
			.Add(x => x.SelectedLabelChanged, label => changes.Add(label)));

		Assert.Equal(MokaLabelColors.Distinct.Count, cut.FindAll(".moka-color-swatch-name").Count);
		Assert.Equal(MokaLabelColors.Name(MokaLabelColors.Distinct[0], null), cut.Find(".moka-color-swatch-name").TextContent);

		Radios(cut)[0].Change(true);

		Assert.Equal([MokaLabelColors.Distinct[0]], changes);
	}

	// Columns limits the swatches in a row. A group with no more than that is one row anyway, so its cap goes and a
	// custom entry can sit beside it.
	[Fact]
	public void AGroupThatFitsColumns_LosesItsCap_InAFlowingLook()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, MokaSwatchLook.Ring).Add(x => x.Columns, 3));

		Assert.Contains("moka-color-swatch-grid--one-row", cut.Find(".moka-color-swatch-grid").ClassList);

		cut.Render(p => p.Add(x => x.Columns, 2));

		Assert.DoesNotContain("moka-color-swatch-grid--one-row", cut.Find(".moka-color-swatch-grid").ClassList);

		cut.Render(p => p.Add(x => x.Columns, 8).Add(x => x.Look, MokaSwatchLook.Standard));

		Assert.Equal("moka-color-swatch-grid", cut.Find(".moka-color-swatch-grid").ClassName);
	}

	public static TheoryData<MokaSwatchMark, string, string?> Marks => new()
	{
		{ MokaSwatchMark.Check, "check", ".moka-color-swatch-check" },
		{ MokaSwatchMark.Ring, "ring", null },
		{ MokaSwatchMark.RingAndCheck, "ring-and-check", ".moka-color-swatch-check" },
		{ MokaSwatchMark.ColorRing, "color-ring", null },
		{ MokaSwatchMark.Dot, "dot", ".moka-color-swatch-dot-mark" },
		{ MokaSwatchMark.Badge, "badge", ".moka-color-swatch-badge" }
	};

	[Theory]
	[MemberData(nameof(Marks))]
	public void AMark_MarksTheRoot_AndDrawsWhatItNeeds(MokaSwatchMark mark, string css, string? drawn)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, MokaSwatchLook.Ring).Add(x => x.Mark, mark));

		IElement root = cut.Find(".moka-color-swatch");
		Assert.Contains($"moka-color-swatch--mark-{css}", root.ClassList);
		Assert.Contains("moka-color-swatch--marked", root.ClassList);

		string[] parts = [".moka-color-swatch-check", ".moka-color-swatch-dot-mark", ".moka-color-swatch-badge"];
		foreach (string part in parts)
		{
			IReadOnlyList<IElement> found = cut.FindAll(part);
			Assert.Equal(part == drawn ? 1 : 0, found.Count);
			Assert.All(found, element => Assert.NotNull(element.Closest(".moka-color-swatch-item--selected")));
		}
	}

	[Fact]
	public void TheBadge_HoldsACheck_HiddenFromScreenReaders()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Mark, MokaSwatchMark.Badge));

		IElement badge = cut.Find(".moka-color-swatch-badge");
		Assert.Equal("true", badge.GetAttribute("aria-hidden"));
		Assert.NotNull(badge.QuerySelector("svg"));
	}

	[Fact]
	public void TheLooksOwnMark_IsStandard_AndAddsNoMarkClass()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, MokaSwatchLook.Ring));

		Assert.DoesNotContain(cut.Find(".moka-color-swatch").ClassList, c => c.StartsWith("moka-color-swatch--mark", StringComparison.Ordinal));
		Assert.Single(cut.FindAll(".moka-color-swatch-item--selected .moka-color-swatch-check"));
	}

	[Theory]
	[InlineData(MokaSwatchLook.Chips)]
	[InlineData(MokaSwatchLook.Menu)]
	public void ANamedLook_IgnoresMark(MokaSwatchLook look)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.Look, look).Add(x => x.Mark, MokaSwatchMark.Badge));

		Assert.DoesNotContain("moka-color-swatch--marked", cut.Find(".moka-color-swatch").ClassList);
		Assert.Empty(cut.FindAll(".moka-color-swatch-badge"));
	}

	// The default look marks the chosen no color swatch with its border alone. The others give it the same mark as
	// any color, drawn in the text color.
	[Fact]
	public void NoColor_TakesTheMark_OutsideTheDefaultLook()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p.Add(x => x.Colors, Palette).Add(x => x.ShowNone, true));

		Assert.Empty(cut.FindAll(".moka-color-swatch-item--none .moka-color-swatch-check"));

		cut.Render(p => p.Add(x => x.Look, MokaSwatchLook.Ring));
		Assert.Single(cut.FindAll(".moka-color-swatch-item--none .moka-color-swatch-check"));

		cut.Render(p => p.Add(x => x.Mark, MokaSwatchMark.Dot));
		Assert.Single(cut.FindAll(".moka-color-swatch-item--none .moka-color-swatch-dot-mark"));
	}

	public static TheoryData<MokaSwatchNoneLook, string> NoneLooks => new()
	{
		{ MokaSwatchNoneLook.Chip, "moka-color-swatch-item--none-chip" },
		{ MokaSwatchNoneLook.Empty, "moka-color-swatch-item--none-empty" },
		{ MokaSwatchNoneLook.Checkerboard, "moka-color-swatch-item--none-checkerboard" }
	};

	[Theory]
	[MemberData(nameof(NoneLooks))]
	public void ANoneLook_MarksTheNoColorSwatch(MokaSwatchNoneLook none, string css)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.ShowNone, true).Add(x => x.NoneLook, none));

		IElement item = cut.FindAll(".moka-color-swatch-item")[0];
		Assert.Contains("moka-color-swatch-item--none", item.ClassList);
		Assert.Contains(css, item.ClassList);
	}

	// A pill that says None names itself with the same word, so it needs no tooltip.
	[Fact]
	public void TheNoneChip_SaysNone_AndIsNamedByIt()
	{
		List<string?> changes = [];
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p
			.Add(x => x.ShowNone, true)
			.Add(x => x.NoneLook, MokaSwatchNoneLook.Chip)
			.Add(x => x.SelectedColorChanged, color => changes.Add(color)));

		IElement chip = cut.Find(".moka-color-swatch-item--none-chip");
		Assert.Equal("None", chip.QuerySelector(".moka-color-swatch-name")!.TextContent);
		Assert.Equal("None", chip.QuerySelector("input")!.GetAttribute("aria-label"));
		Assert.Null(chip.Closest(".moka-tooltip-trigger"));
		Assert.Null(chip.QuerySelector(".moka-color-swatch-dot"));
		Assert.Contains("moka-color-swatch-grid--flow", cut.Find(".moka-color-swatch-grid").ClassList);

		chip.QuerySelector("input")!.Change(true);

		Assert.Equal([null], changes);
	}

	[Fact]
	public void TheNoneChip_TakesItsWordFromText()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p
			.Add(x => x.ShowNone, true)
			.Add(x => x.NoneLook, MokaSwatchNoneLook.Chip)
			.Add(x => x.Text, new MokaColorSwatchText { NoneChip = "Aucune" }));

		Assert.Equal("Aucune", cut.Find(".moka-color-swatch-item--none-chip input").GetAttribute("aria-label"));
	}

	[Theory]
	[InlineData(MokaSwatchNoneLook.Empty)]
	[InlineData(MokaSwatchNoneLook.Checkerboard)]
	public void ACircleNoneLook_KeepsItsTooltip(MokaSwatchNoneLook none)
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p.Add(x => x.ShowNone, true).Add(x => x.NoneLook, none));

		IconTooltip.AssertNamed(Radios(cut)[0], "No color", MokaTooltipPosition.Bottom);
	}

	[Fact]
	public void InANamedLook_NoColorHasADotDrawnByItsLook()
	{
		IRenderedComponent<MokaColorSwatch> cut = Swatch(p => p
			.Add(x => x.Look, MokaSwatchLook.Chips)
			.Add(x => x.ShowNone, true)
			.Add(x => x.NoneLook, MokaSwatchNoneLook.Checkerboard));

		IElement none = cut.FindAll(".moka-color-swatch-item")[0];
		Assert.Contains("moka-color-swatch-item--none-checkerboard", none.ClassList);
		Assert.NotNull(none.QuerySelector(".moka-color-swatch-dot"));
		Assert.Equal("No color", none.QuerySelector(".moka-color-swatch-name")!.TextContent);
	}

	[Fact]
	public void EndContent_SitsAfterTheSwatches_InTheLastGroup()
	{
		IRenderedComponent<MokaColorSwatch> cut = Render<MokaColorSwatch>(p => p
			.Add(x => x.Groups, [new MokaSwatchGroup("Theme", ["#ef5350"]), new MokaSwatchGroup("Recent", ["#42a5f5"])])
			.Add(x => x.EndContent, (RenderFragment)(b => b.AddMarkupContent(0, "<button type=\"button\" class=\"app-button\">More</button>"))));

		IReadOnlyList<IElement> grids = cut.FindAll(".moka-color-swatch-grid");
		Assert.Null(grids[0].QuerySelector(".moka-color-swatch-end"));
		IElement end = grids[1].QuerySelector(".moka-color-swatch-end")!;
		Assert.Equal(grids[1].LastElementChild, end);
		Assert.NotNull(end.QuerySelector(".app-button"));
		Assert.All(grids, grid => Assert.Contains("moka-color-swatch-grid--flow", grid.ClassList));
	}
}
