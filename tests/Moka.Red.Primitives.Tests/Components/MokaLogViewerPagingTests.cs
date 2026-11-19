using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Moka.Red.Primitives.LogViewer;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     Newest first, older lines paged in from the old end, and the count of rows put above the reader's that the
///     script moves the list past.
/// </summary>
public sealed class MokaLogViewerPagingTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Primitives/LogViewer/MokaLogViewer.razor.js";

	private static readonly DateTime At = new(2026, 11, 19, 9, 30, 0, DateTimeKind.Utc);

	private readonly BunitJSModuleInterop _module;
	private int _asked;
	private int _remoteTotal = 100;

	public MokaLogViewerPagingTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
	}

	private static List<MokaLogEntry> Lines(int from, int count) =>
	[
		.. Enumerable.Range(from, count).Select(i => new MokaLogEntry(At.AddSeconds(i), MokaLogLevel.Info, $"Line {i}", "Worker"))
	];

	private static IEnumerable<string> Messages(IRenderedComponent<MokaLogViewer> cut) =>
		cut.FindAll(".moka-log-viewer__message").Select(m => m.TextContent);

	private static IElement Body(IRenderedComponent<MokaLogViewer> cut) => cut.Find(".moka-log-viewer__body");

	private ValueTask<ItemsProviderResult<MokaLogEntry>> Provide(ItemsProviderRequest request)
	{
		_asked++;
		MokaLogEntry[] rows =
		[
			.. Enumerable.Range(request.StartIndex, Math.Max(0, Math.Min(request.Count, _remoteTotal - request.StartIndex)))
				.Select(i => new MokaLogEntry(At.AddSeconds(i), MokaLogLevel.Info, $"Remote line {i}"))
		];
		return ValueTask.FromResult(new ItemsProviderResult<MokaLogEntry>(rows, _remoteTotal));
	}

	[Fact]
	public void ByDefault_TheOldestLineIsFirst_WithNoOlderButtonAndNoCount()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.Entries, Lines(0, 3)));

		Assert.Equal(["Line 0", "Line 1", "Line 2"], Messages(cut));
		Assert.Empty(cut.FindAll(".moka-log-viewer__older"));
		Assert.DoesNotContain("moka-log-viewer--anchored", cut.Find(".moka-log-viewer").ClassList);

		cut.Render(p => p.Add(x => x.Entries, Lines(-2, 5)));

		Assert.False(Body(cut).HasAttribute("data-moka-above"));
	}

	[Fact]
	public void NewestFirst_PutsTheNewestLineAtTheTop()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Lines(0, 3))
			.Add(x => x.Order, MokaLogOrder.NewestFirst));

		Assert.Equal(["Line 2", "Line 1", "Line 0"], Messages(cut));
		Assert.Contains("moka-log-viewer--anchored", cut.Find(".moka-log-viewer").ClassList);
	}

	[Fact]
	public void NewestFirst_TrimsTheOldestLines_AtTheBottom()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Lines(0, 10))
			.Add(x => x.Order, MokaLogOrder.NewestFirst)
			.Add(x => x.MaxEntries, 3));

		Assert.Equal(["Line 9", "Line 8", "Line 7"], Messages(cut));
	}

	[Fact]
	public void NewestFirst_UnderVirtualize_HandsVirtualizeTheNewestFirst()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Lines(0, 1000))
			.Add(x => x.Order, MokaLogOrder.NewestFirst)
			.Add(x => x.Virtualize, true));

		ICollection<MokaLogEntry> items = cut.FindComponent<Virtualize<MokaLogEntry>>().Instance.Items!;
		Assert.Equal("Line 999", items.First().Message);
		Assert.Equal("Line 0", items.Last().Message);
	}

	[Fact]
	public void NewestFirst_CountsTheLinesThatArriveAboveTheReader()
	{
		List<MokaLogEntry> entries = Lines(0, 3);
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, entries)
			.Add(x => x.Order, MokaLogOrder.NewestFirst));
		Assert.False(Body(cut).HasAttribute("data-moka-above"));

		entries.AddRange(Lines(3, 2));
		cut.Render(p => p.Add(x => x.Entries, entries));
		Assert.Equal("1:2", Body(cut).GetAttribute("data-moka-above"));

		cut.Render(p => p.Add(x => x.Entries, entries));
		Assert.Equal("1:2", Body(cut).GetAttribute("data-moka-above"));

		entries.AddRange(Lines(5, 2));
		cut.Render(p => p.Add(x => x.Entries, entries));
		Assert.Equal("2:2", Body(cut).GetAttribute("data-moka-above"));
	}

	// A search or a level shows other rows; that isn't rows arriving, and the reader's place isn't kept for it.
	[Fact]
	public void AFilterChange_CountsNothing()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Lines(0, 5))
			.Add(x => x.Order, MokaLogOrder.NewestFirst)
			.Add(x => x.SearchText, "Line 1"));

		cut.Render(p => p.Add(x => x.SearchText, string.Empty));

		Assert.False(Body(cut).HasAttribute("data-moka-above"));
	}

	[Fact]
	public void HasOlder_PutsTheButtonAtTheTop_WhenOldestFirst()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Lines(0, 3))
			.Add(x => x.HasOlder, true)
			.Add(x => x.OnLoadOlder, () => { }));

		IElement first = Body(cut).FirstElementChild!;
		Assert.Contains("moka-log-viewer__older", first.ClassList);
		IElement button = first.QuerySelector("button")!;
		Assert.Equal("Show older lines", button.TextContent.Trim());
		Assert.Equal("button", button.GetAttribute("type"));
	}

	[Fact]
	public void HasOlder_PutsTheButtonAtTheBottom_WhenNewestFirst()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Lines(0, 3))
			.Add(x => x.Order, MokaLogOrder.NewestFirst)
			.Add(x => x.HasOlder, true)
			.Add(x => x.OnLoadOlder, () => { }));

		Assert.Contains("moka-log-viewer__older", Body(cut).LastElementChild!.ClassList);
		Assert.Contains("moka-log-viewer__row", Body(cut).FirstElementChild!.ClassList);
	}

	[Fact]
	public void HasOlder_WithNoEntries_StillShowsTheButtonBesideTheEmptyState()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.HasOlder, true)
			.Add(x => x.OnLoadOlder, () => { }));

		Assert.Single(cut.FindAll(".moka-log-viewer__older"));
		Assert.Single(cut.FindAll(".moka-log-viewer__empty"));
	}

	[Fact]
	public async Task TheButton_IsBusyWhileOnLoadOlderRuns_AndIgnoresMorePresses()
	{
		TaskCompletionSource loading = new(TaskCreationOptions.RunContinuationsAsynchronously);
		int loads = 0;
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Lines(0, 3))
			.Add(x => x.HasOlder, true)
			.Add(x => x.OnLoadOlder, () =>
			{
				loads++;
				return loading.Task;
			}));

		Task pressed = cut.Find(".moka-log-viewer__older button").ClickAsync(new MouseEventArgs());

		IElement busy = cut.Find(".moka-log-viewer__older button");
		Assert.Equal("true", busy.GetAttribute("aria-busy"));
		Assert.Equal("true", busy.GetAttribute("aria-disabled"));
		await cut.Find(".moka-log-viewer__older button").ClickAsync(new MouseEventArgs());
		Assert.Equal(1, loads);

		loading.SetResult();
		await pressed;

		IElement done = cut.Find(".moka-log-viewer__older button");
		Assert.False(done.HasAttribute("aria-busy"));
		Assert.False(done.HasAttribute("aria-disabled"));
	}

	[Fact]
	public void OlderLinesAddedAboveTheFirst_AreCounted_SoTheReaderKeepsTheirPlace()
	{
		IRenderedComponent<PagingHost> host = Render<PagingHost>();

		host.Find(".moka-log-viewer__older button").Click();

		Assert.Equal(["Line -5", "Line -4", "Line -3", "Line -2", "Line -1", "Line 0"],
			host.FindAll(".moka-log-viewer__message").Take(6).Select(m => m.TextContent));
		Assert.Equal("1:5", host.Find(".moka-log-viewer__body").GetAttribute("data-moka-above"));
		Assert.Equal(1, host.Instance.Loads);
	}

	[Fact]
	public void OnceTheStartIsIn_TheButtonGoes_AndFocusGoesToTheList()
	{
		IRenderedComponent<PagingHost> host = Render<PagingHost>(p => p.Add(x => x.Pages, 1));
		IElement body = host.Find(".moka-log-viewer__body");
		Assert.Equal("-1", body.GetAttribute("tabindex"));
		string reference = body.GetAttribute("blazor:elementReference")!;

		host.Find(".moka-log-viewer__older button").Click();

		Assert.Empty(host.FindAll(".moka-log-viewer__older"));
		JSRuntimeInvocation focus = Assert.Single(JSInterop.Invocations, i => i.Identifier == "Blazor._internal.domWrapper.focus");
		Assert.Equal(reference, Assert.IsType<ElementReference>(focus.Arguments[0]).Id);
	}

	[Fact]
	public void WhileMoreAreLeft_FocusStaysOnTheButton()
	{
		IRenderedComponent<PagingHost> host = Render<PagingHost>(p => p.Add(x => x.Pages, 3));

		host.Find(".moka-log-viewer__older button").Click();

		Assert.Single(host.FindAll(".moka-log-viewer__older"));
		Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "Blazor._internal.domWrapper.focus");
	}

	[Fact]
	public void WithOnLoadOlder_MaxEntriesAppliesOnlyWhenSet()
	{
		IRenderedComponent<MokaLogViewer> paged = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Lines(0, 600))
			.Add(x => x.OnLoadOlder, () => { }));
		IRenderedComponent<MokaLogViewer> capped = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Lines(0, 600))
			.Add(x => x.OnLoadOlder, () => { })
			.Add(x => x.MaxEntries, 100));

		Assert.Equal(600, paged.FindAll(".moka-log-viewer__row").Count);
		Assert.Equal(100, capped.FindAll(".moka-log-viewer__row").Count);
	}

	[Fact]
	public void WithAProvider_OlderLinesAreAskedFor_AndCountedAboveTheReader()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.ItemsProvider, Provide)
			.Add(x => x.HasOlder, true)
			.Add(x => x.OnLoadOlder, () => _remoteTotal += 40));
		int before = _asked;

		cut.Find(".moka-log-viewer__older button").Click();

		Assert.True(_asked > before);
		Assert.Equal("1:40", Body(cut).GetAttribute("data-moka-above"));
	}

	[Fact]
	public async Task WithAProvider_NewestFirst_NewRowsARefreshCounts_AreAboveTheReader()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.ItemsProvider, Provide)
			.Add(x => x.Order, MokaLogOrder.NewestFirst));

		_remoteTotal += 7;
		await cut.InvokeAsync(cut.Instance.RefreshDataAsync);

		Assert.Equal("1:7", Body(cut).GetAttribute("data-moka-above"));
	}

	// Oldest first, new rows from a refresh land at the bottom, under the reader.
	[Fact]
	public async Task WithAProvider_OldestFirst_ARefreshCountsNothingAbove()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.ItemsProvider, Provide)
			.Add(x => x.OnLoadOlder, () => { }));

		_remoteTotal += 7;
		await cut.InvokeAsync(cut.Instance.RefreshDataAsync);

		Assert.False(Body(cut).HasAttribute("data-moka-above"));
	}

	[Fact]
	public void NewestFirst_TheScriptFollowsTheTop()
	{
		Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Lines(0, 3))
			.Add(x => x.Order, MokaLogOrder.NewestFirst)
			.Add(x => x.Virtualize, true)
			.Add(x => x.VirtualRowHeight, 22f));

		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(_module.VerifyInvoke("watch").Arguments[1]);
		Assert.Equal(true, options["follow"]);
		Assert.Equal(true, options["newestFirst"]);
		Assert.Equal(22f, options["rowHeight"]);
	}

	// Without AutoScroll the script still loads, to keep the reader's place as rows arrive above them.
	[Fact]
	public void WithoutAutoScroll_NewestFirst_TheScriptOnlyKeepsPlace()
	{
		Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Lines(0, 3))
			.Add(x => x.Order, MokaLogOrder.NewestFirst)
			.Add(x => x.AutoScroll, false));

		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(_module.VerifyInvoke("watch").Arguments[1]);
		Assert.Equal(false, options["follow"]);
		Assert.Equal(true, options["newestFirst"]);
	}

	[Fact]
	public void ChangingTheOrder_WatchesAgain()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.Entries, Lines(0, 3)));

		cut.Render(p => p.Add(x => x.Order, MokaLogOrder.NewestFirst));

		Assert.Equal(2, _module.Invocations.Count(i => i.Identifier == "watch"));
		Assert.Equal(true, ((Dictionary<string, object?>)_module.Invocations.Last(i => i.Identifier == "watch").Arguments[1]!)["newestFirst"]);
	}

	/// <summary>A page that adds five older lines a press, from its own list, as an app does.</summary>
	public sealed class PagingHost : ComponentBase
	{
		private List<MokaLogEntry> _entries = Lines(0, 20);
		private int _pagesLeft;

		[Parameter]
		public int Pages { get; set; } = 3;

		public int Loads { get; private set; }

		protected override void OnInitialized() => _pagesLeft = Pages;

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenComponent<MokaLogViewer>(0);
			builder.AddComponentParameter(1, nameof(MokaLogViewer.Entries), _entries);
			builder.AddComponentParameter(2, nameof(MokaLogViewer.HasOlder), _pagesLeft > 0);
			builder.AddComponentParameter(3, nameof(MokaLogViewer.OnLoadOlder), EventCallback.Factory.Create(this, LoadOlder));
			builder.CloseComponent();
		}

		private void LoadOlder()
		{
			Loads++;
			_pagesLeft--;
			int oldest = int.Parse(_entries[0].Message["Line ".Length..], System.Globalization.CultureInfo.InvariantCulture);
			_entries = [.. Lines(oldest - 5, 5), .. _entries];
		}
	}
}
