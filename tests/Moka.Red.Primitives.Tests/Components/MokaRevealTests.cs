using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Primitives.Reveal;

namespace Moka.Red.Primitives.Tests.Components;

// MokaReveal tells the app when its content comes into view and leaves it, so a thumbnail can load only once it can be
// seen. The browser watches; .NET hears only the crossings.
public class MokaRevealTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Primitives/Reveal/MokaReveal.razor.js";

	private readonly BunitJSModuleInterop _module;
	private readonly List<string> _heard = [];

	public MokaRevealTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	private IReadOnlyDictionary<string, object?> Options(int call = 0) =>
		(IReadOnlyDictionary<string, object?>)_module.Invocations["observe"][call].Arguments[2]!;

	private IRenderedComponent<MokaReveal> Watching(Action<ComponentParameterCollectionBuilder<MokaReveal>>? more = null) =>
		Render<MokaReveal>(p =>
		{
			p.Add(x => x.Animation, MokaRevealAnimation.None)
				.Add(x => x.OnEnter, () => _heard.Add("enter"))
				.Add(x => x.OnLeave, () => _heard.Add("leave"))
				.AddChildContent("<img alt=\"\">");
			more?.Invoke(p);
		});

	[Fact]
	public void ByDefault_TheMarkupAndTheWatchAreAsTheyWere()
	{
		IRenderedComponent<MokaReveal> cut = Render<MokaReveal>(p => p.AddChildContent("<p>Hi</p>"));

		IElement root = cut.Find(".moka-reveal");
		Assert.Equal("moka-reveal moka-reveal--fade-up", root.ClassName);
		Assert.Equal("--moka-reveal-duration: 600ms", root.GetAttribute("style"));
		Assert.Single(_module.Invocations["observe"]);
		Assert.Equal(0.1, Options()["threshold"]);
		Assert.Equal("0px", Options()["rootMargin"]);
		Assert.True((bool)Options()["once"]!);
		Assert.Equal(0, Options()["debounce"]);
		Assert.False((bool)Options()["leaves"]!);
	}

	[Fact]
	public void AnAnimationThatPlaysAgain_HearsLeaves()
	{
		Render<MokaReveal>(p => p.Add(x => x.Once, false));

		Assert.True((bool)Options()["leaves"]!);
	}

	[Fact]
	public void NoAnimation_AndNoListener_WatchesNothing()
	{
		IRenderedComponent<MokaReveal> cut = Render<MokaReveal>(p => p.Add(x => x.Animation, MokaRevealAnimation.None));

		Assert.Equal("moka-reveal moka-reveal--none", cut.Find(".moka-reveal").ClassName);
		Assert.Null(cut.Find(".moka-reveal").GetAttribute("style"));
		Assert.Empty(_module.Invocations["observe"]);
	}

	[Fact]
	public void NoAnimation_WithAListener_WatchesWithoutAHiddenState()
	{
		IRenderedComponent<MokaReveal> cut = Watching();

		Assert.Equal("moka-reveal moka-reveal--none", cut.Find(".moka-reveal").ClassName);
		Assert.Null(cut.Find(".moka-reveal").GetAttribute("style"));
		Assert.Single(_module.Invocations["observe"]);
		Assert.True((bool)Options()["leaves"]!);
	}

	[Fact]
	public void OnlyOnEnter_LeavesTheLeavesInTheBrowser()
	{
		Render<MokaReveal>(p => p
			.Add(x => x.Animation, MokaRevealAnimation.None)
			.Add(x => x.Once, false)
			.Add(x => x.OnEnter, () => _heard.Add("enter")));

		Assert.False((bool)Options()["leaves"]!);
	}

	[Fact]
	public async Task Crossings_RaiseEnterAndLeave()
	{
		IRenderedComponent<MokaReveal> cut = Watching(p => p.Add(x => x.Once, false));

		await cut.InvokeAsync(() => cut.Instance.OnVisibilityChanged(true));
		await cut.InvokeAsync(() => cut.Instance.OnVisibilityChanged(false));
		await cut.InvokeAsync(() => cut.Instance.OnVisibilityChanged(true));

		cut.WaitForAssertion(() => Assert.Equal(["enter", "leave", "enter"], _heard));
	}

	[Fact]
	public async Task NoAnimation_NeverMarksTheContentVisible()
	{
		IRenderedComponent<MokaReveal> cut = Watching();

		await cut.InvokeAsync(() => cut.Instance.OnVisibilityChanged(true));

		Assert.DoesNotContain("moka-reveal--visible", cut.Find(".moka-reveal").ClassList);
	}

	[Fact]
	public async Task AnAnimation_StillRevealsAndHides()
	{
		IRenderedComponent<MokaReveal> cut = Render<MokaReveal>(p => p.Add(x => x.Once, false));

		await cut.InvokeAsync(() => cut.Instance.OnVisibilityChanged(true));
		Assert.Contains("moka-reveal--visible", cut.Find(".moka-reveal").ClassList);

		await cut.InvokeAsync(() => cut.Instance.OnVisibilityChanged(false));
		Assert.DoesNotContain("moka-reveal--visible", cut.Find(".moka-reveal").ClassList);
	}

	// The browser stops watching by itself after the first time in view, so .NET neither asks it to nor watches again.
	[Fact]
	public async Task Once_StopsWatchingAfterTheFirstTimeInView_WithNoMoreCalls()
	{
		IRenderedComponent<MokaReveal> cut = Render<MokaReveal>();

		await cut.InvokeAsync(() => cut.Instance.OnVisibilityChanged(true));
		cut.Render(p => p.Add(x => x.Threshold, 0.5));
		await DisposeComponentsAsync();

		Assert.Single(_module.Invocations["observe"]);
		Assert.Empty(_module.Invocations["unobserve"]);
	}

	[Fact]
	public void ChangedOptions_WatchAgain_AndTheSameOnesDont()
	{
		IRenderedComponent<MokaReveal> cut = Watching();

		cut.Render(p => p.Add(x => x.RootMargin, "200px"));
		cut.Render(p => p.Add(x => x.RootMargin, "200px"));

		Assert.Equal(2, _module.Invocations["observe"].Count);
		Assert.Equal("200px", Options(1)["rootMargin"]);
	}

	[Fact]
	public void Debounce_AndThreshold_ReachTheBrowser_Checked()
	{
		Watching(p => p.Add(x => x.Debounce, 120).Add(x => x.Threshold, 3d));

		Assert.Equal(120, Options()["debounce"]);
		Assert.Equal(1d, Options()["threshold"]);
	}

	[Theory]
	[InlineData("200px", "200px")]
	[InlineData(" 10px 5% -20px 0px ", "10px 5% -20px 0px")]
	[InlineData(".5px", ".5px")]
	[InlineData("1em", "0px")]
	[InlineData("0", "0px")]
	[InlineData("200px; color: red", "0px")]
	[InlineData("1px 2px 3px 4px 5px", "0px")]
	[InlineData(null, "0px")]
	public void RootMargin_TakesOnlyWhatTheBrowserTakes(string? margin, string expected) =>
		Assert.Equal(expected, MokaReveal.ValidRootMargin(margin));

	[Fact]
	public async Task Disposing_StopsTheWatch_OnlyWhenThereWasOne()
	{
		Watching();
		Render<MokaReveal>(p => p.Add(x => x.Animation, MokaRevealAnimation.None));

		await DisposeComponentsAsync();

		Assert.Single(_module.Invocations["unobserve"]);
	}
}
