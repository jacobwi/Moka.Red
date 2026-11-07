using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Navigation.Breadcrumb;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// Each level used to work out whether it was last while it rendered, and a level with only string
// parameters never rendered again. A level added at runtime left the old last one bold, current and
// without a separator, a removed one left a link with a separator after it, and MaxItems="1" hid the
// current page behind "Home / ... /". The old tests rendered a single level, so none of it showed.
public class MokaBreadcrumbLevelsTests : BunitContext
{
	public MokaBreadcrumbLevelsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ALevelAddedLater_BecomesTheOnlyCurrentPage()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p.Add(x => x.Levels, ["Home", "Network"]));

		cut.Render(p => p.Add(x => x.Levels, ["Home", "Network", "Devices"]));

		Assert.Equal("Home > Network > Devices*", Shape(cut));
		Assert.Equal("Devices", Assert.Single(cut.FindAll("[aria-current]")).TextContent.Trim());
		Assert.Equal("/Network", cut.Find("a[href='/Network']").GetAttribute("href"));
	}

	[Fact]
	public void ALevelRemovedLater_LeavesNoSeparatorBehind()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p.Add(x => x.Levels, ["Home", "Network", "Devices"]));

		cut.Render(p => p.Add(x => x.Levels, ["Home", "Network"]));

		Assert.Equal("Home > Network*", Shape(cut));
		Assert.Empty(cut.FindAll("a[href='/Network']"));
	}

	[Fact]
	public void LevelsAddedAndRemovedInTurn_StayInStep()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p.Add(x => x.Levels, ["Home"]));

		cut.Render(p => p.Add(x => x.Levels, ["Home", "Network"]));
		cut.Render(p => p.Add(x => x.Levels, ["Home", "Network", "Devices"]));
		cut.Render(p => p.Add(x => x.Levels, ["Home", "Network"]));
		cut.Render(p => p.Add(x => x.Levels, ["Home", "Network", "Wired"]));

		Assert.Equal("Home > Network > Wired*", Shape(cut));
	}

	// Under @key a level inserted in the middle is a new component that registers last, while its
	// markup sits in the middle. The trail creates its levels again so they count in markup order.
	[Fact]
	public void AKeyedLevelInsertedInTheMiddle_CountsWhereItSits()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.Keyed, true)
			.Add(x => x.Levels, ["Home", "Devices"]));

		cut.Render(p => p.Add(x => x.Levels, ["Home", "Network", "Devices"]));

		Assert.Equal("Home > Network > Devices*", Shape(cut));
	}

	[Fact]
	public void AKeyedLevelRemovedFromTheMiddle_LeavesTheRestInOrder()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.Keyed, true)
			.Add(x => x.Levels, ["Home", "Network", "Devices"]));

		cut.Render(p => p.Add(x => x.Levels, ["Home", "Devices"]));

		Assert.Equal("Home > Devices*", Shape(cut));
	}

	[Fact]
	public void MaxItemsOne_KeepsTheCurrentPageAndFoldsTheRest()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 1)
			.Add(x => x.Levels, ["Home", "Network", "Devices", "Desktop"]));

		Assert.Equal("... > Desktop*", Shape(cut));
		string[] folded = ["Home", "Network", "Devices"];
		Assert.Equal(folded, MenuTexts(cut));
	}

	[Fact]
	public void MaxItemsTwo_KeepsTheFirstAndTheCurrentPage()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 2)
			.Add(x => x.Levels, ["Home", "Network", "Devices", "Desktop"]));

		Assert.Equal("Home > ... > Desktop*", Shape(cut));
		string[] folded = ["Network", "Devices"];
		Assert.Equal(folded, MenuTexts(cut));
	}

	[Fact]
	public void MaxItemsThree_KeepsTheFirstAndTheLastTwo()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 3)
			.Add(x => x.Levels, ["Home", "Network", "Devices", "Desktop"]));

		Assert.Equal("Home > ... > Devices > Desktop*", Shape(cut));
		string[] folded = ["Network"];
		Assert.Equal(folded, MenuTexts(cut));
	}

	[Fact]
	public void MaxItems_FoldsNothingWhileTheTrailFits()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 3)
			.Add(x => x.Levels, ["Home", "Network", "Devices"]));

		Assert.Equal("Home > Network > Devices*", Shape(cut));
	}

	[Fact]
	public void MaxItems_RefoldsALevelAddedLater()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 3)
			.Add(x => x.Levels, ["Home", "Network", "Devices"]));

		cut.Render(p => p.Add(x => x.Levels, ["Home", "Network", "Devices", "Desktop"]));

		Assert.Equal("Home > ... > Devices > Desktop*", Shape(cut));
	}

	[Fact]
	public void MaxItems_ChangedLater_RedrawsEveryLevel()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 3)
			.Add(x => x.Levels, ["Home", "Network", "Devices", "Desktop"]));

		cut.Render(p => p.Add(x => x.MaxItems, 1));
		Assert.Equal("... > Desktop*", Shape(cut));

		cut.Render(p => p.Add(x => x.MaxItems, null));
		Assert.Equal("Home > Network > Devices > Desktop*", Shape(cut));
	}

	[Fact]
	public void TheFoldButton_IsNamedByTheNumberOfLevelsItHolds()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 2)
			.Add(x => x.Levels, ["Home", "Network", "Devices", "Desktop"]));

		IconTooltip.AssertNamed(cut.Find(".moka-breadcrumb__more"), "Show 2 more levels", MokaTooltipPosition.Bottom);
		Assert.Equal("button", cut.Find(".moka-breadcrumb__more").GetAttribute("type"));

		cut.Render(p => p.Add(x => x.MaxItems, 3));
		Assert.Equal("Show 1 more level", cut.Find(".moka-breadcrumb__more").GetAttribute("aria-label"));
	}

	[Fact]
	public async Task AFoldedLevel_InTheMenu_GoesToItsPage()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 2)
			.Add(x => x.Levels, ["Home", "Network", "Devices", "Desktop"]));

		await cut.Find(".moka-breadcrumb__fold .moka-popover-trigger").ClickAsync(new MouseEventArgs());
		await cut.FindAll(".moka-breadcrumb__fold-item")[1].ClickAsync(new MouseEventArgs());

		Assert.EndsWith("/Devices", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
		Assert.Empty(cut.FindAll(".moka-breadcrumb__fold-item"));
	}

	[Fact]
	public async Task TheMenu_StepsEachFoldedLevelIn()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 2)
			.Add(x => x.Levels, ["Home", "Network", "Wired", "Desktop"]));

		await cut.Find(".moka-breadcrumb__fold .moka-popover-trigger").ClickAsync(new MouseEventArgs());

		IReadOnlyList<IElement> rows = cut.FindAll(".moka-breadcrumb__fold-item");
		Assert.Equal("--_breadcrumb-depth: 0", rows[0].GetAttribute("style"));
		Assert.Equal("--_breadcrumb-depth: 1", rows[1].GetAttribute("style"));
	}

	// The tooltip would sit where the menu opens.
	[Fact]
	public async Task TheFoldButtonsTooltip_GoesWhileTheMenuIsOpen()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 2)
			.Add(x => x.Levels, ["Home", "Network", "Wired", "Desktop"]));

		await cut.Find(".moka-breadcrumb__fold .moka-popover-trigger").ClickAsync(new MouseEventArgs());

		IElement trigger = cut.Find(".moka-breadcrumb__more").Closest(".moka-tooltip-trigger")!;
		Assert.DoesNotContain(trigger.Children, child => child.ClassList.Contains("moka-tooltip-popup"));
		Assert.Contains("moka-breadcrumb__more--open", cut.Find(".moka-breadcrumb__more").ClassList);
		Assert.Equal("Show 2 more levels", cut.Find(".moka-breadcrumb__more").GetAttribute("aria-label"));
	}

	[Fact]
	public void ALevelWithoutALink_IsDisabledInTheMenu()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 2)
			.Add(x => x.Unlinked, "Network")
			.Add(x => x.Levels, ["Home", "Network", "Devices", "Desktop"]));

		cut.Find(".moka-breadcrumb__fold .moka-popover-trigger").Click();

		IReadOnlyList<IElement> rows = cut.FindAll(".moka-breadcrumb__fold-item");
		Assert.Equal("true", rows[0].GetAttribute("aria-disabled"));
		Assert.Null(rows[1].GetAttribute("aria-disabled"));
	}

	[Fact]
	public void ShowCurrentOff_DropsTheCurrentPageAndItsSeparator()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.ShowCurrent, false)
			.Add(x => x.Levels, ["Home", "Network", "Devices"]));

		Assert.Equal("Home > Network", Shape(cut));
		Assert.Empty(cut.FindAll("[aria-current]"));
	}

	[Fact]
	public void ShowCurrentOff_StillCountsTheCurrentPageForMaxItems()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.ShowCurrent, false)
			.Add(x => x.MaxItems, 3)
			.Add(x => x.Levels, ["Home", "Network", "Wired", "Devices", "Desktop"]));

		Assert.Equal("Home > ... > Devices", Shape(cut));
	}

	[Fact]
	public void FoldNarrow_MarksWhatHidesAndWhatShowsInANarrowBox()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.Fold, MokaBreadcrumbFold.Narrow)
			.Add(x => x.Levels, ["Home", "Network", "Wired", "Devices", "Desktop"]));

		Assert.Contains("moka-breadcrumb--fold-narrow", cut.Find("nav").ClassList);
		Assert.Equal("Home > Network(w) >(w) Wired(w) >(w) ...(n) >(n) Devices > Desktop*", Shape(cut));
		Assert.Equal("Show 2 more levels", cut.Find(".moka-breadcrumb__more").GetAttribute("aria-label"));
	}

	[Fact]
	public void FoldMedium_HasItsOwnClass()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.Fold, MokaBreadcrumbFold.Medium)
			.Add(x => x.Levels, ["Home", "Network", "Wired", "Devices", "Desktop"]));

		Assert.Contains("moka-breadcrumb--fold-medium", cut.Find("nav").ClassList);
		Assert.DoesNotContain("moka-breadcrumb--fold-narrow", cut.Find("nav").ClassList);
	}

	[Fact]
	public void FoldWithMaxItems_HasAButtonForEachWidth()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.Fold, MokaBreadcrumbFold.Narrow)
			.Add(x => x.MaxItems, 4)
			.Add(x => x.Levels, ["Home", "Network", "Lan", "Wired", "Devices", "Desktop"]));

		Assert.Equal("Home > ...(w) >(w) Wired(w) >(w) ...(n) >(n) Devices > Desktop*", Shape(cut));
		IReadOnlyList<IElement> buttons = cut.FindAll(".moka-breadcrumb__more");
		Assert.Equal("Show 2 more levels", buttons[0].GetAttribute("aria-label"));
		Assert.Equal("Show 3 more levels", buttons[1].GetAttribute("aria-label"));
	}

	[Fact]
	public void FoldNever_IsTheDefault_AndMarksNothing()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p.Add(x => x.Levels, ["Home", "Network", "Wired", "Devices", "Desktop"]));

		Assert.Equal("Home > Network > Wired > Devices > Desktop*", Shape(cut));
		Assert.DoesNotContain(cut.Find("nav").ClassList, c => c.StartsWith("moka-breadcrumb--fold", StringComparison.Ordinal));
	}

	[Fact]
	public void TheBackLink_GoesToTheCurrentPagesParent()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p.Add(x => x.Levels, ["Home", "Network", "Devices"]));

		IElement back = cut.Find("nav > a.moka-breadcrumb__back");
		Assert.Equal("/Network", back.GetAttribute("href"));
		Assert.Equal("Network", back.TextContent.Trim());
		Assert.Contains("moka-breadcrumb--has-back", cut.Find("nav").ClassList);
	}

	[Fact]
	public void TheBackLink_FollowsALevelAddedLater()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p.Add(x => x.Levels, ["Home", "Network"]));

		cut.Render(p => p.Add(x => x.Levels, ["Home", "Network", "Devices"]));

		Assert.Equal("/Network", cut.Find(".moka-breadcrumb__back").GetAttribute("href"));
	}

	// The trail renders before a level hears its new text, so the level has to tell it.
	[Fact]
	public void TheBackLink_FollowsALevelWhoseTextChanges()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p.Add(x => x.Levels, ["Home", "Network", "Devices"]));

		cut.Render(p => p.Add(x => x.Levels, ["Home", "Wired", "Devices"]));

		Assert.Equal("Wired", cut.Find(".moka-breadcrumb__back").TextContent.Trim());
		Assert.Equal("/Wired", cut.Find(".moka-breadcrumb__back").GetAttribute("href"));
	}

	[Fact]
	public void AParentWithoutALink_GivesNoBackLink()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.Unlinked, "Network")
			.Add(x => x.Levels, ["Home", "Network", "Devices"]));

		Assert.Empty(cut.FindAll(".moka-breadcrumb__back"));
		Assert.DoesNotContain("moka-breadcrumb--has-back", cut.Find("nav").ClassList);
	}

	[Fact]
	public void DisposingTheTrail_WithLevelsInIt_Throws_Nothing()
	{
		IRenderedComponent<Trail> cut = Render<Trail>(p => p
			.Add(x => x.MaxItems, 2)
			.Add(x => x.Levels, ["Home", "Network", "Devices", "Desktop"]));

		cut.Render(p => p.Add(x => x.Levels, Array.Empty<string>()));

		Assert.Empty(cut.FindAll("ol > li"));
	}

	// Separators read ">", a folded run "...", the current page ends in "*", and a part that shows only
	// in a wide or a narrow box ends in "(w)" or "(n)".
	private static string Shape(IRenderedComponent<Trail> cut) => string.Join(" ", cut.FindAll("ol.moka-breadcrumb__list > li").Select(li =>
	{
		string part = li.ClassList.Contains("moka-breadcrumb-item__separator") ? ">"
			: li.ClassList.Contains("moka-breadcrumb__fold") ? "..."
			: li.TextContent.Trim() + (li.QuerySelector("[aria-current='page']") is null ? "" : "*");
		return part + (li.ClassList.Contains("moka-breadcrumb__wide") ? "(w)" : li.ClassList.Contains("moka-breadcrumb__narrow") ? "(n)" : "");
	}));

	private static List<string> MenuTexts(IRenderedComponent<Trail> cut)
	{
		cut.Find(".moka-breadcrumb__fold .moka-popover-trigger").Click();
		return [.. cut.FindAll(".moka-breadcrumb__fold-item").Select(row => row.TextContent.Trim())];
	}

	private sealed class Trail : ComponentBase
	{
		[Parameter] public IReadOnlyList<string> Levels { get; set; } = [];

		[Parameter] public bool Keyed { get; set; }

		[Parameter] public int? MaxItems { get; set; }

		[Parameter] public bool ShowCurrent { get; set; } = true;

		[Parameter] public MokaBreadcrumbFold Fold { get; set; }

		[Parameter] public string? Unlinked { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaBreadcrumb>(0);
			builder.AddComponentParameter(1, nameof(MokaBreadcrumb.MaxItems), MaxItems);
			builder.AddComponentParameter(2, nameof(MokaBreadcrumb.ShowCurrent), ShowCurrent);
			builder.AddComponentParameter(3, nameof(MokaBreadcrumb.Fold), Fold);
			builder.AddComponentParameter(4, nameof(MokaBreadcrumb.ChildContent), (RenderFragment)Items);
			builder.CloseComponent();
		}

		private void Items(RenderTreeBuilder builder)
		{
			foreach (string level in Levels)
			{
				builder.OpenComponent<MokaBreadcrumbItem>(0);
				if (Keyed)
				{
					builder.SetKey(level);
				}

				builder.AddComponentParameter(1, nameof(MokaBreadcrumbItem.Text), level);
				if (level != Unlinked)
				{
					builder.AddComponentParameter(2, nameof(MokaBreadcrumbItem.Href), $"/{level}");
				}

				builder.CloseComponent();
			}
		}
	}
}
