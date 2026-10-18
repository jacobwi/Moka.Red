using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;
using Moka.Red.Core.Hotkeys;

namespace Moka.Red.Feedback.Cheatsheet;

/// <summary>
///     A keyboard-shortcut overlay: a centered modal listing shortcut groups in a
///     two-column grid, with descriptions on the left and key chips on the right.
///     Typically toggled by a "?" key handler in the host app. Renders nothing while closed.
/// </summary>
public partial class MokaCheatsheet : MokaVisualComponentBase
{
	private readonly MokaOverlayFocusTrap _focusTrap;
	private ElementReference _dialogRef;
	private bool _disposed;
	private bool? _lastOpen;
	private bool _open;
	private bool _scrollLocked;

	/// <summary>Creates the cheatsheet.</summary>
	public MokaCheatsheet() => _focusTrap = new MokaOverlayFocusTrap(
		element => SafeModuleInvokeAsync<int>(MokaOverlayFocusTrap.Module, "trapFocus", element),
		handle => SafeModuleInvokeVoidAsync(MokaOverlayFocusTrap.Module, "releaseFocus", handle));

	/// <summary>Whether the cheatsheet is currently visible. Two-way bindable. Renders nothing when false.</summary>
	[Parameter]
	public bool Open { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>Heading shown in the header row. Defaults to "Keyboard Shortcuts".</summary>
	[Parameter]
	public string Title { get; set; } = "Keyboard Shortcuts";

	/// <summary>
	///     The shortcut groups rendered in the grid. Leave it null to show the hotkeys registered with
	///     <see cref="IMokaHotkeyService" />, so a shortcut is listed by the same registration that binds it
	///     and the two cannot drift apart. Setting it lists these instead, registry or no registry.
	/// </summary>
	[Parameter]
	public IReadOnlyList<MokaCheatsheetGroup>? Groups { get; set; }

	/// <summary>
	///     Heading for registered hotkeys that name no <see cref="MokaHotkey.Group" />. Defaults to "General".
	/// </summary>
	[Parameter]
	public string UngroupedTitle { get; set; } = "General";

	// Resolved from the provider rather than with [Inject], which does a required resolve even for a nullable
	// property: a cheatsheet given its own Groups has to work in an app that registered no hotkey registry.
	[Inject]
	private IServiceProvider Services { get; set; } = default!;

	private IMokaHotkeyService? Hotkeys => Services.GetService<IMokaHotkeyService>();

	// The registry is read per render rather than cached: it changes as components come and go, and a
	// cheatsheet is built once per open.
	private IReadOnlyList<MokaCheatsheetGroup> DisplayedGroups =>
		Groups ?? (Hotkeys is null ? [] : FromRegistry(Hotkeys.Hotkeys, UngroupedTitle));

	/// <summary>
	///     Turns registered hotkeys into cheatsheet groups: one group per <see cref="MokaHotkey.Group" />, in the
	///     order the groups first appear, and one row per hotkey with its keys split into caps. Hotkeys with no
	///     description are left out, since a row with no text says nothing.
	/// </summary>
	/// <param name="hotkeys">The registered hotkeys.</param>
	/// <param name="ungroupedTitle">Heading for the hotkeys that name no group.</param>
	/// <returns>The groups, ready for <see cref="Groups" />.</returns>
	public static IReadOnlyList<MokaCheatsheetGroup> FromRegistry(
		IReadOnlyList<MokaHotkey> hotkeys,
		string ungroupedTitle = "General")
	{
		ArgumentNullException.ThrowIfNull(hotkeys);

		List<MokaCheatsheetGroup> groups = [];
		Dictionary<string, List<MokaCheatsheetItem>> rows = new(StringComparer.Ordinal);

		foreach (MokaHotkey hotkey in hotkeys)
		{
			if (string.IsNullOrWhiteSpace(hotkey.Description) || string.IsNullOrWhiteSpace(hotkey.Keys))
			{
				continue;
			}

			string title = string.IsNullOrWhiteSpace(hotkey.Group) ? ungroupedTitle : hotkey.Group;
			if (!rows.TryGetValue(title, out List<MokaCheatsheetItem>? items))
			{
				items = [];
				rows[title] = items;
				groups.Add(new MokaCheatsheetGroup(title, items));
			}

			items.Add(new MokaCheatsheetItem(hotkey.Description, ShortcutKeys.Split(hotkey.Keys)));
		}

		return groups;
	}

	/// <summary>Whether clicking the backdrop closes the overlay. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnBackdropClick { get; set; } = true;

	/// <summary>Whether pressing Escape closes the overlay. Defaults to true.</summary>
	[Parameter]
	public bool CloseOnEscape { get; set; } = true;

	/// <summary>Maximum modal width. Accepts any CSS width value. Defaults to "720px".</summary>
	[Parameter]
	public string MaxWidth { get; set; } = "720px";

	/// <summary>Optional hint line rendered under the shortcut grid.</summary>
	[Parameter]
	public RenderFragment? FooterContent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-cheatsheet";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("max-width", MaxWidth)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Open only seeds the state when the parent passes a new value. Copying it on every parent
		// render reopened an unbound cheatsheet the user had just closed.
		if (_lastOpen != Open)
		{
			_lastOpen = Open;
			_open = Open;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		await _focusTrap.SyncAsync(() => _open, _dialogRef);
		await SyncScrollLockAsync();
	}

	// The page behind the open cheatsheet stays still, like behind a dialog. The lock in
	// moka-dialog.js is counted and shared with dialogs, drawers and bottom sheets, so the
	// cheatsheet holds at most one and gives back only that one. The flag flips before the call,
	// so a render that finishes while a call is still out cannot send a second lock. Every close
	// renders or disposes, and both end here.
	private async Task SyncScrollLockAsync()
	{
		bool wanted = _open && !_disposed;
		if (wanted == _scrollLocked)
		{
			return;
		}

		_scrollLocked = wanted;
		await SafeModuleInvokeVoidAsync(MokaOverlayFocusTrap.Module, wanted ? "lockBodyScroll" : "unlockBodyScroll");
	}

	private async Task HandleBackdropClick()
	{
		if (CloseOnBackdropClick)
		{
			await CloseAsync();
		}
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (CloseOnEscape && e.Key == "Escape")
		{
			await CloseAsync();
		}
	}

	private async Task CloseAsync()
	{
		_open = false;

		if (OpenChanged.HasDelegate)
		{
			await OpenChanged.InvokeAsync(false);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		// Set first: a render still finishing must not take the lock again after it is given back.
		_disposed = true;
		await _focusTrap.ReleaseAsync();
		await SyncScrollLockAsync();
		await base.DisposeAsyncCore();
	}
}
