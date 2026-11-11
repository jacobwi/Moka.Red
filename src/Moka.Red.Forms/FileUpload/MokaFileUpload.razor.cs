using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Files;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.FileUpload;

/// <summary>
///     A file upload component with drag-and-drop support.
///     Uses Blazor's built-in <see cref="InputFile" /> component and HTML5 drag events.
/// </summary>
public partial class MokaFileUpload
{
	private const string ModulePath = "./_content/Moka.Red.Forms/FileUpload/MokaFileUpload.razor.js";

	private static readonly MokaFileUploadText DefaultText = new();

	private readonly List<UploadError> _errors = [];
	private readonly List<IBrowserFile> _files = [];
	private readonly string _generatedId = $"moka-fileupload-{Guid.NewGuid():N}";

	// Files are compared by reference: two picks of the same file are two entries.
	private readonly Dictionary<IBrowserFile, int> _ids = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<IBrowserFile, string> _previewUrls = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<IBrowserFile, MokaUploadStatus?> _statuses = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<IBrowserFile, MokaUploadState?> _lastStates = new(ReferenceEqualityComparer.Instance);
	private readonly List<(IBrowserFile File, int Index)> _pendingPreviews = [];
	private readonly List<string> _staleUrls = [];

	// dragenter and dragleave also fire when the pointer crosses from the zone's border onto the
	// input that covers it, so the zone counts them rather than trusting the last one.
	private int _dragDepth;

	private IMokaFilePicker? _registeredPicker;
	private CancellationTokenSource? _lifetime;
	private bool _picking;
	private int _nextId;
	private int _refused;
	private string? _announcement;
	private InputFile? _input;
	private MokaFileUploadLook? _checkBoundTo;
	private bool _chipsBound;

	[Inject] private IServiceProvider Services { get; set; } = default!;

	// The consumer's Id names the file input, which also takes the unmatched attributes: it is the
	// control, and attributes such as capture, name or aria-describedby only work there. The labels
	// that open it follow the Id.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>Label text displayed above the upload area.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the upload area.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the upload area when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>
	///     Accepted file types (e.g., "image/*", ".pdf,.docx"). Dropped files are checked against it too,
	///     and a file of another type is listed as an error. The <see cref="MokaFileUploadLook.Picture" /> and
	///     <see cref="MokaFileUploadLook.Gallery" /> looks take "image/*" when it isn't set.
	/// </summary>
	[Parameter]
	public string? Accept { get; set; }

	/// <summary>
	///     Whether to allow multiple file selection. Default is false. The <see cref="MokaFileUploadLook.Picture" /> and
	///     <see cref="MokaFileUploadLook.Slot" /> looks always take one file.
	/// </summary>
	[Parameter]
	public bool Multiple { get; set; }

	/// <summary>Maximum file size in bytes. Default is 10 MB.</summary>
	[Parameter]
	public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

	/// <summary>
	///     Maximum number of files in the list when <see cref="Multiple" /> is on, across every pick and
	///     drop. Default is 10.
	/// </summary>
	[Parameter]
	public int MaxFiles { get; set; } = 10;

	/// <summary>Callback invoked when files are selected or changed.</summary>
	[Parameter]
	public EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelected { get; set; }

	/// <summary>
	///     Callback invoked when a file is removed from the list. A file the app reports uploading is removed by the same
	///     button, named Cancel, so this is where the app stops its upload.
	/// </summary>
	[Parameter]
	public EventCallback<IBrowserFile> OnFileRemoved { get; set; }

	/// <summary>
	///     Whether to show the selected file list. Default is true. Applies to the lists under the
	///     <see cref="MokaFileUploadLook.Standard" />, <see cref="MokaFileUploadLook.CardList" /> and
	///     <see cref="MokaFileUploadLook.Quiet" /> looks; the others show their files as part of the control.
	/// </summary>
	[Parameter]
	public bool ShowFileList { get; set; } = true;

	/// <summary>Whether to use compact single-line mode. Default is false. Only the standard look has one.</summary>
	[Parameter]
	public bool Compact { get; set; }

	/// <summary>Whether files can be dropped on the zone. Default is true.</summary>
	[Parameter]
	public bool DragDrop { get; set; } = true;

	/// <summary>
	///     Picks files in place of the browser's dialog, over the <see cref="IMokaFilePicker" /> the app registered.
	///     Picked files go through the same checks as the browser's. Dropped files still arrive through the file input.
	/// </summary>
	[Parameter]
	public IMokaFilePicker? FilePicker { get; set; }

	/// <summary>How the upload draws itself and its files. Default <see cref="MokaFileUploadLook.Standard" />.</summary>
	[Parameter]
	public MokaFileUploadLook Look { get; set; }

	/// <summary>
	///     How the zone looks while files are dragged over it, in the <see cref="MokaFileUploadLook.Standard" /> and
	///     <see cref="MokaFileUploadLook.CardList" /> looks. Default <see cref="MokaFileUploadDragLook.Standard" />.
	/// </summary>
	[Parameter]
	public MokaFileUploadDragLook DragLook { get; set; }

	/// <summary>
	///     How the chosen files are listed under the <see cref="MokaFileUploadLook.Standard" /> and
	///     <see cref="MokaFileUploadLook.Quiet" /> looks. Default <see cref="MokaFileUploadListLook.Standard" />.
	/// </summary>
	[Parameter]
	public MokaFileUploadListLook ListLook { get; set; }

	/// <summary>
	///     Reads the dragged files' types and count while they are over the upload, and says before the drop whether they
	///     fit <see cref="Accept" />, <see cref="Multiple" /> and <see cref="MaxFiles" />. Sizes are only known after the
	///     drop, so <see cref="MaxFileSize" /> is checked then, as always. It only advises: a drop goes through every check
	///     either way. Default false.
	/// </summary>
	[Parameter]
	public bool CheckWhileDragging { get; set; }

	/// <summary>The frame of the <see cref="MokaFileUploadLook.Picture" /> look. Default <see cref="MokaPictureShape.Circle" />.</summary>
	[Parameter]
	public MokaPictureShape PictureShape { get; set; }

	/// <summary>
	///     The picture the app holds now, for the <see cref="MokaFileUploadLook.Picture" /> look: an address an <c>img</c>
	///     can show. A picked picture shows in its place while its preview is known (see <see cref="Previews" />).
	/// </summary>
	[Parameter]
	public string? PictureSource { get; set; }

	/// <summary>
	///     Raised by Remove in the <see cref="MokaFileUploadLook.Picture" /> look when the picture is the app's own
	///     <see cref="PictureSource" />, not a picked file. Remove shows for it only when this has a listener.
	/// </summary>
	[Parameter]
	public EventCallback OnPictureRemoved { get; set; }

	/// <summary>
	///     Shows picked pictures in the looks that have room for one. The browser draws each from the file it already holds,
	///     through an object URL, so nothing is read or sent to .NET; the URL is let go when the file leaves the list. Works
	///     for files picked or dropped through the browser. Files an <see cref="IMokaFilePicker" /> hands over get a picture
	///     only through <see cref="PreviewSource" />. Default false.
	/// </summary>
	[Parameter]
	public bool Previews { get; set; }

	/// <summary>
	///     The app's own address for a file's picture, such as where it was uploaded to, or null for none. Wins over
	///     <see cref="Previews" />. Asked every time the upload draws.
	/// </summary>
	[Parameter]
	public Func<IBrowserFile, string?>? PreviewSource { get; set; }

	/// <summary>
	///     Each file's upload state as the app sees it, or null for a file it says nothing about. Asked for every file every
	///     time the upload draws, so when an upload moves on, re-render the component that holds the upload (call
	///     <c>StateHasChanged</c> there) and it asks again. Uploading is the app's job: the upload only shows it.
	/// </summary>
	[Parameter]
	public Func<IBrowserFile, MokaUploadStatus?>? Status { get; set; }

	/// <summary>Shows Retry on files whose <see cref="Status" /> is failed, and raises this with the file.</summary>
	[Parameter]
	public EventCallback<IBrowserFile> OnRetry { get; set; }

	/// <summary>Every word the upload writes. Null keeps the English ones.</summary>
	[Parameter]
	public MokaFileUploadText? Text { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-fileupload";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(LookClass)
		.AddClass(Class)
		.Build();

	private string? LookClass => Look switch
	{
		MokaFileUploadLook.CardList => "moka-fileupload--cards",
		MokaFileUploadLook.Field => "moka-fileupload--field",
		MokaFileUploadLook.Picture => "moka-fileupload--picture",
		MokaFileUploadLook.Gallery => "moka-fileupload--gallery",
		MokaFileUploadLook.Quiet => "moka-fileupload--quiet",
		MokaFileUploadLook.Terminal => "moka-fileupload--terminal",
		MokaFileUploadLook.Slot => "moka-fileupload--slot",
		_ => null
	};

	private MokaFileUploadText Words => Text ?? DefaultText;

	private bool TakesOneFile => !Multiple || Look is MokaFileUploadLook.Picture or MokaFileUploadLook.Slot;

	private string? EffectiveAccept => Accept ??
	                                   (Look is MokaFileUploadLook.Picture or MokaFileUploadLook.Gallery ? "image/*" : null);

	// A disabled upload's input takes no drop, so the file would fall through to the page, which opens it.
	private bool AcceptsDrops => DragDrop && !Disabled;

	private IMokaFilePicker? ActivePicker => FilePicker ?? _registeredPicker;

	private bool HasPicker => ActivePicker is not null;

	// No listener at all without a picker, so the zone renders as it always has.
	private EventCallback<MouseEventArgs> PickClick =>
		HasPicker ? EventCallback.Factory.Create<MouseEventArgs>(this, PickAsync) : default;

	private bool HasDragLook => DragLook != MokaFileUploadDragLook.Standard &&
	                            Look is MokaFileUploadLook.Standard or MokaFileUploadLook.CardList;

	private string? DragLookName => HasDragLook ? MokaFileTypes.Lower(DragLook.ToString()) : null;

	private bool ShowsCheckFaces => CheckWhileDragging && Look is MokaFileUploadLook.Standard or MokaFileUploadLook.CardList;

	private string DropzoneCssClass => new CssBuilder("moka-fileupload-dropzone")
		.AddClass("moka-fileupload-dropzone--dragging", _dragDepth > 0)
		.AddClass("moka-fileupload-dropzone--compact", Compact && Look == MokaFileUploadLook.Standard)
		.AddClass("moka-fileupload-dropzone--row", Look == MokaFileUploadLook.CardList)
		.AddClass("moka-fileupload-dropzone--disabled", Disabled)
		.Build();

	// The field wrapper is the outermost element and the only one that holds everything, so it
	// takes the margin, Class and Style. The drop zone draws the box, so it takes the padding and
	// radius.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string? DropzoneStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		_registeredPicker = Services.GetService<IMokaFilePicker>();
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		TrackStatuses();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (_staleUrls.Count > 0)
		{
			string[] stale = [.. _staleUrls];
			_staleUrls.Clear();
			await SafeModuleInvokeVoidAsync(ModulePath, "revokePreviews", (object)stale);
		}

		if (_pendingPreviews.Count > 0 && _input?.Element is { } input)
		{
			await CreatePreviewsAsync(input);
		}

		await SyncDragCheckAsync();
		await SyncChipsAsync();
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_lifetime is not null)
		{
			await _lifetime.CancelAsync();
			_lifetime.Dispose();
		}

		string[] urls = [.. _previewUrls.Values, .. _staleUrls];
		_previewUrls.Clear();
		_staleUrls.Clear();
		if (urls.Length > 0)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "revokePreviews", (object)urls);
		}

		await base.DisposeAsyncCore();
	}

	// The browser holds the picked files only in the input, and only until the next pick, so the pictures are made
	// right after the render that follows the pick, from the files' places in that pick.
	private async Task CreatePreviewsAsync(ElementReference input)
	{
		(IBrowserFile File, int Index)[] pending = [.. _pendingPreviews];
		_pendingPreviews.Clear();
		int[] indexes = [.. pending.Select(p => p.Index)];
		string?[]? urls = await SafeModuleInvokeAsync<string?[]>(ModulePath, "createPreviews", input, indexes);
		if (urls is null)
		{
			return;
		}

		bool shown = false;
		for (int i = 0; i < pending.Length && i < urls.Length; i++)
		{
			if (urls[i] is not { } url)
			{
				continue;
			}

			if (_files.Contains(pending[i].File))
			{
				_previewUrls[pending[i].File] = url;
				shown = true;
			}
			else
			{
				_staleUrls.Add(url);
			}
		}

		if (shown || _staleUrls.Count > 0)
		{
			StateHasChanged();
		}
	}

	private bool WantsCheck => CheckWhileDragging && AcceptsDrops;

	private async Task SyncDragCheckAsync()
	{
		if (WantsCheck && _checkBoundTo != Look && _input?.Element is { } input)
		{
			_checkBoundTo = Look;
			await SafeModuleInvokeVoidAsync(ModulePath, "bindDragCheck", input);
		}
		else if (!WantsCheck && _checkBoundTo is not null)
		{
			_checkBoundTo = null;
			if (_input?.Element is { } current)
			{
				await SafeModuleInvokeVoidAsync(ModulePath, "unbindDragCheck", current);
			}
		}
	}

	// The field keeps its chips on one line. The script hides the ones that wrap out of sight, takes them out of Tab and
	// counts them, so it is loaded only once the field holds a file. The row is drawn only while there are chips, so a
	// field emptied and filled again has a new row to bind.
	private async Task SyncChipsAsync()
	{
		if (Look != MokaFileUploadLook.Field || _files.Count == 0)
		{
			_chipsBound = false;
			return;
		}

		if (!_chipsBound && _files.Count > 0 && _input?.Element is { } input)
		{
			_chipsBound = true;
			await SafeModuleInvokeVoidAsync(ModulePath, "bindChips", input);
		}
	}

	// GetMultipleFiles throws past its limit, so the limit is the file count.
	private Task HandleFileSelected(InputFileChangeEventArgs e) => AddFilesAsync(e.GetMultipleFiles(e.FileCount), true);

	private async Task PickAsync()
	{
		if (Disabled || _picking || ActivePicker is not { } picker)
		{
			return;
		}

		_lifetime ??= new CancellationTokenSource();
		CancellationToken lifetime = _lifetime.Token;
		IReadOnlyList<IBrowserFile> files;
		_picking = true;
		try
		{
			files = await picker.PickAsync(new MokaFilePickRequest(!TakesOneFile, EffectiveAccept), lifetime);
		}
		catch (OperationCanceledException)
		{
			return;
		}
		finally
		{
			_picking = false;
		}

		if (files is { Count: > 0 } && !lifetime.IsCancellationRequested)
		{
			await AddFilesAsync(files, false);
		}
	}

	private async Task AddFilesAsync(IReadOnlyList<IBrowserFile> files, bool fromInput)
	{
		// The input is disabled, but a change already on its way when Disabled was set gets here, and so do
		// the files of a picker that was open when it was set.
		if (Disabled)
		{
			return;
		}

		_errors.Clear();
		_refused = 0;

		// A drop skips the dialog's type filter and can bring any number of files, even to a single-file
		// input, and an app's picker may not filter at all, so the rules are checked here, where every way
		// in meets.
		if (TakesOneFile && files.Count > 1)
		{
			files = [files[0]];
		}

		int overLimit = 0;
		for (int index = 0; index < files.Count; index++)
		{
			IBrowserFile file = files[index];
			if (!IsAccepted(file))
			{
				_errors.Add(new UploadError(Words.NotAccepted(file.Name), file.Name, file.Size, Words.TerminalWrongType));
				_refused++;
				continue;
			}

			if (file.Size > MaxFileSize)
			{
				_errors.Add(new UploadError(Words.TooLarge(file.Name, FormatSize(MaxFileSize)), file.Name, file.Size,
					Words.TerminalTooBig));
				_refused++;
				continue;
			}

			if (TakesOneFile)
			{
				ForgetAll();
			}
			else if (_files.Count >= MaxFiles)
			{
				overLimit++;
				continue;
			}

			_files.Add(file);
			_ids[file] = ++_nextId;
			if (fromInput && Previews && PreviewSource is null &&
			    MokaFileTypes.KindOf(file.Name, file.ContentType) == MokaFileKind.Image)
			{
				_pendingPreviews.Add((file, index));
			}
		}

		// One line, not one per file: a drop can hold thousands.
		if (overLimit > 0)
		{
			_errors.Add(new UploadError(Words.OverLimit(overLimit, MaxFiles), null, 0, null));
			_refused += overLimit;
		}

		TrackStatuses();

		if (OnFilesSelected.HasDelegate)
		{
			await OnFilesSelected.InvokeAsync(_files.AsReadOnly());
		}
	}

	private void ForgetAll()
	{
		foreach (IBrowserFile file in _files)
		{
			Forget(file);
		}

		_files.Clear();
	}

	private void Forget(IBrowserFile file)
	{
		_ids.Remove(file);
		_pendingPreviews.RemoveAll(p => ReferenceEquals(p.File, file));
		if (_previewUrls.Remove(file, out string? url))
		{
			_staleUrls.Add(url);
		}
	}

	private async Task RemoveFile(IBrowserFile file)
	{
		// The buttons are disabled, but a click already on its way when Disabled was set gets here.
		if (Disabled)
		{
			return;
		}

		_files.Remove(file);
		Forget(file);
		TrackStatuses();

		if (OnFileRemoved.HasDelegate)
		{
			await OnFileRemoved.InvokeAsync(file);
		}

		if (OnFilesSelected.HasDelegate)
		{
			await OnFilesSelected.InvokeAsync(_files.AsReadOnly());
		}
	}

	private async Task ClearAllAsync()
	{
		if (Disabled || _files.Count == 0)
		{
			return;
		}

		IBrowserFile[] removed = [.. _files];
		ForgetAll();
		_errors.Clear();
		TrackStatuses();

		if (OnFileRemoved.HasDelegate)
		{
			foreach (IBrowserFile file in removed)
			{
				await OnFileRemoved.InvokeAsync(file);
			}
		}

		if (OnFilesSelected.HasDelegate)
		{
			await OnFilesSelected.InvokeAsync(_files.AsReadOnly());
		}
	}

	private async Task RetryAsync(IBrowserFile file)
	{
		if (!Disabled)
		{
			await OnRetry.InvokeAsync(file);
		}
	}

	private async Task RemovePictureAsync()
	{
		if (Disabled)
		{
			return;
		}

		if (_files.Count > 0)
		{
			await RemoveFile(_files[0]);
		}
		else if (OnPictureRemoved.HasDelegate)
		{
			await OnPictureRemoved.InvokeAsync();
		}
	}

	// Asked once per render pass, so a file's state is read the same everywhere it shows, and a move to done or failed
	// is said once.
	private void TrackStatuses()
	{
		_statuses.Clear();
		if (Status is null)
		{
			_lastStates.Clear();
			return;
		}

		List<string>? said = null;
		foreach (IBrowserFile file in _files)
		{
			MokaUploadStatus? status = Status(file);
			_statuses[file] = status;
			MokaUploadState? state = status?.State;
			bool seen = _lastStates.TryGetValue(file, out MokaUploadState? before);
			_lastStates[file] = state;
			if (!seen || before == state)
			{
				continue;
			}

			if (state == MokaUploadState.Done)
			{
				(said ??= []).Add(Words.Uploaded(file.Name));
			}
			else if (state == MokaUploadState.Failed)
			{
				(said ??= []).Add(Words.UploadFailed(file.Name, status?.Message));
			}
		}

		foreach (IBrowserFile gone in _lastStates.Keys.Where(file => !_statuses.ContainsKey(file)).ToList())
		{
			_lastStates.Remove(gone);
		}

		if (said is not null)
		{
			_announcement = string.Join(". ", said);
		}
	}

	private MokaUploadStatus? StatusOf(IBrowserFile file) => _statuses.GetValueOrDefault(file);

	private int IdOf(IBrowserFile file) => _ids.GetValueOrDefault(file);

	private string? PreviewOf(IBrowserFile file)
	{
		if (PreviewSource is not null)
		{
			return UrlValues.SafeImageSource(PreviewSource(file));
		}

		return _previewUrls.TryGetValue(file, out string? url) ? UrlValues.SafeImageSource(url) : null;
	}

	private static MokaFileKind KindOf(IBrowserFile file) => MokaFileTypes.KindOf(file.Name, file.ContentType);

	private static string TypeLabelOf(IBrowserFile file) => MokaFileTypes.TypeLabel(file.Name, file.ContentType);

	private string RemoveLabel(IBrowserFile file) => StatusOf(file)?.State == MokaUploadState.Uploading
		? Words.Cancel(file.Name)
		: Words.Remove(file.Name);

	private bool CanRetry(IBrowserFile file) => OnRetry.HasDelegate && StatusOf(file)?.State == MokaUploadState.Failed;

	private string MetaOf(IBrowserFile file, bool withType = false)
	{
		MokaUploadStatus? status = StatusOf(file);
		string size = FormatSize(file.Size);
		if (!string.IsNullOrWhiteSpace(status?.Message))
		{
			return status.Message;
		}

		string type = TypeLabelOf(file);
		string sized = withType && type.Length > 0 ? $"{size}{Words.Separator}{type}" : size;
		return status?.State switch
		{
			MokaUploadState.Waiting => $"{sized}{Words.Separator}{Words.Waiting}",
			MokaUploadState.Uploading => Words.Amount(FormatSize((long)(file.Size * PercentOf(status) / 100)), size),
			MokaUploadState.Failed => Words.Failed,
			_ => sized
		};
	}

	private static double PercentOf(MokaUploadStatus? status) => status switch
	{
		null => 0,
		{ State: MokaUploadState.Done or MokaUploadState.Failed } => 100,
		{ State: MokaUploadState.Uploading } => double.IsFinite(status.Percent) ? Math.Clamp(status.Percent, 0, 100) : 0,
		_ => 0
	};

	private static string PercentText(MokaUploadStatus? status) =>
		string.Create(CultureInfo.CurrentCulture, $"{PercentOf(status):0}%");

	private static string? BarStyle(MokaUploadStatus? status) => new StyleBuilder()
		.AddStyle("width", string.Create(CultureInfo.InvariantCulture, $"{PercentOf(status):0.##}%"))
		.Build();

	private static string BarClass(MokaUploadStatus? status) => new CssBuilder("moka-fileupload-bar")
		.AddClass("moka-fileupload-bar--done", status?.State == MokaUploadState.Done)
		.AddClass("moka-fileupload-bar--failed", status?.State == MokaUploadState.Failed)
		.Build();

	private static string TextBar(MokaUploadStatus? status)
	{
		int filled = (int)Math.Round(PercentOf(status) / 10);
		return string.Create(CultureInfo.CurrentCulture, $"[{new string('#', filled)}{new string('-', 10 - filled)}] {PercentOf(status):0}%");
	}

	private string TargetClass(string root, params (string Modifier, bool When)[] extra)
	{
		CssBuilder builder = new CssBuilder(root)
			.AddClass($"{root}--dragging", _dragDepth > 0)
			.AddClass($"{root}--disabled", Disabled);
		foreach ((string modifier, bool when) in extra)
		{
			builder.AddClass($"{root}--{modifier}", when);
		}

		return builder.Build();
	}

	private static string TypeTileClass(IBrowserFile file, string? size = null) => new CssBuilder("moka-fileupload-type")
		.AddClass($"moka-fileupload-type--{size}", size is not null)
		.AddClass($"moka-fileupload-tone--{MokaFileTypes.ToneOf(KindOf(file))}")
		.Build();

	private string TotalText => Words.Total(_files.Count, FormatSize(_files.Sum(file => file.Size)));

	private IReadOnlyList<string> AcceptLabels => MokaFileTypes.AcceptLabels(EffectiveAccept, Words.Images);

	private string? AcceptPhrase => AcceptLabels.Count == 0 ? null : MokaFileTypes.JoinLabels(AcceptLabels, Words.Or);

	private int Room => TakesOneFile ? 1 : Math.Max(0, MaxFiles - _files.Count);

	private string RoomText => TakesOneFile ? Words.CheckOneAtATime : Words.CheckRoom(Room);

	private string? RoomAttribute => CheckWhileDragging ? Room.ToString(CultureInfo.InvariantCulture) : null;

	private string TerminalHintText
	{
		get
		{
			string? types = AcceptLabels.Count == 0
				? null
				: string.Join(' ', AcceptLabels.Select(MokaFileTypes.Lower));
			return Words.TerminalHint(types, MokaFileTypes.FormatShortSize(MaxFileSize));
		}
	}

	private string LimitText => MokaFileTypes.FormatLimit(MaxFileSize);

	private (string Before, string After, bool Slot) ReadyWords => Around(Words.CheckReady);

	private (string Before, string After, bool Slot) WrongTypeWords => Around(Words.CheckWrongType);

	// The browser fills in the count or the type while a file is dragged over, so the words around the slot are drawn
	// here and the slot is left empty for it.
	private static (string Before, string After, bool Slot) Around(string template)
	{
		int at = template.IndexOf("{0}", StringComparison.Ordinal);
		return at < 0 ? (template, string.Empty, false) : (template[..at], template[(at + 3)..], true);
	}

	private static string ThumbClass(MokaUploadStatus? status) => new CssBuilder("moka-fileupload-thumb")
		.AddClass("moka-fileupload-thumb--busy", status?.State == MokaUploadState.Uploading)
		.AddClass("moka-fileupload-thumb--failed", status?.State == MokaUploadState.Failed)
		.Build();

	private static string StripImageClass(IBrowserFile file, MokaUploadStatus? status) => new CssBuilder("moka-fileupload-st-img")
		.AddClass($"moka-fileupload-tone--{MokaFileTypes.ToneOf(KindOf(file))}")
		.AddClass("moka-fileupload-thumb--busy", status?.State == MokaUploadState.Uploading)
		.AddClass("moka-fileupload-thumb--failed", status?.State == MokaUploadState.Failed)
		.Build();

	private static string RowClass(MokaUploadStatus? status) => new CssBuilder("moka-fileupload-row")
		.AddClass("moka-fileupload-row--failed", status?.State == MokaUploadState.Failed)
		.Build();

	private static string ChipClass(IBrowserFile file, MokaUploadStatus? status) => new CssBuilder("moka-fileupload-fchip")
		.AddClass($"moka-fileupload-tone--{MokaFileTypes.ToneOf(KindOf(file))}")
		.AddClass("moka-fileupload-fchip--failed", status?.State == MokaUploadState.Failed)
		.Build();

	private string TerminalState(MokaUploadStatus? status) => status?.State switch
	{
		MokaUploadState.Done => Words.TerminalDone,
		MokaUploadState.Waiting => Words.TerminalWaiting,
		MokaUploadState.Uploading => TextBar(status),
		MokaUploadState.Failed => string.IsNullOrWhiteSpace(status.Message) ? Words.TerminalFailed : status.Message,
		_ => string.Empty
	};

	private static string TerminalStateClass(MokaUploadStatus? status) => status?.State switch
	{
		MokaUploadState.Done => "moka-fileupload-term-ok",
		MokaUploadState.Failed => "moka-fileupload-term-err",
		MokaUploadState.Waiting => "moka-fileupload-term-dim",
		_ => "moka-fileupload-term-state"
	};

	// Without DragDrop the zone takes no drops, so it does not light up for one.
	private void HandleDragEnter()
	{
		if (DragDrop && !Disabled)
		{
			_dragDepth++;
		}
	}

	private void HandleDragLeave() => _dragDepth = Math.Max(0, _dragDepth - 1);

	// A drop ends the drag without a dragleave.
	private void HandleDrop() => _dragDepth = 0;

	private bool IsAccepted(IBrowserFile file) => MokaFileTypes.IsAccepted(file, EffectiveAccept);

	private static string FormatSize(long bytes) => MokaFileTypes.FormatSize(bytes);

	private sealed record UploadError(string Message, string? FileName, long Size, string? Short);
}
