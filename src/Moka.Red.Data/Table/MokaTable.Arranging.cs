using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Table;

// What people arrange and keep: the columns (Layout, the chooser, a heading's edge) and saved views.
public partial class MokaTable<TItem>
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";
	private const int MaxViewNameLength = 40;

	// Enter in the name box would submit a form the table sits in.
	private static readonly Dictionary<string, object?>[] ViewNameKeyRules =
	[
		new()
		{
			["selector"] = null,
			["keys"] = new[] { "Enter" }
		}
	];

	private static readonly string[] FocusSaveView =
		[".moka-table-view-save:not([disabled])", ".moka-table-views button:not([disabled])"];

	private static readonly string[] FocusChooser = [".moka-table-chooser input"];

	private static readonly string[] FocusChooserToggle = [".moka-table-chooser-toggle"];

	private readonly string _chooserId = $"moka-table-columns-{Guid.NewGuid():N}";

	// The order the columns registered in, which is what "put them back" goes back to.
	private readonly List<MokaColumn<TItem>> _registeredColumns = [];

	private bool _chooserOpen;
	private string[]? _focusAfterRender;
	private bool _focusViewName;
	private MokaTableLayout? _lastLayoutParameter;

	// The arrangement in force as far as anyone was told: the last Layout applied or the last one raised. Columns
	// register after the first render and can come back later, so each one that arrives takes its place from this.
	private MokaTableLayout? _layoutInForce;
	private bool _namingView;
	private string? _viewName;
	private string? _viewNameProblem;
	private ElementReference _viewNameRef;

	/// <summary>
	///     Shows a Columns button and a panel to show, hide and order the columns. Without a <see cref="Layout" /> it
	///     still works, for as long as the table lives. Default false.
	/// </summary>
	[Parameter]
	public bool ShowColumnChooser { get; set; }

	/// <summary>
	///     How someone arranged the columns, to put back: their order, the hidden ones and the widths, by column
	///     <see cref="MokaColumn{TItem}.Title" />. Applied when it says something new. An empty layout means the columns
	///     as their parameters set them. Null leaves the arrangement alone. With this or <see cref="LayoutChanged" />
	///     a heading's right edge also takes Left and Right (16 px) and Home on the keyboard.
	/// </summary>
	[Parameter]
	public MokaTableLayout? Layout { get; set; }

	/// <summary>
	///     Raised when someone hides, moves or resizes a column, or puts the columns back. The table stores nothing,
	///     so keeping the layout per person is the app's job. Two-way bindable.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTableLayout> LayoutChanged { get; set; }

	/// <summary>The saved views to offer as chips over the table. Null shows none.</summary>
	[Parameter]
	public IReadOnlyList<MokaTableView>? Views { get; set; }

	/// <summary>
	///     The filters in force now, as a view. The chip whose filters match it is lit, and "Save this view" keeps
	///     these filters under the name typed, so the name given here is not used. Null lights no chip and offers
	///     nothing to save.
	/// </summary>
	[Parameter]
	public MokaTableView? ActiveView { get; set; }

	/// <summary>Raised when a chip is picked. Putting its filters in force is the app's job.</summary>
	[Parameter]
	public EventCallback<MokaTableView> ViewPicked { get; set; }

	/// <summary>
	///     Raised with a new view when someone saves the filters in <see cref="ActiveView" /> under a name. Adding it to
	///     <see cref="Views" /> and keeping it is the app's job. Without a handler there is no "Save this view" button.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTableView> ViewSaved { get; set; }

	/// <summary>
	///     Raised when someone forgets a view. Removing it from <see cref="Views" /> is the app's job. Without a handler
	///     chips can't be forgotten, and a view with no filters, such as "Everything", never can.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTableView> ViewForgotten { get; set; }

	private bool Arrangeable => ShowColumnChooser || LayoutChanged.HasDelegate || Layout is not null;

	private bool ShowViews => Views is not null || ViewSaved.HasDelegate;

	private string ToolbarClass => new CssBuilder("moka-table-toolbar")
		.AddClass("moka-table-toolbar--views", ShowViews)
		.Build();

	private List<MokaColumn<TItem>> ChooserColumns() =>
		_columns.Where(static col => col is { Fixed: false, Title: not null }).ToList();

	private string ColumnsButtonText
	{
		get
		{
			int hidden = ChooserColumns().Count(col => !IsColumnVisible(col));
			return hidden > 0 ? $"Columns ({hidden.ToString(CultureInfo.CurrentCulture)} hidden)" : "Columns";
		}
	}

	private bool CanResetLayout => _columnVisibility.Count > 0 || _columnWidths.Count > 0 || !_columns.SequenceEqual(_registeredColumns);

	private string? ResizeRole => Arrangeable ? "separator" : null;

	private string? ResizeTabIndex => Arrangeable ? "0" : null;

	private string? ResizeOrientation => Arrangeable ? "vertical" : null;

	private string? ResizeLabel(MokaColumn<TItem> col) =>
		Arrangeable ? $"Width of {col.Title}: left and right arrows, Home for its own" : null;

	private string? ResizeValue(MokaColumn<TItem> col) =>
		Arrangeable && _columnWidths.TryGetValue(col, out double width)
			? Math.Round(width).ToString(CultureInfo.InvariantCulture)
			: null;

	private string? ColumnDraggableFor(MokaColumn<TItem> col) => ColumnReorderable && !col.Fixed ? "true" : null;

	private string? ColumnWidthText(MokaColumn<TItem> col) =>
		_columnWidths.TryGetValue(col, out double width)
			? $"{Math.Round(width).ToString(CultureInfo.CurrentCulture)} px, reset"
			: null;

	private bool IsActiveView(MokaTableView view) => ActiveView is not null && view.HasSameFilters(ActiveView);

	private bool CanForgetView(MokaTableView view) => ViewForgotten.HasDelegate && view.Filters.Count > 0;

	private string ViewClass(MokaTableView view) => new CssBuilder("moka-table-view")
		.AddClass("moka-table-view--on", IsActiveView(view))
		.Build();

	private bool CanSaveView => ActiveView is { Filters.Count: > 0 } && !(Views?.Any(IsActiveView) ?? false);

	private string SaveViewHint =>
		ActiveView is not { Filters.Count: > 0 } ? "Pick a filter first"
		: CanSaveView ? "Keep these filters under a name"
		: "These filters are a view already";

	private static string ViewTitle(MokaTableView view) =>
		view.Filters.Count == 0
			? "Everything"
			: string.Join(", ", view.Filters.Select(filter => $"{filter.Key}: {filter.Value}"));

	// ── Layout ──

	private void SyncLayoutFromParameters()
	{
		if (ReferenceEquals(Layout, _lastLayoutParameter))
		{
			return;
		}

		_lastLayoutParameter = Layout;

		// A bound parent handing back what the table just raised has nothing new to say.
		if (Layout is null || Layout.Equals(_layoutInForce))
		{
			return;
		}

		_layoutInForce = Layout;
		ApplyLayout(Layout);
	}

	private void ApplyLayout(MokaTableLayout layout)
	{
		if (layout.IsEmpty)
		{
			ForgetArrangement();
		}
		else
		{
			ApplyOrder(layout.Order);

			HashSet<string> hidden = new(layout.Hidden, StringComparer.Ordinal);
			foreach (MokaColumn<TItem> col in ChooserColumns())
			{
				_columnVisibility[col] = !hidden.Contains(col.Title!);
			}

			foreach (MokaColumn<TItem> col in _columns)
			{
				if (col.Title is not null && layout.Widths.TryGetValue(col.Title, out double width))
				{
					_columnWidths[col] = width;
				}
				else
				{
					_columnWidths.Remove(col);
				}
			}
		}

		_layoutVersion++;
		ClampFocusedCell();
		if (DropFiltersWithoutControl())
		{
			_columnReloadPending = true;
		}
	}

	private void ApplyOrder(IReadOnlyList<string> order)
	{
		if (order.Count == 0)
		{
			return;
		}

		Dictionary<string, int> positions = new(StringComparer.Ordinal);
		for (int i = 0; i < order.Count; i++)
		{
			positions.TryAdd(order[i], i);
		}

		ArrangeMovableColumns(_columns
			.Where(static col => !col.Fixed)
			.OrderBy(col => col.Title is not null && positions.TryGetValue(col.Title, out int position) ? position : int.MaxValue)
			.ToList());
	}

	// Fixed columns keep their places, and the others fill the rest in the order given.
	private void ArrangeMovableColumns(List<MokaColumn<TItem>> movable)
	{
		int next = 0;
		for (int i = 0; i < _columns.Count; i++)
		{
			if (!_columns[i].Fixed)
			{
				_columns[i] = movable[next++];
			}
		}
	}

	private void ForgetArrangement()
	{
		_columns.Clear();
		_columns.AddRange(_registeredColumns);
		_columnVisibility.Clear();
		_columnWidths.Clear();
	}

	// Titles no column has right now are kept, so a column that comes and goes doesn't lose its place in the store.
	private MokaTableLayout BuildLayout()
	{
		List<string> order = [];
		List<string> hidden = [];
		foreach (MokaColumn<TItem> col in ChooserColumns())
		{
			order.Add(col.Title!);
			if (!IsColumnVisible(col))
			{
				hidden.Add(col.Title!);
			}
		}

		Dictionary<string, double> widths = new(StringComparer.Ordinal);
		foreach ((MokaColumn<TItem> col, double width) in _columnWidths)
		{
			if (col.Title is { } title)
			{
				widths[title] = Math.Round(width);
			}
		}

		if (_layoutInForce is { } last)
		{
			hidden.AddRange(last.Hidden.Where(title => !_columns.Exists(col => col.Title == title)));
			foreach ((string title, double width) in last.Widths)
			{
				if (!_columns.Exists(col => col.Title == title))
				{
					widths[title] = width;
				}
			}
		}

		return new MokaTableLayout { Order = order, Hidden = hidden, Widths = widths };
	}

	private async Task NoteLayoutChangedAsync()
	{
		if (!Arrangeable)
		{
			_layoutInForce = null;
			return;
		}

		_layoutInForce = BuildLayout();
		await LayoutChanged.InvokeAsync(_layoutInForce);
	}

	private async Task ResetArrangementAsync()
	{
		ForgetArrangement();
		_layoutVersion++;
		ClampFocusedCell();
		if (DropFiltersWithoutControl())
		{
			_currentPage = 1;
			await LoadDataAsync();
		}

		// The button is disabled now, and focus would fall to the page.
		_focusAfterRender = FocusChooser;
		_layoutInForce = new MokaTableLayout();
		await LayoutChanged.InvokeAsync(_layoutInForce);
	}

	private async Task MoveColumnAsync(MokaColumn<TItem> source, MokaColumn<TItem> target)
	{
		if (ReferenceEquals(source, target) || source.Fixed || target.Fixed)
		{
			return;
		}

		List<MokaColumn<TItem>> movable = _columns.Where(static col => !col.Fixed).ToList();
		int from = movable.IndexOf(source);
		int to = movable.IndexOf(target);
		if (from < 0 || to < 0)
		{
			return;
		}

		var visible = _columns.Where(IsColumnVisible).ToList();
		int oldIndex = visible.IndexOf(source);
		int newIndex = visible.IndexOf(target);

		// Taking the target's place pushes the columns in between over by one: the moved column
		// ends up after the target when it moves right and before it when it moves left.
		movable.RemoveAt(from);
		movable.Insert(to, source);
		ArrangeMovableColumns(movable);
		_layoutVersion++;

		if (oldIndex >= 0 && newIndex >= 0)
		{
			await OnColumnReordered.InvokeAsync((oldIndex, newIndex));
		}

		await NoteLayoutChangedAsync();
	}

	// ── Chooser ──

	// Blazor moves a keyed row by taking it out and putting it back, which drops focus, and the button is disabled once
	// the column reaches the end. So focus goes to the same button again, or to the other one at an end.
	private async Task MoveInChooserAsync(MokaColumn<TItem> col, MokaColumn<TItem> target, bool earlier)
	{
		string title = col.Title!.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
		string sameWay = $"button[aria-label=\"Move {title} {(earlier ? "earlier" : "later")}\"]:not([disabled])";
		string otherWay = $"button[aria-label=\"Move {title} {(earlier ? "later" : "earlier")}\"]:not([disabled])";
		_focusAfterRender = [sameWay, otherWay];
		await MoveColumnAsync(col, target);
	}

	private void ToggleChooser()
	{
		_chooserOpen = !_chooserOpen;
		_focusAfterRender = _chooserOpen ? FocusChooser : null;
	}

	private void CloseChooser()
	{
		_chooserOpen = false;
		_focusAfterRender = FocusChooserToggle;
	}

	private void HandleChooserKey(KeyboardEventArgs e)
	{
		if (e.Key == "Escape")
		{
			CloseChooser();
		}
	}

	private async Task ResetColumnWidthAsync(MokaColumn<TItem> col)
	{
		if (_columnWidths.Remove(col))
		{
			await NoteLayoutChangedAsync();
		}
	}

	// ── Views ──

	private Task PickViewAsync(MokaTableView view) => ViewPicked.InvokeAsync(view);

	private async Task ForgetViewAsync(MokaTableView view)
	{
		_focusAfterRender = FocusSaveView;
		await ViewForgotten.InvokeAsync(view);
	}

	private void StartNamingView()
	{
		_namingView = true;
		_focusViewName = true;
		_viewName = null;
		_viewNameProblem = null;
	}

	private void CancelNamingView()
	{
		_namingView = false;
		_viewNameProblem = null;
		_focusAfterRender = FocusSaveView;
	}

	private async Task SaveViewAsync()
	{
		string name = (_viewName ?? string.Empty).Trim();
		if (name.Length == 0 || ActiveView is null)
		{
			_focusViewName = true;
			return;
		}

		if (Views?.Any(view => string.Equals(view.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)) == true)
		{
			_viewNameProblem = $"There's a view called {name} already.";
			_focusViewName = true;
			return;
		}

		_namingView = false;
		_viewNameProblem = null;
		_focusAfterRender = FocusSaveView;
		await ViewSaved.InvokeAsync(new MokaTableView(name, new Dictionary<string, string>(ActiveView.Filters, StringComparer.Ordinal)));
	}

	// Typing goes through this without a render: Enter and Escape are the keys that change anything.
	private void HandleViewNameKey(KeyboardEventArgs e)
	{
		if (e.Key == "Enter" && !e.Repeat)
		{
			RunDetached(async () =>
			{
				await SaveViewAsync();
				StateHasChanged();
			});
		}
		else if (e.Key == "Escape")
		{
			CancelNamingView();
			RunDetached(StateHasChanged);
		}
	}
}
