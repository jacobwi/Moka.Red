using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Cheatsheet;
using Moka.Red.Feedback.CookieConsent;
using Moka.Red.Feedback.Drawer;
using Moka.Red.Feedback.Onboarding;
using Moka.Red.Feedback.SlashMenu;
using Moka.Red.Feedback.Wizard;

namespace Moka.Red.Feedback.Tests.Components;

// These components used to write the user's change into their own [Parameter]. Blazor passes every
// parameter again whenever the parent renders, so with a one-way value the parent's next render put
// the old value back: a closed drawer reopened, a wizard jumped back a step. Each test changes the
// state through the UI, re-renders with the same value (what an unrelated parent render does), and
// then checks that a new value from the parent still wins.
public class OneWayBindingTests : BunitContext
{
	private static readonly IReadOnlyList<MokaOnboardingStep> TourSteps =
	[
		new() { Title = "Welcome", Description = "Start here.", TargetSelector = "#one" },
		new() { Title = "Settings", Description = "Change things.", TargetSelector = "#two" },
		new() { Title = "Save", Description = "Keep them.", TargetSelector = "#three" }
	];

	private static readonly IReadOnlyList<MokaSlashMenuItem> SlashItems =
	[
		new() { Title = "Heading" },
		new() { Title = "Bullet list" }
	];

	public OneWayBindingTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task Drawer_StaysClosed_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p.Add(x => x.Open, true));

		await cut.Find(".moka-drawer").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Empty(cut.FindAll(".moka-drawer"));
	}

	[Fact]
	public async Task Drawer_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaDrawer> cut = Render<MokaDrawer>(p => p.Add(x => x.Open, true));

		await cut.Find(".moka-drawer").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Single(cut.FindAll(".moka-drawer"));
	}

	[Fact]
	public async Task Cheatsheet_StaysClosed_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p.Add(x => x.Open, true));

		await cut.Find(".moka-cheatsheet").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Empty(cut.FindAll(".moka-cheatsheet"));
	}

	[Fact]
	public async Task Cheatsheet_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p.Add(x => x.Open, true));

		await cut.Find(".moka-cheatsheet").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Single(cut.FindAll(".moka-cheatsheet"));
	}

	[Fact]
	public async Task CookieConsent_StaysAnswered_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaCookieConsent> cut = Render<MokaCookieConsent>(p => p.Add(x => x.Visible, true));

		await Button(cut, "Accept All").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Visible, true));

		Assert.Empty(cut.FindAll(".moka-cookie-consent"));
	}

	[Fact]
	public async Task CookieConsent_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaCookieConsent> cut = Render<MokaCookieConsent>(p => p.Add(x => x.Visible, true));

		await Button(cut, "Reject All").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Visible, false));
		cut.Render(p => p.Add(x => x.Visible, true));

		Assert.Single(cut.FindAll(".moka-cookie-consent"));
	}

	[Fact]
	public async Task Onboarding_StaysOnTheStep_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaOnboarding> cut = RenderTour();

		await Button(cut, "Next").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.ActiveStep, 0));

		Assert.Equal("Step 2 of 3", cut.Find(".moka-onboarding-step-count").TextContent.Trim());
	}

	[Fact]
	public async Task Onboarding_FollowsANewStepFromTheParent()
	{
		IRenderedComponent<MokaOnboarding> cut = RenderTour();

		await Button(cut, "Next").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.ActiveStep, 2));

		Assert.Equal("Step 3 of 3", cut.Find(".moka-onboarding-step-count").TextContent.Trim());
	}

	[Fact]
	public async Task Onboarding_StaysSkipped_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaOnboarding> cut = RenderTour();

		await cut.Find(".moka-onboarding-skip").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Active, true));

		Assert.Empty(cut.FindAll(".moka-onboarding"));
	}

	[Fact]
	public async Task Onboarding_RestartsWhenTheParentTurnsItOffAndOn()
	{
		IRenderedComponent<MokaOnboarding> cut = RenderTour();

		await cut.Find(".moka-onboarding-skip").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Active, false));
		cut.Render(p => p.Add(x => x.Active, true));

		Assert.Single(cut.FindAll(".moka-onboarding"));
	}

	[Fact]
	public async Task SlashMenu_StaysClosed_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderSlashMenu();

		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "Escape" }));
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Empty(cut.FindAll(".moka-slash-menu"));

		// Closed, it leaves the keys to the editor.
		bool consumed = true;
		await cut.InvokeAsync(async () =>
			consumed = await cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "ArrowDown" }));
		Assert.False(consumed);
	}

	[Fact]
	public async Task SlashMenu_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaSlashMenu> cut = RenderSlashMenu();

		await cut.InvokeAsync(() => cut.Instance.HandleKeyAsync(new KeyboardEventArgs { Key = "Escape" }));
		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Single(cut.FindAll(".moka-slash-menu"));
	}

	[Fact]
	public async Task Wizard_StaysOnTheStep_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaWizard> cut = RenderWizard();

		await Button(cut, "Next").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.ActiveStep, 0));

		Assert.Equal("Two", cut.Find(".moka-wizard-step").TextContent.Trim());
	}

	[Fact]
	public async Task Wizard_FollowsANewStepFromTheParent()
	{
		IRenderedComponent<MokaWizard> cut = RenderWizard();

		await Button(cut, "Next").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.ActiveStep, 2));

		Assert.Equal("Three", cut.Find(".moka-wizard-step").TextContent.Trim());
	}

	private static IElement Button<T>(IRenderedComponent<T> cut, string text) where T : IComponent =>
		cut.FindAll("button").Single(b => b.TextContent.Trim() == text);

	private IRenderedComponent<MokaOnboarding> RenderTour() => Render<MokaOnboarding>(p => p
		.Add(x => x.Steps, TourSteps)
		.Add(x => x.Active, true)
		.Add(x => x.ActiveStep, 0));

	private IRenderedComponent<MokaSlashMenu> RenderSlashMenu() => Render<MokaSlashMenu>(p => p
		.Add(x => x.Items, SlashItems)
		.Add(x => x.Open, true));

	private IRenderedComponent<MokaWizard> RenderWizard() => Render<MokaWizard>(p => p
		.Add(x => x.ActiveStep, 0)
		.AddChildContent<MokaWizardStep>(step => step.Add(x => x.Title, "First").AddChildContent("One"))
		.AddChildContent<MokaWizardStep>(step => step.Add(x => x.Title, "Second").AddChildContent("Two"))
		.AddChildContent<MokaWizardStep>(step => step.Add(x => x.Title, "Third").AddChildContent("Three")));
}
