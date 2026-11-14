using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Announcer;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Feedback.Toast;

/// <summary>
///     Draws the toasts of <see cref="IMokaToastService" />. Place one in the application layout. Each toast counts down
///     in the browser, as the animation of the bar along its edge, which waits while the pointer is over the toasts,
///     focus is in them or the tab is hidden, so a toast is never gone before someone could read it. Screen readers hear
///     each toast once, through <see cref="IMokaAnnouncer" />; the host carries a <see cref="MokaAnnouncerHost" />.
/// </summary>
public sealed partial class MokaToastHost : MokaComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/Toast/MokaToastHost.razor.js";

	// Progress can be reported as fast as an app likes; drawing it more often than this would only cost renders.
	private static readonly TimeSpan RenderGap = TimeSpan.FromMilliseconds(100);

	private readonly Dictionary<Guid, int> _announced = [];
	private readonly string _idBase = $"moka-toast-{Guid.NewGuid():N}";
	private bool _bound;
	private bool _canAnnounce;
	private TimeProvider? _clock;
	private bool _disposed;
	private DotNetObjectReference<MokaToastHost>? _dotNetRef;
	private long _lastApplied;
	private int _drawLater;
	private int _drawNow;
	private ElementReference _root;
	private object? _settings;
	private int _settingsVersion;
	private ITimer? _throttle;
	private IReadOnlyList<MokaToastMessage> _toasts = [];

	[Inject]
	private IMokaToastService ToastService { get; set; } = default!;

	[Inject]
	private IMokaAnnouncer Announcer { get; set; } = default!;

	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	/// <summary>
	///     How the toasts are drawn. Null takes the theme's choice (<see cref="MokaTheme.WithToasts" />), which is
	///     <see cref="MokaToastLook.Standard" /> unless the app set one. Only the new design draws the looks: on
	///     <see cref="MokaTheme.Dark" /> and <see cref="MokaTheme.Light" /> nothing changes, even with a look set here.
	/// </summary>
	[Parameter]
	public MokaToastLook? Look { get; set; }

	/// <summary>
	///     How several toasts sit together. <see cref="MokaToastStacking.Pile" /> puts the newest in front, nearest the
	///     window's edge, with older ones peeking out behind it, and fans them out into the full list while the pointer
	///     is over the pile or focus is inside it. Null takes the theme's choice
	///     (<see cref="MokaTheme.WithToastStacking" />), which is <see cref="MokaToastStacking.List" /> unless the app
	///     set one. Only the new design piles toasts: on <see cref="MokaTheme.Dark" /> and <see cref="MokaTheme.Light" />
	///     nothing changes, even with a value set here.
	/// </summary>
	[Parameter]
	public MokaToastStacking? Stacking { get; set; }

	/// <summary>Screen position of the toast container. Defaults to <see cref="MokaToastPosition.TopRight" />.</summary>
	[Parameter]
	public MokaToastPosition Position { get; set; } = MokaToastPosition.TopRight;

	/// <summary>
	///     How many toasts show at once, the newest. Older ones wait their turn without counting down, so none goes
	///     unseen. Defaults to 5.
	/// </summary>
	[Parameter]
	public int MaxVisible { get; set; } = 5;

	/// <summary>
	///     How toasts with progress show it, unless a toast sets its own. Null draws
	///     <see cref="MokaToastProgressLook.Spinner" />, or <see cref="MokaToastProgressLook.Pill" /> under the one-line
	///     Pill and Snackbar looks. Only the new design draws the choice; the original design draws Spinner.
	/// </summary>
	[Parameter]
	public MokaToastProgressLook? ProgressLook { get; set; }

	/// <summary>Every word the host writes around the app's own, to change or translate.</summary>
	[Parameter]
	public MokaToastText Text { get; set; } = new();

	/// <summary>
	///     A CSS length to keep clear along the top of the window, such as <c>48px</c> for an app's own title bar.
	///     Applies to the top positions, on top of the host's padding. Written as <c>--moka-toast-offset-top</c>,
	///     which an app can also set from its own CSS on the host or any element around it. Null leaves it at 0.
	/// </summary>
	[Parameter]
	public string? OffsetTop { get; set; }

	/// <summary>
	///     A CSS length to keep clear along the right edge of the window. Applies to the right and centred positions,
	///     on top of the host's padding, as <c>--moka-toast-offset-right</c>. Null leaves it at 0.
	/// </summary>
	[Parameter]
	public string? OffsetRight { get; set; }

	/// <summary>
	///     A CSS length to keep clear along the bottom of the window, such as <c>24px</c> for a status bar. Applies to
	///     the bottom positions, on top of the host's padding, as <c>--moka-toast-offset-bottom</c>. Null leaves it
	///     at 0.
	/// </summary>
	[Parameter]
	public string? OffsetBottom { get; set; }

	/// <summary>
	///     A CSS length to keep clear along the left edge of the window, such as a side rail's width. Applies to the
	///     left and centred positions, on top of the host's padding, as <c>--moka-toast-offset-left</c>. Null leaves
	///     it at 0.
	/// </summary>
	[Parameter]
	public string? OffsetLeft { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-toast-host";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-toast-host--{MokaEnumHelpers.ToCssClass(Position)}")
		.AddClass(LookClass)
		.AddClass("moka-toast-host--pile", Piled)
		.AddClass("moka-toast-host--pile-top", Piled && AtTop)
		.AddClass("moka-toast-host--pile-bottom", Piled && !AtTop)
		.AddClass(Class)
		.Build();

	// Only the offsets that were set are written, so one an app sets in its own CSS, on the host or around it, is not
	// overridden by an empty parameter. The stylesheet reads each with a fallback of 0. Style goes last, so a variable
	// set there wins over its parameter.
	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-toast-offset-top", OffsetTop, !string.IsNullOrWhiteSpace(OffsetTop))
		.AddStyle("--moka-toast-offset-right", OffsetRight, !string.IsNullOrWhiteSpace(OffsetRight))
		.AddStyle("--moka-toast-offset-bottom", OffsetBottom, !string.IsNullOrWhiteSpace(OffsetBottom))
		.AddStyle("--moka-toast-offset-left", OffsetLeft, !string.IsNullOrWhiteSpace(OffsetLeft))
		.AddStyle(Style)
		.Build();

	private TimeProvider Clock => _clock ??= Services.GetService<TimeProvider>() ?? TimeProvider.System;

	// The looks only exist on the new design, and some of them change the markup, so the original design never gets
	// one: a class alone would be harmless there, but a [FAIL] tag or a reversed list would not.
	private bool OnOriginalDesign => CascadedTheme?.Look == MokaLook.Matrix;

	private MokaToastLook ResolvedLook => OnOriginalDesign
		? MokaToastLook.Standard
		: Look ?? CascadedTheme?.Toasts ?? MokaToastLook.Standard;

	private string? LookClass => ResolvedLook == MokaToastLook.Standard
		? null
		: $"moka-toast-host--{MokaEnumHelpers.ToCssClass(ResolvedLook)}";

	private bool Piled => !OnOriginalDesign &&
	                      (Stacking ?? CascadedTheme?.ToastStacking ?? MokaToastStacking.List) == MokaToastStacking.Pile;

	private bool AtTop => Position is MokaToastPosition.TopRight or MokaToastPosition.TopLeft or MokaToastPosition.TopCenter;

	// The terminal look writes a tag such as [FAIL] in place of the icon. The icon is left out of the markup rather than
	// hidden, because MokaIcon sets its display inline.
	private bool ShowsTag => ResolvedLook == MokaToastLook.Terminal;

	private bool ShowsOneLine => ResolvedLook is MokaToastLook.Snackbar or MokaToastLook.Pill;

	// The close button ends the toast's top row. Toasts along the bottom of the window open its name above.
	private MokaTooltipPosition CloseTooltipPosition =>
		Position is MokaToastPosition.BottomRight or MokaToastPosition.BottomLeft or MokaToastPosition.BottomCenter
			? MokaTooltipPosition.TopEnd
			: MokaTooltipPosition.BottomEnd;

	private int VisibleCount => Math.Min(_toasts.Count, Math.Max(0, MaxVisible));

	/// <summary>
	///     For the host's script: a toast's countdown ended. The toast goes unless it started another countdown since.
	/// </summary>
	/// <param name="id">The toast's id, as the countdown's <c>data-moka-toast</c> wrote it.</param>
	/// <param name="run">The countdown's <see cref="MokaToastMessage.Run" />.</param>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnCountdownEnded(string id, int run) =>
		RunDetached(() => ToastService.Expire(Guid.Parse(id, CultureInfo.InvariantCulture), run));

	/// <summary>For the host's script: someone swiped a toast away on a touch screen.</summary>
	/// <param name="id">The toast's id, as its <c>data-moka-toast</c> wrote it.</param>
	[JSInvokable]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnSwiped(string id) =>
		RunDetached(() => ToastService.Dismiss(Guid.Parse(id, CultureInfo.InvariantCulture), MokaToastDismissReason.Swiped));

	/// <summary>Draws whenever the service's toasts change, which no parameter says.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		ToastService.Changed += HandleChanged;
		_toasts = ToastService.Toasts;
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// The toasts redraw only when something they all read changed; a parent's render alone doesn't move this.
		(MokaToastLook, bool, bool, MokaToastPosition, MokaToastProgressLook?, MokaToastText) settings =
			(ResolvedLook, Piled, OnOriginalDesign, Position, ProgressLook, Text);
		if (!Equals(settings, _settings))
		{
			_settings = settings;
			_settingsVersion++;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
		{
			return;
		}

		// The announcer host inside this one has joined by now, so what was already showing is heard.
		_canAnnounce = true;
		Announce();

		_dotNetRef = DotNetObjectReference.Create(this);
		await SafeModuleInvokeVoidAsync(ModulePath, "bindHost", _root, _dotNetRef);
		_bound = true;
	}

	// Written straight into one builder so each toast's key is matched across renders. Only the newest MaxVisible
	// show; a pile keeps its newest toast against the window's edge, which at the top means drawing the list turned
	// round, so Tab still walks the toasts in the order they are drawn.
	private void WriteToasts(RenderTreeBuilder builder)
	{
		int shown = VisibleCount;
		int first = _toasts.Count - shown;
		bool reversed = Piled && AtTop;
		for (int i = 0; i < shown; i++)
		{
			MokaToastMessage toast = _toasts[reversed ? _toasts.Count - 1 - i : first + i];
			MokaToastGate.Write(builder, toast, _settingsVersion, ToastMarkup(toast));
		}
	}

	// A toast that comes, goes, or has new words to read draws at once. An update that only moves progress or redraws
	// content draws at once after a quiet spell, and within the gap after a draw waits for one draw at its end, which
	// shows the latest; so progress reported ten or a thousand times a second costs at most ten draws.
	private void HandleChanged()
	{
		if (_disposed)
		{
			return;
		}

		bool quiet = OnlyQuietChanges(Volatile.Read(ref _toasts), ToastService.Toasts);
		TimeSpan wait = quiet ? RenderGap - Clock.GetElapsedTime(Volatile.Read(ref _lastApplied)) : TimeSpan.Zero;
		if (wait <= TimeSpan.Zero)
		{
			if (Interlocked.Exchange(ref _drawNow, 1) == 0)
			{
				RunDetached(ApplyQueued);
			}

			return;
		}

		if (Volatile.Read(ref _drawNow) == 1 || Interlocked.Exchange(ref _drawLater, 1) == 1)
		{
			return;
		}

		_throttle ??= Clock.CreateTimer(_ => RunDetached(ApplyQueued), null, Timeout.InfiniteTimeSpan,
			Timeout.InfiniteTimeSpan);
		_throttle.Change(wait, Timeout.InfiniteTimeSpan);
	}

	// Quiet: the same toasts in the same order, none starting a new countdown, counting a repeat or saying new words.
	private static bool OnlyQuietChanges(IReadOnlyList<MokaToastMessage> drawn, IReadOnlyList<MokaToastMessage> now)
	{
		if (drawn.Count != now.Count)
		{
			return false;
		}

		for (int i = 0; i < now.Count; i++)
		{
			MokaToastMessage before = drawn[i];
			MokaToastMessage after = now[i];
			if (!ReferenceEquals(before, after) &&
			    (before.Id != after.Id || before.Run != after.Run || before.Count != after.Count ||
			     before.Announcement != after.Announcement))
			{
				return false;
			}
		}

		return true;
	}

	private void ApplyQueued()
	{
		if (_disposed)
		{
			return;
		}

		Volatile.Write(ref _drawNow, 0);
		Volatile.Write(ref _drawLater, 0);
		_throttle?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
		Volatile.Write(ref _lastApplied, Clock.GetTimestamp());
		Volatile.Write(ref _toasts, ToastService.Toasts);
		Announce();
		StateHasChanged();
	}

	// Each toast is heard when it first shows and again when the service says its words are new; a toast waiting
	// beyond MaxVisible is heard when its turn comes. Errors interrupt; everything else waits its turn.
	private void Announce()
	{
		if (!_canAnnounce)
		{
			return;
		}

		int shown = VisibleCount;
		for (int i = _toasts.Count - shown; i < _toasts.Count; i++)
		{
			MokaToastMessage toast = _toasts[i];
			if (_announced.TryGetValue(toast.Id, out int heard) && heard == toast.Announcement)
			{
				continue;
			}

			_announced[toast.Id] = toast.Announcement;
			string words = Text.Announcement(toast);
			if (!string.IsNullOrWhiteSpace(words))
			{
				Announcer.Announce(words,
					toast.Severity == MokaToastSeverity.Error ? MokaPoliteness.Assertive : MokaPoliteness.Polite);
			}
		}

		if (_announced.Count > _toasts.Count)
		{
			HashSet<Guid> present = [.. _toasts.Select(t => t.Id)];
			foreach (Guid gone in _announced.Keys.Where(id => !present.Contains(id)).ToArray())
			{
				_announced.Remove(gone);
			}
		}
	}

	private void HandleClose(Guid id) => ToastService.Dismiss(id, MokaToastDismissReason.Closed);

	private async Task HandleActionAsync(MokaToastMessage toast)
	{
		if (toast.OnActionAsync is not null)
		{
			await toast.OnActionAsync();
		}
		else
		{
			toast.OnAction?.Invoke();
		}

		if (toast.CloseOnAction)
		{
			ToastService.Dismiss(toast.Id, MokaToastDismissReason.Action);
		}
	}

	// Only a toast with something to run on a click listens to clicks: a default callback renders no listener.
	private EventCallback ClickCallback(MokaToastMessage toast)
	{
		if (toast.OnClickAsync is { } clickAsync)
		{
			return EventCallback.Factory.Create(this, clickAsync);
		}

		return toast.OnClick is { } click ? EventCallback.Factory.Create(this, click) : default;
	}

	private MokaToastProgressLook? ProgressLookFor(MokaToastMessage toast)
	{
		if (toast.Progress is null)
		{
			return null;
		}

		if (OnOriginalDesign)
		{
			return MokaToastProgressLook.Spinner;
		}

		return toast.ProgressLook ?? ProgressLook ??
			(ShowsOneLine ? MokaToastProgressLook.Pill : MokaToastProgressLook.Spinner);
	}

	private static string ToastCss(MokaToastMessage toast, MokaToastProgressLook? progressLook) =>
		new CssBuilder("moka-toast")
			.AddClass($"moka-toast--{MokaEnumHelpers.ToCssClass(toast.Severity)}")
			.AddClass("moka-toast--clickable", toast.OnClick is not null || toast.OnClickAsync is not null)
			.AddClass($"moka-toast--progress-{MokaEnumHelpers.ToCssClass(progressLook ?? default)}", progressLook is not null)
			.Build();

	// A one-line look shows the title alone. The message stays in the page for screen readers.
	private string MessageCss(MokaToastMessage toast) => new CssBuilder("moka-toast-message")
		.AddClass("moka-visually-hidden", ShowsOneLine && !string.IsNullOrEmpty(toast.Title))
		.Build();

	private string MessageId(MokaToastMessage toast) => $"{_idBase}-{toast.Id:N}";

	private static string RingCss(MokaToastMessage toast) => new CssBuilder("moka-toast-ring")
		.AddClass("moka-toast-ring--indeterminate", toast.Progress?.IsIndeterminate == true)
		.Build();

	// An indeterminate ring turns with the shared spin, which keeps turning under reduced motion like every busy icon.
	private static string RingTrackCss(MokaToastMessage toast) => new CssBuilder("moka-toast-ring__track")
		.AddClass("moka-spin", toast.Progress?.IsIndeterminate == true)
		.Build();

	private static string FillCss(MokaToastMessage toast, string part) => new CssBuilder(part)
		.AddClass($"{part}--indeterminate", toast.Progress?.IsIndeterminate == true)
		.Build();

	private static string? ProgressStyle(MokaToastMessage toast) => toast.Progress?.Value is { } share
		? new StyleBuilder()
			.AddStyle("--_toast-progress", share.ToString("0.####", CultureInfo.InvariantCulture))
			.Build()
		: null;

	private static string? PercentNow(MokaToastMessage toast) => toast.Progress?.Value is { } share
		? Math.Round(share * 100).ToString(CultureInfo.InvariantCulture)
		: null;

	private string ProgressWords(MokaToastMessage toast) => toast.Progress?.Value is { } share
		? Text.Percent(share)
		: Text.Working;

	private static string? TimerStyle(MokaToastMessage toast) => toast.Duration is { } duration
		? new StyleBuilder()
			.AddStyle("--_toast-duration",
				$"{Math.Round(duration.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)}ms")
			.Build()
		: null;

	/// <summary>Gets the severity icon for a toast, honouring any icon of its own.</summary>
	internal static MokaIconDefinition GetSeverityIcon(MokaToastMessage toast)
		=> toast.Icon ?? MokaFeedbackFormat.SeverityIcon(toast.Severity);

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_disposed = true;
		ToastService.Changed -= HandleChanged;
		_throttle?.Dispose();

		if (_bound)
		{
			_bound = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "unbindHost", _root);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
