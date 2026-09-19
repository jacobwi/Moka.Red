using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Sidebar;

/// <summary>
///     A vertical sidebar navigation panel with collapsible, overlay, and mini modes.
///     Provides header, body (scrollable), and footer sections.
/// </summary>
public partial class MokaSidebar
{
	private const string ModulePath = "./_content/Moka.Red.Navigation/Sidebar/MokaSidebar.razor.js";

	private bool _disposed;
	private bool? _lastOpen;
	private ElementReference _navRef;
	private bool _open = true;

	// What the script was last told, so it hears each open and close of the overlay once.
	private bool _overlayShown;
	private bool _scrollLocked;

	/// <summary>Main body content (menu items, links, custom content).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Top area content (logo, app name).</summary>
	[Parameter]
	public RenderFragment? Header { get; set; }

	/// <summary>Bottom area content (user info, settings).</summary>
	[Parameter]
	public RenderFragment? Footer { get; set; }

	/// <summary>
	///     Whether the sidebar is open. Two-way bindable. Default true. An overlay sidebar closes itself
	///     on Escape and on a backdrop click, and reports it through <see cref="OpenChanged" />.
	/// </summary>
	[Parameter]
	public bool Open { get; set; } = true;

	/// <summary>Callback when <see cref="Open" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> OpenChanged { get; set; }

	/// <summary>
	///     Mini-sidebar mode (icons only). Default false. The sidebar has no collapse control of its own,
	///     so the parent sets this.
	/// </summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>Full width when expanded. Default "240px".</summary>
	[Parameter]
	public string Width { get; set; } = "240px";

	/// <summary>Width when collapsed (mini mode). Default "56px".</summary>
	[Parameter]
	public string CollapsedWidth { get; set; } = "56px";

	/// <summary>
	///     When true, sidebar overlays content (mobile mode). Default false. While open it takes focus,
	///     keeps Tab inside it and closes on Escape, like a modal dialog.
	/// </summary>
	[Parameter]
	public bool Overlay { get; set; }

	/// <summary>Whether to show a right border. Default true.</summary>
	[Parameter]
	public bool Bordered { get; set; } = true;

	/// <summary>Whether to apply elevation shadow. Default false.</summary>
	[Parameter]
	public bool Elevated { get; set; }

	/// <summary>Position of the sidebar. Default Left.</summary>
	[Parameter]
	public MokaSidebarPosition Position { get; set; } = MokaSidebarPosition.Left;

	/// <inheritdoc />
	protected override string RootClass => "moka-sidebar";

	private string ResolvedWidth => Collapsed ? CollapsedWidth : Width;

	// An open overlay covers the page on a backdrop, so it behaves like a modal dialog.
	private bool OverlayOpen => Overlay && _open;

	// Closed, or collapsed to a rail with no width: the links are still in the page, but unseen.
	private bool OutOfView => !_open || (Collapsed && IsZeroLength(CollapsedWidth));

	private EventCallback<KeyboardEventArgs> KeyDownHandler => OverlayOpen
		? EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown)
		: default;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-height", !Overlay)
		.AddClass("moka-sidebar--open", _open)
		.AddClass("moka-sidebar--closed", !_open)
		.AddClass("moka-sidebar--collapsed", Collapsed)
		.AddClass("moka-sidebar--overlay", Overlay)
		.AddClass("moka-sidebar--bordered", Bordered)
		.AddClass("moka-sidebar--elevated", Elevated)
		.AddClass($"moka-sidebar--{MokaEnumHelpers.ToCssClass(Position)}")
		.AddClass(Class)
		.Build();

	// Closed, the sidebar drops its margin and padding. At width 0 the padding would still show as a
	// strip and the margin as a gap, and an overlay slid out by its own width would leave its margin
	// on screen.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin, _open)
		.AddStyle("padding", ResolvedPadding, _open)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("width", ResolvedWidth)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Open only seeds the state when the parent passes a new value. Copying it on every parent
		// render reopened an unbound overlay sidebar the user had just dismissed.
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
		await SyncOverlayAsync();
		await SyncScrollLockAsync();
	}

	private async Task SyncOverlayAsync()
	{
		// Opening moves focus in and holds Tab, closing hands focus back. Both need the rendered
		// state: an open sidebar that is no longer inert, a closed one that is.
		bool shown = OverlayOpen;
		if (shown == _overlayShown)
		{
			return;
		}

		_overlayShown = shown;
		if (shown)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "openOverlay", _navRef);
		}
		else
		{
			// Overlay turned off while open, as in a layout that goes inline on wide screens: the
			// sidebar is still on screen, so focus stays where it is.
			await SafeModuleInvokeVoidAsync(ModulePath, "closeOverlay", _navRef, !_open);
		}
	}

	// The page behind an open overlay sidebar stays still, as behind a drawer. The lock is Core's
	// counted one, shared with dialogs and drawers, so the sidebar gives back only its own. The
	// flag flips before the call, so a render that finishes while a call is out cannot lock twice.
	// Kept apart from the focus calls: a sidebar removed while open is no longer in the page for
	// the script to find, but its lock still has to be given back.
	private async Task SyncScrollLockAsync()
	{
		bool wanted = OverlayOpen && !_disposed;
		if (wanted == _scrollLocked)
		{
			return;
		}

		_scrollLocked = wanted;
		await SafeModuleInvokeVoidAsync(ModulePath, wanted ? "lockBodyScroll" : "unlockBodyScroll");
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		// Set first: a render still finishing must not take the lock again after it is given back.
		_disposed = true;
		await SyncScrollLockAsync();
		await base.DisposeAsyncCore();
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Escape")
		{
			await CloseOverlayAsync();
		}
	}

	private Task HandleBackdropClick() => CloseOverlayAsync();

	private async Task CloseOverlayAsync()
	{
		if (!OverlayOpen)
		{
			return;
		}

		_open = false;
		await OpenChanged.InvokeAsync(false);
	}

	// "0", "0px", "0rem" and the like. Anything else, calc() included, counts as a width that shows.
	private static bool IsZeroLength(string? length)
	{
		ReadOnlySpan<char> value = length.AsSpan().Trim();
		int end = value.Length;
		while (end > 0 && (char.IsAsciiLetter(value[end - 1]) || value[end - 1] == '%'))
		{
			end--;
		}

		return end > 0
		       && double.TryParse(value[..end], NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
		       && number == 0;
	}
}
