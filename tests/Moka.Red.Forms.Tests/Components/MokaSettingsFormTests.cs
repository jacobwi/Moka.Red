using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Forms.SettingsForm;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

public class MokaSettingsFormTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Forms/SettingsForm/MokaSettingsForm.razor.js";

	private static readonly TimeSpan Plenty = TimeSpan.FromSeconds(10);

	private readonly BunitJSModuleInterop _module;

	public MokaSettingsFormTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	private IRenderedComponent<MokaSettingsForm> Form(Action<ComponentParameterCollectionBuilder<MokaSettingsForm>>? more = null) =>
		Render<MokaSettingsForm>(p =>
		{
			p.Add(x => x.Title, "General");
			more?.Invoke(p);
		});

	private static RenderFragment AdvancedGroup(string id = "ntp") => builder =>
	{
		builder.OpenComponent<MokaSettingsGroup>(0);
		builder.AddComponentParameter(1, nameof(MokaSettingsGroup.Id), id);
		builder.AddComponentParameter(2, nameof(MokaSettingsGroup.Title), "Time servers");
		builder.AddComponentParameter(3, nameof(MokaSettingsGroup.Advanced), true);
		builder.CloseComponent();
	};

	private static IElement AdvancedToggle(IRenderedComponent<MokaSettingsForm> cut) =>
		cut.Find(".moka-settings-form__advanced");

	[Fact]
	public void TheHeading_NamesTheSection()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form();

		Assert.Equal("General", cut.Find("h2").TextContent);
		Assert.Equal("General", cut.Find("section").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheLead_IsHelp_SoDescriptionsHideIt()
	{
		IElement lead = Form(p => p.Add(x => x.Lead, "How this router names itself."))
			.Find(".moka-settings-form__lead");

		Assert.Equal("How this router names itself.", lead.TextContent);
		Assert.Contains("moka-about", lead.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void TheNowLine_IsState_SoItAlwaysShows()
	{
		IElement now = Form(p => p.Add(x => x.Now, "this router is moka-gw")).Find(".moka-settings-form__now");

		Assert.Equal("Now: this router is moka-gw", string.Join(' ', now.TextContent.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries)));
		Assert.DoesNotContain("moka-about", now.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void NowContent_WinsOverNow()
	{
		IElement now = Form(p => p
			.Add(x => x.Now, "plain")
			.Add(x => x.NowContent, b => b.AddMarkupContent(0, "<strong>bold</strong>"))).Find(".moka-settings-form__now");

		Assert.Equal("bold", now.QuerySelector("strong")!.TextContent);
		Assert.DoesNotContain("plain", now.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void WithoutNow_ThereIsNoNowLine() =>
		Assert.Empty(Form().FindAll(".moka-settings-form__now"));

	[Fact]
	public void Status_SitsInTheHead()
	{
		IElement head = Form(p => p.Add(x => x.Status, b => b.AddMarkupContent(0, "<span id=\"status\">Saved</span>")))
			.Find(".moka-settings-form__head");

		Assert.NotNull(head.QuerySelector("#status"));
	}

	[Fact]
	public void Clean_ThereIsNoTagNoFooterAndNoLock()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form();

		Assert.Empty(cut.FindAll(".moka-tag"));
		Assert.Empty(cut.FindAll("footer"));
		Assert.Empty(cut.FindComponents<NavigationLock>());
	}

	[Fact]
	public void Dirty_ShowsTheTagAndTheFooter()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p
			.Add(x => x.Dirty, true)
			.Add(x => x.DirtyText, "2 changes not saved yet")
			.Add(x => x.OnReset, EventCallback.Factory.Create(this, () => { })));

		Assert.Equal("Unsaved", cut.Find(".moka-tag").TextContent.Trim());
		Assert.Equal("2 changes not saved yet", cut.Find(".moka-settings-form__foot-text").TextContent);
		Assert.Equal(["Put back", "Save"], cut.FindAll("footer button").Select(b => b.TextContent.Trim()));
	}

	[Fact]
	public void TheFooter_IsStickyAtTheBottom()
	{
		IReadOnlyDictionary<string, string> foot = ScopedCss.Declarations("MokaSettingsForm.razor.css", ".moka-settings-form__foot");

		Assert.Equal("sticky", foot["position"]);
		Assert.Equal("var(--moka-settings-footer-offset, var(--moka-action-bar-space, 0px))", foot["bottom"]);
	}

	// The question before leaving is scrolled to, and a floating action bar used to cover it, buttons and all.
	[Fact]
	public void TheFooter_AndTheScrollToIt_ClearAnActionBar()
	{
		IReadOnlyDictionary<string, string> foot = ScopedCss.Declarations("MokaSettingsForm.razor.css", ".moka-settings-form__foot");

		Assert.Equal(foot["bottom"], foot["scroll-margin-bottom"]);
	}

	// A clipping ancestor would stop the footer sticking to the window.
	[Fact]
	public void TheForm_DoesNotClip() =>
		Assert.False(ScopedCss.Declarations("MokaSettingsForm.razor.css", ".moka-settings-form").ContainsKey("overflow"));

	[Fact]
	public void Save_RaisesOnSave()
	{
		int saves = 0;
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p
			.Add(x => x.Dirty, true)
			.Add(x => x.OnSave, EventCallback.Factory.Create(this, () => saves++)));

		cut.Find("footer button").Click();

		Assert.Equal(1, saves);
	}

	[Fact]
	public void PutBack_RaisesOnReset()
	{
		int resets = 0;
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p
			.Add(x => x.Dirty, true)
			.Add(x => x.OnReset, EventCallback.Factory.Create(this, () => resets++)));

		cut.FindAll("footer button")[0].Click();

		Assert.Equal(1, resets);
	}

	[Fact]
	public void WithoutAResetListener_ThereIsNoPutBack() =>
		Assert.Equal(["Save"], Form(p => p.Add(x => x.Dirty, true)).FindAll("footer button").Select(b => b.TextContent.Trim()));

	[Fact]
	public void CanSaveOff_DisablesSave()
	{
		IElement save = Form(p => p.Add(x => x.Dirty, true).Add(x => x.CanSave, false)).Find("footer button");

		Assert.True(save.HasAttribute("disabled"));
	}

	[Fact]
	public void Saving_DisablesBothButtons_AndSaveSaysSo()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p
			.Add(x => x.Dirty, true)
			.Add(x => x.Saving, true)
			.Add(x => x.OnReset, EventCallback.Factory.Create(this, () => { })));

		IReadOnlyList<IElement> buttons = cut.FindAll("footer button");
		Assert.All(buttons, button => Assert.True(button.HasAttribute("disabled")));
		Assert.Equal("Saving", buttons[1].TextContent.Trim());
	}

	[Fact]
	public void ARefusal_IsShownInline_AndNeverHidden()
	{
		IElement refusal = Form(p => p.Add(x => x.Refusal, "That name is taken.")).Find(".moka-settings-form__refusal");

		Assert.Equal("alert", refusal.GetAttribute("role"));
		Assert.Equal("That name is taken.", refusal.TextContent.Trim());
		Assert.DoesNotContain("moka-about", refusal.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void WithoutARefusal_ThereIsNoAlert() =>
		Assert.Empty(Form().FindAll("[role=alert]"));

	[Fact]
	public void ShowAdvanced_AppearsOnlyWhenThereIsSomethingAdvanced() =>
		Assert.Empty(Form().FindAll(".moka-settings-form__advanced"));

	[Fact]
	public void ShowAdvanced_AppearsForAnAdvancedGroup()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p.Add(x => x.ChildContent, AdvancedGroup()));

		cut.WaitForAssertion(() => Assert.Equal("Show advanced", AdvancedToggle(cut).TextContent.Trim()), Plenty);
	}

	[Fact]
	public void HasAdvanced_ShowsTheToggleWithoutAGroup() =>
		Assert.Single(Form(p => p.Add(x => x.HasAdvanced, true)).FindAll(".moka-settings-form__advanced"));

	[Fact]
	public void TheToggle_ShowsAdvancedGroups_AndHidesThemAgain()
	{
		bool? reported = null;
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p
			.Add(x => x.ChildContent, AdvancedGroup())
			.Add(x => x.ShowAdvancedChanged, EventCallback.Factory.Create<bool>(this, on => reported = on)));
		cut.WaitForElement(".moka-settings-form__advanced", Plenty);
		Assert.Empty(cut.FindAll(".moka-settings-group"));

		AdvancedToggle(cut).Click();

		Assert.Single(cut.FindAll(".moka-settings-group"));
		Assert.Equal("Hide advanced", AdvancedToggle(cut).TextContent.Trim());
		Assert.Equal("true", AdvancedToggle(cut).GetAttribute("aria-expanded"));
		Assert.True(reported);

		AdvancedToggle(cut).Click();

		Assert.Empty(cut.FindAll(".moka-settings-group"));
		Assert.Equal("false", AdvancedToggle(cut).GetAttribute("aria-expanded"));
		Assert.False(reported);
	}

	[Fact]
	public void ThePersonsChoice_SurvivesAParentRender()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p
			.Add(x => x.HasAdvanced, true)
			.Add(x => x.ChildContent, AdvancedGroup()));
		cut.WaitForElement(".moka-settings-form__advanced", Plenty).Click();

		cut.Render(p => p.Add(x => x.Now, "something new"));

		Assert.Equal("true", AdvancedToggle(cut).GetAttribute("aria-expanded"));
	}

	[Fact]
	public void TheShowAdvancedParameter_SetsIt()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p
			.Add(x => x.ShowAdvanced, true)
			.Add(x => x.ChildContent, AdvancedGroup()));

		cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".moka-settings-group")), Plenty);
	}

	[Fact]
	public void WithoutAFragment_NothingIsRevealed()
	{
		Form();

		Assert.Empty(_module.Invocations["reveal"]);
	}

	[Fact]
	public void ALinkAlreadyInTheAddress_IsRevealedOnLoad()
	{
		_module.Setup<bool>("reveal", _ => true).SetResult(true);
		Navigation.NavigateTo("/settings#time-zone");

		IRenderedComponent<MokaSettingsForm> cut = Form();

		cut.WaitForAssertion(() => Assert.Equal("time-zone", Assert.Single(_module.Invocations["reveal"]).Arguments[1]), Plenty);
	}

	[Fact]
	public void ALinkOnThePage_IsRevealed_AfterTheLocationChanges()
	{
		_module.Setup<bool>("reveal", _ => true).SetResult(true);
		IRenderedComponent<MokaSettingsForm> cut = Form();

		Navigation.NavigateTo("/settings#time-zone");

		cut.WaitForAssertion(() => Assert.Equal("time-zone", Assert.Single(_module.Invocations["reveal"]).Arguments[1]), Plenty);
	}

	[Fact]
	public void TheBrowser_IsAskedToReportLinksOnThePage()
	{
		Form();

		Assert.Single(_module.Invocations["watchFragment"]);
	}

	// Blazor follows a link to another part of the same page itself, with no LocationChanged and a stale Uri.
	[Fact]
	public void ALinkBlazorDoesntReport_IsRevealed_WhenTheBrowserReportsIt()
	{
		_module.Setup<bool>("reveal", _ => true).SetResult(true);
		IRenderedComponent<MokaSettingsForm> cut = Form();

		cut.InvokeAsync(() => cut.Instance.OnFragmentFollowed("#time-zone"));

		cut.WaitForAssertion(() => Assert.Equal("time-zone", Assert.Single(_module.Invocations["reveal"]).Arguments[1]), Plenty);
	}

	[Fact]
	public void TheSameLinkFollowedAgain_MarksTheSettingAgain()
	{
		_module.Setup<bool>("reveal", _ => true).SetResult(true);
		IRenderedComponent<MokaSettingsForm> cut = Form();
		cut.InvokeAsync(() => cut.Instance.OnFragmentFollowed("#time-zone"));
		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["reveal"]), Plenty);

		cut.InvokeAsync(() => cut.Instance.OnFragmentFollowed("#time-zone"));

		cut.WaitForAssertion(() => Assert.Equal(2, _module.Invocations["reveal"].Count), Plenty);
	}

	[Fact]
	public void ALinkBlazorDoesntReport_ToAnAdvancedGroup_TurnsShowAdvancedOn()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p.Add(x => x.ChildContent, AdvancedGroup()));
		Assert.Equal("false", AdvancedToggle(cut).GetAttribute("aria-expanded"));

		cut.InvokeAsync(() => cut.Instance.OnFragmentFollowed("#ntp"));

		cut.WaitForAssertion(() => Assert.Equal("true", AdvancedToggle(cut).GetAttribute("aria-expanded")), Plenty);
	}

	[Fact]
	public void OnceRevealed_LaterRendersLeaveIt()
	{
		_module.Setup<bool>("reveal", _ => true).SetResult(true);
		Navigation.NavigateTo("/settings#time-zone");
		IRenderedComponent<MokaSettingsForm> cut = Form();
		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["reveal"]), Plenty);

		cut.Render(p => p.Add(x => x.Now, "something new"));

		Assert.Single(_module.Invocations["reveal"]);
	}

	// What the link points at may render only once the page has its data.
	[Fact]
	public void AMiss_IsTriedAgainOnTheNextRender()
	{
		_module.Setup<bool>("reveal", _ => true).SetResult(false);
		Navigation.NavigateTo("/settings#time-zone");
		IRenderedComponent<MokaSettingsForm> cut = Form();
		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["reveal"]), Plenty);

		cut.Render(p => p.Add(x => x.Now, "something new"));

		cut.WaitForAssertion(() => Assert.Equal(2, _module.Invocations["reveal"].Count), Plenty);
	}

	[Fact]
	public void ALinkToAnAdvancedGroup_TurnsShowAdvancedOnFirst()
	{
		bool? reported = null;
		Navigation.NavigateTo("/settings#ntp");

		IRenderedComponent<MokaSettingsForm> cut = Form(p => p
			.Add(x => x.ChildContent, AdvancedGroup())
			.Add(x => x.ShowAdvancedChanged, EventCallback.Factory.Create<bool>(this, on => reported = on)));

		cut.WaitForAssertion(() =>
		{
			Assert.Single(cut.FindAll(".moka-settings-group"));
			Assert.True(reported);
		}, Plenty);
	}

	[Fact]
	public void ThePerson_CanTurnAdvancedOffAgain_WhileTheLinkStaysInTheAddress()
	{
		Navigation.NavigateTo("/settings#ntp");
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p.Add(x => x.ChildContent, AdvancedGroup()));
		cut.WaitForAssertion(() => Assert.Equal("true", AdvancedToggle(cut).GetAttribute("aria-expanded")), Plenty);

		AdvancedToggle(cut).Click();
		cut.Render(p => p.Add(x => x.Now, "something new"));

		Assert.Equal("false", AdvancedToggle(cut).GetAttribute("aria-expanded"));
	}

	[Fact]
	public void GoingToTheLinkAgain_TurnsAdvancedBackOn()
	{
		Navigation.NavigateTo("/settings#ntp");
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p.Add(x => x.ChildContent, AdvancedGroup()));
		cut.WaitForAssertion(() => Assert.Equal("true", AdvancedToggle(cut).GetAttribute("aria-expanded")), Plenty);
		AdvancedToggle(cut).Click();

		Navigation.NavigateTo("/settings#ntp");

		cut.WaitForAssertion(() => Assert.Equal("true", AdvancedToggle(cut).GetAttribute("aria-expanded")), Plenty);
	}

	[Fact]
	public void ALinkToAnAdvancedEntry_TurnsShowAdvancedOnFirst()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p
			.Add(x => x.HasAdvanced, true)
			.Add(x => x.Entries, [new MokaSettingsEntry("Time server", "ntp", "ntp-server", Advanced: true)]));

		Navigation.NavigateTo("/settings#ntp-server");

		cut.WaitForAssertion(() => Assert.Equal("true", AdvancedToggle(cut).GetAttribute("aria-expanded")), Plenty);
	}

	[Fact]
	public void ALinkToAPlainEntry_LeavesAdvancedOff()
	{
		_module.Setup<bool>("reveal", _ => true).SetResult(true);
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p
			.Add(x => x.HasAdvanced, true)
			.Add(x => x.Entries, [new MokaSettingsEntry("Time zone", "clock", "time-zone")]));

		Navigation.NavigateTo("/settings#time-zone");

		cut.WaitForAssertion(() => Assert.NotEmpty(_module.Invocations["reveal"]), Plenty);
		Assert.Equal("false", AdvancedToggle(cut).GetAttribute("aria-expanded"));
	}

	[Fact]
	public void Dirty_ArmsTheLock_ForLeavingTheAppToo()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p.Add(x => x.Dirty, true));

		Assert.True(cut.FindComponent<NavigationLock>().Instance.ConfirmExternalNavigation);
	}

	[Fact]
	public void LeavingTheAppWithNoEdits_IsNotAsked() =>
		Assert.Empty(Form(p => p.Add(x => x.Dirty, false)).FindComponents<NavigationLock>());

	[Fact]
	public void LeavingWithEdits_IsAsked_InTheFooter()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p.Add(x => x.Dirty, true));

		AttemptToLeave(cut, "elsewhere");

		IElement question = cut.WaitForElement("footer [role=alert]", Plenty);
		Assert.Equal("Leave without saving your edits to General?", question.TextContent);
		Assert.Equal(["Stay", "Leave without saving"], cut.FindAll("footer button").Select(b => b.TextContent.Trim()));
	}

	// The footer sticks only while the form is in view, so the question could otherwise be asked where nobody sees.
	[Fact]
	public void TheQuestion_IsBroughtIntoViewOnce()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p.Add(x => x.Dirty, true));

		AttemptToLeave(cut, "elsewhere");

		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["showPrompt"]), Plenty);
		cut.Render(p => p.Add(x => x.Now, "something new"));
		Assert.Single(_module.Invocations["showPrompt"]);
	}

	[Fact]
	public void LeavePrompt_ReplacesTheQuestion()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p
			.Add(x => x.Dirty, true)
			.Add(x => x.LeavePrompt, "Really?"));

		AttemptToLeave(cut, "elsewhere");

		Assert.Equal("Really?", cut.WaitForElement("footer [role=alert]", Plenty).TextContent);
	}

	[Fact]
	public void ALinkWithinThePage_IsNotLeaving()
	{
		Navigation.NavigateTo("/settings");
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p.Add(x => x.Dirty, true));

		AttemptToLeave(cut, "settings#time-zone");

		Assert.Empty(cut.FindAll("footer [role=alert]"));
	}

	[Fact]
	public void Stay_PutsTheFooterBack()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p.Add(x => x.Dirty, true));
		AttemptToLeave(cut, "elsewhere");
		cut.WaitForElement("footer [role=alert]", Plenty);

		cut.FindAll("footer button")[0].Click();

		Assert.Empty(cut.FindAll("footer [role=alert]"));
		Assert.Equal("Save", cut.Find("footer button").TextContent.Trim());
	}

	[Fact]
	public void LeaveWithoutSaving_GoesWhereThePersonWasGoing()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p.Add(x => x.Dirty, true));
		AttemptToLeave(cut, "elsewhere");
		cut.WaitForElement("footer [role=alert]", Plenty);

		cut.FindAll("footer button")[1].Click();

		cut.WaitForAssertion(() => Assert.EndsWith("/elsewhere", Navigation.Uri, StringComparison.Ordinal), Plenty);
	}

	[Fact]
	public void TheEditsGoingAway_ClearsTheQuestion()
	{
		IRenderedComponent<MokaSettingsForm> cut = Form(p => p.Add(x => x.Dirty, true));
		AttemptToLeave(cut, "elsewhere");
		cut.WaitForElement("footer [role=alert]", Plenty);

		cut.Render(p => p.Add(x => x.Dirty, false));
		cut.Render(p => p.Add(x => x.Dirty, true));

		Assert.Empty(cut.FindAll("footer [role=alert]"));
	}

	[Fact]
	public void TheFoundMark_IsAThreePixelPrimaryRing_ThatMovesNothing()
	{
		IReadOnlyDictionary<string, string> found =
			ScopedCss.Declarations("MokaSettingsForm.razor.css", ".moka-settings-form ::deep .moka-settings-found");

		Assert.Equal("0.1875rem solid var(--moka-color-primary)", found["outline"]);
	}

	[Fact]
	public void TheForm_TakesClassStyleIdAndAttributes()
	{
		IElement form = Form(p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "max-width: 30rem")
			.Add(x => x.Id, "general")
			.AddUnmatched("data-area", "main")).Find("section");

		Assert.EndsWith("mine", form.ClassName, StringComparison.Ordinal);
		Assert.Equal("max-width: 30rem", form.GetAttribute("style"));
		Assert.Equal("general", form.Id);
		Assert.Equal("main", form.GetAttribute("data-area"));
	}

	// The lock asks through the handler it was given, the same call Blazor makes for a link.
	private static void AttemptToLeave(IRenderedComponent<MokaSettingsForm> cut, string target)
	{
		NavigationLock navigationLock = cut.FindComponent<NavigationLock>().Instance;
		cut.InvokeAsync(() => navigationLock.OnBeforeInternalNavigation.InvokeAsync(new LocationChangingContext
		{
			TargetLocation = new Uri(new Uri(cut.Services.GetRequiredService<NavigationManager>().BaseUri), target).ToString()
		}));
	}
}
