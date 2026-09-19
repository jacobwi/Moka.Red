using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Interactions;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Every row had a click and a context-menu handler, and every cell a double-click handler, whether
// anything listened or not. Each of those events went to .NET and rendered the whole table again.
public class MokaTableHandlerTests : BunitContext
{
	public MokaTableHandlerTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task RowsWithoutOnRowClick_HaveNoClickHandler()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		await Assert.ThrowsAsync<MissingEventHandlerException>(() => Cell(cut, 0, 0).ClickAsync(new MouseEventArgs()));
	}

	[Fact]
	public async Task RowsWithoutOnRowContextMenu_HaveNoContextMenuHandler()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		await Assert.ThrowsAsync<MissingEventHandlerException>(
			() => Cell(cut, 0, 0).ContextMenuAsync(new MouseEventArgs()));
	}

	[Fact]
	public async Task OnlyCellsThatCanBeEdited_HaveADoubleClickHandler()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(columns: [Name, Team with { Editable = true }]);

		await Assert.ThrowsAsync<MissingEventHandlerException>(
			() => Cell(cut, 0, 0).DoubleClickAsync(new MouseEventArgs()));
		await Cell(cut, 0, 1).DoubleClickAsync(new MouseEventArgs());

		Assert.NotNull(cut.Find(".moka-table-edit-input"));
	}

	[Fact]
	public async Task WithListeners_RowClickAndContextMenuArrive()
	{
		Person? clicked = null;
		MokaItemContextMenuArgs<Person>? menu = null;
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.OnRowClick, person => clicked = person)
			.Add(t => t.OnRowContextMenu, args => menu = args));

		await Cell(cut, 1, 0).ClickAsync(new MouseEventArgs());
		await Cell(cut, 2, 0).ContextMenuAsync(new MouseEventArgs());

		Assert.Equal(People[1], clicked);
		Assert.Equal(People[2], menu?.Item);
	}

	// Selection, the detail toggle and row actions have handlers of their own.
	[Fact]
	public async Task WithoutOnRowClick_SelectionDetailAndRowActionsWork()
	{
		int actions = 0;
		HashSet<Person>? selected = null;
		IRenderedComponent<MokaTable<Person>> cut = RenderTable(p => p
			.Add(t => t.Selectable, true)
			.Add(t => t.SelectedItemsChanged, set => selected = set)
			.Add(t => t.Expandable, true)
			.Add(t => t.DetailTemplate, person => builder => builder.AddContent(0, "detail " + person.Name))
			.Add(t => t.RowActions, (RenderFragment<Person>)(_ => builder =>
			{
				builder.OpenElement(0, "button");
				builder.AddAttribute(1, "class", "row-action");
				builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => actions++));
				builder.CloseElement();
			})));

		await DataRows(cut)[1].QuerySelector("input[type=checkbox]")!.ChangeAsync(new ChangeEventArgs { Value = true });
		await DataRows(cut)[0].QuerySelector(".moka-table-expand-btn")!.ClickAsync(new MouseEventArgs());
		await DataRows(cut)[2].QuerySelector("button.row-action")!.ClickAsync(new MouseEventArgs());

		Assert.Equal([People[1]], selected!);
		Assert.Single(cut.FindAll("tr.moka-table-detail-row"));
		Assert.Equal(1, actions);
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null,
		Col[]? columns = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, columns is null ? DefaultColumns : Columns(columns));
			extra?.Invoke(p);
		});

	private static IElement Cell(IRenderedComponent<MokaTable<Person>> cut, int row, int col) =>
		cut.Find(string.Create(CultureInfo.InvariantCulture,
			$"tbody tr[data-row-index='{row}'] td[data-col-index='{col}']"));
}
