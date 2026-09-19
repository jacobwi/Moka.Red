using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;

namespace Moka.Red.Forms.Base;

/// <summary>
///     Abstract base class for select/dropdown input components.
///     Provides items collection, selected item tracking, and dropdown open/close state.
/// </summary>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
public abstract class MokaSelectBase<TValue> : MokaVisualInputBase<TValue>
{
	/// <summary>The collection of items available for selection.</summary>
	[Parameter]
	public IReadOnlyList<TValue> Items { get; set; } = [];

	/// <summary>Placeholder text displayed when no item is selected.</summary>
	[Parameter]
	public string? Placeholder { get; set; }

	/// <summary>
	///     Template for rendering each item in the dropdown list.
	///     When null, <see cref="object.ToString" /> is used.
	/// </summary>
	[Parameter]
	public RenderFragment<TValue>? ItemTemplate { get; set; }

	/// <summary>
	///     Whether the dropdown is open. Two-way bindable with <see cref="IsOpenChanged" />. The
	///     dropdown still opens and closes itself; a value from the parent only takes effect when it
	///     differs from the last one the parent passed.
	/// </summary>
	[Parameter]
	public bool IsOpen { get; set; }

	/// <summary>Callback invoked when the dropdown opens or closes.</summary>
	[Parameter]
	public EventCallback<bool> IsOpenChanged { get; set; }

	/// <summary>Whether the dropdown is open right now.</summary>
	protected bool DropdownOpen { get; private set; }

	private bool _lastIsOpenParameter;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Before, IsOpen was not a parameter at all, so a dropdown could not be opened from markup
		// even though IsOpenChanged was. A re-render passing the old value must not undo what the
		// user did (gotcha #9).
		if (IsOpen != _lastIsOpenParameter)
		{
			_lastIsOpenParameter = IsOpen;
			DropdownOpen = IsOpen && !Disabled;
		}
	}

	/// <summary>
	///     Opens the dropdown. Does nothing if <see cref="Disabled" /> is true.
	/// </summary>
	protected async Task OpenAsync()
	{
		if (Disabled || DropdownOpen)
		{
			return;
		}

		DropdownOpen = true;
		await NotifyOpenStateChangedAsync();
	}

	/// <summary>
	///     Closes the dropdown.
	/// </summary>
	protected async Task CloseAsync()
	{
		if (!DropdownOpen)
		{
			return;
		}

		DropdownOpen = false;
		await NotifyOpenStateChangedAsync();
	}

	/// <summary>
	///     Toggles the dropdown open/close state.
	/// </summary>
	protected async Task ToggleAsync()
	{
		if (DropdownOpen)
		{
			await CloseAsync();
		}
		else
		{
			await OpenAsync();
		}
	}

	/// <summary>
	///     Selects an item and closes the dropdown.
	/// </summary>
	/// <param name="item">The item to select.</param>
	protected async Task SelectItemAsync(TValue item)
	{
		CurrentValue = item;
		await CloseAsync();
	}

	private async Task NotifyOpenStateChangedAsync()
	{
		if (IsOpenChanged.HasDelegate)
		{
			await IsOpenChanged.InvokeAsync(DropdownOpen);
		}
	}
}
