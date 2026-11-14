using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

#pragma warning disable BL0006 // Counting render batches is the point: bUnit does not expose them.
#pragma warning disable CA2000 // The table owns each stand-in module and disposes it.

namespace Moka.Red.Data.Tests.Components;

// What a render of the table costs on the wire. A table of 25 rows by 5 columns with a handler on every cell
// sent 317 edits and retired 255 handler ids on any render, even one that changed nothing: 12.4 KB on Blazor
// Server, per click or keystroke, growing with rows times columns. bUnit hides the render batches, so these
// count them with a renderer of their own. Budgets, so a change that brings the cost back fails here.
public sealed class MokaTableRenderBudgetTests : IAsyncDisposable
{
	private readonly CountingRenderer _renderer;
	private readonly ServiceProvider _services;

	public MokaTableRenderBudgetTests()
	{
		_services = new ServiceCollection()
			.AddSingleton<IJSRuntime>(new SilentJs())
			.AddLogging()
			.BuildServiceProvider();
		_renderer = new CountingRenderer(_services, NullLoggerFactory.Instance);
	}

	// Each column's sort handler is made once, like the rows', so a render that changes nothing sends nothing.
	[Fact]
	public async Task ARenderWhereNothingChanged_StaysWithinItsBudget()
	{
		Host host = await _renderer.MountAsync(Table());

		await _renderer.Measure(host.RenderAgainAsync);

		Assert.True(_renderer.DisposedHandlers == 0, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits == 0, $"{_renderer.Edits} edits.");
	}

	// Every row drawn, with nothing changed: the diff walks the rows but sends nothing new, so the edits and the handlers
	// a render retires stay where they are whatever the row count. The numbers are written to the test output.
	[Theory]
	[InlineData(25)]
	[InlineData(1_000)]
	[InlineData(10_000)]
	public async Task ARenderWhereNothingChanged_DoesNotGrowWithTheRows(int rows)
	{
		Stopwatch first = Stopwatch.StartNew();
		Host host = await _renderer.MountAsync(Table(count: rows, paged: false));
		first.Stop();

		Stopwatch again = Stopwatch.StartNew();
		await _renderer.Measure(host.RenderAgainAsync);
		again.Stop();

		TestContext.Current.TestOutputHelper?.WriteLine(
			$"{rows} rows: first render {first.ElapsedMilliseconds} ms; unchanged re-render {_renderer.Edits} edits, " +
			$"{_renderer.DisposedHandlers} handlers retired, {again.ElapsedMilliseconds} ms");
		Assert.True(_renderer.DisposedHandlers == 0, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits == 0, $"{_renderer.Edits} edits.");
	}

	// The pager part and the column kinds are opt-in: a table with a pager part costs a numbered table's re-render.
	[Fact]
	public async Task APagerPart_AddsNothingToARender()
	{
		Host host = await _renderer.MountAsync(Table(count: 100, withPager: true));

		await _renderer.Measure(host.RenderAgainAsync);

		// The entries pager's page size list is bound, and @bind makes its handler again on each render.
		Assert.True(_renderer.DisposedHandlers <= 1, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits <= 7, $"{_renderer.Edits} edits.");
	}

	// Pinned columns, the column menu, heading filter buttons, chips and quick filters: what a render that changes nothing
	// costs stays the same whatever the row count. The numbers are written to the test output.
	[Theory]
	[InlineData(25)]
	[InlineData(1_000)]
	public async Task AFullyFeaturedTable_CostsTheSame_WhateverTheRowCount(int rows)
	{
		Host host = await _renderer.MountAsync(FeaturedTable(rows));

		await _renderer.Measure(host.RenderAgainAsync);

		TestContext.Current.TestOutputHelper?.WriteLine(
			$"{rows} rows, every feature: unchanged re-render {_renderer.Edits} edits, {_renderer.DisposedHandlers} handlers retired");
		Assert.True(_renderer.DisposedHandlers <= FeaturedHandlers, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits <= FeaturedEdits, $"{_renderer.Edits} edits.");
	}

	// A million rows under Virtualize: only the rows in view are drawn, so a render that changes nothing costs the same as
	// a short table's, and the grid's row numbers are worked out from each row's place, not walked.
	[Fact]
	public async Task AMillionVirtualizedRows_CostNothingToDrawAgain()
	{
		Stopwatch first = Stopwatch.StartNew();
		Host host = await _renderer.MountAsync(Table(count: 1_000_000, paged: false, virtualize: true));
		first.Stop();

		Stopwatch again = Stopwatch.StartNew();
		await _renderer.Measure(host.RenderAgainAsync);
		again.Stop();

		TestContext.Current.TestOutputHelper?.WriteLine(
			$"1000000 rows, virtualized: first render {first.ElapsedMilliseconds} ms; unchanged re-render {_renderer.Edits} edits, " +
			$"{_renderer.DisposedHandlers} handlers retired, {again.ElapsedMilliseconds} ms");
		Assert.True(_renderer.DisposedHandlers == 0, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits == 0, $"{_renderer.Edits} edits.");
	}

	// A million rows a server sends window by window: the table holds one window, and drawing it again sends nothing.
	[Fact]
	public async Task AMillionRemoteRows_CostNothingToDrawAgain()
	{
		Stopwatch first = Stopwatch.StartNew();
		Host host = await _renderer.MountAsync(RemoteTable(1_000_000));
		first.Stop();

		Stopwatch again = Stopwatch.StartNew();
		await _renderer.Measure(host.RenderAgainAsync);
		again.Stop();

		TestContext.Current.TestOutputHelper?.WriteLine(
			$"1000000 rows, remote windows: first render {first.ElapsedMilliseconds} ms; unchanged re-render {_renderer.Edits} edits, " +
			$"{_renderer.DisposedHandlers} handlers retired, {again.ElapsedMilliseconds} ms");
		Assert.True(_renderer.DisposedHandlers == 0, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits == 0, $"{_renderer.Edits} edits.");
	}

	// Editing, the edit column, blocks of cells, row copying and column virtualization are opt-in: a table with all of
	// them, and nothing open, costs what a selectable table costs.
	[Theory]
	[InlineData(25)]
	[InlineData(1_000)]
	public async Task AnEditingTable_CostsNothingToDrawAgain(int rows)
	{
		Host host = await _renderer.MountAsync(EditingTable(rows));

		await _renderer.Measure(host.RenderAgainAsync);

		TestContext.Current.TestOutputHelper?.WriteLine(
			$"{rows} rows, editing, ranges and column window: unchanged re-render {_renderer.Edits} edits, {_renderer.DisposedHandlers} handlers retired");

		// What is left is the selectable table's own: the box in the header, which @bind makes again on each render.
		Assert.True(_renderer.DisposedHandlers <= 1, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits <= 13, $"{_renderer.Edits} edits.");
	}

	private static RenderFragment EditingTable(int count) => builder =>
	{
		Person[] rows = Enumerable.Range(0, count).Select(i => new Person($"Name {i}", "Team", i, "City")).ToArray();
		builder.OpenComponent<MokaTable<Person>>(0);
		builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), (IEnumerable<Person>)rows);
		builder.AddComponentParameter(2, nameof(MokaTable<Person>.ShowPagination), false);
		builder.AddComponentParameter(3, nameof(MokaTable<Person>.EditMode), MokaTableEditMode.Row);
		builder.AddComponentParameter(4, nameof(MokaTable<Person>.OnRowSave),
			EventCallback.Factory.Create<MokaTableRowSave<Person>>(new object(), static () => { }));
		builder.AddComponentParameter(5, nameof(MokaTable<Person>.OnRowDelete), EventCallback.Factory.Create<Person>(new object(), static () => { }));
		builder.AddComponentParameter(6, nameof(MokaTable<Person>.NewItem), (Func<Person>)(static () => new Person("", "", 0, "")));
		builder.AddComponentParameter(7, nameof(MokaTable<Person>.CellRanges), true);
		builder.AddComponentParameter(8, nameof(MokaTable<Person>.Selectable), true);
		builder.AddComponentParameter(9, nameof(MokaTable<Person>.CopySelectedRows), true);
		builder.AddComponentParameter(10, nameof(MokaTable<Person>.VirtualizeColumns), true);
		builder.AddComponentParameter(11, nameof(MokaTable<Person>.ItemKey), (Func<Person, object>)(static x => x.Age));
		builder.AddComponentParameter(12, nameof(MokaTable<Person>.ChildContent), (RenderFragment)(inner =>
		{
			Columns(Name with { Editable = true }, Team with { Editable = true }, Age with { Editable = true }, City)(inner);
			inner.OpenComponent<MokaEditColumn<Person>>(100);
			inner.CloseComponent();
		}));
		builder.CloseComponent();
	};

	private static RenderFragment RemoteTable(int total) => builder =>
	{
		builder.OpenComponent<MokaTable<Person>>(0);
		builder.AddComponentParameter(1, nameof(MokaTable<Person>.Virtualize), true);
		builder.AddComponentParameter(2, nameof(MokaTable<Person>.Height), "400px");
		builder.AddComponentParameter(3, nameof(MokaTable<Person>.ServerData), (Func<MokaTableRequest, Task<MokaTableResult<Person>>>)(request =>
			Task.FromResult(new MokaTableResult<Person>
			{
				Items = Enumerable.Range(request.StartIndex, Math.Min(request.PageSize, total - request.StartIndex))
					.Select(static i => new Person($"Name {i}", "Team", i, "City")).ToList(),
				TotalItems = total
			})));
		builder.AddComponentParameter(4, nameof(MokaTable<Person>.ItemKey), (Func<Person, object>)(static x => x.Age));
		builder.AddComponentParameter(5, nameof(MokaTable<Person>.ChildContent), Columns(Name, Team, Age, City));
		builder.CloseComponent();
	};

	private const int FeaturedHandlers = 0;
	private const int FeaturedEdits = 0;

	private static RenderFragment FeaturedTable(int count) => builder =>
	{
		Person[] rows = Enumerable.Range(0, count).Select(i => new Person($"Name {i}", i % 2 == 0 ? "Even" : "Odd", i, "City")).ToArray();
		builder.OpenComponent<MokaTable<Person>>(0);
		builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), (IEnumerable<Person>)rows);
		builder.AddComponentParameter(2, nameof(MokaTable<Person>.ShowPagination), false);
		builder.AddComponentParameter(3, nameof(MokaTable<Person>.ColumnMenu), true);
		builder.AddComponentParameter(4, nameof(MokaTable<Person>.FilterControls),
			MokaTableFilterControls.Row | MokaTableFilterControls.Menus | MokaTableFilterControls.Chips);
		builder.AddComponentParameter(5, nameof(MokaTable<Person>.HighlightSearch), true);
		builder.AddComponentParameter(6, nameof(MokaTable<Person>.QuickFilters), (IReadOnlyList<MokaTableQuickFilter>)
			[new MokaTableQuickFilter("Even", [new MokaTableFilter("Team", MokaFilterOperator.Is, "Even")])]);
		builder.AddComponentParameter(7, nameof(MokaTable<Person>.ChildContent),
			Columns(Name with { Pin = MokaColumnPin.Start, Filterable = true }, Team with { Filterable = true }, Age, City with { Pin = MokaColumnPin.End }));
		builder.CloseComponent();
	};

	[Fact]
	public async Task TheHandlers_DoNotGrowWithTheCells()
	{
		await _renderer.MountAsync(Table());

		Assert.DoesNotContain("onfocusin", _renderer.HandlerNames);
		Assert.DoesNotContain("onkeydown", _renderer.HandlerNames);
		Assert.True(_renderer.HandlerNames.Count <= 6, $"{_renderer.HandlerNames.Count} handlers.");
	}

	[Fact]
	public async Task FocusMovingToACell_RendersNothing()
	{
		MokaTable<Person> table = null!;
		await _renderer.MountAsync(Table(captured => table = captured));

		await _renderer.Measure(() => _renderer.Dispatcher.InvokeAsync(() => table.OnCellFocused(3, 2)));

		Assert.Equal(0, _renderer.Batches);
	}

	[Fact]
	public async Task TypingInTheSearchBox_RendersNothingBeforeTheDebounce()
	{
		await _renderer.MountAsync(Table());

		await _renderer.Measure(() => _renderer.FireAsync("oninput", new ChangeEventArgs { Value = "a" }));

		Assert.Equal(0, _renderer.Batches);
	}

	[Fact]
	public async Task TypingInAFilter_RendersNothingBeforeTheDebounce()
	{
		await _renderer.MountAsync(Table(filters: true));

		await _renderer.Measure(() => _renderer.FireAsync("oninput", new ChangeEventArgs { Value = "a" }));

		Assert.Equal(0, _renderer.Batches);
	}

	// A lambda in the row template was a new delegate on every render, so a selectable table retired and re-sent a
	// handler per row for its boxes, its toggles and its clicks, even when nothing had changed. What is left is the box
	// in the header, which @bind makes again on each render.
	[Fact]
	public async Task ATableWhoseRowsListen_RetiresNoRowHandlers()
	{
		Host host = await _renderer.MountAsync(Table(rowsListen: true));

		await _renderer.Measure(host.RenderAgainAsync);

		Assert.True(_renderer.DisposedHandlers <= 1, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits <= 13, $"{_renderer.Edits} edits.");
	}

	// The toggle at the row's end shares the row's handlers, so it adds no handler that a render replaces.
	[Fact]
	public async Task TheToggleAtTheRowsEnd_RetiresNoRowHandlers()
	{
		Host host = await _renderer.MountAsync(Table(rowsListen: true, toggleAtEnd: true));

		await _renderer.Measure(host.RenderAgainAsync);

		Assert.True(_renderer.DisposedHandlers <= 1, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits <= 13, $"{_renderer.Edits} edits.");
	}

	[Fact]
	public async Task ATableInSlots_CostsNoMoreThanAPlainOne()
	{
		Host host = await _renderer.MountAsync(Table(phone: true));

		await _renderer.Measure(host.RenderAgainAsync);

		Assert.True(_renderer.DisposedHandlers == 0, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits == 0, $"{_renderer.Edits} edits.");
	}

	// A tick: a new list of new rows, one of them changed. Only that cell's text goes to the browser, plus the steps
	// down to it, where a rebuilt row would send every cell of every row.
	[Fact]
	public async Task ATickThatChangesOneCell_SendsOnlyThatCell()
	{
		string firstName = "Name 0";
		Host host = await _renderer.MountAsync(Table(rowsListen: true, firstName: () => firstName));
		await _renderer.Measure(host.RenderAgainAsync);
		int nothingChanged = _renderer.Edits;

		firstName = "Renamed";
		await _renderer.Measure(host.RenderAgainAsync);

		Assert.True(_renderer.Edits <= nothingChanged + 7, $"{nothingChanged} edits became {_renderer.Edits}.");
	}

	public async ValueTask DisposeAsync()
	{
		await _renderer.DisposeAsync();
		await _services.DisposeAsync();
	}

	private static RenderFragment Table(
		Action<MokaTable<Person>>? capture = null,
		bool filters = false,
		bool rowsListen = false,
		bool phone = false,
		Func<string>? firstName = null,
		bool toggleAtEnd = false,
		int count = 25,
		bool paged = true,
		bool withPager = false,
		bool virtualize = false)
	{
		// The same rows on every render, so a render where nothing changed is measured as one; a tick makes new ones.
		Person[] Make() => Enumerable.Range(0, count)
			.Select(i => new Person(i == 0 && firstName is not null ? firstName() : $"Name {i}", "Team", i, "City"))
			.ToArray();
		Person[]? same = firstName is null ? Make() : null;
		return builder => Draw(builder, same ?? Make());

		void Draw(RenderTreeBuilder builder, Person[] rows)
		{
		Col[] columns = filters
			? [Name with { Filterable = true }, Team, Age, City, new Col("Extra", static x => x.City)]
			: phone
				? [
					Name with { Phone = MokaColumnPhone.Primary },
					Team with { Phone = MokaColumnPhone.Secondary },
					Age with { Phone = MokaColumnPhone.Trailing },
					City with { Phone = MokaColumnPhone.Secondary },
					new Col("Extra", static x => x.City)
				]
				: [Name, Team, Age, City, new Col("Extra", static x => x.City)];

		builder.OpenComponent<MokaTable<Person>>(0);
		builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), (IEnumerable<Person>)rows);
		builder.AddComponentParameter(2, nameof(MokaTable<Person>.PageSize), 25);
		builder.AddComponentParameter(3, nameof(MokaTable<Person>.Searchable), !filters);
		builder.AddComponentParameter(4, nameof(MokaTable<Person>.FilterControls), filters ? MokaTableFilterControls.Row : MokaTableFilterControls.None);
		builder.AddComponentParameter(5, nameof(MokaTable<Person>.ChildContent),
			withPager ? ColumnsWithPager(new Pager { Look = MokaPagerLook.Entries }, columns) : Columns(columns));
		if (!paged)
		{
			builder.AddComponentParameter(14, nameof(MokaTable<Person>.ShowPagination), false);
		}

		if (virtualize)
		{
			builder.AddComponentParameter(15, nameof(MokaTable<Person>.Virtualize), true);
			builder.AddComponentParameter(16, nameof(MokaTable<Person>.Height), "400px");
		}

		if (rowsListen)
		{
			builder.AddComponentParameter(6, nameof(MokaTable<Person>.Selectable), true);
			builder.AddComponentParameter(7, nameof(MokaTable<Person>.Expandable), true);
			builder.AddComponentParameter(8, nameof(MokaTable<Person>.ItemKey), (Func<Person, object>)(static x => x.Age));
			builder.AddComponentParameter(9, nameof(MokaTable<Person>.OnRowClick),
				EventCallback.Factory.Create<Person>(new object(), static () => { }));
			if (toggleAtEnd)
			{
				builder.AddComponentParameter(11, nameof(MokaTable<Person>.ExpandTogglePosition), MokaTableTogglePosition.End);
				builder.AddComponentParameter(12, nameof(MokaTable<Person>.SingleExpand), true);
				builder.AddComponentParameter(13, nameof(MokaTable<Person>.ItemName), (Func<Person, string>)(static x => x.Name));
			}
		}

		if (capture is not null)
		{
			builder.AddComponentReferenceCapture(10, instance => capture((MokaTable<Person>)instance));
		}

		builder.CloseComponent();
		}
	}

	private sealed class CountingRenderer(IServiceProvider services, ILoggerFactory logs) : Renderer(services, logs)
	{
		private readonly List<(string Name, ulong Id)> _handlers = [];

		public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

		public int Batches { get; private set; }

		public int Edits { get; private set; }

		public int DisposedHandlers { get; private set; }

		public List<string> HandlerNames => _handlers.Select(handler => handler.Name).ToList();

		public async Task<Host> MountAsync(RenderFragment body)
		{
			var host = (Host)InstantiateComponent(typeof(Host));
			int id = AssignRootComponentId(host);
			await Dispatcher.InvokeAsync(() => RenderRootComponentAsync(id, ParameterView.FromDictionary(
				new Dictionary<string, object?> { [nameof(Host.Body)] = body })));
			return host;
		}

		public async Task Measure(Func<Task> action)
		{
			(Batches, Edits, DisposedHandlers) = (0, 0, 0);
			await action();
		}

		public Task FireAsync(string name, EventArgs args)
		{
			ulong id = _handlers.Last(handler => handler.Name == name).Id;
			return Dispatcher.InvokeAsync(() => DispatchEventAsync(id, null, args));
		}

		protected override void HandleException(Exception exception) => throw exception;

		protected override Task UpdateDisplayAsync(in RenderBatch batch)
		{
			Batches++;
			DisposedHandlers += batch.DisposedEventHandlerIDs.Count;
			for (int i = 0; i < batch.UpdatedComponents.Count; i++)
			{
				Edits += batch.UpdatedComponents.Array[i].Edits.Count;
			}

			for (int i = 0; i < batch.ReferenceFrames.Count; i++)
			{
				ref RenderTreeFrame frame = ref batch.ReferenceFrames.Array[i];
				if (frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId != 0)
				{
					_handlers.Add((frame.AttributeName, frame.AttributeEventHandlerId));
				}
			}

			return Task.CompletedTask;
		}
	}

	private sealed class Host : ComponentBase
	{
		[Parameter] public RenderFragment? Body { get; set; }

		public Task RenderAgainAsync() => InvokeAsync(StateHasChanged);

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Body);
	}

	private sealed class SilentJs : IJSRuntime
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
			InvokeAsync<TValue>(identifier, CancellationToken.None, args);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
			new(typeof(TValue) == typeof(IJSObjectReference) ? (TValue)(object)new SilentModule() : default!);
	}

	private sealed class SilentModule : IJSObjectReference
	{
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => new(default(TValue)!);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
			new(default(TValue)!);

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
}
