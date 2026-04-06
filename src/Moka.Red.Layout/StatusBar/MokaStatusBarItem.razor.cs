using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.StatusBar;

/// <summary>
///     Individual item within a <see cref="MokaStatusBar" />.
///     Supports text, icon, click handling, and tooltip.
/// </summary>
public partial class MokaStatusBarItem : MokaComponentBase
{
	/// <summary>Custom content. Overrides <see cref="Text" /> and <see cref="Icon" />.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Text to display.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Icon to display before the text.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Click handler. When set, the item becomes interactive.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>Tooltip text shown on hover.</summary>
	[Parameter]
	public string? Tooltip { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-statusbar-item";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-statusbar-item--clickable", OnClick.HasDelegate)
		.AddClass(Class)
		.Build();

	private bool IsClickable => OnClick.HasDelegate;

	private async Task HandleClick(MouseEventArgs args)
	{
		if (IsClickable)
		{
			await OnClick.InvokeAsync(args);
		}
	}
}
