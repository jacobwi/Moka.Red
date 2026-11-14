using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;

namespace Moka.Red.Data.Table;

/// <summary>
///     The buttons that edit a row: Edit and Delete, and while the row is open Save and Cancel. Edit opens the row's
///     editable cells (a whole row in <see cref="MokaTableEditMode.Row" />, the first editable cell in
///     <see cref="MokaTableEditMode.Cell" />), and Delete raises the table's <c>OnRowDelete</c>, asking first when it
///     says so. A new row from the table's Add button shows Save and Cancel here too.
/// </summary>
/// <remarks>Its buttons are an actions column's: no click handler or button component per row.</remarks>
/// <typeparam name="TItem">The row data type.</typeparam>
public class MokaEditColumn<TItem> : MokaActionsColumn<TItem>
{
	private IReadOnlyList<MokaTableAction<TItem>> _drawn = [];

	/// <summary>Whether each row has a Delete button, when the table has an <c>OnRowDelete</c>. Default true.</summary>
	[Parameter]
	public bool ShowDelete { get; set; } = true;

	/// <summary>More buttons, drawn after the edit buttons. Optional here, since the column makes its own.</summary>
	[Parameter]
	public override IReadOnlyList<MokaTableAction<TItem>>? Actions { get; set; }

	/// <inheritdoc />
	/// <remarks>Save, Cancel, Edit and Delete, then <see cref="Actions" />.</remarks>
	protected override IReadOnlyList<MokaTableAction<TItem>> DrawnActions => _drawn;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		_drawn =
		[
			new MokaTableAction<TItem>(Words.SaveRow, MokaIcons.Status.Check, SaveAsync)
			{
				Hidden = item => !IsOpen(item),
				Disabled = _ => Table?.IsSavingEdit == true
			},
			new MokaTableAction<TItem>(Words.CancelEdit, MokaIcons.Navigation.Close, CancelAsync) { Hidden = item => !IsOpen(item) },
			new MokaTableAction<TItem>(Words.EditRow, MokaIcons.Action.Edit, EditAsync) { Hidden = IsOpen },
			new MokaTableAction<TItem>(Words.DeleteRow, MokaIcons.Action.Delete, DeleteAsync)
			{
				Hidden = item => IsOpen(item) || !ShowDelete || Table?.HasDeletes != true,
				Color = MokaColor.Error
			},
			.. Actions ?? []
		];
		base.OnParametersSet();
	}

	private bool IsOpen(TItem item) => Table is { } table && (table.IsEditingItem(item) || table.IsNewItem(item));

	private Task SaveAsync(TItem item) => Table?.SaveEditAsync() ?? Task.CompletedTask;

	private Task CancelAsync(TItem item)
	{
		Table?.CancelEdit();
		return Task.CompletedTask;
	}

	private Task EditAsync(TItem item) => Table?.StartEditAsync(item) ?? Task.CompletedTask;

	private Task DeleteAsync(TItem item) => Table?.DeleteRowAsync(item) ?? Task.CompletedTask;
}
