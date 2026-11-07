using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.AutoComplete;

namespace Moka.Red.Forms.Tests.Components;

// Answers can land in any order, and whichever arrived last used to win.
public class MokaAutoCompleteRaceTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

	private readonly Dictionary<string, TaskCompletionSource<IEnumerable<string>>> _searches = new(StringComparer.Ordinal);

	public MokaAutoCompleteRaceTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task ASlowOldAnswer_DoesNotReplaceANewerOne()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = RenderSearching();

		await TypeAndWaitForSearch(cut, "ab");
		await TypeAndWaitForSearch(cut, "abc");

		_searches["abc"].SetResult(["fresh answer"]);
		cut.WaitForAssertion(() => Assert.Contains("fresh answer", cut.Markup, StringComparison.Ordinal), Wait);

		_searches["ab"].SetResult(["stale answer"]);
		await Settle(cut);

		Assert.DoesNotContain("stale answer", cut.Markup, StringComparison.Ordinal);
		Assert.Contains("fresh answer", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public async Task AnOldSearchThatFails_IsIgnored()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = RenderSearching();

		await TypeAndWaitForSearch(cut, "ab");
		await TypeAndWaitForSearch(cut, "abc");

		_searches["ab"].SetException(new InvalidOperationException("the old search failed"));
		await Settle(cut);

		Assert.False(Renderer.UnhandledException.IsCompleted);
	}

	[Fact]
	public async Task TheLatestSearchFailing_StillReachesTheRenderer()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = RenderSearching();

		await TypeAndWaitForSearch(cut, "ab");
		_searches["ab"].SetException(new InvalidOperationException("the search failed"));

		Exception error = await Renderer.UnhandledException.WaitAsync(Wait, Xunit.TestContext.Current.CancellationToken);
		Assert.Equal("the search failed", error.Message);
	}

	[Fact]
	public async Task ASearchRunning_WhenTheTextGetsTooShort_DoesNotFillTheList()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = RenderSearching();

		await TypeAndWaitForSearch(cut, "ab");
		await cut.Find("input[role=combobox]").InputAsync(new ChangeEventArgs { Value = "" });

		_searches["ab"].SetResult(["stale answer"]);
		await Settle(cut);

		Assert.DoesNotContain("stale answer", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public async Task TheSpinner_StaysUntilTheLatestAnswer()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = RenderSearching();

		await TypeAndWaitForSearch(cut, "ab");
		await TypeAndWaitForSearch(cut, "abc");
		_searches["ab"].SetResult(["stale answer"]);
		await Settle(cut);

		Assert.NotEmpty(cut.FindAll(".moka-autocomplete-loading"));

		_searches["abc"].SetResult(["fresh answer"]);
		cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".moka-autocomplete-loading")), Wait);
	}

	[Fact]
	public async Task ASearchWaitingOnItsDebounce_DoesNotRunAfterAClear()
	{
		// The clear has to land inside the debounce. At 50 ms a loaded test run could miss it, and the search then
		// ran for good reason.
		IRenderedComponent<MokaAutoComplete<string>> cut = RenderSearching(500, p => p
			.Add(x => x.Clearable, true)
			.Add(x => x.Value, "Bern"));

		await cut.Find("input[role=combobox]").InputAsync(new ChangeEventArgs { Value = "abc" });
		await cut.Find(".moka-autocomplete-clear").ClickAsync(new MouseEventArgs());
		await Task.Delay(700, Xunit.TestContext.Current.CancellationToken);

		Assert.False(_searches.ContainsKey("abc"));
	}

	private IRenderedComponent<MokaAutoComplete<string>> RenderSearching(int debounce = 1,
		Action<ComponentParameterCollectionBuilder<MokaAutoComplete<string>>>? more = null) =>
		Render<MokaAutoComplete<string>>(p =>
		{
			p.Add(x => x.Debounce, debounce);
			p.Add(x => x.SearchFunc, text =>
			{
				TaskCompletionSource<IEnumerable<string>> search = new();
				_searches[text] = search;
				return search.Task;
			});
			more?.Invoke(p);
		});

	private async Task TypeAndWaitForSearch(IRenderedComponent<MokaAutoComplete<string>> cut, string text)
	{
		await cut.Find("input[role=combobox]").InputAsync(new ChangeEventArgs { Value = text });
		DateTime deadline = DateTime.UtcNow + Wait;
		while (!_searches.ContainsKey(text) && DateTime.UtcNow < deadline)
		{
			await Task.Delay(5, Xunit.TestContext.Current.CancellationToken);
		}

		Assert.True(_searches.ContainsKey(text));
	}

	// Long enough for a continuation on the renderer's thread to have run.
	private static Task Settle(IRenderedComponent<MokaAutoComplete<string>> cut) => cut.InvokeAsync(() => Task.Delay(30));
}
