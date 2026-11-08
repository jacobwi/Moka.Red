using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Forms.SettingsForm;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// Cards draws the header and every group as a card of its own, with the groups in a grid the form's own width decides.
// The cards are CSS, so the rules are read as text; the markup they need is checked here.
public class MokaSettingsFormCardsTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Forms/SettingsForm/MokaSettingsForm.razor.js";

	private static readonly TimeSpan Plenty = TimeSpan.FromSeconds(10);

	private readonly BunitJSModuleInterop _module;

	public MokaSettingsFormCardsTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	public void Plain_KeepsItsOneBox_AndPutsNoGroupsInAGrid()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(MokaSettingsAppearance.Plain);

		Assert.DoesNotContain("moka-settings-form--cards", cut.Find(".moka-settings-form").ClassList);
		Assert.Empty(cut.FindAll(".moka-settings-form__groups"));
		Assert.Empty(cut.FindAll(".moka-settings-form__title-row"));
		Assert.Equal(2, cut.FindAll(".moka-settings-group").Count);
	}

	[Fact]
	public void Cards_PutsEveryGroupInTheGrid()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(MokaSettingsAppearance.Cards);

		Assert.Contains("moka-settings-form--cards", cut.Find(".moka-settings-form").ClassList);
		IElement grid = cut.Find(".moka-settings-form__groups");
		Assert.Equal(["location", "limits"], grid.QuerySelectorAll(".moka-settings-group").Select(group => group.Id));
	}

	[Fact]
	public void Cards_DrawEachGroupAsACard_TwoToARowWhenTheFormIsWide()
	{
		IReadOnlyDictionary<string, string> card = ScopedCss.Declarations("MokaSettingsForm.razor.css",
			".moka-settings-form__groups ::deep .moka-settings-group");

		Assert.Equal("1px solid var(--moka-color-outline)", card["border"]);
		Assert.Equal("var(--moka-radius-card)", card["border-radius"]);
		Assert.Equal("var(--moka-color-surface)", card["background"]);
		Assert.Equal("repeat(2, minmax(0, 1fr))", ScopedCss.Declarations("MokaSettingsForm.razor.css", ".moka-settings-form__groups")["grid-template-columns"]);
		Assert.Equal("moka-settings-form / inline-size",
			ScopedCss.Declarations("MokaSettingsForm.razor.css", ".moka-settings-form--cards > .moka-settings-form__body")["container"]);
	}

	[Fact]
	public void AFullGroup_TakesTheWholeRow()
	{
		IRenderedComponent<MokaSettingsGroup> cut = Render<MokaSettingsGroup>(p => p
			.Add(x => x.Title, "Cleanup")
			.Add(x => x.Span, MokaSettingsSpan.Full));

		Assert.Contains("moka-settings-group--full", cut.Find(".moka-settings-group").ClassList);
		Assert.Equal("1 / -1", ScopedCss.Declarations("MokaSettingsForm.razor.css",
			".moka-settings-form__groups ::deep .moka-settings-group--full")["grid-column"]);
	}

	[Fact]
	public void OtherContent_CanTakeTheWholeRow()
	{
		IRenderedComponent<MokaSettingsForm> cut = Render<MokaSettingsForm>(p => p
			.Add(x => x.Title, "Storage")
			.Add(x => x.Appearance, MokaSettingsAppearance.Cards)
			.AddChildContent("<div class=\"moka-settings-full home\">Everything</div>"));

		Assert.NotNull(cut.Find(".moka-settings-form__groups").QuerySelector(".moka-settings-full.home"));
		Assert.Equal("1 / -1", ScopedCss.Declarations("MokaSettingsForm.razor.css",
			".moka-settings-form__groups ::deep .moka-settings-full")["grid-column"]);
	}

	[Fact]
	public void AGroup_TakesOneColumn_ByDefault()
	{
		IRenderedComponent<MokaSettingsGroup> cut = Render<MokaSettingsGroup>(p => p.Add(x => x.Title, "Location"));

		Assert.DoesNotContain("moka-settings-group--full", cut.Find(".moka-settings-group").ClassList);
	}

	// A hidden advanced group renders nothing, so the grid gets no empty card.
	[Fact]
	public void AHiddenAdvancedGroup_LeavesNoCard()
	{
		IRenderedComponent<MokaSettingsForm> cut = Render<MokaSettingsForm>(p => p
			.Add(x => x.Title, "Storage")
			.Add(x => x.Appearance, MokaSettingsAppearance.Cards)
			.AddChildContent(Group("location", "Location"))
			.AddChildContent(Group("cache", "Cache", advanced: true)));

		Assert.Single(cut.Find(".moka-settings-form__groups").Children);
		Assert.Empty(cut.FindAll("#cache"));
	}

	[Fact]
	public void TheHeader_HasTheIconTile_TheStatusBesideTheTitle_AndTheActionsAtTheEnd()
	{
		IRenderedComponent<MokaSettingsForm> cut = Render<MokaSettingsForm>(p => p
			.Add(x => x.Title, "Storage")
			.Add(x => x.Appearance, MokaSettingsAppearance.Cards)
			.Add(x => x.Icon, MokaIcons.File.Database)
			.Add(x => x.Status, "<span class=\"status\">Healthy</span>")
			.Add(x => x.HeaderActions, "<button class=\"mode\">Detailed</button>"));

		IElement head = cut.Find(".moka-settings-form__head");
		Assert.Equal("true", head.QuerySelector(".moka-settings-form__icon")?.GetAttribute("aria-hidden"));
		Assert.NotNull(head.QuerySelector(".moka-settings-form__title-row .status"));
		Assert.Same(head.LastElementChild, head.QuerySelector(".moka-settings-form__actions"));
		Assert.NotNull(head.QuerySelector(".moka-settings-form__actions .mode"));
	}

	[Fact]
	public void WithoutAnIconOrActions_TheHeaderHasNeither()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(MokaSettingsAppearance.Cards);

		Assert.Empty(cut.FindAll(".moka-settings-form__icon"));
		Assert.Empty(cut.FindAll(".moka-settings-form__actions"));
	}

	// The group a link names is still there, inside its card, so the link lands on it.
	[Fact]
	public void ALinkToAGroup_StillLands_InsideItsCard()
	{
		_module.Setup<bool>("reveal", _ => true).SetResult(true);
		Navigation.NavigateTo("/settings#limits");

		IRenderedComponent<MokaSettingsForm> cut = Form(MokaSettingsAppearance.Cards);

		Assert.NotNull(cut.Find(".moka-settings-form__groups").QuerySelector("#limits"));
		cut.WaitForAssertion(() => Assert.Equal("limits", Assert.Single(_module.Invocations["reveal"]).Arguments[1]), Plenty);
	}

	private IRenderedComponent<MokaSettingsForm> Form(MokaSettingsAppearance appearance) =>
		Render<MokaSettingsForm>(p => p
			.Add(x => x.Title, "Storage")
			.Add(x => x.Appearance, appearance)
			.AddChildContent(Group("location", "Location"))
			.AddChildContent(Group("limits", "Limits")));

	private static RenderFragment Group(string id, string title, bool advanced = false) => builder =>
	{
		builder.OpenComponent<MokaSettingsGroup>(0);
		builder.AddComponentParameter(1, nameof(MokaSettingsGroup.Id), id);
		builder.AddComponentParameter(2, nameof(MokaSettingsGroup.Title), title);
		builder.AddComponentParameter(3, nameof(MokaSettingsGroup.Advanced), advanced);
		builder.CloseComponent();
	};
}
