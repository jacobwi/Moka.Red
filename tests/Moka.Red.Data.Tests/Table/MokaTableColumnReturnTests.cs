using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Table;

// A column, or a heading group, kept its place among the others by the order the columns registered in, so one inside
// an @if that the page rendered again went after all of them, and "put back" put it there too. The table now takes the
// order its columns and groups stand in the markup, and a group the order of its own columns.
public class MokaTableColumnReturnTests : BunitContext
{
	public MokaTableColumnReturnTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task AColumnThatComesBack_TakesItsPlaceAgain()
	{
		IRenderedComponent<Host> host = Render<Host>();

		await host.InvokeAsync(() => host.Instance.Set(show: false));
		Assert.Equal(["Name", "Age", "City"], Headings(host));

		await host.InvokeAsync(() => host.Instance.Set(show: true));
		Assert.Equal(["Name", "Team", "Age", "City"], Headings(host));
	}

	[Fact]
	public async Task AColumnThatComesBack_InAHeadingGroup_TakesItsPlaceInTheGroup()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Layout, Layouts.ColumnInGroup));

		await host.InvokeAsync(() => host.Instance.Set(show: false));
		Assert.Equal(["Team", "Age", "City"], Headings(host));

		await host.InvokeAsync(() => host.Instance.Set(show: true));
		Assert.Equal(["Name", "Team", "Age", "City"], Headings(host));
	}

	[Fact]
	public async Task AHeadingGroupThatComesBack_TakesItsPlaceAgain()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Layout, Layouts.WholeGroup));

		await host.InvokeAsync(() => host.Instance.Set(show: false));
		Assert.Equal(["Age", "City"], Headings(host));

		await host.InvokeAsync(() => host.Instance.Set(show: true));
		Assert.Equal(["Name", "Team", "Age", "City"], Headings(host));
		Assert.Equal("Person", host.Find(".moka-table-group-heading-row th").TextContent.Trim());
	}

	[Fact]
	public async Task AColumnThatComesBack_KeepsTheMovesSomeoneMade()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Reorderable, true));
		await Heading(host, "City").DragStartAsync(new DragEventArgs());
		await Heading(host, "Name").DropAsync(new DragEventArgs());
		Assert.Equal(["City", "Name", "Team", "Age"], Headings(host));

		await host.InvokeAsync(() => host.Instance.Set(show: false));
		await host.InvokeAsync(() => host.Instance.Set(show: true));

		Assert.Equal(["City", "Name", "Team", "Age"], Headings(host));
	}

	[Fact]
	public async Task AColumnThatComesBack_GoesBackToItsPlace_WhenTheColumnsArePutBack()
	{
		IRenderedComponent<Host> host = Render<Host>(p => p.Add(x => x.Chooser, true));
		await host.InvokeAsync(() => host.Instance.Set(show: false));
		await host.InvokeAsync(() => host.Instance.Set(show: true));

		await host.Find(".moka-table-chooser-toggle").ClickAsync(new MouseEventArgs());
		await host.Find(".moka-table-chooser input[aria-label='Show Age']").ChangeAsync(new ChangeEventArgs { Value = false });
		await host.FindAll(".moka-table-chooser-foot button").First(b => b.TextContent.Contains("back", StringComparison.OrdinalIgnoreCase))
			.ClickAsync(new MouseEventArgs());

		Assert.Equal(["Name", "Team", "Age", "City"], Headings(host));
	}

	private static List<string> Headings(IRenderedComponent<Host> host) =>
		host.FindAll("thead tr:not(.moka-table-group-heading-row) th .moka-table-header-text").Select(e => e.TextContent.Trim()).ToList();

	private static AngleSharp.Dom.IElement Heading(IRenderedComponent<Host> host, string title) =>
		host.FindAll("thead tr:not(.moka-table-group-heading-row) th")
			.First(th => th.QuerySelector(".moka-table-header-text")?.TextContent.Trim() == title);

	internal enum Layouts
	{
		Flat,
		ColumnInGroup,
		WholeGroup
	}

	private sealed class Host : ComponentBase
	{
		private bool _show = true;

		[Parameter] public Layouts Layout { get; set; }

		[Parameter] public bool Reorderable { get; set; }

		[Parameter] public bool Chooser { get; set; }

		public void Set(bool show)
		{
			_show = show;
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaTable<Person>>(0);
			builder.AddComponentParameter(1, nameof(MokaTable<Person>.Items), People);
			builder.AddComponentParameter(2, nameof(MokaTable<Person>.ColumnReorderable), Reorderable);
			builder.AddComponentParameter(3, nameof(MokaTable<Person>.ShowColumnChooser), Chooser);
			builder.AddComponentParameter(4, nameof(MokaTable<Person>.ChildContent), (RenderFragment)Columns);
			builder.CloseComponent();
		}

		private void Columns(RenderTreeBuilder builder)
		{
			switch (Layout)
			{
				case Layouts.ColumnInGroup:
					Group(builder, 0, inner =>
					{
						Column(inner, 0, Name, _show);
						Column(inner, 10, Team, true);
					});
					break;
				case Layouts.WholeGroup:
					if (_show)
					{
						Group(builder, 0, inner =>
						{
							Column(inner, 0, Name, true);
							Column(inner, 10, Team, true);
						});
					}

					break;
				default:
					Column(builder, 0, Name, true);
					Column(builder, 10, Team, _show);
					break;
			}

			Column(builder, 20, Age, true);
			Column(builder, 30, City, true);
		}

		private static void Group(RenderTreeBuilder builder, int sequence, RenderFragment columns)
		{
			builder.OpenComponent<MokaColumnGroup>(sequence);
			builder.AddComponentParameter(sequence + 1, nameof(MokaColumnGroup.Title), "Person");
			builder.AddComponentParameter(sequence + 2, nameof(MokaColumnGroup.ChildContent), columns);
			builder.CloseComponent();
		}

		private static void Column(RenderTreeBuilder builder, int sequence, Col column, bool show)
		{
			if (show)
			{
				builder.OpenRegion(sequence);
				column.Write(builder);
				builder.CloseRegion();
			}
		}
	}
}
