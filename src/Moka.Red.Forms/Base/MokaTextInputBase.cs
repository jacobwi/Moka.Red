using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;

namespace Moka.Red.Forms.Base;

/// <summary>
///     Abstract base class for text-based input components (TextField, etc.).
///     Provides shared behavior for placeholder, maxlength, readonly, disabled,
///     input type, focus management, and input event debouncing.
/// </summary>
/// <typeparam name="TValue">The type of the input value.</typeparam>
public abstract class MokaTextInputBase<TValue> : MokaVisualInputBase<TValue>
{
	private Timer? _debounceTimer;
	private string? _pendingValue;

	/// <summary>Placeholder text displayed when the input is empty.</summary>
	[Parameter]
	public string? Placeholder { get; set; }

	/// <summary>Maximum number of characters allowed in the input.</summary>
	[Parameter]
	public int? MaxLength { get; set; }

	/// <summary>Whether the input is read-only.</summary>
	[Parameter]
	public bool ReadOnly { get; set; }

	/// <summary>
	///     The HTML input type (e.g., "text", "password", "email").
	///     Defaults to "text".
	/// </summary>
	[Parameter]
	public string InputType { get; set; } = "text";

	/// <summary>
	///     Debounce delay in milliseconds for input events. Set to 0 to disable debouncing.
	///     Defaults to 0 (no debounce).
	/// </summary>
	[Parameter]
	public int DebounceDelay { get; set; }

	/// <summary>Callback invoked when the input receives focus.</summary>
	[Parameter]
	public EventCallback OnFocus { get; set; }

	/// <summary>Callback invoked when the input loses focus.</summary>
	[Parameter]
	public EventCallback OnBlur { get; set; }

	/// <summary>
	///     Handles the focus event by invoking the <see cref="OnFocus" /> callback.
	/// </summary>
	protected async Task HandleFocusAsync()
	{
		if (OnFocus.HasDelegate)
		{
			await OnFocus.InvokeAsync();
		}
	}

	/// <summary>
	///     Handles the blur event by invoking the <see cref="OnBlur" /> callback.
	/// </summary>
	protected async Task HandleBlurAsync()
	{
		if (OnBlur.HasDelegate)
		{
			await OnBlur.InvokeAsync();
		}
	}

	/// <summary>
	///     Handles an input event with optional debouncing. When <see cref="DebounceDelay" />
	///     is greater than zero, the value update is delayed until the user stops typing.
	/// </summary>
	/// <param name="value">The raw string value from the input event.</param>
	protected void HandleInputWithDebounce(string? value)
	{
		if (DebounceDelay <= 0)
		{
			OnDebouncedInput(value);
			return;
		}

		_pendingValue = value;
		_debounceTimer?.Dispose();
		_debounceTimer = new Timer(
			_ => InvokeAsync(() => OnDebouncedInput(_pendingValue)),
			null,
			DebounceDelay,
			Timeout.Infinite);
	}

	/// <summary>
	///     Called when a debounced input value is ready to be processed.
	///     Override this to parse and apply the value.
	/// </summary>
	/// <param name="value">The debounced string value.</param>
	protected virtual void OnDebouncedInput(string? value)
	{
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_debounceTimer is not null)
		{
			await _debounceTimer.DisposeAsync();
			_debounceTimer = null;
		}

		await base.DisposeAsyncCore();
	}
}
