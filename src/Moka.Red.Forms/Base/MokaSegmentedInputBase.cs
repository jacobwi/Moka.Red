using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.Base;

// The EditForm wiring mirrors MokaInputBase<TValue> instead of inheriting it. InputBase<TValue>
// models one input element: it puts aria-invalid into the attributes meant for that element, which
// here land on the group rather than the boxes, and it fires ValueChanged without awaiting it.
// Staying on MokaVisualComponentBase also keeps the protected API custom segmented inputs build on:
// virtual appearance parameters, CssClass and SafeJsInvokeAsync.

/// <summary>
///     Abstract base class for segmented input components (OTP, PIN, IP address, MAC address).
///     Provides shared behavior for multiple connected input boxes with auto-tab,
///     keyboard navigation, paste distribution, value synchronization, and
///     <see cref="EditContext" /> validation.
/// </summary>
public abstract class MokaSegmentedInputBase : MokaVisualComponentBase
{
	private const string SegmentsModule = "./_content/Moka.Red.Forms/moka-segments.js";

	private EditContext? _editContext;
	private FieldIdentifier? _fieldIdentifier;
	private bool _fieldResolved;
	private string? _generatedId;
	private int _previousSegmentCount;

	// The boxes follow _value, not Value: typing changes it before the parent answers, and a
	// parent that passes Value one way must not undo the typing by rendering again (gotcha #9).
	private string? _value;
	private string? _lastValueParameter;
	private bool _valueSeeded;

	/// <summary>The complete combined value. Two-way bindable.</summary>
	[Parameter]
	public string? Value { get; set; }

	/// <summary>Callback when the combined value changes.</summary>
	[Parameter]
	public EventCallback<string?> ValueChanged { get; set; }

	/// <summary>
	///     Names the bound field for validation. <c>@bind-Value</c> supplies it. Without one the input
	///     takes no part in the validation of a surrounding <c>EditForm</c>.
	/// </summary>
	[Parameter]
	public Expression<Func<string?>>? ValueExpression { get; set; }

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the input.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the input. Overrides any EditContext message.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>
	///     Shows a required marker after the label and sets <c>aria-required</c> on every box. The
	///     model's validation attributes still decide what is valid.
	/// </summary>
	[Parameter]
	public bool Required { get; set; }

	[CascadingParameter]
	private EditContext? CascadedEditContext { get; set; }

	/// <summary>
	///     Validation messages the cascaded <see cref="EditContext" /> holds for the bound field.
	///     Empty outside an <c>EditForm</c> or without a <see cref="ValueExpression" />.
	/// </summary>
	protected IEnumerable<string> ValidationMessages =>
		_editContext is not null && _fieldIdentifier is { } bound ? _editContext.GetValidationMessages(bound) : [];

	/// <summary>True when the cascaded <see cref="EditContext" /> reports a message for the bound field.</summary>
	protected bool HasValidationError => ValidationMessages.Any();

	/// <summary>First validation message for the bound field, or <c>null</c> when there is none.</summary>
	protected string? ValidationErrorText => ValidationMessages.FirstOrDefault();

	/// <summary>True when <see cref="ErrorText" /> is set or the EditContext reports a message.</summary>
	protected bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	protected string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	/// <summary>
	///     The framework's field classes (<c>modified</c>, <c>valid</c>, <c>invalid</c>) for the bound
	///     field. Empty outside an <c>EditForm</c>. Add it to the root element's classes.
	/// </summary>
	protected string ValidationCssClass =>
		_editContext is not null && _fieldIdentifier is { } bound ? _editContext.FieldCssClass(bound) : string.Empty;

	/// <summary>
	///     Base for the ids of the label and the message: <see cref="MokaComponentBase.Id" /> when set,
	///     otherwise one generated for this instance.
	/// </summary>
	protected string FieldId =>
		string.IsNullOrEmpty(Id) ? _generatedId ??= $"{RootClass}-{Guid.NewGuid():N}" : Id;

	/// <summary>Whether a label is rendered.</summary>
	protected bool HasLabel => !string.IsNullOrEmpty(Label);

	/// <summary>Id of the label, for the group's <c>aria-labelledby</c>. <c>null</c> without a label.</summary>
	protected string? LabelId => HasLabel ? $"{FieldId}-label" : null;

	/// <summary>The text below the boxes: the error while there is one, otherwise the helper text.</summary>
	protected string? MessageText => HasError ? ResolvedErrorText : HelperText;

	/// <summary>Whether the message element is rendered.</summary>
	protected bool HasMessage => !string.IsNullOrEmpty(MessageText);

	/// <summary>Id of the message element.</summary>
	protected string MessageId => $"{FieldId}-message";

	/// <summary><c>aria-describedby</c> for every box: the message element while it is rendered.</summary>
	protected string? DescribedBy => HasMessage ? MessageId : null;

	/// <summary><c>aria-invalid</c> for every box, as the string ARIA expects.</summary>
	protected string AriaInvalid => HasError ? "true" : "false";

	/// <summary><c>aria-required</c> for every box: <c>"true"</c> when required, otherwise left out.</summary>
	protected string? AriaRequired => Required ? "true" : null;

	/// <summary>The number of input segments. Override to configure (e.g., 6 for OTP, 4 for IPv4).</summary>
	protected abstract int SegmentCount { get; }

	/// <summary>Maximum character length per segment (e.g., 1 for OTP, 3 for IPv4 octet, 4 for IPv6 group).</summary>
	protected abstract int MaxSegmentLength { get; }

	/// <summary>
	///     The separator character between segments when combining/splitting values.
	///     Empty string for OTP/PIN (concatenated), "." for IPv4, ":" for IPv6/MAC.
	/// </summary>
	protected abstract string Separator { get; }

	/// <summary>HTML inputmode attribute value ("numeric", "text", etc.).</summary>
	protected abstract string InputMode { get; }

	/// <summary>Whether to automatically advance focus when a segment reaches <see cref="MaxSegmentLength" />.</summary>
	protected virtual bool AutoTabOnFull => true;

	/// <summary>
	///     A regular expression for one valid character, written to every box as
	///     <c>data-moka-accept</c> so the browser only moves on from a box that holds valid text.
	///     Null accepts anything. Keep it in line with <see cref="IsValidChar" />.
	/// </summary>
	protected virtual string? AcceptPattern => null;

	/// <summary>The separator for the root's <c>data-moka-separator</c>, or null when there is none.</summary>
	protected string? SeparatorAttribute => string.IsNullOrEmpty(Separator) ? null : Separator;

	/// <inheritdoc />
	/// <remarks>
	///     The margin and the padding go on the root, around the label, the boxes and the message.
	///     The radius goes on each box through <see cref="SegmentStyle" />, since every box draws its
	///     own border and the root draws none.
	/// </remarks>
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <summary>The inline style for every box: the radius from <c>Rounded</c> or <c>RoundedValue</c>.</summary>
	protected string? SegmentStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	/// <summary>The component's root element. Subclasses put <c>@ref="RootRef"</c> on it.</summary>
	protected ElementReference RootRef { get; set; }

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Focus moves between boxes in the browser as well: from .NET it waits for a round trip,
		// and on Blazor Server a fast typist's next key would hit the full box and be dropped.
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(SegmentsModule, "bindSegments", RootRef);
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		AttachEditContext();

		int count = SegmentCount;
		bool resized = count != _previousSegmentCount;
		if (resized)
		{
			_previousSegmentCount = count;
			Segments = new string[count];
			InputRefs = new ElementReference[count];
		}

		if (!_valueSeeded || !string.Equals(Value, _lastValueParameter, StringComparison.Ordinal))
		{
			_valueSeeded = true;
			_lastValueParameter = Value;
			_value = Value;
			SyncFromValue();
		}
		else if (resized)
		{
			SyncFromValue();
		}
	}

	/// <summary>
	///     The value the boxes show: the last one typed, or the last new <see cref="Value" /> the
	///     parent passed.
	/// </summary>
	protected string? CurrentValue => _value;

	/// <summary>
	///     Splits <see cref="CurrentValue" /> into individual segments.
	///     Default splits on <see cref="Separator" /> or distributes characters for empty separator.
	/// </summary>
	protected virtual void SyncFromValue()
	{
		string val = _value ?? "";

		if (string.IsNullOrEmpty(Separator))
		{
			// Character-per-segment (OTP, PIN)
			for (int i = 0; i < SegmentCount; i++)
			{
				Segments[i] = i < val.Length ? val[i].ToString() : "";
			}
		}
		else
		{
			// Separator-delimited (IP, MAC)
			if (string.IsNullOrEmpty(val))
			{
				for (int i = 0; i < SegmentCount; i++)
				{
					Segments[i] = "";
				}
			}
			else
			{
				string[] parts = val.Split(Separator);
				for (int i = 0; i < SegmentCount; i++)
				{
					Segments[i] = i < parts.Length ? parts[i] : "";
				}
			}
		}
	}

	/// <summary>
	///     Combines segments into the final value string.
	///     Default concatenates for empty separator, or joins with <see cref="Separator" />.
	/// </summary>
	protected virtual string? CombineSegments()
	{
		if (string.IsNullOrEmpty(Separator))
		{
			return string.Join("", Segments);
		}

		bool allEmpty = Array.TrueForAll(Segments, string.IsNullOrEmpty);
		return allEmpty ? null : string.Join(Separator, Segments);
	}

	/// <summary>
	///     Validates and filters a raw input character. Return true if the character is allowed.
	/// </summary>
	protected abstract bool IsValidChar(char c);

	/// <summary>
	///     Optional post-filter validation on a segment value (e.g., IPv4 range clamping to 0-255).
	///     Default returns the value unchanged.
	/// </summary>
	protected virtual string ClampSegment(int index, string value) => value ?? "";

	/// <summary>Handles input on a specific segment index.</summary>
	protected async Task HandleSegmentInput(int index, ChangeEventArgs? e)
	{
		string raw = e?.Value?.ToString() ?? "";

		// Handle paste or multi-char input
		if (raw.Length > MaxSegmentLength && string.IsNullOrEmpty(Separator))
		{
			// Distribute across segments (OTP/PIN style)
			string filtered = new(raw.Where(IsValidChar).ToArray());
			for (int i = 0; i < filtered.Length && index + i < SegmentCount; i++)
			{
				Segments[index + i] = filtered[i].ToString();
			}

			await UpdateValue();
			int nextIndex = Math.Min(index + filtered.Length, SegmentCount - 1);
			await FocusSegment(nextIndex);
			return;
		}

		// Filter to valid chars and clamp length
		string value = new(raw.Where(IsValidChar).ToArray());
		if (value.Length > MaxSegmentLength)
		{
			value = value[..MaxSegmentLength];
		}

		value = ClampSegment(index, value);
		if (!string.Equals(value, raw, StringComparison.Ordinal) &&
		    string.Equals(value, Segments[index], StringComparison.Ordinal))
		{
			// The box shows what was typed, but the last render already holds the filtered value,
			// so Blazor sees nothing to change and leaves the rejected characters (a separator, a
			// letter in a digit box) on screen, where they also count against maxlength. One
			// render with the raw text gives the next render a value to replace.
			Segments[index] = raw;
			StateHasChanged();
			await Task.Yield();
			if (!string.Equals(Segments[index], raw, StringComparison.Ordinal))
			{
				// A later input to this box got there first.
				return;
			}
		}

		Segments[index] = value;
		await UpdateValue();

		// Auto-tab when segment is full
		if (AutoTabOnFull && value.Length == MaxSegmentLength && index < SegmentCount - 1)
		{
			await FocusSegment(index + 1);
		}
	}

	/// <summary>Handles keyboard navigation between segments.</summary>
	protected async Task HandleSegmentKeyDown(int index, KeyboardEventArgs? e)
	{
		if (e is null)
		{
			return;
		}

		// Separator key advances to next segment (for IP/MAC style inputs)
		if (!string.IsNullOrEmpty(Separator) && e.Key == Separator && index < SegmentCount - 1)
		{
			await FocusSegment(index + 1);
			return;
		}

		// Backspace on empty segment goes to previous
		if (e.Key == "Backspace" && string.IsNullOrEmpty(Segments[index]) && index > 0)
		{
			Segments[index - 1] = "";
			await UpdateValue();
			await FocusSegment(index - 1);
			return;
		}

		// Arrow key navigation
		if (e.Key == "ArrowLeft" && index > 0)
		{
			await FocusSegment(index - 1);
		}
		else if (e.Key == "ArrowRight" && index < SegmentCount - 1)
		{
			await FocusSegment(index + 1);
		}
	}

	/// <summary>Handles paste events by distributing across segments.</summary>
	protected async Task HandleSegmentPaste(int index, ClipboardEventArgs e)
	{
		string? clipboardText = await SafeJsInvokeAsync<string>("navigator.clipboard.readText");

		if (string.IsNullOrEmpty(clipboardText))
		{
			return;
		}

		if (!string.IsNullOrEmpty(Separator))
		{
			// Split pasted text on separator and distribute
			string[] parts = clipboardText.Split(Separator);
			for (int i = 0; i < parts.Length && index + i < SegmentCount; i++)
			{
				string part = new(parts[i].Trim().Where(IsValidChar).ToArray());
				if (part.Length > MaxSegmentLength)
				{
					part = part[..MaxSegmentLength];
				}

				part = ClampSegment(index + i, part);
				Segments[index + i] = part;
			}

			await UpdateValue();
			int nextIndex = Math.Min(index + parts.Length, SegmentCount - 1);
			await FocusSegment(nextIndex);
		}
		else
		{
			// Distribute character-by-character
			string filtered = new(clipboardText.Where(IsValidChar).ToArray());
			for (int i = 0; i < filtered.Length && index + i < SegmentCount; i++)
			{
				Segments[index + i] = filtered[i].ToString();
			}

			await UpdateValue();
			int nextIndex = Math.Min(index + filtered.Length, SegmentCount - 1);
			await FocusSegment(nextIndex);
		}
	}

	/// <summary>
	///     Updates the combined value, invokes the callback, and tells the cascaded
	///     <see cref="EditContext" /> when the value changed.
	/// </summary>
	protected async Task UpdateValue()
	{
		string? newValue = CombineSegments();
		// Cleared OTP and PIN inputs give "" while IP and MAC give null. Both mean empty.
		bool changed = !string.Equals(newValue ?? "", _value ?? "", StringComparison.Ordinal);
		_value = newValue;

		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync(newValue);
		}

		// After the callback, so the validator reads a model that already holds the new value.
		if (changed && _editContext is not null && _fieldIdentifier is { } bound)
		{
			_editContext.NotifyFieldChanged(bound);
		}

		await OnValueUpdated(newValue);
	}

	/// <summary>
	///     Called after the value has been updated. Override to add completion callbacks etc.
	/// </summary>
	protected virtual Task OnValueUpdated(string? newValue) => Task.CompletedTask;

	/// <summary>Moves focus to the specified segment input.</summary>
	protected async Task FocusSegment(int index)
	{
		if (index >= 0 && index < SegmentCount)
		{
			try
			{
				await InputRefs[index].FocusAsync();
			}
			catch (JSDisconnectedException)
			{
				// The circuit is gone.
			}
			catch (InvalidOperationException)
			{
				// Prerendering, or the box is no longer on the page.
			}
		}
	}

	/// <summary>Checks whether a character is a valid hexadecimal digit (0-9, a-f, A-F).</summary>
	protected static bool IsHexChar(char c) =>
		char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

	/// <inheritdoc />
	protected override ValueTask DisposeAsyncCore()
	{
		DetachEditContext();
		return base.DisposeAsyncCore();
	}

	private void AttachEditContext()
	{
		if (!ReferenceEquals(CascadedEditContext, _editContext))
		{
			DetachEditContext();
			_editContext = CascadedEditContext;
			_fieldIdentifier = null;
			_fieldResolved = false;

			if (_editContext is not null)
			{
				_editContext.OnValidationStateChanged += HandleValidationStateChanged;
			}
		}

		// Parsed once per EditContext, as InputBase does: bind passes a new lambda on every render,
		// but it names the same field.
		if (_editContext is null || _fieldResolved || ValueExpression is null)
		{
			return;
		}

		_fieldResolved = true;
		try
		{
			_fieldIdentifier = FieldIdentifier.Create(ValueExpression);
		}
		catch (ArgumentException)
		{
			// A getter that is not a field or property (bind:get calling a method) names no field.
			// Before ValueExpression existed such a binding worked, so it stays out of validation
			// rather than throwing.
		}
	}

	private void DetachEditContext()
	{
		if (_editContext is not null)
		{
			_editContext.OnValidationStateChanged -= HandleValidationStateChanged;
			_editContext = null;
		}
	}

	// A submit or another field's change can set this field's messages, so redraw on every change.
	// InvokeAsync because consumer code may raise it off the renderer's thread.
	private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) =>
		_ = InvokeAsync(StateHasChanged);

	/// <summary>Backing array for individual segment values.</summary>
#pragma warning disable CA1819 // Array property needed for Blazor @ref binding in .razor files
	protected string[] Segments { get; private set; } = [];

	/// <summary>Element references for each segment input, used for focus management.</summary>
	protected ElementReference[] InputRefs { get; private set; } = [];
#pragma warning restore CA1819
}
