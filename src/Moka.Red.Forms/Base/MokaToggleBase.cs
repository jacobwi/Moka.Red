using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;

namespace Moka.Red.Forms.Base;

/// <summary>
///     Abstract base class for toggle-style input components (Checkbox, Switch).
///     Provides checked/unchecked state management and label positioning.
/// </summary>
public abstract class MokaToggleBase : MokaVisualInputBase<bool>
{
	/// <summary>
	///     Where the label is displayed relative to the toggle control.
	/// </summary>
	public enum LabelPosition
	{
		/// <summary>Label appears after the toggle control.</summary>
		After,

		/// <summary>Label appears before the toggle control.</summary>
		Before
	}

	/// <summary>The text label for the toggle.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     Controls whether the label appears before or after the toggle control.
	///     Defaults to <see cref="LabelPosition.After" />.
	/// </summary>
	[Parameter]
	public LabelPosition LabelPlacement { get; set; } = LabelPosition.After;

	/// <summary>
	///     Marks the toggle busy, such as while its new value saves to a server. A spinner shows in the knob or the box,
	///     the input carries <c>aria-busy</c> and keeps focus, and a click, a click on the label or Space doesn't change it
	///     again until it's done: the browser's own toggle is cancelled, so the box never flips and no change is raised.
	///     That takes an interactive render; a static page can't stop the click.
	/// </summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>
	///     Toggles the checked state. Does nothing if <see cref="MokaVisualInputBase{TValue}.Disabled" /> is true.
	/// </summary>
	/// <remarks>
	///     A change that arrives while <see cref="Loading" /> is still taken: the browser has already flipped the box by
	///     then, because the click came before the busy state reached it, and refusing would leave the box and the value
	///     disagreeing.
	/// </remarks>
	protected void Toggle()
	{
		if (Disabled)
		{
			return;
		}

		CurrentValue = !CurrentValue;
	}

	/// <inheritdoc />
	protected override bool TryParseValueFromString(
		string? value,
		out bool result,
		out string validationErrorMessage)
	{
		if (bool.TryParse(value, out result))
		{
			validationErrorMessage = string.Empty;
			return true;
		}

		result = false;
		validationErrorMessage = $"The value '{value}' is not a valid boolean.";
		return false;
	}
}
