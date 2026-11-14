namespace Moka.Red.Data.Table;

/// <summary>One value an edit changed.</summary>
/// <param name="Column">The column's title.</param>
/// <param name="OldValue">The value the row had.</param>
/// <param name="NewValue">The value it gets, of the column's type: a number column's <c>double?</c>, a date column's <c>DateTimeOffset?</c>.</param>
public sealed record MokaTableCellChange(string Column, object? OldValue, object? NewValue);

/// <summary>
///     What <see cref="MokaTable{TItem}.OnRowSave" /> receives: a row someone edited, added or pasted into, or the last save
///     undone. The table changes none of your objects: save <see cref="Updated" /> (or apply <see cref="Changes" />) to your
///     data and hand the table the new rows. To refuse the save, call <see cref="AddError(string)" /> or
///     <see cref="AddError(string, string)" />, or throw; the row stays open with the messages.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public sealed class MokaTableRowSave<TItem>
{
	private readonly List<KeyValuePair<string?, string>> _errors = [];

	internal MokaTableRowSave(TItem item, TItem updated, IReadOnlyList<MokaTableCellChange> changes, MokaTableSaveKind kind)
	{
		Item = item;
		Updated = updated;
		Changes = changes;
		Kind = kind;
	}

	/// <summary>The row as it was: the item from your data, or for an added row the one <c>NewItem</c> made.</summary>
	public TItem Item { get; }

	/// <summary>
	///     The row with every change written into it through each column's setter (<see cref="MokaColumn{TItem}.SetField" />,
	///     or a column kind's <c>SetValue</c>): a new record when the setters use <c>with</c>. A change in a column without a
	///     setter is only in <see cref="Changes" />.
	/// </summary>
	public TItem Updated { get; }

	/// <summary>The values that changed, by column title, in the order the columns stand.</summary>
	public IReadOnlyList<MokaTableCellChange> Changes { get; }

	/// <summary>Why the table asks: an edit, an added row, a paste or an undo.</summary>
	public MokaTableSaveKind Kind { get; }

	/// <summary>Whether the row is new: true for <see cref="MokaTableSaveKind.Add" />.</summary>
	public bool IsNew => Kind == MokaTableSaveKind.Add;

	/// <summary>The messages added so far, each with the title of the column it belongs to, or null for the row.</summary>
	public IReadOnlyList<KeyValuePair<string?, string>> Errors => _errors;

	/// <summary>Refuses the save with a message about the whole row.</summary>
	/// <param name="message">What is wrong.</param>
	public void AddError(string message) => _errors.Add(new KeyValuePair<string?, string>(null, message));

	/// <summary>Refuses the save with a message shown under one column's editor.</summary>
	/// <param name="column">The column's title.</param>
	/// <param name="message">What is wrong.</param>
	public void AddError(string column, string message) => _errors.Add(new KeyValuePair<string?, string>(column, message));

	/// <summary>The new value of a column, or the default when the save didn't change it.</summary>
	/// <typeparam name="TValue">The column's value type.</typeparam>
	/// <param name="column">The column's title.</param>
	/// <returns>The value it gets.</returns>
	public TValue? NewValue<TValue>(string column)
	{
		foreach (MokaTableCellChange change in Changes)
		{
			if (string.Equals(change.Column, column, StringComparison.Ordinal))
			{
				return change.NewValue is TValue value ? value : default;
			}
		}

		return default;
	}
}

/// <summary>What a column's <see cref="MokaColumn{TItem}.EditTemplate" /> draws its editor from.</summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public sealed class MokaCellEdit<TItem>
{
	private readonly Action<object?> _set;

	internal MokaCellEdit(TItem item, string? column, object? value, string? error, string label, Action<object?> set)
	{
		Item = item;
		Column = column;
		Value = value;
		Error = error;
		Label = label;
		_set = set;
	}

	/// <summary>The row being edited, with the changes made so far written into it.</summary>
	public TItem Item { get; }

	/// <summary>The column's title.</summary>
	public string? Column { get; }

	/// <summary>The value the editor shows.</summary>
	public object? Value { get; }

	/// <summary>What is wrong with the value, or null.</summary>
	public string? Error { get; }

	/// <summary>A name for the editor, for its <c>aria-label</c>.</summary>
	public string Label { get; }

	/// <summary>Takes a new value, of the column's type. The table checks it and keeps it until the row is saved.</summary>
	/// <param name="value">The value.</param>
	public void SetValue(object? value) => _set(value);
}
