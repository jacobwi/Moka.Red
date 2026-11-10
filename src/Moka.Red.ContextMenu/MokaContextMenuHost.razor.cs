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
	///     here wins over the theme for this host's menus and their sub-menus. The radial menu has a look of its own and
	///     ignores it. Only the new design draws the looks: under <see cref="MokaTheme.Dark" /> and
	///     <see cref="MokaTheme.Light" /> the menu looks the same whatever this says.
	/// </summary>
	[Parameter]
	public MokaPopupLook? Look { get; set; }

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
