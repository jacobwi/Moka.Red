using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.Table;

// What people arrange and keep: the columns (the chooser, a heading's edge) and saved views. Both are parts of the one
// state record.
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

	// The arrangement in force as far as anyone was told: the last one applied or raised. Columns register after the
	// first render and can come back later, so each one that arrives takes its place from this.
	private IReadOnlyList<MokaTableColumnState>? _arrangementInForce;

	private bool _chooserOpen;
	private string[]? _focusAfterRender;
	private bool _focusViewName;
	private bool _namingView;
	private string? _viewName;
	private string? _viewNameProblem;
	private ElementReference _viewNameRef;

	// The view the table shows, taken once per render that draws the view chips.
	private MokaTableState? _viewNow;

	/// <summary>
	///     Shows a Columns button and a panel to show, hide and order the columns. What someone arranges is part of the
	///     <see cref="State" />. Default false.
	/// </summary>
	[Parameter]
	public bool ShowColumnChooser { get; set; }

	/// <summary>
	///     Saved views to offer as chips over the table. Picking a chip puts its state in force; the chip is lit while the
	///     table shows that view. Null shows none.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaTableView>? Views { get; set; }

	/// <summary>
	///     Raised with a new view when someone saves how the table looks now under a name. Adding it to
	///     <see cref="Views" /> and keeping it is the app's job. Without a handler there is no "Save this view" button.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTableView> ViewSaved { get; set; }

	/// <summary>
	///     Raised when someone forgets a view. Removing it from <see cref="Views" /> is the app's job. Without a handler
	///     chips can't be forgotten, and a view of the table as it starts never can.
	/// </summary>
	[Parameter]
	public EventCallback<MokaTableView> ViewForgotten { get; set; }

	// A heading's edge takes the keyboard only where the widths are kept, since a tab stop per heading changes the
	// default keyboard path.
	private bool Arrangeable => ShowColumnChooser || StateChanged.HasDelegate || StorageKey is not null;

	private bool ShowViews => Views is not null || ViewSaved.HasDelegate;

	private string ToolbarClass => new CssBuilder("moka-table-toolbar")
		.AddClass("moka-table-toolbar--views", ShowViews)
		.Build();

	private List<MokaColumn<TItem>> ChooserColumns() =>
		_columns.Where(static col => col is { Fixed: false, Title: not null }).ToList();

	private string ColumnsButtonText => Words.ColumnsButton(ChooserColumns().Count(col => !IsColumnVisible(col)));

	private bool CanResetLayout => _columnVisibility.Count > 0 || _columnWidths.Count > 0 || _columnPins.Count > 0 ||
	                               !_columns.SequenceEqual(_registeredColumns);

	private string? ResizeRole => Arrangeable ? "separator" : null;

	private string? ResizeTabIndex => Arrangeable ? "0" : null;

	private string? ResizeOrientation => Arrangeable ? "vertical" : null;

	private string? ResizeLabel(MokaColumn<TItem> col) => Arrangeable ? Words.ResizeEdge(col.Title ?? string.Empty) : null;

	private string? ResizeValue(MokaColumn<TItem> col) =>
		Arrangeable && _columnWidths.TryGetValue(col, out double width)
			? Math.Round(width).ToString(CultureInfo.InvariantCulture)
			: null;

	private string? ColumnDraggableFor(MokaColumn<TItem> col) => ColumnReorderable && !col.Fixed ? "true" : null;

	private string? ColumnWidthText(MokaColumn<TItem> col) =>
		_columnWidths.TryGetValue(col, out double width) ? Words.WidthReset(width) : null;

	// ── Views ──

	private static MokaTableState ViewOf(MokaTableState state) =>
		state with { Page = 1, Density = null, CollapsedGroups = [], Expanded = [] };

	private bool IsActiveView(MokaTableView view) => _viewNow is not null && Resolve(view.State).SameView(_viewNow);

	private bool CanForgetView(MokaTableView view) => ViewForgotten.HasDelegate && !Resolve(view.State).SameView(DefaultState());

	private string ViewClass(MokaTableView view) => new CssBuilder("moka-table-view")
		.AddClass("moka-table-view--on", IsActiveView(view))
		.Build();

	private bool ShowsDefaultView => _viewNow is null || _viewNow.SameView(DefaultState());

	private bool CanSaveView => !ShowsDefaultView && !(Views?.Any(IsActiveView) ?? false);

	private string SaveViewHint =>
		ShowsDefaultView ? Words.SaveViewNothing
		: CanSaveView ? Words.SaveViewHint
		: Words.SaveViewSame;

	private string ViewTitle(MokaTableView view) => Words.ViewSummary(view.State);

	// ── Arrangement ──

	private void ApplyArrangement(IReadOnlyList<MokaTableColumnState> arrangement)
	{
		if (arrangement.Count == 0)
		{
			ForgetArrangement();
		}
		else
		{
			ApplyOrder(arrangement);

			Dictionary<string, MokaTableColumnState> byTitle = new(StringComparer.Ordinal);
			foreach (MokaTableColumnState column in arrangement)
			{
				byTitle.TryAdd(column.Title, column);
			}

			foreach (MokaColumn<TItem> col in ChooserColumns())
			{
				_columnVisibility[col] = !(byTitle.TryGetValue(col.Title!, out MokaTableColumnState? said) && said.Hidden);
			}

			foreach (MokaColumn<TItem> col in _columns)
			{
				MokaTableColumnState? said = col.Title is null ? null : byTitle.GetValueOrDefault(col.Title);
				if (said?.Width is { } width)
				{
					_columnWidths[col] = width;
				}
				else
				{
					_columnWidths.Remove(col);
				}

				if (said is not null && said.Pin != col.Pin)
				{
					_columnPins[col] = said.Pin;
				}
				else
				{
					_columnPins.Remove(col);
				}
			}
		}

		_layoutVersion++;
		ClampFocusedCell();
	}

	private void ApplyOrder(IReadOnlyList<MokaTableColumnState> order)
	{
		Dictionary<string, int> positions = new(StringComparer.Ordinal);
		for (int i = 0; i < order.Count; i++)
		{
			positions.TryAdd(order[i].Title, i);
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
		_columnPins.Clear();
	}

	// Empty while the columns stand as their parameters set them. Titles no column has right now are kept, so a column
	// that comes and goes doesn't lose its place in the store.
	private List<MokaTableColumnState> BuildColumnStates()
	{
		List<MokaTableColumnState> columns = [];
		if (!CanResetLayout && _arrangementInForce is not { Count: > 0 })
		{
			return columns;
		}

		HashSet<string> present = new(StringComparer.Ordinal);
		foreach (MokaColumn<TItem> col in ChooserColumns())
		{
			present.Add(col.Title!);
			columns.Add(new MokaTableColumnState(col.Title!)
			{
				Hidden = !IsColumnVisible(col),
				Width = _columnWidths.TryGetValue(col, out double width) ? Math.Round(width) : null,
				Pin = PinOf(col)
			});
		}

		if (_arrangementInForce is { } last)
		{
			columns.AddRange(last.Where(column => !present.Contains(column.Title) && !_columns.Exists(col => col.Title == column.Title)));
		}

		return columns;
	}

	private async Task NoteArrangementChangedAsync()
	{
		_arrangementInForce = BuildColumnStates();
		await NotifyStateChangedAsync();
	}

	private async Task ResetArrangementAsync()
	{
		ForgetArrangement();
		_arrangementInForce = [];
		_layoutVersion++;
		ClampFocusedCell();
		if (DropFiltersWithoutControl())
		{
			_currentPage = 1;
			await LoadDataAsync();
		}

		// The button is disabled now, and focus would fall to the page.
		_focusAfterRender = FocusChooser;
		await NotifyStateChangedAsync();
	}

	private async Task MoveColumnAsync(MokaColumn<TItem> source, MokaColumn<TItem> target)
	{
		if (ReferenceEquals(source, target) || source.Fixed || target.Fixed || KeepsApart(source, target))
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

		await NoteArrangementChangedAsync();
	}

	// ── Chooser ──

	// Blazor moves a keyed row by taking it out and putting it back, which drops focus, and the button is disabled once
	// the column reaches the end. So focus goes to the same button again, or to the other one at an end.
	private async Task MoveInChooserAsync(MokaColumn<TItem> col, MokaColumn<TItem> target, bool earlier)
	{
		string title = col.Title!;
		string sameWay = MoveButtonSelector(earlier ? Words.MoveEarlier(title) : Words.MoveLater(title));
		string otherWay = MoveButtonSelector(earlier ? Words.MoveLater(title) : Words.MoveEarlier(title));
		_focusAfterRender = [sameWay, otherWay];
		await MoveColumnAsync(col, target);
	}

	private static string MoveButtonSelector(string label) =>
		$"button[aria-label=\"{label.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"]:not([disabled])";

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
			await NoteArrangementChangedAsync();
		}
	}

	// ── Views ──

	// A view sets what the table shows; the density, the page and what is folded or opened stay as they are.
	private async Task PickViewAsync(MokaTableView view)
	{
		MokaTableState now = BuildState();
		ApplyState(view.State with
		{
			Page = 1,
			Density = now.Density,
			CollapsedGroups = now.CollapsedGroups,
			Expanded = now.Expanded
		});
		await LoadDataAsync();
		StateHasChanged();
		await NotifyStateChangedAsync();
	}

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
		if (name.Length == 0)
		{
			_focusViewName = true;
			return;
		}

		if (Views?.Any(view => string.Equals(view.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)) == true)
		{
			_viewNameProblem = Words.ViewExists(name);
			_focusViewName = true;
			return;
		}

		_namingView = false;
		_viewNameProblem = null;
		_focusAfterRender = FocusSaveView;
		await ViewSaved.InvokeAsync(new MokaTableView(name, ViewOf(BuildState())));
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
