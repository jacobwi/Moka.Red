using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.InfiniteScroll;

namespace Moka.Red.Data.Tests.Components;

// The one component in the library with no tests (known issue 4). Its whole job is deciding when to ask for
// more, and every one of those decisions was unguarded.
public class MokaInfiniteScrollTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Data/InfiniteScroll/MokaInfiniteScroll.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaInfiniteScrollTests()
	{
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
	}

	// scrollTop, scrollHeight, clientHeight: the bottom is scrollHeight - scrollTop - clientHeight away.
	private void Metrics(double scrollTop, double scrollHeight, double clientHeight) =>
		_module.Setup<double[]>("getScrollMetrics", _ => true)
			.SetResult([scrollTop, scrollHeight, clientHeight]);

	[Fact]
	public async Task NearTheBottom_AsksForMore()
	{
		int calls = 0;
		Metrics(900, 1200, 300);  // 0px from the bottom
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => calls++);

		await cut.Find(".moka-infinite-scroll").ScrollAsync(new EventArgs());

		Assert.Equal(1, calls);
	}

	[Fact]
	public async Task FarFromTheBottom_AsksForNothing()
	{
		int calls = 0;
		Metrics(0, 5000, 300);  // 4700px from the bottom, past the 200px default
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => calls++);

		await cut.Find(".moka-infinite-scroll").ScrollAsync(new EventArgs());

		Assert.Equal(0, calls);
	}

	// Threshold is a CSS length, and the number in it is what counts.
	[Fact]
	public async Task TheThresholdWidensTheTriggerZone()
	{
		int calls = 0;
		Metrics(0, 1000, 500);  // 500px from the bottom
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => calls++, threshold: "600px");

		await cut.Find(".moka-infinite-scroll").ScrollAsync(new EventArgs());

		Assert.Equal(1, calls);
	}

	[Fact]
	public async Task WhileLoading_AsksForNothing()
	{
		int calls = 0;
		Metrics(900, 1200, 300);
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => calls++, loading: true);

		await cut.Find(".moka-infinite-scroll").ScrollAsync(new EventArgs());

		Assert.Equal(0, calls);
	}

	[Fact]
	public async Task WithNothingLeft_AsksForNothing()
	{
		int calls = 0;
		Metrics(900, 1200, 300);
		IRenderedComponent<MokaInfiniteScroll> cut = Render(calls: () => calls++, hasMore: false);

		await cut.Find(".moka-infinite-scroll").ScrollAsync(new EventArgs());

		Assert.Equal(0, calls);
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

	// An exception out of OnLoadMore used to wedge the component for good, because the guard was never released.
	[Fact]
	public async Task AFailedLoad_DoesNotWedgeTheComponent()
	{
		int calls = 0;
		Metrics(900, 1200, 300);
		IRenderedComponent<MokaInfiniteScroll> cut = Render<MokaInfiniteScroll>(p => p
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>rows</p>")))
			.Add(x => x.OnLoadMore, EventCallback.Factory.Create(this, () =>
			{
				calls++;
				throw new InvalidOperationException("the page failed to load");
			})));

		IElement root = cut.Find(".moka-infinite-scroll");
		await Assert.ThrowsAsync<InvalidOperationException>(() => root.ScrollAsync(new EventArgs()));
		await Assert.ThrowsAsync<InvalidOperationException>(() => root.ScrollAsync(new EventArgs()));

		// The second scroll got through, so the guard was released after the first one threw.
		Assert.Equal(2, calls);
	}

	// Without a listener there is nothing to ask, so the component should not even measure.
	[Fact]
	public async Task WithNoListener_ItDoesNotMeasure()
	{
		Metrics(900, 1200, 300);
		IRenderedComponent<MokaInfiniteScroll> cut = Render<MokaInfiniteScroll>(p => p
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>rows</p>"))));

		await cut.Find(".moka-infinite-scroll").ScrollAsync(new EventArgs());

		Assert.Empty(_module.Invocations["getScrollMetrics"]);
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
