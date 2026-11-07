using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.InfiniteScroll;

namespace Moka.Red.Data.Tests.Components;

// The observer lives in the browser, so these cover what .NET does with its report and when it asks for one.
public class MokaInfiniteScrollTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Data/InfiniteScroll/MokaInfiniteScroll.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaInfiniteScrollTests()
	{
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void ItStartsWatching_OnceItHasAListener()
	{
		Render(calls: () => { });

		Assert.Single(_module.Invocations["watchSentinel"]);
	}

	[Fact]
	public void ScrollingSendsNothingToDotNet()
	{
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => { });

		Assert.False(cut.Find(".moka-infinite-scroll").HasAttribute("blazor:onscroll"));
	}

	[Fact]
	public void WithNoListener_ItDoesNotWatch()
	{
		Render<MokaInfiniteScroll>(p => p
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>rows</p>"))));

		Assert.Empty(_module.Invocations["watchSentinel"]);
	}

	[Fact]
	public void WhileLoading_ItDoesNotWatch()
	{
		Render(calls: () => { }, loading: true);

		Assert.Empty(_module.Invocations["watchSentinel"]);
	}

	[Fact]
	public void WithNothingLeft_ItDoesNotWatch()
	{
		Render(calls: () => { }, hasMore: false);

		Assert.Empty(_module.Invocations["watchSentinel"]);
	}

	[Fact]
	public void TheSentinelInView_AsksForMore()
	{
		int calls = 0;
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => calls++);

		cut.InvokeAsync(cut.Instance.OnSentinelVisible);

		cut.WaitForAssertion(() => Assert.Equal(1, calls));
	}

	[Fact]
	public void AfterALoad_ItWatchesAgain()
	{
		// That's what fills a view the first page didn't: a new watch reports the sentinel again if it's still in view.
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => { });

		cut.InvokeAsync(cut.Instance.OnSentinelVisible);

		cut.WaitForAssertion(() => Assert.Equal(2, _module.Invocations["watchSentinel"].Count));
	}

	[Fact]
	public void StartingToLoad_StopsWatching_AndFinishingStartsAgain()
	{
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => { });

		cut.Render(p => p.Add(x => x.Loading, true));
		Assert.Single(_module.Invocations["unwatchSentinel"]);

		cut.Render(p => p.Add(x => x.Loading, false));
		Assert.Equal(2, _module.Invocations["watchSentinel"].Count);
	}

	[Fact]
	public void TheLastPage_StopsWatching()
	{
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => { });

		cut.Render(p => p.Add(x => x.HasMore, false));

		Assert.Single(_module.Invocations["unwatchSentinel"]);
	}

	// A report can cross with a render that changed the answer.
	[Fact]
	public void AReportThatArrivesWhileLoading_AsksForNothing()
	{
		int calls = 0;
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => calls++, loading: true);

		cut.InvokeAsync(cut.Instance.OnSentinelVisible);

		Assert.Equal(0, calls);
	}

	[Fact]
	public void OnlyOneLoadRunsAtATime()
	{
		int calls = 0;
		TaskCompletionSource running = new();
		IRenderedComponent<MokaInfiniteScroll> cut = Render<MokaInfiniteScroll>(p => p
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>rows</p>")))
			.Add(x => x.OnLoadMore, EventCallback.Factory.Create(this, () =>
			{
				calls++;
				return running.Task;
			})));

		cut.InvokeAsync(cut.Instance.OnSentinelVisible);
		cut.InvokeAsync(cut.Instance.OnSentinelVisible);
		running.SetResult();

		cut.WaitForAssertion(() => Assert.Equal(1, calls));
	}

	[Fact]
	public void TheThresholdSetsHowFarTheSentinelReaches()
	{
		IRenderedComponent<MokaInfiniteScroll> cut = Render(threshold: "12rem");

		Assert.Contains("height: 12rem", cut.Find(".moka-infinite-scroll__reach").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void AThresholdThatCouldEndItsDeclaration_IsDropped()
	{
		IRenderedComponent<MokaInfiniteScroll> cut = Render(threshold: "1px;color:red");

		Assert.DoesNotContain("color", cut.Find(".moka-infinite-scroll__reach").GetAttribute("style") ?? "", StringComparison.Ordinal);
	}

	// The sentinel is what the spinner lives in, and it goes when the list is complete.
	[Fact]
	public void TheSentinelGoesWithTheLastPage()
	{
		Assert.NotEmpty(Render().FindAll(".moka-infinite-scroll__sentinel"));
		Assert.Empty(Render(hasMore: false).FindAll(".moka-infinite-scroll__sentinel"));
	}

	[Fact]
	public void LoadingShowsTheSpinnerAndMarksTheRoot()
	{
		IRenderedComponent<MokaInfiniteScroll> cut = Render(loading: true);

		Assert.NotEmpty(cut.FindAll(".moka-infinite-scroll__spinner"));
		Assert.Contains("moka-infinite-scroll--loading", cut.Find(".moka-infinite-scroll").ClassList);
	}

	[Fact]
	public void ALoadingTemplateReplacesTheSpinner()
	{
		IRenderedComponent<MokaInfiniteScroll> cut = Render<MokaInfiniteScroll>(p => p
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>rows</p>")))
			.Add(x => x.Loading, true)
			.Add(x => x.LoadingTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<p id=\"mine\">Loading</p>"))));

		Assert.Empty(cut.FindAll(".moka-infinite-scroll__spinner"));
		Assert.NotNull(cut.Find("#mine"));
	}

	// Nothing awaits a report from the browser, so what the handler throws has to go to the renderer on its own.
	[Fact]
	public async Task AFailedLoad_ReachesTheRenderer_AndDoesNotWedgeTheComponent()
	{
		int calls = 0;
		IRenderedComponent<MokaInfiniteScroll> cut = Render<MokaInfiniteScroll>(p => p
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>rows</p>")))
			.Add(x => x.OnLoadMore, EventCallback.Factory.Create(this, () =>
			{
				calls++;
				throw new InvalidOperationException("the page failed to load");
			})));

		await cut.InvokeAsync(cut.Instance.OnSentinelVisible);

		Exception error = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
		Assert.Equal("the page failed to load", error.Message);

		await cut.InvokeAsync(cut.Instance.OnSentinelVisible);
		Assert.Equal(2, calls);
	}

	[Fact]
	public async Task Disposing_ForgetsTheWatch()
	{
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => { });

		await cut.Instance.DisposeAsync();

		Assert.Single(_module.Invocations["forgetSentinel"]);
	}

	private IRenderedComponent<MokaInfiniteScroll> Render(
		Action? calls = null,
		bool loading = false,
		bool hasMore = true,
		string threshold = "200px") =>
		Render<MokaInfiniteScroll>(p =>
		{
			p.Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>rows</p>")));
			p.Add(x => x.Loading, loading);
			p.Add(x => x.HasMore, hasMore);
			p.Add(x => x.Threshold, threshold);
			if (calls is not null)
			{
				p.Add(x => x.OnLoadMore, EventCallback.Factory.Create(this, calls));
			}
		});
}
