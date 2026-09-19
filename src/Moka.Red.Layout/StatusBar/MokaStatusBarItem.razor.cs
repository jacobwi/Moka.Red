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
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private ElementReference _element;
	private bool _keysBound;

	/// <summary>Custom content. Overrides <see cref="Text" /> and <see cref="Icon" />.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Text to display.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Icon to display before the text.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>
	///     Click handler. When set, the item is a button: it joins the tab order, and Enter or Space
	///     click it.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>Tooltip text shown on hover. An icon-only item also takes its accessible name from it.</summary>
	[Parameter]
	public string? Tooltip { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-statusbar-item";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-statusbar-item--clickable", IsClickable)
		.AddClass(Class)
		.Build();

	private bool IsClickable => OnClick.HasDelegate;

	private int? TabIndex => IsClickable ? 0 : null;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Enter and Space are handled in the browser, for the item itself only, and Space does not
		// scroll the page. Static items never load the script.
		if (IsClickable && !_keysBound)
		{
			_keysBound = true;
			await SafeModuleInvokeVoidAsync(KeysModule, "bindActivation", _element);
		}
	}

	private async Task HandleClick(MouseEventArgs args)
	{
		if (IsClickable)
		{
			await OnClick.InvokeAsync(args);
		}
	}
}
