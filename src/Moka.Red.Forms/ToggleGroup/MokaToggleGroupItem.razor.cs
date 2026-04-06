using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.ToggleGroup;

/// <summary>
///     A single item within a <see cref="MokaToggleGroup" />.
///     Renders as a toggle button that can be selected or deselected.
/// </summary>
public partial class MokaToggleGroupItem : MokaComponentBase
{
	/// <summary>The value identifying this item. Required.</summary>
	[Parameter]
	[EditorRequired]
	public string Value { get; set; } = string.Empty;

	/// <summary>Display text for the item. Optional if <see cref="Icon" /> is provided.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Optional icon for the item.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Whether this item is disabled independently of the group.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>Content rendered inside the toggle button. Overrides <see cref="Text" /> when set.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Parent toggle group.</summary>
	[CascadingParameter]
	public MokaToggleGroup? Parent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-toggle-group-item";

	private bool IsActive => Parent?.IsSelected(Value) == true;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-toggle-group-item--selected", IsActive)
		.AddClass("moka-toggle-group-item--disabled", Disabled)
		.AddClass(Class)
		.Build();

	/// <summary>Active state depends on parent — always re-render.</summary>
	protected override bool ShouldRender() => true;

	private async Task HandleClick()
	{
		if (!Disabled && Parent is not null)
		{
			await Parent.ToggleAsync(Value);
		}
	}
}
