using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;

namespace Moka.Red.Primitives.Emoji;

/// <summary>
///     Picks an emoji: search, group tabs and a grid of every emoji Unicode lists, with skin tones, a frequently used
///     group the app keeps, a preview strip and arrow keys across the grid. <see cref="MokaEmojiOptions.Shape" /> lays it
///     out as a popover grid, a quick strip, a docked composer panel or a search list. Inline by default; with
///     <see cref="Anchor" /> it opens in the browser's top layer against that element, as the emoji buttons of
///     MokaEmojiPicker, MokaTextField, MokaTextArea, MokaChat and MokaReactions use it.
/// </summary>
/// <remarks>
///     Blazor draws the frame and the browser draws the grid: a collocated module fetches the data file
///     (<c>_content/Moka.Red.Primitives/moka-emoji.en.json</c>, about 42 KB with Brotli) on first open, draws the cells,
///     runs search, hover and the keys, and calls .NET once per pick. Emoji this system can't draw are hidden by a probe
///     on first open, or above <see cref="MokaEmojiOptions.MaxVersion" />. Flags draw in MokaFlag's font, so they show
///     on Windows.
/// </remarks>
public partial class MokaEmojiPanel : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/Emoji/MokaEmojiPanel.razor.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";
	private const string Hand = "\u270B";

	private static readonly string[] ToneMarks = ["", "\U0001F3FB", "\U0001F3FC", "\U0001F3FD", "\U0001F3FE", "\U0001F3FF"];

	private static readonly MokaSkinTone[] AllTones = Enum.GetValues<MokaSkinTone>();

	private static readonly (string Id, MokaIconDefinition Icon)[] GroupTabs =
	[
		("smileys", MokaIcons.Content.Smile),
		("people", MokaIcons.Content.Hand),
		("nature", MokaIcons.Content.Leaf),
		("food", MokaIcons.Content.Apple),
		("travel", MokaIcons.Content.Plane),
		("activities", MokaIcons.Content.Trophy),
		("objects", MokaIcons.Content.Lightbulb),
		("symbols", MokaIcons.Content.Hash),
		("flags", MokaIcons.Toggle.Flag)
	];

	private readonly string _id = $"moka-emoji-{Guid.NewGuid():N}";
	private ElementReference _backdrop;
	private ElementReference? _closeFocus;
	private DotNetObjectReference<MokaEmojiPanel>? _dotNetRef;
	private bool _expanded;
	private bool _focusMore;
	private bool _focusStart;
	private bool _focusToneButton;
	private bool _focusTones;
	private int _generation;
	private bool? _lastOpenParameter;
	private IReadOnlyList<MokaEmojiUse>? _lastRecentParameter;
	private MokaSkinTone? _lastToneParameter;
	private ElementReference _moreButton;
	private bool _open = true;
	private IReadOnlyList<MokaEmojiUse>? _recent;
	private ElementReference _root;
	private string? _sentSignature;
	private string? _shownPopup;
	private bool _tonesOpen;
	private string? _tonesShown;
	private MokaSkinTone _tone;
	private ElementReference _toneButton;
	private ElementReference _tones;
	private ElementReference _tonesBackdrop;

	[Inject] private IServiceProvider Services { get; set; } = default!;

	/// <summary>How the panel looks and behaves. Null is <see cref="MokaEmojiOptions.Default" />.</summary>
	[Parameter]
	public MokaEmojiOptions? Options { get; set; }

	/// <summary>
	///     Raised with a new record when the panel changes the skin tone or the frequently used list, for the app to
	///     store. The panel keeps both itself for as long as it lives either way.
	/// </summary>
	[Parameter]
	public EventCallback<MokaEmojiOptions> OptionsChanged { get; set; }

	/// <summary>Raised once per pick, with the emoji as it goes into text and its name and shortcode.</summary>
	[Parameter]
	public EventCallback<MokaEmojiPick> OnPick { get; set; }

	/// <summary>The emoji currently chosen, marked in the grid and pressed in the quick strip.</summary>
	[Parameter]
	public string? Selected { get; set; }

	/// <summary>
	///     The element the panel opens against, usually the button that opened it. With it the panel is a popup in the
	///     browser's top layer, placed by <see cref="Placement" />, so no card or dialog that hides its overflow can cut
	///     it off; a click outside or Escape closes it and puts focus back. Without it the panel sits where it is.
	/// </summary>
	[Parameter]
	public ElementReference? Anchor { get; set; }

	/// <summary>Whether the panel shows. Default true. Two-way bindable; Escape and a click outside close a popup.</summary>
	[Parameter]
	public bool Open { get; set; } = true;

	/// <summary>Raised when the panel closes itself.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>Where a popup opens against <see cref="Anchor" />, flipping when there's no room. Default below its start.</summary>
	[Parameter]
	public MokaTooltipPosition Placement { get; set; } = MokaTooltipPosition.BottomStart;

	/// <summary>
	///     Whether a pick closes the panel: a popup, or the grid a strip opened. Default true. A composer panel stays
	///     open for several picks either way.
	/// </summary>
	[Parameter]
	public bool CloseOnPick { get; set; } = true;

	/// <summary>Where focus goes when the panel closes. Null is <see cref="Anchor" />.</summary>
	[Parameter]
	public ElementReference? FocusOnClose { get; set; }

	/// <summary>
	///     Whether a pick that closes the panel puts focus where <see cref="FocusOnClose" /> says. Default true. Turn it
	///     off when <see cref="OnPick" /> moves focus itself, as a field's emoji button does when it puts focus back in the
	///     field; Escape still returns focus.
	/// </summary>
	[Parameter]
	public bool FocusAfterPick { get; set; } = true;

	/// <summary>Whether an inline panel moves focus to its search box when it appears. A popup always does.</summary>
	[Parameter]
	public bool AutoFocus { get; set; }

	/// <summary>The panel's height, such as "24rem". Null takes the shape's own.</summary>
	[Parameter]
	public string? Height { get; set; }

	/// <summary>The panel's name. Null is <see cref="MokaEmojiText.PanelLabel" />.</summary>
	[Parameter]
	public string? AriaLabel { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-emoji-panel";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-emoji-panel--{MokaEnumHelpers.ToCssClass(Shape)}")
		.AddClass($"moka-emoji-panel--nav-{MokaEnumHelpers.ToCssClass(Opt.Navigation)}")
		.AddClass("moka-emoji-panel--popup", IsPopup)
		.AddClass("moka-emoji-panel--expanded", Shape == MokaEmojiShape.Strip && _expanded)
		.AddClass("moka-emoji-panel--disabled", Disabled)
		.AddClass(Class)
		.Build();

	// The root holds the strip and the panel's box, so it takes the margin and padding; the box takes the radius.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("--_body-height", Height)
		.AddStyle(Style)
		.Build();

	private string? BodyStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private MokaEmojiOptions Opt => Options ?? MokaEmojiOptions.Default;

	private MokaEmojiText Words => Opt.Text ?? MokaEmojiText.Default;

	private MokaEmojiShape Shape => Opt.Shape;

	// A field passed before the host's first render holds an empty reference, which can't be measured.
	private bool IsPopup => Anchor is { Id.Length: > 0 };

	private bool ShowsBody => Shape != MokaEmojiShape.Strip || _expanded;

	private bool HasTop => Opt.ShowSearch || Opt.SkinTones;

	private bool ClosesOnEscape => IsPopup || OpenChanged.HasDelegate || (Shape == MokaEmojiShape.Strip && _expanded);

	private bool StopsKeys => _open && ClosesOnEscape;

	private string PanelLabel => AriaLabel ?? Words.PanelLabel;

	private string BodyId => $"{_id}-body";

	private string GridId => $"{_id}-grid";

	private string TonesId => $"{_id}-tones";

	private string PopupKey => $"{_id}-popup";

	private string TonesKey => $"{_id}-tones";

	private string BodyCss => new CssBuilder("moka-emoji-panel__body")
		.AddClass("moka-emoji-panel__body--rail", TabsAt == TabPlace.Side)
		.AddClass("moka-emoji-panel__body--tabs-below", TabsAt == TabPlace.Bottom)
		.Build();

	private IReadOnlyList<string> QuickList => Opt.Quick ?? MokaEmojiOptions.DefaultQuick;

	private string ToneButtonLabel => $"{Words.SkinTone}, {Words.ToneName(_tone)}";

	private IReadOnlyList<string> Frequent { get; set; } = [];

	private List<NavGroup> NavGroups { get; } = [];

	private TabPlace TabsAt => Opt.Navigation switch
	{
		MokaEmojiNavigation.Tabs => Shape == MokaEmojiShape.Composer ? TabPlace.Bottom : TabPlace.Top,
		MokaEmojiNavigation.Rail => TabPlace.Side,
		_ => TabPlace.None
	};
	private DateTimeOffset Now => (Services.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Open moves the panel only when the parent passes a new value, so a parent render can't undo an Escape
		// (gotcha 9).
		if (_lastOpenParameter != Open)
		{
			bool wasShown = _lastOpenParameter is not null && _open;
			_lastOpenParameter = Open;
			_open = Open;
			if (Open && !wasShown)
			{
				Shown();
			}
		}

		if (_lastToneParameter != Opt.SkinTone)
		{
			_lastToneParameter = Opt.SkinTone;
			_tone = Opt.SkinTone;
		}

		if (_recent is null || !ReferenceEquals(_lastRecentParameter, Opt.Recent))
		{
			_lastRecentParameter = Opt.Recent;
			_recent = Opt.Recent ?? _recent ?? [];
		}

		Rank();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		string? popup = _open && IsPopup ? FormattableString.Invariant($"{Anchor!.Value.Id}|{Placement}") : null;
		if (popup != _shownPopup)
		{
			_shownPopup = popup;
			if (popup is null)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", PopupKey);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", PopupKey, _root, Anchor!.Value, _backdrop,
					new Dictionary<string, object?> { ["placement"] = MokaEnumHelpers.ToCssClass(Placement) });
			}
		}

		string? tones = _open && ShowsBody && _tonesOpen && Opt.SkinTones ? TonesKey : null;
		if (tones != _tonesShown)
		{
			_tonesShown = tones;
			if (tones is null)
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", TonesKey);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "open", TonesKey, _tones, _toneButton, _tonesBackdrop,
					new Dictionary<string, object?> { ["placement"] = "bottom-end" });
			}
		}

		if (_open)
		{
			string signature = Signature();
			if (signature != _sentSignature)
			{
				_sentSignature = signature;
				_dotNetRef ??= DotNetObjectReference.Create(this);
				await SafeModuleInvokeVoidAsync(ModulePath, "update", _root, _dotNetRef, ModuleOptions());
			}
		}
		else
		{
			_sentSignature = null;
		}

		if (_focusStart && _open)
		{
			_focusStart = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "focusStart", _root);
		}

		if (_focusTones && _tonesShown is not null)
		{
			_focusTones = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "focusTone", _tones);
		}

		if (_focusToneButton)
		{
			_focusToneButton = false;
			await SafeFocusAsync(_toneButton);
		}

		if (_focusMore)
		{
			_focusMore = false;
			await SafeFocusAsync(_moreButton);
		}

		if (_closeFocus is { } target)
		{
			_closeFocus = null;
			await SafeFocusAsync(target);
		}
	}

	/// <summary>Called from the panel's script when someone picks an emoji.</summary>
	/// <param name="emoji">The emoji as it goes into text, with the skin tone applied.</param>
	/// <param name="baseEmoji">The emoji without a skin tone.</param>
	/// <param name="name">Its name.</param>
	/// <param name="shortcode">Its main shortcode, without colons.</param>
	[JSInvokable]
	public Task OnEmojiPicked(string emoji, string baseEmoji, string name, string shortcode)
	{
		if (string.IsNullOrEmpty(emoji) || Disabled)
		{
			return Task.CompletedTask;
		}

		MokaEmojiPick pick = new(emoji, name ?? "", shortcode ?? "", string.IsNullOrEmpty(baseEmoji) ? emoji : baseEmoji);

		// A failure in the app's handler reaches its error boundary, not the script's promise.
		RunDetached(() => PickAsync(pick));
		return Task.CompletedTask;
	}

	/// <summary>Closes the panel, as Escape does, and raises <see cref="OpenChanged" />.</summary>
	public Task CloseAsync() => CloseAsync(true);

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_shownPopup is not null)
		{
			_shownPopup = null;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", PopupKey);
		}

		if (_tonesShown is not null)
		{
			_tonesShown = null;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", TonesKey);
		}

		if (_sentSignature is not null)
		{
			_sentSignature = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "forget", _root);
		}

		_dotNetRef?.Dispose();
		_dotNetRef = null;
		await base.DisposeAsyncCore();
	}

	private void Rank()
	{
		Frequent = Opt.ShowFrequent ? MokaEmojiRecent.Ranked(_recent, Math.Max(0, Opt.FrequentCount)) : [];
		NavGroups.Clear();
		if (Frequent.Count > 0)
		{
			NavGroups.Add(new NavGroup("frequent", Words.Frequent, MokaIcons.Status.Clock));
		}

		for (int i = 0; i < GroupTabs.Length; i++)
		{
			NavGroups.Add(new NavGroup(GroupTabs[i].Id, Words.GroupName(i), GroupTabs[i].Icon));
		}
	}

	private void Shown()
	{
		_generation++;
		_tonesOpen = false;
		_focusStart = IsPopup || AutoFocus;
	}

	private string Signature() => string.Join('\u001f',
		_generation.ToString(CultureInfo.InvariantCulture),
		_expanded ? "1" : "0",
		Shape.ToString(),
		Opt.Navigation.ToString(),
		Opt.SearchKeywords ? "1" : "0",
		((int)_tone).ToString(CultureInfo.InvariantCulture),
		string.Join(' ', Frequent),
		Opt.Columns?.ToString(CultureInfo.InvariantCulture) ?? "",
		Opt.MaxVersion?.ToString(CultureInfo.InvariantCulture) ?? "",
		Opt.DataSource ?? "",
		Selected ?? "",
		Words.GetHashCode().ToString(CultureInfo.InvariantCulture),
		string.Join(' ', QuickList));

	private Dictionary<string, object?> ModuleOptions()
	{
		MokaEmojiText words = Words;
		return new Dictionary<string, object?>
		{
			["shape"] = MokaEnumHelpers.ToCssClass(Shape),
			["navigation"] = MokaEnumHelpers.ToCssClass(Opt.Navigation),
			["keywords"] = Opt.SearchKeywords,
			["tone"] = (int)_tone,
			["frequent"] = Frequent.ToArray(),
			["columns"] = Opt.Columns,
			["maxVersion"] = Opt.MaxVersion,
			["dataSource"] = Opt.DataSource,
			["selected"] = Selected,
			["groups"] = words.Groups?.ToArray(),
			["words"] = new Dictionary<string, object?>
			{
				["frequent"] = words.Frequent,
				["common"] = words.Common,
				["searchOnly"] = words.SearchOnlyNote,
				["oneMatch"] = words.OneMatch,
				["matches"] = words.Matches,
				["noMatch"] = words.NoMatch,
				["noMatchHint"] = words.NoMatchHint,
				["loading"] = words.Loading,
				["failed"] = words.Failed,
				["nothing"] = words.NothingLit
			}
		};
	}

	private async Task PickAsync(MokaEmojiPick pick)
	{
		_recent = MokaEmojiRecent.Add(_recent, pick.Base, Now);
		Rank();

		if (OnPick.HasDelegate)
		{
			await OnPick.InvokeAsync(pick);
		}

		await RaiseOptionsChangedAsync();

		if (CloseOnPick && Shape != MokaEmojiShape.Composer)
		{
			if (IsPopup)
			{
				await CloseAsync(FocusAfterPick);
				return;
			}

			if (Shape == MokaEmojiShape.Strip && _expanded)
			{
				_expanded = false;
				_focusMore = true;
			}
		}

		StateHasChanged();
	}

	private async Task RaiseOptionsChangedAsync()
	{
		if (!OptionsChanged.HasDelegate)
		{
			return;
		}

		await OptionsChanged.InvokeAsync(Opt with { Recent = _recent, SkinTone = _tone });
	}

	private async Task CloseAsync(bool returnFocus)
	{
		if (!_open)
		{
			return;
		}

		_open = false;
		_tonesOpen = false;
		if (returnFocus)
		{
			_closeFocus = FocusOnClose is { Id.Length: > 0 } ? FocusOnClose : IsPopup ? Anchor : null;
		}

		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(false);
		}

		StateHasChanged();
	}

	private Task CloseFromBackdropAsync() => CloseAsync(false);

	private async Task HandleKeyDownAsync(KeyboardEventArgs e)
	{
		if (e.Key != "Escape" || e.IsComposing)
		{
			return;
		}

		if (IsPopup || OpenChanged.HasDelegate)
		{
			await CloseAsync(true);
		}
		else if (Shape == MokaEmojiShape.Strip && _expanded)
		{
			_expanded = false;
			_focusMore = true;
		}
	}

	private void ToggleMore()
	{
		_expanded = !_expanded;
		_tonesOpen = false;
		_focusStart = _expanded;
	}

	private void ToggleTones()
	{
		_tonesOpen = !_tonesOpen;
		_focusTones = _tonesOpen;
	}

	private void CloseTones()
	{
		_tonesOpen = false;
	}

	private void HandleTonesKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Escape")
		{
			_tonesOpen = false;
			_focusToneButton = true;
		}
	}

	private async Task ChooseToneAsync(MokaSkinTone tone)
	{
		_tonesOpen = false;
		_focusToneButton = true;
		if (tone == _tone)
		{
			return;
		}

		_tone = tone;
		await RaiseOptionsChangedAsync();
	}

	private static string ToneGlyph(MokaSkinTone tone) => Hand + ToneMarks[(int)tone];

	private string QuickCss(string glyph) => new CssBuilder("moka-emoji-panel__quick")
		.AddClass("moka-emoji-panel__quick--chosen", IsSelected(glyph))
		.Build();

	private string Pressed(string glyph) => IsSelected(glyph) ? "true" : "false";

	private bool IsSelected(string glyph) =>
		Selected is { Length: > 0 } selected &&
		string.Equals(MokaEmojiRecent.Bare(selected), MokaEmojiRecent.Bare(glyph), StringComparison.Ordinal);

	private MokaTooltipPosition TabTipPosition(int index)
	{
		int last = NavGroups.Count - 1;
		return TabsAt switch
		{
			TabPlace.Side => MokaTooltipPosition.Right,
			TabPlace.Bottom => index < 2 ? MokaTooltipPosition.TopStart : index > last - 2 ? MokaTooltipPosition.TopEnd : MokaTooltipPosition.Top,
			_ => index < 2 ? MokaTooltipPosition.BottomStart : index > last - 2 ? MokaTooltipPosition.BottomEnd : MokaTooltipPosition.Bottom
		};
	}

	private enum TabPlace
	{
		None,
		Top,
		Side,
		Bottom
	}

	private readonly record struct NavGroup(string Id, string Name, MokaIconDefinition Icon);
}
