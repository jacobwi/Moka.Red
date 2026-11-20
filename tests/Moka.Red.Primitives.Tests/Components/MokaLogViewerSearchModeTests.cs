using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Primitives.LogViewer;
using Moka.Red.Primitives.Tooltip;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>When the search runs: on every key, once typing pauses, or when it's sent.</summary>
public sealed class MokaLogViewerSearchModeTests : BunitContext
{
	private static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(300);

	private static readonly DateTime At = new(2026, 11, 19, 9, 30, 0, DateTimeKind.Utc);

	private static readonly IReadOnlyList<MokaLogEntry> Entries =
	[
		new(At, MokaLogLevel.Info, "Application started.", "Host"),
		new(At.AddSeconds(1), MokaLogLevel.Warning, "Cache miss", "Cache"),
		new(At.AddSeconds(2), MokaLogLevel.Error, "Gateway timeout", "Payments"),
		new(At.AddSeconds(3), MokaLogLevel.Fatal, "Out of memory", "Runtime")
	];

	private readonly FakeClock _clock = new();
	private readonly List<string> _asked = [];
	private readonly List<string> _reported = [];
	private string _search = string.Empty;

	public MokaLogViewerSearchModeTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<TimeProvider>(_clock);
	}

	private static IEnumerable<string> Messages(IRenderedComponent<MokaLogViewer> cut) =>
		cut.FindAll(".moka-log-viewer__message").Select(m => m.TextContent);

	private IRenderedComponent<MokaLogViewer> Debounced() => Render<MokaLogViewer>(p => p
		.Add(x => x.Entries, Entries)
		.Add(x => x.SearchDebounce, Pause)
		.Add(x => x.SearchTextChanged, (string text) => _reported.Add(text)));

	private IRenderedComponent<MokaLogViewer> OnSubmit() => Render<MokaLogViewer>(p => p
		.Add(x => x.Entries, Entries)
		.Add(x => x.SearchMode, MokaLogSearchMode.OnSubmit)
		.Add(x => x.SearchTextChanged, (string text) => _reported.Add(text)));

	// On the renderer's thread, so the search a timer starts has run by the time this returns.
	private Task AdvanceAsync(IRenderedComponent<MokaLogViewer> cut, TimeSpan by) => cut.InvokeAsync(() => _clock.Advance(by));

	private static Task TypeAsync(IRenderedComponent<MokaLogViewer> cut, string text) =>
		cut.Find(".moka-log-viewer__search-input").InputAsync(new ChangeEventArgs { Value = text });

	private ValueTask<ItemsProviderResult<MokaLogEntry>> Provide(ItemsProviderRequest request)
	{
		_asked.Add(_search);
		MokaLogEntry[] rows = [.. Entries.Where(entry => entry.Message.Contains(_search, StringComparison.OrdinalIgnoreCase))];
		return ValueTask.FromResult(new ItemsProviderResult<MokaLogEntry>(rows.Skip(request.StartIndex).Take(request.Count), rows.Length));
	}

	[Fact]
	public void ByDefault_EveryKeySearches_AndNoTimerWaits()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.SearchTextChanged, (string text) => _reported.Add(text)));

		cut.Find(".moka-log-viewer__search-input").Input("g");
		cut.Find(".moka-log-viewer__search-input").Input("ga");

		Assert.Equal(["g", "ga"], _reported);
		Assert.Equal(["Gateway timeout"], Messages(cut));
		Assert.Empty(_clock.Timers);
		Assert.Equal("DIV", cut.Find(".moka-log-viewer__search").TagName);
	}

	[Fact]
	public async Task Debounced_SearchesOnceTypingPauses()
	{
		IRenderedComponent<MokaLogViewer> cut = Debounced();

		await TypeAsync(cut, "mem");
		await AdvanceAsync(cut, Pause - TimeSpan.FromMilliseconds(1));

		Assert.Empty(_reported);
		Assert.Equal(4, Messages(cut).Count());

		await AdvanceAsync(cut, TimeSpan.FromMilliseconds(1));

		cut.WaitForAssertion(() => Assert.Equal(["Out of memory"], Messages(cut)));
		Assert.Equal(["mem"], _reported);
	}

	[Fact]
	public async Task Debounced_EveryKeyStartsTheWaitAgain_AndOnlyTheLastSearchRuns()
	{
		IRenderedComponent<MokaLogViewer> cut = Debounced();

		await TypeAsync(cut, "m");
		await AdvanceAsync(cut, TimeSpan.FromMilliseconds(200));
		await TypeAsync(cut, "mi");
		await AdvanceAsync(cut, TimeSpan.FromMilliseconds(200));
		Assert.Empty(_reported);

		await AdvanceAsync(cut, TimeSpan.FromMilliseconds(100));

		cut.WaitForAssertion(() => Assert.Equal(["mi"], _reported));
		Assert.Equal(["Cache miss"], Messages(cut));
		Assert.All(_clock.Timers, timer => Assert.True(timer.Disposed));
	}

	// A callback that fired just before a newer key arrived is let go: it carries the number it was started under.
	[Fact]
	public async Task Debounced_AWaitAlreadyOverwritten_DoesNothing()
	{
		IRenderedComponent<MokaLogViewer> cut = Debounced();
		await TypeAsync(cut, "c");
		FakeTimer first = _clock.Timers[^1];
		await TypeAsync(cut, "ca");

		await cut.InvokeAsync(first.InvokeCallback);

		Assert.Empty(_reported);
	}

	[Fact]
	public async Task Debounced_TypingDoesntRender()
	{
		IRenderedComponent<MokaLogViewer> cut = Debounced();
		int renders = cut.RenderCount;

		await TypeAsync(cut, "c");
		await TypeAsync(cut, "ca");

		Assert.Equal(renders, cut.RenderCount);
	}

	[Fact]
	public async Task Debounced_ASearchFromTheParent_DropsTheWait()
	{
		IRenderedComponent<MokaLogViewer> cut = Debounced();
		await TypeAsync(cut, "cache");

		cut.Render(p => p.Add(x => x.SearchText, "gateway"));
		await AdvanceAsync(cut, Pause);

		Assert.Empty(_reported);
		Assert.Equal(["Gateway timeout"], Messages(cut));
		Assert.Equal("gateway", cut.Find(".moka-log-viewer__search-input").GetAttribute("value"));
	}

	[Fact]
	public async Task Debounced_Disposing_StopsTheWait()
	{
		IRenderedComponent<MokaLogViewer> cut = Debounced();
		await TypeAsync(cut, "cache");

		await DisposeComponentsAsync();

		Assert.True(Assert.Single(_clock.Timers).Disposed);
	}

	[Fact]
	public void OnSubmit_IsASearchFormWithANamedButton()
	{
		IRenderedComponent<MokaLogViewer> cut = OnSubmit();

		IElement form = cut.Find(".moka-log-viewer__search");
		Assert.Equal("FORM", form.TagName);
		Assert.Equal("search", form.GetAttribute("role"));
		Assert.Equal("search", form.QuerySelector("input")!.GetAttribute("enterkeyhint"));
		IElement button = form.QuerySelector("button.moka-log-viewer__submit")!;
		Assert.Equal("submit", button.GetAttribute("type"));
		IconTooltip.AssertNamed(button, "Search", MokaTooltipPosition.BottomEnd);
	}

	[Fact]
	public async Task OnSubmit_TypingSearchesNothing_AndReportsNothing()
	{
		IRenderedComponent<MokaLogViewer> cut = OnSubmit();
		int renders = cut.RenderCount;

		await TypeAsync(cut, "time");

		Assert.Empty(_reported);
		Assert.Equal(4, Messages(cut).Count());
		Assert.Equal(renders, cut.RenderCount);
		Assert.Empty(_clock.Timers);
	}

	[Fact]
	public void OnSubmit_SendingSearches_AndReportsWhatWasSent()
	{
		IRenderedComponent<MokaLogViewer> cut = OnSubmit();
		cut.Find(".moka-log-viewer__search-input").Input("timeout");

		cut.Find("form").Submit();

		Assert.Equal(["timeout"], _reported);
		Assert.Equal(["Gateway timeout"], Messages(cut));
	}

	[Fact]
	public void OnSubmit_AProviderIsAskedOnlyWhenTheSearchIsSent()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.ItemsProvider, Provide)
			.Add(x => x.SearchMode, MokaLogSearchMode.OnSubmit)
			.Add(x => x.SearchText, _search)
			.Add(x => x.SearchTextChanged, (string text) => _search = text));
		int before = _asked.Count;

		cut.Find(".moka-log-viewer__search-input").Input("memory");
		Assert.Equal(before, _asked.Count);

		cut.Find("form").Submit();

		Assert.Equal("memory", _asked[^1]);
		Assert.Equal(["Out of memory"], Messages(cut));
	}

	[Fact]
	public void OnSubmit_TheButtonIsNamedByText()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.SearchMode, MokaLogSearchMode.OnSubmit)
			.Add(x => x.Text, new MokaLogViewerText { SearchButton = "Suchen" }));

		IconTooltip.AssertNamed(cut.Find(".moka-log-viewer__submit"), "Suchen", MokaTooltipPosition.BottomEnd);
	}
}
