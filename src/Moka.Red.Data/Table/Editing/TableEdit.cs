using Microsoft.AspNetCore.Components.Forms;

namespace Moka.Red.Data.Table;

// One row being edited or added: the values entered so far, the copy they are written into, and what is wrong with them.
internal sealed class TableEdit<TItem>
{
	private readonly Dictionary<MokaColumn<TItem>, object?> _values = [];
	private readonly Dictionary<MokaColumn<TItem>, string> _errors = [];
	private readonly Dictionary<MokaColumn<TItem>, string?> _texts = [];
	private EditContext? _editContext;

	public TableEdit(TItem original, TItem draft, object? key, MokaColumn<TItem>? column, bool isNew)
	{
		Original = original;
		Draft = draft;
		Key = key;
		Column = column;
		IsNew = isNew;
	}

	/// <summary>The row as the app has it.</summary>
	public TItem Original { get; private set; }

	/// <summary>The copy the entered values are written into, through the columns' setters.</summary>
	public TItem Draft { get; private set; }

	/// <summary>The row's key, which finds it again after a reload.</summary>
	public object? Key { get; }

	/// <summary>The cell being edited in cell mode, or null when the whole row is.</summary>
	public MokaColumn<TItem>? Column { get; set; }

	public bool IsNew { get; }

	public bool Saving { get; set; }

	/// <summary>Messages about the whole row: from the app's save, a validator, or a failed save.</summary>
	public List<string> RowErrors { get; } = [];

	public IReadOnlyDictionary<MokaColumn<TItem>, object?> Values => _values;

	/// <summary>
	///     The edit context of the copy, for the table's validators. A record's setter makes a new copy, so the context is
	///     made again for it, and the validators drawn against it with it.
	/// </summary>
	public EditContext? EditContext(bool wanted)
	{
		if (!wanted || Draft is null)
		{
			return null;
		}

		if (_editContext is null || !ReferenceEquals(_editContext.Model, Draft))
		{
			_editContext = new EditContext(Draft);
		}

		return _editContext;
	}

	public object? ValueOf(MokaColumn<TItem> col) => _values.TryGetValue(col, out object? value) ? value : col.ValueOf(Draft);

	public bool HasValue(MokaColumn<TItem> col) => _values.ContainsKey(col);

	public void Set(MokaColumn<TItem> col, object? value)
	{
		_values[col] = value;
		if (col.CanWrite)
		{
			Draft = col.WriteValue(Draft, value);
		}
	}

	public string? ErrorOf(MokaColumn<TItem> col) => _errors.GetValueOrDefault(col);

	public bool HasErrors => _errors.Count > 0 || RowErrors.Count > 0;

	public void SetError(MokaColumn<TItem> col, string? message)
	{
		if (message is null)
		{
			_errors.Remove(col);
		}
		else
		{
			_errors[col] = message;
		}
	}

	public void ClearErrors()
	{
		_errors.Clear();
		RowErrors.Clear();
	}

	public MokaColumn<TItem>? FirstInvalid(IEnumerable<MokaColumn<TItem>> columns) =>
		columns.FirstOrDefault(col => _errors.ContainsKey(col));

	public bool HasText(MokaColumn<TItem> col) => _texts.ContainsKey(col);

	public string? TextOf(MokaColumn<TItem> col) => _texts.GetValueOrDefault(col);

	public void SetText(MokaColumn<TItem> col, string? text) => _texts[col] = text;

	public void ClearText(MokaColumn<TItem> col) => _texts.Remove(col);

	// Whether the text typed for a column read as a value; the value then holds it.
	public bool TextReads(MokaColumn<TItem> col) =>
		!_texts.TryGetValue(col, out string? text) || TableValues.TryParse(text ?? string.Empty, col.EditValueType(Draft), out _);

	public bool HasErrorsIn(IEnumerable<MokaColumn<TItem>> columns) => columns.Any(_errors.ContainsKey);
}
