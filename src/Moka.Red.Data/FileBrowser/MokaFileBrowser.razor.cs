using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.ContextMenu;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.JobProgress;
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
	private int? _revealLine;
	private int _revealAt;
	private bool _sideOpen;
	private bool _detailsOpen;
	private bool _itemFilterWasSet;
	private bool _raisedBusy;
	private HeldItems? _raisedHeld;
	private MokaFileFailure? _raisedFailure;
	private MokaFileFitWidths? _lastFitWidths;

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

	/// <summary>
	///     Asks before a drag moves items, given the items and the key of the folder they were dropped on. False leaves them
	///     where they are. Cut and paste doesn't ask.
	/// </summary>
	[Parameter]
	public Func<IReadOnlyList<TItem>, string, Task<bool>>? ConfirmDrop { get; set; }

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

	/// <summary>More controls at the start of the first toolbar row.</summary>
	[Parameter]
	public RenderFragment? ToolbarStart { get; set; }

	/// <summary>
	///     Draws each built-in toolbar control, in its place. Return its <see cref="MokaFileControlContext.Default" /> to
	///     keep one. <see cref="Controls" /> decides which are asked for.
	/// </summary>
	[Parameter]
	public RenderFragment<MokaFileControlContext>? ToolbarControl { get; set; }

	/// <summary>Draws what follows the icon in a list row. A row being renamed shows its field instead.</summary>
	[Parameter]
	public RenderFragment<MokaFileNameContext<TItem>>? NameCell { get; set; }

	/// <summary>Shows in place of the list when it has nothing to show.</summary>
	[Parameter]
	public RenderFragment<MokaFileEmptyContext>? Empty { get; set; }

	/// <summary>Buttons at the end of the alert that says something failed, such as a way to try again differently.</summary>
	[Parameter]
	public RenderFragment<MokaFileFailure>? FailureActions { get; set; }

	/// <summary>
	///     Words the title of the failure alert, such as to name the folder that didn't open. Null, or null returned, is
	///     the failure's own <see cref="MokaFileFailure.Title" />.
	/// </summary>
	[Parameter]
	public Func<MokaFileFailure, string?>? FailureTitle { get; set; }

	/// <summary>Which built-in toolbar controls show. Default is all of them.</summary>
	[Parameter]
	public MokaFileControls Controls { get; set; } = MokaFileControls.All;

	/// <summary>Draws New folder and Upload as icons with tooltips, for a narrow panel.</summary>
	[Parameter]
	public bool IconOnlyActions { get; set; }

	/// <summary>Which keys the browser takes. Default is all of them.</summary>
	[Parameter]
	public MokaFileKeys Keys { get; set; } = MokaFileKeys.All;

	/// <summary>Where focus has to be for the browser to take its keys. Default is the list.</summary>
	[Parameter]
	public MokaFileKeyScope KeyScope { get; set; }

	/// <summary>
	///     Lets the columns after the name be resized, by dragging the edge of a heading or from the keyboard with focus on
	///     that edge. The name takes what is left.
	/// </summary>
	[Parameter]
	public bool ResizableColumns { get; set; }

	/// <summary>
	///     The widths columns have been given, in CSS pixels by <see cref="MokaFileColumn{TItem}.Id" />, to store and hand
	///     back. A column not listed keeps its own width. Read only with <see cref="ResizableColumns" />.
	/// </summary>
	[Parameter]
	public IReadOnlyDictionary<string, double>? ColumnWidths { get; set; }

	/// <summary>Raised when a column is resized or put back, with every width given so far.</summary>
	[Parameter]
	public EventCallback<IReadOnlyDictionary<string, double>> ColumnWidthsChanged { get; set; }

	/// <summary>The narrowest a resized column can be, and the narrowest resizing leaves the name, in CSS pixels. Default 50.</summary>
	[Parameter]
	public double MinColumnWidth { get; set; } = 50;

	/// <summary>
	///     Lets a mouse press on the empty space of the list draw a box that selects every item it touches, in the list
	///     and the tiles, rows that aren't drawn included. Shift adds to the selection and Control or Command toggles.
	///     Default off. <see cref="SelectedChanged" /> is raised once the box is released.
	/// </summary>
	[Parameter]
	public bool BoxSelect { get; set; }

	/// <summary>The text in the filter box. Bind it to clear or fill the box from outside, or to replace it.</summary>
	[Parameter]
	public string? Filter { get; set; }

	/// <summary>Raised when the text in the filter box changes.</summary>
	[Parameter]
	public EventCallback<string> FilterChanged { get; set; }

	/// <summary>
	///     Hides items without reading the folder again, such as dot files. Asked again on every render of the parent, once
	///     per item, so whatever it reads can change freely.
	/// </summary>
	[Parameter]
	public Func<TItem, bool>? ItemFilter { get; set; }

	/// <summary>
	///     When a folder can't be read and there is a list on screen, keeps that list and shows an alert with a way to try
	///     again, and the folder doesn't change. Off, the problem replaces the list.
	/// </summary>
	[Parameter]
	public bool KeepListOnLoadFailure { get; set; }

	/// <summary>Going Back, Forward or Up selects the folder just left, when the folder arrived at holds it.</summary>
	[Parameter]
	public bool ReselectOnReturn { get; set; }

	/// <summary>Where the columns start to show. Null is 36rem and 52rem.</summary>
	[Parameter]
	public MokaFileFitWidths? FitWidths { get; set; }

	/// <summary>How sizes are counted, in the column, the tiles and the status bar.</summary>
	[Parameter]
	public MokaByteUnits ByteUnits { get; set; }

	/// <summary>Writes a size in bytes, unit included. When set it wins over <see cref="ByteUnits" />.</summary>
	[Parameter]
	public Func<double, string>? ByteFormatter { get; set; }

	/// <summary>
	///     The text a dragged item carries to other apps and to other parts of the page. Null is the row's text. Rows can
	///     be dragged with this alone, without a <see cref="Move" />.
	/// </summary>
	[Parameter]
	public Func<TItem, string>? DragText { get; set; }

	/// <summary>
	///     Raised by Enter when several items are selected and all are files, in place of opening the one under the
	///     cursor.
	/// </summary>
	[Parameter]
	public EventCallback<IReadOnlyList<TItem>> OnOpenFiles { get; set; }

	/// <summary>Raised by Control+C on some items when there is no <see cref="Copy" />, to tell the person why nothing happened.</summary>
	[Parameter]
	public EventCallback OnCopyUnavailable { get; set; }

	/// <summary>Raised when what is held for the next paste changes. Null when nothing is.</summary>
	[Parameter]
	public EventCallback<MokaFileHeld<TItem>?> OnHeldChanged { get; set; }

	/// <summary>Raised when <see cref="IsBusy" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> OnBusyChanged { get; set; }

	/// <summary>Raised when the failure alert appears, changes or closes. Null when it has closed.</summary>
	[Parameter]
	public EventCallback<MokaFileFailure?> OnFailureChanged { get; set; }

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	/// <summary>Whether a folder is loading or an action is running.</summary>
	public bool IsBusy => _loading || _busy;

	/// <summary>What is held for the next paste. Null when nothing is.</summary>
	public MokaFileHeld<TItem>? Held => _held is { } held ? new MokaFileHeld<TItem>(held.Rows.ConvertAll(row => row.Item), held.Cut) : null;

	/// <inheritdoc />
	protected override string RootClass => "moka-file-browser";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass("moka-file-browser--own-fit", FitWidths is not null)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("height", Height)
		.AddStyle(Style)
		.Build();

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

	private int ListedCount => _rows.Count - HiddenCount;

	private int HiddenCount => _itemFilterWasSet ? _rows.Count(row => row.FilteredOut) : 0;

	private string CountText => IsFiltering
		? Text.FilteredCount(_visible.Count, ListedCount)
		: Text.ItemCount(ListedCount);

	private string? KeysAttribute => Keys == MokaFileKeys.All ? null : FormattableString.Invariant($"{(int)Keys}");

	private string? KeyScopeAttribute => KeyScope == MokaFileKeyScope.Browser ? "browser" : null;

	private string? FitMedium => FitWidths is { } fit ? FormattableString.Invariant($"{fit.Medium}") : null;

	private string? FitWide => FitWidths is { } fit ? FormattableString.Invariant($"{fit.Wide}") : null;

	private bool ShowsActionRow =>
		ToolbarEnd is not null || Details is not null ||
		HasControl(MokaFileControls.Sort) || HasControl(MokaFileControls.ViewMode) ||
		(CreateFolder is not null && HasControl(MokaFileControls.NewFolder)) ||
		(Upload is not null && HasControl(MokaFileControls.Upload));

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

			return any ? BytesText(total) : null;
		}
	}

	private IReadOnlyList<TItem> SelectedItems => _selectedRows.ConvertAll(row => row.Item);

	/// <summary>Reads the folder on screen again, keeping the selection where it can.</summary>
	/// <returns>A task that ends when the folder has been read.</returns>
	public Task ReloadAsync() => InvokeAsync(() => LoadAsync(fresh: false));

	/// <summary>Goes to a folder, as opening it would.</summary>
	/// <param name="folder">The folder's key.</param>
	/// <param name="select">The key of an item to select and scroll to once the folder has arrived.</param>
	/// <returns>A task that ends when the folder has been read.</returns>
	public Task NavigateAsync(string folder, string? select = null)
	{
		ArgumentNullException.ThrowIfNull(folder);
		return InvokeAsync(async () =>
		{
			await GoToAsync(folder, select: select);
			StateHasChanged();
		});
	}

	/// <summary>
	///     Selects a row and scrolls to it, clearing a filter that hides it. While a folder is loading it waits for the
	///     folder to arrive. A key the folder doesn't hold does nothing.
	/// </summary>
	/// <param name="key">The item's key.</param>
	/// <returns>A task that ends once the row is selected, or at once while a folder is loading.</returns>
	public Task RevealAsync(string key)
	{
		ArgumentNullException.ThrowIfNull(key);
		return InvokeAsync(async () =>
		{
			await RevealOrWaitAsync(key);
			StateHasChanged();
		});
	}

	/// <summary>Pastes what is held. A cut moves and is then let go, and a copy stays held.</summary>
	/// <param name="folder">The key of the folder to paste into. Null is the folder on screen.</param>
	/// <returns>A task that ends when the paste is over.</returns>
	public Task PasteAsync(string? folder = null) => InvokeAsync(async () =>
	{
		await PasteIntoAsync(folder ?? CurrentFolder);
		StateHasChanged();
	});

	/// <summary>
	///     Holds items for the next paste, as cutting or copying them would. They need not be in the folder on screen. Does
	///     nothing for an empty list, for a cut without a <see cref="Move" /> or a copy without a <see cref="Copy" />.
	/// </summary>
	/// <param name="items">What to hold.</param>
	/// <param name="cut">Whether a paste moves them, rather than copying them.</param>
	/// <returns>A task that ends once the browser has redrawn.</returns>
	public Task HoldAsync(IReadOnlyList<TItem> items, bool cut)
	{
		ArgumentNullException.ThrowIfNull(items);
		return InvokeAsync(() =>
		{
			Hold([.. items.Select(item => new MokaFileRow<TItem>(item, Describe(item)))], cut);
			StateHasChanged();
		});
	}

	/// <summary>Lets go of what is held for the next paste.</summary>
	/// <returns>A task that ends once the browser has redrawn.</returns>
	public Task ClearHeldAsync() => InvokeAsync(() =>
	{
		_held = null;
		StateHasChanged();
	});

	/// <summary>
	///     Starts Back and Forward again from the folder on screen, or from the one on its way while a kept list is
	///     waiting for it. For an app that mounts the browser on a placeholder and then moves to the real start. Call it
	///     once the new <see cref="Folder" /> has reached the browser.
	/// </summary>
	/// <returns>A task that ends once the browser has redrawn.</returns>
	public Task ResetHistoryAsync() => InvokeAsync(() =>
	{
		_history.Reset(CurrentFolder);
		_restartHistoryOnArrival = _folderOnItsWay;
		StateHasChanged();
	});

	/// <summary>
	///     Says something failed, for an app that dealt with a failure itself rather than throwing. Called from inside an
	///     action, what was cut stays held. The folder still reloads, since part of the action may have worked.
	/// </summary>
	/// <param name="error">Shown in the browser's alert. Null leaves showing it to the app.</param>
	/// <returns>A task that ends once the browser has redrawn.</returns>
	public Task ReportFailureAsync(Exception? error = null) => InvokeAsync(() =>
	{
		_reportedFailure = _busy;
		if (error is not null)
		{
			ShowFailure(error, Text.ActionFailed, _runningRetry);
			StateHasChanged();
		}
	});

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
		ApplyFitWidthsParameter();
		ApplyColumnWidthsParameter();

		bool newFolder = !string.Equals(Folder, _lastFolder, StringComparison.Ordinal) &&
		                 !string.Equals(Folder, _folder, StringComparison.Ordinal);
		_lastFolder = Folder;
		bool keepingList = newFolder && CanKeepList;
		if (newFolder && !keepingList)
		{
			_folder = Folder;
			_history.Visit(Folder);
			ForgetFolder();
		}

		// After the folder is forgotten, so a selection or filter passed with a new folder belongs to that folder.
		if (!keepingList)
		{
			ApplySelectedParameter();
		}

		ApplyFilterParameter();
		if (ReadItemFilter())
		{
			RebuildVisible();
			await RaiseSelectionIfChangedAsync();
		}

		if (keepingList)
		{
			await LoadAsync(fresh: false, new PendingFolder(Folder, HistoryStep.Visit, Select: null, FromParameter: true));
		}
		else if (newFolder)
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

	private void ApplyFitWidthsParameter()
	{
		if (FitWidths != _lastFitWidths)
		{
			_lastFitWidths = FitWidths;
			_measureColumns = true;
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

		await SyncBoxSelectAsync();

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

		if (_revealLine is { } line && !_loading && _rendersFinished >= _revealAt)
		{
			_revealLine = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "revealLine", _root, line);
		}

		await RaiseStateChangesAsync();
	}

	// After the render that shows the change. Every change to these is followed by one, wherever it came from.
	private async Task RaiseStateChangesAsync()
	{
		if (IsBusy != _raisedBusy)
		{
			_raisedBusy = IsBusy;
			await OnBusyChanged.InvokeAsync(_raisedBusy);
		}

		if (!ReferenceEquals(_held, _raisedHeld))
		{
			_raisedHeld = _held;
			await OnHeldChanged.InvokeAsync(Held);
		}

		if (!ReferenceEquals(_failure, _raisedFailure))
		{
			_raisedFailure = _failure;
			await OnFailureChanged.InvokeAsync(_raisedFailure);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		await CancelRunningLoadAsync();

		if (_boxShape is not null)
		{
			_boxShape = null;
			await SafeModuleInvokeVoidAsync(BoxModulePath, "unbindBoxSelect", _root);
		}

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

	private bool HasControl(MokaFileControls control) => (Controls & control) != 0;

	private string BytesText(long bytes) => ByteFormatter is { } format ? format(bytes) : MokaByteFormat.Format(bytes, ByteUnits);

	private string ColumnTitle(MokaFileColumn<TItem> column) => column.Title ?? column.Id switch
	{
		MokaFileSort.Size => Text.SortSize,
		MokaFileSort.Modified => Text.ColumnModified,
		_ => column.Id
	};

	private static bool IsBuiltIn(MokaFileColumn<TItem> column) =>
		column.Id is MokaFileSort.Size or MokaFileSort.Modified && column.Text is null && column.Cell is null;

	private static bool IsSortable(MokaFileColumn<TItem> column) => column.Compare is not null || IsBuiltIn(column);

	private string? WidthStyle(MokaFileColumn<TItem> column, int index)
	{
		string width = column.Width ?? (column.Id == MokaFileSort.Modified ? "9rem" : column.Id == MokaFileSort.Size ? "6rem" : "8rem");
		return new StyleBuilder()
			.AddStyle("flex-basis", ResizableColumns ? $"var({ColumnVariable(index)}, {width})" : width)
			.Build();
	}

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
		.AddClass(AlignClass(column, "moka-file-browser__cell"))
		.Build();

	private static string HeadingClass(MokaFileColumn<TItem> column) => new CssBuilder("moka-file-browser__heading")
		.AddClass(FitClass(column))
		.AddClass("moka-file-browser__heading--number", IsBuiltIn(column) || column.Mono)
		.AddClass(AlignClass(column, "moka-file-browser__heading"))
		.Build();

	private static string HeadingWrapClass(MokaFileColumn<TItem> column) => new CssBuilder("moka-file-browser__heading-wrap")
		.AddClass(FitClass(column))
		.Build();

	private static string AlignClass(MokaFileColumn<TItem> column, string baseClass) => column.Align switch
	{
		null => "",
		MokaTextAlign.Center => $"{baseClass}--align-center",
		MokaTextAlign.Right => $"{baseClass}--align-right",
		_ => $"{baseClass}--align-left"
	};

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

	private string? SizeOf(MokaFileRow<TItem> row) =>
		row.Info.IsFolder || row.Info.Size is not { } size ? null : BytesText(size);

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
