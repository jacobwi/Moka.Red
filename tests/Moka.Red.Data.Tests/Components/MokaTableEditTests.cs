using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Saving an edited cell. With Format the editor opens on the formatted text, "36.00" for 36.
public class MokaTableEditTests : BunitContext
{
	private readonly List<(Person Item, object? NewValue)> _columnEdits = [];
	private readonly List<MokaTableCellEditResult<Person>> _tableEdits = [];

	public MokaTableEditTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// A save compared the entered text with the raw value's ToString(), "36", so saving the
	// untouched "36.00" raised both edit callbacks.
	[Fact]
	public async Task SavingAFormattedCellUnchanged_ReportsNoEdit()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		await OpenEditorAsync(cut);
		await Editor(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Empty(cut.FindAll(".moka-table-edit-input"));
		Assert.Empty(_columnEdits);
		Assert.Empty(_tableEdits);
	}

	[Fact]
	public async Task SavingAChangedFormattedCell_ReportsTheEdit()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		await OpenEditorAsync(cut);
		await Editor(cut).InputAsync(new ChangeEventArgs { Value = "40" });
		await Editor(cut).KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("40", Assert.Single(_columnEdits).NewValue);
		MokaTableCellEditResult<Person> edit = Assert.Single(_tableEdits);
		Assert.Equal(36, edit.OldValue);
		Assert.Equal("40", edit.NewValue);
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable() =>
		Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.OnCellEdit, e => _tableEdits.Add(e))
			.Add(t => t.ChildContent, Columns(
				Name,
				Age with
				{
					Editable = true,
					Format = "N2",
					OnCellEdited = EventCallback.Factory.Create<(Person Item, object? NewValue)>(
						this, e => _columnEdits.Add(e))
				})));

	private static async Task OpenEditorAsync(IRenderedComponent<MokaTable<Person>> cut)
	{
		IElement ageCell = cut.Find("tbody tr[data-row-index='0'] td[data-col-index='1']");
		await ageCell.FocusInAsync(new FocusEventArgs());
		await ageCell.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
	}

	private static IElement Editor(IRenderedComponent<MokaTable<Person>> cut) => cut.Find(".moka-table-edit-input");
}
