using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Enums;

namespace Moka.Red.Data.Table;

/// <summary>
///     Defines a column in a <see cref="MokaTable{TItem}" />.
///     Add as child content of MokaTable to define columns.
///     This is a headless configuration component — it renders no visible output.
/// </summary>
/// <typeparam name="TItem">The row data type.</typeparam>
public sealed class MokaColumn<TItem> : ComponentBase, IDisposable
{
	/// <summary>Column header text.</summary>
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

	/// <summary>Whether this column is sortable. Default true if Field is set.</summary>
	[Parameter]
	public bool Sortable { get; set; } = true;

	/// <summary>Whether this column is filterable.</summary>
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

	/// <summary>Whether this column is visible. Default true.</summary>
	[Parameter]
	public bool Visible { get; set; } = true;

	/// <summary>Whether this column is sticky (fixed position on horizontal scroll).</summary>
	[Parameter]
	public bool Sticky { get; set; }

	/// <summary>CSS class applied to cells in this column.</summary>
	[Parameter]
	public string? CellClass { get; set; }

	/// <summary>Custom sort comparer. Overrides default Field-based sorting.</summary>
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

	/// <summary>Filter type: Text (free text) or Select (distinct values dropdown). Default Text.</summary>
	[Parameter]
	public MokaColumnFilterType FilterType { get; set; } = MokaColumnFilterType.Text;

	/// <summary>Whether cells in this column are editable. Default false.</summary>
	[Parameter]
	public bool Editable { get; set; }

	/// <summary>Callback when a cell value is edited. Receives (item, newValue).</summary>
	[Parameter]
	public EventCallback<(TItem Item, object? NewValue)> OnCellEdited { get; set; }

	/// <summary>Aggregation function for this column's footer. Default None.</summary>
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
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
	}
}
