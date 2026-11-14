using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;
using TestContext = Xunit.TestContext;

namespace Moka.Red.Data.Tests.Components;

// The debounced reload ran in an async void timer callback, so a ServerData that threw had its exception rethrown on the
// thread pool, where nothing could catch it and the process ended: on Blazor Server, every circuit on the machine. A
// failed load now shows in the table with a way to try again, raised through OnLoadFailed, and reaches no error boundary.
public class MokaTableFailureTests : BunitContext
{
	private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

	public MokaTableFailureTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task AFailedReloadAfterSearching_ShowsInTheTable()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderFailingTable(p => p.Add(t => t.Searchable, true));

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "ada" });

		cut.WaitForAssertion(() => Assert.Contains("The server failed", cut.Find(".moka-table-failure").TextContent, StringComparison.Ordinal), Wait);
		Assert.Equal("alert", cut.Find(".moka-table-failure").GetAttribute("role"));
		Assert.Contains("Couldn't load the rows", cut.Find(".moka-table-failure-title").TextContent, StringComparison.Ordinal);
		Assert.False(Renderer.UnhandledException.IsCompleted);
		Assert.True(cut.Instance.HasLoadFailed);
	}

	[Fact]
	public async Task AFailedReloadAfterFiltering_ShowsInTheTable()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderFailingTable(
			p => p.Add(t => t.FilterControls, MokaTableFilterControls.Row),
			Columns(Name, Team with { Filterable = true }));

		await cut.Find("input[aria-label='Filter by Team']").InputAsync(new ChangeEventArgs { Value = "Research" });

		cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".moka-table-failure")), Wait);
		Assert.False(Renderer.UnhandledException.IsCompleted);
	}

	// A disposed DbContext throws this too, and that one must not vanish: the catch that kept the callback quiet when the
	// renderer went away took every ObjectDisposedException with it.
	[Fact]
	public async Task AnObjectDisposedExceptionFromTheServer_IsShownAndRaised()
	{
		Exception? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = RenderFailingTable(
			p => p.Add(t => t.Searchable, true).Add(t => t.OnLoadFailed, error => raised = error),
			failure: new ObjectDisposedException("DbContext"));

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "ada" });

		cut.WaitForAssertion(() => Assert.IsType<ObjectDisposedException>(raised), Wait);
		Assert.NotNull(cut.Find(".moka-table-failure"));
	}

	[Fact]
	public async Task AFailedReload_ReachesNoErrorBoundary()
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
		cut.WaitForAssertion(() => Assert.Equal(1, calls), Wait);

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "ada" });

		cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".moka-table-failure")), Wait);
		Assert.Empty(cut.FindAll("#boundary"));
	}

	[Fact]
	public async Task Retry_LoadsAgain_AndTheRowsComeBack()
	{
		int calls = 0;
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ChildContent, DefaultColumns)
			.Add(t => t.ServerData, _ => ++calls == 1
				? Task.FromException<MokaTableResult<Person>>(new InvalidOperationException("Down"))
				: Task.FromResult(Rows())));
		cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".moka-table-failure")), Wait);

		await cut.Find(".moka-table-failure button").ClickAsync(new MouseEventArgs());

		cut.WaitForAssertion(() => Assert.Equal(People.Length, DataRows(cut).Count), Wait);
		Assert.Empty(cut.FindAll(".moka-table-failure"));
		Assert.False(cut.Instance.HasLoadFailed);
	}

	[Fact]
	public void ErrorContent_TakesTheFailuresPlace_WithTheErrorAndRetry()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.ChildContent, DefaultColumns)
			.Add(t => t.ServerData, _ => Task.FromException<MokaTableResult<Person>>(new InvalidOperationException("Down")))
			.Add(t => t.ErrorContent, failure => builder =>
			{
				builder.OpenElement(0, "p");
				builder.AddAttribute(1, "class", "own-failure");
				builder.AddContent(2, failure.Error.Message);
				builder.CloseElement();
			}));

		cut.WaitForAssertion(() => Assert.Equal("Down", cut.Find(".own-failure").TextContent), Wait);
		Assert.Empty(cut.FindAll(".moka-table-failure"));
	}

	// The table has gone by the time the source answers, so there is nothing to show and nothing to redraw.
	[Fact]
	public async Task AFailureAfterTheTableHasGone_IsNotReported()
	{
		var answer = new TaskCompletionSource<MokaTableResult<Person>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var reloading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		int calls = 0;
		Exception? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Searchable, true)
			.Add(t => t.ChildContent, DefaultColumns)
			.Add(t => t.OnLoadFailed, error => raised = error)
			.Add(t => t.ServerData, _ =>
			{
				if (++calls == 1)
				{
					return Task.FromResult(Rows());
				}

				reloading.TrySetResult();
				return answer.Task;
			}));
		cut.WaitForAssertion(() => Assert.Equal(1, calls), Wait);

		await cut.Find(".moka-table-search input").InputAsync(new ChangeEventArgs { Value = "ada" });
		await reloading.Task.WaitAsync(Wait, TestContext.Current.CancellationToken);

		await DisposeComponentsAsync();
		answer.SetException(new InvalidOperationException("The server failed"));
		await Task.Delay(50, TestContext.Current.CancellationToken);

		Assert.False(Renderer.UnhandledException.IsCompleted);
		Assert.Null(raised);
	}

	private IRenderedComponent<MokaTable<Person>> RenderFailingTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>> configure,
		RenderFragment? columns = null,
		Exception? failure = null)
	{
		int calls = 0;
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.ServerData, FailAfterFirstLoad(() => ++calls, failure));
			p.Add(t => t.ChildContent, columns ?? DefaultColumns);
			configure(p);
		});
		cut.WaitForAssertion(() => Assert.Equal(People.Length, DataRows(cut).Count), Wait);
		return cut;
	}

	private static Func<MokaTableRequest, Task<MokaTableResult<Person>>> FailAfterFirstLoad(
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
