using System.Globalization;
using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Feedback.Onboarding;

namespace Moka.Red.Feedback.Tests.Components;

// Two ways a tour opened wrong: started again after it finished, it opened on the step it had ended
// on (its last), and a tour active from the first render kept its spotlight and card hidden until
// something unrelated rendered it again.
public class MokaOnboardingTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/Onboarding/MokaOnboarding.razor.js";

	private static readonly IReadOnlyList<MokaOnboardingStep> TourSteps =
	[
		new() { Title = "Welcome", Description = "Start here.", TargetSelector = "#one" },
		new() { Title = "Settings", Description = "Change things.", TargetSelector = "#two" },
		new() { Title = "Save", Description = "Keep them.", TargetSelector = "#three" }
	];

	private readonly BunitJSModuleInterop _module;

	public MokaOnboardingTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
	}

	// The docs and the demo app start the tour with a one-way Active and never bind ActiveStep, so
	// the parent passes the same 0 every time.
	[Fact]
	public async Task AFinishedTour_StartedAgain_OpensOnTheFirstStep()
	{
		IRenderedComponent<MokaOnboarding> cut = RenderTour();

		await ClickAsync(cut, "Next");
		await ClickAsync(cut, "Next");
		await ClickAsync(cut, "Finish");
		Assert.Empty(cut.FindAll(".moka-onboarding"));

		cut.Render(p => p.Add(x => x.Active, false));
		cut.Render(p => p.Add(x => x.Active, true).Add(x => x.ActiveStep, 0));

		Assert.Equal("Step 1 of 3", StepCount(cut));
		Assert.Equal("Welcome", cut.Find(".moka-onboarding-tooltip-title").TextContent);
	}

	[Fact]
	public async Task ASkippedTour_StartedAgain_OpensOnTheStepTheParentAsks()
	{
		IRenderedComponent<MokaOnboarding> cut = RenderTour();

		await ClickAsync(cut, "Next");
		await cut.Find(".moka-onboarding-skip").ClickAsync(new MouseEventArgs());

		cut.Render(p => p.Add(x => x.Active, false));
		cut.Render(p => p.Add(x => x.Active, true));
		Assert.Equal("Step 1 of 3", StepCount(cut));

		cut.Render(p => p.Add(x => x.Active, false));
		cut.Render(p => p.Add(x => x.Active, true).Add(x => x.ActiveStep, 2));
		Assert.Equal("Step 3 of 3", StepCount(cut));
	}

	// Only turning the tour on starts it over. A parent render that passes Active=true again, while
	// the tour runs, leaves the user where they are.
	[Fact]
	public async Task ARunningTour_KeepsItsStep_WhenTheParentPassesActiveAgain()
	{
		IRenderedComponent<MokaOnboarding> cut = RenderTour();

		await ClickAsync(cut, "Next");
		cut.Render(p => p.Add(x => x.Active, true).Add(x => x.ActiveStep, 0));

		Assert.Equal("Step 2 of 3", StepCount(cut));
	}

	[Fact]
	public void ATourActiveFromTheStart_ShowsItsSpotlightAfterTheFirstRender()
	{
		ReturnTargetRect(top: 100, left: 40, width: 120, height: 30);

		IRenderedComponent<CountingTour> cut = RenderTour<CountingTour>();

		cut.WaitForAssertion(() =>
		{
			string spotlight = cut.Find(".moka-onboarding-spotlight").GetAttribute("style") ?? "";
			Assert.Contains("top: 92px", spotlight, StringComparison.Ordinal);
			Assert.Contains("width: 136px", spotlight, StringComparison.Ordinal);
			Assert.DoesNotContain("display: none", cut.Find(".moka-onboarding-tooltip").GetAttribute("style") ?? "",
				StringComparison.Ordinal);
		});

		// Measured once, after the first render, and rendered once more to show it. Measuring after
		// every render as well would never settle on a target that moves.
		Assert.Single(_module.Invocations["measureTarget"]);
		Assert.Equal(2, cut.Instance.Renders);
	}

	// Target boxes are fractional, and "92,5px" is not CSS.
	[Fact]
	public void ThePositions_AreWrittenWithADecimalPoint_InAnyCulture()
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
		try
		{
			ReturnTargetRect(top: 100.5, left: 40.25, width: 120, height: 30);

			// Started by the parent after the first render, so the target is measured before the tour
			// renders.
			IRenderedComponent<MokaOnboarding> cut = Render<MokaOnboarding>(p => p.Add(x => x.Steps, TourSteps));
			cut.Render(p => p.Add(x => x.Active, true));

			Assert.Contains("top: 92.5px", cut.Find(".moka-onboarding-spotlight").GetAttribute("style") ?? "",
				StringComparison.Ordinal);
			Assert.Contains("left: 40.25px", cut.Find(".moka-onboarding-tooltip").GetAttribute("style") ?? "",
				StringComparison.Ordinal);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	// A missing target used to hide the card as well, so the overlay covered the page with nothing to
	// click. The card now shows in the middle, one render after the measurement, and nothing loops.
	[Fact]
	public void ATourWhoseTargetIsMissing_ShowsItsCardCentred_WithoutRenderingAgainAndAgain()
	{
		IRenderedComponent<CountingTour> cut = RenderTour<CountingTour>();

		Assert.Equal(2, cut.Instance.Renders);
		Assert.Equal("display: none;", cut.Find(".moka-onboarding-spotlight").GetAttribute("style"));
		IElement card = cut.Find(".moka-onboarding-tooltip");
		Assert.Contains("moka-onboarding-tooltip--centered", card.ClassList);
		Assert.DoesNotContain("display: none", card.GetAttribute("style") ?? "", StringComparison.Ordinal);
		Assert.Single(_module.Invocations["measureTarget"]);
	}

	// The overlay blocks the page, so the card is a modal dialog: named by its title, with focus
	// trapped in it.
	[Fact]
	public void TheCard_IsANamedModalDialog_ThatTakesFocus()
	{
		_module.Setup<int>("trapFocus", _ => true).SetResult(3);
		IRenderedComponent<MokaOnboarding> cut = RenderTour();

		IElement card = cut.Find("[role=dialog]");
		Assert.Equal("true", card.GetAttribute("aria-modal"));
		Assert.Equal("Welcome", cut.Find($"#{card.GetAttribute("aria-labelledby")}").TextContent);
		JSRuntimeInvocation trap = _module.VerifyInvoke("trapFocus");
		Assert.IsType<ElementReference>(trap.Arguments[0]);
	}

	// There was no way out of the tour from the keyboard.
	[Theory]
	[InlineData(true, 1)]
	[InlineData(false, 0)]
	public async Task Escape_SkipsTheTour_WhenSkippingIsAllowed(bool showSkip, int skips)
	{
		int skipped = 0;
		IRenderedComponent<MokaOnboarding> cut = Render<MokaOnboarding>(p => p
			.Add(x => x.Steps, TourSteps)
			.Add(x => x.Active, true)
			.Add(x => x.ShowSkipButton, showSkip)
			.Add(x => x.OnSkip, () => skipped++));

		await cut.Find(".moka-onboarding").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Equal(skips, skipped);
		Assert.Equal(showSkip, cut.FindAll(".moka-onboarding").Count == 0);
	}

	// The spotlight is measured in viewport coordinates, so the tour follows scrolling and resizing
	// while it shows, and stops when it closes.
	[Fact]
	public async Task TheTour_WatchesTheViewportWhileItShows()
	{
		_module.Setup<int?>("watchViewport", _ => true).SetResult(7);
		IRenderedComponent<MokaOnboarding> cut = RenderTour();
		_module.VerifyInvoke("watchViewport");

		await cut.Find(".moka-onboarding-skip").ClickAsync(new MouseEventArgs());

		Assert.Equal(7, _module.VerifyInvoke("unwatchViewport").Arguments[0]);
	}

	private IRenderedComponent<MokaOnboarding> RenderTour() => RenderTour<MokaOnboarding>();

	private IRenderedComponent<TTour> RenderTour<TTour>() where TTour : MokaOnboarding => Render<TTour>(p => p
		.Add(x => x.Steps, TourSteps)
		.Add(x => x.Active, true)
		.Add(x => x.ActiveStep, 0));

	// ElementRect is internal to Moka.Red.Feedback, and bUnit hands a planned result back as it is,
	// so the handler has to be made for that exact type.
	private void ReturnTargetRect(double top, double left, double width, double height)
	{
		Type rectType = typeof(MokaOnboarding).GetNestedType("ElementRect", BindingFlags.NonPublic)!;
		object rect = Activator.CreateInstance(rectType)!;
		SetProperty(rect, "Top", top);
		SetProperty(rect, "Left", left);
		SetProperty(rect, "Width", width);
		SetProperty(rect, "Height", height);
		SetProperty(rect, "Right", left + width);
		SetProperty(rect, "Bottom", top + height);
		SetProperty(rect, "ViewportWidth", 1280d);
		SetProperty(rect, "ViewportHeight", 800d);

		MethodInfo setup = typeof(BunitJSInteropSetupExtensions).GetMethods()
			.Single(m => m.Name == nameof(BunitJSInteropSetupExtensions.Setup)
			             && m.IsGenericMethodDefinition
			             && m.GetParameters().Select(p => p.ParameterType)
				             .SequenceEqual([typeof(BunitJSInterop), typeof(string), typeof(InvocationMatcher)]))
			.MakeGenericMethod(rectType);
		InvocationMatcher anySelector = _ => true;
		object handler = setup.Invoke(null, [_module, "measureTarget", anySelector])!;
		handler.GetType().GetMethod("SetResult")!.Invoke(handler, [rect]);
	}

	private static void SetProperty(object target, string name, double value) =>
		target.GetType().GetProperty(name)!.SetValue(target, value);

	private static Task ClickAsync(IRenderedComponent<MokaOnboarding> cut, string text) =>
		cut.FindAll("button").Single(b => b.TextContent.Trim() == text).ClickAsync(new MouseEventArgs());

	private static string StepCount(IRenderedComponent<MokaOnboarding> cut) =>
		cut.Find(".moka-onboarding-step-count").TextContent.Trim();

	// Counts the tour's own renders. bUnit's RenderCount also counts the buttons and icons inside it.
	private sealed class CountingTour : MokaOnboarding
	{
		public int Renders { get; private set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			Renders++;
			base.BuildRenderTree(builder);
		}
	}
}
