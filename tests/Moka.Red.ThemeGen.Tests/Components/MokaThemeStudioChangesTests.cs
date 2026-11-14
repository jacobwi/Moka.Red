using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Announcer;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Tests.Shared;
using Moka.Red.ThemeGen.Appearance;

namespace Moka.Red.ThemeGen.Tests.Components;

// Showing what a change touched is off until the person turns it on. While it's off the studio loads no script and
// calls nothing; once on, it reads the previews before a change and compares them once the change has drawn.
public sealed class MokaThemeStudioChangesTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.ThemeGen/Studio/MokaThemeStudio.razor.js";

	private readonly FakeClock _clock = new();
	private readonly BunitJSModuleInterop _module;
	private readonly MokaThemeStudioText _text = new();

	public MokaThemeStudioChangesTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<TimeProvider>(_clock);
		Services.AddMokaFeedback();
		SetRendererInfo(new RendererInfo("Server", true));
		_module = JSInterop.SetupModule(ModulePath);
		_module.SetupVoid("snapshot", _ => true).SetVoidResult();
	}

	private IMokaAnnouncer Announcer => Services.GetRequiredService<IMokaAnnouncer>();

	[Fact]
	public async Task ByDefault_NothingIsLoadedReadOrSaid()
	{
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>();

		await cut.PickAsync("Tight");
		await cut.Find(".moka-theme-studio__controls input[role='switch']").ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.False(ShowSwitch(cut).HasAttribute("checked"));
		Assert.DoesNotContain(JSInterop.Invocations, ImportsTheScript);
		Assert.Empty(_module.Invocations);
		Assert.Empty(cut.FindAll(".moka-theme-studio__change"));
		Assert.Empty(Announcer.Recent);
	}

	[Fact]
	public async Task TurningItOn_LoadsTheScript_AndNamesWhatAChangeTouched()
	{
		_module.Setup<int>("compare", _ => true).SetResult(23);
		bool? raised = null;
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>(p => p
			.Add(x => x.ShowChangesChanged, EventCallback.Factory.Create<bool>(this, value => raised = value)));

		await ShowSwitch(cut).ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.True(raised);
		Assert.Single(JSInterop.Invocations, ImportsTheScript);
		Assert.Equal(_text.ChangeHint, Line(cut));
		Assert.Empty(_module.Invocations);

		await cut.PickAsync("Tight");

		Assert.Equal(["snapshot", "compare"], _module.Invocations.Select(invocation => invocation.Identifier));
		Assert.Equal("Corners: 23 elements changed.", Line(cut));
		Assert.Equal("Corners: 23 elements changed.", Assert.Single(Announcer.Recent).Text);
	}

	[Fact]
	public async Task BoundOn_ItShowsFromTheStart()
	{
		_module.Setup<int>("compare", _ => true).SetResult(1);
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>(p => p.Add(x => x.ShowChanges, true));

		await cut.PickAsync("Tight");

		Assert.True(ShowSwitch(cut).HasAttribute("checked"));
		Assert.Equal("Corners: 1 element changed.", Line(cut));
	}

	[Fact]
	public async Task AChoiceThatShowsOnlyOnceSomethingOpens_SaysWhereToLook()
	{
		_module.Setup<int>("compare", _ => true).SetResult(0);
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>(p => p.Add(x => x.ShowChanges, true));

		await Row(cut, _text.Appearance.Dialogs, _text.Appearance.DialogsBands).ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal(_text.ShownLater(_text.Appearance.Dialogs, MokaAppearanceRows.Dialogs), Line(cut));
	}

	[Fact]
	public async Task AChoiceThatMarksNothing_SaysSo()
	{
		_module.Setup<int>("compare", _ => true).SetResult(0);
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>(p => p.Add(x => x.ShowChanges, true));

		await Row(cut, _text.Appearance.Callouts, _text.Appearance.CalloutsEdge).ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal("Callouts: nothing in the preview uses this choice.", Line(cut));
	}

	// The component style row sets six rows at once, and the change carries that row's name rather than one of the six.
	[Fact]
	public async Task TheComponentStyleRow_IsNamedAsOneChange()
	{
		_module.Setup<int>("compare", _ => true).SetResult(40);
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>(p => p.Add(x => x.ShowChanges, true));

		await Row(cut, _text.Appearance.ComponentStyle, _text.Appearance.ComponentStyleTerminal).ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal("Component style: 40 elements changed.", Line(cut));
	}

	// A slider's frames are drawn as they come, and the previews are compared once, after it has stood still.
	[Fact]
	public void ASliderDrag_IsComparedOnce_WhenItStandsStill()
	{
		_module.Setup<int>("compare", _ => true).SetResult(12);
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>(p => p.Add(x => x.ShowChanges, true));

		for (int step = 1; step <= 3; step++)
		{
			cut.Find(".moka-slider-input").Input(FormattableString.Invariant($"{step * 0.01:0.00}"));
			_clock.Advance(TimeSpan.FromMilliseconds(16));
		}

		Assert.Equal(["snapshot"], _module.Invocations.Select(invocation => invocation.Identifier));

		_clock.Advance(TimeSpan.FromMilliseconds(400));

		cut.WaitForAssertion(() => Assert.Equal("Neutral strength: 12 elements changed.", Line(cut)));
		Assert.Equal(["snapshot", "compare"], _module.Invocations.Select(invocation => invocation.Identifier));
	}

	[Fact]
	public async Task TurningItOff_DropsTheLine_AndReadsNothingMore()
	{
		_module.Setup<int>("compare", _ => true).SetResult(5);
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>(p => p.Add(x => x.ShowChanges, true));
		await cut.PickAsync("Tight");
		int calls = _module.Invocations.Count;

		await ShowSwitch(cut).ChangeAsync(new ChangeEventArgs { Value = false });
		await cut.PickAsync("Medium");

		Assert.Empty(cut.FindAll(".moka-theme-studio__change"));
		Assert.Equal(calls, _module.Invocations.Count);
	}

	// A new start changes nearly everything, so marking it would only be noise. The next change is shown again.
	[Fact]
	public async Task ANewStart_IsNotShown_AndTheNextChangeIs()
	{
		_module.Setup<int>("compare", _ => true).SetResult(9);
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>(p => p.Add(x => x.ShowChanges, true));

		await cut.Find("input[type='radio'][value='gate']").ChangeAsync(new ChangeEventArgs { Value = "gate" });

		Assert.Empty(_module.Invocations);
		Assert.Equal(_text.ChangeHint, Line(cut));
		Assert.Empty(Announcer.Recent);

		await cut.FindAll(".moka-color-input__box--split input")[0].InputAsync(new ChangeEventArgs { Value = "#7C3AED" });

		Assert.Equal(["snapshot", "compare"], _module.Invocations.Select(invocation => invocation.Identifier));
		Assert.Equal("Accent: 9 elements changed.", Line(cut));
	}

	[Fact]
	public async Task SwitchingTheSchemesShown_IsNotAChange()
	{
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>(p => p.Add(x => x.ShowChanges, true));

		await cut.PickAsync(_text.Light);

		Assert.Empty(_module.Invocations);
		Assert.Equal(_text.ChangeHint, Line(cut));
	}

	private static bool ImportsTheScript(JSRuntimeInvocation invocation) =>
		invocation.Identifier == "import" && invocation.Arguments.Count > 0 && Equals(invocation.Arguments[0], ModulePath);

	private static IElement ShowSwitch(IRenderedComponent<MokaThemeStudio> cut) => cut.Find(".moka-theme-studio__stage-tools input[role='switch']");

	private static string Line(IRenderedComponent<MokaThemeStudio> cut) => cut.Find(".moka-theme-studio__change").TextContent.Trim();

	// Option words repeat across rows, so a row's option is found inside the radio group the row's name labels.
	private static IElement Row(IRenderedComponent<MokaThemeStudio> cut, string row, string option) =>
		cut.FindAll("[role='radiogroup']")
			.First(group => group.GetAttribute("aria-label") == row)
			.QuerySelectorAll(".moka-segment")
			.First(label => label.TextContent.Trim() == option)
			.QuerySelector("input")!;
}
