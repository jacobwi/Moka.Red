using Microsoft.AspNetCore.Components;

namespace Moka.Red.Feedback.Internal;

/// <summary>
///     Moves focus into a modal overlay while it is open, keeps Tab inside it, and hands focus back
///     when it closes, through moka-dialog.js. Without it an overlay's Escape handler hears nothing
///     until the user clicks into the overlay. The owner passes in its own safe module calls, so
///     prerendering and a lost circuit are handled the way the rest of the component handles them.
/// </summary>
internal sealed class MokaOverlayFocusTrap(
	Func<ElementReference, ValueTask<int>> trapFocus,
	Func<int, ValueTask> releaseFocus)
{
	/// <summary>The module that holds <c>trapFocus</c> and <c>releaseFocus</c>.</summary>
	internal const string Module = "./_content/Moka.Red.Feedback/moka-dialog.js";

	private int? _handle;
	private bool _pending;

	/// <summary>Traps or releases focus to match the overlay. Call after every render.</summary>
	/// <param name="wanted">
	///     Whether the overlay is open and modal. Read again once the trap is set up, because the
	///     overlay can close while the module loads.
	/// </param>
	/// <param name="element">The overlay's dialog element.</param>
	internal async Task SyncAsync(Func<bool> wanted, ElementReference element)
	{
		if (wanted() && _handle is null && !_pending)
		{
			// Another render can finish before the import does, and must not set up a second trap.
			_pending = true;
			try
			{
				int handle = await trapFocus(element);
				if (handle > 0)
				{
					_handle = handle;
				}
			}
			finally
			{
				_pending = false;
			}

			if (!wanted())
			{
				await ReleaseAsync();
			}
		}
		else if (!wanted() && _handle is not null)
		{
			await ReleaseAsync();
		}
	}

	/// <summary>Removes the trap and returns focus to where it was before the overlay opened.</summary>
	internal async Task ReleaseAsync()
	{
		if (_handle is not int handle)
		{
			return;
		}

		_handle = null;
		await releaseFocus(handle);
	}
}
