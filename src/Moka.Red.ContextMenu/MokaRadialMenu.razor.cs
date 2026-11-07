using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
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
	private bool _open;
	private OpenKey? _openedFor;
	private bool _placed;
	private bool _restoreFocus;
	private ElementReference _ringRef;
	private double _x;
	private double _y;

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
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-radial-x", Pixels(_x))
		.AddStyle("--moka-radial-y", Pixels(_y))
		.AddStyle(Style)
		.Build();

	private IReadOnlyList<MokaRadialItem> Items => Spec?.Items ?? [];

	private MokaRadialItem? ActiveItem => _active >= 0 && _active < Items.Count ? Items[_active] : null;

	private bool InChoices => _choice >= 0 && ActiveItem is { HasChoices: true, Disabled: false } item && _choice < item.Choices!.Count;

	private string? ActiveDescendant => InChoices ? ChoiceId(_active, _choice) : ActiveItem is null ? null : ItemId(_active);

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

		OpenKey key = new(X, Y, OpenedFromKeyboard, AnchorToFocus, Flick);
		if (key == _openedFor)
		{
			ClampHighlight();
			return;
		}

		_openedFor = key;
		_captureFocus = !_open;
		_open = true;
		_restoreFocus = false;
		_active = OpenedFromKeyboard && Items.Count > 0 ? 0 : -1;
		_choice = -1;
		_x = X;
		_y = Y;
		_measureFocus = AnchorToFocus;
		_placed = !AnchorToFocus;
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

		if (_open && _measureFocus)
		{
			_measureFocus = false;
			if (await SafeModuleInvokeAsync<FocusCenter?>(Module, "measureFocusCenter") is { } center)
			{
				_x = center.X;
				_y = center.Y;
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
			await SafeModuleInvokeVoidAsync(Module, "bind", _ringRef, _dotNetRef, Flick);
			await SafeFocusAsync(_ringRef);
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
	/// </summary>
	/// <param name="index">The button's position.</param>
	/// <param name="choice">The choice's position in the button's list.</param>
	[JSInvokable]
	public void OnPointed(int index, int choice)
	{
		_active = index >= 0 && index < Items.Count ? index : -1;
		_choice = _active >= 0 && choice >= 0 ? choice : -1;
		ClampHighlight();
	}

	/// <summary>Called from JS when a held button is let go over a button or a choice after moving.</summary>
	/// <param name="index">The button's position.</param>
	/// <param name="choice">The choice's position in the button's list, or -1 for the button itself.</param>
	[JSInvokable]
	public void OnFlicked(int index, int choice)
	{
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

	private async Task HandleKeyDownAsync(KeyboardEventArgs e)
	{
		if (!_open || e.CtrlKey || e.AltKey || e.MetaKey)
		{
			return;
		}

		int count = Items.Count;
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
		if (index < 0 || index >= Items.Count)
		{
			return;
		}

		_active = index;
		_choice = -1;
		RequestFit(Items[index], index);
	}

	private async Task ActivateAsync()
	{
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

		// A button with choices opens its list first: the keyboard lands on the current choice.
		if (choice is null && item.HasChoices)
		{
			int index = IndexOf(item);
			if (index < 0)
			{
				return;
			}

			_active = index;
			int current = item.Choices!.ToList().FindIndex(c => c.Checked);
			_choice = Math.Max(current, 0);
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

	private async Task CloseAsync()
	{
		if (!_open)
		{
			return;
		}

		_open = false;
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
		if (item.HasChoices && !item.Disabled)
		{
			_fitRequest = index;
		}
	}

	private void ClampHighlight()
	{
		if (_active >= Items.Count)
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

	private string HubWord(MokaRadialMenuSpec spec)
	{
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

	// A string, not a bool: Blazor drops a false boolean attribute, and a missing aria-expanded does not read as collapsed.
	private string? AriaExpanded(MokaRadialItem item, int index)
	{
		if (!item.HasChoices || item.Disabled)
		{
			return null;
		}

		return index == _active ? "true" : "false";
	}

	private string SlotClass(MokaRadialItem item, int index) => new CssBuilder("moka-radial-menu__slot")
		.AddClass("moka-radial-menu__slot--danger", item.Tone == MokaRadialTone.Danger)
		.AddClass("moka-radial-menu__slot--good", item.Tone == MokaRadialTone.Good)
		.AddClass("moka-radial-menu__slot--disabled", item.Disabled)
		.AddClass("moka-radial-menu__slot--active", index == _active)
		.Build();

	private static string? SlotStyle(int index, int count) => new StyleBuilder()
		.AddStyle("--moka-radial-angle", FormattableString.Invariant($"{Angle(index, count):0.##}deg"))
		.AddStyle("--moka-radial-delay", FormattableString.Invariant($"{index * OpenStagger}ms"))
		.Build();

	private static string ChoicesClass(int index, int count) => new CssBuilder("moka-radial-menu__choices")
		.AddClass($"moka-radial-menu__choices--{ChoicesSide(Angle(index, count))}")
		.Build();

	private string ChoiceClass(int index, int choice) => new CssBuilder("moka-radial-menu__choice")
		.AddClass("moka-radial-menu__choice--active", index == _active && choice == _choice)
		.Build();

	/// <summary>The button's angle in degrees, clockwise from the right, with the first button at the top.</summary>
	private static double Angle(int index, int count) => index * 360.0 / Math.Max(count, 1) - 90;

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

	private static string Pixels(double value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "px";

	private readonly record struct OpenKey(double X, double Y, bool FromKeyboard, bool AnchorToFocus, bool Flick);

	/// <summary>The focused element's centre, as returned by <c>measureFocusCenter</c>.</summary>
	internal sealed class FocusCenter
	{
		/// <summary>Distance from the left of the viewport.</summary>
		public double X { get; set; }

		/// <summary>Distance from the top of the viewport.</summary>
		public double Y { get; set; }
	}
}
