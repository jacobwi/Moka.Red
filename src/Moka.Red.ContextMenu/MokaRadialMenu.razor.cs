using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Round buttons around what was right-clicked, with its name in the hub. A button with choices opens a list
///     beside itself. Pick with a click, by letting go of a held button over one (a flick), with the arrow keys and
///     Enter, or with digits 1 to 9. Escape closes it, and focus goes back where it was.
///     <see cref="MokaContextMenuHost" /> draws one for <see cref="IMokaContextMenuService.ShowRadial(MouseEventArgs, MokaRadialMenuSpec)" />,
///     and <see cref="MokaRadialMenuTrigger" /> opens one from markup.
/// </summary>
/// <remarks>
///     The pointer's highlight and the flick stay in the browser: .NET hears where the pointer was just before a
///     key, and what a flick picked. Focus stays on the ring and <c>aria-activedescendant</c> names the highlighted
///     button or choice. The open ring sits in the browser's top layer, so no ancestor clips or captures it and no
///     stacking context covers it.
/// </remarks>
public partial class MokaRadialMenu : MokaComponentBase
{
	private const string Module = "./_content/Moka.Red.ContextMenu/MokaRadialMenu.razor.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	/// <summary>Digits 1 to 9 pick the first nine buttons.</summary>
	private const int DigitKeys = 9;

	/// <summary>How far each button's opening waits after the one before it, in milliseconds.</summary>
	private const int OpenStagger = 16;

	/// <summary>How far each choice on an arc waits after the one before it, in milliseconds.</summary>
	private const int ArcStagger = 30;

	/// <summary>
	///     How close to an edge of the window, in pixels, a ring would be pushed off its target: its radius and a
	///     margin, with the library's 16 px root.
	/// </summary>
	private const double EdgeReach = 140;

	/// <summary>Degrees between the buttons of a corner fan, as long as they fit in <see cref="FanSpan" />.</summary>
	private const double FanStep = 13.5;

	/// <summary>The most a corner fan spreads its buttons over, in degrees, keeping clear of the window's edges.</summary>
	private const double FanSpan = 84;

	/// <summary>The fewest degrees between two choices on an arc.</summary>
	private const double ArcStep = 22;

	/// <summary>The radius of the arc of choices around a ring, in pixels at a 16 px root, for spacing them.</summary>
	private const double ArcRadius = 151;

	private const string DefaultBackText = "Back";

	// The .NET handler moves the highlight and picks, so these must not also scroll the page or move focus out.
	// A button can hold focus for a moment in a browser that focuses what is clicked, so it is covered too.
	private static readonly Dictionary<string, object?>[] KeyRules =
	[
		new()
		{
			["selector"] = null,
			["keys"] = new[] { "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End", "Tab", " ", "Enter" },
			["unlessModified"] = true
		},
		new()
		{
			["selector"] = "button",
			["keys"] = new[] { "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight", "Home", "End", "Tab", " ", "Enter" },
			["unlessModified"] = true
		}
	];

	private readonly string _idPrefix = $"moka-radial-{Guid.NewGuid():N}";

	private int _active = -1;
	private ElementReference _backdropRef;
	private bool _captureFocus;
	private int _choice = -1;
	private DotNetObjectReference<MokaRadialMenu>? _dotNetRef;
	private int _fitRequest = -1;
	private string? _focusToken;
	private bool _justOpened;
	private string? _liftedRing;
	private bool _measureFocus;
	private bool _measureViewport;
	private bool _open;
	private OpenKey? _openedFor;
	private bool _opensLeft = true;
	private bool _opensUp = true;
	private bool _placed;
	private bool _restoreFocus;
	private ElementReference _ringRef;
	private MokaRadialShape _shape = MokaRadialShape.Ring;
	private int _view = -1;
	private bool _viewChanged;
	private double _x;
	private double _y;

	[CascadingParameter]
	private MokaTheme? CascadedTheme { get; set; }

	/// <summary>What the menu holds. Nothing is drawn without it.</summary>
	[Parameter]
	public MokaRadialMenuSpec? Spec { get; set; }

	/// <summary>
	///     Whether the menu is open. It opens when this turns true or the point moves, and closes itself after a
	///     pick or Escape without changing this, so pass a new value or handle <see cref="OnClose" />.
	/// </summary>
	[Parameter]
	public bool Visible { get; set; }

	/// <summary>The centre's distance from the left of the viewport, in pixels. Kept clear of the viewport's edges.</summary>
	[Parameter]
	public double X { get; set; }

	/// <summary>The centre's distance from the top of the viewport, in pixels. Kept clear of the viewport's edges.</summary>
	[Parameter]
	public double Y { get; set; }

	/// <summary>Whether the keyboard opened the menu. It then starts on its first button, so Enter picks it.</summary>
	[Parameter]
	public bool OpenedFromKeyboard { get; set; }

	/// <summary>
	///     Centres the menu on whatever has focus instead of on <see cref="X" /> and <see cref="Y" />, for a menu the
	///     keyboard opened from an element. Measured before the menu takes focus.
	/// </summary>
	[Parameter]
	public bool AnchorToFocus { get; set; }

	/// <summary>
	///     Whether a button was still held when the menu opened. Letting it go over a button, after moving, picks
	///     that button.
	/// </summary>
	[Parameter]
	public bool Flick { get; set; }

	/// <summary>
	///     How the buttons sit around the target. Null takes <see cref="MokaRadialMenuSpec.Shape" />, and without that a
	///     <see cref="MokaRadialShape.Ring" />. A change while open opens the menu again in the new shape.
	/// </summary>
	[Parameter]
	public MokaRadialShape? Shape { get; set; }

	/// <summary>
	///     How the buttons are drawn. Null takes <see cref="MokaRadialMenuSpec.Look" />, and without that the cascaded
	///     theme's <see cref="MokaTheme.Popups" /> where a look of the same name exists (Glass, Terminal), else
	///     <see cref="MokaRadialLook.Standard" />. Under <see cref="MokaTheme.Dark" /> and <see cref="MokaTheme.Light" />
	///     nothing follows the theme, but a look set here or on the spec still draws.
	/// </summary>
	[Parameter]
	public MokaRadialLook? Look { get; set; }

	/// <summary>
	///     How a button's choices show. Null takes <see cref="MokaRadialMenuSpec.ChoiceStyle" />, and without that a
	///     <see cref="MokaRadialChoiceStyle.List" />.
	/// </summary>
	[Parameter]
	public MokaRadialChoiceStyle? ChoiceStyle { get; set; }

	/// <summary>
	///     Whether a <see cref="MokaRadialShape.Ring" /> turns into a <see cref="MokaRadialShape.HalfRing" /> when it
	///     opens near the bottom of the window and a <see cref="MokaRadialShape.CornerFan" /> near a corner, instead of
	///     being pushed away from its target. Null takes <see cref="MokaRadialMenuSpec.AdaptToEdges" />, and without
	///     that it stays off. Measured once as the menu opens.
	/// </summary>
	[Parameter]
	public bool? AdaptToEdges { get; set; }

	/// <summary>
	///     The hub's word for going back while <see cref="MokaRadialChoiceStyle.Replace" /> shows a button's choices.
	///     Null takes <see cref="MokaRadialMenuSpec.BackText" />, and without that "Back".
	/// </summary>
	[Parameter]
	public string? BackText { get; set; }

	/// <summary>Raised with what was picked, after <see cref="MokaRadialMenuSpec.OnPick" />.</summary>
	[Parameter]
	public EventCallback<MokaRadialPick> OnPick { get; set; }

	/// <summary>Raised when the menu closes itself: a pick, Escape, or a click outside.</summary>
	[Parameter]
	public EventCallback OnClose { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-radial-menu";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-radial-menu--{MokaEnumHelpers.ToCssClass(Spec?.Color ?? MokaColor.Primary)}",
			Spec is not null && Spec.Color != MokaColor.Primary)
		.AddClass($"moka-radial-menu--{MokaEnumHelpers.ToCssClass(_shape)}", _shape != MokaRadialShape.Ring)
		.AddClass(_opensLeft ? "moka-radial-menu--opens-left" : "moka-radial-menu--opens-right", _shape == MokaRadialShape.CornerFan)
		.AddClass(_opensUp ? "moka-radial-menu--opens-up" : "moka-radial-menu--opens-down", _shape == MokaRadialShape.CornerFan)
		.AddClass($"moka-radial-menu--{MokaEnumHelpers.ToCssClass(ResolvedLook)}", ResolvedLook != MokaRadialLook.Standard)
		.AddClass($"moka-radial-menu--choices-{MokaEnumHelpers.ToCssClass(ResolvedChoiceStyle)}",
			ResolvedChoiceStyle != MokaRadialChoiceStyle.List)
		.AddClass("moka-radial-menu--back", InSubView)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-radial-x", Pixels(_x))
		.AddStyle("--moka-radial-y", Pixels(_y))
		.AddStyle(Style)
		.Build();

	/// <summary>The shape the menu was asked for, before it adapts to an edge.</summary>
	private MokaRadialShape RequestedShape => Shape ?? Spec?.Shape ?? MokaRadialShape.Ring;

	/// <summary>The look the menu draws.</summary>
	private MokaRadialLook ResolvedLook => Look ?? Spec?.Look ?? ThemeLook;

	/// <summary>How the menu shows a button's choices.</summary>
	private MokaRadialChoiceStyle ResolvedChoiceStyle =>
		ChoiceStyle ?? Spec?.ChoiceStyle ?? MokaRadialChoiceStyle.List;

	private bool ResolvedAdaptToEdges => AdaptToEdges ?? Spec?.AdaptToEdges ?? false;

	private string ResolvedBackText => BackText ?? Spec?.BackText ?? DefaultBackText;

	// Only the new design follows the theme, so Dark and Light draw as they always have.
	private MokaRadialLook ThemeLook => CascadedTheme is { Look: MokaLook.Gate } theme
		? theme.Popups switch
		{
			MokaPopupLook.Glass => MokaRadialLook.Glass,
			MokaPopupLook.Terminal => MokaRadialLook.Terminal,
			_ => MokaRadialLook.Standard
		}
		: MokaRadialLook.Standard;

	private IReadOnlyList<MokaRadialItem> Items => Spec?.Items ?? [];

	private bool Replaces => ResolvedChoiceStyle == MokaRadialChoiceStyle.Replace;

	private bool InSubView => _view >= 0 && _view < Items.Count && Items[_view] is { HasChoices: true, Disabled: false };

	private MokaRadialItem? SubItem => InSubView ? Items[_view] : null;

	private IReadOnlyList<MokaRadialChoice> SubChoices => SubItem?.Choices ?? [];

	private int SlotCount => InSubView ? SubChoices.Count : Items.Count;

	private MokaRadialItem? ActiveItem => !InSubView && _active >= 0 && _active < Items.Count ? Items[_active] : null;

	private MokaRadialChoice? ActiveSubChoice => InSubView && _active >= 0 && _active < SubChoices.Count ? SubChoices[_active] : null;

	private bool InChoices => _choice >= 0 && ActiveItem is { HasChoices: true, Disabled: false } item && _choice < item.Choices!.Count;

	private string? ActiveDescendant
	{
		get
		{
			if (InSubView)
			{
				return ActiveSubChoice is null ? null : ChoiceId(_view, _active);
			}

			return InChoices ? ChoiceId(_active, _choice) : ActiveItem is null ? null : ItemId(_active);
		}
	}

	private bool HasTrack => _shape is MokaRadialShape.Ring or MokaRadialShape.HalfRing or MokaRadialShape.CornerFan or MokaRadialShape.Labeled;

	private bool HasHub => _shape is not (MokaRadialShape.Compact or MokaRadialShape.Marking);

	private bool WordInButton => _shape is MokaRadialShape.Pie or MokaRadialShape.Marking || ResolvedLook == MokaRadialLook.Terminal;

	private bool KeyOnButton => _shape is not (MokaRadialShape.Compact or MokaRadialShape.Marking or MokaRadialShape.Labeled);

	private string IconSize => _shape is MokaRadialShape.Compact or MokaRadialShape.Marking || ResolvedLook == MokaRadialLook.Terminal
		? "1rem"
		: "1.125rem";

	// No listener at all outside the choices, so a click on the hub does nothing and costs nothing.
	private EventCallback<MouseEventArgs> HubBack =>
		InSubView ? EventCallback.Factory.Create<MouseEventArgs>(this, GoBack) : default;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (!Visible || Spec is null)
		{
			_openedFor = null;
			if (_open)
			{
				_open = false;
				_restoreFocus = true;
			}

			return;
		}

		OpenKey key = new(X, Y, OpenedFromKeyboard, AnchorToFocus, Flick, RequestedShape, ResolvedChoiceStyle, ResolvedAdaptToEdges);
		if (key == _openedFor)
		{
			ClampHighlight();
			return;
		}

		_openedFor = key;
		_captureFocus = !_open;
		_open = true;
		_restoreFocus = false;
		_view = -1;
		_viewChanged = false;
		_active = OpenedFromKeyboard && Items.Count > 0 ? 0 : -1;
		_choice = -1;
		_x = X;
		_y = Y;
		_shape = RequestedShape;
		_opensLeft = true;
		_opensUp = true;
		_measureFocus = AnchorToFocus;
		_measureViewport = _shape == MokaRadialShape.CornerFan ||
		                   (_shape == MokaRadialShape.Ring && ResolvedAdaptToEdges);
		_placed = !_measureFocus && !_measureViewport;
		_justOpened = true;
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		// Before anything measures the ring or focuses it: in the top layer its fixed centre is always the window's,
		// which a transformed, filtered or contained ancestor would otherwise have captured.
		await SyncTopLayerAsync();

		// Before the ring takes focus, and before it is even drawn when it centres on the focused element.
		if (_open && _captureFocus)
		{
			_captureFocus = false;
			_focusToken = await SafeModuleInvokeAsync<string?>(Module, "captureFocus");
		}

		// Where it centres and which way it opens are measured before it is drawn, so it never jumps.
		if (_open && (_measureFocus || _measureViewport))
		{
			if (_measureFocus)
			{
				_measureFocus = false;
				if (await SafeModuleInvokeAsync<FocusCenter?>(Module, "measureFocusCenter") is { } center)
				{
					_x = center.X;
					_y = center.Y;
				}
			}

			if (_measureViewport)
			{
				_measureViewport = false;
				FitToWindow(await SafeModuleInvokeAsync<ViewportSize?>(Module, "measureViewport"));
			}

			_placed = true;
			StateHasChanged();
			return;
		}

		if (_open && _justOpened)
		{
			_justOpened = false;
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(Module, "preventKeys", _ringRef, KeyRules);
			await SafeModuleInvokeVoidAsync(Module, "bind", _ringRef, _dotNetRef, Flick, PointerOptions());
			await SafeFocusAsync(_ringRef);
		}

		// The pointer marked buttons that are gone, so the keyboard's highlight shows until it moves again.
		if (_open && _viewChanged)
		{
			_viewChanged = false;
			await SafeModuleInvokeVoidAsync(Module, "reset", _ringRef);
		}

		if (_open && _fitRequest >= 0)
		{
			int index = _fitRequest;
			_fitRequest = -1;
			await SafeModuleInvokeVoidAsync(Module, "fitChoices", _ringRef, index);
		}

		if (!_open && _restoreFocus)
		{
			_restoreFocus = false;
			await RestoreFocusAsync();
		}
	}

	/// <summary>
	///     Called from JS just before a key press while the pointer leads, with the button and choice it points
	///     at (-1 for none), so the key carries on from there. Changes nothing on screen until the key is handled.
	///     While <see cref="MokaRadialChoiceStyle.Replace" /> shows a button's choices, the index is the choice's.
	/// </summary>
	/// <param name="index">The button's position.</param>
	/// <param name="choice">The choice's position in the button's list.</param>
	[JSInvokable]
	public void OnPointed(int index, int choice)
	{
		_active = index >= 0 && index < SlotCount ? index : -1;
		_choice = !InSubView && _active >= 0 && choice >= 0 ? choice : -1;
		ClampHighlight();
	}

	/// <summary>Called from JS when a held button is let go over a button or a choice after moving.</summary>
	/// <param name="index">The button's position, or the choice's while the choices replace the buttons.</param>
	/// <param name="choice">The choice's position in the button's list, or -1 for the button itself.</param>
	[JSInvokable]
	public void OnFlicked(int index, int choice)
	{
		if (InSubView)
		{
			if (index >= 0 && index < SubChoices.Count)
			{
				MokaRadialItem owner = SubItem!;
				MokaRadialChoice flicked = SubChoices[index];
				RunDetached(() => PickAsync(owner, flicked));
			}

			return;
		}

		if (index < 0 || index >= Items.Count)
		{
			return;
		}

		MokaRadialItem item = Items[index];
		MokaRadialChoice? picked = item.HasChoices && choice >= 0 && choice < item.Choices!.Count ? item.Choices[choice] : null;
		RunDetached(() => PickAsync(item, picked));
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		await RestoreFocusAsync();
		if (_liftedRing is not null)
		{
			_liftedRing = null;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _idPrefix);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	// The ring and its backdrop go to the top layer once drawn, and come out once gone. The ring places itself, so
	// Core's moka-popup.js gets no anchor and only lifts it. A ring element drawn again is lifted again.
	private async Task SyncTopLayerAsync()
	{
		string? ring = _open && _placed && Spec is not null ? _ringRef.Id : null;
		if (ring == _liftedRing)
		{
			return;
		}

		_liftedRing = ring;
		if (ring is null)
		{
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _idPrefix);
			return;
		}

		await SafeModuleInvokeVoidAsync(PopupModule, "open", _idPrefix, _ringRef, null, _backdropRef, null);
	}

	// What the module needs to follow the pointer in this shape. It reads the buttons' angles from the page, so a
	// view of choices with a different count needs nothing new.
	private Dictionary<string, object?> PointerOptions() => new()
	{
		["around"] = _shape is not (MokaRadialShape.HalfRing or MokaRadialShape.CornerFan),
		["reach"] = _shape == MokaRadialShape.CornerFan ? 0.8 : 1.0,
		["inner"] = _shape switch
		{
			MokaRadialShape.HalfRing => 0.48,
			MokaRadialShape.CornerFan => 0.96,
			_ => 0.44
		},
		["turn"] = _shape == MokaRadialShape.Pie,
		["arc"] = ResolvedChoiceStyle == MokaRadialChoiceStyle.Ring
	};

	// A corner fan opens toward the middle of the window. A ring that may adapt becomes a fan at a corner and a half
	// ring at the bottom edge, where a whole one would be pushed off its target.
	private void FitToWindow(ViewportSize? viewport)
	{
		if (viewport is not { Width: > 0, Height: > 0 })
		{
			return;
		}

		_opensLeft = _x > viewport.Width / 2;
		_opensUp = _y > viewport.Height / 2;

		if (_shape != MokaRadialShape.Ring)
		{
			return;
		}

		bool nearSide = _x < EdgeReach || _x > viewport.Width - EdgeReach;
		bool nearTop = _y < EdgeReach;
		bool nearBottom = _y > viewport.Height - EdgeReach;

		if (nearSide && (nearTop || nearBottom))
		{
			_shape = MokaRadialShape.CornerFan;
		}
		else if (nearBottom)
		{
			_shape = MokaRadialShape.HalfRing;
		}
	}

	private async Task HandleKeyDownAsync(KeyboardEventArgs e)
	{
		if (!_open || e.CtrlKey || e.AltKey || e.MetaKey)
		{
			return;
		}

		int count = SlotCount;
		bool inChoices = InChoices;
		int choiceCount = inChoices ? ActiveItem!.Choices!.Count : 0;

		switch (e.Key)
		{
			case "ArrowDown" when inChoices:
				_choice = (_choice + 1) % choiceCount;
				break;

			case "ArrowUp" when inChoices:
				_choice = (_choice - 1 + choiceCount) % choiceCount;
				break;

			case "ArrowLeft" or "Escape" when inChoices:
				_choice = -1;
				break;

			case "Escape" when InSubView:
				GoBack();
				break;

			case "ArrowRight" or "ArrowDown":
			case "Tab" when !e.ShiftKey:
				Highlight((_active + 1) % Math.Max(count, 1));
				break;

			case "ArrowLeft" or "ArrowUp" or "Tab":
				Highlight(((_active < 0 ? 0 : _active) - 1 + count) % Math.Max(count, 1));
				break;

			case "Home":
				Highlight(0);
				break;

			case "End":
				Highlight(count - 1);
				break;

			case "Enter" or " " when !e.Repeat:
				await ActivateAsync();
				break;

			case "Escape":
				await CloseAsync();
				break;

			default:
				if (e.Key is [>= '1' and <= '9'] && e.Key[0] - '1' < Math.Min(count, DigitKeys) && !e.Repeat)
				{
					Highlight(e.Key[0] - '1');
					await ActivateAsync();
				}

				break;
		}
	}

	private void Highlight(int index)
	{
		if (index < 0 || index >= SlotCount)
		{
			return;
		}

		_active = index;
		_choice = -1;
		if (!InSubView)
		{
			RequestFit(Items[index], index);
		}
	}

	private async Task ActivateAsync()
	{
		if (InSubView)
		{
			if (ActiveSubChoice is { } subChoice)
			{
				await PickAsync(SubItem!, subChoice);
			}

			return;
		}

		if (ActiveItem is not { } item || item.Disabled)
		{
			return;
		}

		if (InChoices)
		{
			await PickAsync(item, item.Choices![_choice]);
			return;
		}

		await PickAsync(item, null);
	}

	private async Task PickAsync(MokaRadialItem item, MokaRadialChoice? choice)
	{
		if (!_open || item.Disabled)
		{
			return;
		}

		// A button with choices shows them first: the keyboard lands on the current choice.
		if (choice is null && item.HasChoices)
		{
			int index = IndexOf(item);
			if (index < 0)
			{
				return;
			}

			int current = Math.Max(item.Choices!.ToList().FindIndex(c => c.Checked), 0);
			if (Replaces)
			{
				_view = index;
				_active = current;
				_choice = -1;
				_viewChanged = true;

				// A flick lands here outside any event handler, and the ring has to change.
				StateHasChanged();
				return;
			}

			_active = index;
			_choice = current;
			RequestFit(item, index);
			return;
		}

		MokaRadialMenuSpec? spec = Spec;
		MokaRadialPick pick = new(item, choice);

		// Closed first, so an action that opens a dialog doesn't leave the ring over it.
		await CloseAsync();

		if (spec?.OnPick is not null)
		{
			await spec.OnPick(pick);
		}

		await OnPick.InvokeAsync(pick);
	}

	private void GoBack()
	{
		if (!InSubView)
		{
			return;
		}

		_active = _view;
		_view = -1;
		_choice = -1;
		_viewChanged = true;
	}

	private async Task CloseAsync()
	{
		if (!_open)
		{
			return;
		}

		_open = false;
		_view = -1;
		_restoreFocus = true;
		StateHasChanged();
		await OnClose.InvokeAsync();
	}

	private async Task RestoreFocusAsync()
	{
		if (_focusToken is null)
		{
			return;
		}

		string token = _focusToken;
		_focusToken = null;
		await SafeModuleInvokeVoidAsync(Module, "restoreFocus", token);
	}

	private void RequestFit(MokaRadialItem item, int index)
	{
		if (item.HasChoices && !item.Disabled && !Replaces)
		{
			_fitRequest = index;
		}
	}

	private void ClampHighlight()
	{
		if (_view >= 0 && !InSubView)
		{
			_view = -1;
			_active = -1;
		}

		if (_active >= SlotCount)
		{
			_active = -1;
		}

		if (!InChoices)
		{
			_choice = -1;
		}
	}

	private int IndexOf(MokaRadialItem item)
	{
		for (int i = 0; i < Items.Count; i++)
		{
			if (ReferenceEquals(Items[i], item))
			{
				return i;
			}
		}

		return -1;
	}

	private string MenuLabel(MokaRadialMenuSpec spec) => SubItem?.Label ?? spec.Title;

	private string HubTitle(MokaRadialMenuSpec spec) => SubItem?.Label ?? spec.Title;

	private string? HubSub(MokaRadialMenuSpec spec) => InSubView ? ResolvedBackText : spec.Sub;

	private string HubWord(MokaRadialMenuSpec spec)
	{
		if (InSubView)
		{
			return _shape == MokaRadialShape.Labeled ? ResolvedBackText : ActiveSubChoice?.Label ?? ResolvedBackText;
		}

		// The labels say what every button is, so the hub keeps saying what the thing is.
		if (_shape == MokaRadialShape.Labeled)
		{
			return spec.Sub ?? string.Empty;
		}

		if (InChoices)
		{
			return ActiveItem!.Choices![_choice].Label;
		}

		return ActiveItem?.Label ?? spec.Sub ?? string.Empty;
	}

	private string ItemId(int index) => $"{_idPrefix}-item-{index.ToString(CultureInfo.InvariantCulture)}";

	private string ChoiceId(int index, int choice) =>
		$"{ItemId(index)}-choice-{choice.ToString(CultureInfo.InvariantCulture)}";

	private static string? KeyShortcut(int index) =>
		index < DigitKeys ? (index + 1).ToString(CultureInfo.InvariantCulture) : null;

	private static string Digit(int index) => (index + 1).ToString(CultureInfo.InvariantCulture);

	// A string, not a bool: Blazor drops a false boolean attribute, and a missing aria-expanded does not read as collapsed.
	private string? AriaExpanded(MokaRadialItem item, int index)
	{
		if (!item.HasChoices || item.Disabled)
		{
			return null;
		}

		return index == _active && !Replaces ? "true" : "false";
	}

	// A view of choices draws new buttons, so the opening plays again and nothing the pointer marked carries over.
	private string? SlotKey(int index) => Replaces ? (InSubView ? $"{_view}:{index}" : index.ToString(CultureInfo.InvariantCulture)) : null;

	private string SlotClass(MokaRadialItem item, int index) => new CssBuilder("moka-radial-menu__slot")
		.AddClass("moka-radial-menu__slot--danger", item.Tone == MokaRadialTone.Danger)
		.AddClass("moka-radial-menu__slot--good", item.Tone == MokaRadialTone.Good)
		.AddClass("moka-radial-menu__slot--disabled", item.Disabled)
		.AddClass("moka-radial-menu__slot--active", index == _active)
		.Build();

	private string SubSlotClass(int index) => new CssBuilder("moka-radial-menu__slot")
		.AddClass("moka-radial-menu__slot--active", index == _active)
		.Build();

	private string? SlotStyle(int index, int count) => new StyleBuilder()
		.AddStyle("--moka-radial-angle", Degrees(Angle(index, count)))
		.AddStyle("--moka-radial-delay", FormattableString.Invariant($"{index * OpenStagger}ms"))
		.Build();

	private static string? DiscStyle(int count) => new StyleBuilder()
		.AddStyle("--moka-radial-step", Degrees(360.0 / Math.Max(count, 1)))
		.Build();

	private string? SeparatorStyle(int index, int count) => new StyleBuilder()
		.AddStyle("--moka-radial-angle", Degrees(Angle(index, count) + 180.0 / Math.Max(count, 1)))
		.Build();

	private string ChoicesClass(int index, int count) => ResolvedChoiceStyle switch
	{
		MokaRadialChoiceStyle.Ring => "moka-radial-menu__choices moka-radial-menu__choices--arc",
		MokaRadialChoiceStyle.Flyout => "moka-radial-menu__choices moka-radial-menu__choices--flyout",
		_ => new CssBuilder("moka-radial-menu__choices")
			.AddClass($"moka-radial-menu__choices--{ChoicesSide(Angle(index, count))}")
			.Build()
	};

	private string ChoiceClass(int index, int choice) => new CssBuilder("moka-radial-menu__choice")
		.AddClass("moka-radial-menu__choice--active", index == _active && choice == _choice)
		.Build();

	// A conic gradient starts at the top, a quarter turn from where the buttons' angles start.
	private string? ArcStyle(MokaRadialItem item, int index, int count)
	{
		double step = ArcSpacing(item, Angle(index, count));
		double span = step * item.Choices!.Count;
		return new StyleBuilder()
			.AddStyle("--moka-radial-arc-from", Degrees(Angle(index, count) - span / 2 + 90))
			.AddStyle("--moka-radial-arc-span", Degrees(span))
			.Build();
	}

	private string? ArcChoiceStyle(MokaRadialItem item, int index, int count, int choice)
	{
		if (ResolvedChoiceStyle != MokaRadialChoiceStyle.Ring)
		{
			return null;
		}

		double angle = Angle(index, count);
		double offset = (choice - (item.Choices!.Count - 1) / 2.0) * ArcSpacing(item, angle);
		return new StyleBuilder()
			.AddStyle("--moka-radial-choice-angle", Degrees(angle + offset))
			.AddStyle("--moka-radial-choice-delay", FormattableString.Invariant($"{choice * ArcStagger}ms"))
			.Build();
	}

	// Pills lie flat, so along the arc they take their width at the top and bottom and their height at the sides. A
	// longer label needs more of the arc; the estimate is generous, since overlapping pills can't be read.
	private static double ArcSpacing(MokaRadialItem item, double degrees)
	{
		int longest = item.Choices!.Max(c => c.Label.Length);
		bool icons = item.Choices!.Any(c => c.Icon is not null);
		double width = 28 + 7 * longest + (icons ? 20 : 0);
		const double height = 29;
		double radians = degrees * Math.PI / 180;
		double along = Math.Abs(width * Math.Sin(radians)) + Math.Abs(height * Math.Cos(radians)) + 6;
		return Math.Max(ArcStep, along / ArcRadius * 180 / Math.PI);
	}

	/// <summary>The button's angle in degrees, clockwise from the right.</summary>
	private double Angle(int index, int count)
	{
		count = Math.Max(count, 1);
		return _shape switch
		{
			MokaRadialShape.HalfRing => -180 + (index + 0.5) * 180.0 / count,
			MokaRadialShape.CornerFan => FanAngle(index, count),
			_ => index * 360.0 / count - 90
		};
	}

	private double FanAngle(int index, int count)
	{
		double centre = (_opensLeft, _opensUp) switch
		{
			(true, true) => -135,
			(false, true) => -45,
			(false, false) => 45,
			_ => 135
		};
		double step = count > 1 ? Math.Min(FanStep, FanSpan / (count - 1)) : 0;
		return centre + (index - (count - 1) / 2.0) * step;
	}

	// Beside the button, on the side away from the hub: right or left unless the button sits near the top or bottom.
	private static string ChoicesSide(double degrees)
	{
		double radians = degrees * Math.PI / 180;
		double across = Math.Cos(radians);
		if (across > 0.3)
		{
			return "right";
		}

		if (across < -0.3)
		{
			return "left";
		}

		return Math.Sin(radians) < 0 ? "top" : "bottom";
	}

	private static string Degrees(double value) => FormattableString.Invariant($"{value:0.##}deg");

	private static string Pixels(double value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "px";

	private readonly record struct OpenKey(
		double X,
		double Y,
		bool FromKeyboard,
		bool AnchorToFocus,
		bool Flick,
		MokaRadialShape Shape,
		MokaRadialChoiceStyle ChoiceStyle,
		bool AdaptToEdges);

	/// <summary>The focused element's centre, as returned by <c>measureFocusCenter</c>.</summary>
	internal sealed class FocusCenter
	{
		/// <summary>Distance from the left of the viewport.</summary>
		public double X { get; set; }

		/// <summary>Distance from the top of the viewport.</summary>
		public double Y { get; set; }
	}

	/// <summary>The window's size, as returned by <c>measureViewport</c>.</summary>
	internal sealed class ViewportSize
	{
		/// <summary>The width in pixels.</summary>
		public double Width { get; set; }

		/// <summary>The height in pixels.</summary>
		public double Height { get; set; }
	}
}
