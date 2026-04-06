using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.List;

/// <summary>
///     A single item in a <see cref="MokaList" />, supporting icon, text, secondary text,
///     end content, links, click handlers, and active/disabled states.
/// </summary>
public partial class MokaListItem
{
	/// <summary>Custom child content replacing or augmenting Text/SecondaryText.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Primary text for the list item.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Secondary/description text below the primary text.</summary>
	[Parameter]
	public string? SecondaryText { get; set; }

	/// <summary>Leading icon.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Trailing icon (shown when <see cref="EndContent" /> is null).</summary>
	[Parameter]
	public MokaIconDefinition? EndIcon { get; set; }

	/// <summary>Custom content rendered on the right side (badges, switches, etc.).</summary>
	[Parameter]
	public RenderFragment? EndContent { get; set; }

	/// <summary>Navigation URL. When set, renders as an anchor element.</summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>Click event callback.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>Whether this item is in the active/selected state.</summary>
	[Parameter]
	public bool Active { get; set; }

	/// <summary>Shows a divider line after this item.</summary>
	[Parameter]
	public bool Divider { get; set; }

	/// <summary>Parent list reference for cascaded configuration.</summary>
	[CascadingParameter]
	public MokaList? ParentList { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-list-item";

	private bool IsLink => !string.IsNullOrEmpty(Href);

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-list-item--active", Active)
		.AddClass("moka-list-item--disabled", Disabled)
		.AddClass("moka-list-item--divider", Divider)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private async Task HandleClick(MouseEventArgs args)
	{
		if (!Disabled && OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(args);
		}
	}
}
