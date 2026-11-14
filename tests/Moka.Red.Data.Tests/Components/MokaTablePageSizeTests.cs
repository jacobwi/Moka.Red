using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// The pager hides while every row fits on one page, and has to stay while the user's own page
// size is the reason they fit.
public class MokaTablePageSizeTests : BunitContext
{
	private static readonly Person[] Rows = Enumerable.Range(1, 25)
		.Select(i => new Person(
			string.Create(CultureInfo.InvariantCulture, $"Person {i:00}"), "Engineering", 20 + i, "London"))
		.ToArray();

	public MokaTablePageSizeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// The pager stayed while _pageSize differed from PageSize. Under a bound state the page size
	// follows the user's pick, so a size that fit every row took the size menu away, and with it
	// the only way back to a smaller size.
	[Fact]
	public async Task BoundPageSize_PagerStays_AfterPickingASizeThatFitsEveryRow()
	{
		IRenderedComponent<BoundPageSizeHost> host = Render<BoundPageSizeHost>(p => p.Add(h => h.Rows, Rows));
		IRenderedComponent<MokaTable<Person>> cut = host.FindComponent<MokaTable<Person>>();

		await SizeMenu(cut).ChangeAsync(new ChangeEventArgs { Value = "50" });
		Assert.Equal(50, host.Instance.PageSize);
		Assert.Equal(Rows.Length, DataRows(cut).Count);

		await SizeMenu(cut).ChangeAsync(new ChangeEventArgs { Value = "10" });
		Assert.Equal(10, host.Instance.PageSize);
		Assert.Equal(10, DataRows(cut).Count);
	}

	[Fact]
	public async Task UnboundPageSize_PagerStays_AfterPickingASizeThatFitsEveryRow()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, Rows)
			.Add(t => t.PageSize, 10)
			.Add(t => t.ChildContent, DefaultColumns));

		await SizeMenu(cut).ChangeAsync(new ChangeEventArgs { Value = "50" });
		cut.Render(p => p.Add(t => t.PageSize, 10));
		await SizeMenu(cut).ChangeAsync(new ChangeEventArgs { Value = "10" });

		Assert.Equal(10, DataRows(cut).Count);
	}

	// With every row on one page even at the parent's size, the size menu has nothing to offer.
	[Fact]
	public void PagerStaysHidden_WhileTheParentsSizeFitsEveryRow()
	{
		IRenderedComponent<MokaTable<Person>> cut = Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, Rows.Take(8).ToArray())
			.Add(t => t.ChildContent, DefaultColumns));

		Assert.Empty(cut.FindAll(".moka-pagination"));
	}

	// A bound state hands back the size the user picked. Treating that as a new size from the
	// parent loaded the rows a second time.
	[Fact]
	public async Task BoundPageSize_LoadsOncePerPick()
	{
		int loads = 0;
		IRenderedComponent<BoundPageSizeHost> host = Render<BoundPageSizeHost>(p => p
			.Add(h => h.ServerData, state =>
			{
				loads++;
				return Task.FromResult(new MokaTableResult<Person>
				{
					Items = Rows.Skip((state.Page - 1) * state.PageSize).Take(state.PageSize).ToList(),
					TotalItems = Rows.Length
				});
			}));
		IRenderedComponent<MokaTable<Person>> cut = host.FindComponent<MokaTable<Person>>();
		cut.WaitForAssertion(() => Assert.NotEmpty(DataRows(cut)));
		loads = 0;

		await SizeMenu(cut).ChangeAsync(new ChangeEventArgs { Value = "25" });

		Assert.Equal(1, loads);
		Assert.Equal(Rows.Length, DataRows(cut).Count);
	}

	private static IElement SizeMenu(IRenderedComponent<MokaTable<Person>> cut) =>
		cut.Find(".moka-pagination-size-select");

	/// <summary>A parent that binds the state both ways, and reads the page size from it.</summary>
	private sealed class BoundPageSizeHost : ComponentBase
	{
		private static readonly int[] SizeOptions = [10, 25, 50];

		private MokaTableState? _state;

		/// <summary>Client-side rows, used when <see cref="ServerData" /> is not set.</summary>
		[Parameter]
		public IReadOnlyList<Person> Rows { get; set; } = [];

		/// <summary>A server-side source.</summary>
		[Parameter]
		public Func<MokaTableRequest, Task<MokaTableResult<Person>>>? ServerData { get; set; }

		/// <summary>The page size in the bound state.</summary>
		public int PageSize => _state?.PageSize ?? 10;

		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenComponent<MokaTable<Person>>(0);
			if (ServerData is null)
			{
				builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), Rows);
			}
			else
			{
				builder.AddComponentParameter(2, nameof(MokaTable<Person>.ServerData), ServerData);
			}

			builder.AddComponentParameter(3, nameof(MokaTable<Person>.PageSize), 10);
			builder.AddComponentParameter(4, nameof(MokaTable<Person>.State), _state);
			builder.AddComponentParameter(5, nameof(MokaTable<Person>.StateChanged),
				EventCallback.Factory.Create<MokaTableState>(this, value => _state = value));
			builder.AddComponentParameter(6, nameof(MokaTable<Person>.ChildContent),
				DefaultColumnsWithPager(new Pager { PageSizeOptions = SizeOptions }));
			builder.CloseComponent();
		}
	}
}
