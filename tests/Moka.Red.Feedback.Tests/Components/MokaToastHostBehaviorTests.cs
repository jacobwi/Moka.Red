using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Announcer;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Toast;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaToastHostBehaviorTests : BunitContext
{
	// The default words format percentages in the current culture, which differs between machines.
	private static readonly MokaToastText Words = new();

	private const string Css = "MokaToastHost.razor.css";
	private const string Module = "./_content/Moka.Red.Feedback/Toast/MokaToastHost.razor.js";

	private readonly FakeClock _clock = new();
	private readonly BunitJSModuleInterop _module;

	public MokaToastHostBehaviorTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		Services.AddSingleton<TimeProvider>(_clock);
		Services.AddMokaFeedback();
	}

	private IMokaToastService Toasts => Services.GetRequiredService<IMokaToastService>();

	private IRenderedComponent<CascadingValue<MokaTheme>> RenderIn(MokaTheme theme,
		Action<ComponentParameterCollectionBuilder<MokaToastHost>>? host = null) =>
		Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, theme)
			.AddChildContent<MokaToastHost>(h => host?.Invoke(h)));

	[Fact]
	public void AToastWithoutOnClick_ListensToNoClicks()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		Toasts.ShowInfo("Saved");

		IElement toast = cut.Find(".moka-toast");
		Assert.False(toast.HasAttribute("blazor:onclick"));
		Assert.DoesNotContain("moka-toast--clickable", toast.ClassList);
	}

	[Fact]
	public void AToastWithOnClick_ListensAndRunsIt()
	{
		int clicks = 0;
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		Toasts.ShowInfo("Open the report", o => o.OnClick = () => clicks++);

		IElement toast = cut.Find(".moka-toast");
		Assert.Contains("moka-toast--clickable", toast.ClassList);
		toast.Click();

		Assert.Equal(1, clicks);
	}

	[Fact]
	public void TheActionButton_IsAButtonThatSubmitsNothing_AndClosesTheToast()
	{
		List<MokaToastDismissReason> reasons = [];
		int undone = 0;
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		Toasts.ShowInfo("Moved to trash", o =>
		{
			o.ActionText = "Undo";
			o.OnAction = () => undone++;
			o.OnDismissed = reasons.Add;
		});

		IElement action = cut.Find(".moka-toast-action");
		Assert.Equal("button", action.GetAttribute("type"));
		action.Click();

		Assert.Equal(1, undone);
		Assert.Empty(cut.FindAll(".moka-toast"));
		Assert.Equal([MokaToastDismissReason.Action], reasons);
	}

	[Fact]
	public void TheWords_CanBeReplaced()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>(p => p.Add(x => x.Text, new MokaToastText
		{
			Close = "Fermer",
			CountBadge = n => $"x{n}",
			Repeated = n => $"{n} fois"
		}));
		Toasts.ShowInfo("Copié", o => o.Repeats = MokaToastRepeats.Count);
		Toasts.ShowInfo("Copié", o => o.Repeats = MokaToastRepeats.Count);

		Assert.Equal("Fermer", cut.Find(".moka-toast-close").GetAttribute("aria-label"));
		Assert.Equal("x22 fois", cut.Find(".moka-toast-count").TextContent);
	}

	[Fact]
	public void TheCountdown_IsTheTimerBarsAnimation_NamedForTheScript()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		MokaToastHandle handle = Toasts.ShowInfo("Saved", o => o.Duration = TimeSpan.FromSeconds(8));

		IElement timer = cut.Find(".moka-toast-timer");
		Assert.Equal(handle.Id.ToString(), timer.GetAttribute("data-moka-toast"));
		Assert.Equal("0", timer.GetAttribute("data-moka-run"));
		Assert.Equal("--_toast-duration: 8000ms", timer.GetAttribute("style"));
		Assert.Equal("true", timer.GetAttribute("aria-hidden"));
	}

	[Fact]
	public void AToastThatStays_HasNoCountdown()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		Toasts.ShowInfo("Read me", o => o.Duration = null);

		Assert.Empty(cut.FindAll(".moka-toast-timer"));
	}

	// The countdown element is keyed by its run, so a new run is a new element and its animation starts from the top.
	[Fact]
	public void ARepeat_StartsANewCountdown()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		Toasts.ShowInfo("Again", o => o.Repeats = MokaToastRepeats.Count);
		Assert.Equal("0", cut.Find(".moka-toast-timer").GetAttribute("data-moka-run"));

		Toasts.ShowInfo("Again", o => o.Repeats = MokaToastRepeats.Count);

		Assert.Equal("1", cut.Find(".moka-toast-timer").GetAttribute("data-moka-run"));
	}

	[Fact]
	public async Task TheEndOfTheCountdown_EndsTheToast()
	{
		List<MokaToastDismissReason> reasons = [];
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		MokaToastHandle handle = Toasts.ShowInfo("Saved", o => o.OnDismissed = reasons.Add);

		await cut.InvokeAsync(() => cut.Instance.OnCountdownEnded(handle.Id.ToString(), 0));

		Assert.Empty(cut.FindAll(".moka-toast"));
		Assert.Equal([MokaToastDismissReason.Expired], reasons);
	}

	[Fact]
	public async Task TheEndOfAnOldCountdown_LeavesARestartedToast()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		MokaToastHandle handle = Toasts.ShowInfo("Again", o => o.Repeats = MokaToastRepeats.Count);
		Toasts.ShowInfo("Again", o => o.Repeats = MokaToastRepeats.Count);

		await cut.InvokeAsync(() => cut.Instance.OnCountdownEnded(handle.Id.ToString(), 0));

		Assert.Single(cut.FindAll(".moka-toast"));
	}

	[Fact]
	public async Task ASwipe_EndsTheToast()
	{
		List<MokaToastDismissReason> reasons = [];
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		MokaToastHandle handle = Toasts.ShowInfo("Swipe me", o => o.OnDismissed = reasons.Add);

		await cut.InvokeAsync(() => cut.Instance.OnSwiped(handle.Id.ToString()));

		Assert.Empty(cut.FindAll(".moka-toast"));
		Assert.Equal([MokaToastDismissReason.Swiped], reasons);
	}

	[Fact]
	public async Task TheHost_BindsItsScriptOnce_AndUnbindsOnDisposal()
	{
		Render<MokaToastHost>();
		Toasts.ShowInfo("One");
		Toasts.ShowInfo("Two");

		Assert.Single(_module.Invocations["bindHost"]);

		await DisposeComponentsAsync();

		Assert.Single(_module.Invocations["unbindHost"]);
	}

	[Theory]
	[InlineData(".moka-toast-host:hover .moka-toast-timer")]
	[InlineData(".moka-toast-host:focus-within .moka-toast-timer")]
	[InlineData(".moka-toast-host[data-moka-hidden] .moka-toast-timer")]
	[InlineData(".moka-toast[data-moka-swiping] .moka-toast-timer")]
	public void TheCountdown_Pauses(string selector) =>
		Assert.Equal("paused", ScopedCss.Declarations(Css, selector)["animation-play-state"]);

	[Fact]
	public void TheCountdown_RunsForItsDuration_ReadingTheToastsOwnLength()
	{
		Assert.Equal("moka-toast-timer var(--_toast-duration) linear forwards",
			ScopedCss.Declarations(Css, ".moka-toast-timer")["animation"]);
	}

	// moka.css cuts every animation to 0.01ms under reduced motion; the countdown takes its length back and hides.
	[Fact]
	public void ReducedMotion_KeepsTheTiming_AndHidesTheMovement()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css)).ReplaceLineEndings("\n");
		int media = css.IndexOf("@media (prefers-reduced-motion: reduce) {\n\t.moka-toast-timer {", StringComparison.Ordinal);
		Assert.True(media >= 0);
		string block = css[media..css.IndexOf("}\n}", media, StringComparison.Ordinal)];

		Assert.Contains("animation-duration: var(--_toast-duration) !important;", block, StringComparison.Ordinal);
		Assert.Contains("visibility: hidden;", block, StringComparison.Ordinal);
	}

	[Fact]
	public void ToastsPastMaxVisible_WaitWithoutCountingDown()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>(p => p.Add(x => x.MaxVisible, 2));
		Toasts.ShowInfo("Oldest");
		Toasts.ShowInfo("Middle");
		Toasts.ShowInfo("Newest");

		Assert.Equal(["Middle", "Newest"], cut.FindAll(".moka-toast-message").Select(m => m.TextContent));
		Assert.Equal(2, cut.FindAll(".moka-toast-timer").Count);
	}

	[Fact]
	public void ScreenReaders_HearEachToastOnce_PolitelyOrAtOnceForErrors()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		Toasts.ShowSuccess("12 items synced.", o => o.Title = "Vault unlocked");
		Toasts.ShowError("report.pdf is too big.");

		cut.WaitForAssertion(() =>
		{
			Assert.Equal(["Vault unlocked. 12 items synced."], Region(cut, "status"));
			Assert.Equal(["report.pdf is too big."], Region(cut, "alert"));
		});
	}

	[Fact]
	public void ARepeatCount_IsNotReadAgain_ButNewWordsAndATrackedResultAre()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		Toasts.ShowInfo("Ping", o => o.Repeats = MokaToastRepeats.Count);
		Toasts.ShowInfo("Ping", o => o.Repeats = MokaToastRepeats.Count);
		MokaToastHandle upload = Toasts.ShowInfo("Uploading", o => o.Progress = 0.1);
		upload.Update(o => o.Progress = 0.6);
		upload.Update(o =>
		{
			o.Progress = null;
			o.Message = "Uploaded";
		});
		_clock.Advance(TimeSpan.FromSeconds(1));

		cut.WaitForAssertion(() => Assert.Equal(["Ping", "Uploading", "Uploaded"], Region(cut, "status")));
	}

	[Fact]
	public void ToastsAlreadyShowing_AreHeardOnceTheRegionsAreInThePage()
	{
		Toasts.ShowInfo("Shown before the host");

		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();

		cut.WaitForAssertion(() => Assert.Equal(["Shown before the host"], Region(cut, "status")));
		Assert.True(cut.FindComponent<MokaAnnouncerHost>().RenderCount >= 2);
	}

	[Fact]
	public void ProgressUpdates_CoalesceToOneDrawPerGap_ShowingTheLatest()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		MokaToastHandle handle = Toasts.ShowInfo("Uploading", o => o.Progress = 0);
		IRenderedComponent<MokaToastGate> gate = cut.FindComponent<MokaToastGate>();
		int before = gate.RenderCount;

		for (int i = 1; i <= 9; i++)
		{
			double share = i / 10d;
			handle.Update(o => o.Progress = share);
		}

		Assert.Equal(before, gate.RenderCount);

		_clock.Advance(TimeSpan.FromMilliseconds(100));

		cut.WaitForAssertion(() => Assert.Equal(before + 1, gate.RenderCount));
		Assert.Equal("--_toast-progress: 0.9", cut.Find(".moka-toast-edge__fill").GetAttribute("style"));
	}

	[Fact]
	public void AnUpdate_RedrawsOnlyItsOwnToast()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>(p => p.Add(x => x.MaxVisible, 50));
		List<MokaToastHandle> handles = [.. Enumerable.Range(0, 50).Select(i => Toasts.ShowInfo($"Toast {i}"))];
		IReadOnlyList<IRenderedComponent<MokaToastGate>> gates = cut.FindComponents<MokaToastGate>();
		int[] before = [.. gates.Select(g => g.RenderCount)];

		handles[20].Update(o => o.Message = "Toast 20, changed");

		int[] after = [.. gates.Select(g => g.RenderCount)];
		Assert.Equal(before[20] + 1, after[20]);
		Assert.Equal(before.Where((_, i) => i != 20), after.Where((_, i) => i != 20));
	}

	[Fact]
	public void OnTheOriginalDesign_ProgressIsASpinnerAndTheEdge_WhateverTheLook()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Dark,
			h => h.Add(x => x.ProgressLook, MokaToastProgressLook.Ring));
		Toasts.ShowInfo("Copying", o => o.Progress = 0.4);

		Assert.Contains("moka-spin", cut.Find(".moka-toast-icon").ClassList);
		Assert.Empty(cut.FindAll(".moka-toast-ring"));
		IElement bar = Assert.Single(cut.FindAll("[role=progressbar]"));
		Assert.Contains("moka-toast-edge", bar.ClassList);
		Assert.Equal("40", bar.GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void TheRing_ShowsThePercent_AsTheOneProgressBar()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red,
			h => h.Add(x => x.ProgressLook, MokaToastProgressLook.Ring));
		Toasts.ShowInfo("Copying", o => o.Progress = 0.4);

		IElement ring = Assert.Single(cut.FindAll("[role=progressbar]"));
		Assert.Contains("moka-toast-ring", ring.ClassList);
		Assert.Equal(Words.Percent(0.4), cut.Find(".moka-toast-ring__value").TextContent);
		Assert.Equal(cut.Find(".moka-toast-message").Id, ring.GetAttribute("aria-labelledby"));
	}

	[Fact]
	public void Detailed_ShowsThePercentAndTheTimeLeft()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red);
		Toasts.ShowInfo("Copying", o =>
		{
			o.Progress = new MokaToastProgress(0.25, TimeSpan.FromMinutes(2));
			o.ProgressLook = MokaToastProgressLook.Detailed;
		});

		Assert.Equal([Words.Percent(0.25), "About 2 min left"],
			cut.FindAll(".moka-toast-detail__facts > span").Select(s => s.TextContent));
		Assert.Contains("moka-toast-track", Assert.Single(cut.FindAll("[role=progressbar]")).ClassList);
	}

	[Fact]
	public void Indeterminate_SaysItIsWorking_WithoutANumber()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red);
		Toasts.ShowInfo("Connecting", o => o.Progress = MokaToastProgress.Indeterminate);

		IElement bar = Assert.Single(cut.FindAll("[role=progressbar]"));
		Assert.False(bar.HasAttribute("aria-valuenow"));
		Assert.Equal("Working", bar.GetAttribute("aria-valuetext"));
		Assert.Contains("moka-toast-edge__fill--indeterminate", cut.Find(".moka-toast-edge__fill").ClassList);
	}

	[Theory]
	[InlineData(MokaToastLook.Pill)]
	[InlineData(MokaToastLook.Snackbar)]
	public void OneLineLooks_ShowProgressOnTheLine(MokaToastLook look)
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red, h => h.Add(x => x.Look, look));
		Toasts.ShowInfo("Exporting", o => o.Progress = 0.5);

		Assert.Equal(Words.Percent(0.5), Assert.Single(cut.FindAll("[role=progressbar]")).TextContent);
		Assert.Contains("moka-toast-pill", cut.Find("[role=progressbar]").ClassList);
	}

	[Fact]
	public void BarOnly_KeepsTheSeverityIconStill()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = RenderIn(MokaTheme.Red,
			h => h.Add(x => x.ProgressLook, MokaToastProgressLook.BarOnly));
		Toasts.ShowWarning("Low space", o => o.Progress = 0.8);

		Assert.DoesNotContain("moka-spin", cut.Find(".moka-toast-icon").ClassList);
		Assert.Contains("moka-toast-edge", Assert.Single(cut.FindAll("[role=progressbar]")).ClassList);
	}

	[Fact]
	public void Content_RendersUnderTheMessage()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();
		Toasts.ShowInfo("Shared", o => o.Content = builder => builder.AddMarkupContent(0, "<em class=\"extra\">with 3 people</em>"));

		Assert.Equal("with 3 people", cut.Find(".moka-toast-content .extra").TextContent);
	}

	private static string[] Region<TComponent>(IRenderedComponent<TComponent> cut, string role) where TComponent : IComponent =>
		[.. cut.Find($".moka-announcer[role={role}]").Children.Select(c => c.TextContent)];
}
