using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

public class MokaTableSelectionTests : BunitContext
{
	public MokaTableSelectionTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// Clicking the selected row's box kept the row selected. The browser had already unticked the
	// box, and the markup said checked before and after, so Blazor left the box unticked.
	[Fact]
	public async Task SingleSelect_UntickingTheSelectedRow_ClearsTheSelection()
	{
		HashSet<Person>? selected = null;
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.SingleSelect, true)
			.Add(t => t.SelectedItemsChanged, s => selected = s));

		await RowBox(cut, 0).ChangeAsync(new ChangeEventArgs { Value = true });
		Assert.Equal([People[0]], selected!);

		await RowBox(cut, 0).ChangeAsync(new ChangeEventArgs { Value = false });

		Assert.Empty(selected!);
		Assert.Empty(cut.FindAll("tbody input[type=checkbox]:checked"));
	}

	[Fact]
	public async Task SingleSelect_TickingAnotherRow_ReplacesTheSelection()
	{
		HashSet<Person>? selected = null;
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.SingleSelect, true)
			.Add(t => t.SelectedItemsChanged, s => selected = s));

		await RowBox(cut, 0).ChangeAsync(new ChangeEventArgs { Value = true });
		await RowBox(cut, 2).ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.Equal([People[2]], selected!);
	}

	// The template got the table's own set, so clearing or filling it changed the selection
	// without the table or the parent hearing about it.
	[Fact]
	public async Task SelectionActions_GetsACopyOfTheSelection()
	{
		HashSet<Person>? given = null;
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.SelectionActions, (RenderFragment<HashSet<Person>>)(set =>
			{
				given = set;
				return b => b.AddContent(0, "actions");
			})));
		await RowBox(cut, 1).ChangeAsync(new ChangeEventArgs { Value = true });

		given!.Clear();
		cut.Render();

		Assert.Equal("1 selected", cut.Find(".moka-table-selection-count").TextContent.Trim());
		Assert.Single(cut.FindAll("tbody input[type=checkbox]:checked"));
	}

	// A parent that only listens to SelectedItemsChanged passes no SelectedItems. The table took
	// the render that follows the parent's handler for a new, empty selection, so every tick was
	// undone at once.
	[Fact]
	public async Task ParentThatOnlyListens_KeepsTheTick()
	{
		IRenderedComponent<ListeningHost> host = Render<ListeningHost>();
		IRenderedComponent<MokaTable<Person>> cut = host.FindComponent<MokaTable<Person>>();

		await RowBox(cut, 1).ChangeAsync(new ChangeEventArgs { Value = true });

		Assert.Equal(1, host.Instance.SelectedCount);
		Assert.Single(cut.FindAll("tbody input[type=checkbox]:checked"));
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>> extra) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.Selectable, true);
			p.Add(t => t.ChildContent, DefaultColumns);
			extra(p);
		});

	private static IElement RowBox(IRenderedComponent<MokaTable<Person>> cut, int row) =>
		cut.FindAll("tbody tr[data-row-index]")[row].QuerySelector("input[type=checkbox]")!;

	/// <summary>A parent that counts the selection and does not pass it back.</summary>
	private sealed class ListeningHost : ComponentBase
	{
		/// <summary>How many rows the last SelectedItemsChanged reported.</summary>
		public int SelectedCount { get; private set; }

		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenComponent<MokaTable<Person>>(0);
			builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), People);
			builder.AddComponentParameter(2, nameof(MokaTable<Person>.Selectable), true);
			builder.AddComponentParameter(3, nameof(MokaTable<Person>.SelectedItemsChanged),
				EventCallback.Factory.Create<HashSet<Person>>(this, selected => SelectedCount = selected.Count));
			builder.AddComponentParameter(4, nameof(MokaTable<Person>.ChildContent), DefaultColumns);
			builder.CloseComponent();
		}
	}
}
