using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.SelectionBar;

/// <summary>
///     A floating bulk-actions bar that appears when a selection set is non-empty.
///     Shows the selected count, a slot for action buttons, and an optional deselect button.
///     While <see cref="Count" /> is zero only a hidden live region is rendered, which announces
///     the count to screen readers when a selection starts or changes.
///     By default the bar sits sticky at the bottom of its scroll container;
///     set <see cref="Fixed" /> to pin it bottom-centered on the viewport instead.
/// </summary>
public partial class MokaSelectionBar : MokaVisualComponentBase
{
	/// <summary>Number of selected items. The bar shows only when greater than zero.</summary>
	[Parameter]
	public int Count { get; set; }

	/// <summary>
	///     Text shown after the count, also used as the toolbar's accessible label and read out with
	///     the count, as in "3 files selected". Defaults to "selected".
	/// </summary>
	[Parameter]
	public string Label { get; set; } = "selected";

	/// <summary>The action buttons area, rendered between the count and the clear button.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether to show the trailing deselect button. Defaults to true.</summary>
	[Parameter]
	public bool ShowClear { get; set; } = true;

	/// <summary>Text and accessible label of the clear button. Defaults to "Deselect".</summary>
	[Parameter]
	public string ClearLabel { get; set; } = "Deselect";

	/// <summary>Raised when the clear button is clicked.</summary>
	[Parameter]
	public EventCallback OnClear { get; set; }

	/// <summary>
	///     When false (default) the bar is position: sticky at the bottom of its container.
	///     When true it is position: fixed, bottom-centered on the viewport.
	/// </summary>
	[Parameter]
	public bool Fixed { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-selection-bar";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-selection-bar--fixed", Fixed)
		.AddClass(Class)
		.Build();

	private string CountText => Count.ToString(CultureInfo.InvariantCulture);

	// Empty while nothing is selected, so clearing the selection is not read out as "0 selected".
	private string Announcement => Count > 0 ? $"{CountText} {Label}" : string.Empty;

	private async Task HandleClearAsync()
	{
		if (OnClear.HasDelegate)
		{
			await OnClear.InvokeAsync();
		}
	}
}
