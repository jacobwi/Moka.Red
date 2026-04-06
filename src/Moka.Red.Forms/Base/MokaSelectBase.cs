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

	/// <summary>Callback invoked when the dropdown opens or closes.</summary>
	[Parameter]
	public EventCallback<bool> IsOpenChanged { get; set; }

	/// <summary>Whether the dropdown is currently open.</summary>
	protected bool IsOpen { get; private set; }

	/// <summary>
	///     Opens the dropdown. Does nothing if <see cref="Disabled" /> is true.
	/// </summary>
	protected async Task OpenAsync()
	{
		if (Disabled || IsOpen)
		{
			return;
		}

		IsOpen = true;
		await NotifyOpenStateChangedAsync();
	}

	/// <summary>
	///     Closes the dropdown.
	/// </summary>
	protected async Task CloseAsync()
	{
		if (!IsOpen)
		{
			return;
		}

		IsOpen = false;
		await NotifyOpenStateChangedAsync();
	}

	/// <summary>
	///     Toggles the dropdown open/close state.
	/// </summary>
	protected async Task ToggleAsync()
	{
		if (IsOpen)
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
			await IsOpenChanged.InvokeAsync(IsOpen);
		}
	}
}
