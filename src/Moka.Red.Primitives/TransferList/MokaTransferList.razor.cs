using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.TransferList;

/// <summary>
///     A dual-list picker component where items can be moved between "Available" and "Selected" lists.
///     Supports custom item templates, search filtering, and bulk transfer operations.
/// </summary>
/// <typeparam name="TItem">The type of the items in the lists.</typeparam>
public partial class MokaTransferList<TItem> : MokaVisualComponentBase
{
	private readonly HashSet<int> _availableChecked = [];
	private readonly HashSet<int> _selectedChecked = [];
	private string _availableSearch = string.Empty;
	private string _selectedSearch = string.Empty;

	/// <summary>The items available for selection (left list).</summary>
	[Parameter]
	public IList<TItem> AvailableItems { get; set; } = [];

	/// <summary>The items that have been selected (right list).</summary>
	[Parameter]
	public IList<TItem> SelectedItems { get; set; } = [];

	/// <summary>Optional template for rendering each item. Falls back to <c>ToString()</c>.</summary>
	[Parameter]
	public RenderFragment<TItem>? ItemTemplate { get; set; }

	/// <summary>Title for the available items list.</summary>
	[Parameter]
	public string AvailableTitle { get; set; } = "Available";

	/// <summary>Title for the selected items list.</summary>
	[Parameter]
	public string SelectedTitle { get; set; } = "Selected";

	/// <summary>Whether the lists include a search/filter input.</summary>
	[Parameter]
	public bool Searchable { get; set; }

	/// <summary>Callback invoked after items are transferred between lists.</summary>
	[Parameter]
	public EventCallback OnTransfer { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-transfer-list";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-transfer-list--{SizeToKebab(Size)}")
		.AddClass("moka-transfer-list--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	private IEnumerable<TItem> FilteredAvailable =>
		string.IsNullOrWhiteSpace(_availableSearch)
			? AvailableItems
			: AvailableItems.Where(i =>
				(i?.ToString() ?? "").Contains(_availableSearch, StringComparison.OrdinalIgnoreCase));

	private IEnumerable<TItem> FilteredSelected =>
		string.IsNullOrWhiteSpace(_selectedSearch)
			? SelectedItems
			: SelectedItems.Where(i =>
				(i?.ToString() ?? "").Contains(_selectedSearch, StringComparison.OrdinalIgnoreCase));

	private bool HasAvailableChecked => _availableChecked.Count > 0;
	private bool HasSelectedChecked => _selectedChecked.Count > 0;

	private void ToggleAvailableCheck(int index)
	{
		if (!_availableChecked.Remove(index))
		{
			_availableChecked.Add(index);
		}
	}

	private void ToggleSelectedCheck(int index)
	{
		if (!_selectedChecked.Remove(index))
		{
			_selectedChecked.Add(index);
		}
	}

	private async Task MoveToSelectedAsync()
	{
		var toMove = _availableChecked
			.OrderByDescending(i => i)
			.Where(i => i < AvailableItems.Count)
			.Select(i => AvailableItems[i])
			.ToList();

		foreach (TItem item in toMove)
		{
			AvailableItems.Remove(item);
			SelectedItems.Add(item);
		}

		_availableChecked.Clear();
		await OnTransfer.InvokeAsync();
	}

	private async Task MoveToAvailableAsync()
	{
		var toMove = _selectedChecked
			.OrderByDescending(i => i)
			.Where(i => i < SelectedItems.Count)
			.Select(i => SelectedItems[i])
			.ToList();

		foreach (TItem item in toMove)
		{
			SelectedItems.Remove(item);
			AvailableItems.Add(item);
		}

		_selectedChecked.Clear();
		await OnTransfer.InvokeAsync();
	}

	private void OnAvailableSearch(ChangeEventArgs e) => _availableSearch = e.Value?.ToString() ?? string.Empty;

	private void OnSelectedSearch(ChangeEventArgs e) => _selectedSearch = e.Value?.ToString() ?? string.Empty;
}
