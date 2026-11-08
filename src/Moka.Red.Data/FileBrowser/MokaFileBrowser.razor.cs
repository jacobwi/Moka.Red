using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.ContextMenu;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Data.FileBrowser;

/// <summary>
///     A folder of items with navigation, a filter, sorting, a list and a tile view, selection, keyboard control and
///     drag and drop. The app gives it a way to load a folder and to describe an item, and each action it wants to offer
///     as a callback: an action left null doesn't appear. Permissions, transfers and what gets stored are the app's.
/// </summary>
/// <typeparam name="TItem">The app's own type for an item in a folder.</typeparam>
public partial class MokaFileBrowser<TItem> : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Data/FileBrowser/MokaFileBrowser.razor.js";

	// Below this every row is on the page. Above it only those in view are.
	private const int VirtualizeFrom = 200;

	private const float RowEstimate = 32;
	private const float TileRowEstimate = 124;
	private const int Overscan = 8;
	private const int FilterDelay = 150;

	private static readonly TimeSpan AbsoluteDatesAfter = TimeSpan.FromDays(30);

	private static readonly IReadOnlyList<MokaFileColumn<TItem>> DefaultColumns =
	[
		new(MokaFileSort.Size),
		new(MokaFileSort.Modified)
	];

	private readonly string _idBase = $"moka-file-{Guid.NewGuid():N}";
	private readonly MokaFileHistory _history = new();
	private readonly Dictionary<string, MokaFileRow<TItem>> _byKey = new(StringComparer.Ordinal);

	private IReadOnlyList<MokaFileColumn<TItem>> _columns = DefaultColumns;
	private IReadOnlyList<MokaFileColumn<TItem>>? _lastColumns;
	private DotNetObjectReference<MokaFileBrowser<TItem>>? _dotNetRef;
	private IMokaContextMenuService? _menuService;
	private ElementReference _root;
	private string? _folder;
	private string? _lastFolder;
	private string _filter = "";
	private MokaFileView _view = new();
	private MokaFileView? _lastView;
	private List<MokaFileRow<TItem>> _rows = [];
	private List<MokaFileRow<TItem>> _visible = [];
	private List<IReadOnlyList<MokaFileRow<TItem>>> _tileRows = [];
	private int _tileColumns = 4;
	private bool _measureColumns;
	private bool _coarsePointer;
	private bool _focusListWanted;

	// A render already on its way to the browser can finish after the list changed, and focusing from its after-render
	// would run before the render that draws the list. These say which render the wait is for.
	private int _rendersBuilt;
	private int _rendersFinished;
	private int _focusListAt;
	private bool _sideOpen;
	private bool _detailsOpen;

	/// <summary>Reads a folder. Cancelled when another load replaces it.</summary>
	[Parameter]
	[EditorRequired]
	public Func<string, CancellationToken, Task<IReadOnlyList<TItem>>> Load { get; set; } = default!;

	/// <summary>Tells the browser what an item is. Called once per item per load.</summary>
	[Parameter]
	[EditorRequired]
	public Func<TItem, MokaFileInfo> Describe { get; set; } = default!;

	/// <summary>The key of the folder on screen.</summary>
	[Parameter]
	[EditorRequired]
	public string Folder { get; set; } = "";

	/// <summary>Raised when the browser moves to another folder.</summary>
	[Parameter]
	public EventCallback<string> FolderChanged { get; set; }

	/// <summary>The path to the folder on screen, from the top. The last entry is the folder itself.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaFileCrumb> Crumbs { get; set; } = [];

	/// <summary>Renames an item, given the new name. A throw shows in the field, and success reloads.</summary>
	[Parameter]
	public Func<TItem, string, Task>? Rename { get; set; }

	/// <summary>Makes a folder, given the folder to make it in and its name. A throw shows in the field.</summary>
	[Parameter]
	public Func<string, string, Task>? CreateFolder { get; set; }

	/// <summary>Deletes items. <see cref="ConfirmDelete" /> runs first.</summary>
	[Parameter]
	public Func<IReadOnlyList<TItem>, Task>? Delete { get; set; }

	/// <summary>Moves items to a folder, given its key. Offers cut and paste, and dragging onto a folder.</summary>
	[Parameter]
	public Func<IReadOnlyList<TItem>, string, Task>? Move { get; set; }

	/// <summary>Copies items to a folder, given its key. Offers copy, paste and duplicate.</summary>
	[Parameter]
	public Func<IReadOnlyList<TItem>, string, Task>? Copy { get; set; }

	/// <summary>Receives files for a folder, given its key. Read them before the task ends.</summary>
	[Parameter]
	public Func<IReadOnlyList<IBrowserFile>, string, Task>? Upload { get; set; }

	/// <summary>Downloads items. A success doesn't reload, since nothing in the folder changed.</summary>
	[Parameter]
	public Func<IReadOnlyList<TItem>, Task>? Download { get; set; }

	/// <summary>Raised when a file is opened. Folders open in the browser.</summary>
	[Parameter]
	public EventCallback<TItem> OnOpenFile { get; set; }

	/// <summary>
	///     Asks whether to delete, with the app's own wording. Null shows the browser's own dialog, using
	///     <see cref="MokaFileBrowserText.DeleteTitle" /> and <see cref="MokaFileBrowserText.DeleteMessage" />.
	/// </summary>
	[Parameter]
	public Func<IReadOnlyList<TItem>, Task<bool>>? ConfirmDelete { get; set; }

	/// <summary>Checks a new name, returning an error message that blocks it, or null when it is fine.</summary>
	[Parameter]
	public Func<string, string?>? ValidateName { get; set; }

	/// <summary>Builds the menu of the items right-clicked. Null shows the standard one.</summary>
	[Parameter]
	public Func<MokaFileMenuContext<TItem>, IReadOnlyList<MokaContextMenuItem>>? ItemMenu { get; set; }

	/// <summary>Builds the menu of the empty space in the list. Null shows the standard one.</summary>
	[Parameter]
	public Func<MokaFileMenuContext<TItem>, IReadOnlyList<MokaContextMenuItem>>? BackgroundMenu { get; set; }

	/// <summary>The columns after the name. Null shows size and modified.</summary>
	[Parameter]
	public IReadOnlyList<MokaFileColumn<TItem>>? Columns { get; set; }

	/// <summary>A pane beside the list, such as a tree of folders. In a narrow browser a button opens it in a drawer.</summary>
	[Parameter]
	public RenderFragment? Side { get; set; }

	/// <summary>A pane about the selection, on the other side. It receives the selected items, none when nothing is.</summary>
	[Parameter]
	public RenderFragment<IReadOnlyList<TItem>>? Details { get; set; }

	/// <summary>More controls at the end of the second toolbar row.</summary>
	[Parameter]
	public RenderFragment? ToolbarEnd { get; set; }

	/// <summary>More items at the end of the status bar, as <c>MokaStatusBarItem</c>s.</summary>
	[Parameter]
	public RenderFragment? StatusEnd { get; set; }

	/// <summary>Shows in place of the list when a folder can't be read, such as a prompt to unlock it or to reconnect.</summary>
	[Parameter]
	public RenderFragment<MokaFileProblem>? Problem { get; set; }

	/// <summary>The keys of the selected items.</summary>
	[Parameter]
	public IReadOnlySet<string>? Selected { get; set; }

	/// <summary>Raised when the selection changes.</summary>
	[Parameter]
	public EventCallback<IReadOnlySet<string>> SelectedChanged { get; set; }

	/// <summary>The view, sort and direction. Bind it to keep them.</summary>
	[Parameter]
	public MokaFileView? View { get; set; }

	/// <summary>Raised when the view, sort or direction changes.</summary>
	[Parameter]
	public EventCallback<MokaFileView> ViewChanged { get; set; }

	/// <summary>Every word the browser writes.</summary>
	[Parameter]
	public MokaFileBrowserText Text { get; set; } = new();

	/// <summary>The browser's height as a CSS length. Default is the full height of its parent, at least 20rem.</summary>
	[Parameter]
	public string? Height { get; set; }

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-file-browser";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("height", Height)
		.AddStyle(Style)
		.Build();

	private bool Busy => _loading || _busy;

	// What was typed stays in the box as it is, trailing space and all. Only the match ignores the ends.
	private string FilterNeedle => _filter.Trim();

	private bool IsFiltering => FilterNeedle.Length > 0;

	private string CurrentFolder => _folder ?? Folder;

	private string? ParentFolder => Crumbs.Count >= 2 ? Crumbs[^2].FolderKey : null;

	private bool UsesSharedMenu => _menuService is { HasHost: true };

	private string ListClass => new CssBuilder("moka-file-browser__list moka-thin-scrollbar")
		.AddClass("moka-file-browser__list--tiles", _view.Mode == MokaFileViewMode.Tiles)
		.Build();

	private string? ListStyle => _view.Mode == MokaFileViewMode.Tiles
		? new StyleBuilder().AddStyle("--_fb-columns", FormattableString.Invariant($"{_tileColumns}")).Build()
		: null;

	private string? ActiveOptionId => CursorRow is { } row && IsShowing(row) ? OptionId(row.Index) : null;

	private string CountText => IsFiltering
		? Text.FilteredCount(_visible.Count, _rows.Count)
		: Text.ItemCount(_rows.Count);

	private string AnnouncedText => _loading ? "" : CountText;

	private string? SizeText
	{
		get
		{
			IEnumerable<MokaFileRow<TItem>> counted = _selectedRows.Count > 0 ? _selectedRows : _visible;
			long total = 0;
			bool any = false;
			foreach (MokaFileRow<TItem> row in counted)
			{
				if (!row.Info.IsFolder && row.Info.Size is { } size)
				{
					total += size;
					any = true;
				}
			}

			return any ? MokaByteFormat.Format(total) : null;
		}
	}

	private IReadOnlyList<TItem> SelectedItems => _selectedRows.ConvertAll(row => row.Item);

	/// <summary>Reads the folder on screen again, keeping the selection where it can.</summary>
	/// <returns>A task that ends when the folder has been read.</returns>
	public Task ReloadAsync() => InvokeAsync(() => LoadAsync(fresh: false));

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized() => _menuService = Services.GetService<IMokaContextMenuService>();

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		await base.OnParametersSetAsync();
		ApplyColumnsParameter();
		ApplyViewParameter();

		bool newFolder = !string.Equals(Folder, _lastFolder, StringComparison.Ordinal) &&
		                 !string.Equals(Folder, _folder, StringComparison.Ordinal);
		_lastFolder = Folder;
		if (newFolder)
		{
			_folder = Folder;
			_history.Visit(Folder);
			ForgetFolder();
		}

		// After the folder is forgotten, so a selection passed with a new folder belongs to that folder.
		ApplySelectedParameter();

		if (newFolder)
		{
			await LoadAsync(fresh: true);
		}
	}

	private void ApplyColumnsParameter()
	{
		if (!ReferenceEquals(Columns, _lastColumns))
		{
			_lastColumns = Columns;
			_columns = Columns ?? DefaultColumns;
			RebuildVisible();
		}
	}

	private void ApplyViewParameter()
	{
		if (View is null || View == _lastView)
		{
			return;
		}

		_lastView = View;
		if (View != _view)
		{
			_view = View;
			_measureColumns = _view.Mode == MokaFileViewMode.Tiles;
			RebuildVisible();
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		_rendersFinished++;
		if (firstRender)
		{
			_dotNetRef = DotNetObjectReference.Create(this);
			_coarsePointer = await SafeModuleInvokeAsync<bool>(ModulePath, "bind", _root, _dotNetRef);
		}

		if (_measureColumns)
		{
			_measureColumns = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "measure", _root);
		}

		if (_focusListWanted && !_loading && _rendersFinished >= _focusListAt)
		{
			_focusListWanted = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "focusListIfLost", _root);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		await CancelRunningLoadAsync();

		if (_dotNetRef is not null)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "unbind", _root);
			_dotNetRef.Dispose();
			_dotNetRef = null;
		}

		await base.DisposeAsyncCore();
	}

	private bool IsShowing(MokaFileRow<TItem> row) =>
		row.Index < _visible.Count && ReferenceEquals(_visible[row.Index], row);

	private void FocusListWhenDrawn()
	{
		_focusListWanted = true;
		_focusListAt = _rendersBuilt + 1;
	}

	private string OptionId(int index) => FormattableString.Invariant($"{_idBase}-{index}");

	private bool Shows(string id) => _columns.Any(column => string.Equals(column.Id, id, StringComparison.Ordinal));

	private string ColumnTitle(MokaFileColumn<TItem> column) => column.Title ?? column.Id switch
	{
		MokaFileSort.Size => Text.SortSize,
		MokaFileSort.Modified => Text.ColumnModified,
		_ => column.Id
	};

	private static bool IsBuiltIn(MokaFileColumn<TItem> column) =>
		column.Id is MokaFileSort.Size or MokaFileSort.Modified && column.Text is null && column.Cell is null;

	private static bool IsSortable(MokaFileColumn<TItem> column) => column.Compare is not null || IsBuiltIn(column);

	private static string? WidthStyle(MokaFileColumn<TItem> column) => new StyleBuilder()
		.AddStyle("flex-basis", column.Width ?? (column.Id == MokaFileSort.Modified ? "9rem" : column.Id == MokaFileSort.Size ? "6rem" : "8rem"))
		.Build();

	private static MokaFileColumnFit FitOf(MokaFileColumn<TItem> column) => column.Fit ?? column.Id switch
	{
		MokaFileSort.Size => MokaFileColumnFit.Always,
		MokaFileSort.Modified => MokaFileColumnFit.Medium,
		_ => MokaFileColumnFit.Wide
	};

	private static string FitClass(MokaFileColumn<TItem> column) => FitOf(column) switch
	{
		MokaFileColumnFit.Medium => "moka-file-browser__cell--medium",
		MokaFileColumnFit.Wide => "moka-file-browser__cell--wide",
		_ => ""
	};

	private static string CellClass(MokaFileColumn<TItem> column) => new CssBuilder("moka-file-browser__cell")
		.AddClass(FitClass(column))
		.AddClass("moka-file-browser__cell--number", IsBuiltIn(column) || column.Mono)
		.Build();

	private static string HeadingClass(MokaFileColumn<TItem> column) => new CssBuilder("moka-file-browser__heading")
		.AddClass(FitClass(column))
		.AddClass("moka-file-browser__heading--number", IsBuiltIn(column) || column.Mono)
		.Build();

	private string ModeClass(MokaFileViewMode mode) => new CssBuilder("moka-file-browser__mode")
		.AddClass("moka-file-browser__mode--on", _view.Mode == mode)
		.Build();

	private string ItemClass(MokaFileRow<TItem> row, string baseClass) => new CssBuilder(baseClass)
		.AddClass($"{baseClass}--selected", _selection.Contains(row.Key))
		.AddClass($"{baseClass}--cursor", string.Equals(_cursor, row.Key, StringComparison.Ordinal))
		.AddClass($"{baseClass}--held", _held is { Cut: true } held && held.Keys.Contains(row.Key))
		.AddClass($"{baseClass}--editing", IsEditing(row))
		.AddClass("moka-file-browser__line", baseClass == "moka-file-browser__row")
		.Build();

	private static MokaIconDefinition IconOf(MokaFileRow<TItem> row) =>
		row.Info.Icon ?? (row.Info.IsFolder ? MokaIcons.File.Folder : MokaIcons.File.Document);

	private static MokaColor? ColorOf(MokaFileRow<TItem> row) =>
		row.Info.IconColor ?? (row.Info.IsFolder ? MokaColor.Warning : null);

	private static string? SizeOf(MokaFileRow<TItem> row) =>
		row.Info.IsFolder || row.Info.Size is not { } size ? null : MokaByteFormat.Format(size);

	private Task OpenSide()
	{
		_sideOpen = true;
		return Task.CompletedTask;
	}

	private Task OpenDetails()
	{
		_detailsOpen = true;
		return Task.CompletedTask;
	}
}
