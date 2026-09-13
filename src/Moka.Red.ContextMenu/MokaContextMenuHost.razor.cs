using Microsoft.AspNetCore.Components;

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
