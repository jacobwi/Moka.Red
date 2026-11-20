using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// The one state record: built from what the table shows, applied from a parameter, bound, reset and kept.
public sealed class MokaTableStateTests : BunitContext
{
	public MokaTableStateTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void AState_RoundTripsThroughJson()
	{
		MokaTableState state = new()
		{
			Sorts = [new MokaTableSort("Age", MokaSortDirection.Descending)],
			Filters = [new MokaTableFilter("Team", MokaFilterOperator.IsAnyOf, ["Research", "Engineering"])],
			Search = "ad",
			Page = 3,
			PageSize = 25,
			Density = MokaTableDensity.Comfortable,
			Columns = [new MokaTableColumnState("City") { Hidden = true, Width = 120, Pin = MokaColumnPin.End }],
			Groups = ["Team"],
			CollapsedGroups = ["Research"],
			Expanded = ["Ada"]
		};

		string json = state.ToJson();

		Assert.Equal(state, MokaTableState.FromJson(json));
		Assert.Contains("\"direction\":\"Descending\"", json, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("not json")]
	[InlineData("[1, 2]")]
	public void TextThatIsNotAState_ReadsAsNone(string? json) => Assert.Null(MokaTableState.FromJson(json));

	[Fact]
	public void TwoStates_AreEqualWhenTheySayTheSame()
	{
		MokaTableState a = new() { Sorts = [new MokaTableSort("Age", MokaSortDirection.Ascending)], Expanded = ["b", "a"] };
		MokaTableState b = new() { Sorts = [new MokaTableSort("Age", MokaSortDirection.Ascending)], Expanded = ["a", "b"] };

		Assert.Equal(a, b);
		Assert.NotEqual(a, b with { Page = 2 });
		Assert.True(a.SameView(b with { Page = 2, Density = MokaTableDensity.Standard }));
		Assert.False(a.SameView(b with { Search = "x" }));
	}

	[Fact]
	public void SortingFromAHeading_RaisesTheState()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(x => x.StateChanged, (MokaTableState s) => raised = s));

		HeaderCell(cut, "Age").Click();

		Assert.NotNull(raised);
		Assert.Equal([new MokaTableSort("Age", MokaSortDirection.Ascending)], raised.Sorts);
		Assert.Equal(1, raised.Page);
		Assert.Equal(10, raised.PageSize);
		Assert.Equal(MokaTableDensity.Compact, raised.Density);
	}

	[Fact]
	public void ATableNobodyListensTo_BuildsNoState()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		HeaderCell(cut, "Age").Click();

		Assert.Equal(["Barbara", "Ada", "Alan", "Grace"], ColumnTexts(cut, 0));
	}

	[Fact]
	public void AStateParameter_PutsEveryPartInForce()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(x => x.FilterControls, MokaTableFilterControls.Row)
			.Add(x => x.Searchable, true)
			.Add(x => x.ChildContent, Columns(Name, Team with { Filterable = true }, Age, City))
			.Add(x => x.State, new MokaTableState
			{
				Sorts = [new MokaTableSort("Age", MokaSortDirection.Descending)],
				Filters = [new MokaTableFilter("Team", MokaFilterOperator.Contains, "eng")],
				Search = "a",
				Density = MokaTableDensity.Comfortable,
				Columns = [new MokaTableColumnState("City") { Hidden = true }]
			}));

		Assert.Equal(["Alan", "Ada"], ColumnTexts(cut, 0));
		Assert.DoesNotContain("City", HeaderTitles(cut));
		Assert.Contains("moka-table--comfortable", cut.Find("table").ClassName, StringComparison.Ordinal);
		Assert.Equal("a", cut.Find(".moka-table-search-input").GetAttribute("value"));
		Assert.Equal("eng", cut.Find(".moka-table-filter-input").GetAttribute("value"));
	}

	[Fact]
	public void ABoundState_KeepsTheUsersSort_AcrossParentRenders()
	{
		IRenderedComponent<BoundHost> host = Render<BoundHost>();

		HeaderCell(TableIn(host), "Age").Click();
		host.Render();

		Assert.Equal(["Barbara", "Ada", "Alan", "Grace"], ColumnTexts(TableIn(host), 0));
		Assert.Equal([new MokaTableSort("Age", MokaSortDirection.Ascending)], host.Instance.State!.Sorts);
	}

	// An inline new state on every parent render says the same thing each time, so it doesn't undo a click.
	[Fact]
	public void AnInlineStateBuiltOnEveryRender_DoesNotUndoTheUser()
	{
		IRenderedComponent<InlineHost> host = Render<InlineHost>();

		HeaderCell(TableIn(host), "Age").Click();
		host.Render();

		Assert.Equal(["Barbara", "Ada", "Alan", "Grace"], ColumnTexts(TableIn(host), 0));
	}

	[Fact]
	public void ANewStateFromTheParent_ReplacesTheUsers()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();
		HeaderCell(cut, "Age").Click();

		cut.Render(p => p.Add(x => x.State, SortedBy("Name", MokaSortDirection.Descending)));

		Assert.Equal(["Grace", "Barbara", "Alan", "Ada"], ColumnTexts(cut, 0));
	}

	[Fact]
	public void ThePage_BindsBothWays()
	{
		int? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(x => x.PageSize, 2)
			.Add(x => x.PageChanged, (int page) => raised = page));

		cut.FindAll(".moka-pagination-page").First(b => b.TextContent.Trim() == "2").Click();
		Assert.Equal(2, raised);
		Assert.Equal(["Alan", "Barbara"], ColumnTexts(cut, 0));

		cut.Render(p => p.Add(x => x.Page, 2));
		cut.Render(p => p.Add(x => x.Page, 1));
		Assert.Equal(["Ada", "Grace"], ColumnTexts(cut, 0));
	}

	[Fact]
	public void AColumnsDefaultSort_IsTheStartingSort_AndResetGoesBackToIt()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(x => x.ChildContent, Columns(Name, Team, Age with { DefaultSort = MokaSortDirection.Descending }, City)));
		Assert.Equal(["Grace", "Alan", "Ada", "Barbara"], ColumnTexts(cut, 0));

		HeaderCell(cut, "Name").Click();
		Assert.Equal(["Ada", "Alan", "Barbara", "Grace"], ColumnTexts(cut, 0));

		cut.InvokeAsync(cut.Instance.ResetStateAsync);
		Assert.Equal(["Grace", "Alan", "Ada", "Barbara"], ColumnTexts(cut, 0));
	}

	[Fact]
	public void CurrentState_SaysWhatTheTableShows()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(x => x.PageSize, 2));
		HeaderCell(cut, "Name").Click();

		MokaTableState state = cut.Instance.CurrentState();

		Assert.Equal([new MokaTableSort("Name", MokaSortDirection.Ascending)], state.Sorts);
		Assert.Equal(2, state.PageSize);
		Assert.Empty(state.Columns);
	}

	[Fact]
	public void OpenRows_AreInTheState_ByItemKey()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(x => x.Expandable, true)
			.Add(x => x.ItemKey, static x => x.Name)
			.Add(x => x.DetailTemplate, (RenderFragment<Person>)(person => b => b.AddContent(0, $"About {person.Name}")))
			.Add(x => x.StateChanged, (MokaTableState s) => raised = s));

		cut.FindAll(".moka-table-expand-btn")[1].Click();
		cut.WaitForAssertion(() => Assert.Equal(["Grace"], raised?.Expanded));

		cut.Render(p => p.Add(x => x.State, new MokaTableState { Expanded = ["Alan"] }));
		Assert.Equal("About Alan", cut.Find(".moka-table-detail-row").TextContent.Trim());
	}

	[Fact]
	public void AStateGroups_ByAColumnsTitle()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(x => x.State, new MokaTableState { Groups = ["Team"] }));

		Assert.Equal(["Engineering", "Research"], cut.FindAll(".moka-table-group-key").Select(static e => e.TextContent));
	}

	[Fact]
	public void AKeptState_IsPutBackFromTheStore_AndKeptOnChange()
	{
		MemoryStore store = new();
		store.States["people"] = SortedBy("Age", MokaSortDirection.Descending);
		Services.AddSingleton<IMokaTableStateStore>(store);
		SetRendererInfo(new RendererInfo("Server", true));

		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(x => x.StorageKey, "people"));

		cut.WaitForAssertion(() => Assert.Equal(["Grace", "Alan", "Ada", "Barbara"], ColumnTexts(cut, 0)));

		HeaderCell(cut, "Name").Click();
		cut.WaitForAssertion(() => Assert.Equal([new MokaTableSort("Name", MokaSortDirection.Ascending)], store.States["people"].Sorts));
	}

	[Fact]
	public void WithoutAStore_TheStateIsKeptInLocalStorage()
	{
		SetRendererInfo(new RendererInfo("Server", true));
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Setup<string?>("readState", "moka-table:people").SetResult(SortedBy("Name", MokaSortDirection.Descending).ToJson());
		module.SetupVoid("writeState", _ => true);

		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(x => x.StorageKey, "people"));

		cut.WaitForAssertion(() => Assert.Equal(["Grace", "Barbara", "Alan", "Ada"], ColumnTexts(cut, 0)));
		HeaderCell(cut, "Age").Click();

		JSRuntimeInvocation write = module.Invocations["writeState"][^1];
		Assert.Equal("moka-table:people", write.Arguments[0]);
		Assert.Equal([new MokaTableSort("Age", MokaSortDirection.Ascending)], MokaTableState.FromJson((string?)write.Arguments[1])!.Sorts);
	}

	private IRenderedComponent<MokaTable<Person>> Table(Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? configure = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(x => x.Items, People).Add(x => x.ChildContent, DefaultColumns);
			configure?.Invoke(p);
		});

	private static IRenderedComponent<MokaTable<Person>> TableIn<THost>(IRenderedComponent<THost> host) where THost : class, IComponent =>
		host.FindComponent<MokaTable<Person>>();

	private sealed class MemoryStore : IMokaTableStateStore
	{
		public Dictionary<string, MokaTableState> States { get; } = [];

		public ValueTask<MokaTableState?> LoadAsync(string key, CancellationToken cancellationToken) =>
			new(States.GetValueOrDefault(key));

		public ValueTask SaveAsync(string key, MokaTableState state, CancellationToken cancellationToken)
		{
			States[key] = state;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class BoundHost : ComponentBase
	{
		public MokaTableState? State { get; private set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaTable<Person>>(0);
			builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), People);
			builder.AddComponentParameter(2, nameof(MokaTable<Person>.State), State);
			builder.AddComponentParameter(3, nameof(MokaTable<Person>.StateChanged),
				EventCallback.Factory.Create<MokaTableState>(this, value => State = value));
			builder.AddComponentParameter(4, nameof(MokaTable<Person>.ChildContent), DefaultColumns);
			builder.CloseComponent();
		}
	}

	private sealed class InlineHost : ComponentBase
	{
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaTable<Person>>(0);
			builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), People);
			builder.AddComponentParameter(2, nameof(MokaTable<Person>.State), new MokaTableState { PageSize = 10 });
			builder.AddComponentParameter(3, nameof(MokaTable<Person>.StateChanged),
				EventCallback.Factory.Create<MokaTableState>(this, static () => { }));
			builder.AddComponentParameter(4, nameof(MokaTable<Person>.ChildContent), DefaultColumns);
			builder.CloseComponent();
		}
	}
}
