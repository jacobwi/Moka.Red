using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;

namespace Moka.Red.ContextMenu;

/// <summary>
///     Renders the single shared context menu driven by <see cref="IMokaContextMenuService" />.
///     Place one instance in your root layout (alongside MokaToastHost / MokaDialogHost).
/// </summary>
public sealed partial class MokaContextMenuHost : IDisposable
{
	private bool _disposed;

	[Inject]
	private IMokaContextMenuService Service { get; set; } = default!;

	/// <summary>
	///     How the shared context menu's panel is drawn. Null takes the theme's popup look
	///     (<see cref="MokaTheme.WithPopups" />), which reaches every floating panel through the provider's root. A value
	///     here wins over the theme for this host's menus and their sub-menus. The radial menu has a look of its own,
	///     <see cref="RadialLook" />, and ignores it. Only the new design draws the looks: under <see cref="MokaTheme.Dark" /> and
	///     <see cref="MokaTheme.Light" /> the menu looks the same whatever this says.
	/// </summary>
	[Parameter]
	public MokaPopupLook? Look { get; set; }

	/// <summary>
	///     How the shared context menu and its sub-menus draw their rows' text (<see cref="MokaContextMenu.ItemStyle" />).
	///     <see cref="MokaContextMenuItemStyle.Standard" /> by default. New design only.
	/// </summary>
	[Parameter]
	public MokaContextMenuItemStyle ItemStyle { get; set; }

	/// <summary>
	///     Whether the shared menu that holds keyboard focus also draws the page's focus ring around itself
	///     (<see cref="MokaContextMenu.FocusRing" />). True by default; false leaves only the lit row showing it, so a menu
	///     and its sub-menu look alike.
	/// </summary>
	[Parameter]
	public bool FocusRing { get; set; } = true;

	/// <summary>
	///     How the buttons of every radial menu this host draws sit around their target, unless the menu's spec says
	///     (<see cref="MokaRadialMenuSpec.Shape" />). Null leaves it a <see cref="MokaRadialShape.Ring" />.
	/// </summary>
	[Parameter]
	public MokaRadialShape? RadialShape { get; set; }

	/// <summary>
	///     How every radial menu this host draws is drawn, unless the menu's spec says (<see cref="MokaRadialMenuSpec.Look" />).
	///     Null follows the theme's <see cref="MokaTheme.Popups" /> where a radial look has the same name.
	/// </summary>
	[Parameter]
	public MokaRadialLook? RadialLook { get; set; }

	/// <summary>
	///     How every radial menu this host draws shows a button's choices, unless the menu's spec says
	///     (<see cref="MokaRadialMenuSpec.ChoiceStyle" />). Null leaves them in a <see cref="MokaRadialChoiceStyle.List" />.
	/// </summary>
	[Parameter]
	public MokaRadialChoiceStyle? RadialChoiceStyle { get; set; }

	/// <summary>
	///     Whether a ring this host draws turns into a half ring or a corner fan near the window's edges, unless the
	///     menu's spec says (<see cref="MokaRadialMenuSpec.AdaptToEdges" />). Null leaves it off.
	/// </summary>
	[Parameter]
	public bool? RadialAdaptToEdges { get; set; }

	/// <summary>Service state changes outside the parameter flow, so always re-render.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		Service.OnChanged += HandleChanged;
		Service.RegisterHost();
	}

	private void HandleChanged() => _ = InvokeAsync(StateHasChanged);

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		Service.OnChanged -= HandleChanged;
		Service.UnregisterHost();
		GC.SuppressFinalize(this);
	}
}
