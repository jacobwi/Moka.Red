using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;

namespace Moka.Red.Data.Base;

/// <summary>
///     Abstract base class for data display components (Table, DataGrid, etc.).
///     Provides items collection management, loading/empty state,
///     and virtualization support.
/// </summary>
/// <typeparam name="TItem">The type of items displayed by the component.</typeparam>
public abstract class MokaDataComponentBase<TItem> : MokaVisualComponentBase
{
	/// <summary>The collection of items to display.</summary>
	[Parameter]
	public IReadOnlyList<TItem> Items { get; set; } = [];

	/// <summary>
	///     Whether the component is in a loading state.
	///     When true, a loading indicator is shown instead of data.
	/// </summary>
	[Parameter]
	public bool Loading { get; set; }

	/// <summary>
	///     Template to render when there are no items and the component is not loading.
	/// </summary>
	[Parameter]
	public RenderFragment? EmptyTemplate { get; set; }

	/// <summary>
	///     Template to render while the component is loading.
	/// </summary>
	[Parameter]
	public RenderFragment? LoadingTemplate { get; set; }

	/// <summary>
	///     Whether to enable virtualization for large data sets.
	///     When true, only visible items are rendered.
	///     Defaults to false.
	/// </summary>
	[Parameter]
	public bool Virtualize { get; set; }

	/// <summary>
	///     The fixed height of each item in pixels, used for virtualization calculations.
	///     Only applicable when <see cref="Virtualize" /> is true.
	///     Defaults to 48.
	/// </summary>
	[Parameter]
	public float ItemSize { get; set; } = 48f;

	/// <summary>
	///     The number of extra items to render outside the visible area
	///     when virtualization is enabled. Helps reduce flickering during fast scrolling.
	///     Defaults to 3.
	/// </summary>
	[Parameter]
	public int OverscanCount { get; set; } = 3;

	/// <summary>Whether the items collection is empty and not loading.</summary>
	protected bool IsEmpty => !Loading && Items.Count == 0;

	/// <summary>Whether the component has items to display.</summary>
	protected bool HasItems => Items.Count > 0;

	/// <summary>The total number of items.</summary>
	protected int ItemCount => Items.Count;
}
