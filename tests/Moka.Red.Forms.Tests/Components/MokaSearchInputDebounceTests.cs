using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Forms.SearchInput;

namespace Moka.Red.Forms.Tests.Components;

// Up to 0.4.2 the clear button left the debounce timer running, so text typed just before a clear was searched
// after the box was emptied, and that search overrode the clear's own OnSearch("").
public class MokaSearchInputDebounceTests : BunitContext
{
	private readonly ManualClock _clock = new();
	private readonly List<string> _searches = [];

	public MokaSearchInputDebounceTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<TimeProvider>(_clock);
	}

	[Fact]
	public async Task TypedText_IsSearchedOnceTheDelayEnds()
	{
		IRenderedComponent<MokaSearchInput> cut = Search();

		await TypeAsync(cut, "rep");
		Assert.Empty(_searches);

		await cut.InvokeAsync(_clock.FireAll);

		Assert.Equal(["rep"], _searches);
	}

	[Fact]
	public async Task OnlyTheLastKeystroke_IsSearched()
	{
		IRenderedComponent<MokaSearchInput> cut = Search();

		await TypeAsync(cut, "r");
		await TypeAsync(cut, "re");
		await TypeAsync(cut, "rep");
		await cut.InvokeAsync(_clock.FireAll);

		Assert.Equal(["rep"], _searches);
	}

	[Fact]
	public async Task Clear_DropsTheSearchStillWaiting()
	{
		IRenderedComponent<MokaSearchInput> cut = Search();

		await TypeAsync(cut, "rep");
		await cut.Find(".moka-search-clear").ClickAsync(new MouseEventArgs());
		await cut.InvokeAsync(_clock.FireAll);

		Assert.Equal([""], _searches);
	}

	// A timer that fired just before the clear has already queued its search on the renderer; disposing the
	// timer can't take that back.
	[Fact]
	public async Task Clear_DropsASearchThatAlreadyFired()
	{
		IRenderedComponent<MokaSearchInput> cut = Search();

		await TypeAsync(cut, "rep");
		TimerCallback fired = _clock.Timers.Single().Callback;
		await cut.Find(".moka-search-clear").ClickAsync(new MouseEventArgs());
		await cut.InvokeAsync(() => fired(null));

		Assert.Equal([""], _searches);
	}

	// Escape in a search box empties it in Chromium and Safari, which arrives as typing an empty value.
	[Fact]
	public async Task EmptyingTheBoxByTyping_DropsTheEarlierSearch()
	{
		IRenderedComponent<MokaSearchInput> cut = Search();

		await TypeAsync(cut, "rep");
		TimerCallback fired = _clock.Timers.Single().Callback;
		await TypeAsync(cut, "");
		await cut.InvokeAsync(() => fired(null));
		await cut.InvokeAsync(_clock.FireAll);

		Assert.Equal([""], _searches);
	}

	[Fact]
	public async Task TypingAfterAClear_IsSearchedAgain()
	{
		IRenderedComponent<MokaSearchInput> cut = Search();

		await TypeAsync(cut, "rep");
		await cut.Find(".moka-search-clear").ClickAsync(new MouseEventArgs());
		await TypeAsync(cut, "log");
		await cut.InvokeAsync(_clock.FireAll);

		Assert.Equal(["", "log"], _searches);
	}

	[Fact]
	public async Task AWaitingSearch_ReportsWhatTheParentPutInTheBox()
	{
		IRenderedComponent<MokaSearchInput> cut = Search(p => p.Add(x => x.Value, "old"));

		await TypeAsync(cut, "rep");
		cut.Render(p => p.Add(x => x.Value, "new"));
		await cut.InvokeAsync(_clock.FireAll);

		Assert.Equal("new", cut.Find("input").GetAttribute("value"));
		Assert.Equal(["new"], _searches);
	}

	[Fact]
	public async Task ABoundBox_StillSearchesWhatWasTyped()
	{
		string bound = "";
		IRenderedComponent<MokaSearchInput> cut = Search(p => p.Add(x => x.ValueChanged, value => bound = value));

		await TypeAsync(cut, "rep");
		cut.Render(p => p.Add(x => x.Value, bound));
		await cut.InvokeAsync(_clock.FireAll);

		Assert.Equal(["rep"], _searches);
	}

	[Fact]
	public async Task NoDebounce_SearchesAtOnce()
	{
		IRenderedComponent<MokaSearchInput> cut = Search(p => p.Add(x => x.Debounce, 0));

		await TypeAsync(cut, "rep");

		Assert.Equal(["rep"], _searches);
		Assert.Empty(_clock.Timers);
	}

	[Fact]
	public async Task Disposing_DropsTheSearchStillWaiting()
	{
		IRenderedComponent<MokaSearchInput> cut = Search();

		await TypeAsync(cut, "rep");
		TimerCallback fired = _clock.Timers.Single().Callback;
		await cut.Instance.DisposeAsync();
		await cut.InvokeAsync(() => fired(null));

		Assert.Empty(_searches);
		Assert.True(_clock.Timers.Single().Disposed);
	}

	private IRenderedComponent<MokaSearchInput> Search(
		Action<ComponentParameterCollectionBuilder<MokaSearchInput>>? parameters = null) => Render<MokaSearchInput>(p =>
	{
		p.Add(x => x.OnSearch, value => _searches.Add(value));
		parameters?.Invoke(p);
	});

	private static Task TypeAsync(IRenderedComponent<MokaSearchInput> cut, string text) =>
		cut.Find("input").InputAsync(new ChangeEventArgs { Value = text });

	private sealed class ManualClock : TimeProvider
	{
		public List<ManualTimer> Timers { get; } = [];

		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
		{
			ManualTimer timer = new(callback);
			Timers.Add(timer);
			return timer;
		}

		public void FireAll()
		{
			foreach (ManualTimer timer in Timers.ToArray())
			{
				timer.Fire();
			}
		}
	}

	private sealed class ManualTimer(TimerCallback callback) : ITimer
	{
		public TimerCallback Callback => callback;

		public bool Disposed { get; private set; }

		public void Fire()
		{
			if (!Disposed)
			{
				callback(null);
			}
		}

		public bool Change(TimeSpan dueTime, TimeSpan period) => true;

		public void Dispose() => Disposed = true;

		public ValueTask DisposeAsync()
		{
			Disposed = true;
			return ValueTask.CompletedTask;
		}
	}
}
