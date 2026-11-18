using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// A column rendered by a component of the page's, such as a reusable column, reports after the table's own columns, so
// it went last whatever the markup said. Order says where it stands, among the table's parts or within its group.
public class MokaTableColumnOrderTests : BunitContext
{
	public MokaTableColumnOrderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void AWrappedColumnWithoutOrder_GoesLast()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Orders, [0, 0, 0]));

		Assert.Equal(["Name", "Age", "Team"], Headings(host));
	}

	[Fact]
	public void AWrappedColumn_StandsWhereItsOrderSays()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Orders, [1, 2, 3]));

		Assert.Equal(["Name", "Team", "Age"], Headings(host));
	}

	[Fact]
	public void AWrappedColumnInAGroup_StandsWhereItsOrderSays_WithinTheGroup()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p
			.Add(x => x.Orders, [1, 2, 3])
			.Add(x => x.InGroup, true));

		Assert.Equal(["Name", "Team", "Age"], Headings(host));
		Assert.Equal("Person", host.Find(".moka-table-group-heading-row th").TextContent.Trim());
	}

	[Fact]
	public async Task ChangingAColumnsOrder_MovesIt()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Orders, [1, 2, 3]));

		await host.InvokeAsync(() => host.Instance.SetOrders([3, 2, 1]));

		Assert.Equal(["Age", "Team", "Name"], Headings(host));
	}

	private static List<string> Headings(IRenderedComponent<Host> host) =>
		host.FindAll("thead tr:not(.moka-table-group-heading-row) th .moka-table-header-text").Select(e => e.TextContent.Trim()).ToList();

	private sealed class Host : ComponentBase
	{
		[Parameter] public int[] Orders { get; set; } = [0, 0, 0];

		[Parameter] public bool InGroup { get; set; }

		public void SetOrders(int[] orders)
		{
			Orders = orders;
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaTable<Person>>(0);
			builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), People);
			builder.AddComponentParameter(2, nameof(MokaTable<Person>.ChildContent), (RenderFragment)Parts);
			builder.CloseComponent();
		}

		private void Parts(RenderTreeBuilder builder)
		{
			if (InGroup)
			{
				builder.OpenComponent<MokaColumnGroup>(0);
				builder.AddComponentParameter(1, nameof(MokaColumnGroup.Title), "Person");
				builder.AddComponentParameter(2, nameof(MokaColumnGroup.ChildContent), (RenderFragment)Columns);
				builder.CloseComponent();
			}
			else
			{
				Columns(builder);
			}
		}

		private void Columns(RenderTreeBuilder builder)
		{
			Column(builder, 10, "Name", static x => x.Name, Orders[0]);
			builder.OpenComponent<WrappedColumn>(20);
			builder.AddComponentParameter(21, nameof(WrappedColumn.Order), Orders[1]);
			builder.CloseComponent();
			Column(builder, 30, "Age", static x => x.Age, Orders[2]);
		}

		internal static void Column(RenderTreeBuilder builder, int sequence, string title, Func<Person, object?> field, int order)
		{
			builder.OpenComponent<MokaColumn<Person>>(sequence);
			builder.AddComponentParameter(sequence + 1, nameof(MokaColumn<Person>.Title), title);
			builder.AddComponentParameter(sequence + 2, nameof(MokaColumn<Person>.Field), field);
			builder.AddComponentParameter(sequence + 3, nameof(MokaColumn<Person>.Order), order);
			builder.CloseComponent();
		}
	}

	private sealed class WrappedColumn : ComponentBase
	{
		[Parameter] public int Order { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) =>
			Host.Column(builder, 0, "Team", static x => x.Team, Order);
	}
}
