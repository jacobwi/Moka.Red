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
	private bool _hasTypedText;
	private string? _pendingValue;
	private string? _typedText;
	private TValue? _valueWhenTyped;

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
	///     Defaults to 0 (no debounce). <c>MokaCurrencyInput</c> and <c>MokaPhoneInput</c> report their
	///     value on blur, so they have nothing to delay.
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
	///     The text the input shows: what was typed last, until the value changes another way (the parent sets a
	///     new one, the field is cleared). Bind the input with <c>@bind:get="InputText"</c> and
	///     <c>@bind:set="HandleInputWithDebounce"</c>, never <c>value</c> with <c>@oninput</c>: only <c>@bind</c> tells
	///     Blazor what the browser holds, so a render racing fast typing can't put back an older value. Text that
	///     doesn't parse yet, such as "1." in a number field, stays as typed.
	/// </summary>
	protected string? InputText =>
		_hasTypedText && EqualityComparer<TValue>.Default.Equals(CurrentValue, _valueWhenTyped)
			? _typedText
			: CurrentValueAsString;

	/// <summary>
	///     Makes the input show the formatted value again, for a component that tidies the text, for example on blur.
	/// </summary>
	protected void ShowValueText() => _hasTypedText = false;

	/// <summary>
	///     Handles an input event with optional debouncing. When <see cref="DebounceDelay" />
	///     is greater than zero, the value update is delayed until the user stops typing.
	/// </summary>
	/// <param name="value">The raw string value from the input event.</param>
	protected void HandleInputWithDebounce(string? value)
	{
		_typedText = value;
		_hasTypedText = true;

		if (DebounceDelay <= 0)
		{
			OnDebouncedInput(value);
			_valueWhenTyped = CurrentValue;
			return;
		}

		_valueWhenTyped = CurrentValue;
		_pendingValue = value;
		_debounceTimer?.Dispose();
		_debounceTimer = new Timer(
			_ => RunDetached(() =>
			{
				OnDebouncedInput(_pendingValue);
				_valueWhenTyped = CurrentValue;
				StateHasChanged();
			}),
			null,
			DebounceDelay,
			Timeout.Infinite);
	}

	/// <summary>
	///     Called when a debounced input value is ready to be processed. Parses and applies the value
	///     through <see cref="Microsoft.AspNetCore.Components.Forms.InputBase{TValue}.CurrentValueAsString" />.
	/// </summary>
	/// <param name="value">The debounced string value.</param>
	protected virtual void OnDebouncedInput(string? value) => CurrentValueAsString = value;

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
