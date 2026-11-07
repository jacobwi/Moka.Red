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

	// What is left is one sort handler per column, which is a lambda over the column.
	[Fact]
	public async Task ARenderWhereNothingChanged_StaysWithinItsBudget()
	{
		Host host = await _renderer.MountAsync(Table());

		await _renderer.Measure(host.RenderAgainAsync);

		Assert.True(_renderer.DisposedHandlers <= 5, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits <= 15, $"{_renderer.Edits} edits.");
	}

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
	// handler per row for its boxes, its toggles and its clicks, even when nothing had changed. What is left is the
	// sort handlers and the box in the header.
	[Fact]
	public async Task ATableWhoseRowsListen_RetiresNoRowHandlers()
	{
		Host host = await _renderer.MountAsync(Table(rowsListen: true));

		await _renderer.Measure(host.RenderAgainAsync);

		Assert.True(_renderer.DisposedHandlers <= 6, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits <= 21, $"{_renderer.Edits} edits.");
	}

	[Fact]
	public async Task ATableInSlots_CostsNoMoreThanAPlainOne()
	{
		Host host = await _renderer.MountAsync(Table(phone: true));

		await _renderer.Measure(host.RenderAgainAsync);

		Assert.True(_renderer.DisposedHandlers <= 5, $"{_renderer.DisposedHandlers} handlers retired.");
		Assert.True(_renderer.Edits <= 15, $"{_renderer.Edits} edits.");
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
		Func<string>? firstName = null) => builder =>
	{
		Person[] rows = Enumerable.Range(0, 25)
			.Select(i => new Person(i == 0 && firstName is not null ? firstName() : $"Name {i}", "Team", i, "City"))
			.ToArray();
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
		builder.AddComponentParameter(4, nameof(MokaTable<Person>.ShowFilters), filters);
		builder.AddComponentParameter(5, nameof(MokaTable<Person>.ChildContent), Columns(columns));
		if (rowsListen)
		{
			builder.AddComponentParameter(6, nameof(MokaTable<Person>.Selectable), true);
			builder.AddComponentParameter(7, nameof(MokaTable<Person>.Expandable), true);
			builder.AddComponentParameter(8, nameof(MokaTable<Person>.ItemKey), (Func<Person, object>)(static x => x.Age));
			builder.AddComponentParameter(9, nameof(MokaTable<Person>.OnRowClick),
				EventCallback.Factory.Create<Person>(new object(), static () => { }));
		}

		if (capture is not null)
		{
			builder.AddComponentReferenceCapture(10, instance => capture((MokaTable<Person>)instance));
		}

		builder.CloseComponent();
	};

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
