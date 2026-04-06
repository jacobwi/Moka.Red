using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;

namespace Moka.Red.Feedback.Base;

/// <summary>
///     Abstract base class for popup/overlay components (Dialog, Tooltip, Popover, Snackbar).
///     Provides open/close state management, anchor element tracking,
///     click-outside-to-close support, and focus trap support.
/// </summary>
public abstract class MokaPopupBase : MokaVisualComponentBase
{
	private bool _isOpen;

	/// <summary>Whether the popup is currently visible.</summary>
	[Parameter]
	public bool IsOpen { get; set; }

	/// <summary>Callback invoked when the open state changes.</summary>
	[Parameter]
	public EventCallback<bool> IsOpenChanged { get; set; }

	/// <summary>
	///     Whether clicking outside the popup should close it.
	///     Defaults to true.
	/// </summary>
	[Parameter]
	public bool CloseOnClickOutside { get; set; } = true;

	/// <summary>
	///     Whether the popup should trap focus within itself when open.
	///     Defaults to false. Dialogs typically set this to true.
	/// </summary>
	[Parameter]
	public bool TrapFocus { get; set; }

	/// <summary>
	///     Reference to the anchor element that the popup is positioned relative to.
	///     Not all popups require an anchor (e.g., Dialogs are centered).
	/// </summary>
	protected ElementReference AnchorElement { get; set; }

	/// <summary>
	///     The preferred position of the popup relative to its anchor element.
	/// </summary>
	protected virtual PopupPosition Position { get; set; } = PopupPosition.Bottom;

	/// <summary>Popups have internal open/close state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (_isOpen != IsOpen)
		{
			_isOpen = IsOpen;
			OnOpenStateChanged(_isOpen);
		}
	}

	/// <summary>
	///     Opens the popup.
	/// </summary>
	protected async Task OpenAsync()
	{
		if (_isOpen)
		{
			return;
		}

		_isOpen = true;
		IsOpen = true;
		OnOpenStateChanged(true);
		await NotifyOpenStateChangedAsync(true);
	}

	/// <summary>
	///     Closes the popup.
	/// </summary>
	protected async Task CloseAsync()
	{
		if (!_isOpen)
		{
			return;
		}

		_isOpen = false;
		IsOpen = false;
		OnOpenStateChanged(false);
		await NotifyOpenStateChangedAsync(false);
	}

	/// <summary>
	///     Handles a click outside the popup. Closes the popup if
	///     <see cref="CloseOnClickOutside" /> is true.
	/// </summary>
	protected async Task HandleClickOutsideAsync()
	{
		if (CloseOnClickOutside)
		{
			await CloseAsync();
		}
	}

	/// <summary>
	///     Called when the open state changes. Override to perform
	///     setup/teardown when the popup opens or closes.
	/// </summary>
	/// <param name="isOpen">The new open state.</param>
	protected virtual void OnOpenStateChanged(bool isOpen)
	{
	}

	private async Task NotifyOpenStateChangedAsync(bool isOpen)
	{
		if (IsOpenChanged.HasDelegate)
		{
			await IsOpenChanged.InvokeAsync(isOpen);
		}
	}
}

/// <summary>
///     The preferred position of a popup relative to its anchor element.
/// </summary>
public enum PopupPosition
{
	/// <summary>Above the anchor element.</summary>
	Top,

	/// <summary>Below the anchor element.</summary>
	Bottom,

	/// <summary>To the left of the anchor element.</summary>
	Start,

	/// <summary>To the right of the anchor element.</summary>
	End,

	/// <summary>Above and to the left of the anchor element.</summary>
	TopStart,

	/// <summary>Above and to the right of the anchor element.</summary>
	TopEnd,

	/// <summary>Below and to the left of the anchor element.</summary>
	BottomStart,

	/// <summary>Below and to the right of the anchor element.</summary>
	BottomEnd
}
