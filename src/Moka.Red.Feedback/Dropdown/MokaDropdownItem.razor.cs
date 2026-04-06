using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Feedback.Dropdown;

/// <summary>
///     An individual item within a <see cref="MokaDropdown" />.
///     Supports text, icon, click handler, disabled state, and divider mode.
/// </summary>
public partial class MokaDropdownItem : MokaComponentBase
{
	/// <summary>Custom content for the item. Overrides <see cref="Text" />.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Text label for the item.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Optional icon displayed before the text.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Click handler for the item.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>Whether the item is disabled.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <summary>When true, renders as a horizontal divider instead of a menu item.</summary>
	[Parameter]
	public bool Divider { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-dropdown-item";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-dropdown-item--disabled", Disabled)
		.AddClass(Class)
		.Build();

	private async Task HandleClick(MouseEventArgs e)
	{
		if (Disabled)
		{
			return;
		}

		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(e);
		}
	}
}
