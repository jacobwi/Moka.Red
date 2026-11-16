using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Table;

/// <summary>
///     Defines a column in a <see cref="MokaTable{TItem}" />. Add it as child content of the table. It renders nothing
///     where it stands: the table reads its settings and draws its cells.
/// </summary>
/// <remarks>
///     A column kind (<c>MokaPersonColumn</c>, <c>MokaNumberColumn</c> and the rest) derives from this and overrides
///     what its cells show (<see cref="CellContent" />) and what sorting, filtering, the search and export read
///     (<see cref="ValueOf" />, <see cref="TextOf" />, <see cref="ExportTextOf" />).
/// </remarks>
/// <typeparam name="TItem">The row data type.</typeparam>
public class MokaColumn<TItem> : ComponentBase, IDisposable, ITableField<TItem>
{
	private const char Separator = '\u001F';

	// The settings the table renders, as the parent last passed them. Null before the first set.
	private RenderedSettings? _lastSettings;

	/// <summary>
	///     Column header text. Sorting, column filters, grouping and the saved state name the column by it, including
	///     in the <see cref="MokaTableRequest" /> a server-side data source receives, so keep titles unique.
	/// </summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Property selector for the cell value. Used for sorting, filtering, the search and the default cell.</summary>
	[Parameter]
	public Func<TItem, object?>? Field { get; set; }

	/// <summary>Custom cell template. Receives the row item. It wins over what a column kind draws.</summary>
	[Parameter]
	public RenderFragment<TItem>? CellTemplate { get; set; }

	/// <summary>Custom header template. Overrides Title.</summary>
	[Parameter]
	public RenderFragment? HeaderTemplate { get; set; }

	/// <summary>
	///     Whether clicking the header sorts by this column. Default true. The header sorts only when the column has a
	///     <see cref="Title" /> and, with client-side data, a value to order the rows by (a <see cref="Field" />, a column
	///     kind's own value, or a <see cref="SortComparer" />). Under server-side data the title is enough.
	/// </summary>
	[Parameter]
	public bool Sortable { get; set; } = true;

	/// <summary>
	///     The sort the table starts with and goes back to, and its first click when unsorted. Default none. Several
	///     columns with one sort in the order they stand. A <see cref="MokaTable{TItem}.State" /> that names sorts wins.
	/// </summary>
	[Parameter]
	public MokaSortDirection DefaultSort { get; set; }

	/// <summary>
	///     Whether the table's filter controls offer this column: an input in the filter row, a filter button in its
	///     heading, a choice in the builder, and a list in the facet panel for list and yes or no filters. See
	///     <see cref="MokaTable{TItem}.FilterControls" />. Default false.
	/// </summary>
	[Parameter]
	public bool Filterable { get; set; }

	/// <summary>
	///     The choices a list filter offers, in place of the values the table finds in its rows. For a server-side source
	///     that doesn't send facets, or to offer values no row has yet. Null, the default, lists the column's values.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaFilterOption>? FilterOptions { get; set; }

	/// <summary>Width as CSS value (e.g., "200px", "30%", "1fr"). Null = auto.</summary>
	[Parameter]
	public string? Width { get; set; }

	/// <summary>Minimum width. Prevents column from shrinking too small.</summary>
	[Parameter]
	public string? MinWidth { get; set; }

	/// <summary>Text alignment for cells. Default Left; number columns default to Right.</summary>
	[Parameter]
	public virtual MokaTextAlign Align { get; set; } = MokaTextAlign.Left;

	/// <summary>
	///     Whether the column starts out shown. Default true. The table's column toggle can show or
	///     hide it afterwards, and passing a new value replaces what the user picked there.
	/// </summary>
	[Parameter]
	public bool Visible { get; set; } = true;

	/// <summary>
	///     The edge the column starts pinned to while the rows scroll sideways: <see cref="MokaColumnPin.Start" /> or
	///     <see cref="MokaColumnPin.End" />. Default none. Pinned columns move to their edge, in the order they stand, and
	///     any number can pin to each side. The column menu and the state can pin it too, and a new value here replaces
	///     what they chose.
	/// </summary>
	[Parameter]
	public MokaColumnPin Pin { get; set; }

	/// <summary>CSS class applied to cells in this column.</summary>
	[Parameter]
	public string? CellClass { get; set; }

	/// <summary>
	///     Compares two rows in place of their values, in single and multi-column sorts alike. A column with a comparer
	///     sorts without a Field.
	/// </summary>
	[Parameter]
	public Func<TItem, TItem, int>? SortComparer { get; set; }

	/// <summary>
	///     The heading a row goes under when the table is grouped by this column. Default the cell's text, so rows that
	///     show the same text share a heading. A column with a key groups without a <see cref="Field" />, so a hidden
	///     column can hold just the key.
	/// </summary>
	[Parameter]
	public Func<TItem, string>? GroupKey { get; set; }

	/// <summary>Format string for the cell value (e.g., "C2" for currency, "d" for date).</summary>
	[Parameter]
	public string? Format { get; set; }

	/// <summary>Whether to hide this column on small screens.</summary>
	[Parameter]
	public bool HideOnMobile { get; set; }

	/// <summary>
	///     Where the cell goes when the table is a list below 720 px. Null, the default, names no slot, and a table
	///     where no column names one stays a scrolling table on a phone. Once any column names a slot, a column
	///     without one is hidden in the list, except that the first column is the primary line when no column claims
	///     it. A slot shows on a phone even if the column is hidden above 720 px.
	/// </summary>
	[Parameter]
	public MokaColumnPhone? Phone { get; set; }

	/// <summary>
	///     Keeps this column where it is: it can't be dragged, nothing can be dropped on it, and it isn't offered in
	///     the column toggle or the chooser. Other columns move around it. Default false. It says nothing about width:
	///     <see cref="Resizable" /> does.
	/// </summary>
	[Parameter]
	public bool Fixed { get; set; }

	/// <summary>Draws the cells in the monospace font, for addresses, hashes and rates. Default false.</summary>
	[Parameter]
	public bool Mono { get; set; }

	/// <summary>
	///     Keeps every value in the column on one line, for short values that read wrong broken in two, such as names,
	///     event ids and times. Default false, where a long value wraps. The column is as wide as its longest value until
	///     it has a width, from <see cref="Width" /> or a drag on its heading's edge; then a longer value is cut off with an
	///     ellipsis instead of widening the column. In the list a table becomes on a phone the value stays on one line and
	///     is cut off at the row's edge. Headings never wrap, so this changes only the other cells.
	/// </summary>
	[Parameter]
	public bool NoWrap { get; set; }

	/// <summary>
	///     Classes on every cell of the column: the header, the filter, skeleton, data and aggregate cells. A rule that
	///     hides the column at some widths then hides all of them, and the rows stay lined up. <see cref="CellClass" />
	///     reaches only the data cells.
	/// </summary>
	[Parameter]
	public string? Class { get; set; }

	/// <summary>
	///     The first click on this column sorts it descending: largest, latest or most first, for rates, byte counts and
	///     times. Default false, where the first click sorts ascending.
	/// </summary>
	[Parameter]
	public bool SortDescendingFirst { get; set; }

	/// <summary>Whether this column can be resized by dragging. Default true.</summary>
	[Parameter]
	public bool Resizable { get; set; } = true;

	/// <summary>
	///     What the column's filter controls take. Default <see cref="MokaColumnFilterType.Auto" />, from the column's
	///     values. A list's choices are <see cref="FilterOptions" /> when set, else under
	///     <see cref="MokaTable{TItem}.Items" /> the values among the rows that pass the search, the quick filter and the
	///     other columns' filters, else what a server-side source sends in <see cref="MokaTableResult{TItem}.Facets" />,
	///     else the values on the rows loaded.
	/// </summary>
	[Parameter]
	public MokaColumnFilterType FilterType { get; set; }

	/// <summary>
	///     Whether cells in this column can be edited in place. Default false. Enter, F2 or a double click on a cell opens
	///     its editor (see <see cref="MokaTable{TItem}.EditMode" />), and the table hands the change to
	///     <see cref="MokaTable{TItem}.OnRowSave" />. It changes none of your objects itself.
	/// </summary>
	[Parameter]
	public bool Editable { get; set; }

	/// <summary>
	///     The editor the cells open with. Default <see cref="MokaColumnEditor.Auto" />, picked from the column kind, its
	///     choices and the value's type.
	/// </summary>
	[Parameter]
	public MokaColumnEditor Editor { get; set; }

	/// <summary>
	///     The choices a <see cref="MokaColumnEditor.Select" /> editor offers, each value written the way the column's text
	///     reads back (an enum's name, a number in the invariant culture). Null, the default, takes
	///     <see cref="FilterOptions" />, else an enum's values, else the values the loaded rows have.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaFilterOption>? EditOptions { get; set; }

	/// <summary>Your own editor in place of the one <see cref="Editor" /> picks. It wins over <see cref="Editor" />.</summary>
	[Parameter]
	public RenderFragment<MokaCellEdit<TItem>>? EditTemplate { get; set; }

	/// <summary>
	///     Writes a value into a row and returns the row: <c>(p, v) =&gt; p with { Name = (string?)v ?? "" }</c> for a
	///     record, which makes a new one, so your object is never changed. The value is of the column's type. The table
	///     writes edits into a copy it keeps for the row (see <see cref="MokaTable{TItem}.EditCopy" />), checks it, and hands
	///     it to <see cref="MokaTable{TItem}.OnRowSave" /> as <see cref="MokaTableRowSave{TItem}.Updated" />. Null, the
	///     default, edits all the same, and the change is only in <see cref="MokaTableRowSave{TItem}.Changes" />.
	/// </summary>
	[Parameter]
	public Func<TItem, object?, TItem>? SetField { get; set; }

	/// <summary>
	///     Checks a value someone entered: return what is wrong with it, or null when it is fine. It runs as the value
	///     changes and before a save, with the row as it stands with the other changes.
	/// </summary>
	[Parameter]
	public Func<TItem, object?, string?>? Validate { get; set; }

	/// <summary>
	///     The name of the property the column edits, under which the table's <see cref="MokaTable{TItem}.Validators" />
	///     (a <c>DataAnnotationsValidator</c>, say) file their messages. Default the <see cref="Title" />.
	/// </summary>
	[Parameter]
	public string? Property { get; set; }

	/// <summary>
	///     Aggregation function for this column's footer. Default None. Aggregates run over the rows
	///     the table has loaded: the whole filtered set in client mode, but only the current page
	///     under server-side data, where the table cannot see rows it did not fetch. With the table's
	///     pager hidden it fetches every row, so server-side aggregates cover them all.
	/// </summary>
	[Parameter]
	public MokaAggregateType Aggregate { get; set; } = MokaAggregateType.None;

	/// <summary>Custom aggregate format string (e.g., "C2" for currency).</summary>
	[Parameter]
	public string? AggregateFormat { get; set; }

	[CascadingParameter] private IMokaTablePartHost? Host { get; set; }

	private MokaTable<TItem>? ParentTable => Host as MokaTable<TItem>;

	/// <summary>The table the column belongs to, for a column kind that acts on it.</summary>
	internal MokaTable<TItem>? Table => ParentTable;

	[CascadingParameter] private MokaColumnGroup? ColumnGroup { get; set; }

	/// <summary>The heading group the column stands under, or null.</summary>
	internal MokaColumnGroup? Group => ColumnGroup;

	/// <summary>The column's number in its table, which its cells' actions carry. Set once the column joins the table.</summary>
	internal int ActionKey { get; set; }

	/// <summary>
	///     What <see cref="MokaColumnFilterType.Auto" /> means for this column when its values can't say. A column kind
	///     whose values are text but read as a list, such as statuses, returns <see cref="MokaColumnFilterType.MultiSelect" />.
	///     The default, <see cref="MokaColumnFilterType.Auto" />, goes by the values.
	/// </summary>
	protected internal virtual MokaColumnFilterType AutoFilterType => MokaColumnFilterType.Auto;

	/// <summary>
	///     What a cell of this column shows when no <see cref="CellTemplate" /> is set. Null, the default, writes
	///     <see cref="TextOf" />. A column kind returns its own, once, so the same fragment serves every row.
	/// </summary>
	protected internal virtual RenderFragment<TItem>? CellContent => null;

	/// <summary>
	///     Content the table draws once, beside its rows, for this column: a dialog or viewer its cells open. Null, the
	///     default, draws nothing. Call <see cref="Redraw" /> after changing what it shows.
	/// </summary>
	protected internal virtual RenderFragment? Overlay => null;

	/// <summary>
	///     What the column is called when it has no <see cref="Title" />, in its column menu, the column toggle and its
	///     editor's name: a column kind names what it shows ("Actions"). Null by default, which names it by its place.
	/// </summary>
	protected internal virtual string? KindName => null;

	/// <summary>A class every data cell of the column carries, for a column kind's own look. Null by default.</summary>
	protected internal virtual string? KindCellClass => null;

	/// <summary>
	///     The settings a column kind draws from, as one value that compares equal when nothing it draws changed: a
	///     record or a tuple of its own parameters, leaving delegates out. The table draws again in the same render when
	///     it changes.
	/// </summary>
	protected virtual object? KindSettings => null;

	/// <summary>Whether the column has a value to sort, filter, search and group by. True when it has a <see cref="Field" />.</summary>
	protected internal virtual bool HasValue => Field is not null;

	/// <summary>The table's words, for a column kind's own.</summary>
	protected MokaTableText Words => ParentTable?.Words ?? MokaTableText.Default;

	/// <summary>The rows the table shows now, in order.</summary>
	protected IReadOnlyList<TItem> ShownItems => ParentTable?.ShownItems ?? [];

	string? ITableField<TItem>.Title => Title;

	bool ITableField<TItem>.HasValue => HasValue;

	Func<TItem, TItem, int>? ITableField<TItem>.Comparer => SortComparer;

	/// <inheritdoc />
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	///     The row's value: what the sort orders by, what value filters compare and what the footer totals. The
	///     <see cref="Field" />'s value by default.
	/// </summary>
	/// <param name="item">The row.</param>
	/// <returns>The value, or null when there is none.</returns>
	protected internal virtual object? ValueOf(TItem item) => Field?.Invoke(item);

	/// <summary>
	///     The row's text as the cell shows it: what the search and text filters read and the default cell writes. The
	///     value with <see cref="Format" /> applied in the current culture by default.
	/// </summary>
	/// <param name="item">The row.</param>
	/// <returns>The text, empty when there is no value.</returns>
	protected internal virtual string TextOf(TItem item)
	{
		object? value = ValueOf(item);
		if (value is null)
		{
			return string.Empty;
		}

		if (Format is not null && value is IFormattable formattable)
		{
			return formattable.ToString(Format, CultureInfo.CurrentCulture);
		}

		return value.ToString() ?? string.Empty;
	}

	/// <summary>What an export writes for the row. The cell's text by default.</summary>
	/// <param name="item">The row.</param>
	/// <returns>The text to export.</returns>
	protected internal virtual string ExportTextOf(TItem item) => TextOf(item);

	/// <summary>The heading the row goes under when grouped by this column: <see cref="GroupKey" />, else the cell's text.</summary>
	/// <param name="item">The row.</param>
	/// <returns>The group's key.</returns>
	protected internal virtual string GroupKeyFor(TItem item) => GroupKey?.Invoke(item) ?? TextOf(item);

	/// <summary>
	///     The type of the column's values, for its editor and for reading pasted text. A column kind returns its own. By
	///     default it comes from the <see cref="Field" /> values, since a value read through <c>object</c> has lost its
	///     declared type: the row's value's type, else the type of the first value among the table's rows, nullable when
	///     the row or another leaves the column empty. A column that no row gives a value takes the type its
	///     <see cref="Editor" /> edits, else text.
	/// </summary>
	/// <param name="item">The row.</param>
	/// <returns>The type.</returns>
	protected internal virtual Type EditValueType(TItem item) =>
		ParentTable?.FieldValueType(this, item) ?? ValueOf(item)?.GetType() ?? typeof(string);

	/// <summary>
	///     The editor <see cref="MokaColumnEditor.Auto" /> means for this column. A column kind returns its own; the default
	///     goes by the choices and the value's type.
	/// </summary>
	/// <param name="item">The row being edited.</param>
	/// <returns>The editor.</returns>
	protected internal virtual MokaColumnEditor AutoEditor(TItem item)
	{
		if (EditOptions is not null || FilterOptions is not null)
		{
			return MokaColumnEditor.Select;
		}

		Type type = TableValues.Plain(EditValueType(item));
		return type == typeof(bool) ? MokaColumnEditor.Checkbox
			: TableValues.IsNumber(type) ? MokaColumnEditor.Number
			: TableValues.IsDate(type) ? MokaColumnEditor.Date
			: type.IsEnum ? MokaColumnEditor.Select
			: MokaColumnEditor.Text;
	}

	/// <summary>Whether the column can write a value into a row: <see cref="SetField" /> is set, or a kind's own setter.</summary>
	protected internal virtual bool CanWrite => SetField is not null;

	/// <summary>Writes a value of the column's type into a row through <see cref="SetField" />, or a kind's own setter.</summary>
	/// <param name="item">The row.</param>
	/// <param name="value">The value.</param>
	/// <returns>The row with the value written.</returns>
	protected internal virtual TItem WriteValue(TItem item, object? value) => SetField is null ? item : SetField(item, value);

	/// <summary>
	///     Reads text, as a pasted cell holds it, as a value of the column's type, in the current culture or the invariant
	///     one. A column kind reads its own words, such as Yes and No.
	/// </summary>
	/// <param name="text">The text.</param>
	/// <param name="item">The row the value goes into.</param>
	/// <param name="value">The value read.</param>
	/// <returns>Whether the text reads as a value.</returns>
	protected internal virtual bool TryParseText(string text, TItem item, out object? value)
	{
		ArgumentNullException.ThrowIfNull(text);
		return TableValues.TryParse(text, EditValueType(item), out value);
	}

	/// <summary>
	///     The value a spreadsheet export writes in the cell: a number, a date, a yes or no, or text. The <see cref="Field" />'s
	///     value when it is one of those, else <see cref="ExportTextOf" />.
	/// </summary>
	/// <param name="item">The row.</param>
	/// <returns>The value.</returns>
	protected internal virtual object? ExportValueOf(TItem item)
	{
		object? value = ValueOf(item);
		return value is null ? null
			: value is bool || value is DateTime || value is DateTimeOffset || value is DateOnly || TableValues.IsNumber(value.GetType()) ? value
			: ExportTextOf(item);
	}

	/// <summary>Asks the table to draw again, after an <see cref="Overlay" /> changed.</summary>
	protected void Redraw() => ParentTable?.RedrawForColumn();

	/// <summary>
	///     The value of a <c>data-moka-act</c> attribute for a control in this column's cells. The table listens for clicks
	///     on such controls once, for every row, and calls <see cref="HandleCellActionAsync" /> with the row and the action,
	///     so a cell needs no click handler of its own.
	/// </summary>
	/// <param name="action">The action's name, unique in the column.</param>
	/// <returns>The attribute's value.</returns>
	protected string ActionAttribute(string action) => string.Create(CultureInfo.InvariantCulture, $"{ActionKey}:{action}");

	/// <summary>
	///     Runs an action a control in one of the column's cells asked for through <see cref="ActionAttribute" />. Nothing by
	///     default. The table draws again afterwards.
	/// </summary>
	/// <param name="item">The row whose cell holds the control.</param>
	/// <param name="action">The action's name.</param>
	/// <param name="click">Where the click was. A click from the keyboard reports no position and a detail of 0.</param>
	/// <returns>A task that completes once the action has run.</returns>
	protected internal virtual Task HandleCellActionAsync(TItem item, string action, MouseEventArgs click) => Task.CompletedTask;

	/// <summary>Leaves the table when the column goes away.</summary>
	/// <param name="disposing">True when called from <see cref="Dispose()" />.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			ParentTable?.RemoveColumn(this);
		}
	}

	/// <inheritdoc />
	protected override void OnInitialized() => ParentTable?.AddColumn(this);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		ParentTable?.SawColumn(this);
		RenderedSettings settings = CurrentSettings();
		if (_lastSettings is not { } previous)
		{
			_lastSettings = settings;
			return;
		}

		if (previous == settings)
		{
			return;
		}

		_lastSettings = settings;

		// The table renders before its columns get their new parameters, so without this it would
		// show them one render late.
		ParentTable?.OnColumnChanged(this, previous.Visible != settings.Visible, previous.Pin != settings.Pin, previous.Title);
	}

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
	}

	object? ITableField<TItem>.ValueOf(TItem item) => ValueOf(item);

	string ITableField<TItem>.TextOf(TItem item) => TextOf(item);

	string ITableField<TItem>.GroupKeyOf(TItem item) => GroupKeyFor(item);

	// Delegates and templates count only by whether they are set. A parent render passes new ones
	// every time, and the table calls them as it renders, so comparing them would cost every table
	// a second render on every parent render.
	private RenderedSettings CurrentSettings() => new(
		Title, Field is not null, CellTemplate is not null, HeaderTemplate is not null, Sortable, DefaultSort, Filterable,
		FilterType, FilterOptions is null ? null : string.Join(Separator, FilterOptions.Select(static o => o.Value + Separator + o.Text)),
		Width, MinWidth, Align, Visible, Pin, CellClass, SortComparer is not null, GroupKey is not null,
		Format, HideOnMobile, Resizable, Editable, Editor, EditOptions is null ? null : string.Join(Separator, EditOptions.Select(static o => o.Value + Separator + o.Text)),
		EditTemplate is not null, CanWrite, Validate is not null, Property, Aggregate, AggregateFormat, Phone, Fixed, Mono, NoWrap, Class, KindSettings);

	/// <summary>What the table renders from a column.</summary>
	private sealed record RenderedSettings(
		string? Title,
		bool HasField,
		bool HasCellTemplate,
		bool HasHeaderTemplate,
		bool Sortable,
		MokaSortDirection DefaultSort,
		bool Filterable,
		MokaColumnFilterType FilterType,
		string? FilterOptions,
		string? Width,
		string? MinWidth,
		MokaTextAlign Align,
		bool Visible,
		MokaColumnPin Pin,
		string? CellClass,
		bool HasSortComparer,
		bool HasGroupKey,
		string? Format,
		bool HideOnMobile,
		bool Resizable,
		bool Editable,
		MokaColumnEditor Editor,
		string? EditOptions,
		bool HasEditTemplate,
		bool CanWrite,
		bool HasValidate,
		string? Property,
		MokaAggregateType Aggregate,
		string? AggregateFormat,
		MokaColumnPhone? Phone,
		bool Fixed,
		bool Mono,
		bool NoWrap,
		string? Class,
		object? Kind);
}
