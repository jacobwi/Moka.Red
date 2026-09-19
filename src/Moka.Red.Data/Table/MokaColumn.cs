using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Table;

/// <summary>
///     Defines a column in a <see cref="MokaTable{TItem}" />.
///     Add as child content of MokaTable to define columns.
///     This is a headless configuration component - it renders no visible output.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public sealed class MokaColumn<TItem> : ComponentBase, IDisposable
{
	// The settings the table renders, as the parent last passed them. Null before the first set.
	private RenderedSettings? _lastSettings;

	/// <summary>
	///     Column header text. Sorting and column filters name the column by it, including in the
	///     <see cref="MokaTableState" /> a server-side data source receives, so keep titles unique.
	/// </summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Property selector for the cell value. Used for sorting and default rendering.</summary>
	[Parameter]
	public Func<TItem, object?>? Field { get; set; }

	/// <summary>Custom cell template. Receives the row item.</summary>
	[Parameter]
	public RenderFragment<TItem>? CellTemplate { get; set; }

	/// <summary>Custom header template. Overrides Title.</summary>
	[Parameter]
	public RenderFragment? HeaderTemplate { get; set; }

	/// <summary>
	///     Whether clicking the header sorts by this column. Default true. The header sorts only when
	///     the column has a <see cref="Title" /> and, with client-side data, a <see cref="Field" /> or a
	///     <see cref="SortComparer" /> to order the rows by. Under server-side data the title is enough.
	/// </summary>
	[Parameter]
	public bool Sortable { get; set; } = true;

	/// <summary>
	///     Whether this column gets an input in the filter row. Requires
	///     <see cref="MokaTable{TItem}.ShowFilters" /> on the parent table.
	/// </summary>
	[Parameter]
	public bool Filterable { get; set; }

	/// <summary>Width as CSS value (e.g., "200px", "30%", "1fr"). Null = auto.</summary>
	[Parameter]
	public string? Width { get; set; }

	/// <summary>Minimum width. Prevents column from shrinking too small.</summary>
	[Parameter]
	public string? MinWidth { get; set; }

	/// <summary>Text alignment for cells. Default Left.</summary>
	[Parameter]
	public MokaTextAlign Align { get; set; } = MokaTextAlign.Left;

	/// <summary>
	///     Whether the column starts out shown. Default true. The table's column toggle can show or
	///     hide it afterwards, and passing a new value replaces what the user picked there.
	/// </summary>
	[Parameter]
	public bool Visible { get; set; } = true;

	/// <summary>Whether this column is sticky (fixed position on horizontal scroll).</summary>
	[Parameter]
	public bool Sticky { get; set; }

	/// <summary>CSS class applied to cells in this column.</summary>
	[Parameter]
	public string? CellClass { get; set; }

	/// <summary>
	///     Compares two rows in place of their <see cref="Field" /> values, in single and multi-column
	///     sorts alike. A column with a comparer sorts without a Field.
	/// </summary>
	[Parameter]
	public Func<TItem, TItem, int>? SortComparer { get; set; }

	/// <summary>Format string for the cell value (e.g., "C2" for currency, "d" for date).</summary>
	[Parameter]
	public string? Format { get; set; }

	/// <summary>Whether to hide this column on small screens.</summary>
	[Parameter]
	public bool HideOnMobile { get; set; }

	/// <summary>Whether this column can be resized by dragging. Default true.</summary>
	[Parameter]
	public bool Resizable { get; set; } = true;

	/// <summary>
	///     Filter type: Text (free text) or Select (dropdown of distinct values). Default Text.
	///     The Select options come from the rows the table has loaded, which under server-side
	///     data is the current page only, unless the table's pager is hidden and it loads every row.
	/// </summary>
	[Parameter]
	public MokaColumnFilterType FilterType { get; set; } = MokaColumnFilterType.Text;

	/// <summary>
	///     Whether cells in this column can be edited in place. Default false. Double-click a cell,
	///     or press Enter while it has keyboard focus, to start editing; Enter or blur commits and
	///     Escape cancels.
	/// </summary>
	[Parameter]
	public bool Editable { get; set; }

	/// <summary>
	///     Callback when a cell value is edited. Receives (item, newValue), where newValue is the
	///     entered text. Raised only when that text differs from the text the editor opened with,
	///     which is the cell's text with <see cref="Format" /> applied.
	/// </summary>
	[Parameter]
	public EventCallback<(TItem Item, object? NewValue)> OnCellEdited { get; set; }

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

	[CascadingParameter] private MokaTable<TItem>? ParentTable { get; set; }

	/// <inheritdoc />
	public void Dispose()
	{
		ParentTable?.RemoveColumn(this);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	protected override void OnInitialized() => ParentTable?.AddColumn(this);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
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
		ParentTable?.OnColumnChanged(this, previous.Visible != settings.Visible, previous.Title);
	}

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
	}

	// Delegates and templates count only by whether they are set. A parent render passes new ones
	// every time, and the table calls them as it renders, so comparing them would cost every table
	// a second render on every parent render.
	private RenderedSettings CurrentSettings() => new(
		Title, Field is not null, CellTemplate is not null, HeaderTemplate is not null, Sortable, Filterable,
		FilterType, Width, MinWidth, Align, Visible, Sticky, CellClass, SortComparer is not null, Format,
		HideOnMobile, Resizable, Editable, Aggregate, AggregateFormat);

	/// <summary>What the table renders from a column.</summary>
	private readonly record struct RenderedSettings(
		string? Title,
		bool HasField,
		bool HasCellTemplate,
		bool HasHeaderTemplate,
		bool Sortable,
		bool Filterable,
		MokaColumnFilterType FilterType,
		string? Width,
		string? MinWidth,
		MokaTextAlign Align,
		bool Visible,
		bool Sticky,
		string? CellClass,
		bool HasSortComparer,
		string? Format,
		bool HideOnMobile,
		bool Resizable,
		bool Editable,
		MokaAggregateType Aggregate,
		string? AggregateFormat);
}
