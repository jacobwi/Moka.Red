using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Data.VirtualList;

/// <summary>
///     Virtualized list for rendering large datasets efficiently. Only renders visible items
///     using Blazor's built-in <c>Virtualize</c> component internally.
/// </summary>
/// <remarks>
///     With <see cref="OnItemClick" /> the list is a WAI-ARIA listbox with a single tab stop. Up
///     and Down Arrow, Page Up, Page Down, Home and End move the active item and scroll it into
///     view, and Enter or Space raise <see cref="OnItemClick" /> for it. Give the list a name with
///     an <c>aria-label</c> attribute, and keep buttons and links out of <see cref="ItemTemplate" />:
///     the content of a listbox option is read as plain text.
/// </remarks>
/// <typeparam name="TItem">The type of items in the list.</typeparam>
public partial class MokaVirtualList<TItem> : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Data/VirtualList/MokaVirtualList.razor.js";

	private readonly string _idPrefix = $"moka-virtual-list-{Guid.NewGuid():N}";
	private int _activeIndex = -1;
	private bool _bound;
	private ElementReference _container;
	private DotNetObjectReference<MokaVirtualList<TItem>>? _dotNetRef;
	private MokaVirtualListRows<TItem> _rows = new([]);

	/// <summary>The full dataset to display. Required.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<TItem> Items { get; set; } = [];

	/// <summary>Template for rendering each item. Required.</summary>
	[Parameter]
	[EditorRequired]
	public RenderFragment<TItem> ItemTemplate { get; set; } = default!;

	/// <summary>Fixed height of each item in pixels. Required for virtualization calculations.</summary>
	[Parameter]
	[EditorRequired]
	public float ItemHeight { get; set; }

	/// <summary>Visible height of the list container. Default "400px".</summary>
	[Parameter]
	public string Height { get; set; } = "400px";

	/// <summary>Number of extra items rendered above and below the viewport. Default 3.</summary>
	[Parameter]
	public int OverscanCount { get; set; } = 3;

	/// <summary>
	///     Callback when an item is clicked, or activated with Enter or Space. Setting it makes the
	///     list a listbox the keyboard can reach.
	/// </summary>
	[Parameter]
	public EventCallback<TItem> OnItemClick { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-virtual-list";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-virtual-list--clickable", OnItemClick.HasDelegate)
		.AddClass("moka-virtual-list--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("height", Height)
		.AddStyle(Style)
		.Build();

	private bool Interactive => OnItemClick.HasDelegate && !Disabled;

	private string ItemHeightValue => ItemHeight.ToString(CultureInfo.InvariantCulture);

	// Invariant, so a comma-decimal culture cannot turn it into invalid CSS.
	private string? ItemStyle => new StyleBuilder()
		.AddStyle("height", $"{ItemHeightValue}px")
		.Build();

	// Only the rendered items are in the DOM, so each says how many there are and where it is.
	private string SetSize => Items.Count.ToString(CultureInfo.InvariantCulture);

	private string? ActiveDescendant => Interactive && _activeIndex >= 0 ? ItemId(_activeIndex) : null;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (!ReferenceEquals(Items, _rows.Source))
		{
			_rows = new MokaVirtualListRows<TItem>(Items);
		}

		// The list can shrink under the active item.
		_activeIndex = Math.Min(_activeIndex, Items.Count - 1);
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The keys are read in the browser, where the list can tell its own keys from ones pressed
		// in content inside an item, and where it can scroll.
		if (Interactive && !_bound)
		{
			_bound = true;
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(ModulePath, "bindList", _container, _dotNetRef);
		}
	}

	/// <summary>
	///     Moves the active item for a navigation key and returns its index, which the script
	///     scrolls into view. Called from the list's script.
	/// </summary>
	/// <param name="key">ArrowDown, ArrowUp, PageDown, PageUp, Home or End, or Focus when the list takes focus.</param>
	/// <param name="firstVisible">Index of the first item in view, where the first key starts.</param>
	/// <param name="pageSize">How many items fit in view.</param>
	/// <returns>The active item's index, or -1 when the list is empty.</returns>
	[JSInvokable]
	public async Task<int> MoveActive(string key, int firstVisible, int pageSize)
	{
		int count = Items.Count;
		if (!Interactive || count == 0)
		{
			return -1;
		}

		int current = _activeIndex;
		int start = Math.Clamp(firstVisible, 0, count - 1);
		int next = key switch
		{
			"Home" => 0,
			"End" => count - 1,
			_ when current < 0 => start,
			"ArrowDown" => current + 1,
			"ArrowUp" => current - 1,
			"PageDown" => current + Math.Max(pageSize, 1),
			"PageUp" => current - Math.Max(pageSize, 1),
			_ => current
		};

		next = Math.Clamp(next, 0, count - 1);
		if (next != _activeIndex)
		{
			_activeIndex = next;
			await InvokeAsync(StateHasChanged);
		}

		return next;
	}

	/// <summary>Raises <see cref="OnItemClick" /> for the active item. Called from the list's script for Enter and Space.</summary>
	[JSInvokable]
	public async Task ActivateActive()
	{
		if (Interactive && _activeIndex >= 0 && _activeIndex < Items.Count)
		{
			await OnItemClick.InvokeAsync(Items[_activeIndex]);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_dotNetRef?.Dispose();
		_dotNetRef = null;
		await base.DisposeAsyncCore();
	}

	private string ItemId(int index) => $"{_idPrefix}-item-{index.ToString(CultureInfo.InvariantCulture)}";

	private string ItemClass(int index) => new CssBuilder("moka-virtual-list__item")
		.AddClass("moka-virtual-list__item--active", index == _activeIndex)
		.Build();

	private async Task HandleClick(MokaVirtualListRow<TItem> row)
	{
		// The keyboard carries on from the item that was clicked.
		_activeIndex = row.Index;

		if (OnItemClick.HasDelegate)
		{
			await OnItemClick.InvokeAsync(row.Item);
		}
	}
}
