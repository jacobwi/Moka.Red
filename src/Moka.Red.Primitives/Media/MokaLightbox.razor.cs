using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Keys;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Media;

/// <summary>
///     A full-screen viewer that steps through a list of <see cref="MokaMediaItem" />s. It is a modal dialog: it takes
///     focus when it opens, keeps Tab inside, locks the page's scrolling, and hands focus back to where it was when it
///     closes. The arrow keys, Home and End step through the list and Escape closes it. With <see cref="LoadSource" />
///     it reads each full image only when it comes near the one on show, showing the thumbnail until it arrives, and
///     gives back what drops away through <see cref="ReleaseSource" />. <see cref="MokaMediaGallery" /> opens one of these
///     for its own lightbox.
/// </summary>
public partial class MokaLightbox : MokaVisualComponentBase
{
	private const string LightboxModule = "./_content/Moka.Red.Primitives/Media/MokaLightbox.razor.js";

	// What the stand-in and the failed state take while there is no image to size them.
	private const string StandInSize = "min(72vw, 60vh)";

	private static readonly MokaMediaText DefaultText = new();

	// Keyed by the item's Key when it has one, so a thumbnail arriving, which makes a new item, doesn't start the full
	// image again. Items without a Key are keyed by value.
	private readonly Dictionary<object, LoadedSource> _sources = [];
	private readonly HashSet<object> _failed = [];
	private readonly HashSet<object> _previewFailed = [];

	// The script's name for this lightbox. The dialog element goes away when it closes, so it can't be the handle.
	private readonly string _scriptKey = $"moka-lightbox-{Guid.NewGuid():N}";

	private ElementReference _dialog;
	private int _index;
	private int _indexParameter;
	private bool _open;
	private bool _openParameter;
	private bool _shownToScript;

	/// <summary>The items it steps through, in order. Required.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaMediaItem> Items { get; set; } = [];

	/// <summary>
	///     Whether the lightbox is showing. Two-way bindable. It closes itself (Escape, the close button, a click beside
	///     the image) and reports that through <see cref="OpenChanged" />; a new value from the parent replaces that
	///     state. Opening shows the image at <see cref="Index" />.
	/// </summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Raised when the lightbox closes itself, with false.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>
	///     The position in <see cref="Items" /> of the image on show, from 0. Two-way bindable: stepping raises
	///     <see cref="IndexChanged" />, and a new value from the parent moves there. Kept inside the list.
	/// </summary>
	[Parameter]
	public int Index { get; set; }

	/// <summary>Raised when the person steps to another image.</summary>
	[Parameter]
	public EventCallback<int> IndexChanged { get; set; }

	/// <summary>
	///     Reads an item's full image when it comes near the one on show and returns a URL for it, such as an object URL
	///     made from the bytes, so an app needn't hand over every full <see cref="MokaMediaItem.Src" /> up front. The token
	///     is cancelled once the image is no longer wanted. Returning null, or throwing, shows the image as one that can't
	///     be shown. Unset, the lightbox shows <see cref="MokaMediaItem.Src" />, else the thumbnail, and a skeleton while an
	///     item has neither.
	/// </summary>
	[Parameter]
	public Func<MokaMediaItem, CancellationToken, Task<string?>>? LoadSource { get; set; }

	/// <summary>
	///     Gives back a URL <see cref="LoadSource" /> returned, with the item it was read for, once the image is no longer
	///     near the one on show, when the lightbox closes and when it goes away. A URL that arrives after it stopped being
	///     wanted is given back at once.
	/// </summary>
	[Parameter]
	public Func<MokaMediaItem, string, Task>? ReleaseSource { get; set; }

	/// <summary>
	///     How many images on each side of the one on show <see cref="LoadSource" /> reads ahead. Default 1: the next and
	///     the previous. Anything further is given back.
	/// </summary>
	[Parameter]
	public int Preload { get; set; } = 1;

	/// <summary>Facts about the item on show, such as its name, date and size, at the top of the lightbox.</summary>
	[Parameter]
	public RenderFragment<MokaMediaItem>? Facts { get; set; }

	/// <summary>Every word the lightbox writes or a screen reader hears. English by default.</summary>
	[Parameter]
	public MokaMediaText Text { get; set; } = DefaultText;

	/// <inheritdoc />
	protected override string RootClass => "moka-lightbox";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	// The root covers the window and draws nothing, so the radius goes to the picture.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string? ImageStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private bool Shown => _open && Items.Count > 0;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		await base.OnParametersSetAsync();

		bool opening = false;
		if (Open != _openParameter)
		{
			_openParameter = Open;
			opening = Open && !_open;
			_open = Open;
		}

		if (opening || Index != _indexParameter)
		{
			_indexParameter = Index;
			_index = Index;
		}

		_index = Items.Count == 0 ? 0 : Math.Clamp(_index, 0, Items.Count - 1);
		await SyncSourcesAsync();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Focus moves into the lightbox when it shows and back when it goes; the script also takes the page's scroll
		// lock and gives it back. Every way of closing ends in a render without it.
		if (Shown == _shownToScript)
		{
			return;
		}

		_shownToScript = Shown;
		if (_shownToScript)
		{
			await SafeModuleInvokeVoidAsync(LightboxModule, "openLightbox", _scriptKey, _dialog);
		}
		else
		{
			await SafeModuleInvokeVoidAsync(LightboxModule, "closeLightbox", _scriptKey);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_open = false;
		await SyncSourcesAsync();

		// Removed while showing, it never renders closed, and the page would stay locked.
		if (_shownToScript)
		{
			_shownToScript = false;
			await SafeModuleInvokeVoidAsync(LightboxModule, "closeLightbox", _scriptKey);
		}

		await base.DisposeAsyncCore();
	}

	private static object Identity(MokaMediaItem item) => item.Key ?? item;

	private static string? Thumbnail(MokaMediaItem item) =>
		!string.IsNullOrWhiteSpace(item.ThumbnailSrc) ? item.ThumbnailSrc :
		!string.IsNullOrWhiteSpace(item.Src) ? item.Src : null;

	private static string? ItemName(MokaMediaItem item) =>
		!string.IsNullOrWhiteSpace(item.Alt) ? item.Alt :
		!string.IsNullOrWhiteSpace(item.Caption) ? item.Caption : null;

	private static string ImageClass(LightboxStage stage) => new CssBuilder("moka-lightbox-image")
		.AddClass("moka-lightbox-image--preview", stage.Kind == LightboxStageKind.Preview)
		.Build();

	private LightboxStage StageOf(MokaMediaItem item)
	{
		object id = Identity(item);
		if (item.Failed || _failed.Contains(id))
		{
			return new LightboxStage(LightboxStageKind.Failed, null);
		}

		// With no source yet the item is waiting, as its gallery tile is.
		if (LoadSource is null)
		{
			string? source = !string.IsNullOrWhiteSpace(item.Src) ? item.Src : Thumbnail(item);
			return source is null
				? new LightboxStage(LightboxStageKind.Waiting, null)
				: new LightboxStage(LightboxStageKind.Image, source);
		}

		if (_sources.TryGetValue(id, out LoadedSource? loaded) && loaded.Url is { } url)
		{
			return new LightboxStage(LightboxStageKind.Image, url);
		}

		return Thumbnail(item) is { } preview && !_previewFailed.Contains(id)
			? new LightboxStage(LightboxStageKind.Preview, preview)
			: new LightboxStage(LightboxStageKind.Waiting, null);
	}

	// The image on show first, then its neighbours outwards, so the reads start in the order they're needed.
	private List<MokaMediaItem> Wanted()
	{
		List<MokaMediaItem> wanted = [];
		if (!Shown || LoadSource is null)
		{
			return wanted;
		}

		wanted.Add(Items[_index]);
		for (int step = 1; step <= Preload && step < Items.Count; step++)
		{
			if (_index + step < Items.Count)
			{
				wanted.Add(Items[_index + step]);
			}

			if (_index - step >= 0)
			{
				wanted.Add(Items[_index - step]);
			}
		}

		return wanted;
	}

	// Starts reading what is near the image on show and gives back the rest. Closed, nothing is near, so everything
	// goes, and the images that failed get another chance next time.
	private async Task SyncSourcesAsync()
	{
		List<MokaMediaItem> wanted = Wanted();
		HashSet<object> wantedIds = [.. wanted.Select(Identity)];

		List<LoadedSource> dropped = [];
		foreach ((object id, LoadedSource source) in _sources)
		{
			if (!wantedIds.Contains(id))
			{
				dropped.Add(source);
			}
		}

		foreach (LoadedSource source in dropped)
		{
			_sources.Remove(source.Id);
			source.Cancel();
		}

		foreach (MokaMediaItem item in wanted)
		{
			object id = Identity(item);
			if (item.Failed || _failed.Contains(id) || _sources.ContainsKey(id))
			{
				continue;
			}

			LoadedSource source = new(id, item);
			_sources[id] = source;
			RunDetached(() => LoadAsync(source));
		}

		if (!Shown)
		{
			_failed.Clear();
			_previewFailed.Clear();
		}

		foreach (LoadedSource source in dropped)
		{
			await ReleaseAsync(source.Item, source.Url);
		}
	}

	private async Task LoadAsync(LoadedSource source)
	{
		// Dropped before it started, while it waited its turn.
		if (LoadSource is not { } load || source.Token.IsCancellationRequested)
		{
			source.Dispose();
			return;
		}

		string? url = null;
		try
		{
			url = await load(source.Item, source.Token);
		}
		catch (OperationCanceledException) when (source.Token.IsCancellationRequested)
		{
		}
#pragma warning disable CA1031 // Whatever the app's loader throws means the image can't be shown, and the lightbox says so.
		catch (Exception)
#pragma warning restore CA1031
		{
			url = null;
		}
		finally
		{
			source.Dispose();
		}

		bool current = _sources.TryGetValue(source.Id, out LoadedSource? now) && ReferenceEquals(now, source);
		if (!current)
		{
			await ReleaseAsync(source.Item, url);
			return;
		}

		if (string.IsNullOrEmpty(url))
		{
			_sources.Remove(source.Id);
			_failed.Add(source.Id);
		}
		else
		{
			source.Url = url;
		}

		StateHasChanged();
	}

	private async Task ReleaseAsync(MokaMediaItem item, string? url)
	{
		if (!string.IsNullOrEmpty(url) && ReleaseSource is { } release)
		{
			await release(item, url);
		}
	}

	private void ImageFailed(MokaMediaItem item, LightboxStageKind kind)
	{
		// A stand-in that fails gives way to the skeleton; the image itself failing is the failed state.
		object id = Identity(item);
		if (kind == LightboxStageKind.Preview)
		{
			_previewFailed.Add(id);
		}
		else
		{
			_failed.Add(id);
		}
	}

	private Task ShowPreviousAsync() => ShowAsync(_index - 1);

	private Task ShowNextAsync() => ShowAsync(_index + 1);

	private async Task ShowAsync(int index)
	{
		if (!Shown)
		{
			return;
		}

		int target = Math.Clamp(index, 0, Items.Count - 1);
		if (target == _index)
		{
			return;
		}

		_index = target;
		await SyncSourcesAsync();
		await IndexChanged.InvokeAsync(target);
	}

	private async Task CloseAsync()
	{
		// The close button's click also reaches the backdrop's handler, on the root.
		if (!_open)
		{
			return;
		}

		_open = false;
		await SyncSourcesAsync();
		await OpenChanged.InvokeAsync(false);
	}

	private async Task HandleKeyDownAsync(MokaKeyEventArgs e)
	{
		if (e.IsComposing)
		{
			return;
		}

		if (e.Key == "Escape")
		{
			await CloseAsync();
			return;
		}

		// With Alt, Ctrl or Cmd these are the browser's own shortcuts, such as Alt+Left for back. A field or a control in
		// the facts keeps the keys it uses itself.
		if (e.AltKey || e.CtrlKey || e.MetaKey || e.Owner is MokaKeyOwner.Field or MokaKeyOwner.Control)
		{
			return;
		}

		switch (e.Key)
		{
			case "ArrowLeft":
				await ShowAsync(_index - 1);
				break;
			case "ArrowRight":
				await ShowAsync(_index + 1);
				break;
			case "Home":
				await ShowAsync(0);
				break;
			case "End":
				await ShowAsync(Items.Count - 1);
				break;
		}
	}

	private enum LightboxStageKind
	{
		Image,
		Preview,
		Waiting,
		Failed
	}

	private readonly record struct LightboxStage(LightboxStageKind Kind, string? Source)
	{
		public bool Waiting => Kind is LightboxStageKind.Preview or LightboxStageKind.Waiting;
	}

	// One read of a full image. Cancelled when it drops away, and disposed once the read has finished, so a cancel after
	// that is skipped.
	private sealed class LoadedSource : IDisposable
	{
		private readonly CancellationTokenSource _cancellation = new();
		private bool _finished;

		public LoadedSource(object id, MokaMediaItem item)
		{
			Id = id;
			Item = item;
			Token = _cancellation.Token;
		}

		public object Id { get; }

		public MokaMediaItem Item { get; }

		public CancellationToken Token { get; }

		public string? Url { get; set; }

		public void Cancel()
		{
			if (!_finished)
			{
				_cancellation.Cancel();
			}
		}

		public void Dispose()
		{
			_finished = true;
			_cancellation.Dispose();
		}
	}
}
