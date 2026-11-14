using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Saving an edited cell: the value in the column's own type, a save only when it changed, and the app's objects untouched.
public class MokaTableEditTests : BunitContext
{
	private readonly List<MokaTableRowSave<Person>> _saves = [];

	public MokaTableEditTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task SavingACellUnchanged_RaisesNoSave()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		Assert.Empty(cut.FindAll("td[data-editing]"));
		Assert.Empty(_saves);
	}

	// The number reaches the save as the column's own type, written into a new record by the setter.
	[Fact]
	public async Task SavingAChangedNumber_HandsTheTypedValueAndTheUpdatedRow()
	{
		IRenderedComponent<MokaTable<Person>> cut = RenderTable();

		await cut.InvokeAsync(() => cut.Instance.OnCellKey(0, 1, "Enter"));
		await cut.Find("td[data-editing] input").InputAsync(new ChangeEventArgs { Value = "40" });
		await cut.InvokeAsync(() => cut.Instance.OnEditorKey("Enter"));

		MokaTableRowSave<Person> save = Assert.Single(_saves);
		MokaTableCellChange change = Assert.Single(save.Changes);
		Assert.Equal("Age", change.Column);
		Assert.Equal(36, change.OldValue);
		Assert.Equal(40, change.NewValue);
		Assert.Equal(40, save.Updated.Age);
		Assert.Same(People[0], save.Item);
		Assert.Equal(36, People[0].Age);
		Assert.Equal(MokaTableSaveKind.Edit, save.Kind);
	}

	private IRenderedComponent<MokaTable<Person>> RenderTable() =>
		Render<MokaTable<Person>>(p => p
			.Add(t => t.Items, People)
			.Add(t => t.OnRowSave, save => _saves.Add(save))
			.Add(t => t.ChildContent, Columns(
				Name,
				Age with { Editable = true, Format = "N2", SetField = static (p, v) => p with { Age = (int)v! } })));
}
