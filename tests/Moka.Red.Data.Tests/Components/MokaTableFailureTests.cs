using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;
using TestContext = Xunit.TestContext;

namespace Moka.Red.Data.Tests.Components;

// The debounced reload ran in an async void timer callback, so a ServerData that threw had its exception
// rethrown on the thread pool, where nothing could catch it and the process ended: on Blazor Server, every
// circuit on the machine. On that code these tests do not fail, they take the test host down with them.
public class MokaTableFailureTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

	public MokaTableFailureTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task AFailedReloadAfterSearching_ReachesTheRenderer()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderFailingTable(p => p.Add(t => t.Searchable, true));

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "ada" });

		Exception error = await Renderer.UnhandledException.WaitAsync(Wait, TestContext.Current.CancellationToken);
		Assert.Equal("The server failed", error.Message);
	}

	[Fact]
	public async Task AFailedReloadAfterFiltering_ReachesTheRenderer()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderFailingTable(
			p => p.Add(t => t.ShowFilters, true),
			Columns(Name, Team with { Filterable = true }));

		await cut.Find("input[aria-label='Filter by Team']").InputAsync(new ChangeEventArgs { Value = "Research" });

		Exception error = await Renderer.UnhandledException.WaitAsync(Wait, TestContext.Current.CancellationToken);
		Assert.Equal("The server failed", error.Message);
	}

	// A disposed DbContext throws this too, and that one must not vanish: the catch that kept the callback
	// quiet when the renderer went away took every ObjectDisposedException with it.
	[Fact]
	public async Task AnObjectDisposedExceptionFromTheServer_StillReachesTheRenderer()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderFailingTable(
			p => p.Add(t => t.Searchable, true),
			failure: new ObjectDisposedException("DbContext"));

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "ada" });

		Exception error = await Renderer.UnhandledException.WaitAsync(Wait, TestContext.Current.CancellationToken);
		Assert.IsType<ObjectDisposedException>(error);
	}

	[Fact]
	public async Task AFailedReload_ShowsTheErrorBoundary()
	{
		int calls = 0;
		IRenderedComponent<ErrorBoundary> cut = Render<ErrorBoundary>(p => p
			.Add(b => b.ErrorContent, error => builder =>
			{
				builder.OpenElement(0, "p");
				builder.AddAttribute(1, "id", "boundary");
				builder.AddContent(2, error.Message);
				builder.CloseElement();
			})
			.Add(b => b.ChildContent, builder =>
			{
				builder.OpenComponent<MokaTable<Person>>(0);
				builder.AddComponentParameter(1, nameof(MokaTable<Person>.Searchable), true);
				builder.AddComponentParameter(2, nameof(MokaTable<Person>.ServerData), FailAfterFirstLoad(() => ++calls));
				builder.AddComponentParameter(3, nameof(MokaTable<Person>.ChildContent), DefaultColumns);
				builder.CloseComponent();
			}));

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "ada" });

		cut.WaitForAssertion(() => Assert.Equal("The server failed", cut.Find("#boundary").TextContent), Wait);
	}

	// The table has gone by the time the source answers, so there is no boundary to tell and nothing to redraw.
	[Fact]
	public async Task AFailureAfterTheTableHasGone_IsNotReported()
	{
		var answer = new TaskCompletionSource<MokaTableResult<Person>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var reloading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		int calls = 0;
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Searchable, true)
			.Add(t => t.ChildContent, DefaultColumns)
			.Add(t => t.ServerData, _ =>
			{
				if (++calls == 1)
				{
					return Task.FromResult(Rows());
				}

				reloading.TrySetResult();
				return answer.Task;
			}));

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "ada" });
		await reloading.Task.WaitAsync(Wait, TestContext.Current.CancellationToken);

		await DisposeComponentsAsync();
		answer.SetException(new InvalidOperationException("The server failed"));
		await Task.Delay(50, TestContext.Current.CancellationToken);

		Assert.False(Renderer.UnhandledException.IsCompleted);
	}

	private IRenderedComponent<MokaTable<Person>> RenderFailingTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>> configure,
		RenderFragment? columns = null,
		Exception? failure = null)
	{
		int calls = 0;
		return Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.ServerData, FailAfterFirstLoad(() => ++calls, failure));
			p.Add(t => t.ChildContent, columns ?? DefaultColumns);
			configure(p);
		});
	}

	private static Func<MokaTableState, Task<MokaTableResult<Person>>> FailAfterFirstLoad(
		Func<int> nextCall, Exception? failure = null) =>
		async _ =>
		{
			if (nextCall() == 1)
			{
				return Rows();
			}

			await Task.Yield();
			throw failure ?? new InvalidOperationException("The server failed");
		};

	private static MokaTableResult<Person> Rows() => new() { Items = People, TotalItems = People.Length };
}
