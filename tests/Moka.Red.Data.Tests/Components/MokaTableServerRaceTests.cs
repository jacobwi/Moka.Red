using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Data.Table;

namespace Moka.Red.Data.Tests.Components;

// Answers can land in any order, and whichever arrived last used to win, so a slow old page could overwrite a newer one.
public class MokaTableServerRaceTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

	private readonly List<Request> _requests = [];

	public MokaTableServerRaceTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task ASlowOldAnswer_DoesNotReplaceANewerOne()
	{
		IRenderedComponent<MokaTable<Row>> cut = await RenderLoadedTable();
		Task older = await Reload(cut);
		Task newer = await Reload(cut);

		_requests[2].Answer("fresh answer");
		await newer;
		_requests[1].Answer("stale answer");
		await older;

		Assert.Contains("fresh answer", cut.Markup, StringComparison.Ordinal);
		Assert.DoesNotContain("stale answer", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ANewerRequest_CancelsTheOneBefore()
	{
		IRenderedComponent<MokaTable<Row>> cut = await RenderLoadedTable();
		await Reload(cut);
		CancellationToken older = _requests[1].State.CancellationToken;
		Assert.False(older.IsCancellationRequested);

		await Reload(cut);

		Assert.True(older.IsCancellationRequested);
		Assert.False(_requests[2].State.CancellationToken.IsCancellationRequested);
	}

	[Fact]
	public async Task GoingAway_CancelsTheRequest()
	{
		IRenderedComponent<MokaTable<Row>> cut = RenderTable();
		await Until(() => _requests.Count == 1);

		await cut.Instance.DisposeAsync();

		Assert.True(_requests[0].State.CancellationToken.IsCancellationRequested);
	}

	[Fact]
	public async Task ARequestThatHonoursItsToken_IsNotAFailure()
	{
		IRenderedComponent<MokaTable<Row>> cut = await RenderLoadedTable();
		Task older = await Reload(cut);
		Task newer = await Reload(cut);

		_requests[1].Cancel();
		_requests[2].Answer("fresh answer");
		await older;
		await newer;

		Assert.False(Renderer.UnhandledException.IsCompleted);
		Assert.Contains("fresh answer", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public async Task AnOldRequestThatFails_IsIgnored()
	{
		IRenderedComponent<MokaTable<Row>> cut = await RenderLoadedTable();
		Task older = await Reload(cut);
		Task newer = await Reload(cut);

		_requests[1].Fail(new InvalidOperationException("the old page failed"));
		_requests[2].Answer("fresh answer");
		await older;
		await newer;

		Assert.False(Renderer.UnhandledException.IsCompleted);
		Assert.Contains("fresh answer", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public async Task TheLatestRequestFailing_StillFails()
	{
		IRenderedComponent<MokaTable<Row>> cut = await RenderLoadedTable();
		Task reload = await Reload(cut);

		_requests[1].Fail(new InvalidOperationException("the page failed"));

		await Assert.ThrowsAsync<InvalidOperationException>(() => reload);
	}

	[Fact]
	public async Task TheLoadingState_LastsUntilTheLatestAnswer()
	{
		IRenderedComponent<MokaTable<Row>> cut = await RenderLoadedTable();
		Task older = await Reload(cut);
		Task newer = await Reload(cut);

		_requests[1].Answer("stale answer");
		await older;

		Assert.NotEmpty(cut.FindAll(".moka-table-skeleton-row"));

		_requests[2].Answer("fresh answer");
		await newer;

		Assert.Empty(cut.FindAll(".moka-table-skeleton-row"));
	}

	[Fact]
	public async Task WithThePagerHidden_AnOldAnswerAsksForNothingMore()
	{
		// An answer reporting more rows than were asked for normally gets a follow-up request.
		IRenderedComponent<MokaTable<Row>> cut = await RenderLoadedTable(showPagination: false);
		Task older = await Reload(cut);
		Task newer = await Reload(cut);
		int asked = _requests.Count;

		_requests[1].Answer("stale answer", totalItems: 1_000_000);
		await older;

		Assert.Equal(asked, _requests.Count);

		_requests[2].Answer("fresh answer");
		await newer;
		Assert.Contains("fresh answer", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ANormalLoad_HasATokenNothingHasCancelled()
	{
		await RenderLoadedTable();

		Assert.True(_requests[0].State.CancellationToken.CanBeCanceled);
		Assert.False(_requests[0].State.CancellationToken.IsCancellationRequested);
	}

	private async Task<IRenderedComponent<MokaTable<Row>>> RenderLoadedTable(bool showPagination = true)
	{
		IRenderedComponent<MokaTable<Row>> cut = RenderTable(showPagination);
		await Until(() => _requests.Count == 1);
		_requests[0].Answer("first");
		cut.WaitForAssertion(() => Assert.Contains("first", cut.Markup, StringComparison.Ordinal), Wait);
		return cut;
	}

	private IRenderedComponent<MokaTable<Row>> RenderTable(bool showPagination = true) =>
		Render<MokaTable<Row>>(p => p
			.Add(x => x.ShowPagination, showPagination)
			.Add(x => x.ServerData, state =>
			{
				Request request = new(state);
				_requests.Add(request);
				return request.Answered;
			})
			.Add(x => x.ChildContent, (RenderFragment)(b =>
			{
				b.OpenComponent<MokaColumn<Row>>(0);
				b.AddComponentParameter(1, nameof(MokaColumn<Row>.Title), "Name");
				b.AddComponentParameter(2, nameof(MokaColumn<Row>.Field), (Func<Row, object?>)(r => r.Name));
				b.CloseComponent();
			})));

	// The dispatcher may queue the call, so the request isn't always there when InvokeAsync returns.
	private async Task<Task> Reload(IRenderedComponent<MokaTable<Row>> cut)
	{
		int asked = _requests.Count;
		Task reload = cut.InvokeAsync(cut.Instance.ReloadAsync);
		await Until(() => _requests.Count > asked);
		return reload;
	}

	private static async Task Until(Func<bool> condition)
	{
		DateTime deadline = DateTime.UtcNow + Wait;
		while (!condition() && DateTime.UtcNow < deadline)
		{
			await Task.Delay(5, Xunit.TestContext.Current.CancellationToken);
		}

		Assert.True(condition());
	}

	public sealed record Row(string Name);

	private sealed class Request(MokaTableState state)
	{
		private readonly TaskCompletionSource<MokaTableResult<Row>> _answer = new();

		public MokaTableState State { get; } = state;

		public Task<MokaTableResult<Row>> Answered => _answer.Task;

		public void Answer(string name, int totalItems = 1) =>
			_answer.SetResult(new MokaTableResult<Row> { Items = [new Row(name)], TotalItems = totalItems });

		public void Cancel() => _answer.SetCanceled();

		public void Fail(Exception failure) => _answer.SetException(failure);
	}
}
