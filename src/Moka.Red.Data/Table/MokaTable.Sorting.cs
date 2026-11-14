using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Moka.Red.Data.Table;

// Sorting from the headings: one list of sorts, first sort first, for single and multi-column sorts alike.
public partial class MokaTable<TItem>
{
	// Every active sort, single-column sorts included, in priority order.
	private readonly List<MokaTableSort> _sorts = [];

	// Whether the sorts are the columns' defaults, which a column arriving later adds to.
	private bool _sortsFromDefault = true;

	/// <summary>Allow sorting by multiple columns with Shift and a click. Default false.</summary>
	[Parameter]
	public bool MultiSort { get; set; }

	/// <summary>
	///     Whether a click on a column sorted both ways clears its sort. Default true: ascending, descending, then
	///     unsorted. False flips the sorted column between its two directions and never clears it, in a
	///     <see cref="MultiSort" /> too; picking another column still replaces a single sort.
	/// </summary>
	[Parameter]
	public bool AllowUnsort { get; set; } = true;

	private MokaTableSort? SortOf(string? columnTitle) => _sorts.Find(sort => sort.Column == columnTitle);

	private int SortPriority(MokaTableSort sort) => _sorts.IndexOf(sort) + 1;

	// With client-side data a click has to have something to order the rows by. A server-side
	// source sorts by the title it receives.
	private bool CanSort(MokaColumn<TItem> col) =>
		col.Sortable
		&& col.Title is not null
		&& (ServerData is not null || col.HasValue || col.SortComparer is not null);

	// On the header cell, so a click anywhere in it sorts, and so does the sort button's Enter or
	// Space, whose click bubbles up with Shift for a multi-column sort. Headers that cannot sort
	// get no handler.
	private EventCallback<MouseEventArgs> SortClickHandler(MokaColumn<TItem> col) =>
		CanSort(col) ? HandlersFor(col).Sort : default;

	// ARIA wants aria-sort on one header at a time, so a multi-column sort names only its first
	// column.
	private string? AriaSort(MokaColumn<TItem> col)
	{
		if (!CanSort(col) || _sorts.Count == 0 || _sorts[0].Column != col.Title)
		{
			return null;
		}

		return _sorts[0].Direction switch
		{
			MokaSortDirection.Ascending => "ascending",
			MokaSortDirection.Descending => "descending",
			_ => null
		};
	}

	// Without a HeaderTemplate the button holds the title, which names it.
	private string? SortButtonLabel(MokaColumn<TItem> col) =>
		col.HeaderTemplate is null ? null : Words.SortBy(col.Title ?? string.Empty);

	private async Task HandleSort(MokaColumn<TItem> column, bool addToExisting = false)
	{
		if (!CanSort(column) || column.Title is not { } title)
		{
			return;
		}

		if (MultiSort && addToExisting)
		{
			ApplyMultiSort(column, title);
		}
		else
		{
			ApplySingleSort(column, title);
		}

		_sortsFromDefault = false;
		ResetPaging();
		await LoadDataAsync();
		await NotifyStateChangedAsync();
	}

	private void ApplyMultiSort(MokaColumn<TItem> column, string title)
	{
		int index = _sorts.FindIndex(sort => sort.Column == title);
		if (index < 0)
		{
			_sorts.Add(new MokaTableSort(title, FirstSortDirection(column)));
			return;
		}

		MokaSortDirection next = NextSortDirection(column, _sorts[index].Direction);
		if (next == MokaSortDirection.None)
		{
			_sorts.RemoveAt(index);
		}
		else
		{
			_sorts[index] = _sorts[index] with { Direction = next };
		}
	}

	private void ApplySingleSort(MokaColumn<TItem> column, string title)
	{
		MokaSortDirection next = _sorts.Count > 0 && _sorts[0].Column == title
			? NextSortDirection(column, _sorts[0].Direction)
			: FirstSortDirection(column);

		_sorts.Clear();
		if (next != MokaSortDirection.None)
		{
			_sorts.Add(new MokaTableSort(title, next));
		}
	}

	private static MokaSortDirection FirstSortDirection(MokaColumn<TItem> column) =>
		column.DefaultSort != MokaSortDirection.None ? column.DefaultSort
		: column.SortDescendingFirst ? MokaSortDirection.Descending
		: MokaSortDirection.Ascending;

	// The first direction, then the other, then unsorted, or round to the first again when the sort can't be cleared.
	private MokaSortDirection NextSortDirection(MokaColumn<TItem> column, MokaSortDirection current)
	{
		MokaSortDirection first = FirstSortDirection(column);
		MokaSortDirection second = first == MokaSortDirection.Ascending ? MokaSortDirection.Descending : MokaSortDirection.Ascending;
		if (current == first)
		{
			return second;
		}

		if (current == second)
		{
			return AllowUnsort ? MokaSortDirection.None : first;
		}

		return first;
	}
}
