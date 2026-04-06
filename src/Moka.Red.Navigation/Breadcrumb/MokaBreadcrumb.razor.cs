using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Breadcrumb;

/// <summary>
///     A breadcrumb navigation trail. Renders child <see cref="MokaBreadcrumbItem" /> elements
///     separated by configurable separators.
/// </summary>
public partial class MokaBreadcrumb
{
	private readonly List<MokaBreadcrumbItem> _items = [];

	/// <summary>Child breadcrumb items.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Text separator between items. Default "/".</summary>
	[Parameter]
	public string Separator { get; set; } = "/";

	/// <summary>Custom separator content. Overrides <see cref="Separator" /> text when set.</summary>
	[Parameter]
	public RenderFragment? SeparatorContent { get; set; }

	/// <summary>
	///     When set, collapses middle items with an ellipsis if the total item count exceeds this value.
	/// </summary>
	[Parameter]
	public int? MaxItems { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-breadcrumb";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	internal void RegisterItem(MokaBreadcrumbItem item)
	{
		if (!_items.Contains(item))
		{
			_items.Add(item);
			StateHasChanged();
		}
	}

	internal void UnregisterItem(MokaBreadcrumbItem item)
	{
		if (_items.Remove(item))
		{
			StateHasChanged();
		}
	}

	internal bool IsLastItem(MokaBreadcrumbItem item) => _items.Count > 0 && _items[^1] == item;

	internal bool ShouldShowItem(MokaBreadcrumbItem item)
	{
		if (!MaxItems.HasValue || _items.Count <= MaxItems.Value)
		{
			return true;
		}

		int index = _items.IndexOf(item);
		// Show first item and last (MaxItems - 1) items
		return index == 0 || index >= _items.Count - (MaxItems.Value - 1);
	}

	internal bool ShouldShowEllipsis(MokaBreadcrumbItem item)
	{
		if (!MaxItems.HasValue || _items.Count <= MaxItems.Value)
		{
			return false;
		}

		// Show ellipsis after the first item
		return _items.IndexOf(item) == 0;
	}
}
