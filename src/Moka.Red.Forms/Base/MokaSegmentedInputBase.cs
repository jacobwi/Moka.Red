using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;

namespace Moka.Red.Forms.Base;

/// <summary>
///     Abstract base class for segmented input components (OTP, PIN, IP address, MAC address).
///     Provides shared behavior for multiple connected input boxes with auto-tab,
///     keyboard navigation, paste distribution, and value synchronization.
/// </summary>
public abstract class MokaSegmentedInputBase : MokaVisualComponentBase
{
	private int _previousSegmentCount;

	/// <summary>The complete combined value. Two-way bindable.</summary>
	[Parameter]
	public string? Value { get; set; }

	/// <summary>Callback when the combined value changes.</summary>
	[Parameter]
	public EventCallback<string?> ValueChanged { get; set; }

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the input.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Error text displayed below the input when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the input has an error state.</summary>
	protected bool HasError => !string.IsNullOrEmpty(ErrorText);

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

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		int count = SegmentCount;
		if (count != _previousSegmentCount)
		{
			_previousSegmentCount = count;
			Segments = new string[count];
			InputRefs = new ElementReference[count];
		}

		SyncFromValue();
	}

	/// <summary>
	///     Splits the combined <see cref="Value" /> into individual segments.
	///     Default splits on <see cref="Separator" /> or distributes characters for empty separator.
	/// </summary>
	protected virtual void SyncFromValue()
	{
		string val = Value ?? "";

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

	/// <summary>Updates the combined value and invokes the callback.</summary>
	protected async Task UpdateValue()
	{
		string? newValue = CombineSegments();
		Value = newValue;

		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync(newValue);
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
				// Focus may fail during prerender
			}
		}
	}

	/// <summary>Checks whether a character is a valid hexadecimal digit (0-9, a-f, A-F).</summary>
	protected static bool IsHexChar(char c) =>
		char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

	/// <summary>Backing array for individual segment values.</summary>
#pragma warning disable CA1819 // Array property needed for Blazor @ref binding in .razor files
	protected string[] Segments { get; private set; } = [];

	/// <summary>Element references for each segment input, used for focus management.</summary>
	protected ElementReference[] InputRefs { get; private set; } = [];
#pragma warning restore CA1819
}
